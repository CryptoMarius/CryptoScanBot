"""
Full position simulation: entry + DCA ladder + stop-loss + take-profit, exactly the way the trader
builds a position.

What the trader does (PositionMonitor / StopLossCalculator / AltradyWebhook):

  1. the entry fills, 1 unit at the entry price
  2. EVERY DCA level is placed at once as a separate limit order, at a fixed percentage from the
     entry price, sized by its Factor (200 = twice the entry amount)
  3. DCA levels beyond a signal-provided stop-loss are skipped - they could never fill
  4. the stop-loss sits BEYOND the furthest placed DCA order, not at the entry:
        trader : furthest DCA price   x  (1 - stop%/100)
        altrady: entry price          x  (1 - (furthest DCA% + stop%)/100)
     (those two differ by a fraction of a percent; --stop-mode picks which one to simulate)
  5. the take-profit is a percentage from the BREAK-EVEN price, so it moves every time a DCA fills

Result is expressed as a percentage of the largest amount the position ever had at risk, so a run
with DCA can be compared with one without: 100 euro at risk means 100 euro at risk either way.

Ordering inside one candle, when several levels are touched: DCA first (the position gets bigger),
then the stop-loss, then the take-profit. That is the pessimistic order.

Usage:
    python simulate_dca.py --data "<dir>" --window 7 --dca "5:200" --stop 2.5 --target 1.8
"""

import argparse
import sys

import numpy as np
import pandas as pd

import evaluate_rules as core


def parse_dca(text):
    """"5:200,10:400" -> [(5.0, 200.0), (10.0, 400.0)] (percentage from entry, size factor)."""
    levels = []
    for part in text.split(","):
        part = part.strip()
        if not part:
            continue
        percentage, factor = part.split(":")
        levels.append((float(percentage), float(factor)))
    return sorted(levels)


