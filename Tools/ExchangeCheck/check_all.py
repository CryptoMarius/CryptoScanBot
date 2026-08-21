#!/usr/bin/env python3
"""
Runs check_exchange.py over every scanner data folder under one base folder.

Meant to be started by "Make reports.cmd" (double click), not typed by hand: it finds the data
folders itself, matches each one to its memory samples, writes a report per folder and prints one
overview line per exchange.

A data folder is any folder holding a CryptoScanBot.db - so both
    %APPDATA%\\CryptoScanBot\\Data\\Binance\\Futures
    %APPDATA%\\CryptoScanBot-KRTest
are found without having to list them anywhere.

Next to the per-exchange reports it writes OVERVIEW_NAME: one page holding the verdict of every
exchange, a subject-by-exchange grid, the measurements side by side and the drift against the
previous night. Nineteen reports answer "what happened on Kraken"; this one answers "what happened
last night", and a number that is normal on eighteen exchanges and wild on the nineteenth only
shows up when they stand next to each other.
"""

import argparse
import json
import os
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

# The report writer doubles as a library here: same stylesheet, same coloured marks, same wording
# for the verdicts. Sharing it is the point - an overview in its own style would drift away from
# the pages it links to at the first change. Importing is safe, check_exchange guards its main().
import check_exchange

MAXIMUM_DEPTH = 3          # how deep below the base folder to look
# Emulator sessions live in the same folder tree and look like a data folder, but they hold a
# backtest, not a night run: no live stream, no log of subscriptions, and a Position table with
# hundreds of thousands of rows. Checking them answers nothing and costs minutes.
EXCLUDED_PARTS = ("emulator",)
WORKERS = 4                # checks run in parallel; the work is disk bound, so more is not faster
VERDICT_ORDER = {"bad": 0, "attention": 1, "unknown": 2, "good": 3}

# Leading underscore so it sorts above the per-exchange reports in the folder listing: this is the
# one to open first. The name is kept from the hand-written version that preceded it.
OVERVIEW_NAME = "_Overzicht alle exchanges"


def find_data_folders(base, depth=MAXIMUM_DEPTH):
    """Every folder with a main database in it, deepest match wins (no nested duplicates)."""
    found = []
    if not base.is_dir():
        return found
    for path in sorted(base.iterdir()):
        if not path.is_dir():
            continue
        if any(part in path.name.lower() for part in EXCLUDED_PARTS):
            continue
        if any(child.name.lower().startswith("cryptoscanbot") and child.suffix.lower() == ".db"
               for child in path.iterdir() if child.is_file()):
            found.append(path)
        elif depth > 1:
            found.extend(find_data_folders(path, depth - 1))
    return found


def last_activity(folder):
    """Newest log file timestamp, or None when the folder has no log at all."""
    log_folder = folder / "Log"
    if not log_folder.is_dir():
        return None
    stamps = [path.stat().st_mtime for path in log_folder.glob("*.log")]
    return max(stamps) if stamps else None


def sample_name(folder):
    """
    Same naming as sample-process.ps1: the last two parts of the data folder joined with a dash,
    so "...\\Data\\Binance\\Futures" pairs with "Binance-Futures-memory.csv".
    """
    parts = [part for part in folder.parts if part not in ("\\", "/")]
    if len(parts) >= 2:
        return "{}-{}".format(parts[-2], parts[-1])
    return folder.name


