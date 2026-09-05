"""
One report, several scenarios, always the same layout.

Each scenario prints the settings it used and then the same table for every entry rule, so two
scenarios can be laid side by side without wondering what changed between them. The candles and
indicators are computed once and reused across all scenarios.

Usage:
    python report.py --candles "<db>" --settings "<CryptoScanBot-settings.json>" --capital 500
"""

import argparse
import sys
import time

import numpy as np
import pandas as pd

import account
import candledb
import measure_entry_timing as met

# label, entry order, minimum bollinger width
SCENARIOS = [
    ("1  limietorder, geen extra filter",              "limit",  None),
    ("2  limietorder, bollinger breedte boven 3,0%",   "limit",  3.0),
    ("3  marktorder, geen extra filter",               "market", None),
]


def header(label, args, entry_order, bb_width, ladder, deepest):
    line = "=" * 104
    print(f"\n{line}\nSCENARIO {label}\n{'-' * 104}")
    print(f"  interval                   : {args.interval}")
    print(f"  signaal                    : vbs-bandbreuk + rsi "
          f"{met.RSI_OVERSOLD:.0f}/{met.RSI_OVERBOUGHT:.0f}")
    print(f"  bollinger binnen vbs-band  : "
          f"{'nee (uitgezet)' if args.no_bb_inside else 'ja (VbsSignalShort regel 77/82)'}")
    print(f"  bollinger breedte minimaal : "
          f"{met.BB_WIDTH_MINIMUM if bb_width is None else bb_width}%")
    print(f"  order bij instap           : " + (
        "limietorder op de vbs-band (EntryOrderType = Limit), vervalt na het wachtvenster"
        if entry_order == "limit" else
        "marktorder op de close van de candle waar de regel afgaat (altijd gevuld)"))
    print(f"  wachtvenster               : {args.window} candles (EntryRemoveTime)")
    print(f"  dca                        : {args.dca}  (volle positie {ladder:.1f}x de instap)")
    print(f"  stop-loss                  : {args.stop}% voorbij de verste dca "
          f"(= {deepest + args.stop:.1f}% van de instap)")
    print(f"  doel                       : {args.target}% vanaf het anker (de fee zit erin)")
    print(f"  fee                        : {args.fee}% per transactie")
    print(f"  slots                      : {args.slots_long} long / {args.slots_short} short")
    print(f"  startkapitaal              : {args.capital:.0f}")
    print(f"  stochastiek / bollinger    : stoch {met.STOCH_LENGTH}/{met.STOCH_K}/{met.STOCH_D} "
          f"({met.STOCH_OVERSOLD:.0f}/{met.STOCH_OVERBOUGHT:.0f}) | "
          f"bollinger {met.BB_LENGTH}/{met.BB_DEVIATION}")
    print("-" * 104)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--candles", required=True)
    parser.add_argument("--settings", default="")
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
    parser.add_argument("--no-bb-inside", action="store_true", default=True)
    parser.add_argument("--out-prefix", default="")
    args = parser.parse_args()

    if args.settings:
        met.load_settings(args.settings)

    dca_levels = [tuple(float(v) for v in part.split(":")) for part in args.dca.split(",") if part]
    ladder = 1.0 + sum(f / 100.0 for _, f in dca_levels)
    deepest = max((p for p, _ in dca_levels), default=0.0)
    interval_id = candledb.INTERVAL_IDS[args.interval]
    interval_minutes = candledb.INTERVAL_MINUTES[args.interval]
    connection = candledb.open_readonly(args.candles)

    started = time.time()
    prepared = []
    for symbol_id, name, count in candledb.list_symbols(connection, interval_id, met.WARMUP + 100):
        frame = candledb.load_candles(connection, symbol_id, interval_id)
        if len(frame) < met.WARMUP + 100:
            continue
        prepared.append((name, met.compute(frame), candledb.gap_mask(frame, interval_minutes)))
    print(f"indicatoren voor {len(prepared)} munten klaar in {time.time()-started:.0f}s")

    for label, entry_order, bb_width in SCENARIOS:
        rows = []
        for rule in account.ENTRY_RULES:
            positions = []
            for name, data, contiguous in prepared:
                for side in ("short", "long"):
                    positions.extend(account.find_positions(
                        name, data, contiguous, side, rule, args.window, dca_levels, args.stop,
                        args.target, args.fee, 5, interval_minutes, 1.0,
                        not args.no_bb_inside, bb_width, entry_order))
            frame = pd.DataFrame(positions)
            if not len(frame):
                continue
            result = account.run_account(frame, args.capital, args.slots_long, args.slots_short,
                                         args.minimum_order, ladder)
            curve = result["curve"]
            drop = ""
            if len(curve):
                top = curve["equity"].cummax()
                drop = f"{100*((curve['equity']-top)/top).min():.1f}%"
            rows.append({
                "instapregel": rule,
                "kansen": len(frame),
                "geopend": result["taken"],
                # over the positions the account actually opened, not over every chance
                "winst/verlies": f"{int((result['opened']['outcome']=='target').sum())}/"
                                 f"{int((result['opened']['outcome']=='stop').sum())}",
                "eind": round(result["equity"], 2),
                "pf groei": f"{100*(result['equity']/args.capital-1):+.1f}%",
                "terugval": drop,
            })

        header(label, args, entry_order, bb_width, ladder, deepest)
        table = pd.DataFrame(rows).sort_values("eind", ascending=False)
        shown = table.copy()
        width = max(len(v) for v in shown["instapregel"])
        shown["instapregel"] = shown["instapregel"].str.ljust(width)
        with pd.option_context("display.width", 250, "display.max_columns", 20,
                               "display.colheader_justify", "left"):
            print(shown.to_string(index=False))
        if args.out_prefix:
            table.to_csv(f"{args.out_prefix}_{label.split()[0]}.csv", index=False)


if __name__ == "__main__":
    sys.exit(main())
