"""
Analyse of a set of emulator runs: which indicator values separate the winners from the losers.

Follows the recipe from earlier sessions: read the closed positions, split long from short (they
have opposite logic), then try a series of thresholds per indicator and report the win rate above
and below each one. Only splits that actually move the win rate are worth reporting.

All Position columns are TEXT, so every number needs a CAST.

Usage:
    python analyse_runs.py --db "<session>\\CryptoScanBot.db" --runs 70-97
"""

import argparse
import io
import re
import sqlite3
import sys

import numpy as np
import pandas as pd

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

COLUMNS = """
    p.EmulatorRunId, p.Side, p.Strategy,
    CAST(p.Profit AS REAL) AS Profit,
    CAST(p.Invested AS REAL) AS Invested,
    CAST(p.Percentage AS REAL) AS Percentage,
    CAST(p.TrendPercentagePrimary AS REAL) AS TrendPrimary,
    CAST(p.TrendPercentageSecondary AS REAL) AS TrendSecondary,
    CAST(p.StochOscillator AS REAL) AS StochK,
    CAST(p.StochSignal AS REAL) AS StochD,
    CAST(p.Rsi AS REAL) AS Rsi,
    CAST(p.BollingerBandsPercentage AS REAL) AS BbWidth,
    CAST(p.MacdHistogram AS REAL) AS MacdHist,
    CAST(p.BandRangeIndex AS REAL) AS BandIndex,
    CAST(p.BandRangeCount AS REAL) AS BandCount,
    p.PartCount, p.EventText
"""


def load(database, runs):
    connection = sqlite3.connect("file:" + database.replace("\\", "/") + "?mode=ro", uri=True)
    have = {c[1] for c in connection.execute("pragma table_info(Position)")}
    columns = COLUMNS
    for optional in ("BandRangeIndex", "BandRangeCount"):
        if optional not in have:
            columns = re.sub(rf"\s*CAST\(p\.{optional} AS REAL\) AS \w+,", "", columns)
    placeholders = ",".join("?" for _ in runs)
    frame = pd.read_sql_query(
        # Status 2 = Ready: the position actually traded and was closed. Status 3 (Timeout) and
        # 6 (Cancelled) never filled and carry no profit at all - counting those as losses is what
        # turns an 82% win rate into a 41% one.
        f"SELECT {columns} FROM Position p WHERE p.EmulatorRunId IN ({placeholders}) "
        f"AND p.Status = 2", connection, params=list(runs))
    frame["win"] = frame["Profit"] > 0
    # the VBS band margin sits in the event text, not in a column
    frame["band_pct"] = frame["EventText"].str.extract(r"band\s+([\d.]+)%").astype(float)
    return frame


def summary(frame, label):
    if not len(frame):
        return None
    wins = int(frame["win"].sum())
    return {
        "wat": label,
        "n": len(frame),
        "W": wins,
        "L": len(frame) - wins,
        "winst%": round(100 * wins / len(frame), 1),
        "totaal": round(frame["Profit"].sum(), 2),
        "per positie": round(frame["Profit"].mean(), 4),
    }


def splits(frame, column, thresholds, label):
    """Win rate and profit above and below each threshold - only where there is data on both sides."""
    rows = []
    values = frame[column]
    if values.notna().sum() < 30:
        return rows
    for threshold in thresholds:
        low, high = frame[values < threshold], frame[values >= threshold]
        if len(low) < 20 or len(high) < 20:
            continue
        rows.append({
            "indicator": label,
            "grens": threshold,
            "onder n": len(low), "onder winst%": round(100 * low["win"].mean(), 1),
            "onder per pos": round(low["Profit"].mean(), 4),
            "boven n": len(high), "boven winst%": round(100 * high["win"].mean(), 1),
            "boven per pos": round(high["Profit"].mean(), 4),
            "verschil": round(high["Profit"].mean() - low["Profit"].mean(), 4),
        })
    return rows


def analyse(frame, title):
    print(f"\n{'=' * 100}\n{title}\n{'-' * 100}")
    rows = [summary(frame, "alles")]
    for side, name in ((0, "long"), (1, "short")):
        rows.append(summary(frame[frame["Side"] == side], name))
    print(pd.DataFrame([r for r in rows if r]).to_string(index=False))

    for side, name in ((0, "long"), (1, "short")):
        part = frame[frame["Side"] == side]
        if len(part) < 60:
            continue
        found = []
        found += splits(part, "StochK", [15, 20, 25, 30, 50, 60, 70, 75, 80, 85, 90], "stoch %K")
        found += splits(part, "Rsi", [15, 20, 25, 30, 60, 65, 70, 75, 80], "rsi")
        found += splits(part, "TrendPrimary", [-80, -60, -40, -20, 0, 20, 40, 60], "trend primair")
        found += splits(part, "TrendSecondary", [-60, -40, -20, 0, 20, 40], "trend secundair")
        found += splits(part, "BbWidth", [2, 3, 4, 5, 7, 10], "bollinger breedte")
        found += splits(part, "band_pct", [2, 2.5, 3, 3.5, 4, 5], "vbs bandmarge")
        found += splits(part, "MacdHist", [0], "macd histogram")
        if "BandIndex" in part:
            found += splits(part, "BandIndex", [2, 3, 3.5, 4, 5, 6, 8], "band range index")
        if not found:
            continue
        table = pd.DataFrame(found)
        # only the splits that really move the needle
        table = table.reindex(table["verschil"].abs().sort_values(ascending=False).index).head(8)
        print(f"\n  sterkste splitsingen, {name} ({len(part)} posities):")
        print(table.to_string(index=False))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--db", required=True)
    parser.add_argument("--runs", required=True, help="e.g. 70-97 or 70,73,87")
    parser.add_argument("--per-run", action="store_true", help="also analyse each run separately")
    args = parser.parse_args()

    if "-" in args.runs and "," not in args.runs:
        first, last = args.runs.split("-")
        runs = list(range(int(first), int(last) + 1))
    else:
        runs = [int(v) for v in args.runs.split(",")]

    frame = load(args.db, runs)
    print(f"{len(frame)} gesloten posities uit {frame['EmulatorRunId'].nunique()} runs")

    for strategy in sorted(frame["Strategy"].dropna().unique()):
        analyse(frame[frame["Strategy"] == strategy], f"strategie {strategy} - alle runs samen")

    if args.per_run:
        for run in runs:
            part = frame[frame["EmulatorRunId"] == run]
            if len(part) >= 60:
                analyse(part, f"run {run}")


if __name__ == "__main__":
    sys.exit(main())