def archive_previous_reports(output):
    """
    Move whatever is already in the report folder into Older/<date it was written>.

    Two things went wrong without this, both on 20-08-2026:

      - The folder held Coinbase-Futures-report.html from a crashed run of 15-08. Nothing had
        refreshed it since (that data folder is gone), so it sat between the reports of last night
        looking exactly as current as they did, full of "not measured" - and it was read as a report
        about last night.

      - Every night overwrote the *-facts.json of the night before. Those files exist to be able to
        compare night four against night one, and there was never more than one night on disk.

    Moving, not deleting: a report is cheap to keep and the user decides when it goes.
    """
    keep = [path for path in output.glob("*")
            if path.is_file() and (path.name.endswith("-report.html")
                                   or path.name.endswith("-report.md")
                                   or path.name.endswith("-facts.json")
                                   # The overview goes along for the same reason: left behind it
                                   # would sit above the fresh reports, name first in the listing,
                                   # describing a night that is no longer the one on disk.
                                   or path.stem == OVERVIEW_NAME)]
    if not keep:
        return 0

    moved = 0
    for path in keep:
        stamp = time.strftime("%Y-%m-%d", time.localtime(path.stat().st_mtime))
        target_folder = output / "Older" / stamp
        target_folder.mkdir(parents=True, exist_ok=True)
        target = target_folder / path.name
        # A second run on the same day would collide with the first; number those instead of
        # silently throwing one of them away.
        if target.exists():
            index = 2
            while (target_folder / "{} ({}){}".format(target.stem, index, target.suffix)).exists():
                index += 1
            target = target_folder / "{} ({}){}".format(target.stem, index, target.suffix)
        path.replace(target)
        moved += 1
    return moved


# ==============================================================================================
# Het overzicht over alle exchanges
# ==============================================================================================
def section_of(facts, key):
    """One section out of a facts file, or an empty one when that check never ran."""
    for section in facts.get("sections", []):
        if section.get("key") == key:
            return section
    return {"key": key, "title": key, "verdict": check_exchange.UNKNOWN, "facts": {}}


def total_over_exchanges(section, field):
    """
    Sum one field over the exchanges inside a section.

    The candle and signal sections are keyed by exchange, because a data folder can hold more than
    one candle store. In practice it holds one, but summing is right when it holds two and picking
    an arbitrary first one is not.
    """
    total = 0
    for value in (section.get("facts") or {}).values():
        if isinstance(value, dict):
            total += value.get(field) or 0
    return total


def overview_row(facts_path):
    """Everything the overview needs from one exchange, read back from its facts file."""
    facts = json.loads(facts_path.read_text(encoding="utf-8"))
    header = facts.get("header") or {}
    label = facts_path.name[:-len("-facts.json")]

    candles = section_of(facts, "candles")
    streams = section_of(facts, "streams")
    errors = section_of(facts, "errors")
    signals = section_of(facts, "signals")
    memory_facts = section_of(facts, "memory").get("facts") or {}

    # The tail of the run is the number that says something about a leak; the slope over the whole
    # run still carries the cache warm-up. Falls back when the run was too short for a tail.
    plateau = memory_facts.get("plateauGrowthMbPerHour")
    if plateau is None:
        plateau = memory_facts.get("growthMbPerHour")

    return {
        "label": label,
        "report": "{}-report.html".format(label),
        "exchanges": ", ".join(header.get("exchanges") or []) or label,
        "verdict": facts.get("overall", check_exchange.UNKNOWN),
        "hours": header.get("windowHours") or 0.0,
        "windowStart": header.get("windowStart") or "",
        "windowEnd": header.get("windowEnd") or "",
        "stillRunning": bool(header.get("stillRunning")),
        "sections": [(section.get("key"), section.get("title"), section.get("verdict"))
                     for section in facts.get("sections", [])],
        "attention": [section.get("title") for section in facts.get("sections", [])
                      if section.get("verdict") in (check_exchange.ATTENTION, check_exchange.BAD)],
        "numbers": {
            "symbols": total_over_exchanges(section_of(facts, "symbols"), "symbols"),
            "subscribed": total_over_exchanges(candles, "subscribed"),
            "silent": total_over_exchanges(candles, "silentSymbols"),
            "missing": total_over_exchanges(candles, "missingMinutes"),
            "impossible": total_over_exchanges(candles, "impossibleCandles"),
            "outages": (streams.get("facts") or {}).get("outages") or 0,
            "drops": (streams.get("facts") or {}).get("connectionDrops") or 0,
            "dropsPerHour": (streams.get("facts") or {}).get("connectionDropsPerHour") or 0.0,
            "never": len((streams.get("facts") or {}).get("neverRestored") or []),
            "errors": (errors.get("facts") or {}).get("errorLines") or 0,
            "signals": total_over_exchanges(signals, "signalsInWindow"),
            "memory": plateau,
        },
    }


