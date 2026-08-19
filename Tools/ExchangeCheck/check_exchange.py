#!/usr/bin/env python3
"""
Nachtelijke controle van een exchange-run.

Leest alles wat een afgeronde (of nog draaiende) scannersessie in EEN datamap heeft achtergelaten
en maakt daar een markdown-rapport van met een oordeel per onderwerp:

    instellingen - staat de handel aan, hoe, en met welke grenzen
    symbolen     - de instrumentenlijst in CryptoScanBot.db
    candles      - de candle-voorraad "<Exchange>.db": dekking, gaten, plausibiliteit
    barometer    - de $BMP-reeks, met eigen regels want dit zijn geen gewone candles
    streams      - websocket-abonnementen zoals gelogd: starts, drops, herstel, herstarts
    fouten       - het foutenlog, gegroepeerd op genormaliseerde melding
    signalen     - signalen, zones en posities uit het venster
    geheugen     - groei van het proces over de nacht (vereist sample-process.ps1)

Er wordt niets naar de datamap geschreven; elke database gaat alleen-lezen open.

Gebruik:
    python check_exchange.py --folder "CryptoScanBot-KRTest"
    python check_exchange.py --folder "%APPDATA%\\CryptoScanBot\\Data\\Kraken" --out rapport.md
    python check_exchange.py --folder ... --memory-csv geheugen.csv --json rapport.json

Een mapnaam zonder padscheiding wordt binnen %APPDATA% opgezocht.
Het venster (--start/--end) valt terug op de eerste en laatste tijdstempel in het log.
"""

import argparse
import json
import os
import re
import socket
import sqlite3
import subprocess
import sys
from collections import Counter, defaultdict
from datetime import datetime, timedelta, timezone
from pathlib import Path

# ----------------------------------------------------------------------------------------------
# Drempelwaarden. Alles wat "goed / aandacht / slecht" bepaalt staat hier, zodat een regel
# aanscherpen een wijziging van een regel is in plaats van een zoektocht door de rapportagecode.
# ----------------------------------------------------------------------------------------------
LAG_ATTENTION_MINUTES = 5          # nieuwste 1m-candle achter het einde van het venster
LAG_BAD_MINUTES = 30
# Ruimere grenzen zolang het proces nog draait, en niet zomaar ruimer: de opslagdraad houdt de
# nieuwste candles in het geheugen en schrijft ze eens per UUR weg
# (ScannerSession.SaveCandleDataIntervalSeconds = 1 * 60 * 60). Vlak voor die ronde is de jongste
# candle op schijf dus bijna een uur oud terwijl er niets aan de hand is. Zonder dit onderscheid
# staat elk rapport van een levende scanner op oranje, en een oranje dat niets betekent leert je
# oranje negeren. Loopt de achterstand op tot boven twee wegschrijfrondes, dan is er wel iets mis.
# Deze grenzen gelden ook voor de barometer: die gaat via dezelfde opslagronde naar schijf.
LAG_RUNNING_ATTENTION_MINUTES = 75
LAG_RUNNING_BAD_MINUTES = 130
# Zo vers moet de laatste logregel zijn om het proces als "draait nog" te lezen. Alleen de terugval
# voor mappen zonder lock: normaal wordt het proces uit CryptoScanBot.lock.info gecontroleerd.
STILL_RUNNING_MINUTES = 10
GAP_ATTENTION_PERCENTAGE = 1.0     # ontbrekende minuten als aandeel van het gedekte bereik
GAP_BAD_PERCENTAGE = 5.0
NO_CANDLE_ATTENTION_PERCENTAGE = 10.0   # symbolen in de instrumentenlijst zonder enige candle
NO_CANDLE_BAD_PERCENTAGE = 40.0
DROPS_ATTENTION_PER_HOUR = 2.0     # verbroken websocketverbindingen per uur
DROPS_BAD_PER_HOUR = 10.0
OUTAGE_ATTENTION_MINUTES = 5.0     # zolang duurde de langste onderbreking voordat het opvalt
ERRORS_ATTENTION = 1               # regels in het foutenlog
ERRORS_BAD = 50
MEMORY_ATTENTION_MB_PER_HOUR = 50.0
MEMORY_BAD_MB_PER_HOUR = 200.0
MEMORY_MINIMAL_HOURS = 1.0         # hieronder zegt de helling niets, dus wordt er niet geoordeeld
# Houden de metingen langer dan dit voor het einde van het venster op, dan is het proces uit beeld
# geraakt en gaat een oordeel over de verkeerde uren. Ruim boven het steekproefinterval van vijf
# minuten, zodat een laatste ronde die net niet meer paste geen melding oplevert.
MEMORY_COVERAGE_GAP_MINUTES = 30.0

# Barometer. Het aantal munten waarop een meting rust is de betrouwbaarheidsmaat: zakt dat ver weg,
# dan beschrijft de barometer nog maar een restje markt. Gemeten tegen het hoogste aantal van
# diezelfde nacht, niet tegen de abonnementen - zie de toelichting bij de controle zelf.
BAROMETER_COINS_ATTENTION_SHARE = 80.0   # percentage van het eigen hoogtepunt
BAROMETER_COINS_BAD_SHARE = 50.0
# Het begin van een run telt niet mee voor dat minimum: de abonnementen worden dan nog opgebouwd,
# dus het aantal munten groeit in de eerste minuten naar zijn niveau toe. Zonder deze marge meldt
# elke nette run de opstartdip als een gebrek.
BAROMETER_WARMUP_MINUTES = 30
# Die marge is alleen niet genoeg: bij een exchange met een strenge snelheidsbegrenzing loopt de
# inhaalslag uren door, en zolang die bezig is groeit het aantal munten nog. Daarom wordt niet het
# minimum over het hele venster beoordeeld maar het minimum NA het hoogtepunt - een zuiver stijgende
# reeks is gezond, hoe laag hij ook begon; een terugval na de piek is de melding waard.
# Een barometer die op minder munten dan dit rust beschrijft geen markt en krijgt geen oordeel:
# een quote-munt met een of twee genoteerde munten (Bitvavo USDC, Bybit EU EUR) is een instelling,
# geen gebrek.
BAROMETER_MINIMAL_COINS = 5
# En zelfs dat minimum is nog te scherp, want er zit een kortstondige dip in de reeks die niets met
# de gezondheid van de nacht te maken heeft: bij elke verversing van de symbolenlijst (circa elke
# drie uur) telt de barometer een paar minuten lang minder munten mee. Op HyperLiquid Futures zakte
# hij daardoor vijf keer per nacht van 70 naar 14 a 39 en stond hij binnen vijf minuten weer op 68.
# Een terugval telt daarom pas als hij zo veel metingen achter elkaar aanhoudt. Een echte instorting
# (HyperLiquid Spot, 18-08-2026) houdt de hele nacht aan en wordt hier niet door gemist.
# Regels die in het foutenlog belanden maar niet van de scanner zijn. Ze verdwijnen niet uit het
# rapport - ze worden apart geteld en getoond - maar ze bepalen het oordeel niet.
#
# De Avalonia-melding is de tekenlaag van het venster die zijn beeldcommit niet op tijd rond krijgt en
# zelf doortikt. Met twintig scanners op een machine is dat drukte op de grafische kaart en verder
# niets: hij raakt de scan niet, er gaat geen candle verloren, en er is van onze kant niets aan te
# doen behalve minder vensters tegelijk openen. In de nacht van 18/19-08-2026 waren het 73 van de 131
# foutregels, waarmee elke exchange op "aandacht" stond om iets waar geen beslissing uit volgt.
ERRORS_NOT_OURS = (
    "[Avalonia/Visual] RunLoopHandler",
)

BAROMETER_DIP_MINUTES = 10
# De grens die SubscriptionManager.MaximumTickerSilence in de scanner aanhoudt. Alleen om de meting
# in het rapport ernaast te kunnen zetten; wijzigen hier verandert niets in de scanner. Houd hem
# gelijk aan de constante daar (5 minuten sinds 18-08-2026).
SILENCE_LIMIT_MINUTES = 5
# Een gemiddelde verder dan dit van nul is geen markt maar een storing. Zelfde grens die de grafiek
# hanteert (BarometerCandleFields.GetScale, IgnoreBeyond).
BAROMETER_EXTREME_PERCENTAGE = 50.0
BAROMETER_LAG_ATTENTION_MINUTES = 15     # nieuwste barometermeting achter het venstereinde
BAROMETER_LAG_BAD_MINUTES = 60
# Op een draaiend proces gelden LAG_RUNNING_*: de barometer wordt door dezelfde opslagronde
# weggeschreven als de gewone candles, dus hij heeft daar geen eigen grenzen voor nodig.

# De candle-voorraad houdt OpenTime als hele minuten sinds dit tijdstip (CryptoCandle.CandleTime.
# Epoch, een maandag zodat modulo ten opzichte van het tijdstip weekcandles op maandag 00:00 UTC
# uitlijnt).
CANDLE_EPOCH = datetime(2010, 1, 4, 0, 0, 0)

# Het log wordt in LOKALE tijd geschreven (NLog ${longdate}), terwijl candletijden en
# Signal.OpenDate in UTC staan (CandleTime.ToDateTime rekent vanaf het tijdstip hierboven). Ze
# rechtstreeks vergelijken geeft een "twee uur achter" die in werkelijkheid een minuut is, dus
# to_utc rekent het venster om voordat het een database raakt.

# Barometermetingen worden als candles onder een pseudosymbool bewaard. Het zijn geen koersen maar
# percentages, dus ze krijgen hun eigen sectie met eigen regels in plaats van te worden
# meegerekend met de echte candles.
BAROMETER_PREFIXES = ("$BMP", "$BMV")

# Wat er in de prijsvelden van een barometercandle staat. Dit volgt BarometerCandleFields.Store in
# Core/Barometer: een barometer is een enkel getal, dus de vier prijsvelden dragen de losse cijfers
# van dezelfde meting. Hoog en laag zijn hier dus NIET de hoogste en laagste waarde.
BAROMETER_FIELDS = (
    ("Open", "Mediaan"),
    ("High", "Percentage stijgers"),
    ("Low", "Spreiding"),
    ("Close", "Gemiddelde (de barometerwaarde zelf)"),
    ("Volume", "Aantal munten waarop gemeten is"),
)

# Het instellingenbestand bewaart enums als getal. Hier vertaald zodat het rapport leest als de
# gebruikersinterface in plaats van als de json. Waarden volgen CryptoTradeVia en CryptoOrderType
# in Core/Enums.
TRADE_VIA = {1: "PaperTrade", 2: "RealTrading", 3: "Altrady", 4: "PaperTradingAndAltrady"}
ORDER_TYPE = {0: "Market", 1: "Limit", 2: "StopLimit", 3: "Oco"}
TRADE_SIDE = {0: "long", 1: "short"}

# De codes blijven Engels omdat ze de json in gaan: check_all.py leest ze, en een reeks json's van
# meerdere nachten moet vergelijkbaar blijven over deze vertaalslag heen. Alleen de weergave is
# Nederlands, via VERDICT_TEXT hieronder.
GOOD, ATTENTION, BAD, UNKNOWN = "good", "attention", "bad", "unknown"
VERDICT_TEXT = {GOOD: "goed", ATTENTION: "aandacht", BAD: "slecht", UNKNOWN: "onbekend"}

# Gekleurde bollen in plaats van tekst: markdown kent geen kleur, maar deze tekens worden overal
# gekleurd getoond. Zo springt de aandacht meteen naar de secties die er toe doen.
VERDICT_MARK = {GOOD: "\U0001F7E2", ATTENTION: "\U0001F7E0", BAD: "\U0001F534", UNKNOWN: "⚪"}
VERDICT_RANK = {GOOD: 0, UNKNOWN: 1, ATTENTION: 2, BAD: 3}

# De onderwerpen die een nacht echt kunnen afkeuren. Deze komen bovenaan te staan met hun getal
# erbij; de rest van het rapport is naslag en verklaart meestal een instelling, geen gebrek.
DECISIVE_KEYS = ("streams", "candles", "barometer", "memory")


# ==============================================================================================
# Kleine hulpfuncties
# ==============================================================================================
def candle_time_to_datetime(minutes):
    return CANDLE_EPOCH + timedelta(minutes=int(minutes))


