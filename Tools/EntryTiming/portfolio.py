"""
Portfolio simulation: what one account actually does over the measured period.

The per-signal simulation in simulate_dca.py answers "what would this chance have been worth".
That is not what an account does. An account has limits, and they throw most chances away:

  - one position per symbol at a time (the trader refuses a second one)
  - a maximum number of long and short slots (SettingsTrading.SlotsMaximalLong / Short)
  - a fixed amount of money. Every open position holds its entry AND the limit orders of its
    whole DCA ladder, so that capital is not available for anything else
  - an exchange minimum per order, so a position that would be too small is simply not opened

Signals arrive in time order; whichever comes first takes the slot. A signal that arrives while
everything is occupied is skipped - and skipped is not the same as lost, because the account was
busy earning (or losing) elsewhere.

Usage:
    python portfolio.py --data "<dir>" --capital 500 --slots-long 15 --slots-short 15
"""

import argparse
import sys

import numpy as np
import pandas as pd

import candledb
import evaluate_rules as core
import simulate_dca

INTERVAL_MINUTES = 15


def run(positions, capital, slots_long, slots_short, minimum_order, ladder_size, verbose=False):
    """Walk through every signal in time order and take the ones the account has room for."""
    positions = positions[positions["filled"]].copy()
    positions["open_time"] = positions["opentime"] + positions["open_offset"] * INTERVAL_MINUTES
    positions["close_time"] = positions["opentime"] + positions["close_offset"] * INTERVAL_MINUTES
    positions = positions.sort_values("open_time").reset_index(drop=True)

    equity = capital             # only money that has actually been banked
    free = capital
    open_positions = []          # dicts with close_time, symbol, side, reserved, raw_result
    busy_symbols = set()
    counts = {"long": 0, "short": 0}
    realised = 0

    taken, skipped = [], {"symbol busy": 0, "no slot": 0, "no money": 0}
    curve = []

    for row in positions.itertuples():
        # close everything that finished before this signal
        still = []
        for pos in open_positions:
            # A position that never reached its stop or target inside the measured window is NOT
            # closed: it keeps its slot and its money, and its profit is not banked. Counting it as
            # closed would book a result that was never taken.
            if pos["never_closes"]:
                still.append(pos)
            elif pos["close_time"] <= row.open_time:
                profit = pos["invested"] * pos["raw_result"] / 100.0
                equity += profit
                realised += profit
                free += pos["reserved"] + profit
                busy_symbols.discard(pos["symbol"])
                counts[pos["side"]] -= 1
                curve.append((pos["close_time"], equity))
            else:
                still.append(pos)
        open_positions = still

        if row.symbol in busy_symbols:
            skipped["symbol busy"] += 1
            continue
        limit = slots_long if row.side == "long" else slots_short
        if counts[row.side] >= limit:
            skipped["no slot"] += 1
            continue

        # Size: divide the capital over the slots, then reserve the whole ladder for it.
        reserved = capital / (slots_long + slots_short)
        entry_amount = reserved / ladder_size
        if entry_amount < minimum_order or free < reserved:
            skipped["no money"] += 1
            continue

        free -= reserved
        busy_symbols.add(row.symbol)
        counts[row.side] += 1
        open_positions.append({
            "close_time": row.close_time, "symbol": row.symbol, "side": row.side,
            "reserved": reserved, "invested": entry_amount * row.at_risk,
            "raw_result": row.raw_result_pct,
            "never_closes": row.outcome == "open",
        })
        taken.append(row)

    # Whatever is still running at the end: not banked, so reported separately.
    unrealised = sum(p["invested"] * p["raw_result"] / 100.0 for p in open_positions)
    stuck = sum(p["reserved"] for p in open_positions)

    return {
        "capital": capital,
        "equity": equity,                 # start + banked profit
        "realised": realised,
        "unrealised": unrealised,
        "still_open": len(open_positions),
        "stuck": stuck,
        "free": free,
        "taken": taken,
        "skipped": skipped,
        "curve": pd.DataFrame(curve, columns=["time", "equity"]).sort_values("time"),
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--data", required=True)
    parser.add_argument("--capital", type=float, default=500.0)
    parser.add_argument("--slots-long", type=int, default=15)
    parser.add_argument("--slots-short", type=int, default=15)
    parser.add_argument("--minimum-order", type=float, default=5.0,
                        help="exchange minimum per order in quote currency")
    parser.add_argument("--window", type=int, default=7)
    parser.add_argument("--dca", default="5:200")
    parser.add_argument("--stop", type=float, default=2.5)
    parser.add_argument("--target", type=float, default=1.8)
    parser.add_argument("--entry", default="limit", choices=["limit", "riding", "market"])
    parser.add_argument("--stop-mode", default="altrady", choices=["trader", "altrady"])
    parser.add_argument("--fee", type=float, default=0.1,
                        help="Exchange.FeeRate: fee per transaction in percent (0.1 in the database)")
    parser.add_argument("--rules", default="now (current behaviour),stochastic turning")
    parser.add_argument("--sides", default="both", choices=["both", "long", "short"])
    args = parser.parse_args()

    dca_levels = simulate_dca.parse_dca(args.dca)
    ladder_size = 1.0 + sum(f / 100.0 for _, f in dca_levels)
    events, panel = core.load(args.data)
    matrices, horizon = core.to_matrices(events, panel, core.COLUMNS)
    rules = core.build_rules(matrices, args.window)

    slots_long = args.slots_long if args.sides in ("both", "long") else 0
    slots_short = args.slots_short if args.sides in ("both", "short") else 0

    print(f"startkapitaal {args.capital:.0f} | slots {slots_long} long / {slots_short} short | "
          f"DCA {args.dca} (ladder {ladder_size:.1f}x) | stop {args.stop}% ({args.stop_mode}) | "
          f"doel {args.target}% | fee {args.fee}% per transactie | minimum order {args.minimum_order:.0f}")
    reserved = args.capital / max(slots_long + slots_short, 1)
    print(f"per positie gereserveerd: {reserved:.2f}  ->  eerste instap {reserved/ladder_size:.2f}"
          f"{'   TE KLEIN VOOR DE EXCHANGE' if reserved/ladder_size < args.minimum_order else ''}\n")

    for name in [r.strip() for r in args.rules.split(",")]:
        frame = simulate_dca.simulate(events, matrices, rules[name], args.window, dca_levels,
                                      args.stop, args.target, args.entry, args.stop_mode, args.fee)
        if args.sides != "both":
            frame = frame[frame["side"] == args.sides]
        r = run(frame, args.capital, slots_long, slots_short, args.minimum_order, ladder_size)
        chances = int(frame["filled"].sum())
        taken, curve = r["taken"], r["curve"]
        print(f"--- {name} ---")
        print(f"  kansen die een positie hadden kunnen worden : {chances:6}")
        print(f"  daadwerkelijk geopend                       : {len(taken):6}"
              f"  ({100*len(taken)/max(chances,1):.1f}%)")
        for reden, aantal in r["skipped"].items():
            print(f"    overgeslagen, {reden:<12}              : {aantal:6}")
        print()
        print(f"  startkapitaal                               : {r['capital']:9.2f}")
        print(f"  geincasseerd (gesloten posities)            : {r['realised']:+9.2f}")
        print(f"  ------------------------------------------------------")
        print(f"  eindkapitaal                                : {r['equity']:9.2f}  "
              f"({100*(r['equity']/r['capital']-1):+.1f}%)")
        print()
        print(f"  nog open aan het eind                       : {r['still_open']:6} posities")
        print(f"  daarin vastgezet kapitaal                   : {r['stuck']:9.2f}")
        print(f"  openstaande winst/verlies (niet geincasseerd): {r['unrealised']:+9.2f}")
        if len(curve):
            top = curve["equity"].cummax()
            print(f"  grootste terugval van het eindkapitaal      : "
                  f"{100*((curve['equity']-top)/top).min():8.1f}%")
        print()


if __name__ == "__main__":
    sys.exit(main())
