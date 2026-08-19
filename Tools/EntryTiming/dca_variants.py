"""
Compare DCA settings on one and the same set of signals.

The DCA does two separate things and they are easy to confuse:
  1. it buys more when price moves against the position (tripling its size with Factor 200)
  2. it pushes the stop-loss out, because the stop is placed beyond the furthest DCA order

A variant list separates the two: run the same signals with and without a ladder, and with the
stop-loss held at the same distance in both cases. Whatever difference is left then belongs to the
buying, not to the stop.

The candles and indicators are computed once and reused for every variant, so this costs about the
same as a single account.py run.

Usage:
    python dca_variants.py --candles "<db>" --capital 500
"""

import argparse
import sys
import time

import numpy as np
import pandas as pd

import account
import candledb
import measure_entry_timing as met

# (label, dca levels, stop percentage beyond the furthest dca)
VARIANTS = [
    ("A  huidig: dca 5% factor 200",      [(5.0, 200.0)], 2.5),
    ("B  geen dca, stop even ver (7,5%)", [],             7.5),
    ("C  geen dca, stop op 2,5%",         [],             2.5),
    ("D  dca 5% factor 100",              [(5.0, 100.0)], 2.5),
    ("E  dca 2,5% factor 200",            [(2.5, 200.0)], 2.5),
    ("F  dca 5%+10%, factor 200/400",     [(5.0, 200.0), (10.0, 400.0)], 2.5),
]


def terugval(curve):
    """Largest drop from a previous high in the banked capital."""
    if not len(curve):
        return ""
    top = curve["equity"].cummax()
    return f"{100 * ((curve['equity'] - top) / top).min():.1f}%"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--candles", required=True)
    parser.add_argument("--interval", default="15m")
    parser.add_argument("--capital", type=float, default=500.0)
    parser.add_argument("--slots-long", type=int, default=15)
    parser.add_argument("--slots-short", type=int, default=15)
    parser.add_argument("--minimum-order", type=float, default=5.0)
    parser.add_argument("--window", type=int, default=9)
    parser.add_argument("--target", type=float, default=1.8)
    parser.add_argument("--fee", type=float, default=0.1)
    parser.add_argument("--rule", default="now", choices=["now", "stochastic"])
    args = parser.parse_args()

    interval_id = candledb.INTERVAL_IDS[args.interval]
    interval_minutes = candledb.INTERVAL_MINUTES[args.interval]
    connection = candledb.open_readonly(args.candles)

    started = time.time()
    prepared = []
    symbols = candledb.list_symbols(connection, interval_id, met.WARMUP + 100)
    for number, (symbol_id, name, count) in enumerate(symbols, 1):
        frame = candledb.load_candles(connection, symbol_id, interval_id)
        if len(frame) < met.WARMUP + 100:
            continue
        prepared.append((name, met.compute(frame), candledb.gap_mask(frame, interval_minutes)))
        print(f"  [{number}/{len(symbols)}] {name}", flush=True)
    print(f"indicatoren klaar in {time.time()-started:.0f}s\n")

    rows = []
    for label, dca_levels, stop_pct in VARIANTS:
        positions = []
        for name, data, contiguous in prepared:
            for side in ("short", "long"):
                positions.extend(account.find_positions(
                    name, data, contiguous, side, args.rule, args.window, dca_levels,
                    stop_pct, args.target, args.fee, 5, interval_minutes))
        frame = pd.DataFrame(positions)
        ladder = 1.0 + sum(f / 100.0 for _, f in dca_levels)
        result = account.run_account(frame, args.capital, args.slots_long, args.slots_short,
                                     args.minimum_order, ladder)
        deepest = max((p for p, _ in dca_levels), default=0.0)
        wins = frame[frame["outcome"] == "target"]
        losses = frame[frame["outcome"] == "stop"]
        rows.append({
            "variant": label,
            "stop op": f"{deepest + stop_pct:.1f}%",
            "geopend": result["taken"],
            "eind": round(result["equity"], 2),
            "rendement": f"{100*(result['equity']/args.capital-1):+.1f}%",
            "terugval": terugval(result["curve"]),
            "winst/verlies": f"{len(wins)}/{len(losses)}",
            "eenh. winst": round(wins["units"].mean(), 2) if len(wins) else np.nan,
            "eenh. verlies": round(losses["units"].mean(), 2) if len(losses) else np.nan,
            "% per winst": round(wins["net_pct"].mean(), 2) if len(wins) else np.nan,
            "% per verlies": round(losses["net_pct"].mean(), 2) if len(losses) else np.nan,
        })
        print(f"{label:<34} -> {result['equity']:8.2f}", flush=True)

    print()
    table = pd.DataFrame(rows)
    with pd.option_context("display.width", 250, "display.max_columns", 30):
        print(table.to_string(index=False))


if __name__ == "__main__":
    sys.exit(main())
