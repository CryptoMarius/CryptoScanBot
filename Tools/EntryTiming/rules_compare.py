"""
Compare every entry rule on one account, with the same settings.

Same signals, same DCA ladder, same stop-loss, same fee; only the moment the order is placed
differs. The candles and indicators are computed once and reused, so twelve rules cost about as
much as one account.py run.

Optionally adds a band range index filter, so the effect of "only trade symbols with room" can be
seen separately from the entry rule.

Usage:
    python rules_compare.py --candles "<db>" --capital 500 [--min-band-index 3.5]
"""

import argparse
import sys
import time

import numpy as np
import pandas as pd

import account
import candledb
import measure_entry_timing as met


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--candles", required=True)
    parser.add_argument("--interval", default="15m")
    parser.add_argument("--capital", type=float, default=500.0)
    parser.add_argument("--slots-long", type=int, default=15)
    parser.add_argument("--slots-short", type=int, default=15)
    parser.add_argument("--minimum-order", type=float, default=5.0)
    parser.add_argument("--window", type=int, default=9)
    parser.add_argument("--dca", default="5:200")
    parser.add_argument("--stop", type=float, default=2.5)
    parser.add_argument("--target", type=float, default=1.8)
    parser.add_argument("--fee", type=float, default=0.1)
    parser.add_argument("--min-band-index", type=float, default=0.0)
    parser.add_argument("--no-bb-inside", action="store_true")
    parser.add_argument("--entry-order", default="limit", choices=["limit", "market"])
    parser.add_argument("--settings", default="",
                        help="CryptoScanBot-settings.json, for the rsi/stoch/bollinger thresholds")
    parser.add_argument("--bb-width", type=float, default=None)
    parser.add_argument("--out", default="")
    args = parser.parse_args()

    dca_levels = [tuple(float(v) for v in part.split(":")) for part in args.dca.split(",") if part]
    ladder = 1.0 + sum(f / 100.0 for _, f in dca_levels)
    interval_id = candledb.INTERVAL_IDS[args.interval]
    interval_minutes = candledb.INTERVAL_MINUTES[args.interval]
    connection = candledb.open_readonly(args.candles)
    loaded_settings = met.load_settings(args.settings) if args.settings else "standaardwaarden"

    started = time.time()
    prepared = []
    symbols = candledb.list_symbols(connection, interval_id, met.WARMUP + 100)
    for number, (symbol_id, name, count) in enumerate(symbols, 1):
        frame = candledb.load_candles(connection, symbol_id, interval_id)
        if len(frame) < met.WARMUP + 100:
            continue
        prepared.append((name, met.compute(frame), candledb.gap_mask(frame, interval_minutes)))
    print(f"indicatoren voor {len(prepared)} munten klaar in {time.time()-started:.0f}s")
    print(f"instellingen: dca {args.dca} | stop {args.stop}% voorbij de verste dca | "
          f"doel {args.target}% | fee {args.fee}% per transactie | wachtvenster {args.window} candles")
    print(f"              bollinger breedte minimaal "
          f"{met.BB_WIDTH_MINIMUM if args.bb_width is None else args.bb_width}% | "
          f"bollinger binnen vbs-band: {'NEE' if args.no_bb_inside else 'ja'} | "
          f"band range index minimaal "
          f"{args.min_band_index if args.min_band_index > 0 else 'geen filter'}")
    print(f"              {args.slots_long} long / {args.slots_short} short slots | "
          f"startkapitaal {args.capital:.0f}")
    print(f"              instap: " + ("limietorder op de vbs-band (EntryOrderType = Limit)"
          if args.entry_order == "limit" else "marktorder op de close (altijd gevuld)"))
    print(f"              drempels: {loaded_settings}\n")

    rows = []
    for rule in account.ENTRY_RULES:
        positions = []
        for name, data, contiguous in prepared:
            for side in ("short", "long"):
                positions.extend(account.find_positions(
                    name, data, contiguous, side, rule, args.window, dca_levels, args.stop,
                    args.target, args.fee, 5, interval_minutes, args.min_band_index,
                    1.0, not args.no_bb_inside, args.bb_width, args.entry_order))
        frame = pd.DataFrame(positions)
        if not len(frame):
            print(f"{rule:<26} -> geen posities")
            continue
        result = account.run_account(frame, args.capital, args.slots_long, args.slots_short,
                                     args.minimum_order, ladder)
        curve = result["curve"]
        drawdown = ""
        if len(curve):
            top = curve["equity"].cummax()
            drawdown = f"{100*((curve['equity']-top)/top).min():.1f}%"
        opened = result["opened"]
        wins = int((opened["outcome"] == "target").sum())
        losses = int((opened["outcome"] == "stop").sum())
        rows.append({
            "instapregel": rule,
            "kansen": len(frame),
            "geopend": result["taken"],
            "winst/verlies": f"{wins}/{losses}",
            "eind": round(result["equity"], 2),
            "pf groei": f"{100*(result['equity']/args.capital-1):+.1f}%",
            "terugval": drawdown,
            "open eind": result["open"],
        })
        print(f"{rule:<26} -> {result['equity']:8.2f}", flush=True)

    print()
    table = pd.DataFrame(rows).sort_values("eind", ascending=False)
    # the rule name reads as a sentence, so it belongs on the left; pandas right-aligns strings
    shown = table.copy()
    width = max(len(v) for v in shown["instapregel"])
    shown["instapregel"] = shown["instapregel"].str.ljust(width)
    with pd.option_context("display.width", 250, "display.max_columns", 30,
                           "display.colheader_justify", "left"):
        print(shown.to_string(index=False))
    if args.out:
        table.to_csv(args.out, index=False)
        print(f"\nweggeschreven naar {args.out}")


if __name__ == "__main__":
    sys.exit(main())
