"""Report over finished emulator runs, in absolute numbers.

Percentages alone hide whether a result is worth anything: "+0.19% return" and "+12 USDT over
seven months on 324 positions" are the same number, and only the second can be judged. So every
table leads with counts and money.

Two things the report has to answer before any comparison is allowed:
  - which settings did each run use, and
  - are those settings equal enough that the runs may be put side by side at all.

The settings come from the run's stored SettingsJson, never from its label - a label is free text
someone typed, and a mislabelled run silently poisons every conclusion drawn from it.

Usage:
    python emulator_report.py --db "<session>/CryptoScanBot.db" [--runs 98-163] [--last 20]
                              [--out report.html] [--json facts.json]

The extension of --out picks the format: .html gives the browsable report (with the stored
configuration per run), anything else gives markdown.

Exit code 0 = every run produced positions, 1 = at least one run produced no signals at all
(nearly always a configuration problem, not a strategy result), 2 = could not read the database.
"""

import argparse
import html
import io
import json
import sqlite3
import sys
from datetime import datetime

SIDE_LONG = 0
SIDE_SHORT = 1


# --------------------------------------------------------------------------------------------
# Reading
# --------------------------------------------------------------------------------------------

def open_database(path):
    """Read-only, so a running emulator is never disturbed."""
    uri = "file:{}?mode=ro".format(str(path).replace("?", "%3f").replace("#", "%23"))
    connection = sqlite3.connect(uri, uri=True)
    connection.row_factory = sqlite3.Row
    return connection


def parse_run_selection(connection, runs, last):
    available = [r[0] for r in connection.execute("select Id from EmulatorRun order by Id")]
    if runs:
        wanted = set()
        for part in runs.split(","):
            if "-" in part:
                low, high = part.split("-", 1)
                wanted.update(range(int(low), int(high) + 1))
            else:
                wanted.add(int(part))
        return [i for i in available if i in wanted]
    if last:
        return available[-last:]
    return available


def number(value, default=0.0):
    """Position/EmulatorRun columns are TEXT; empty and null both mean zero here."""
    try:
        return float(value)
    except (TypeError, ValueError):
        return default


def percentage_list(entries):
    return [entry.get("Percentage") for entry in (entries or [])]


def format_percentages(values):
    return " + ".join("%g%%" % v for v in values) if values else "geen"


def run_settings(row):
    """Everything that decides what a run tested, split into two groups.

    The 'frame' is what has to be identical before two runs may be compared - period, symbols,
    money, risk. The 'variables' are what a batch is deliberately varying. Keeping them apart is
    the whole point: a difference in the frame means the comparison is invalid, a difference in
    the variables is the experiment.
    """
    frame = {}
    variables = {}
    try:
        settings = json.loads(row["SettingsJson"] or "{}")
    except ValueError:
        settings = {}
    try:
        config = json.loads(row["ConfigJson"] or "{}")
    except ValueError:
        config = {}

    signal = settings.get("Signal", {})
    trading = settings.get("Trading", {})

    frame["period"] = "{} t/m {}".format((row["FromDate"] or "")[:10], (row["ToDate"] or "")[:10])
    symbols = config.get("Symbols") or []
    frame["symbols"] = len(symbols)
    frame["symbol_key"] = ",".join(sorted(symbols))
    frame["base_interval"] = config.get("BaseInterval") or "?"
    frame["exchange"] = config.get("ExchangeName") or "?"

    quotes = settings.get("QuoteCoins", {})
    frame["entry_amount"] = None
    for name in ("USDT", "USDC"):
        if name in quotes and quotes[name].get("EntryAmount"):
            frame["entry_amount"] = quotes[name]["EntryAmount"]
            break

    frame["take_profit"] = percentage_list(trading.get("TpList"))
    frame["dca"] = percentage_list(trading.get("DcaList"))
    frame["stop_loss"] = trading.get("StopLossPercentage")
    frame["stop_loss_limit"] = trading.get("StopLossLimitPercentage")
    frame["slots"] = "{}/{}".format(trading.get("SlotsMaximalLong"),
                                    trading.get("SlotsMaximalShort"))
    frame["leverage"] = trading.get("Leverage")
    frame["entry_remove_time"] = trading.get("EntryRemoveTime")

    # CryptoOrderType: 0 = market, 1 = limit.
    order_type = trading.get("EntryOrderType")
    variables["order_type"] = (None if order_type is None
                               else ("limiet" if order_type == 1 else "markt"))
    variables["band_range_index"] = (signal.get("AnalysisMinBandRangeIndex")
                                     if signal.get("AnalysisBandRangeIndexCheck") else None)

    long_strategies = signal.get("Long", {}).get("Strategy") or []
    short_strategies = signal.get("Short", {}).get("Strategy") or []
    variables["strategy"] = "/".join(sorted(set(long_strategies) | set(short_strategies))) or "?"
    variables["sides"] = ("beide" if long_strategies and short_strategies else
                          "long" if long_strategies else
                          "short" if short_strategies else "geen")
    variables["intervals"] = ",".join(signal.get("Long", {}).get("Interval")
                                      or signal.get("Short", {}).get("Interval") or [])

    # Entry conditions that are switched on are what a "WACHT tot ..." run actually varies.
    conditions = trading.get("EntryConditions") or {}
    variables["wait_rules"] = sorted(key for key, value in conditions.items() if value is True)

    return frame, variables


