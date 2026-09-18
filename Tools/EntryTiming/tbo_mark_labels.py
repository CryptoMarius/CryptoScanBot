"""
The white breakout dots: which days the reference indicator marks, and which ones our own rule
draws on top of that.

Our rule (TboChartOverlay.FindConfirmations) is deliberately wider than the reference - it finds
every dot we know of and about three times as many besides. Narrowing it is a labelling problem
before it is a thinking problem: with a handful of confirmed days any condition can be made to fit,
and none of them can be trusted. This script is the bookkeeping for that.

    candles   the daily stretches embedded in TboBreakoutOnRealCandlesTests, read straight from
              that file so the two can never drift apart
    labels    tbo-mark-labels.json next to this script: per date "dot", "none" or absent
    output    every mark our rule draws, with the properties a missing condition could live in,
              and what is known about that day

A day is only "none" when the reference chart was SEEN not to mark it. An unlabelled day is
unknown, never a negative - counting it as one is how a rule gets fitted to nothing.

Usage:
    python tbo_mark_labels.py
    python tbo_mark_labels.py --window Autumn2024
"""

import argparse
import json
import re
from pathlib import Path

import numpy as np
import pandas as pd

import measure_tbo_exits as tbo

TEST_FILE = Path(__file__).resolve().parents[2] / "CryptoScanner.CoreTests" / "Signal" / "TboBreakoutOnRealCandlesTests.cs"
LABEL_FILE = Path(__file__).resolve().parent / "tbo-mark-labels.json"
WINDOW_NAMES = ["Autumn2020", "Autumn2024", "Spring2025", "Summer2026"]


def read_windows(path=TEST_FILE):
    """The embedded daily stretches: {name: DataFrame with date/high/low/close}."""
    source = path.read_text(encoding="utf-8-sig")
    windows = {}
    for name in WINDOW_NAMES:
        block = re.search(r'private const string ' + name + r' = """(.*?)""";', source, re.S)
        if block is None:
            continue
        rows = []
        for line in block.group(1).strip().splitlines():
            parts = line.split()
            if len(parts) == 4:
                rows.append({"date": parts[0], "high": float(parts[1]),
                             "low": float(parts[2]), "close": float(parts[3])})
        frame = pd.DataFrame(rows)
        # The rest of the tooling expects a full candle; the rule reads high, low and close only.
        frame["open"] = frame["close"]
        frame["volume"] = 0.0
        frame["opentime"] = range(len(frame))
        windows[name] = frame
    return windows


def read_labels(path=LABEL_FILE):
    if not path.exists():
        return {}
    return json.loads(path.read_text(encoding="utf-8"))


def describe(frame, labels):
    """Every mark our rule draws in this stretch, with the properties to look for a rule in."""
    close = frame["close"].to_numpy(dtype=float)
    high = frame["high"].to_numpy(dtype=float)
    low = frame["low"].to_numpy(dtype=float)

    fast = tbo.ema(close, tbo.FAST_EMA)
    second = tbo.ema(close, tbo.SECOND_EMA)
    pivot_high, pivot_low = tbo.pivot_levels(high, low, tbo.PIVOT_LEFT, tbo.PIVOT_RIGHT)
    longs, shorts = tbo.find_signals(frame, "mark")
    long_set = set(longs)

    # Where the cloud last turned, and how many marks it has already carried since then.
    ready = ~(np.isnan(fast) | np.isnan(second))
    pointing_up = ready & (fast > second)
    turned_at, previous, turn_of = -1, None, {}
    for i in range(len(close)):
        if not ready[i]:
            continue
        up = bool(pointing_up[i])
        if previous is None:
            previous = up
        elif up != previous:
            turned_at, previous = i, up
        turn_of[i] = turned_at

    ranges = high - low
    counter, rows = {}, []
    for i in sorted(list(longs) + list(shorts)):
        turn = turn_of[i]
        counter[turn] = counter.get(turn, 0) + 1
        up = i in long_set
        level = pivot_high[i] if up else pivot_low[i]
        average_range = np.nanmean(ranges[max(0, i - 14):i]) if i > 0 else np.nan
        date = frame["date"][i]
        rows.append({
            "date": date,
            "side": "long" if up else "short",
            # How far through the level the wick and the close went, as a percentage of the level.
            "wick": 100 * (high[i] - level) / level if up else 100 * (level - low[i]) / level,
            "close": 100 * (close[i] - level) / level if up else 100 * (level - close[i]) / level,
            "after_turn": i - turn,
            "nth_in_leg": counter[turn],
            "from_ema": abs(100 * (close[i] - fast[i]) / close[i]),
            "size_vs_average": (high[i] - low[i]) / average_range if average_range else np.nan,
            "label": labels.get(date, "?"),
        })
    return pd.DataFrame(rows)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--window", choices=WINDOW_NAMES)
    arguments = parser.parse_args()

    labels = read_labels()
    windows = read_windows()
    total = {"dot": 0, "none": 0, "?": 0}

    for name, frame in windows.items():
        if arguments.window and name != arguments.window:
            continue
        table = describe(frame, labels)
        print(f"\n=== {name}: {frame['date'][0]} t/m {frame['date'][len(frame) - 1]}, "
              f"{len(frame)} candles, {len(table)} markeringen")
        print(table.to_string(index=False, float_format=lambda x: f"{x:6.2f}"))
        for key in total:
            total[key] += int((table["label"] == key).sum())

        known = {d for d, v in labels.items() if v == "dot"}
        inside = {d for d in known if frame["date"].iloc[0] <= d <= frame["date"].iloc[-1]}
        missed = sorted(inside - set(table["date"]))
        if missed:
            print(f"!! bekende stippen die de regel NIET tekent: {', '.join(missed)}")

    print(f"\nmarkeringen in totaal: {sum(total.values())}"
          f" - bevestigde stippen {total['dot']}, bevestigd geen stip {total['none']},"
          f" onbekend {total['?']}")
    if total["?"] > total["dot"] + total["none"]:
        print("De meeste dagen zijn nog niet gelabeld. Een voorwaarde zoeken op deze verhouding "
              "levert een regel op die op toeval past; eerst een venster compleet labelen.")


if __name__ == "__main__":
    main()
