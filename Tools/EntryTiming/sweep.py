"""
Sweep the stop-loss / take-profit grid for a handful of entry rules.

The entry rule decides WHERE you get in; the stop and target decide whether the win rate you get
is enough. This script varies both and reports the expectancy per event, so the two effects can be
told apart.

Every combination is reported on two halves of the data (the first and second half of the period,
or even/odd symbols). A number that only works on one half is noise, not an edge.

Break-even win rate for a given pair is stop / (stop + target); it is printed next to the result so
you can see how much room a rule actually has.

Usage:
    python sweep.py --data "<dir>" --window 9 --split time
"""

import argparse
import os
import sys

import numpy as np
import pandas as pd

import evaluate_rules as core

RULES_OF_INTEREST = [
    "now (current behaviour)",
    "back inside vbs band",
    "two candles inside the band",
    "lower high + parabolic sar",
    "rejection candle (close < 33%)",
]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--data", required=True)
    parser.add_argument("--window", type=int, default=9)
    parser.add_argument("--entry", default="limit", choices=["limit", "riding", "market"])
    parser.add_argument("--split", default="time", choices=["time", "symbol", "none"])
    parser.add_argument("--stops", default="1.5,2.0,2.5,3.0,4.0")
    parser.add_argument("--targets", default="0.75,1.0,1.5,1.8,2.5,3.5")
    parser.add_argument("--rules", default="")
    parser.add_argument("--out", default="")
    args = parser.parse_args()

    events, panel = core.load(args.data)
    matrices, horizon = core.to_matrices(events, panel, core.COLUMNS)
    rules = core.build_rules(matrices, args.window)
    wanted = [r.strip() for r in args.rules.split(",") if r.strip()] or RULES_OF_INTEREST

    if args.split == "time":
        middle = events["opentime"].median()
        group = np.where(events["opentime"] <= middle, "first half", "second half")
    elif args.split == "symbol":
        codes = pd.factorize(events["symbol"])[0]
        group = np.where(codes % 2 == 0, "even symbols", "odd symbols")
    else:
        group = np.full(len(events), "all")
    groups = sorted(set(group))

    stops = [float(v) for v in args.stops.split(",")]
    targets = [float(v) for v in args.targets.split(",")]

    rows = []
    for name in wanted:
        if name not in rules:
            print(f"unknown rule: {name}")
            continue
        for stop in stops:
            for target in targets:
                frame = core.evaluate(events, matrices, rules[name], args.window, stop, target, args.entry)
                record = {"rule": name, "stop%": stop, "target%": target,
                          "break-even win%": 100.0 * stop / (stop + target)}
                for part in groups:
                    mask = group == part
                    subset = frame[mask]
                    taken = subset[subset["filled"]]
                    wins = int((taken["outcome"] == "target").sum())
                    losses = int((taken["outcome"] == "stop").sum())
                    record[f"win% {part}"] = 100.0 * wins / max(wins + losses, 1)
                    record[f"exp% {part}"] = subset["result_pct"].mean()
                    record[f"n {part}"] = len(taken)
                rows.append(record)
                print(".", end="", flush=True)
    print()

    table = pd.DataFrame(rows)
    with pd.option_context("display.width", 250, "display.max_columns", 30, "display.max_rows", 400):
        print(table.to_string(index=False, float_format=lambda v: f"{v:7.3f}"))
    if args.out:
        table.to_csv(args.out, index=False)
        print(f"\nwritten to {args.out}")


if __name__ == "__main__":
    sys.exit(main())
