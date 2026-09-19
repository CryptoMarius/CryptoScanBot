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
import datetime
import json
import re
import sqlite3
from pathlib import Path

import numpy as np
import pandas as pd

import measure_tbo_exits as tbo

TEST_FILE = Path(__file__).resolve().parents[2] / "CryptoScanner.CoreTests" / "Signal" / "TboBreakoutOnRealCandlesTests.cs"
LABEL_FILE = Path(__file__).resolve().parent / "tbo-mark-labels.json"
WINDOW_NAMES = ["Autumn2020", "Autumn2024", "Spring2025", "Summer2026"]

CANDLE_DB = r"E:\CryptoScanBot\Data\Binance\Perpetual\Binance Perpetual.db"
EPOCH = datetime.datetime(2010, 1, 4)
DAILY_INTERVAL_ID = 15


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


def read_symbol(symbol, database=CANDLE_DB):
    """The whole daily series of one symbol, as a frame the rule can read.

    The whole series, not just the labelled window: the cloud and the pivots need the candles
    before the window, and a rule fed a window that starts cold draws nothing for its first
    hundred and fifty days.
    """
    connection = sqlite3.connect("file:" + database.replace("\\", "/") + "?mode=ro", uri=True)
    found = connection.execute("select SymbolId from Symbol where Name=?", (symbol + ".PERP",)).fetchone()
    if found is None:
        connection.close()
        return None
    rows = []
    for open_time, ticks, open_price, high, low, close in connection.execute(
            "select OpenTime,Ticks,Open,High,Low,Close from Candle "
            "where SymbolId=? and IntervalId=? order by OpenTime",
            (found[0], DAILY_INTERVAL_ID)):
        scale = 10 ** (ticks & 15)
        rows.append({"date": str((EPOCH + datetime.timedelta(minutes=open_time)).date()),
                     "open": open_price / scale, "high": high / scale,
                     "low": low / scale, "close": close / scale, "volume": 0.0})
    connection.close()
    if len(rows) < 200:
        return None
    frame = pd.DataFrame(rows)
    frame["opentime"] = range(len(frame))
    return frame


def labelled_symbols(path=LABEL_FILE):
    """The symbols the label file holds a verdict for, with their labelled days."""
    stored = json.loads(path.read_text(encoding="utf-8"))
    out = {}
    for symbol, verdicts in stored.items():
        if symbol.startswith("_") or not isinstance(verdicts, dict):
            continue
        days = {date: value for date, value in verdicts.items() if not date.startswith("_")}
        if days:
            out[symbol] = days
    return out


def read_labels(path=LABEL_FILE, symbol="BTCUSDT"):
    """The verdicts for one symbol. The file is keyed by symbol, because a rule that only fits
    bitcoin is a rule fitted to one chart."""
    if not path.exists():
        return {}
    stored = json.loads(path.read_text(encoding="utf-8"))
    return {date: value for date, value in stored.get(symbol, {}).items() if not date.startswith("_")}


def describe(frame, labels):
    """Every mark our rule draws in this stretch, with the properties to look for a rule in."""
    close = frame["close"].to_numpy(dtype=float)
    high = frame["high"].to_numpy(dtype=float)
    low = frame["low"].to_numpy(dtype=float)

    fast = tbo.ema(close, tbo.FAST_EMA)
    second = tbo.ema(close, tbo.SECOND_EMA)
    medium = tbo.sma(close, tbo.MEDIUM_SMA)
    slow = tbo.sma(close, tbo.SLOW_SMA)
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
            "from_second": abs(100 * (close[i] - second[i]) / close[i]),
            "from_medium": abs(100 * (close[i] - medium[i]) / close[i]),
            "from_slow": abs(100 * (close[i] - slow[i]) / close[i]),
            "cloud_width": abs(100 * (fast[i] - slow[i]) / close[i]),
            # The same distance measured in average daily candles instead of percent. A coin that
            # moves four percent a day reaches ten percent from the line in a week; a quiet coin
            # never does, so the percentage alone would only ever fire on the lively coins.
            "ema_in_candles": (abs(close[i] - fast[i]) / average_range
                               if average_range else np.nan),
            "size_vs_average": (high[i] - low[i]) / average_range if average_range else np.nan,
            "label": labels.get(date, "?"),
        })
    return pd.DataFrame(rows)