def frame_key(frame):
    """Two runs may be compared when this is identical."""
    return json.dumps({key: frame[key] for key in sorted(frame)}, sort_keys=True)


def load_positions(connection, run_id):
    """Only positions that put money to work; a cancelled entry tells us nothing."""
    return connection.execute(
        "select Side, CreateTime, CloseTime, CAST(Profit AS REAL) as Profit, "
        "       CAST(Invested AS REAL) as Invested "
        "from Position "
        "where EmulatorRunId = ? and CloseTime is not null and CAST(Invested AS REAL) > 0",
        (run_id,)).fetchall()


# --------------------------------------------------------------------------------------------
# Measuring
# --------------------------------------------------------------------------------------------

def peak_exposure(positions):
    """Most money, and most positions, open at the same moment.

    Not in the database: the paper Asset balance carries over between runs. Walking the open and
    close times is the only way to get it, and it is the number that says whether a real account
    could have run this at all.
    """
    events = []
    for position in positions:
        events.append((position["CreateTime"], +position["Invested"], +1))
        events.append((position["CloseTime"], -position["Invested"], -1))
    events.sort()
    money = 0.0
    count = 0
    peak_money = 0.0
    peak_count = 0
    for _, delta_money, delta_count in events:
        money += delta_money
        count += delta_count
        peak_money = max(peak_money, money)
        peak_count = max(peak_count, count)
    return peak_money, peak_count


def days_between(from_date, to_date):
    try:
        start = datetime.strptime(from_date, "%Y-%m-%d").date()
        end = datetime.strptime(to_date, "%Y-%m-%d").date()
        return max((end - start).days, 1)
    except (TypeError, ValueError):
        return 1


