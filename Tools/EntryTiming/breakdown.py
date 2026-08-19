"""
Break one entry rule down along the axes that decide whether it is trustworthy:

  volatility   the parabolic SAR is said to be unreliable in a quiet market, so split the events by
               the Bollinger width and by ACS at the signal candle
  side         long and short do not have to behave the same
  window       how many candles the signal must stay armed (EntryRemoveTime) before the trigger
               arrives - a rule that needs 15 candles is a different rule

The reference line is the break-even win rate, stop / (stop + target). What matters is the MARGIN
above it, not the win rate itself.

Usage:
    python breakdown.py --data "<dir>" --rule "lower high + parabolic sar" --stop 2.5 --target 1.8
"""

import argparse
import sys

import numpy as np
import pandas as pd

import evaluate_rules as core


def score(frame, events, mask, stop, target):
    subset = frame[mask]
    taken = subset[subset["filled"]]
    wins = int((taken["outcome"] == "target").sum())
    losses = int((taken["outcome"] == "stop").sum())
    total = max(wins + losses, 1)
    win_rate = 100.0 * wins / total
    break_even = 100.0 * stop / (stop + target)
    return {
        "events": int(mask.sum()),
        "trades": len(taken),
        "fill%": 100.0 * len(taken) / max(int(mask.sum()), 1),
        "win%": win_rate,
        "margin (pp)": win_rate - break_even,
        "per trade%": taken["result_pct"].mean() if len(taken) else np.nan,
        "expectancy%": subset["result_pct"].mean(),
    }


def show(title, rows):
    print(f"\n=== {title} ===")
    table = pd.DataFrame(rows)
    with pd.option_context("display.width", 200, "display.max_columns", 20):
        print(table.to_string(index=False, float_format=lambda v: f"{v:8.3f}"))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--data", required=True)
    parser.add_argument("--rule", default="lower high + parabolic sar")
    parser.add_argument("--window", type=int, default=9)
    parser.add_argument("--stop", type=float, default=2.5)
    parser.add_argument("--target", type=float, default=1.8)
    parser.add_argument("--entry", default="limit", choices=["limit", "riding", "market"])
    args = parser.parse_args()

    events, panel = core.load(args.data)
    matrices, horizon = core.to_matrices(events, panel, core.COLUMNS)
    break_even = 100.0 * args.stop / (args.stop + args.target)
    print(f"rule: {args.rule} | stop {args.stop}% | target {args.target}% | "
          f"break-even win rate {break_even:.2f}% | {len(events)} events")

    rules = core.build_rules(matrices, args.window)
    baseline = core.evaluate(events, matrices, rules["now (current behaviour)"], args.window,
                             args.stop, args.target, args.entry)
    frame = core.evaluate(events, matrices, rules[args.rule], args.window, args.stop, args.target, args.entry)

    everything = np.ones(len(events), dtype=bool)
    show("overall", [
        dict(cut="now (current behaviour)", **score(baseline, events, everything, args.stop, args.target)),
        dict(cut=args.rule, **score(frame, events, everything, args.stop, args.target)),
    ])

    # Volatility: the objection against the parabolic SAR is that it needs a real move.
    for column, label in (("bb_width", "bollinger width %"), ("acs_pct", "average candle size %")):
        values = events[column].to_numpy()
        edges = np.nanpercentile(values, [0, 25, 50, 75, 100])
        rows = []
        for low, high in zip(edges[:-1], edges[1:]):
            mask = (values >= low) & (values <= high if high == edges[-1] else values < high)
            rows.append(dict(cut=f"{label} {low:.2f} - {high:.2f}",
                             **score(frame, events, mask, args.stop, args.target)))
            rows.append(dict(cut="   (current behaviour)",
                             **score(baseline, events, mask, args.stop, args.target)))
        show(f"by {label}", rows)

    sides = events["side"].to_numpy()
    show("by side", [row for side in ("short", "long") for row in (
        dict(cut=f"{side}: {args.rule}", **score(frame, events, sides == side, args.stop, args.target)),
        dict(cut=f"{side}: current behaviour", **score(baseline, events, sides == side, args.stop, args.target)),
    )])

    rows = []
    for window in (3, 5, 7, 9, 12, 16, 24):
        windowed = core.build_rules(matrices, window)[args.rule]
        scored = core.evaluate(events, matrices, windowed, window, args.stop, args.target, args.entry)
        rows.append(dict(cut=f"armed for {window} candles",
                         **score(scored, events, everything, args.stop, args.target)))
    show("by armed window (EntryRemoveTime)", rows)


if __name__ == "__main__":
    sys.exit(main())
