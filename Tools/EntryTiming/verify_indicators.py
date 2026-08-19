"""
Verify that the python indicators in indicators.py reproduce what the C# scanner computed.

The scanner stores a snapshot of its indicator values on every signal it creates (Signal table of
an emulator session database). This script recomputes those same values from the raw candles and
compares them, signal by signal, so any later measurement can be trusted to describe the same
numbers the trader actually sees.

Usage:
    python verify_indicators.py --session "<path>\\CryptoScanBot.db" \
                                --candles "<path>\\Binance Futures.db" \
                                --interval 15m --limit 400
"""

import argparse
import datetime
import sys

import numpy as np
import pandas as pd

import candledb
import indicators

FIELDS = [
    # (column in Signal, label, tolerance in percent of the scanner value)
    ("Rsi", "rsi", 0.5),
    ("StochOscillator", "stoch %K", 1.0),
    ("StochSignal", "stoch %D", 1.0),
    ("MacdValue", "macd line", 2.0),
    ("MacdSignal", "macd signal", 2.0),
    ("MacdHistogram", "macd histogram", 5.0),
    ("Psar", "parabolic sar", 0.5),
    ("BollingerBandsPercentage", "bollinger width %", 1.0),
]


def compute_frame(frame):
    high, low, close, volume = (frame["high"].to_numpy(), frame["low"].to_numpy(),
                                frame["close"].to_numpy(), frame["volume"].to_numpy())
    out = frame.copy()
    out["rsi"] = indicators.rsi(close, 14)
    out["stoch_k"], out["stoch_d"] = indicators.stochastic(high, low, close, 14, 3, 3)
    out["macd"], out["macd_signal"], out["macd_hist"] = indicators.macd(close, 12, 26, 9)
    _, out["bb_upper"], out["bb_lower"], out["bb_width"], out["bb_pct"] = indicators.bollinger(close, 20, 2.0)
    out["psar"], out["psar_rising"] = indicators.parabolic_sar(high, low, 0.02, 0.2)
    out["vbs_basis"], out["vbs_upper"], out["vbs_lower"], _ = indicators.vbs_bands(high, low, close, volume, 50, 2.5)
    out["acs"] = indicators.acs_percentage(high, low, close, 50, 2.17)
    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--session", required=True, help="emulator session CryptoScanBot.db")
    parser.add_argument("--candles", required=True, help="candle database (<Exchange> <Type>.db)")
    parser.add_argument("--interval", default="15m")
    parser.add_argument("--strategy", default="vbs")
    parser.add_argument("--limit", type=int, default=300, help="signals to check")
    args = parser.parse_args()

    interval_id = candledb.INTERVAL_IDS[args.interval]
    session = candledb.open_readonly(args.session)
    candles = candledb.open_readonly(args.candles)

    tick_size = {name: float(tick) for name, tick in
                 session.execute("select Name, PriceTickSize from Symbol").fetchall() if tick}
    candle_symbol_id = {name: sid for sid, name, _ in candledb.list_symbols(candles, interval_id, 1)}

    rows = session.execute(
        "select s.Name, sg.Side, sg.OpenDate, sg.SignalPrice, " +
        ", ".join("sg." + f[0] for f in FIELDS) +
        " from Signal sg join Symbol s on s.Id = sg.SymbolId "
        "where sg.Strategy = ? and sg.IntervalId = ? order by sg.Id desc limit ?",
        (args.strategy, interval_id, args.limit),
    ).fetchall()
    print(f"{len(rows)} signals from the session database")

    cache = {}
    differences = {label: [] for _, label, _ in FIELDS}
    differences["signal price"] = []
    missing = 0

    for row in rows:
        name, side, open_date, signal_price = row[0], row[1], row[2], float(row[3])
        if name not in candle_symbol_id or name not in tick_size:
            missing += 1
            continue
        if name not in cache:
            frame = candledb.load_candles(candles, candle_symbol_id[name], interval_id)
            cache[name] = compute_frame(frame).set_index("opentime")
        computed = cache[name]

        moment = datetime.datetime.strptime(open_date, "%Y-%m-%d %H:%M:%S")
        key = candledb.datetime_to_minutes(moment)
        if key not in computed.index:
            missing += 1
            continue
        mine = computed.loc[key]
        tick = tick_size[name]

        for column, label, _ in FIELDS:
            stored = row[4 + [f[0] for f in FIELDS].index(column)]
            if stored is None:
                continue
            stored = float(stored)
            value = {"rsi": mine["rsi"], "stoch %K": mine["stoch_k"], "stoch %D": mine["stoch_d"],
                     "macd line": mine["macd"] * tick, "macd signal": mine["macd_signal"] * tick,
                     "macd histogram": mine["macd_hist"] * tick, "parabolic sar": mine["psar"] * tick,
                     "bollinger width %": mine["bb_width"]}[label]
            if value is None or np.isnan(value):
                continue
            scale = max(abs(stored), 1e-12)
            differences[label].append(abs(value - stored) / scale * 100.0)

        # The signal price is the band itself (or the close when the body broke through)
        band = mine["vbs_upper"] if side == 1 else mine["vbs_lower"]
        if not np.isnan(band):
            band_price = band * tick
            expected = max(mine["close"] * tick, band_price) if side == 1 else min(mine["close"] * tick, band_price)
            differences["signal price"].append(abs(expected - signal_price) / max(signal_price, 1e-12) * 100.0)

    print(f"skipped {missing} signals (symbol or candle not present in this candle database)\n")
    print(f"{'value':<20}{'n':>7}{'median diff %':>16}{'p90 diff %':>13}{'max diff %':>13}")
    for label in list(differences):
        values = np.array(differences[label], dtype=float)
        values = values[~np.isnan(values)]
        if not len(values):
            print(f"{label:<20}{0:>7}")
            continue
        print(f"{label:<20}{len(values):>7}{np.median(values):>16.4f}"
              f"{np.percentile(values, 90):>13.4f}{values.max():>13.4f}")


if __name__ == "__main__":
    sys.exit(main())