def measure_run(connection, row):
    positions = load_positions(connection, row["Id"])
    frame, variables = run_settings(row)
    profit = number(row["Profit"])
    won = row["PositionsWon"] or 0
    lost = row["PositionsLost"] or 0
    timeout = row["PositionsTimeout"] or 0
    closed = won + lost + timeout
    days = days_between((row["FromDate"] or "")[:10], (row["ToDate"] or "")[:10])
    peak_money, peak_count = peak_exposure(positions)

    measured = {
        "id": row["Id"],
        "label": row["Label"] or "",
        "started": (row["StartedAt"] or "")[:16],
        "git_sha": (row["GitSha"] or "")[:8],
        "days": days,
        "signals": row["SignalCount"] or 0,
        "positions": row["PositionCount"] or 0,
        "closed": closed,
        "won": won,
        "lost": lost,
        "timeout": timeout,
        "cancelled": row["PositionsCancelled"] or 0,
        "open": row["PositionsOpen"] or 0,
        "per_day": closed / days,
        "staked": number(row["Invested"]),
        "profit": profit,
        "peak_capital": peak_money,
        "peak_positions": peak_count,
        "frame": frame,
        "variables": variables,
    }
    measured["end_capital"] = peak_money + profit
    measured["return_on_peak"] = 100 * profit / peak_money if peak_money else 0.0

    for side, name in ((SIDE_LONG, "long"), (SIDE_SHORT, "short")):
        subset = [p for p in positions if p["Side"] == side]
        measured[name] = {
            "closed": len(subset),
            "won": sum(1 for p in subset if p["Profit"] > 0),
            "profit": sum(p["Profit"] for p in subset),
        }
    return measured


def build_ladder(runs):
    """Aggregate per band range index threshold, within one entry order type.

    Runs with a wait rule or a single side are a different experiment, not a rung, so they stay
    out. When a strategy shows up twice under the same threshold the aggregate would be mixing
    runs that were not the same experiment, so that is flagged rather than quietly summed.
    """
    ladders = {}
    for run in runs:
        if run["closed"] == 0:
            continue
        if run["variables"]["wait_rules"] or run["variables"]["sides"] != "beide":
            continue
        order_type = run["variables"]["order_type"] or "?"
        threshold = run["variables"]["band_range_index"]
        ladders.setdefault(order_type, {}).setdefault(threshold, []).append(run)

    result = {}
    for order_type, rungs in ladders.items():
        rows = []
        for threshold in sorted(rungs, key=lambda t: (t is not None, t)):
            group = rungs[threshold]
            strategies = [r["variables"]["strategy"] for r in group]
            rows.append({
                "threshold": threshold,
                "runs": len(group),
                "run_ids": [r["id"] for r in group],
                "strategies": sorted(set(strategies)),
                "duplicates": sorted({s for s in strategies if strategies.count(s) > 1}),
                "positions": sum(r["positions"] for r in group),
                "closed": sum(r["closed"] for r in group),
                "won": sum(r["won"] for r in group),
                "lost": sum(r["lost"] for r in group),
                "open": sum(r["open"] for r in group),
                "staked": sum(r["staked"] for r in group),
                "profit": sum(r["profit"] for r in group),
                "per_day": sum(r["per_day"] for r in group) / len(group),
                "peak_capital": sum(r["peak_capital"] for r in group),
            })
        result[order_type] = rows
    return result


# --------------------------------------------------------------------------------------------
# Shared text
# --------------------------------------------------------------------------------------------

PEAK_EXPLANATION = (
    "Piekinleg is de hoogste som van alle tegelijk openstaande posities op enig moment in de run, "
    "de bijgekochte DCA-delen meegerekend. Het is dus het bedrag dat je minimaal beschikbaar "
    "moest hebben om deze run te draaien zonder ooit een signaal te moeten laten lopen. Met "
    "minder geld had je posities gemist en was de uitkomst een andere; het is een ondergrens, "
    "geen herrekening. Daarnaast staat het hoogste aantal posities dat tegelijk openstond: bij "
    "een inleg van 15 met een DCA erbovenop kost een positie tot 45, dus dat aantal maal 45 is "
    "waar je rekening mee moet houden.")

FRAME_LABELS = [
    ("exchange", "Beurs"),
    ("period", "Periode"),
    ("symbols", "Aantal munten"),
    ("base_interval", "Basisinterval"),
    ("entry_amount", "Inleg per instap"),
    ("take_profit", "Take profit"),
    ("stop_loss", "Stop loss"),
    ("stop_loss_limit", "Stop loss limiet"),
    ("dca", "DCA"),
    ("slots", "Slots long/short"),
    ("leverage", "Hefboom"),
    ("entry_remove_time", "Instap vervalt na (candles)"),
]