def separation(table, column):
    """How well one property tells the confirmed dots from the confirmed false ones.

    Reported as the strictest cut that still keeps EVERY confirmed dot, because a cut that drops a
    real dot is not a candidate: our rule already finds his dots, it finds too much besides. Both
    directions are tried, since a condition can be a floor or a ceiling.
    """
    dots = table.loc[table["label"] == "dot", column].dropna()
    none = table.loc[table["label"] == "none", column].dropna()
    if dots.empty or none.empty:
        return None
    best = None
    for direction in ("at least", "at most"):
        cut = dots.min() if direction == "at least" else dots.max()
        survivors = (none >= cut).sum() if direction == "at least" else (none <= cut).sum()
        dropped = len(none) - survivors
        if best is None or dropped > best[2]:
            best = (direction, cut, dropped, survivors)
    return best


def compare(table):
    """Every property side by side: what the dots look like, what the false ones look like."""
    columns = ["wick", "close", "after_turn", "nth_in_leg", "from_ema", "from_second",
               "from_medium", "from_slow", "cloud_width", "ema_in_candles", "size_vs_average"]
    dots = table[table["label"] == "dot"]
    none = table[table["label"] == "none"]
    print("")
    print(f"=== {len(dots)} bevestigde stippen tegen {len(none)} bevestigd geen stip")
    print(f"{'eigenschap':17} {'stip: laag':>11} {'midden':>8} {'hoog':>8} | "
          f"{'geen: laag':>11} {'midden':>8} {'hoog':>8} | scheiding")
    for column in columns:
        a, b = dots[column].dropna(), none[column].dropna()
        if a.empty or b.empty:
            continue
        found = separation(table, column)
        verdict = "-"
        if found is not None:
            direction, cut, dropped, _ = found
            verdict = f"{direction} {cut:.2f} laat {dropped} van {len(b)} vallen"
        print(f"{column:17} {a.min():11.2f} {a.median():8.2f} {a.max():8.2f} | "
              f"{b.min():11.2f} {b.median():8.2f} {b.max():8.2f} | {verdict}")


def from_database():
    """Our marks on every symbol the label file knows, with the verdict beside each one."""
    tables = []
    for symbol, verdicts in labelled_symbols().items():
        frame = read_symbol(symbol)
        if frame is None:
            print(f"{symbol}: geen dagcandles in de database, overgeslagen")
            continue
        days = sorted(verdicts)
        table = describe(frame, verdicts)
        inside = table[(table["date"] >= days[0]) & (table["date"] <= days[-1])].copy()
        inside.insert(0, "symbol", symbol)
        known = {d for d, v in verdicts.items() if v == "dot"}
        missed = sorted(known - set(inside["date"]))
        print(f"{symbol:10} {days[0]} t/m {days[-1]}: {len(inside):3} markeringen van ons, "
              f"{len(known)} stippen van hem, {len(known & set(inside['date']))} raak"
              + (f", NIET getekend: {', '.join(missed)}" if missed else ""))
        tables.append(inside)
    if not tables:
        return
    total = pd.concat(tables, ignore_index=True)
    print()
    print(total.to_string(index=False, float_format=lambda x: f"{x:6.2f}"))
    compare(total)
    return total


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--window", choices=WINDOW_NAMES)
    parser.add_argument("--database", action="store_true",
                        help="score the labelled symbols from the candle database instead of the "
                             "stretches embedded in the test")
    arguments = parser.parse_args()

    if arguments.database:
        from_database()
        return

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