def previous_night(output, label, window_start):
    """
    The same exchange on the night before, out of Older/<date>/<label>-facts.json.

    Newest first, and the first one whose window differs from tonight's wins. That skips an earlier
    run of THIS night: the archive folder is named after the day the report was written, so a
    second run this morning lands under today's date next to the reports it replaced. Comparing a
    night against itself would show a drift of zero everywhere and hide the real one.
    """
    older = output / "Older"
    if not older.is_dir():
        return None
    for folder in sorted((path for path in older.iterdir() if path.is_dir()),
                         key=lambda path: path.name, reverse=True):
        candidate = folder / "{}-facts.json".format(label)
        if not candidate.is_file():
            continue
        try:
            row = overview_row(candidate)
        except Exception:
            continue
        if row["windowStart"] and row["windowStart"] == window_start:
            continue
        row["night"] = folder.name
        return row
    return None


def number_cell(value, digits=0, signed=False):
    """A measurement as text, with the empty case spelled out instead of shown as a zero."""
    if value is None:
        return "-"
    if digits:
        pattern = "{:+,.%df}" % digits if signed else "{:,.%df}" % digits
        return pattern.format(value)
    return "{:+,}".format(int(value)) if signed else "{:,}".format(int(value))


def drift_cell(now, before, digits=0, lower_is_better=True):
    """Tonight against the night before: what it was, what it is, and the difference."""
    if before is None or now is None:
        return "-"
    difference = now - before
    if abs(difference) < (0.05 if digits else 0.5):
        return "{} (gelijk)".format(number_cell(now, digits))
    worse = (difference > 0) if lower_is_better else (difference < 0)
    return '{} &rarr; <strong>{}</strong> <span class="badge {}">{}</span>'.format(
        number_cell(before, digits), number_cell(now, digits),
        check_exchange.ATTENTION if worse else check_exchange.GOOD,
        number_cell(difference, digits, signed=True))