def frame_value(frame, key):
    value = frame.get(key)
    if key in ("take_profit", "dca"):
        return format_percentages(value)
    if key in ("stop_loss", "stop_loss_limit"):
        return "-" if value is None else "%g%%" % value
    return "-" if value is None else str(value)


def comparability(runs):
    """Group the runs by frame; one group means everything may be compared."""
    groups = {}
    for run in runs:
        groups.setdefault(frame_key(run["frame"]), []).append(run)
    return list(groups.values())


def threshold_text(threshold):
    return "geen" if threshold is None else "index >= %g" % threshold


# --------------------------------------------------------------------------------------------
# Markdown
# --------------------------------------------------------------------------------------------

def write_markdown(runs, ladders, groups):
    lines = ["# Emulatorrapport", ""]

    lines.append("## Instellingen en vergelijkbaarheid")
    lines.append("")
    if len(groups) == 1:
        lines.append("Alle {} runs draaiden op dezelfde periode, munten en risico-instellingen; "
                     "ze mogen onderling vergeleken worden.".format(len(runs)))
        lines.append("")
        for key, label in FRAME_LABELS:
            lines.append("- {}: {}".format(label, frame_value(groups[0][0]["frame"], key)))
    else:
        lines.append("**Let op: {} verschillende instellingengroepen.** Runs uit verschillende "
                     "groepen mogen niet naast elkaar gelegd worden.".format(len(groups)))
        for index, group in enumerate(groups, start=1):
            lines.append("")
            lines.append("**Groep {}** - runs {}".format(
                index, ", ".join(str(r["id"]) for r in group)))
            for key, label in FRAME_LABELS:
                lines.append("- {}: {}".format(label, frame_value(group[0]["frame"], key)))
    lines.append("")

    empty = [r for r in runs if r["signals"] == 0]
    if empty:
        lines.append("## Let op: {} run(s) zonder een enkel signaal".format(len(empty)))
        lines.append("")
        lines.append("Nul signalen is vrijwel nooit een uitspraak over de strategie maar een "
                     "instelling die niet aan stond. Controleer de intervallijst van de strategie.")
        lines.append("")
        for run in empty:
            lines.append("- #{} {}".format(run["id"], run["label"]))
        lines.append("")

    for order_type, rows in sorted(ladders.items()):
        lines.append("## De ladder - {}order".format(order_type))
        lines.append("")
        lines.append("| filter | runs | strategieen | run-ids | posities | gesloten | gewonnen "
                     "| verloren | per dag | omzet | winst |")
        lines.append("|---|---:|---|---|---:|---:|---:|---:|---:|---:|---:|")
        for row in rows:
            lines.append("| {} | {} | {} | {} | {} | {} | {} | {} | {:.2f} | {:.0f} | {:+.2f} |"
                         .format(threshold_text(row["threshold"]), row["runs"],
                                 ", ".join(row["strategies"]),
                                 ", ".join(str(i) for i in row["run_ids"]),
                                 row["positions"], row["closed"], row["won"], row["lost"],
                                 row["per_day"], row["staked"], row["profit"]))
        lines.append("")

    produced = [r for r in runs if r["closed"] > 0]
    if produced:
        lines.append("## Wat je moest inleggen en wat je overhield")
        lines.append("")
        lines.append(PEAK_EXPLANATION)
        lines.append("")
        lines.append("| id | run | filter | order | zijde | signalen | posities | gesloten "
                     "| per dag | piekinleg | max tegelijk | winst | eind |")
        lines.append("|---:|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|")
        for run in produced:
            variables = run["variables"]
            lines.append("| {} | {} | {} | {} | {} | {} | {} | {} | {:.2f} | {:.2f} | {} "
                         "| {:+.2f} | {:.2f} |".format(
                             run["id"], run["label"], threshold_text(variables["band_range_index"]),
                             variables["order_type"] or "-", variables["sides"],
                             run["signals"], run["positions"], run["closed"], run["per_day"],
                             run["peak_capital"], run["peak_positions"],
                             run["profit"], run["end_capital"]))
        lines.append("")

    return "\n".join(lines)