def datetime_to_candle_time(moment):
    return int((moment - CANDLE_EPOCH).total_seconds() // 60)


def parse_moment(text):
    """Accepteert de vormen die de scanner schrijft: ISO, met of zonder fracties van seconden."""
    if not text:
        return None
    text = str(text).strip().replace("T", " ")
    for pattern in ("%Y-%m-%d %H:%M:%S.%f", "%Y-%m-%d %H:%M:%S", "%Y-%m-%d %H:%M", "%Y-%m-%d"):
        try:
            return datetime.strptime(text[:26] if "." in text else text, pattern)
        except ValueError:
            continue
    return None


def to_utc(moment):
    """Lokale kloktijd (logregels, --start/--end) naar de UTC die de databases bewaren."""
    if moment is None:
        return None
    return moment.astimezone(timezone.utc).replace(tzinfo=None)


def is_barometer(name):
    return bool(name) and any(str(name).upper().startswith(prefix) for prefix in BAROMETER_PREFIXES)


def open_readonly(path):
    """Open een database zonder hem ooit aan te raken - een draaiende scanner schrijft hierin."""
    uri = "file:{}?mode=ro".format(str(path).replace("?", "%3f").replace("#", "%23"))
    connection = sqlite3.connect(uri, uri=True, timeout=5)
    connection.row_factory = sqlite3.Row
    return connection


def worst(*verdicts):
    return max(verdicts, key=lambda verdict: VERDICT_RANK[verdict])


def percentage(part, whole):
    return 100.0 * part / whole if whole else 0.0


# ==============================================================================================
# Html-weergave
# ==============================================================================================
# De secties worden als markdown opgebouwd omdat dat prettig schrijft in de controles zelf. Voor het
# lezen is html beter: kleur per oordeel, een inhoudsopgave die meescrolt, en geen editor nodig om
# het te openen. Onderstaande omzetting kent alleen de markdown die de controles ook echt gebruiken
# - tabellen, koppen, opsommingen, **vet** en `code`. Meer hoeft niet, en een volledige
# markdown-bibliotheek zou een afhankelijkheid zijn voor een script dat nu met kale Python draait.
INLINE_CODE = re.compile(r"`([^`]+)`")
INLINE_BOLD = re.compile(r"\*\*([^*]+)\*\*")
TABLE_DIVIDER = re.compile(r"^\|[\s:|-]+\|$")


def html_inline(text):
    """Escaped de tekst en zet daarna alleen de opmaak terug die we zelf geschreven hebben."""
    out = (text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;"))
    # De enige tag die de controles zelf schrijven (meerdere meetwaarden in een cel). Hij is er net
    # uitgeknikkerd door het escapen, dus hier weer terug - de rest blijft leesbare tekst, wat nodig
    # is omdat foutmeldingen dingen als "SortedSet`1" en "<n>" bevatten.
    out = out.replace("&lt;br&gt;", "<br>")
    out = INLINE_CODE.sub(lambda m: "<code>{}</code>".format(m.group(1)), out)
    out = INLINE_BOLD.sub(lambda m: "<strong>{}</strong>".format(m.group(1)), out)
    return out


def html_from_markdown(lines):
    """Zet de regels van een sectie om naar html. Zie de toelichting bij INLINE_CODE."""
    out = []
    index = 0
    while index < len(lines):
        line = lines[index]
        stripped = line.strip()

        if not stripped:
            index += 1
            continue

        # Tabel: kopregel, scheidingsregel, dan de rijen tot de eerste regel die geen pipe meer is.
        if stripped.startswith("|") and index + 1 < len(lines) and TABLE_DIVIDER.match(
                lines[index + 1].strip()):
            headers = [cell.strip() for cell in stripped.strip("|").split("|")]
            index += 2
            body = []
            while index < len(lines) and lines[index].strip().startswith("|"):
                body.append([cell.strip() for cell in lines[index].strip().strip("|").split("|")])
                index += 1
            # De tabel krijgt een eigen schuifbalk: de foutentabellen bevatten stacktraces die op
            # een smal scherm anders de hele pagina laten schuiven.
            out.append('<div class="scroll"><table>')
            out.append("<thead><tr>{}</tr></thead>".format(
                "".join("<th>{}</th>".format(html_inline(cell)) for cell in headers)))
            out.append("<tbody>")
            for row in body:
                cells = "".join('<td{}>{}</td>'.format(verdict_cell_class(cell), html_inline(cell))
                                for cell in row)
                out.append("<tr>{}</tr>".format(cells))
            out.append("</tbody></table></div>")
            continue

        if stripped.startswith("- "):
            out.append("<ul>")
            while index < len(lines) and lines[index].strip().startswith("- "):
                out.append("<li>{}</li>".format(html_inline(lines[index].strip()[2:])))
                index += 1
            out.append("</ul>")
            continue

        if stripped.startswith("#"):
            level = len(stripped) - len(stripped.lstrip("#"))
            out.append("<h{0}>{1}</h{0}>".format(min(level + 1, 6),
                                                 html_inline(stripped.lstrip("#").strip())))
            index += 1
            continue

        # Losse regels achter elkaar horen bij dezelfde alinea; een lege regel sluit hem af.
        paragraph = []
        while index < len(lines) and lines[index].strip() and \
                not lines[index].strip().startswith(("|", "- ", "#")):
            paragraph.append(lines[index].strip())
            index += 1
        out.append("<p>{}</p>".format(html_inline(" ".join(paragraph))))
    return "\n".join(out)


def verdict_cell_class(cell):
    """Kleurt de cel waarin alleen een oordeelbolletje staat, zodat het in een oogopslag leest."""
    for verdict, mark in VERDICT_MARK.items():
        if cell.strip() == mark:
            return ' class="mark {}"'.format(verdict)
    return ""


PAGE_STYLE = """
:root {
  color-scheme: light dark;
  --bg: #f6f7f9; --card: #ffffff; --ink: #1c2024; --soft: #5c6570; --line: #d8dde3;
  --good: #1a7f45; --good-bg: #e6f4ec;
  --attention: #a8620a; --attention-bg: #fdf0dd;
  --bad: #b3261e; --bad-bg: #fbe6e5;
  --unknown: #5c6570; --unknown-bg: #eceff2;
  --accent: #1f5fa8;
}
@media (prefers-color-scheme: dark) {
  :root {
    --bg: #14171a; --card: #1c2024; --ink: #e6e9ec; --soft: #99a2ac; --line: #2e343b;
    --good: #5bcf8b; --good-bg: #16301f;
    --attention: #e5a13d; --attention-bg: #33260f;
    --bad: #f0847c; --bad-bg: #351b1a;
    --unknown: #99a2ac; --unknown-bg: #24292e;
    --accent: #74a9e8;
  }
}
* { box-sizing: border-box; }
body { margin: 0; background: var(--bg); color: var(--ink);
       font: 15px/1.55 "Segoe UI", system-ui, sans-serif; }
.layout { display: flex; align-items: flex-start; gap: 24px; max-width: 1500px;
          margin: 0 auto; padding: 24px; }
nav { position: sticky; top: 24px; flex: 0 0 250px; background: var(--card);
      border: 1px solid var(--line); border-radius: 10px; padding: 14px 16px;
      max-height: calc(100vh - 48px); overflow-y: auto; }
nav h2 { font-size: 12px; text-transform: uppercase; letter-spacing: .07em;
         color: var(--soft); margin: 0 0 10px; }
nav a { display: flex; align-items: center; gap: 8px; padding: 5px 8px; margin: 0 -8px;
        border-radius: 6px; color: var(--ink); text-decoration: none; font-size: 14px; }
nav a:hover { background: var(--bg); }
nav a .dot { width: 9px; height: 9px; border-radius: 50%; flex: 0 0 auto; }
main { flex: 1 1 auto; min-width: 0; }
section, .card { background: var(--card); border: 1px solid var(--line); border-radius: 10px;
                 padding: 18px 22px; margin-bottom: 18px; }
h1 { font-size: 24px; margin: 0 0 4px; }
.subtitle { color: var(--soft); margin: 0 0 18px; }
h2 { font-size: 18px; margin: 0 0 12px; display: flex; align-items: center; gap: 10px; }
h3 { font-size: 15px; margin: 18px 0 8px; color: var(--soft);
     text-transform: uppercase; letter-spacing: .05em; }
h4 { font-size: 15px; margin: 16px 0 8px; }
p { margin: 0 0 10px; }
ul { margin: 0 0 12px; padding-left: 20px; }
li { margin-bottom: 3px; }
code { background: var(--bg); border: 1px solid var(--line); border-radius: 4px;
       padding: 1px 5px; font: 13px/1.4 Consolas, "Cascadia Mono", monospace;
       word-break: break-word; }
.scroll { overflow-x: auto; margin: 0 0 14px; }
table { border-collapse: collapse; width: 100%; font-size: 14px; }
th, td { border-bottom: 1px solid var(--line); padding: 7px 10px; text-align: left;
         vertical-align: top; }
th { color: var(--soft); font-weight: 600; font-size: 12px; text-transform: uppercase;
     letter-spacing: .05em; white-space: nowrap; }
tbody tr:hover { background: var(--bg); }
td.mark { width: 1%; text-align: center; font-size: 15px; }
td.mark.good { background: var(--good-bg); }
td.mark.attention { background: var(--attention-bg); }
td.mark.bad { background: var(--bad-bg); }
td.mark.unknown { background: var(--unknown-bg); }
.badge { display: inline-block; padding: 2px 10px; border-radius: 999px; font-size: 12px;
         font-weight: 600; text-transform: uppercase; letter-spacing: .05em; }
.badge.good { color: var(--good); background: var(--good-bg); }
.badge.attention { color: var(--attention); background: var(--attention-bg); }
.badge.bad { color: var(--bad); background: var(--bad-bg); }
.badge.unknown { color: var(--unknown); background: var(--unknown-bg); }
.verdict { font-size: 19px; font-weight: 600; display: flex; align-items: center; gap: 12px; }
.verdict.good { color: var(--good); }
.verdict.attention { color: var(--attention); }
.verdict.bad { color: var(--bad); }
.verdict.unknown { color: var(--unknown); }
section { border-left: 4px solid var(--line); }
section.good { border-left-color: var(--good); }
section.attention { border-left-color: var(--attention); }
section.bad { border-left-color: var(--bad); }
section.unknown { border-left-color: var(--unknown); }
.dot.good { background: var(--good); }
.dot.attention { background: var(--attention); }
.dot.bad { background: var(--bad); }
.dot.unknown { background: var(--unknown); }
.top { font-size: 13px; color: var(--accent); text-decoration: none; margin-left: auto;
       font-weight: 400; }
.top:hover { text-decoration: underline; }
@media (max-width: 900px) {
  .layout { flex-direction: column; padding: 14px; }
  nav { position: static; flex: 1 1 auto; width: 100%; max-height: none; }
}
"""


class Report:
    """Verzamelt secties en hun oordeel; levert aan het eind markdown, html en json."""

    def __init__(self):
        self.sections = []

    def add(self, key, title, verdict, lines, facts=None, key_points=None):
        self.sections.append({
            "key": key,
            "title": title,
            "verdict": verdict,
            "lines": lines,
            "facts": facts or {},
            # Korte regels voor bovenaan het rapport: de getallen die een nacht afkeuren. De sectie
            # zelf blijft de plek voor de onderbouwing.
            "keyPoints": key_points or [],
        })

    def overall(self):
        return worst(*[section["verdict"] for section in self.sections]) if self.sections else UNKNOWN

    def _key_point_lines(self):
        """De kop van het rapport: eerst waar je naar moet kijken, dan pas de rest."""
        out = ["## Waar moet je naar kijken", ""]
        attention = [section for section in self.sections if section["verdict"] in (ATTENTION, BAD)]
        if attention:
            out.append("**{} van de {} onderwerpen vragen aandacht:** {}".format(
                len(attention), len(self.sections),
                ", ".join("{} {}".format(VERDICT_MARK[section["verdict"]], section["title"])
                          for section in attention)))
        else:
            out.append("**Geen enkel onderwerp vraagt aandacht.** Alles staat op {} goed.".format(
                VERDICT_MARK[GOOD]))
        out.append("")

        # De beslissende getallen, ook als het onderwerp goed staat: bij deze vier wil je de waarde
        # zien en niet alleen een bolletje.
        decisive = [section for section in self.sections if section["key"] in DECISIVE_KEYS]
        if decisive:
            out.append("| | Beslissend onderwerp | Meting |")
            out.append("|---|---|---|")
            for section in decisive:
                points = section["keyPoints"] or ["(niet gemeten)"]
                out.append("| {} | {} | {} |".format(
                    VERDICT_MARK[section["verdict"]], section["title"], "<br>".join(points)))
            out.append("")
            out.append("Deze vier bepalen of een nacht deugt. De overige onderwerpen zijn naslag: "
                       "ze verklaren meestal een instelling, niet een gebrek.")
            out.append("")
        return out

    def to_markdown(self, header_lines):
        out = ["# Controle van de exchange-run", ""]
        out.extend(header_lines)
        out.append("")
        out.extend(self._key_point_lines())
        out.append("## Samenvatting")
        out.append("")
        out.append("| | Onderwerp | Oordeel |")
        out.append("|---|---|---|")
        for section in self.sections:
            out.append("| {} | {} | {} |".format(
                VERDICT_MARK[section["verdict"]], section["title"],
                VERDICT_TEXT[section["verdict"]]))
        out.append("")
        out.append("**{} Eindoordeel: {}**".format(
            VERDICT_MARK[self.overall()], VERDICT_TEXT[self.overall()]))
        out.append("")
        for section in self.sections:
            out.append("## {} {} - {}".format(
                VERDICT_MARK[section["verdict"]], section["title"],
                VERDICT_TEXT[section["verdict"]]))
            out.append("")
            out.extend(section["lines"] or ["(niets te melden)"])
            out.append("")
        return "\n".join(out)

    def to_html(self, header_lines, header):
        """Dezelfde inhoud als to_markdown, maar om te lezen in plaats van om te bewerken."""
        overall = self.overall()
        folder_name = Path(header.get("folder", "")).name or "exchange"
        exchanges = ", ".join(header.get("exchanges") or []) or folder_name
        attention = [s for s in self.sections if s["verdict"] in (ATTENTION, BAD)]

        out = ['<!doctype html>', '<html lang="nl">', "<head>", '<meta charset="utf-8">',
               '<meta name="viewport" content="width=device-width, initial-scale=1">',
               "<title>Controle {} - {}</title>".format(
                   html_inline(exchanges), VERDICT_TEXT[overall]),
               "<style>{}</style>".format(PAGE_STYLE), "</head>", "<body>",
               '<div class="layout">']

        # ---- inhoudsopgave -----------------------------------------------------------------
        # Het bolletje staat ernaast zodat je zonder scrollen ziet waar het misging, en de link
        # brengt je er meteen heen. Dat was de reden om dit in html te doen.
        out.append("<nav>")
        out.append("<h2>Onderwerpen</h2>")
        out.append('<a href="#top"><span class="dot {}"></span>Eindoordeel: {}</a>'.format(
            overall, VERDICT_TEXT[overall]))
        for section in self.sections:
            out.append('<a href="#sec-{}"><span class="dot {}"></span>{}</a>'.format(
                section["key"], section["verdict"], html_inline(section["title"])))
        out.append("</nav>")

        out.append("<main>")
        out.append('<div class="card" id="top">')
        out.append("<h1>Controle van de exchange-run</h1>")
        out.append('<p class="subtitle">{}</p>'.format(html_inline(exchanges)))
        out.append('<p class="verdict {}">{} Eindoordeel: {}</p>'.format(
            overall, VERDICT_MARK[overall], VERDICT_TEXT[overall]))
        out.append(html_from_markdown(header_lines))
        out.append("</div>")

        # ---- waar moet je naar kijken -------------------------------------------------------
        out.append('<div class="card">')
        out.append("<h2>Waar moet je naar kijken</h2>")
        if attention:
            out.append("<p><strong>{} van de {} onderwerpen vragen aandacht:</strong> {}</p>".format(
                len(attention), len(self.sections),
                ", ".join('<a href="#sec-{}"><span class="badge {}">{}</span></a>'.format(
                    s["key"], s["verdict"], html_inline(s["title"])) for s in attention)))
        else:
            out.append("<p><strong>Geen enkel onderwerp vraagt aandacht.</strong> "
                       "Alles staat goed.</p>")
        decisive = [s for s in self.sections if s["key"] in DECISIVE_KEYS]
        if decisive:
            out.append('<div class="scroll"><table>')
            out.append("<thead><tr><th></th><th>Beslissend onderwerp</th><th>Meting</th>"
                       "</tr></thead><tbody>")
            for section in decisive:
                points = section["keyPoints"] or ["(niet gemeten)"]
                out.append('<tr><td class="mark {}">{}</td><td><a href="#sec-{}">{}</a></td>'
                           "<td>{}</td></tr>".format(
                               section["verdict"], VERDICT_MARK[section["verdict"]],
                               section["key"], html_inline(section["title"]),
                               "<br>".join(html_inline(point) for point in points)))
            out.append("</tbody></table></div>")
            out.append("<p>Deze vier bepalen of een nacht deugt. De overige onderwerpen zijn "
                       "naslag: ze verklaren meestal een instelling, niet een gebrek.</p>")
        out.append("</div>")

        # ---- samenvatting -------------------------------------------------------------------
        out.append('<div class="card">')
        out.append("<h2>Samenvatting</h2>")
        out.append('<div class="scroll"><table>')
        out.append("<thead><tr><th></th><th>Onderwerp</th><th>Oordeel</th></tr></thead><tbody>")
        for section in self.sections:
            out.append('<tr><td class="mark {0}">{1}</td><td><a href="#sec-{2}">{3}</a></td>'
                       '<td><span class="badge {0}">{4}</span></td></tr>'.format(
                           section["verdict"], VERDICT_MARK[section["verdict"]], section["key"],
                           html_inline(section["title"]), VERDICT_TEXT[section["verdict"]]))
        out.append("</tbody></table></div>")
        out.append("</div>")

        # ---- de secties zelf ----------------------------------------------------------------
        for section in self.sections:
            out.append('<section class="{}" id="sec-{}">'.format(section["verdict"],
                                                                 section["key"]))
            out.append('<h2>{} {} <span class="badge {}">{}</span>'
                       '<a class="top" href="#top">terug naar boven</a></h2>'.format(
                           VERDICT_MARK[section["verdict"]], html_inline(section["title"]),
                           section["verdict"], VERDICT_TEXT[section["verdict"]]))
            out.append(html_from_markdown(section["lines"] or ["(niets te melden)"]))
            out.append("</section>")

        out.extend(["</main>", "</div>", "</body>", "</html>"])
        return "\n".join(out)

    def to_json(self, header):
        return json.dumps({
            "header": header,
            "overall": self.overall(),
            "sections": [{
                "key": section["key"],
                "title": section["title"],
                "verdict": section["verdict"],
                "facts": section["facts"],
            } for section in self.sections],
        }, indent=2, default=str)


# ==============================================================================================
# Mappen zoeken
# ==============================================================================================
def resolve_folder(argument):
    argument = os.path.expandvars(argument)
    path = Path(argument)
    if not path.is_absolute() and not any(separator in argument for separator in ("\\", "/")):
        appdata = os.environ.get("APPDATA")
        if appdata:
            candidate = Path(appdata) / argument
            if candidate.exists():
                return candidate
    return path


def find_candle_databases(folder, wanted_exchange):
    """Elke "<Exchange>.db" naast de hoofddatabase is een candle-voorraad."""
    result = []
    for path in sorted(folder.glob("*.db")):
        if path.name.lower().startswith("cryptoscanbot"):
            continue
        name = path.stem
        if wanted_exchange and name.lower() != wanted_exchange.lower():
            continue
        result.append((name, path))
    return result


def find_main_database(folder):
    for path in sorted(folder.glob("*.db")):
        if path.name.lower().startswith("cryptoscanbot"):
            return path
    return None


def find_logs(folder):
    """Levert (hoofdlog, foutenlog). Gearchiveerde bestanden (met datumsuffix) tellen mee."""
    log_folder = folder / "Log"
    if not log_folder.is_dir():
        return [], []
    main, errors = [], []
    for path in sorted(log_folder.glob("*.log")):
        if " Error" in path.stem:
            errors.append(path)
        elif " Trace" in path.stem or " Debug" in path.stem:
            continue
        else:
            main.append(path)
    return main, errors


def read_lock_owner(folder):
    """
    The scanner writes "pid|machine|start time" into CryptoScanBot.lock.info while it runs. Returns
    (pid, machine, started) or None when the file is missing or unreadable. The file survives a
    crash, so its presence proves nothing on its own - the pid still has to be checked.
    """
    path = folder / "CryptoScanBot.lock.info"
    try:
        parts = path.read_text(encoding="utf-8", errors="replace").strip().split("|")
    except Exception:
        return None
    if len(parts) < 2 or not parts[0].strip().isdigit():
        return None
    return int(parts[0].strip()), parts[1].strip(), (parts[2].strip() if len(parts) > 2 else "")


def process_is_running(folder):
    """
    Is the scanner of this data folder alive right now? Returns (True/False, explanation), or
    (None, "") when it cannot be established - a missing lock, a lock from another machine, or a
    platform where the check does not work. The caller falls back on the log in that case.
    """
    owner = read_lock_owner(folder)
    if owner is None:
        return None, ""
    pid, machine, _ = owner
    if machine and machine.upper() != os.environ.get("COMPUTERNAME", "").upper() \
            and machine.upper() != socket.gethostname().upper():
        return None, ""

    if os.name == "nt":
        try:
            # A pid on its own is not enough: Windows hands the number to the next process once it
            # is free. So the image name has to match as well, otherwise a recycled pid reads as a
            # scanner that never stopped.
            output = subprocess.run(
                ["tasklist", "/FI", "PID eq {}".format(pid), "/FO", "CSV", "/NH"],
                capture_output=True, text=True, timeout=15).stdout
        except Exception:
            return None, ""
        alive = "cryptoscan" in output.lower()
        return alive, "proces {} volgens de lock in de datamap".format(pid)

    try:
        os.kill(pid, 0)
        return True, "proces {} volgens de lock in de datamap".format(pid)
    except ProcessLookupError:
        return False, "proces {} volgens de lock in de datamap".format(pid)
    except Exception:
        return None, ""


# ==============================================================================================
# 1. Instellingen
# ==============================================================================================
def read_json_file(path):
    try:
        with open(path, "r", encoding="utf-8-sig", errors="replace") as handle:
            return json.load(handle)
    except Exception:
        return None


def check_settings(report, folder):
    settings_path = None
    for path in folder.glob("*-settings.json"):
        settings_path = path
        break

    if settings_path is None:
        report.add("settings", "Instellingen", UNKNOWN,
                   ["Geen `*-settings.json` gevonden in de datamap."])
        return {}

    settings = read_json_file(settings_path)
    if settings is None:
        report.add("settings", "Instellingen", UNKNOWN,
                   ["`{}` kon niet als json gelezen worden.".format(settings_path.name)])
        return {}

    trading = settings.get("Trading", {}) or {}
    general = settings.get("General", {}) or {}
    signal = settings.get("Signal", {}) or {}

    active = bool(trading.get("Active", False))
    trade_via = TRADE_VIA.get(trading.get("TradeVia"), trading.get("TradeVia", "?"))
    via_exchange = trading.get("TradeViaExchange", False)
    no_new = trading.get("DisableNewPositions", False)

    lines = [
        "| Instelling | Waarde |",
        "|---|---|",
        "| Exchange | {} |".format(general.get("ExchangeName", "?")),
        "| Exchange waarop gehandeld wordt | {} |".format(
            general.get("ActivateExchangeName") or "(dezelfde)"),
        "| Signaalanalyse actief | {} |".format(signal.get("Active", "?")),
        "| Handel actief | **{}** |".format("JA" if active else "nee"),
        "| Handel via | {} |".format(trade_via),
        "| Handel via exchange | {} |".format(via_exchange),
        "| Nieuwe posities uitgeschakeld | {} |".format(no_new),
        "| Plekken long / short | {} / {} |".format(
            trading.get("SlotsMaximalLong", "?"), trading.get("SlotsMaximalShort", "?")),
        "| Ordertype bij instap | {} |".format(
            ORDER_TYPE.get(trading.get("EntryOrderType"), trading.get("EntryOrderType", "?"))),
        "| Stop loss percentage | {} |".format(trading.get("StopLossPercentage", "?")),
        "| Hefboom | {} |".format(trading.get("Leverage", "?")),
        "",
    ]

    quotes = settings.get("QuoteCoins") or {}
    if isinstance(quotes, dict):
        enabled = ["{} (minimaal volume {:,.0f})".format(name, float(value.get("MinimalVolume") or 0))
                   for name, value in sorted(quotes.items())
                   if (value or {}).get("FetchCandles", False)]
        lines.append("Quote-munten waarvoor candles worden opgehaald: {}".format(
            "; ".join(enabled) if enabled
            else "**geen** - zonder er een heeft de scanner niets te doen"))
        lines.append("")
        if not enabled:
            lines.append("")

    # Signal.Long / Signal.Short bevatten de intervallen en strategieen waarop de analyse draait;
    # dezelfde twee namen onder Trading bevatten waarop de bot mag handelen. Een run met
    # signaalintervallen maar zonder handelsintervallen levert signalen die nooit posities worden -
    # bewust zo, maar het is prettig ze naast elkaar te zien als een nacht niets opleverde.
    for area, block in (("Analyse", signal), ("Handel", trading)):
        for side in ("Long", "Short"):
            section = (block.get(side) or {})
            intervals = section.get("Interval") or []
            strategies = section.get("Strategy") or []
            if intervals or strategies:
                lines.append("{} {}: intervallen [{}], strategieen [{}]".format(
                    area, side.lower(), ", ".join(map(str, intervals)),
                    ", ".join(map(str, strategies))))
    lines.append("")

    # Een api-sleutel hoort in het exchange-instellingenbestand. Alleen melden OF er een is - het
    # rapport is bedoeld om te kunnen delen, dus er gaat nooit een geheim in.
    for path in folder.glob("*-exchange.json"):
        api = read_json_file(path) or {}
        present = []
        for name, value in api.items():
            if isinstance(value, dict):
                has_key = any(str(value.get(field, "")).strip()
                              for field in ("ApiKey", "Key", "ApiSecret", "Secret"))
                if has_key:
                    present.append(name)
        lines.append("Api-gegevens aanwezig voor: {}".format(
            ", ".join(sorted(present)) if present else "(geen)"))
        break

    lines.append("")
    if active:
        lines.append("De handel stond AAN tijdens deze run, dus de posities en orders hieronder "
                     "horen bij de controle.")
    else:
        lines.append("De handel stond UIT tijdens deze run; dit was een sessie die alleen scant.")

    facts = {"tradingActive": active, "tradeVia": trade_via, "tradeViaExchange": via_exchange}
    report.add("settings", "Instellingen", GOOD, lines, facts)
    return facts


# ==============================================================================================
# 2. Symbolen (hoofddatabase)
# ==============================================================================================
def as_number(value):
    """
    Tick sizes and volumes are stored as TEXT in the symbol table, so "0.0" is a non-empty string
    and therefore true in Python. Testing such a field for truth accepts a zero and lets it reach a
    division; compare the number instead of the text.
    """
    try:
        return float(value)
    except (TypeError, ValueError):
        return 0.0


def check_symbols(report, main_db, exchange_names):
    if main_db is None:
        report.add("symbols", "Symbolen", UNKNOWN, ["Geen hoofddatabase gevonden."])
        return {}

    connection = open_readonly(main_db)
    try:
        exchanges = {row["Name"]: row["Id"] for row in
                     connection.execute("SELECT Id, Name FROM Exchange")}
        lines = []
        verdict = GOOD
        facts = {}

        for name in exchange_names:
            exchange_id = exchanges.get(name)
            lines.append("### {}".format(name))
            lines.append("")
            if exchange_id is None:
                lines.append("Komt niet voor in de Exchange-tabel.")
                verdict = worst(verdict, BAD)
                continue

            rows = list(connection.execute(
                "SELECT Name, Quote, Base, ExchangeName, Status, Volume, PriceTickSize, "
                "QuantityTickSize, IsSpotTradingAllowed FROM Symbol WHERE ExchangeId = ?",
                (exchange_id,)))
            total = len(rows)
            if total == 0:
                lines.append("Geen symbolen bewaard voor deze exchange.")
                verdict = worst(verdict, BAD)
                continue

            # The barometer is stored as a symbol but is not an instrument: it carries percentages,
            # not prices, and older rows have no tick size at all. Judging it by the rules for a
            # tradable symbol turns a healthy exchange red for a pseudo-symbol nobody trades.
            # Same recognition as CandleHelpers.IsBarometerSymbol: the base starts with a dollar.
            # A symbol the exchange no longer lists is never priced either, so a missing tick size
            # on an inactive row is leftover, not a defect. It stays visible in the count below.
            instruments = [row for row in rows
                           if not (row["Base"] or "").startswith("$")
                           and (row["Status"] in (None, "") or as_number(row["Status"]) == 1)]

            per_quote = Counter(row["Quote"] or "?" for row in rows)
            # An empty or missing status means active; a stored zero means it really is inactive.
            inactive = sum(1 for row in rows
                           if row["Status"] not in (None, "") and as_number(row["Status"]) != 1)
            no_price_tick = [row["Name"] or row["ExchangeName"] or "(naamloos)"
                             for row in instruments if as_number(row["PriceTickSize"]) <= 0]
            no_quantity_tick = [row["Name"] or row["ExchangeName"] or "(naamloos)"
                                for row in instruments if as_number(row["QuantityTickSize"]) <= 0]
            no_exchange_name = [row["Name"] for row in rows if not (row["ExchangeName"] or "").strip()]
            zero_volume = sum(1 for row in rows if not row["Volume"])

            lines.append("| Meting | Waarde |")
            lines.append("|---|---|")
            lines.append("| Symbolen | {} |".format(total))
            lines.append("| Per quote | {} |".format(", ".join(
                "{}={}".format(quote, count) for quote, count in per_quote.most_common(8))))
            lines.append("| Niet actief (status != 1) | {} |".format(inactive))
            lines.append("| Volume is nul | {} |".format(zero_volume))
            lines.append("| Prijs-tickgrootte ontbreekt | {} |".format(len(no_price_tick)))
            lines.append("| Aantal-tickgrootte ontbreekt | {} |".format(len(no_quantity_tick)))
            lines.append("| Instrumentnaam ontbreekt | {} |".format(len(no_exchange_name)))
            lines.append("")

            if no_price_tick or no_quantity_tick:
                verdict = worst(verdict, BAD)
                lines.append("Een tickgrootte van nul maakt elke prijs- en aantalberekening fout. "
                             "Voorbeelden: {}".format(
                                 ", ".join((no_price_tick + no_quantity_tick)[:10])))
            if no_exchange_name:
                verdict = worst(verdict, ATTENTION)
                lines.append("Zonder instrumentnaam kan de candle-voorraad het symbool niet "
                             "sleutelen. Voorbeelden: {}".format(", ".join(no_exchange_name[:10])))
            if percentage(zero_volume, total) > 50:
                verdict = worst(verdict, ATTENTION)
                lines.append("Meer dan de helft van de symbolen meldt geen volume - controleer of "
                             "het volumeveld voor deze exchange gevuld wordt.")

            facts[name] = {
                "symbols": total,
                "perQuote": dict(per_quote),
                "inactive": inactive,
                "zeroVolume": zero_volume,
                "missingPriceTick": len(no_price_tick),
                "missingQuantityTick": len(no_quantity_tick),
                "missingInstrumentName": len(no_exchange_name),
            }
            lines.append("")

        report.add("symbols", "Symbolen", verdict, lines, facts)
        return facts
    finally:
        connection.close()


# ==============================================================================================
# 3. Candles (candle-voorraad per exchange)
# ==============================================================================================
def interval_names(main_db):
    if main_db is None:
        return {}
    connection = open_readonly(main_db)
    try:
        return {row["Id"]: (row["Name"], row["Duration"])
                for row in connection.execute("SELECT Id, Name, Duration FROM Interval")}
    finally:
        connection.close()


def subscribed_per_exchange(entries):
    """
    Hoeveel symbolen de run daadwerkelijk heeft geabonneerd, uit regels als

        Bybit Spot started kline subscriptions for 126 symbols over 2 bundles

    Dat is het getal waartegen de candle-dekking gemeten moet worden. De instrumentenlijst is met
    opzet veel langer: het minimale volume per quote-munt houdt de meeste instrumenten er buiten,
    dus candles tegen de volledige lijst afzetten zou een gezonde run als 92% ongedekt melden.
    """
    pattern = re.compile(r"^(?P<exchange>.+?) (?:retry - )?started \S+ subscriptions "
                         r"for (?P<count>\d+) symbols")
    result = {}
    for _, _, _, message in entries:
        match = pattern.match(message.strip())
        if match:
            name = match.group("exchange").strip()
            result[name] = max(result.get(name, 0), int(match.group("count")))
    return result


def read_symbol_maps(connection):
    """
    Levert (instrument, lokale naam, barometer-ids) uit de Symbol-tabel van een candle-voorraad.

    Versie 3 sleutelt op het INSTRUMENT van de exchange; oudere bestanden hebben alleen een naam.
    Het instrument is waarop de hoofddatabase gekoppeld kan worden, dus dat heeft de voorkeur; de
    leesbare naam blijft voor de tabellen.
    """
    if column_exists(connection, "Symbol", "ExchangeName"):
        symbol_rows = list(connection.execute("SELECT SymbolId, Name, ExchangeName FROM Symbol"))
        instrument = {row["SymbolId"]: (row["ExchangeName"] or row["Name"]) for row in symbol_rows}
        local_symbols = {row["SymbolId"]: (row["Name"] or row["ExchangeName"])
                         for row in symbol_rows}
    else:
        symbol_rows = list(connection.execute("SELECT SymbolId, Name FROM Symbol"))
        instrument = {row["SymbolId"]: row["Name"] for row in symbol_rows}
        local_symbols = dict(instrument)

    barometer_ids = {identifier for identifier, name in local_symbols.items() if is_barometer(name)}
    return instrument, local_symbols, barometer_ids


def check_candles(report, candle_databases, main_db, window_start, window_end, top_count,
                  subscribed=None, deep=False, still_running=False):
    """window_start/window_end staan hier in UTC; de aanroeper rekent het lokale logvenster om."""
    if not candle_databases:
        report.add("candles", "Candles", UNKNOWN, ["Geen candle-voorraad in deze map gevonden."])
        return {}

    names = interval_names(main_db)
    lines = []
    verdict = GOOD
    facts = {}
    key_points = []

    for exchange_name, path in candle_databases:
        lines.append("### {}".format(exchange_name))
        lines.append("")
        exchange_facts = {}
        try:
            connection = open_readonly(path)
        except Exception as error:
            lines.append("Kon `{}` niet openen: {}".format(path.name, error))
            verdict = worst(verdict, BAD)
            continue

        try:
            meta = {row["Key"]: row["Value"] for row in connection.execute("SELECT * FROM Meta")}
            stored_name = meta.get("ExchangeName", "")
            if stored_name and stored_name.lower() != exchange_name.lower():
                lines.append("Het bestand heet naar `{}` maar de Meta zegt `{}` - het bestand is "
                             "van een andere exchange gekopieerd.".format(exchange_name, stored_name))
                verdict = worst(verdict, BAD)
            lines.append("Schemaversie {}, exchange `{}`.".format(
                meta.get("SchemaVersion", "?"), stored_name or "?"))
            lines.append("")

            # De symboolkaarten moeten hier al klaar staan: de barometer schrijft elke minuut een
            # rij in bijna elk interval, dus zonder hem uit te sluiten meldt de tabel hieronder de
            # barometertijd als "nieuwste candle" van bijvoorbeeld het uurinterval. De barometer
            # wordt niet overgeslagen maar krijgt verderop een eigen sectie met eigen regels.
            instrument, local_symbols, barometer_ids = read_symbol_maps(connection)
            skip = " AND SymbolId NOT IN ({})".format(
                ",".join(str(identifier) for identifier in barometer_ids)) if barometer_ids else ""

            # ---- dekking per interval --------------------------------------------------------
            per_interval = list(connection.execute(
                "SELECT IntervalId, COUNT(DISTINCT SymbolId) AS symbols, COUNT(*) AS candles, "
                "MIN(OpenTime) AS first, MAX(OpenTime) AS last FROM Candle "
                "WHERE 1=1" + skip + " GROUP BY IntervalId ORDER BY IntervalId"))
            if not per_interval:
                lines.append("De candle-voorraad bevat helemaal geen candles.")
                verdict = worst(verdict, BAD)
                continue

            lines.append("| Interval | Symbolen | Candles | Oudste | Nieuwste | Achterstand |")
            lines.append("|---|---|---|---|---|---|")
            for row in per_interval:
                name, duration = names.get(row["IntervalId"], (str(row["IntervalId"]), None))
                newest = candle_time_to_datetime(row["last"])
                late = ""
                if window_end:
                    # Een interval afstand is normaal: de candle die nog openstaat is nog niet
                    # weggeschreven. Alles daarboven is echte achterstand, dus de duur gaat eraf in
                    # plaats van een weekcandle als "12 dagen achter" te melden.
                    minutes = (window_end - newest).total_seconds() / 60.0 - (duration or 1)
                    late = "{:.0f} min".format(max(0.0, minutes))
                    if duration == 1:
                        bad_limit = LAG_RUNNING_BAD_MINUTES if still_running else LAG_BAD_MINUTES
                        attention_limit = (LAG_RUNNING_ATTENTION_MINUTES if still_running
                                           else LAG_ATTENTION_MINUTES)
                        if minutes > bad_limit:
                            verdict = worst(verdict, BAD)
                        elif minutes > attention_limit:
                            verdict = worst(verdict, ATTENTION)
                        exchange_facts["lagMinutes"] = round(max(0.0, minutes), 1)
                lines.append("| {} | {} | {:,} | {:%Y-%m-%d %H:%M} | {:%Y-%m-%d %H:%M} | {} |".format(
                    name, row["symbols"], row["candles"],
                    candle_time_to_datetime(row["first"]), newest, late))
                exchange_facts.setdefault("intervals", {})[name] = {
                    "symbols": row["symbols"], "candles": row["candles"],
                    "oldest": str(candle_time_to_datetime(row["first"])),
                    "newest": str(newest),
                }
            lines.append("")
            lines.append("Alle candletijden staan in UTC. \"Achterstand\" is de afstand tot het "
                         "einde van het venster min een interval, dus nul betekent bij.")
            lines.append("")
            lines.append("De barometer staat bewust niet in deze tabel: die schrijft elke minuut "
                         "een rij in bijna elk interval en zou hier elke nieuwste candle "
                         "overschaduwen. Hij heeft een eigen sectie met eigen controles.")
            lines.append("")
            if still_running:
                lines.append("De scanner draaide nog toen dit rapport gemaakt werd. De opslagdraad "
                             "schrijft de candles eens per uur weg, dus vlak voor zo'n ronde is de "
                             "jongste candle op schijf bijna een uur oud zonder dat er iets aan de "
                             "hand is. Daarom wordt de achterstand hier pas vanaf {:.0f} minuten "
                             "gemeld en vanaf {:.0f} minuten als fout gelezen, in plaats van "
                             "{:.0f} en {:.0f} na een nette stop.".format(
                                 LAG_RUNNING_ATTENTION_MINUTES, LAG_RUNNING_BAD_MINUTES,
                                 LAG_ATTENTION_MINUTES, LAG_BAD_MINUTES))
            else:
                lines.append("De scanner was gestopt toen dit rapport gemaakt werd, dus de "
                             "opslagdraad had alles weggeschreven en achterstand telt hier volledig "
                             "mee (melden vanaf {:.0f} minuten, fout vanaf {:.0f}).".format(
                                 LAG_ATTENTION_MINUTES, LAG_BAD_MINUTES))
            lines.append("")

            # ---- gaten in de minuutreeks -----------------------------------------------------
            one_minute_id = next((identifier for identifier, (name, duration) in names.items()
                                  if duration == 1), 1)
            if window_start and window_end:
                low = datetime_to_candle_time(window_start)
                high = datetime_to_candle_time(window_end)
                scope = "het venster van de run"
            else:
                low, high = 0, 10 ** 9
                scope = "het volledige bewaarde bereik"

            gap_rows = list(connection.execute(
                "SELECT SymbolId, MIN(OpenTime) AS first, MAX(OpenTime) AS last, COUNT(*) AS have "
                "FROM Candle WHERE IntervalId = ? AND OpenTime BETWEEN ? AND ? "
                "GROUP BY SymbolId", (one_minute_id, low, high)))

            gaps = []
            total_missing = 0
            total_expected = 0
            for row in gap_rows:
                if row["SymbolId"] in barometer_ids:
                    continue
                expected = row["last"] - row["first"] + 1
                missing = expected - row["have"]
                total_missing += max(0, missing)
                total_expected += expected
                if missing > 0:
                    gaps.append((missing, percentage(missing, expected),
                                 local_symbols.get(row["SymbolId"], str(row["SymbolId"]))))

            share = percentage(total_missing, total_expected)
            lines.append("**Gaten in de minuutreeks over {}**".format(scope))
            lines.append("")
            lines.append("Symbolen met candles: {}. Ontbrekende minuten: {:,} van {:,} "
                         "({:.2f}%).".format(len(gap_rows), total_missing, total_expected, share))
            if share > GAP_BAD_PERCENTAGE:
                verdict = worst(verdict, BAD)
            elif share > GAP_ATTENTION_PERCENTAGE:
                verdict = worst(verdict, ATTENTION)
            key_points.append("{:,} ontbrekende minuten van {:,} ({:.2f}%)".format(
                total_missing, total_expected, share))
            if gaps:
                gaps.sort(reverse=True)
                lines.append("")
                lines.append("| Symbool | Ontbrekende minuten | Aandeel |")
                lines.append("|---|---|---|")
                for missing, missing_share, name in gaps[:top_count]:
                    lines.append("| {} | {:,} | {:.2f}% |".format(name, missing, missing_share))
                if len(gaps) > top_count:
                    lines.append("")
                    lines.append("({} symbolen hebben nog meer gaten; alleen de ergste {} staan "
                                 "hier)".format(len(gaps) - top_count, top_count))
            lines.append("")
            exchange_facts["missingMinutes"] = total_missing
            exchange_facts["missingSharePercentage"] = round(share, 3)
            exchange_facts["symbolsWithGaps"] = len(gaps)

            # ---- de langste stilte per symbool -------------------------------------------------
            # Dit meet waar SubscriptionManager.MaximumTickerSilence op gezet moet worden. Die stond
            # op vier minuten en staat sinds 18-08-2026 op vijf, en dat was een gok: geen enkele
            # exchange documenteert hoe lang een munt zonder handel kan zitten, want dat is een
            # eigenschap van de markt en niet van de API.
            #
            # Gemeten op VOLUME NUL, niet op ontbrekende candles. Dat onderscheid is het hele punt:
            # bij KlineDelivery.TimerFlush schrijft de scanner elke minuut een candle weg, ook als er
            # niets verhandeld is (hij herhaalt dan de vorige slotkoers met volume nul). De
            # minuutreeks heeft dus geen gaten en zegt niets over stilte - op de nacht van
            # 17/18-08-2026 leverde geen enkel symbool op vier van de vijf beproefde exchanges ook
            # maar één ontbrekende minuut op. Een aaneengesloten reeks candles met volume nul is wel
            # precies wat de websocket als "geen data" zou hebben gezien.
            #
            # De barometer blijft er buiten: die schrijft elke minuut en zijn volumeveld is het aantal
            # munten, geen handelsvolume.
            silences = []
            for row in gap_rows:
                if row["SymbolId"] in barometer_ids or row["have"] < 2:
                    continue
                longest = 0
                current = 0
                for (volume,) in connection.execute(
                        "SELECT Volume FROM Candle WHERE SymbolId = ? AND IntervalId = ? "
                        "AND OpenTime BETWEEN ? AND ? ORDER BY OpenTime",
                        (row["SymbolId"], one_minute_id, low, high)):
                    if not volume:
                        current += 1
                        if current > longest:
                            longest = current
                    else:
                        current = 0
                if longest > 0:
                    silences.append((longest,
                                     local_symbols.get(row["SymbolId"], str(row["SymbolId"]))))

            lines.append("**Langste stilte per symbool**")
            lines.append("")
            lines.append("Het langste aaneengesloten aantal minuten zonder handel (candles met volume "
                         "nul). Dit is de grootheid waar de stiltecontrole van de scanner tegenaan "
                         "loopt, niet het aantal ontbrekende candles - die worden door de flush-timer "
                         "opgevuld.")
            lines.append("")
            if not silences:
                lines.append("Geen enkel symbool had een minuut zonder handel. Dat komt voor bij een "
                             "exchange die alleen liquide munten in de selectie heeft, en dan zegt "
                             "deze nacht niets over waar de grens moet liggen.")
                exchange_facts["longestSilenceMinutes"] = 0
                exchange_facts["symbolsAboveSilenceLimit"] = 0
            else:
                silences.sort(reverse=True)
                longest_overall = silences[0][0]
                above = [entry for entry in silences if entry[0] >= SILENCE_LIMIT_MINUTES]
                lines.append("| Symbool | Langste stilte (minuten) |")
                lines.append("|---|---|")
                for minutes, name in silences[:top_count]:
                    lines.append("| {} | {:,} |".format(name, minutes))
                if len(silences) > top_count:
                    lines.append("")
                    lines.append("({} symbolen hadden een kortere stilte)".format(
                        len(silences) - top_count))
                lines.append("")
                lines.append("Langste stilte van deze exchange: {:,} minuten. Symbolen die {} minuten "
                             "of langer stil waren: {} van de {} met candles.".format(
                                 longest_overall, SILENCE_LIMIT_MINUTES, len(above), len(gap_rows)))
                lines.append("")
                lines.append("De grens in de scanner staat op {} minuten "
                             "(SubscriptionManager.MaximumTickerSilence)."
                             .format(SILENCE_LIMIT_MINUTES))
                lines.append("")
                lines.append("**Lees dit getal niet als het aantal herstarts.** De stiltecontrole kijkt "
                             "naar het ABONNEMENT en niet naar het symbool, en een abonnement bedient "
                             "er tot SymbolLimitPerSubscription. Zolang één munt van de groep handelt "
                             "is het abonnement actief. Op de nacht van 17/18-08-2026 waren op Coinbase "
                             "Spot 87 van de 115 symbolen langer dan vijf minuten stil en waren er toch "
                             "nul herstarts, want daar zitten 25 symbolen op een abonnement. Deze "
                             "kolom is dus een bovengrens: hij valt alleen samen met het echte risico "
                             "bij een exchange die op één symbool per abonnement staat (HyperLiquid). "
                             "Vergelijk hem daarom met SymbolLimitPerSubscription van deze exchange en "
                             "met de herstartrondes onder Streams.")
                exchange_facts["longestSilenceMinutes"] = longest_overall
                exchange_facts["symbolsAboveSilenceLimit"] = len(above)
            lines.append("")

            # ---- plausibiliteit ---------------------------------------------------------------
            # Standaard beperkt tot het venster. De Candle-tabel heeft (SymbolId, IntervalId,
            # OpenTime) als geclusterde sleutel, dus een scan over de hele historie kost seconden
            # per exchange en beantwoordt een vraag die de nachtcontrole niet stelt: of DEZE run
            # kapotte candles wegschreef. Met --deep stel je wel de historische vraag.
            scope_clause = "" if deep else " AND OpenTime BETWEEN {} AND {}".format(low, high)
            broken_where = ("WHERE (Open <= 0 OR High < Low OR High < Open OR High < Close "
                            "OR Low > Open OR Low > Close)" + skip + scope_clause)
            impossible = connection.execute(
                "SELECT COUNT(*) FROM Candle " + broken_where).fetchone()[0]
            broken_examples = list(connection.execute(
                "SELECT SymbolId, IntervalId, OpenTime, Open, High, Low, Close FROM Candle "
                + broken_where + " LIMIT 5"))
            zero_volume = connection.execute(
                "SELECT COUNT(*) FROM Candle WHERE IntervalId = ?" + skip + scope_clause +
                " AND Volume = 0", (one_minute_id,)).fetchone()[0]
            one_minute_total = connection.execute(
                "SELECT COUNT(*) FROM Candle WHERE IntervalId = ?" + skip + scope_clause,
                (one_minute_id,)).fetchone()[0]

            lines.append("| Plausibiliteit ({}) | Waarde |".format(
                "hele historie" if deep else "binnen het venster"))
            lines.append("|---|---|")
            lines.append("| Onmogelijke candles (hoog/laag/open niet in volgorde, prijs <= 0) "
                         "| {:,} |".format(impossible))
            lines.append("| Minuutcandles zonder volume | {:,} ({:.1f}%) |".format(
                zero_volume, percentage(zero_volume, one_minute_total)))
            lines.append("")
            if not deep:
                lines.append("Alleen het venster is nagerekend; dat is de vraag van een "
                             "nachtcontrole. Gebruik `--deep` om de hele historie te toetsen - dat "
                             "kost seconden per exchange, dus doe het als een exchange voor het "
                             "eerst onderzocht wordt en niet elke ochtend.")
                lines.append("")
            if impossible:
                verdict = worst(verdict, BAD)
                lines.append("Onmogelijke candles betekenen dat de omzetting van het antwoord van "
                             "de exchange niet klopt; elke indicator die erop rust is dan ook fout.")
                lines.append("")
                if deep:
                    lines.append("Let op de datum van de voorbeelden hieronder. Ligt die buiten het "
                                 "venster, dan komt de candle niet uit deze run, en herstelt hij "
                                 "zichzelf ook niet: het ophalen hervat bij de laatste "
                                 "synchronisatie en komt er nooit meer langs. Verwijderen of de "
                                 "synchronisatie voor dat symbool en interval terugzetten is dan de "
                                 "enige weg.")
                    lines.append("")
                for row in broken_examples:
                    lines.append("- `{}` {} {:%Y-%m-%d %H:%M} open={} hoog={} laag={} sluit={}".format(
                        local_symbols.get(row["SymbolId"], row["SymbolId"]),
                        names.get(row["IntervalId"], (row["IntervalId"], None))[0],
                        candle_time_to_datetime(row["OpenTime"]),
                        row["Open"], row["High"], row["Low"], row["Close"]))
                lines.append("")
                key_points.append("{} onmogelijke candles".format(impossible))
            exchange_facts["impossibleCandles"] = impossible
            exchange_facts["impossibleScope"] = "history" if deep else "window"
            exchange_facts["zeroVolumeCandles"] = zero_volume

            # ---- symbolen zonder candles ------------------------------------------------------
            if main_db is not None:
                main_connection = open_readonly(main_db)
                try:
                    exchange_id = main_connection.execute(
                        "SELECT Id FROM Exchange WHERE Name = ?", (exchange_name,)).fetchone()
                    if exchange_id:
                        # Beide kanten zijn op het instrument van de exchange gesleuteld; op een
                        # van beide terugvallen op de scannernaam zou appels met peren vergelijken
                        # en elk symbool als ongedekt melden.
                        listed = {(row["ExchangeName"] or row["Name"]) for row in
                                  main_connection.execute(
                                      "SELECT Name, ExchangeName FROM Symbol WHERE ExchangeId = ?",
                                      (exchange_id[0],))}
                        # De lokale Symbol-tabel IS de lijst van symbolen die de voorraad ooit heeft
                        # opgenomen, dus die beantwoordt dit zonder een scan over miljoenen rijen.
                        with_candles = set(instrument.values())
                        without = sorted(name for name in listed if name not in with_candles)
                        orphan = sorted(name for name in with_candles
                                        if name and not is_barometer(name) and name not in listed)
                        # De instrumentenlijst is context, geen oordeel: het minimale volume per
                        # quote-munt hoort de meeste instrumenten er juist buiten te houden.
                        subscribed_count = (subscribed or {}).get(exchange_name)
                        received = len([row for row in gap_rows
                                        if row["SymbolId"] not in barometer_ids])

                        lines.append("| Dekking instrumenten | Waarde |")
                        lines.append("|---|---|")
                        lines.append("| Symbolen in de instrumentenlijst | {} |".format(len(listed)))
                        lines.append("| Daarvan met candles in de voorraad | {} |".format(
                            len(listed) - len(without)))
                        lines.append("| Geabonneerd tijdens deze run (uit het log) | {} |".format(
                            subscribed_count if subscribed_count is not None else "onbekend"))
                        lines.append("| Candles geleverd binnen het venster | {} |".format(received))
                        lines.append("| Candles voor een symbool dat niet meer genoteerd is | {} |"
                                     .format(len(orphan)))
                        lines.append("")

                        # Een venster van een paar minuten kan geen dekking bewijzen: op de meeste
                        # exchanges komt de eerste candle pas als de minuut sluit, en een symbool
                        # dat gewoon stil is heeft nog niets te sturen.
                        short_window = (window_start and window_end
                                        and (window_end - window_start) < timedelta(minutes=15))
                        if subscribed_count and short_window:
                            lines.append("Het venster is korter dan een kwartier, dus de dekking "
                                         "wordt niet beoordeeld: {} van de {} geabonneerde "
                                         "symbolen leverden iets.".format(received, subscribed_count))
                            lines.append("")
                            exchange_facts["subscribed"] = subscribed_count
                        elif subscribed_count:
                            silent = subscribed_count - received
                            silent_share = percentage(max(0, silent), subscribed_count)
                            if silent > 0:
                                lines.append("{} van de {} geabonneerde symbolen ({:.1f}%) leverden "
                                             "geen enkele candle tijdens het venster.".format(
                                                 silent, subscribed_count, silent_share))
                                lines.append("")
                            if silent_share > NO_CANDLE_BAD_PERCENTAGE:
                                verdict = worst(verdict, BAD)
                            elif silent_share > NO_CANDLE_ATTENTION_PERCENTAGE:
                                verdict = worst(verdict, ATTENTION)
                            exchange_facts["subscribed"] = subscribed_count
                            exchange_facts["silentSymbols"] = max(0, silent)
                        else:
                            lines.append("Geen abonnementsregel in het log, dus de dekking kan niet "
                                         "beoordeeld worden: de instrumentenlijst alleen zegt niet "
                                         "welke symbolen candles hadden moeten leveren.")
                            lines.append("")
                            verdict = worst(verdict, UNKNOWN)

                        if orphan:
                            lines.append("Niet meer genoteerd: {}{}".format(
                                ", ".join(orphan[:top_count]),
                                " ..." if len(orphan) > top_count else ""))
                            lines.append("")
                        exchange_facts["symbolsWithoutCandles"] = len(without)
                        exchange_facts["orphanSymbols"] = len(orphan)
                finally:
                    main_connection.close()

            facts[exchange_name] = exchange_facts
        finally:
            connection.close()

    report.add("candles", "Candles", verdict, lines, facts, key_points)
    return facts


def column_exists(connection, table, column):
    return any(row[1] == column for row in connection.execute(
        "PRAGMA table_info([{}])".format(table)))


# ==============================================================================================
# 3b. Barometer
# ==============================================================================================
def sustained_low(values, minimum_length):
    """
    Het laagste niveau dat minimum_length metingen achter elkaar wordt vastgehouden.

    Het gewone minimum pakt ook een dip van een enkele meting, en die zegt niets: de barometer
    telt tijdens een verversing van de symbolenlijst een paar minuten minder munten mee. Door per
    venster van minimum_length metingen de HOOGSTE waarde te nemen en daarvan de laagste te
    houden, overleeft alleen een dip die het hele venster vult. Geeft None bij een lege reeks.
    """
    if not values:
        return None
    if len(values) <= minimum_length:
        return max(values)
    return min(max(values[index:index + minimum_length])
               for index in range(len(values) - minimum_length + 1))


def judge_barometer_coins(connection, barometer_ids, local_symbols, settled, high, lines,
                          key_points):
    """
    Beoordeelt het muntenaantal per barometer, niet over alle barometers van een exchange samen.

    Een exchange heeft er een per quote-munt, en die zijn niet vergelijkbaar: BitMart heeft naast
    een $BMPUSDT van 88 tot 91 munten een $BMPUSDC die altijd op een munt rust. Een MIN/MAX over
    beide tegelijk vergelijkt het minimum van de ene met het maximum van de andere en meldt
    "magerste meting op 1 van de 91 munten" terwijl er niets aan de hand is.

    Beoordeeld wordt de terugval na het hoogtepunt (zie BAROMETER_MINIMAL_COINS). Het minimum over
    het hele venster wordt wel getoond, want dat is de opbouwcurve en die is het lezen waard.

    Piek en terugval worden PER INTERVAL bepaald en pas daarna samengenomen. De barometer schrijft
    in tien intervallen en die beginnen niet tegelijk: een interval dat pas halverwege de nacht
    genoeg historie heeft, start dan met zijn eigen opbouwcurve. Over alle intervallen tegelijk
    gemeten valt die curve na het hoogtepunt van de reeks als geheel, en dan leest een opstart als
    een instorting - Kraken Spot werd op 18-08-2026 zo als "viel terug naar 9% van 43 munten"
    gemeld, terwijl de lage metingen de eerste zes minuten van de intervallen 13, 14 en 15 waren
    (die om 09:08, 13:08 en 01:08 begonnen) en de reeks verder de hele nacht op 42 stond.
    """
    per_symbol = {}
    lines.append("**Aantal munten per barometer**")
    lines.append("")
    lines.append("| Barometer | Laagste | Hoogste | Laagste na de piek | Oordeel |")
    lines.append("|---|---|---|---|---|")
    for identifier in sorted(barometer_ids, key=lambda i: local_symbols[i]):
        name = local_symbols[identifier]
        row = connection.execute(
            "SELECT MIN(Volume) AS low, MAX(Volume) AS high FROM Candle WHERE SymbolId = ? "
            "AND OpenTime BETWEEN ? AND ?", (identifier, settled, high)).fetchone()
        if not row or row["low"] is None:
            continue
        lowest, highest = row["low"], row["high"]

        # Het eerste moment waarop het hoogtepunt gehaald wordt; alles daarna moet dat niveau
        # vasthouden. Wat ervoor ligt is opbouw en zegt niets over de gezondheid van de nacht.
        # Per interval, want elk interval heeft zijn eigen begin en dus zijn eigen opbouw (zie de
        # toelichting boven deze functie). Het laagste dat een interval na ZIJN piek nog haalt is
        # wat telt; het slechtste interval bepaalt het oordeel.
        # Elk interval wordt tegen ZIJN EIGEN piek afgezet, niet tegen de hoogste van de reeks: een
        # interval dat om zijn eigen redenen op een lager aantal draait is geen terugval. Het
        # interval dat het minste van zijn eigen piek vasthoudt bepaalt het oordeel.
        after_peak = None
        peak_of_worst = None
        share = None
        for (interval_id,) in connection.execute(
                "SELECT DISTINCT IntervalId FROM Candle WHERE SymbolId = ? "
                "AND OpenTime BETWEEN ? AND ?", (identifier, settled, high)).fetchall():
            series = [row[0] for row in connection.execute(
                "SELECT Volume FROM Candle WHERE SymbolId = ? AND IntervalId = ? "
                "AND OpenTime BETWEEN ? AND ? ORDER BY OpenTime",
                (identifier, interval_id, settled, high)).fetchall()]
            if not series:
                continue
            interval_high = max(series)
            if not interval_high:
                continue
            interval_after_peak = sustained_low(series[series.index(interval_high):],
                                                BAROMETER_DIP_MINUTES)
            if interval_after_peak is None:
                continue
            interval_share = percentage(interval_after_peak, interval_high)
            if share is None or interval_share < share:
                share = interval_share
                after_peak = interval_after_peak
                peak_of_worst = interval_high
        if after_peak is None:
            after_peak, peak_of_worst, share = lowest, highest, percentage(lowest, highest)

        if highest < BAROMETER_MINIMAL_COINS:
            share = None
            judgement = "te weinig munten om te beoordelen"
        else:
            if share < BAROMETER_COINS_BAD_SHARE:
                judgement = "**viel terug naar {:.0f}%**".format(share)
                key_points.append("{} viel na de piek terug naar {:.0f}% van {:.0f} munten".format(
                    name, share, peak_of_worst))
            elif share < BAROMETER_COINS_ATTENTION_SHARE:
                judgement = "zakte naar {:.0f}%".format(share)
                key_points.append("{} zakte na de piek naar {:.0f}% van {:.0f} munten".format(
                    name, share, peak_of_worst))
            elif lowest < highest:
                judgement = "bouwde op van {:.0f}, hield {:.0f}% vast".format(lowest, share)
            else:
                judgement = "vlak"
        lines.append("| {} | {:.0f} | {:.0f} | {:.0f} | {} |".format(
            name, lowest, highest, after_peak, judgement))
        per_symbol[name] = {
            "minimum": lowest,
            "maximum": highest,
            "minimumAfterPeak": after_peak,
            "declineSharePercentage": round(share, 1) if share is not None else None,
        }
    lines.append("")
    lines.append("Het oordeel rust op de kolom \"laagste na de piek\": een aantal dat alleen maar "
                 "groeit is een run die zijn abonnementen en historie nog aan het inhalen was, en "
                 "dat duurt bij een strenge snelheidsbegrenzing uren in plaats van de {:.0f} "
                 "minuten opwarming die hierboven al afgetrokken zijn. Pas een terugval NA het "
                 "hoogtepunt betekent dat er munten wegvielen. Barometers met minder dan {} munten "
                 "krijgen geen oordeel; die beschrijven geen markt."
                 .format(BAROMETER_WARMUP_MINUTES, BAROMETER_MINIMAL_COINS))
    lines.append("")
    lines.append("Piek en terugval worden per interval bepaald en tegen de eigen piek van dat "
                 "interval afgezet, en een terugval telt pas als hij {} metingen achter elkaar "
                 "aanhoudt. Zonder het eerste leest een interval dat later op de avond begint zijn "
                 "eigen opbouw als een instorting; zonder het tweede telt de dip van een paar "
                 "minuten bij elke verversing van de symbolenlijst mee. Het interval dat het minste "
                 "van zijn eigen piek vasthoudt bepaalt de regel hierboven."
                 .format(BAROMETER_DIP_MINUTES))
    lines.append("")
    return per_symbol


def check_barometer(report, candle_databases, main_db, window_start, window_end, subscribed=None,
                    still_running=False):
    """
    De barometer staat als candles in dezelfde tabel, maar het zijn geen koersen: de vier
    prijsvelden dragen de losse cijfers van een enkele meting (zie BarometerCandleFields.Store).
    Een controle op hoog/laag-volgorde is hier dus zinloos, terwijl er andere regels zijn die wel
    hard gelden. Vandaar een eigen sectie in plaats van meerekenen of overslaan - de barometer is
    een vast onderdeel van de scanner en verdient een echte controle.

    Alles hier filtert op SymbolId, de eerste kolom van de geclusterde sleutel, dus ook een telling
    over de hele barometerhistorie is een bereikscan over een paar duizend rijen. Daarom hoeft deze
    sectie niet achter --deep, in tegenstelling tot de plausibiliteit van de gewone candles.
    """
    if not candle_databases:
        report.add("barometer", "Barometer", UNKNOWN, ["Geen candle-voorraad in deze map gevonden."])
        return {}

    names = interval_names(main_db)
    lines = []
    verdict = GOOD
    facts = {}
    key_points = []

    for exchange_name, path in candle_databases:
        lines.append("### {}".format(exchange_name))
        lines.append("")
        exchange_facts = {}
        try:
            connection = open_readonly(path)
        except Exception as error:
            lines.append("Kon `{}` niet openen: {}".format(path.name, error))
            verdict = worst(verdict, BAD)
            continue

        try:
            _, local_symbols, barometer_ids = read_symbol_maps(connection)
            if not barometer_ids:
                lines.append("**Er staat geen enkele barometerrij in deze voorraad.** De barometer "
                             "is een vast onderdeel van de scanner, dus dit betekent dat hij niet "
                             "gedraaid heeft of dat zijn pseudosymbool ontbreekt.")
                lines.append("")
                verdict = worst(verdict, BAD)
                key_points.append("geen barometerrijen aanwezig")
                facts[exchange_name] = {"present": False}
                continue

            names_found = sorted(local_symbols[identifier] for identifier in barometer_ids)
            identifiers = ",".join(str(identifier) for identifier in barometer_ids)
            lines.append("Pseudosymbolen: {}.".format(", ".join(names_found)))
            lines.append("")
            lines.append("| Veld | Betekenis |")
            lines.append("|---|---|")
            for field, meaning in BAROMETER_FIELDS:
                lines.append("| {} | {} |".format(field, meaning))
            lines.append("")
            lines.append("Hoog en laag zijn hier dus **niet** de hoogste en laagste waarde. Daarom "
                         "gelden voor deze reeks andere controles dan voor gewone candles: een "
                         "negatieve waarde is normaal, maar spreiding onder nul of een percentage "
                         "stijgers boven honderd kan niet.")
            lines.append("")

            # De prijsvelden staan als tick opgeslagen; met de tickgrootte erbij worden het weer
            # percentages, en pas dan zijn de grenzen hieronder te begrijpen.
            tick = 0.01
            if main_db is not None:
                main_connection = open_readonly(main_db)
                try:
                    # The barometer symbol can be present more than once (one row per exchange that
                    # ever wrote into this database), and older rows carry a tick size of zero.
                    # Take the first usable one and otherwise keep the default above - a zero here
                    # ends up as a divisor further down.
                    for row in main_connection.execute(
                            "SELECT PriceTickSize FROM Symbol WHERE Name = ?", (names_found[0],)):
                        if as_number(row["PriceTickSize"]) > 0:
                            tick = as_number(row["PriceTickSize"])
                            break
                finally:
                    main_connection.close()

            if window_start and window_end:
                low = datetime_to_candle_time(window_start)
                high = datetime_to_candle_time(window_end)
            else:
                low, high = 0, 10 ** 9

            # ---- continuiteit ----------------------------------------------------------------
            per_interval = list(connection.execute(
                "SELECT IntervalId, COUNT(*) AS measurements, MIN(OpenTime) AS first, "
                "MAX(OpenTime) AS last FROM Candle WHERE SymbolId IN ({}) "
                "AND OpenTime BETWEEN ? AND ? GROUP BY IntervalId "
                "ORDER BY IntervalId".format(identifiers), (low, high)))
            if not per_interval:
                # Een datamap kan candle-bestanden van meerdere exchanges bevatten, waarvan er maar
                # een meedeed. Draaide deze exchange niet - geen abonnementsregel in het log - dan
                # is een lege barometer het verwachte beeld en geen gebrek. Alleen als hij wel
                # geabonneerd was, is het er een.
                took_part = exchange_name in (subscribed or {})
                # En zelfs dan pas oordelen als er een wegschrijfronde in het venster paste. De
                # opslagdraad gaat eens per uur, dus een run van een paar minuten heeft nog niets
                # op schijf staan - dat is geen ontbrekende barometer maar een te kort venster.
                window_minutes = ((window_end - window_start).total_seconds() / 60.0
                                  if window_start and window_end else 0.0)
                long_enough = window_minutes >= LAG_RUNNING_ATTENTION_MINUTES
                if took_part and not long_enough:
                    lines.append("Geen barometermetingen binnen het venster, maar het venster is "
                                 "met {:.0f} minuten korter dan een wegschrijfronde van de "
                                 "opslagdraad (een uur). Er kan simpelweg nog niets op schijf "
                                 "staan, dus hier valt niets over te zeggen.".format(window_minutes))
                    lines.append("")
                    verdict = worst(verdict, UNKNOWN)
                    facts[exchange_name] = {"present": True, "measurementsInWindow": 0,
                                            "tookPart": True, "windowTooShort": True}
                    continue
                if took_part:
                    lines.append("**Geen enkele barometermeting binnen het venster,** terwijl deze "
                                 "exchange wel abonnementen had. De reeks bestaat wel, maar tijdens "
                                 "deze run is er niets bij gekomen.")
                    verdict = worst(verdict, BAD)
                    key_points.append("geen metingen terwijl de exchange wel draaide")
                else:
                    lines.append("Geen barometermetingen binnen het venster, maar deze exchange had "
                                 "ook geen abonnementen in dit log - hij draaide niet mee. Het "
                                 "candle-bestand is dan historie van een eerdere run, geen gebrek "
                                 "van deze nacht.")
                lines.append("")
                facts[exchange_name] = {"present": True, "measurementsInWindow": 0,
                                        "tookPart": took_part}
                continue

            # De barometer schrijft elke minuut een rij, ook in de hogere intervallen: elk interval
            # is een eigen terugblik en niet een candle die pas op zijn sluittijd af is. Gaten in
            # die minuutreeks zijn dus de echte continuiteitsmaat.
            lines.append("**Continuiteit binnen het venster** (er hoort een meting per minuut per "
                         "interval te staan)")
            lines.append("")
            lines.append("| Interval | Metingen | Verwacht | Ontbrekend | Eerste | Laatste |")
            lines.append("|---|---|---|---|---|---|")
            total_missing = 0
            total_expected = 0
            newest_overall = None
            for row in per_interval:
                name = names.get(row["IntervalId"], (str(row["IntervalId"]), None))[0]
                expected = row["last"] - row["first"] + 1
                missing = max(0, expected - row["measurements"])
                total_missing += missing
                total_expected += expected
                newest = candle_time_to_datetime(row["last"])
                if newest_overall is None or newest > newest_overall:
                    newest_overall = newest
                lines.append("| {} | {:,} | {:,} | {} | {:%Y-%m-%d %H:%M} | {:%Y-%m-%d %H:%M} |"
                             .format(name, row["measurements"], expected, missing,
                                     candle_time_to_datetime(row["first"]), newest))
            lines.append("")

            missing_share = percentage(total_missing, total_expected)
            if missing_share > GAP_BAD_PERCENTAGE:
                verdict = worst(verdict, BAD)
            elif missing_share > GAP_ATTENTION_PERCENTAGE:
                verdict = worst(verdict, ATTENTION)
            lines.append("Ontbrekende metingen: {:,} van {:,} ({:.2f}%), verdeeld over {} "
                         "intervallen.".format(total_missing, total_expected, missing_share,
                                               len(per_interval)))
            lines.append("")
            key_points.append("{:,} ontbrekende metingen van {:,} ({:.2f}%)".format(
                total_missing, total_expected, missing_share))

            # Welke intervallen de barometer beschrijft is een keuze van de scanner, geen norm die
            # dit script kan vellen. Wat telt is dat het er morgen dezelfde zijn - vandaar in de
            # json, zodat het verschil tussen twee nachten opvalt.
            interval_list = [names.get(row["IntervalId"], (str(row["IntervalId"]), None))[0]
                             for row in per_interval]
            lines.append("Intervallen waarop geschreven wordt: {}.".format(", ".join(interval_list)))
            lines.append("")
            lines.append("Welke intervallen dat zijn wordt hier niet beoordeeld. Het staat wel in "
                         "de json, zodat een interval dat er morgen tussenuit valt opvalt bij het "
                         "vergelijken van twee nachten.")
            lines.append("")

            # ---- actualiteit ------------------------------------------------------------------
            if window_end and newest_overall:
                lag = (window_end - newest_overall).total_seconds() / 60.0
                bad_limit = (LAG_RUNNING_BAD_MINUTES if still_running
                             else BAROMETER_LAG_BAD_MINUTES)
                attention_limit = (LAG_RUNNING_ATTENTION_MINUTES if still_running
                                   else BAROMETER_LAG_ATTENTION_MINUTES)
                if lag > bad_limit:
                    verdict = worst(verdict, BAD)
                elif lag > attention_limit:
                    verdict = worst(verdict, ATTENTION)
                lines.append("Nieuwste meting: {:%Y-%m-%d %H:%M} UTC, dat is {:.0f} minuten voor "
                             "het einde van het venster.".format(newest_overall, max(0.0, lag)))
                if still_running:
                    lines.append("")
                    lines.append("De scanner draaide nog. De barometer gaat via dezelfde opslagronde "
                                 "van een uur naar schijf als de gewone candles, dus er wordt hier "
                                 "pas vanaf {:.0f} minuten gemeld.".format(attention_limit))
                lines.append("")
                exchange_facts["lagMinutes"] = round(max(0.0, lag), 1)

            # ---- oude opmaak ------------------------------------------------------------------
            # Voordat BarometerCandleFields bestond droegen alle vier de prijsvelden hetzelfde
            # getal. Zulke rijen herkent de scanner zelf en berekent hij opnieuw, maar ze zouden
            # hier wel elke inhoudelijke regel schenden. Dus eerst apart zetten, dan pas toetsen.
            # De nulrij valt hier bewust buiten. Een barometer die op een handvol munten rust
            # schrijft een geldige meting waarin alle vier de cijfers nul zijn zodra geen van die
            # munten bewoog: mediaan 0, percentage stijgers 0, spreiding 0, gemiddelde 0. Zonder
            # deze uitzondering telt zo'n rij als oude opmaak - dat leverde 297 "oude" rijen op een
            # $BMPEUR met twee munten. De scanner zelf gooit ze weg en rekent ze opnieuw
            # (BarometerCandleFields.IsLegacyLayout noemt dit "the all-zero case"), maar het is geen
            # opmaak van voor de wijziging. Een echte oude rij met vier keer nul valt hier ook
            # buiten; die zou net zo goed doorstaan wat hieronder getoetst wordt.
            legacy_test = ("Open = High AND High = Low AND Low = Close "
                           "AND NOT (Open = 0 AND High = 0 AND Low = 0 AND Close = 0)")
            legacy_window = connection.execute(
                "SELECT COUNT(*) FROM Candle WHERE SymbolId IN ({}) AND OpenTime BETWEEN ? AND ? "
                "AND {}".format(identifiers, legacy_test), (low, high)).fetchone()[0]
            legacy_all = connection.execute(
                "SELECT COUNT(*) FROM Candle WHERE SymbolId IN ({}) AND {}".format(
                    identifiers, legacy_test)).fetchone()[0]

            # ---- inhoudelijke regels ----------------------------------------------------------
            # Deze volgen rechtstreeks uit wat de velden betekenen; ze gelden alleen voor rijen in
            # de huidige opmaak, vandaar dat de oude opmaak er telkens buiten valt.
            checks = (
                ("Percentage stijgers onder 0%", "High < 0"),
                ("Percentage stijgers boven 100%", "High > {}".format(100.0 / tick)),
                ("Spreiding negatief", "Low < 0"),
                ("Gemiddelde verder dan {:.0f}% van nul".format(BAROMETER_EXTREME_PERCENTAGE),
                 "ABS(Close) > {}".format(BAROMETER_EXTREME_PERCENTAGE / tick)),
                ("Aantal munten nul of negatief", "Volume <= 0"),
            )
            lines.append("**Inhoudelijke controles** (rijen in de oude opmaak tellen niet mee)")
            lines.append("")
            lines.append("| Controle | In het venster | Hele voorraad |")
            lines.append("|---|---|---|")
            violations_window = 0
            for label, condition in checks:
                in_window = connection.execute(
                    "SELECT COUNT(*) FROM Candle WHERE SymbolId IN ({}) AND OpenTime BETWEEN ? AND ? "
                    "AND NOT ({}) AND ({})".format(identifiers, legacy_test, condition),
                    (low, high)).fetchone()[0]
                overall = connection.execute(
                    "SELECT COUNT(*) FROM Candle WHERE SymbolId IN ({}) AND NOT ({}) AND ({})".format(
                        identifiers, legacy_test, condition)).fetchone()[0]
                violations_window += in_window
                lines.append("| {} | {:,} | {:,} |".format(label, in_window, overall))
                exchange_facts.setdefault("violations", {})[label] = {
                    "window": in_window, "all": overall}
            lines.append("| Rijen in de oude opmaak (alle vier de velden gelijk) | {:,} | {:,} |"
                         .format(legacy_window, legacy_all))
            lines.append("")

            if violations_window:
                verdict = worst(verdict, BAD)
                lines.append("Een schending binnen het venster betekent dat de barometer van deze "
                             "nacht niet klopt: de grafiek en elke pauzeregel die erop rust lezen "
                             "dan een waarde die niet kan bestaan.")
                lines.append("")
                key_points.append("{} inhoudelijke schendingen".format(violations_window))
            if legacy_window:
                verdict = worst(verdict, ATTENTION)
                lines.append("Er zijn rijen in de oude opmaak binnen het venster geschreven. Dat "
                             "hoort niet meer voor te komen: de opmaak waarin alle vier de velden "
                             "hetzelfde getal dragen is vervangen door de losse cijfers.")
                lines.append("")
            elif legacy_all:
                lines.append("De rijen in de oude opmaak liggen buiten het venster; dat is historie "
                             "van voor die wijziging. De scanner herkent ze zelf "
                             "(BarometerCandleFields.IsLegacyLayout) en berekent ze opnieuw in "
                             "plaats van ze te tekenen, dus ze hoeven niet opgeruimd te worden.")
                lines.append("")

            # ---- waarden en betrouwbaarheid ---------------------------------------------------
            # Het muntenaantal wordt pas na de opwarmperiode beoordeeld; de overige cijfers gelden
            # over het hele venster.
            settled = min(low + BAROMETER_WARMUP_MINUTES, high)
            row = connection.execute(
                "SELECT MIN(Open) AS a, MAX(Open) AS b, MIN(High) AS c, MAX(High) AS d, "
                "MIN(Low) AS e, MAX(Low) AS f, MIN(Close) AS g, MAX(Close) AS h, "
                "MIN(Volume) AS i, MAX(Volume) AS j FROM Candle WHERE SymbolId IN ({}) "
                "AND OpenTime BETWEEN ? AND ? AND NOT ({})".format(identifiers, legacy_test),
                (settled, high)).fetchone()
            if row and row["a"] is not None:
                lines.append("**Waarden binnen het venster**")
                lines.append("")
                lines.append("| Cijfer | Laagste | Hoogste |")
                lines.append("|---|---|---|")
                lines.append("| Mediaan | {:.2f}% | {:.2f}% |".format(
                    row["a"] * tick, row["b"] * tick))
                lines.append("| Percentage stijgers | {:.1f}% | {:.1f}% |".format(
                    row["c"] * tick, row["d"] * tick))
                lines.append("| Spreiding | {:.2f}% | {:.2f}% |".format(
                    row["e"] * tick, row["f"] * tick))
                lines.append("| Gemiddelde | {:.2f}% | {:.2f}% |".format(
                    row["g"] * tick, row["h"] * tick))
                lines.append("")
                exchange_facts["coinsMinimum"] = row["i"]
                exchange_facts["coinsMaximum"] = row["j"]

                # Het aantal munten staat bewust niet in de tabel hierboven maar in een eigen tabel
                # per barometer: die cijfers gaan over de hele exchange en zijn optelbaar, het
                # muntenaantal is dat niet (zie judge_barometer_coins).
                #
                # Het aantal munten is de betrouwbaarheidsmaat van een meting. De noemer is bewust
                # het eigen maximum van deze nacht en NIET het aantal geabonneerde symbolen: de
                # barometer rekent alleen op munten met genoeg volume (CryptoBarometerPrice gebruikt
                # symbol.EnoughVolume), dus tegen alle abonnementen afzetten meldt een gezonde
                # exchange als 70 procent. Wat je wil weten is of het aantal onderweg wegzakte, en
                # daarvoor is de reeks zijn eigen maatstaf.
                if row["j"]:
                    exchange_facts["coinsPerSymbol"] = judge_barometer_coins(
                        connection, barometer_ids, local_symbols, settled, high, lines,
                        key_points)
                    worst_share = min(
                        (entry["declineSharePercentage"]
                         for entry in exchange_facts["coinsPerSymbol"].values()
                         if entry["declineSharePercentage"] is not None),
                        default=None)
                    if worst_share is not None:
                        exchange_facts["coinsSharePercentage"] = worst_share
                        if worst_share < BAROMETER_COINS_BAD_SHARE:
                            verdict = worst(verdict, BAD)
                        elif worst_share < BAROMETER_COINS_ATTENTION_SHARE:
                            verdict = worst(verdict, ATTENTION)
                    subscribed_count = (subscribed or {}).get(exchange_name)
                    if subscribed_count:
                        # Alleen ter informatie: het verschil met de abonnementen is de volumegrens,
                        # geen tekort.
                        lines.append("Ter vergelijking: er waren {} symbolen geabonneerd. Het "
                                     "verschil is de volumegrens - de barometer telt alleen munten "
                                     "met genoeg volume mee.".format(subscribed_count))
                        lines.append("")
                        exchange_facts["subscribed"] = subscribed_count

            exchange_facts.update({
                "present": True,
                "intervals": interval_list,
                "missingMeasurements": total_missing,
                "missingSharePercentage": round(missing_share, 3),
                "legacyRowsInWindow": legacy_window,
                "legacyRowsAll": legacy_all,
                "violationsInWindow": violations_window,
            })
            facts[exchange_name] = exchange_facts
        finally:
            connection.close()

    report.add("barometer", "Barometer", verdict, lines, facts, key_points)
    return facts


# ==============================================================================================
# 4/5. Loganalyse
# ==============================================================================================
LOG_LINE = re.compile(
    r"^(?P<time>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})[.,]?(?P<fraction>\d*)\|"
    r"(?P<level>\w+)\|(?P<logger>[^|]*)\|(?P<message>.*)$")

PATTERNS = {
    "startingSubscriptions": re.compile(r"starting (?P<type>\S+) subscriptions for \((?P<quotes>[^)]*)\)"),
    "startedSubscriptions": re.compile(r"started (?P<type>\S+) subscriptions(?P<extra>.*)"),
    "connectionLost": re.compile(r"subscription (?P<name>.+) connection lost"),
    "connectionRestored": re.compile(r"subscription (?P<name>.+) connection restored"),
    "restart": re.compile(r"herstarten (?P<count>\d+) (?P<type>\S+) subscriptions \((?P<state>\w+)\)"),
    "symbolsChanged": re.compile(r"(?P<type>\S+) symbols changed:"),
    "nowServing": re.compile(r"now serving (?P<rest>.*)"),
    "symbolCount": re.compile(r"symbols=(?P<count>\d+)"),
}

TROUBLE_WORDS = ("rate limit", "too many requests", "429", "banned", "forbidden", "unauthorized",
                 "timeout", "timed out", "refused", "invalid api", "signature")

# Getallen, symboolnamen en aangehaalde stukken verschillen per keer; die eruit halen zodat
# dezelfde storing tot een regel groepeert in plaats van vijfhonderd.
NORMALISE = [
    (re.compile(r"\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}(:\d{2})?(\.\d+)?"), "<tijd>"),
    (re.compile(r"0x[0-9a-fA-F]+"), "<hex>"),
    (re.compile(r"\b\d+[.,]\d+\b"), "<getal>"),
    (re.compile(r"\b\d+\b"), "<n>"),
]


def normalise_message(message):
    text = message.strip()
    for pattern, replacement in NORMALISE:
        text = pattern.sub(replacement, text)
    return text[:300]


def read_log_lines(paths):
    """Levert (moment, niveau, logger, melding). Vervolgregels gaan bij de vorige."""
    entries = []
    for path in paths:
        try:
            with open(path, "r", encoding="utf-8", errors="replace") as handle:
                for raw in handle:
                    match = LOG_LINE.match(raw.rstrip("\n"))
                    if match:
                        entries.append([parse_moment(match.group("time")),
                                        match.group("level"),
                                        match.group("logger"),
                                        match.group("message")])
                    elif entries and raw.strip():
                        entries[-1][3] += " " + raw.strip()
        except Exception:
            continue
    entries.sort(key=lambda entry: (entry[0] or datetime.min))
    return entries


def find_last_startup(entries):
    """
    Het moment waarop het scannerproces voor het laatst startte, of None.

    Het registreren van de plugins draait een keer per processtart (en nergens anders), wat het het
    enige merkteken maakt dat "deze run" scheidt van wat er verder in hetzelfde logbestand staat.
    Dat is nodig omdat NLog het bestand om middernacht omrolt, dus een nachtrun beslaat altijd twee
    bestanden en het oudste begint meestal midden in de vorige sessie.
    """
    moments = [entry[0] for entry in entries
               if entry[0] and "PluginManager" in entry[2] and "Registered analyzer" in entry[3]]
    if not moments:
        return None
    # Registreren duurt milliseconden, dus terug lopen over de regels uit hetzelfde blok.
    last = moments[-1]
    for moment in reversed(moments):
        if (last - moment).total_seconds() > 60:
            break
        last = moment
    return last


def check_streams(report, entries, window_hours):
    if not entries:
        report.add("streams", "Streams", UNKNOWN, ["Geen logbestand gevonden."])
        return {}

    counts = Counter()
    restart_states = Counter()
    started_lines = []
    serving_lines = []
    quotes = set()

    # Een onderbreking wordt op twee manieren opgelost, en beide tellen als hersteld:
    #   - de verbinding komt zelf terug        -> "connection restored" voor dezelfde groep
    #   - de scanner herstart de abonnementen  -> "herstarten N subscriptions (finished)"
    # Alleen op de eerste letten meldt een keurig opgeloste storing als "nooit hersteld", want bij
    # een herstart komt die regel er niet meer.
    outages = []                      # afgeronde storingen: naam, begin, einde, manier
    open_outages = {}                 # groepsnaam -> moment van wegvallen

    for moment, level, logger, message in entries:
        for key, pattern in PATTERNS.items():
            match = pattern.search(message)
            if not match:
                continue
            counts[key] += 1
            if key == "connectionLost":
                name = match.group("name").strip()
                # Twee keer wegvallen zonder herstel ertussen blijft één storing.
                open_outages.setdefault(name, moment)
            elif key == "connectionRestored":
                name = match.group("name").strip()
                start = open_outages.pop(name, None)
                outages.append({"name": name, "start": start or moment, "end": moment,
                                "how": "eigen herstel"})
            elif key == "restart":
                restart_states[match.group("state")] += 1
                if match.group("state") == "finished":
                    # Een afgeronde herstart zet alles wat nog openstaat weer neer.
                    for name, start in sorted(open_outages.items()):
                        outages.append({"name": name, "start": start, "end": moment,
                                        "how": "herstart"})
                    open_outages.clear()
            elif key == "startedSubscriptions":
                started_lines.append("{:%Y-%m-%d %H:%M} {}".format(moment, message.strip())
                                     if moment else message.strip())
            elif key == "nowServing":
                serving_lines.append(message.strip())
            elif key == "startingSubscriptions":
                quotes.add(match.group("quotes"))

    never_restored = dict(open_outages)
    drops = len(outages) + len(never_restored)
    drops_per_hour = drops / window_hours if window_hours else 0.0

    # Tel er de VERBINDINGEN bij. Een onderbreking hierboven is er een van een abonnement, en een
    # websocket draagt er meerdere: valt de verbinding weg, dan meldt elk abonnement dat apart. Op
    # HyperLiquid Futures leverde dat in de nacht van 18/19-08-2026 208 meldingen op die in werkelijkheid
    # 21 keer wegvallen waren, telkens tien abonnementen op dezelfde seconde - en elk van die 21 was na
    # één seconde weer terug. "19 per uur" leest als een storing, "2 per uur, één seconde" als wat het is.
    # Abonnementen die op dezelfde seconde wegvallen zaten op dezelfde socket; nauwkeuriger dan de seconde
    # is het log niet en fijner hoeft ook niet.
    connection_moments = {outage["start"] for outage in outages if outage["start"]}
    connection_moments.update(start for start in open_outages.values() if start)
    connection_drops = len(connection_moments)
    connection_drops_per_hour = connection_drops / window_hours if window_hours else 0.0

    def minutes(outage):
        if not outage["start"] or not outage["end"]:
            return None
        return (outage["end"] - outage["start"]).total_seconds() / 60.0

    durations = [value for value in (minutes(outage) for outage in outages) if value is not None]
    longest = max(durations) if durations else 0.0
    by_restart = sum(1 for outage in outages if outage["how"] == "herstart")

    # Het oordeel rust op de VERBINDINGEN, niet op de abonnementen: anders krijgt een exchange met
    # veel abonnementen per socket automatisch een slechter cijfer dan een met weinig, terwijl er
    # precies evenveel misging.
    verdict = GOOD
    if never_restored:
        verdict = BAD
    elif connection_drops_per_hour > DROPS_BAD_PER_HOUR:
        verdict = BAD
    elif connection_drops_per_hour > DROPS_ATTENTION_PER_HOUR or longest > OUTAGE_ATTENTION_MINUTES:
        verdict = ATTENTION

    lines = ["| Meting | Waarde |", "|---|---|",
             "| Abonnementsgroepen gestart | {} |".format(counts["startedSubscriptions"]),
             "| Verbindingen weggevallen | {} ({:.2f} per uur) |".format(
                 connection_drops, connection_drops_per_hour),
             "| Abonnementen die dat merkten | {} ({:.2f} per uur) |".format(drops, drops_per_hour),
             "| Daarvan opgelost door de verbinding zelf | {} |".format(len(outages) - by_restart),
             "| Daarvan opgelost door een herstart | {} |".format(by_restart),
             "| Niet hersteld | **{}** |".format(len(never_restored)),
             "| Langste onderbreking | {:.1f} minuten |".format(longest),
             "| Herstartrondes (afgerond) | {} |".format(restart_states.get("finished", 0)),
             "| Wijzigingen in de symbolenlijst | {} |".format(counts["symbolsChanged"]),
             ""]

    if outages or never_restored:
        lines.append("**Alle onderbrekingen**")
        lines.append("")
        lines.append("| Groep | Weg vanaf | Duur | Hersteld door |")
        lines.append("|---|---|---|---|")
        listed = sorted(outages, key=lambda outage: (outage["start"] or datetime.min))
        for outage in listed[:40]:
            duration = minutes(outage)
            lines.append("| `{}` | {} | {} | {} |".format(
                outage["name"],
                "{:%Y-%m-%d %H:%M:%S}".format(outage["start"]) if outage["start"] else "?",
                "{:.0f}s".format(duration * 60) if duration is not None and duration < 2
                else ("{:.1f} min".format(duration) if duration is not None else "?"),
                outage["how"]))
        for name, start in sorted(never_restored.items()):
            lines.append("| `{}` | {} | tot het einde van het venster | **niet hersteld** |".format(
                name, "{:%Y-%m-%d %H:%M:%S}".format(start) if start else "?"))
        if len(listed) > 40:
            lines.append("")
            lines.append("({} onderbrekingen meer, alleen de eerste 40 staan hier)".format(
                len(listed) - 40))
        lines.append("")
        lines.append("Een herstart is een geldig herstel: de scanner zet de abonnementen opnieuw "
                     "op en er volgt geen aparte hersteld-regel meer. Wat telt is of er daarna weer "
                     "symbolen bediend worden - zie de bediend-regels hieronder.")
        lines.append("")

    if quotes:
        lines.append("Geabonneerde quote-munten: {}".format("; ".join(sorted(quotes))))
        lines.append("")
    if started_lines:
        lines.append("Startregels:")
        lines.append("")
        lines.extend("- `{}`".format(line) for line in started_lines[:10])
        lines.append("")
    if serving_lines:
        lines.append("Bediend:")
        lines.append("")
        lines.extend("- `{}`".format(line) for line in serving_lines[-5:])
        lines.append("")
    if never_restored:
        lines.append("**Deze abonnementen vielen weg en kwamen nooit terug:**")
        lines.append("")
        for name, balance in sorted(never_restored.items()):
            lines.append("- `{}` (nog steeds weg na {} keer wegvallen)".format(name, balance))
        lines.append("")
        lines.append("Symbolen achter zo'n abonnement hebben de rest van de run niets ontvangen, "
                     "dus hun candles en signalen zijn onvolledig, wat de getallen hierboven ook "
                     "zeggen.")
        lines.append("")

    # Als de onderbrekingen in de tijd samenklonteren is het meestal de eigen verbinding en niet
    # de exchange: alle groepen tegelijk weg wijst naar het netwerk hier, verspreid over de nacht
    # naar de exchange.
    all_moments = [outage["start"] for outage in outages if outage["start"]]
    all_moments.extend(moment for moment in never_restored.values() if moment)
    if all_moments:
        per_hour = Counter(moment.strftime("%Y-%m-%d %H:00") for moment in all_moments)
        busiest = per_hour.most_common(5)
        lines.append("Onderbrekingen per uur (drukste eerst): {}".format(
            ", ".join("{} = {}".format(hour, count) for hour, count in busiest)))
        lines.append("")
        together = max(per_hour.values())
        if together >= 3 and len(per_hour) == 1:
            lines.append("Alle onderbrekingen vielen in hetzelfde uur. Dat wijst eerder op de "
                         "netwerkverbinding van deze machine dan op de exchange.")
            lines.append("")

    key_points = ["{} onderbrekingen ({:.2f} per uur)".format(drops, drops_per_hour)]
    if never_restored:
        key_points.insert(0, "{} niet hersteld".format(len(never_restored)))
    if longest > OUTAGE_ATTENTION_MINUTES:
        key_points.append("langste {:.0f} minuten".format(longest))

    facts = {
        "outages": drops,
        "connectionDrops": connection_drops,
        "connectionDropsPerHour": round(connection_drops_per_hour, 3),
        "recoveredByConnection": len(outages) - by_restart,
        "recoveredByRestart": by_restart,
        "neverRestored": sorted(never_restored),
        "dropsPerHour": round(drops_per_hour, 3),
        "longestOutageMinutes": round(longest, 2),
        "restartRounds": restart_states.get("finished", 0),
        "symbolListChanges": counts["symbolsChanged"],
        "outageList": [{"name": outage["name"],
                        "start": str(outage["start"]) if outage["start"] else None,
                        "minutes": round(minutes(outage), 2) if minutes(outage) is not None else None,
                        "how": outage["how"]} for outage in outages],
    }
    report.add("streams", "Streams", verdict, lines, facts, key_points)
    return facts


def check_errors(report, entries, error_entries, top_count):
    if not entries and not error_entries:
        report.add("errors", "Fouten", UNKNOWN, ["Geen logbestand gevonden."])
        return {}

    # Het foutenlog is leidend; het hoofdlog wordt ook doorzocht omdat een waarschuwing die
    # tienduizend keer terugkomt op zichzelf al een bevinding is.
    error_messages = [message for _, level, _, message in error_entries] or \
                     [message for _, level, _, message in entries if level.upper() == "ERROR"]
    warnings = [message for _, level, _, message in entries if level.upper() == "WARN"]

    # Meldingen die wel in het foutenlog staan maar niet van de scanner zijn. Ze worden apart geteld
    # en genoemd, maar tellen niet mee voor het oordeel: anders staat een exchange op "aandacht" om
    # iets waar geen enkele beslissing uit volgt, en dan went het oordeel weg.
    ignored = [m for m in error_messages if any(part in m for part in ERRORS_NOT_OURS)]
    error_messages = [m for m in error_messages if not any(part in m for part in ERRORS_NOT_OURS)]

    grouped = Counter(normalise_message(message) for message in error_messages)
    trouble = Counter()
    for message in error_messages + warnings:
        lowered = message.lower()
        for word in TROUBLE_WORDS:
            if word in lowered:
                trouble[word] += 1

    verdict = GOOD
    if len(error_messages) >= ERRORS_BAD:
        verdict = BAD
    elif len(error_messages) >= ERRORS_ATTENTION:
        verdict = ATTENTION

    lines = ["| Meting | Waarde |", "|---|---|",
             "| Foutregels | {} |".format(len(error_messages)),
             "| Waarschuwingsregels | {} |".format(len(warnings)),
             "| Verschillende fouten | {} |".format(len(grouped)),
             ""]

    if ignored:
        buiten = Counter(normalise_message(message) for message in ignored)
        lines.append("Daarnaast {} regels die wel in het foutenlog staan maar niet van de scanner "
                     "zijn; die tellen niet mee in het getal hierboven en niet voor het oordeel:"
                     .format(len(ignored)))
        lines.append("")
        for message, count in buiten.most_common(5):
            lines.append("- {}x `{}`".format(count, message.replace("|", "\|")))
        lines.append("")

    if grouped:
        lines.append("| Aantal | Fout (getallen vervangen) |")
        lines.append("|---|---|")
        for message, count in grouped.most_common(top_count):
            lines.append("| {} | `{}` |".format(count, message.replace("|", "\\|")))
        lines.append("")
    if trouble:
        lines.append("Woorden die naar de kant van de exchange wijzen in plaats van naar ons: "
                     "{}".format(", ".join("{} = {}".format(word, count)
                                           for word, count in trouble.most_common())))
        lines.append("")
    if warnings:
        grouped_warnings = Counter(normalise_message(message) for message in warnings)
        lines.append("Meest voorkomende waarschuwingen:")
        lines.append("")
        for message, count in grouped_warnings.most_common(5):
            lines.append("- {}x `{}`".format(count, message.replace("|", "\\|")))
        lines.append("")

    facts = {"errorLines": len(error_messages), "warningLines": len(warnings),
             "errorLinesNotOurs": len(ignored),
             "distinctErrors": len(grouped), "trouble": dict(trouble)}
    report.add("errors", "Fouten", verdict, lines, facts)
    return facts


# ==============================================================================================
# 6. Signalen, zones, posities
# ==============================================================================================
def check_signals(report, main_db, exchange_names, window_start, window_end, trading_active):
    if main_db is None:
        report.add("signals", "Signalen", UNKNOWN, ["Geen hoofddatabase gevonden."])
        return {}

    connection = open_readonly(main_db)
    try:
        exchanges = {row["Name"]: row["Id"] for row in
                     connection.execute("SELECT Id, Name FROM Exchange")}
        names = {row["Id"]: row["Name"] for row in connection.execute("SELECT Id, Name FROM Interval")}
        lines = []
        verdict = GOOD
        facts = {}

        for exchange_name in exchange_names:
            exchange_id = exchanges.get(exchange_name)
            if exchange_id is None:
                continue
            lines.append("### {}".format(exchange_name))
            lines.append("")

            # Tellen laten we SQLite doen. Alle rijen ophalen en in Python filteren werkt prima op
            # een scanner-database met een paar duizend signalen, maar een emulator-database in
            # dezelfde mappenboom heeft er honderdduizenden met bijna honderd kolommen per rij -
            # dat liep vast op minuten en honderden megabytes.
            live = "(EmulatorRunId IS NULL OR EmulatorRunId = 0)"
            window = " AND OpenDate >= ? AND OpenDate <= ?"
            bounds = (str(window_start), str(window_end)) if window_start and window_end else ()
            if not bounds:
                window = ""

            signal_count = connection.execute(
                "SELECT COUNT(*) FROM Signal WHERE ExchangeId = ? AND " + live,
                (exchange_id,)).fetchone()[0]
            in_window_count = connection.execute(
                "SELECT COUNT(*) FROM Signal WHERE ExchangeId = ? AND " + live + window,
                (exchange_id,) + bounds).fetchone()[0]

            zones = connection.execute(
                "SELECT COUNT(*) FROM Zone WHERE ExchangeId = ?", (exchange_id,)).fetchone()[0]

            lines.append("| Meting | Waarde |")
            lines.append("|---|---|")
            lines.append("| Signalen bewaard (live) | {} |".format(signal_count))
            lines.append("| Daarvan binnen het venster | {} |".format(in_window_count))
            lines.append("| Zones bewaard | {} |".format(zones))
            lines.append("")

            if in_window_count:
                def grouped(column):
                    return Counter({row[0]: row[1] for row in connection.execute(
                        "SELECT {}, COUNT(*) FROM Signal WHERE ExchangeId = ? AND ".format(column)
                        + live + window + " GROUP BY 1", (exchange_id,) + bounds)})

                per_interval = Counter({names.get(key, key): value
                                        for key, value in grouped("IntervalId").items()})
                per_strategy = grouped("Strategy")
                per_side = Counter({TRADE_SIDE.get(key, key): value
                                    for key, value in grouped("Side").items()})
                lines.append("Per interval: {}".format(", ".join(
                    "{}={}".format(key, value) for key, value in per_interval.most_common())))
                lines.append("")
                lines.append("Per strategie: {}".format(", ".join(
                    "{}={}".format(key, value) for key, value in per_strategy.most_common(15))))
                lines.append("")
                lines.append("Per kant: {}".format(", ".join(
                    "{}={}".format(key, value) for key, value in per_side.most_common())))
                lines.append("")
            else:
                verdict = worst(verdict, ATTENTION)
                lines.append("Helemaal geen signalen in dit venster. Dat kan op een rustige nacht, "
                             "maar als de candles normaal binnenkomen betekent het meestal dat de "
                             "analyse voor deze exchange niet gedraaid heeft.")
                lines.append("")

            if trading_active:
                position_window = " AND CreateTime >= ? AND CreateTime <= ?" if bounds else ""
                opened = connection.execute(
                    "SELECT COUNT(*) FROM Position WHERE ExchangeId = ? AND " + live
                    + position_window, (exchange_id,) + bounds).fetchone()[0]
                still_open = connection.execute(
                    "SELECT COUNT(*) FROM Position WHERE ExchangeId = ? AND " + live
                    + " AND (CloseTime IS NULL OR CloseTime = '')", (exchange_id,)).fetchone()[0]
                # Alleen gesloten posities hebben een uitkomst. Bij een openstaande positie staat
                # Returned nog op nul, waardoor Profit neerkomt op min de inleg; dat als "resultaat"
                # optellen leest als een verlies dat er niet is.
                closed_row = connection.execute(
                    "SELECT COUNT(*), COALESCE(SUM(Profit), 0) FROM Position WHERE ExchangeId = ? "
                    "AND " + live + position_window
                    + " AND CloseTime IS NOT NULL AND CloseTime <> ''",
                    (exchange_id,) + bounds).fetchone()
                in_window_positions, open_positions = opened, still_open
                closed_in_window, profit = closed_row[0], float(closed_row[1] or 0)
                lines.append("| Handel | Waarde |")
                lines.append("|---|---|")
                lines.append("| Posities geopend in het venster | {} |".format(in_window_positions))
                lines.append("| Daarvan alweer gesloten | {} |".format(closed_in_window))
                lines.append("| Nog open aan het eind | {} |".format(open_positions))
                lines.append("| Resultaat over de gesloten posities | {:.2f} |".format(profit))
                lines.append("")
                lines.append("Het resultaat telt alleen posities die binnen het venster geopend en "
                             "ook weer gesloten zijn. Een openstaande positie heeft nog geen "
                             "uitkomst: daar staat Returned op nul, waardoor het winstveld gelijk "
                             "is aan min de inleg.")
                lines.append("")
                facts.setdefault(exchange_name, {})["positionsOpened"] = in_window_positions
                facts.setdefault(exchange_name, {})["positionsOpen"] = open_positions
                facts.setdefault(exchange_name, {})["positionsClosedInWindow"] = closed_in_window
                facts.setdefault(exchange_name, {})["resultClosed"] = round(profit, 2)

            facts.setdefault(exchange_name, {}).update({
                "signalsTotal": signal_count,
                "signalsInWindow": in_window_count,
                "zones": zones,
            })

        if not lines:
            lines = ["Geen van de exchanges in deze map komt voor in de Exchange-tabel."]
            verdict = UNKNOWN

        report.add("signals", "Signalen", verdict, lines, facts)
        return facts
    finally:
        connection.close()


def in_range(moment, start, end):
    if moment is None:
        return False
    if start and moment < start:
        return False
    if end and moment > end:
        return False
    return True


# ==============================================================================================
# 7. Geheugen over de tijd
# ==============================================================================================
def linear_slope(points):
    """Kleinste-kwadratenhelling van y over x. Levert None als er niets te fitten valt."""
    if len(points) < 3:
        return None
    count = len(points)
    mean_x = sum(x for x, _ in points) / count
    mean_y = sum(y for _, y in points) / count
    numerator = sum((x - mean_x) * (y - mean_y) for x, y in points)
    denominator = sum((x - mean_x) ** 2 for x, _ in points)
    return numerator / denominator if denominator else None


def optional_number(value, converter=float):
    """
    Een getal uit de csv, of None als de kolom ontbreekt of leeg is.

    Het verschil telt: 0 betekent "gemeten, er waren geen WebView2-processen", None betekent "deze
    csv is geschreven voordat die kolommen bestonden". Het eerste mag in een totaal, het tweede zou
    er een te laag totaal van maken.
    """
    if value is None or str(value).strip() == "":
        return None
    try:
        return converter(float(value))
    except (TypeError, ValueError):
        return None


def read_memory_samples(path):
    """Leest de csv van sample-process.ps1 (puntkomma's, eerste regel is de kop)."""
    samples = []
    try:
        # utf-8-sig: Windows PowerShell 5.1 schrijft een byte order mark, die anders aan de eerste
        # kolomnaam blijft plakken en elke meting laat afvallen.
        with open(path, "r", encoding="utf-8-sig", errors="replace") as handle:
            header = None
            for raw in handle:
                parts = [part.strip() for part in raw.rstrip("\n").split(";")]
                if header is None:
                    header = [part.lower() for part in parts]
                    continue
                if len(parts) != len(header):
                    continue
                row = dict(zip(header, parts))
                moment = parse_moment(row.get("timestamp"))
                if moment is None:
                    continue
                try:
                    samples.append({
                        "moment": moment,
                        # Het procesnummer scheidt de runs in een csv die aangevuld wordt; zonder
                        # dat veld loopt een fit dwars door een herstart heen (select_run_samples).
                        "pid": (row.get("pid") or "").strip(),
                        "workingSetMb": float(row.get("workingsetmb") or 0),
                        "privateMb": float(row.get("privatemb") or 0),
                        "threads": int(float(row.get("threads") or 0)),
                        "handles": int(float(row.get("handles") or 0)),
                        # De Photino-schil zet zijn venster in WebView2, dat in eigen processen
                        # draait; hun geheugen zit niet in het werkgeheugen van het scannerproces.
                        # Ontbreken deze kolommen, dan komt de csv van voor die meting en blijven ze
                        # None - dan gaat het oordeel hieronder gewoon over het scannerproces alleen.
                        "webviewProcesses": optional_number(row.get("webviewprocesses"), int),
                        "webviewMb": optional_number(row.get("webviewworkingsetmb")),
                        "totalMb": optional_number(row.get("totalworkingsetmb")),
                    })
                except ValueError:
                    continue
    except Exception:
        return []
    samples.sort(key=lambda sample: sample["moment"])
    return samples


def read_memory_dumps(folder):
    """Pakt op wat "Dump memory info" heeft geschreven, voor de verdeling beheerd versus native."""
    dump_folder = folder / "$debug" / "Memory Dump"
    if not dump_folder.is_dir():
        return []
    dumps = []
    for sub in sorted(dump_folder.iterdir()):
        summary = sub / "Memory information2.txt"
        if not summary.is_file():
            continue
        values = {}
        try:
            with open(summary, "r", encoding="utf-8", errors="replace") as handle:
                for line in handle:
                    match = re.match(r"\s*(Working set \(total\)|Managed heap|Native / not managed|"
                                     r"Threads|Handles)\s*:\s*([\d.,]+)", line)
                    if match:
                        number = match.group(2).replace(",", "").replace(".", "") \
                            if match.group(1) in ("Threads", "Handles") else \
                            match.group(2).replace(",", "")
                        try:
                            values[match.group(1)] = float(number)
                        except ValueError:
                            pass
                    if len(values) >= 5:
                        break
        except Exception:
            continue
        if values:
            values["folder"] = sub.name
            dumps.append(values)
    return dumps


def select_run_samples(samples, window_start, window_end):
    """
    Houdt van een csv over meerdere nachten alleen de metingen van DEZE run over.

    Twee dingen zitten er anders in dan je zou willen. De csv wordt aangevuld en niet overschreven,
    dus hij bevat ook de vorige nachten - die horen niet in de fit. En een scanner die herstart is
    krijgt een nieuw procesnummer terwijl zijn geheugen weer bij nul begint; een helling door beide
    processen heen meet die terugval mee en zegt niets.

    Vandaar: eerst op het venster snijden, dan alleen het nieuwste procesnummer daarbinnen houden.
    Wat afvalt wordt teruggegeven zodat het rapport het kan melden in plaats van stilzwijgend
    minder te meten.
    """
    if window_start and window_end:
        in_window = [s for s in samples if window_start <= s["moment"] <= window_end]
    else:
        in_window = list(samples)
    dropped_outside = len(samples) - len(in_window)
    if not in_window:
        return [], dropped_outside, 0, None

    # Het nieuwste procesnummer is de run waar het venster over gaat; een eerder nummer binnen het
    # venster hoort bij een scanner die tijdens de nacht herstart is.
    processes = []
    for sample in in_window:
        if not processes or processes[-1] != sample["pid"]:
            processes.append(sample["pid"])
    latest = in_window[-1]["pid"]
    selected = [s for s in in_window if s["pid"] == latest]
    return selected, dropped_outside, len(in_window) - len(selected), processes


def check_memory(report, folder, memory_csv, window_start=None, window_end=None):
    lines = []
    verdict = UNKNOWN
    facts = {}
    key_points = []

    all_samples = read_memory_samples(memory_csv) if memory_csv else []
    samples, dropped_outside, dropped_older, processes = select_run_samples(
        all_samples, window_start, window_end)
    if samples:
        first, last = samples[0], samples[-1]
        hours = (last["moment"] - first["moment"]).total_seconds() / 3600.0

        # Waar het oordeel over gaat. Zijn de WebView2-processen meegemeten, dan is dat het totaal:
        # een lek in de renderer laat het scannerproces zelf vlak en zou anders onzichtbaar blijven.
        # Bij een csv van voor die meting blijft het het scannerproces alleen, en dat zegt het
        # rapport er dan bij in plaats van een totaal te suggereren dat er niet is.
        measured_total = all(sample["totalMb"] is not None for sample in samples)
        basis = (lambda sample: sample["totalMb"]) if measured_total \
            else (lambda sample: sample["workingSetMb"])
        has_webview = measured_total and max(
            (sample["webviewProcesses"] or 0) for sample in samples) > 0

        peak = max(basis(sample) for sample in samples)
        slope = linear_slope([((sample["moment"] - first["moment"]).total_seconds() / 3600.0,
                               basis(sample)) for sample in samples])
        growth = slope if slope is not None else (
            (basis(last) - basis(first)) / hours if hours else 0.0)

        # Los bijgehouden zodat een nacht met en een nacht zonder WebView2-meting nog steeds op
        # hetzelfde getal te vergelijken zijn.
        host_slope = linear_slope([((sample["moment"] - first["moment"]).total_seconds() / 3600.0,
                                    sample["workingSetMb"]) for sample in samples])
        host_growth = host_slope if host_slope is not None else (
            (last["workingSetMb"] - first["workingSetMb"]) / hours if hours else 0.0)

        # Een helling over een paar minuten rekent ruis door naar honderden megabytes per uur.
        # Onder een uur aan metingen wordt het getal wel gemeld maar niet beoordeeld.
        if hours < MEMORY_MINIMAL_HOURS:
            verdict = UNKNOWN
        elif growth > MEMORY_BAD_MB_PER_HOUR:
            verdict = BAD
        elif growth > MEMORY_ATTENTION_MB_PER_HOUR:
            verdict = ATTENTION
        else:
            verdict = GOOD

        lines.extend([
            "| Meting | Waarde |",
            "|---|---|",
            "| Metingen | {} over {:.1f} uur |".format(len(samples), hours),
            "| Werkgeheugen aan het begin | {:.0f} MB |".format(first["workingSetMb"]),
            "| Werkgeheugen aan het eind | {:.0f} MB |".format(last["workingSetMb"]),
        ])
        if has_webview:
            lines.extend([
                "| WebView2 aan het begin | {:.0f} MB in {} processen |".format(
                    first["webviewMb"], first["webviewProcesses"]),
                "| WebView2 aan het eind | {:.0f} MB in {} processen |".format(
                    last["webviewMb"], last["webviewProcesses"]),
                "| **Totaal begin / eind** | **{:.0f} / {:.0f} MB** |".format(
                    first["totalMb"], last["totalMb"]),
            ])
        lines.extend([
            "| Piek | {:.0f} MB |".format(peak),
            "| **Groei** | **{:+.1f} MB per uur** |".format(growth),
            "| Threads begin / eind | {} / {} |".format(first["threads"], last["threads"]),
            "| Handles begin / eind | {} / {} |".format(first["handles"], last["handles"]),
            "",
            "De groei is een kleinste-kwadratenfit over alle metingen, dus een enkele piek bepaalt "
            "hem niet. Een vlak of negatief getal is hoe een gezonde run eruitziet; een gestage "
            "klim over vele uren is het handschrift van een lek.",
            "",
        ])
        if has_webview:
            lines.extend([
                "Piek en groei gaan hier over het **totaal**: het scannerproces plus zijn "
                "WebView2-processen. Die laatste draaien naast de scanner en niet erin, dus hun "
                "geheugen staat niet in het werkgeheugen hierboven. Het scannerproces op zichzelf "
                "groeide {:+.1f} MB per uur; blijft dat vlak terwijl het totaal klimt, dan zit de "
                "groei in de WebView2-processen en niet in de scanner.".format(host_growth),
                "",
            ])
        elif not measured_total:
            lines.extend([
                "De WebView2-processen zijn niet meegemeten - deze csv is geschreven met een versie "
                "van `sample-process.ps1` van voor die kolommen. Piek en groei gaan dus over het "
                "scannerproces alleen, en wat de vensterschil aan geheugen gebruikt valt daarbuiten.",
                "",
            ])
        key_points.append("{:+.1f} MB per uur over {:.1f} uur{}".format(
            growth, hours, " (scanner + WebView2)" if has_webview else ""))
        if hours < MEMORY_MINIMAL_HOURS:
            lines.append("Maar {:.2f} uur aan metingen: te kort om te beoordelen. De groei "
                         "hierboven is doorgerekende ruis, geen trend.".format(hours))
            lines.append("")

        if dropped_outside:
            lines.append("{:,} metingen lagen buiten het venster en tellen niet mee. De csv wordt "
                         "aangevuld en niet overschreven, dus de nachten ervoor staan er ook in; "
                         "die mogen blijven staan.".format(dropped_outside))
            lines.append("")
        if dropped_older:
            lines.append("De scanner is tijdens deze nacht herstart: er staan {} procesnummers in "
                         "het venster ({}). Alleen het nieuwste is gemeten - een helling door een "
                         "herstart heen telt de terugval naar nul mee en zegt niets."
                         .format(len(processes), ", ".join(processes)))
            lines.append("")

        # De steekproef hoort tot het einde van het venster door te lopen. Stopt hij eerder, dan is
        # het proces uit beeld geraakt (sample-process.ps1 die het nieuwe procesnummer niet vond) en
        # gaat het oordeel over de verkeerde uren. Dat is geen gezonde run, dat is een meetgat.
        covered = None
        if window_end:
            missing_minutes = (window_end - last["moment"]).total_seconds() / 60.0
            covered = percentage(hours, (window_end - window_start).total_seconds() / 3600.0) \
                if window_start else None
            if missing_minutes > MEMORY_COVERAGE_GAP_MINUTES:
                verdict = UNKNOWN
                lines.append("**De metingen houden {:.0f} minuten voor het einde van het venster "
                             "op** ({:%Y-%m-%d %H:%M} tegen {:%H:%M}), dus er is over {:.0f}% van "
                             "de run gemeten. Meestal is de scanner herstart en heeft "
                             "`sample-process.ps1` het nieuwe procesnummer niet opgepakt. Er wordt "
                             "hier daarom geen oordeel geveld: de cijfers hierboven gaan over de "
                             "uren voor dat moment, niet over de nacht."
                             .format(missing_minutes, last["moment"], window_end, covered or 0))
                lines.append("")
                key_points.append("meetgat: alleen {:.0f}% van de run gemeten".format(covered or 0))
        if last["threads"] > first["threads"] * 1.5 and last["threads"] - first["threads"] > 20:
            verdict = worst(verdict, ATTENTION)
            lines.append("Het aantal threads groeide ook, wat wijst op threads of timers die wel "
                         "starten maar nooit eindigen.")
            lines.append("")

        facts = {
            "samples": len(samples),
            "hours": round(hours, 2),
            "startMb": round(first["workingSetMb"], 1),
            "endMb": round(last["workingSetMb"], 1),
            "peakMb": round(peak, 1),
            "growthMbPerHour": round(growth, 2),
            # Waar de groei hierboven over gaat, en het scannerproces apart. Zonder dat onderscheid
            # is een nacht met WebView2-meting niet te vergelijken met een nacht zonder.
            "growthBasis": "scanner+webview" if measured_total else "scanner",
            "hostGrowthMbPerHour": round(host_growth, 2),
            "webviewMeasured": measured_total,
            "webviewProcesses": last["webviewProcesses"] if has_webview else None,
            "webviewStartMb": round(first["webviewMb"], 1) if has_webview else None,
            "webviewEndMb": round(last["webviewMb"], 1) if has_webview else None,
            "totalStartMb": round(first["totalMb"], 1) if measured_total else None,
            "totalEndMb": round(last["totalMb"], 1) if measured_total else None,
            "threadsStart": first["threads"], "threadsEnd": last["threads"],
            "handlesStart": first["handles"], "handlesEnd": last["handles"],
            "processId": last["pid"],
            "processesInWindow": processes,
            "samplesOutsideWindow": dropped_outside,
            "windowCoveragePercentage": round(covered, 1) if covered is not None else None,
        }
    elif all_samples:
        lines.append("Er staan wel {:,} metingen in de csv, maar geen enkele binnen het venster "
                     "van deze run (laatste meting {:%Y-%m-%d %H:%M}). Dat is het beeld als "
                     "`sample-process.ps1` niet meer draaide toen de scanner startte, of als hij "
                     "het procesnummer van een herstart niet heeft opgepakt."
                     .format(len(all_samples), all_samples[-1]["moment"]))
        lines.append("")
        key_points.append("geen metingen binnen het venster")
        # Wel vastleggen dat er gemeten IS, anders is een nacht zonder steekproef in de json niet te
        # onderscheiden van een nacht waarin de steekproef het venster miste.
        facts = {
            "samples": 0,
            "samplesOutsideWindow": len(all_samples),
            "lastSampleOutsideWindow": str(all_samples[-1]["moment"]),
            "windowCoveragePercentage": 0.0,
        }
    else:
        lines.append("Geen geheugenmetingen meegegeven. Start `sample-process.ps1` naast de scanner "
                     "om deze sectie te vullen; zonder die metingen blijft een lek onzichtbaar tot "
                     "de machine geen geheugen meer heeft.")
        lines.append("")
        key_points.append("niet gemeten - sample-process.ps1 draaide niet")

    dumps = read_memory_dumps(folder)
    if dumps:
        lines.append("**Geheugendumps in de datamap gevonden** (verdeling beheerd versus native)")
        lines.append("")
        lines.append("| Dump | Werkgeheugen MB | Beheerde heap MB | Native MB | Threads | Handles |")
        lines.append("|---|---|---|---|---|---|")
        for dump in dumps:
            lines.append("| {} | {:.0f} | {:.0f} | {:.0f} | {:.0f} | {:.0f} |".format(
                dump.get("folder", "?"),
                dump.get("Working set (total)", 0) / 1024 / 1024,
                dump.get("Managed heap", 0) / 1024 / 1024,
                dump.get("Native / not managed", 0) / 1024 / 1024,
                dump.get("Threads", 0), dump.get("Handles", 0)))
        lines.append("")
        lines.append("Groeit het werkgeheugen terwijl de beheerde heap vlak blijft, dan zit de "
                     "groei in native geheugen en verklaren heaptype-statistieken hem niet.")
        lines.append("")
        facts["dumps"] = len(dumps)

    report.add("memory", "Geheugen", verdict, lines, facts, key_points)
    return facts


# ==============================================================================================
# Hoofdprogramma
# ==============================================================================================
def main():
    parser = argparse.ArgumentParser(
        description="Controleer een nachtelijke scannerrun tegen zijn logs en databases.")
    parser.add_argument("--folder", required=True,
                        help="Datamap. Een kale naam wordt binnen %%APPDATA%% opgezocht.")
    parser.add_argument("--exchange", help="Beperk het rapport tot deze exchange (standaard: alle).")
    parser.add_argument("--start", help="Begin van het venster, bijvoorbeeld \"2026-08-16 22:00\".")
    parser.add_argument("--end", help="Einde van het venster. Standaard de laatste logregel.")
    parser.add_argument("--memory-csv", help="Csv geschreven door sample-process.ps1.")
    parser.add_argument("--out", help="Schrijf het rapport naar dit bestand. De extensie kiest de "
                                      "vorm: .html geeft een gekleurde pagina met inhoudsopgave, "
                                      "elke andere extensie geeft markdown.")
    parser.add_argument("--json", help="Schrijf de machineleesbare feiten naar dit bestand.")
    parser.add_argument("--top", type=int, default=20, help="Rijen per toplijst (standaard 20).")
    parser.add_argument("--deep", action="store_true",
                        help="Toets de plausibiliteit van de candles over de hele historie in "
                             "plaats van alleen het venster. Kost seconden per exchange; gebruik "
                             "het als een exchange voor het eerst onderzocht wordt, niet elke "
                             "ochtend.")
    arguments = parser.parse_args()

    folder = resolve_folder(arguments.folder)
    if not folder.is_dir():
        print("Datamap niet gevonden: {}".format(folder), file=sys.stderr)
        # De map verkeerd benoemen is de meest voorkomende start, dus tonen wat er wel staat. Let
        # op dat een scanner die vanuit een verpakte of afgeschermde omgeving start zijn map in de
        # omgeleide AppData van die omgeving zet, waar dit script niet bij kan.
        appdata = os.environ.get("APPDATA")
        if appdata and Path(appdata).is_dir():
            candidates = sorted(path.name for path in Path(appdata).iterdir()
                                if path.is_dir() and "cryptoscan" in path.name.lower())
            if candidates:
                print("Datamappen gevonden in {}: {}".format(appdata, ", ".join(candidates)),
                      file=sys.stderr)
        return 2

    main_db = find_main_database(folder)
    candle_databases = find_candle_databases(folder, arguments.exchange)
    exchange_names = [name for name, _ in candle_databases]
    if arguments.exchange and not exchange_names:
        exchange_names = [arguments.exchange]

    main_logs, error_logs = find_logs(folder)
    entries = read_log_lines(main_logs)
    error_entries = read_log_lines(error_logs)

    window_start = parse_moment(arguments.start)
    window_end = parse_moment(arguments.end)
    window_source = "meegegeven op de opdrachtregel"
    if window_start is None and entries:
        window_start = find_last_startup(entries)
        if window_start is not None:
            window_source = "laatste scannerstart die in het log staat"
        else:
            window_start = entries[0][0]
            window_source = ("eerste regel in het log - geen startmerkteken gevonden, dus alles "
                             "wat het log van een eerdere sessie bewaarde valt binnen het venster")
    if window_end is None and entries:
        window_end = entries[-1][0]
    window_hours = ((window_end - window_start).total_seconds() / 3600.0
                    if window_start and window_end else 0.0)

    # Alles hierboven is lokale kloktijd; de databases bewaren UTC.
    utc_start, utc_end = to_utc(window_start), to_utc(window_end)

    # Het log bevat meer dan deze run: NLog rolt om middernacht om, en de vorige sessie staat vaak
    # in hetzelfde bestand. Storingen en fouten van gisterochtend horen niet in het verslag van
    # vannacht, dus alles wat over de run gaat kijkt vanaf hier alleen binnen het venster.
    def inside_window(collection):
        if not (window_start and window_end):
            return collection
        return [entry for entry in collection
                if entry[0] and window_start <= entry[0] <= window_end]

    window_entries = inside_window(entries)
    window_error_entries = inside_window(error_entries)

    # The subscription count is the yardstick for candle coverage, so it has to come from THIS run.
    # Taken from the whole log it picks up the largest count of any earlier session in the same
    # file, and a night that subscribed 125 symbols then gets measured against yesterday's 157.
    subscribed = subscribed_per_exchange(window_entries)

    # Draait het proces nog? Dat verandert wat een achterstand betekent: de opslagdraad heeft dan
    # nog niet weggeschreven, en zonder dat onderscheid staat elk rapport van een levende scanner
    # op oranje om een reden die geen gebrek is.
    # De lock in de datamap is het enige harde antwoord. Een scanner kan minutenlang stil zijn -
    # geen signaal, geen symbolenronde - en de leeftijd van de laatste logregel verklaart hem dan
    # ten onrechte dood, met de strengste achterstandsgrenzen als gevolg. De leeftijd van de
    # logregel blijft de terugval voor mappen zonder lock (een oudere sessie, een kopie).
    still_running, running_source = process_is_running(folder)
    if still_running is None:
        still_running = bool(window_end) and (
            (datetime.now() - window_end).total_seconds() / 60.0 < STILL_RUNNING_MINUTES)
        running_source = "leeftijd van de laatste logregel"

    report = Report()
    settings_facts = check_settings(report, folder)
    check_symbols(report, main_db, exchange_names)
    check_candles(report, candle_databases, main_db, utc_start, utc_end, arguments.top,
                  subscribed, arguments.deep, still_running)
    check_barometer(report, candle_databases, main_db, utc_start, utc_end, subscribed,
                    still_running)
    check_streams(report, window_entries, window_hours or 1.0)
    check_errors(report, window_entries, window_error_entries, arguments.top)
    check_signals(report, main_db, exchange_names, utc_start, utc_end,
                  settings_facts.get("tradingActive", False))
    # Lokale tijden: sample-process.ps1 schrijft Get-Date, niet UTC.
    check_memory(report, folder, arguments.memory_csv, window_start, window_end)

    header = {
        "folder": str(folder),
        "exchanges": exchange_names,
        "windowStart": str(window_start) if window_start else None,
        "windowEnd": str(window_end) if window_end else None,
        "windowStartUtc": str(utc_start) if utc_start else None,
        "windowEndUtc": str(utc_end) if utc_end else None,
        "windowHours": round(window_hours, 2),
        "windowSource": window_source,
        "stillRunning": still_running,
        "stillRunningSource": running_source or "leeftijd van de laatste logregel",
        "generated": str(datetime.now().replace(microsecond=0)),
    }
    header_lines = [
        "| | |",
        "|---|---|",
        "| Datamap | `{}` |".format(folder),
        "| Exchanges | {} |".format(", ".join(exchange_names) or "(geen gevonden)"),
        "| Venster (lokaal) | {} tot {} ({:.1f} uur) |".format(
            window_start or "?", window_end or "?", window_hours),
        "| Venster (utc) | {} tot {} |".format(utc_start or "?", utc_end or "?"),
        "| Venster ontleend aan | {} |".format(window_source),
        "| Scanner | {} ({}) |".format(
            "draait nog - achterstand telt daarom ruimer" if still_running
            else "gestopt - achterstand telt volledig mee",
            running_source or "leeftijd van de laatste logregel"),
        "| Logbestanden | {} hoofd, {} fouten |".format(len(main_logs), len(error_logs)),
        "| Rapport gemaakt op | {} |".format(header["generated"]),
    ]

    # De extensie kiest de vorm. Html leest prettiger (kleur, inhoudsopgave, opent met een
    # dubbelklik), markdown blijft bestaan voor wie het rapport wil doorzoeken of ergens in wil
    # plakken. Zonder --out gaat de markdown naar het scherm, want html op een terminal is niets.
    if arguments.out:
        target = Path(arguments.out)
        if target.suffix.lower() in (".html", ".htm"):
            target.write_text(report.to_html(header_lines, header), encoding="utf-8")
        else:
            target.write_text(report.to_markdown(header_lines), encoding="utf-8")
        print("Rapport geschreven naar {}".format(arguments.out))
    else:
        print(report.to_markdown(header_lines))

    if arguments.json:
        Path(arguments.json).write_text(report.to_json(header), encoding="utf-8")
        print("Feiten geschreven naar {}".format(arguments.json))

    return {GOOD: 0, UNKNOWN: 0, ATTENTION: 1, BAD: 2}[report.overall()]


if __name__ == "__main__":
    sys.exit(main())