def write_overview(output, rows, base, skipped, max_age_days):
    """
    One page above the nineteen: verdict per exchange, subject grid, measurements side by side and
    the drift against the previous night. Written with check_exchange's own style sheet and its
    coloured marks, so it reads as the front page of the same set and not as a separate tool.
    """
    if not rows:
        return None

    rows = sorted(rows, key=lambda row: (VERDICT_ORDER.get(row["verdict"], 9), row["label"]))
    worst = max((row["verdict"] for row in rows),
                key=lambda verdict: check_exchange.VERDICT_RANK.get(verdict, 0))
    counts = {verdict: sum(1 for row in rows if row["verdict"] == verdict)
              for verdict in (check_exchange.BAD, check_exchange.ATTENTION,
                              check_exchange.UNKNOWN, check_exchange.GOOD)}

    # The subject columns come from the reports themselves, so a subject added there turns up here
    # without this file knowing about it. Taken from the widest report: a folder whose check fell
    # over early has fewer sections, and that one must not shorten the grid for all the others.
    columns = []
    for row in rows:
        if len(row["sections"]) > len(columns):
            columns = [(key, title) for key, title, _ in row["sections"]]

    starts = [row["windowStart"] for row in rows if row["windowStart"]]
    ends = [row["windowEnd"] for row in rows if row["windowEnd"]]
    hours = [row["hours"] for row in rows if row["hours"]]
    mark = check_exchange.VERDICT_MARK
    text = check_exchange.VERDICT_TEXT
    escape = check_exchange.html_inline

    out = ["<!doctype html>", '<html lang="nl">', "<head>", '<meta charset="utf-8">',
           '<meta name="viewport" content="width=device-width, initial-scale=1">',
           "<title>Overzicht alle exchanges - {}</title>".format(text[worst]),
           "<style>{}</style>".format(check_exchange.PAGE_STYLE), "</head>", "<body>",
           '<div class="layout">']

    out.append("<nav>")
    out.append("<h2>Op deze pagina</h2>")
    out.append('<a href="#top"><span class="dot {}"></span>Eindoordeel: {}</a>'.format(
        worst, text[worst]))
    out.append('<a href="#verdicts"><span class="dot {}"></span>Per exchange</a>'.format(worst))
    out.append('<a href="#grid"><span class="dot {}"></span>Onderwerpen</a>'.format(worst))
    out.append('<a href="#numbers"><span class="dot {}"></span>Meetwaarden</a>'.format(
        check_exchange.GOOD))
    out.append('<a href="#drift"><span class="dot {}"></span>Vorige nacht</a>'.format(
        check_exchange.GOOD))
    out.append("<h2>Rapporten</h2>")
    for row in rows:
        out.append('<a href="{}"><span class="dot {}"></span>{}</a>'.format(
            escape(row["report"]), row["verdict"], escape(row["label"])))
    out.append("</nav>")

    out.append("<main>")

    # ---- kop --------------------------------------------------------------------------------
    out.append('<div class="card" id="top">')
    out.append("<h1>Overzicht - alle exchanges</h1>")
    out.append('<p class="subtitle">{} datamappen gecontroleerd</p>'.format(len(rows)))
    out.append('<p class="verdict {}">{} Slechtste oordeel van de nacht: {}</p>'.format(
        worst, mark[worst], text[worst]))
    out.append('<div class="scroll"><table><tbody>')
    out.append("<tr><th>Basismap</th><td>{}</td></tr>".format(escape(str(base))))
    if starts and ends:
        out.append("<tr><th>Venster (lokaal)</th><td>{} tot {}</td></tr>".format(
            escape(min(starts)), escape(max(ends))))
    if hours:
        out.append("<tr><th>Duur</th><td>{:.1f} tot {:.1f} uur per scanner</td></tr>".format(
            min(hours), max(hours)))
    running = [row["label"] for row in rows if row["stillRunning"]]
    if running:
        out.append("<tr><th>Draaide nog</th><td>{}</td></tr>".format(escape(", ".join(running))))
    out.append("<tr><th>Rapport gemaakt op</th><td>{}</td></tr>".format(
        time.strftime("%Y-%m-%d %H:%M:%S")))
    if skipped:
        out.append("<tr><th>Overgeslagen</th><td>{} zonder log van de laatste {:.0f} dag(en): {}"
                   "</td></tr>".format(len(skipped), max_age_days, escape(", ".join(skipped))))
    out.append("</tbody></table></div>")
    out.append("<p>Elke exchange draait in een eigen proces met een eigen datamap, dus de vensters "
               "lopen een paar minuten uiteen. Klik een exchange aan voor het hele rapport.</p>")
    out.append("</div>")

    # ---- waar moet je naar kijken -----------------------------------------------------------
    out.append('<div class="card">')
    out.append("<h2>Waar moet je naar kijken</h2>")
    out.append("<p>{}</p>".format(" ".join(
        '<span class="badge {}">{} x {}</span>'.format(verdict, counts[verdict], text[verdict])
        for verdict in (check_exchange.BAD, check_exchange.ATTENTION, check_exchange.UNKNOWN,
                        check_exchange.GOOD) if counts[verdict])))
    flagged = [row for row in rows
               if row["verdict"] in (check_exchange.BAD, check_exchange.ATTENTION)]
    if flagged:
        out.append('<div class="scroll"><table>')
        out.append("<thead><tr><th></th><th>Exchange</th><th>Onderwerpen die aandacht vragen</th>"
                   "</tr></thead><tbody>")
        for row in flagged:
            out.append('<tr><td class="mark {}">{}</td><td><a href="{}">{}</a></td><td>{}</td>'
                       "</tr>".format(row["verdict"], mark[row["verdict"]], escape(row["report"]),
                                      escape(row["label"]),
                                      escape(", ".join(row["attention"])) or "-"))
        out.append("</tbody></table></div>")
    else:
        out.append("<p><strong>Geen enkele exchange vraagt aandacht.</strong></p>")
    out.append("</div>")

    # ---- eindoordeel per exchange -----------------------------------------------------------
    out.append('<section class="{}" id="verdicts">'.format(worst))
    out.append('<h2>{} Eindoordeel per exchange'
               '<a class="top" href="#top">terug naar boven</a></h2>'.format(mark[worst]))
    out.append('<div class="scroll"><table>')
    out.append("<thead><tr><th></th><th>Exchange</th><th>Oordeel</th><th>Uren</th>"
               "<th>Onderwerpen die aandacht vragen</th></tr></thead><tbody>")
    for row in rows:
        out.append('<tr><td class="mark {0}">{1}</td><td><a href="{2}">{3}</a></td>'
                   '<td><span class="badge {0}">{4}</span></td><td>{5:.1f}</td><td>{6}</td></tr>'
                   .format(row["verdict"], mark[row["verdict"]], escape(row["report"]),
                           escape(row["label"]), text[row["verdict"]], row["hours"],
                           escape(", ".join(row["attention"])) or "-"))
    out.append("</tbody></table></div>")
    out.append("</section>")

    # ---- onderwerpen per exchange -----------------------------------------------------------
    out.append('<section class="{}" id="grid">'.format(worst))
    out.append('<h2>{} Onderwerpen per exchange'
               '<a class="top" href="#top">terug naar boven</a></h2>'.format(mark[worst]))
    out.append("<p>Een kolom die op een enkele exchange kleurt wijst naar die exchange; een kolom "
               "die overal kleurt wijst naar ons, of naar het meetinstrument.</p>")
    out.append('<div class="scroll"><table>')
    out.append("<thead><tr><th>Exchange</th>{}</tr></thead><tbody>".format(
        "".join("<th>{}</th>".format(escape(title)) for _, title in columns)))
    for row in rows:
        verdicts = {key: verdict for key, _, verdict in row["sections"]}
        cells = "".join('<td class="mark {0}" title="{1}">{2}</td>'.format(
            verdicts.get(key, check_exchange.UNKNOWN), escape(title),
            mark[verdicts.get(key, check_exchange.UNKNOWN)]) for key, title in columns)
        out.append('<tr><td><a href="{}">{}</a></td>{}</tr>'.format(
            escape(row["report"]), escape(row["label"]), cells))
    out.append("</tbody></table></div>")
    out.append("</section>")

    # ---- meetwaarden ------------------------------------------------------------------------
    out.append('<section class="{}" id="numbers">'.format(check_exchange.GOOD))
    out.append('<h2>{} Meetwaarden naast elkaar'
               '<a class="top" href="#top">terug naar boven</a></h2>'.format(
                   mark[check_exchange.GOOD]))
    out.append("<p>Vergelijk de kolommen, niet de rijen. Het aantal onderbrekingen hangt af van "
               "hoeveel symbolen een exchange in een abonnement stopt, dus een exchange op een "
               "symbool per abonnement telt er onvermijdelijk meer; de verbindingsverbrekingen "
               "ernaast zijn wel vergelijkbaar. Het geheugen is de helling over de staart van de "
               "run, want de opwarming van de caches zit in de eerste uren.</p>")
    out.append('<div class="scroll"><table>')
    out.append("<thead><tr><th>Exchange</th><th>Symbolen</th><th>Abonn.</th><th>Stil</th>"
               "<th>Ontbr. min</th><th>Onmogelijk</th><th>Onderbr.</th><th>Verbr.</th>"
               "<th>Verbr./uur</th><th>Niet hersteld</th><th>Foutregels</th><th>Signalen</th>"
               "<th>Geheugen MB/uur</th></tr></thead><tbody>")
    for row in rows:
        numbers = row["numbers"]
        out.append('<tr><td><a href="{}">{}</a></td>'
                   "<td>{}</td><td>{}</td><td>{}</td><td>{}</td><td>{}</td><td>{}</td>"
                   "<td>{}</td><td>{}</td><td>{}</td><td>{}</td><td>{}</td><td>{}</td></tr>".format(
                       escape(row["report"]), escape(row["label"]),
                       number_cell(numbers["symbols"]), number_cell(numbers["subscribed"]),
                       number_cell(numbers["silent"]), number_cell(numbers["missing"]),
                       number_cell(numbers["impossible"]), number_cell(numbers["outages"]),
                       number_cell(numbers["drops"]), number_cell(numbers["dropsPerHour"], 2),
                       number_cell(numbers["never"]), number_cell(numbers["errors"]),
                       number_cell(numbers["signals"]),
                       number_cell(numbers["memory"], 1, signed=True)))
    out.append("</tbody></table></div>")
    out.append("</section>")

    # ---- drift ------------------------------------------------------------------------------
    compared = [(row, previous_night(output, row["label"], row["windowStart"])) for row in rows]
    nights = sorted({before["night"] for _, before in compared if before})
    out.append('<section class="{}" id="drift">'.format(check_exchange.GOOD))
    out.append('<h2>{} Vergelijking met de vorige nacht'
               '<a class="top" href="#top">terug naar boven</a></h2>'.format(
                   mark[check_exchange.GOOD]))
    if not nights:
        out.append("<p>Er staat nog geen eerdere nacht in <code>Older</code> om mee te "
                   "vergelijken. Vanaf de volgende run staat het verschil hier.</p>")
    else:
        out.append("<p>Vergeleken met {}. Een absoluut getal zegt weinig - dat een exchange twintig "
                   "verbindingsverbrekingen had is pas iets als het er gisteren twee waren.</p>"
                   .format(escape(", ".join(nights))))
        out.append('<div class="scroll"><table>')
        out.append("<thead><tr><th>Exchange</th><th>Oordeel</th><th>Abonnementen</th>"
                   "<th>Ontbrekende minuten</th><th>Verbindingsverbrekingen</th>"
                   "<th>Foutregels</th><th>Geheugen MB/uur</th></tr></thead><tbody>")
        for row, before in compared:
            if before is None:
                out.append('<tr><td><a href="{}">{}</a></td>'
                           '<td colspan="6">geen eerdere nacht op schijf</td></tr>'.format(
                               escape(row["report"]), escape(row["label"])))
                continue
            now, was = row["numbers"], before["numbers"]
            if row["verdict"] == before["verdict"]:
                verdict_cell = '<span class="badge {}">{}</span>'.format(
                    row["verdict"], text[row["verdict"]])
            else:
                verdict_cell = '{} &rarr; <span class="badge {}">{}</span>'.format(
                    text[before["verdict"]], row["verdict"], text[row["verdict"]])
            out.append('<tr><td><a href="{}">{}</a></td><td>{}</td>'
                       "<td>{}</td><td>{}</td><td>{}</td><td>{}</td><td>{}</td></tr>".format(
                           escape(row["report"]), escape(row["label"]), verdict_cell,
                           drift_cell(now["subscribed"], was["subscribed"], lower_is_better=False),
                           drift_cell(now["missing"], was["missing"]),
                           drift_cell(now["drops"], was["drops"]),
                           drift_cell(now["errors"], was["errors"]),
                           drift_cell(now["memory"], was["memory"], digits=1)))
        out.append("</tbody></table></div>")
        out.append("<p>Het aantal abonnementen mag stijgen - dat volgt de volumegrens en het "
                   "aanbod van de exchange - dus daar is meer niet slechter.</p>")
    out.append("</section>")

    out.extend(["</main>", "</div>", "</body>", "</html>"])

    path = output / "{}.html".format(OVERVIEW_NAME)
    path.write_text("\n".join(out), encoding="utf-8")
    return path