# --------------------------------------------------------------------------------------------
# Html
# --------------------------------------------------------------------------------------------

STYLE = """
:root { color-scheme: light dark; }
body { font-family: "Segoe UI", system-ui, sans-serif; margin: 0 auto; padding: 24px;
       max-width: 1600px; line-height: 1.5; }
h1 { margin-bottom: 4px; }
h2 { margin-top: 34px; border-bottom: 1px solid #8884; padding-bottom: 4px; }
h3 { margin-bottom: 4px; }
p.note { color: #777; max-width: 62em; }
table { border-collapse: collapse; width: 100%; margin: 12px 0; font-size: 14px; }
th, td { border-bottom: 1px solid #8883; padding: 5px 9px; text-align: right;
         white-space: nowrap; font-variant-numeric: tabular-nums; }
th { font-weight: 600; background: #8881; position: sticky; top: 0; }
th.left, td.left { text-align: left; white-space: normal; font-variant-numeric: normal; }
tr:hover td { background: #8881; }
.pos { color: #1a7f37; } .neg { color: #b3261e; }
.warn { background: #ffb02033; border-left: 4px solid #ffb020; padding: 10px 14px; margin: 14px 0; }
dl.frame { display: grid; grid-template-columns: max-content 1fr; gap: 2px 18px; margin: 8px 0; }
dl.frame dt { color: #777; } dl.frame dd { margin: 0; font-variant-numeric: tabular-nums; }
details { margin: 6px 0; } summary { cursor: pointer; }
pre { background: #8881; padding: 10px; overflow-x: auto; font-size: 12px; border-radius: 4px; }
.scroll { overflow-x: auto; }
"""


def esc(value):
    return html.escape(str(value))


def money_cell(value):
    css = "pos" if value > 0 else ("neg" if value < 0 else "")
    return '<td class="{}">{:+.2f}</td>'.format(css, value)


def frame_block(frame):
    parts = ["<dl class='frame'>"]
    for key, label in FRAME_LABELS:
        parts.append("<dt>{}</dt><dd>{}</dd>".format(esc(label), esc(frame_value(frame, key))))
    parts.append("</dl>")
    return "".join(parts)


