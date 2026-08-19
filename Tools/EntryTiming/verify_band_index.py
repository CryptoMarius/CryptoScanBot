"""
Verify the python band range index against the values the scanner stored on its own signals.

The scanner writes BandRangeIndex and BandRangeCount on every signal it creates (Signal table).
This script recomputes both from the raw candles with band_index.py and compares them, so the
filter used in the measurements is known to be the same number the scanner sees.

Usage:
    python verify_band_index.py --session "<path>\\CryptoScanBot.db" --candles "<path>\\<Exchange> <Type>.db"
"""

import argparse
import datetime
import sys

import numpy as np

import band_index
import candledb
import indicators


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--session", required=True)
    parser.add_argument("--candles", required=True)
    args = parser.parse_args()

    session = candledb.open_readonly(args.session)
    candles = candledb.open_readonly(args.candles)

    rows = session.execute(
        "select s.Name, sg.IntervalId, sg.OpenDate, sg.BandRangeIndex, sg.BandRangeCount, "
        "sg.BollingerBandsPercentage, sg.Strategy "
        "from Signal sg join Symbol s on s.Id = sg.SymbolId "
        "where sg.BandRangeIndex is not null order by sg.Id desc").fetchall()
    print(f"{len(rows)} signalen met een opgeslagen band range index\n")

    cache = {}
    print(f"{'symbool':<12}{'interval':>9}{'moment':<18}"
          f"{'scanner idx':>13}{'python idx':>12}{'versch %':>10}"
          f"{'scan n':>8}{'py n':>7}")
    differences = []
    for name, interval_id, when, stored_index, stored_count, bb_width, strategy in rows:
        key = (name, interval_id)
        if key not in cache:
            symbols = {n: i for i, n, _ in candledb.list_symbols(candles, interval_id, 1)}
            if name not in symbols:
                print(f"{name:<12}{interval_id:>9}  geen candles in deze database")
                continue
            frame = candledb.load_candles(candles, symbols[name], interval_id)
            if len(frame) < 550:
                print(f"{name:<12}{interval_id:>9}  te weinig candles ({len(frame)})")
                continue
            high = frame["high"].to_numpy()
            low = frame["low"].to_numpy()
            close = frame["close"].to_numpy()
            basis, upper, lower, _, _ = indicators.bollinger(close, 20, 2.0)
            index, width, ratio, count = band_index.band_range_index(high, low, close, basis, upper, lower)
            cache[key] = (frame["opentime"].to_numpy(), index, width, ratio, count)
        opentime, index, width, ratio, count = cache[key]

        moment = candledb.datetime_to_minutes(
            datetime.datetime.strptime(when, "%Y-%m-%d %H:%M:%S"))
        where = np.flatnonzero(opentime == moment)
        if not len(where):
            print(f"{name:<12}{interval_id:>9}{when[:16]:<18}  candle niet gevonden")
            continue
        position = int(where[0])
        mine = index[position]
        stored = float(stored_index)
        if not np.isfinite(mine):
            print(f"{name:<12}{interval_id:>9}{when[:16]:<18}{stored:>13.4f}{'geen waarde':>12}")
            continue
        difference = abs(mine - stored) / max(abs(stored), 1e-12) * 100.0
        differences.append(difference)
        print(f"{name:<12}{interval_id:>9}{when[:16]:<18}{stored:>13.4f}{mine:>12.4f}"
              f"{difference:>10.2f}{stored_count:>8}{count[position]:>7}")

    if differences:
        values = np.array(differences)
        print(f"\nverschil in procent: mediaan {np.median(values):.3f}  "
              f"p90 {np.percentile(values, 90):.3f}  maximaal {values.max():.3f}")


if __name__ == "__main__":
    sys.exit(main())