def main():
    parser = argparse.ArgumentParser(description="Check every scanner data folder under one base.")
    parser.add_argument("--base", required=True, help="Folder holding the data folders.")
    parser.add_argument("--memory", help="Folder with the *-memory.csv files (optional).")
    parser.add_argument("--out", required=True, help="Where the reports are written.")
    parser.add_argument("--format", choices=("html", "md"), default="html",
                        help="html (default) opens with a double click and carries the colours and "
                             "the table of contents; md is the plain text version.")
    parser.add_argument("--max-age-days", type=float, default=3.0,
                        help="Skip folders whose newest log is older than this (default 3). Use 0 "
                             "to check every folder found.")
    parser.add_argument("--deep", action="store_true",
                        help="Pass --deep to every check (whole candle history instead of the run).")
    arguments = parser.parse_args()

    base = Path(os.path.expandvars(arguments.base))
    output = Path(os.path.expandvars(arguments.out))
    memory = Path(os.path.expandvars(arguments.memory)) if arguments.memory else None
    output.mkdir(parents=True, exist_ok=True)

    folders = find_data_folders(base)
    if not folders:
        print("No scanner data folder found under {}".format(base))
        print("A data folder is one that holds CryptoScanBot.db.")
        return 1

    # A folder whose log has not been touched for days did not run last night, and checking it only
    # buries the folders that did.
    skipped = []
    if arguments.max_age_days > 0:
        cutoff = time.time() - arguments.max_age_days * 86400
        fresh = []
        for folder in folders:
            stamp = last_activity(folder)
            if stamp is None or stamp < cutoff:
                skipped.append(folder)
            else:
                fresh.append(folder)
        folders = fresh

    archived = archive_previous_reports(output)
    if archived:
        print("Moved {} report(s) from an earlier run to {}".format(archived, output / "Older"))
        print("The folder now holds THIS run only, so nothing older can be mistaken for it.")
        print()

    print("Found {} data folder(s) under {}".format(len(folders) + len(skipped), base))
    if skipped:
        print("Skipping {} without a log from the last {:.0f} day(s): {}".format(
            len(skipped), arguments.max_age_days,
            ", ".join(sample_name(folder) for folder in skipped)))
    if not folders:
        print("Nothing recent to check. Use --max-age-days 0 to check everything anyway.")
        return 1
    print()

    script = Path(__file__).with_name("check_exchange.py")

    def check_one(folder):
        label = sample_name(folder)
        report_path = output / "{}-report.{}".format(label, arguments.format)
        facts_path = output / "{}-facts.json".format(label)

        command = [sys.executable, str(script), "--folder", str(folder),
                   "--out", str(report_path), "--json", str(facts_path)]
        if arguments.deep:
            command.append("--deep")

        csv_path = None
        if memory:
            candidate = memory / "{}-memory.csv".format(label)
            if candidate.is_file():
                csv_path = candidate
                command.extend(["--memory-csv", str(candidate)])

        try:
            completed = subprocess.run(command, capture_output=True, text=True)
        except Exception as error:
            print("   {} could not run: {}".format(label, error), flush=True)
            return (label, "unknown", "could not run", report_path)

        if completed.returncode not in (0, 1, 2):
            print("   {} failed: {}".format(label, (completed.stderr or "").strip()[:200]),
                  flush=True)
            return (label, "unknown", "check failed", report_path)

        verdict, summary = "unknown", ""
        if facts_path.is_file():
            try:
                facts = json.loads(facts_path.read_text(encoding="utf-8"))
                verdict = facts.get("overall", "unknown")
                bad = [section["title"] for section in facts.get("sections", [])
                       if section.get("verdict") in ("bad", "attention")]
                summary = ", ".join(bad) if bad else "nothing to report"
            except Exception:
                pass
        if not csv_path:
            summary = (summary + "; no memory samples").lstrip("; ")
        print("   {} done ({})".format(label, verdict), flush=True)
        return (label, verdict, summary, report_path)

    started = time.time()
    print("Checking {} folder(s), {} at a time ...".format(len(folders), WORKERS))
    with ThreadPoolExecutor(max_workers=WORKERS) as pool:
        results = list(pool.map(check_one, folders))

    results.sort(key=lambda row: (VERDICT_ORDER.get(row[1], 9), row[0]))

    print()
    print("=" * 78)
    print("{:<24} {:<10} {}".format("EXCHANGE", "VERDICT", "SUBJECTS NEEDING A LOOK"))
    print("=" * 78)
    for label, verdict, summary, _ in results:
        print("{:<24} {:<10} {}".format(label, verdict, summary))
    print("=" * 78)
    print()

    # The overview reads the facts files back rather than the results above: they hold the numbers
    # and the per-subject verdicts, and reading them keeps this page in step with what was actually
    # written to disk. A report that failed to produce a facts file simply stays out of it.
    overview_rows = []
    for label, _, _, _ in results:
        facts_path = output / "{}-facts.json".format(label)
        if not facts_path.is_file():
            continue
        try:
            overview_rows.append(overview_row(facts_path))
        except Exception as error:
            print("   overview skipped {}: {}".format(label, error))
    overview_path = write_overview(output, overview_rows, base,
                                   [sample_name(folder) for folder in skipped],
                                   arguments.max_age_days)

    print("Reports written to: {}".format(output))
    if overview_path:
        print("Start here: {}".format(overview_path.name))
    print("Worst first, so start at the top. Took {:.0f} seconds.".format(time.time() - started))
    return 0


if __name__ == "__main__":
    sys.exit(main())