def write_html(runs, ladders, groups, database_path):
    out = ["<!doctype html><html lang='nl'><head><meta charset='utf-8'>",
           "<title>Emulatorrapport</title><style>{}</style></head><body>".format(STYLE),
           "<h1>Emulatorrapport</h1>",
           "<p class='note'>Bron: <code>{}</code> &mdash; runs {} t/m {}, gemaakt op {}.</p>".format(
               esc(database_path), runs[0]["id"], runs[-1]["id"],
               datetime.now().strftime("%Y-%m-%d %H:%M"))]

    out.append("<h2>Instellingen en vergelijkbaarheid</h2>")
    if len(groups) == 1:
        out.append("<p class='note'>Alle {} runs draaiden op dezelfde periode, dezelfde munten en "
                   "dezelfde risico-instellingen, dus ze mogen onderling vergeleken worden. Wat "
                   "per run wel verschilt staat in de kolommen filter, order en zijde.</p>".format(
                       len(runs)))
        out.append(frame_block(groups[0][0]["frame"]))
    else:
        out.append("<div class='warn'><strong>{} verschillende instellingengroepen.</strong> "
                   "Runs uit verschillende groepen mogen niet naast elkaar gelegd worden: de "
                   "periode, de munten of het risico verschilt.</div>".format(len(groups)))
        for index, group in enumerate(groups, start=1):
            out.append("<h3>Groep {} &mdash; runs {}</h3>".format(
                index, esc(", ".join(str(r["id"]) for r in group))))
            out.append(frame_block(group[0]["frame"]))

    empty = [r for r in runs if r["signals"] == 0]
    if empty:
        out.append("<div class='warn'><strong>{} run(s) zonder een enkel signaal.</strong> "
                   "Nul signalen is vrijwel nooit een uitspraak over de strategie maar een "
                   "instelling die niet aan stond &mdash; controleer de intervallijst. Runs: "
                   "{}.</div>".format(len(empty), esc(", ".join(str(r["id"]) for r in empty))))

    for order_type, rows in sorted(ladders.items()):
        out.append("<h2>De ladder &mdash; {}order</h2>".format(esc(order_type)))
        out.append("<p class='note'>Per drempelwaarde van de band range index, alle strategieen "
                   "bij elkaar opgeteld. Alleen runs met beide zijden aan en zonder wachtregel; "
                   "die laatste zijn een ander experiment.</p>")
        if any(row["duplicates"] for row in rows):
            out.append("<div class='warn'>Een strategie komt binnen dezelfde drempelwaarde meer "
                       "dan eens voor. Die optelling mengt runs die niet hetzelfde experiment "
                       "waren; lees hem niet als een ladder.</div>")
        out.append("<div class='scroll'><table><thead><tr>"
                   "<th class='left'>filter</th><th>runs</th><th class='left'>strategieen</th>"
                   "<th class='left'>run-ids</th><th>posities</th><th>gesloten</th>"
                   "<th>gewonnen</th><th>verloren</th><th>open</th><th>per dag</th>"
                   "<th>omzet</th><th>piekinleg</th><th>winst</th></tr></thead><tbody>")
        for row in rows:
            out.append("<tr><td class='left'>{}</td><td>{}</td><td class='left'>{}</td>"
                       "<td class='left'>{}</td><td>{}</td><td>{}</td><td>{}</td><td>{}</td>"
                       "<td>{}</td><td>{:.2f}</td><td>{:.0f}</td><td>{:.2f}</td>{}</tr>".format(
                           esc(threshold_text(row["threshold"])), row["runs"],
                           esc(", ".join(row["strategies"])),
                           esc(", ".join(str(i) for i in row["run_ids"])),
                           row["positions"], row["closed"], row["won"], row["lost"], row["open"],
                           row["per_day"], row["staked"], row["peak_capital"],
                           money_cell(row["profit"])))
        out.append("</tbody></table></div>")

    produced = [r for r in runs if r["closed"] > 0]
    if produced:
        out.append("<h2>Wat je moest inleggen en wat je overhield</h2>")
        out.append("<p class='note'>{}</p>".format(esc(PEAK_EXPLANATION)))
        out.append("<div class='scroll'><table><thead><tr>"
                   "<th>id</th><th class='left'>run</th><th class='left'>filter</th>"
                   "<th class='left'>order</th><th class='left'>zijde</th>"
                   "<th>signalen</th><th>posities</th><th>gesloten</th><th>per dag</th>"
                   "<th>piekinleg</th><th>max tegelijk</th><th>winst</th><th>eind</th>"
                   "</tr></thead><tbody>")
        for run in produced:
            variables = run["variables"]
            out.append("<tr><td>{}</td><td class='left'>{}</td><td class='left'>{}</td>"
                       "<td class='left'>{}</td><td class='left'>{}</td><td>{}</td><td>{}</td>"
                       "<td>{}</td><td>{:.2f}</td><td>{:.2f}</td><td>{}</td>{}<td>{:.2f}</td>"
                       "</tr>".format(
                           run["id"], esc(run["label"]),
                           esc(threshold_text(variables["band_range_index"])),
                           esc(variables["order_type"] or "-"), esc(variables["sides"]),
                           run["signals"], run["positions"], run["closed"], run["per_day"],
                           run["peak_capital"], run["peak_positions"],
                           money_cell(run["profit"]), run["end_capital"]))
        out.append("</tbody></table></div>")

    out.append("<h2>Configuratie per run</h2>")
    out.append("<p class='note'>Zoals opgeslagen bij de run zelf, ter controle en als vertrekpunt "
               "voor een volgende variatie. De volledige instellingen staan in de kolommen "
               "SettingsJson en ConfigJson van de tabel EmulatorRun bij het genoemde run-id.</p>")
    for run in runs:
        variables = run["variables"]
        out.append("<details><summary>#{} &mdash; {} <em>({}, gestart {})</em></summary>".format(
            run["id"], esc(run["label"]), esc(variables["strategy"]), esc(run["started"])))
        payload = {
            "run": run["id"],
            "git": run["git_sha"],
            "strategie": variables["strategy"],
            "zijde": variables["sides"],
            "signaalinterval": variables["intervals"],
            "ordertype": variables["order_type"],
            "band_range_index": variables["band_range_index"],
            "wachtregels": variables["wait_rules"],
            "kader": {label: frame_value(run["frame"], key) for key, label in FRAME_LABELS},
        }
        out.append("<pre>{}</pre>".format(esc(json.dumps(payload, indent=1, ensure_ascii=False))))
        out.append("</details>")

    out.append("</body></html>")
    return "\n".join(out)