def simulate(events, matrices, triggers, window, dca_levels, stop_pct, target_pct,
             entry_mode, stop_mode, fee_rate):
    """One row per event. Distances are kept in 'against the position' units, see evaluate_rules."""
    adverse = matrices["adverse_pct"]
    favourable = matrices["favourable_pct"]
    close_pct = matrices["close_pct"]
    band_pct = matrices["band_pct"]
    n, width = adverse.shape
    direction = np.where(events["side"].to_numpy() == "short", 1.0, -1.0)

    armable = triggers & ~np.isnan(adverse)
    armed_at = np.where(armable.any(axis=1), armable.argmax(axis=1), -1)

    filled = np.zeros(n, dtype=bool)
    result = np.zeros(n)
    outcome = np.full(n, "no fill", dtype=object)
    dca_hits = np.zeros(n, dtype=int)
    at_risk = np.full(n, np.nan)
    open_offset = np.full(n, -1, dtype=int)     # candles after the signal that the entry filled
    close_offset = np.full(n, -1, dtype=int)    # candles after the signal that the position closed
    raw_result = np.zeros(n)                    # result in percent of the amount actually invested

    # The furthest DCA decides where the stop-loss goes.
    furthest_dca = max((p for p, _ in dca_levels), default=0.0)

    for i in range(n):
        k = armed_at[i]
        if k < 0:
            outcome[i] = "no trigger"
            continue
        sign = direction[i]

        def price_of(distance):
            """Price at `distance` against the position, relative to the original entry E0."""
            return 1.0 + sign * distance / 100.0

        # --- find the entry -------------------------------------------------
        if entry_mode == "market":
            fill = k
            entry_distance = -close_pct[i, k]
        else:
            fill, entry_distance = -1, np.nan
            for j in range(k + 1, min(window, width - 1) + 1):
                source = j if entry_mode == "riding" else k
                limit = np.nanmax([band_pct[i, source], -close_pct[i, source]])
                if not np.isfinite(limit):
                    continue
                if adverse[i, j] >= limit:
                    fill, entry_distance = j, limit
                    break
            if fill < 0:
                continue
        if not np.isfinite(entry_distance):
            continue
        entry = price_of(entry_distance)
        if entry <= 0:
            continue

        # --- build the ladder, all prices relative to the ENTRY --------------
        # A price x% against the position sits at entry * (1 + sign * x/100).
        ladder = [(entry * (1.0 + sign * p / 100.0), f / 100.0) for p, f in dca_levels]
        if stop_mode == "altrady":
            stop_price = entry * (1.0 + sign * (furthest_dca + stop_pct) / 100.0)
        else:
            deepest = entry * (1.0 + sign * furthest_dca / 100.0) if dca_levels else entry
            stop_price = deepest * (1.0 + sign * stop_pct / 100.0)

        quantity = 1.0
        invested = entry            # 1 unit at the entry price
        maximum_at_risk = 1.0       # in units of the entry amount
        pending = list(ladder)
        closed = False

        for j in range(fill + 1, width):
            if not np.isfinite(adverse[i, j]):
                break
            worst = price_of(adverse[i, j])          # high for a short, low for a long
            best = price_of(-favourable[i, j])       # low for a short, high for a long

            # 1. DCA levels the candle reached (they sit against the position)
            still = []
            for level_price, size in pending:
                reached = worst >= level_price if sign > 0 else worst <= level_price
                if reached:
                    quantity += size
                    invested += size * level_price
                    maximum_at_risk = max(maximum_at_risk, quantity)
                    dca_hits[i] += 1
                else:
                    still.append((level_price, size))
            pending = still

            # The anchor the trader prices the take-profit from (TpGridAnchorPrice): the average
            # fill price of entry + DCAs, moved AGAINST the position by the fee already paid plus
            # the fee still to be paid on the exit. So the fee makes the target sit further away,
            # it is not subtracted from the profit afterwards.
            break_even = invested / quantity
            anchor = break_even * (1.0 + sign * 2.0 * fee_rate / 100.0)
            target_price = anchor * (1.0 - sign * target_pct / 100.0)

            # 2. stop-loss
            hit_stop = worst >= stop_price if sign > 0 else worst <= stop_price
            if hit_stop:
                result[i] = ((stop_price / break_even - 1.0) * -sign * 100.0
                             - 2.0 * fee_rate) * quantity
                outcome[i] = "stop"
                close_offset[i] = j
                closed = True
                break

            # 3. take-profit
            hit_target = best <= target_price if sign > 0 else best >= target_price
            if hit_target:
                # what the position actually nets: sell price against the average fill price,
                # minus the fees on the way in and out
                result[i] = ((target_price / break_even - 1.0) * -sign * 100.0
                             - 2.0 * fee_rate) * quantity
                outcome[i] = "target"
                close_offset[i] = j
                closed = True
                break

        if not closed:
            last = price_of(-close_pct[i, width - 1])
            break_even = invested / quantity
            result[i] = ((last / break_even - 1.0) * -sign * 100.0 - 2.0 * fee_rate) * quantity
            outcome[i] = "open"
            close_offset[i] = width - 1

        # Express in percent of the amount that had to be RESERVED for this position: the full
        # ladder, not just the part that filled. The DCA orders sit there as open limit orders, so
        # that capital cannot be used for anything else - measuring against the filled amount only
        # would flatter every position whose DCA never triggered.
        filled[i] = True
        reserved = 1.0 + sum(f / 100.0 for _, f in dca_levels)
        at_risk[i] = maximum_at_risk
        open_offset[i] = fill
        # raw_result: percent of the money that was actually put in (1 unit at entry plus any DCA
        # that filled). The portfolio simulation multiplies this by the real amount invested.
        raw_result[i] = result[i] / maximum_at_risk
        result[i] = result[i] / reserved

    return pd.DataFrame({
        "symbol": events["symbol"].to_numpy(),
        "side": events["side"].to_numpy(),
        "opentime": events["opentime"].to_numpy(),
        "filled": filled,
        "dca_hits": dca_hits,
        "at_risk": at_risk,
        "outcome": outcome,
        "result_pct": np.where(filled, result, 0.0),
        "raw_result_pct": np.where(filled, raw_result, 0.0),
        "open_offset": open_offset,
        "close_offset": close_offset,
    })