# --------------------------------------------------------------------------------------------

def main():
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

    parser = argparse.ArgumentParser(description="Rapport over emulatorruns in absolute getallen.")
    parser.add_argument("--db", required=True, help="De CryptoScanBot.db van de emulatorsessie.")
    parser.add_argument("--runs", help="Run-ids, bijvoorbeeld \"98-163\" of \"98,102,105\".")
    parser.add_argument("--last", type=int, help="Alleen de laatste N runs.")
    parser.add_argument("--out", help="Schrijf het rapport hierheen; .html geeft het "
                                      "doorklikbare rapport, anders markdown.")
    parser.add_argument("--json", help="Schrijf de machineleesbare feiten naar dit bestand.")
    arguments = parser.parse_args()

    try:
        connection = open_database(arguments.db)
        connection.execute("select 1 from EmulatorRun limit 1")
    except sqlite3.Error as error:
        print("Kan de emulatordatabase niet lezen: {}\n{}".format(arguments.db, error),
              file=sys.stderr)
        print("Verwacht bijvoorbeeld "
              "E:\\CryptoScanBot\\Data\\<Exchange>\\Emulator\\Session<N>\\CryptoScanBot.db",
              file=sys.stderr)
        return 2

    run_ids = parse_run_selection(connection, arguments.runs, arguments.last)
    if not run_ids:
        print("Geen runs gevonden in {}".format(arguments.db), file=sys.stderr)
        return 2

    placeholders = ",".join("?" for _ in run_ids)
    rows = connection.execute(
        "select * from EmulatorRun where Id in ({}) order by Id".format(placeholders),
        run_ids).fetchall()
    runs = [measure_run(connection, row) for row in rows]
    ladders = build_ladder(runs)
    groups = comparability(runs)

    if arguments.out and arguments.out.lower().endswith(".html"):
        report = write_html(runs, ladders, groups, arguments.db)
    else:
        report = write_markdown(runs, ladders, groups)

    if arguments.out:
        with io.open(arguments.out, "w", encoding="utf-8") as handle:
            handle.write(report)
        print("Rapport geschreven naar {}".format(arguments.out))
    else:
        print(report)

    if arguments.json:
        with io.open(arguments.json, "w", encoding="utf-8") as handle:
            json.dump(runs, handle, indent=1, ensure_ascii=False)
        print("Feiten geschreven naar {}".format(arguments.json))

    return 1 if any(r["signals"] == 0 for r in runs) else 0


if __name__ == "__main__":
    sys.exit(main())