def report(title, frame):
    taken = frame[frame["filled"]]
    if not len(taken):
        print(f"--- {title} ---\n  geen posities\n")
        return
    counts = taken["outcome"].value_counts()
    print(f"--- {title} ---")
    print(f"  posities geopend      : {len(taken):6}")
    print(f"  waarvan met doel      : {counts.get('target', 0):6}")
    print(f"  waarvan met stop-loss : {counts.get('stop', 0):6}")
    print(f"  nog open op het eind  : {counts.get('open', 0):6}")
    print(f"  gemiddeld aantal DCA  : {taken['dca_hits'].mean():6.2f}")
    print(f"  opbrengst totaal      : {taken['result_pct'].sum():+10.1f}%")
    print(f"  per positie           : {taken['result_pct'].mean():+10.3f}%")
    for naam in ("target", "stop", "open"):
        deel = taken[taken["outcome"] == naam]["result_pct"]
        if len(deel):
            print(f"    waarvan {naam:<10}: {deel.sum():+10.1f}%  ({deel.mean():+.3f}% per stuk)")
    per_symbol = taken.groupby("symbol")["result_pct"].sum().sort_values()
    print(f"  mediaan per munt      : {per_symbol.median():+10.1f}%")
    print(f"  zonder 5 slechtste    : {per_symbol.iloc[5:].sum():+10.1f}%")
    print(f"  zonder 5 beste        : {per_symbol.iloc[:-5].sum():+10.1f}%")
    print(f"  munten met winst      : {(per_symbol > 0).sum():6} van {len(per_symbol)}")
    print()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--data", required=True)
    parser.add_argument("--window", type=int, default=7)
    parser.add_argument("--dca", default="5:200", help="'percentage:factor' pairs, e.g. 5:200,10:400")
    parser.add_argument("--stop", type=float, default=2.5)
    parser.add_argument("--target", type=float, default=1.8)
    parser.add_argument("--entry", default="limit", choices=["limit", "riding", "market"])
    parser.add_argument("--stop-mode", default="trader", choices=["trader", "altrady"])
    parser.add_argument("--fee", type=float, default=0.1,
                        help="Exchange.FeeRate: fee per trade in percent (0.1 in the database)")
    parser.add_argument("--rules", default="now (current behaviour),stochastic turning")
    parser.add_argument("--out", default="")
    args = parser.parse_args()

    dca_levels = parse_dca(args.dca)
    events, panel = core.load(args.data)
    matrices, horizon = core.to_matrices(events, panel, core.COLUMNS)
    rules = core.build_rules(matrices, args.window)

    furthest = max((p for p, _ in dca_levels), default=0.0)
    if args.stop_mode == "altrady":
        depth = furthest + args.stop
    else:
        depth = 100.0 * (1.0 - (1.0 - furthest / 100.0) * (1.0 - args.stop / 100.0))
    total_size = 1.0 + sum(f / 100.0 for _, f in dca_levels)
    print(f"{len(events)} kansen | horizon {horizon} candles | wachtvenster {args.window}")
    print(f"DCA {dca_levels} -> volle positie {total_size:.1f}x de instap")
    print(f"stop-loss ({args.stop_mode}) op {depth:.3f}% van de instap | doel {args.target}% vanaf break-even")
    print(f"kosten {args.costs}% per positie\n")

    frames = {}
    for name in [r.strip() for r in args.rules.split(",")]:
        frame = simulate(events, matrices, rules[name], args.window, dca_levels,
                         args.stop, args.target, args.entry, args.stop_mode, args.fee)
        frames[name] = frame
        for side in ("long", "short"):
            report(f"{name} - {side}", frame[frame["side"] == side])

    if args.out:
        pd.concat([f.assign(rule=n) for n, f in frames.items()]).to_csv(args.out, index=False)
        print(f"weggeschreven naar {args.out}")


if __name__ == "__main__":
    sys.exit(main())
