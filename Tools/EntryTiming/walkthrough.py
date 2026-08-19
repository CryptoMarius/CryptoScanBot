"""
Print one single position candle by candle, so the simulation can be checked by hand.

Shows the entry, every DCA order and where it sits, the stop-loss, the moving take-profit, and what
happens on each candle until the position closes. Run it for a long and for a short and the two
should read as mirror images of each other.

Usage:
    python walkthrough.py --data "<dir>" --side short --n 2
"""

import argparse
import sys

import numpy as np
import pandas as pd

import candledb
import evaluate_rules as core
import simulate_dca


def walk(events, matrices, index, dca_levels, stop_pct, target_pct, window, entry_mode, stop_mode):
    adverse = matrices["adverse_pct"][index]
    favourable = matrices["favourable_pct"][index]
    close_pct = matrices["close_pct"][index]
    band_pct = matrices["band_pct"][index]
    row = events.iloc[index]
    sign = 1.0 if row["side"] == "short" else -1.0
    tegen = "omhoog" if sign > 0 else "omlaag"
    mee = "omlaag" if sign > 0 else "omhoog"

    def price_of(distance):
        return 1.0 + sign * distance / 100.0

    print(f"{row['symbol']}  {row['side'].upper()}  bandbreuk op {row['date']}")
    print(f"de positie verliest geld als de koers {tegen} gaat en verdient als hij {mee} gaat")
    print(f"(alle prijzen als verhouding tot de bandprijs van het signaal, die op 1.000000 staat)\n")

    # entry
    if entry_mode == "market":
        fill, entry_distance = 0, -close_pct[0]
    else:
        fill, entry_distance = -1, np.nan
        for j in range(1, window + 1):
            limit = np.nanmax([band_pct[j if entry_mode == "riding" else 0],
                               -close_pct[j if entry_mode == "riding" else 0]])
            if np.isfinite(limit) and adverse[j] >= limit:
                fill, entry_distance = j, limit
                break
        if fill < 0:
            print("de limietorder is nooit gevuld -> geen positie")
            return
    entry = price_of(entry_distance)
    print(f"candle {fill:>2}: instap gevuld op {entry:.6f}")

    ladder = [(entry * (1.0 + sign * p / 100.0), f / 100.0, p) for p, f in dca_levels]
    furthest = max((p for p, _ in dca_levels), default=0.0)
    if stop_mode == "altrady":
        stop_price = entry * (1.0 + sign * (furthest + stop_pct) / 100.0)
    else:
        deepest = entry * (1.0 + sign * furthest / 100.0) if dca_levels else entry
        stop_price = deepest * (1.0 + sign * stop_pct / 100.0)

    for level_price, size, pct in ladder:
        print(f"          dca-order  op {level_price:.6f}  ({pct}% {tegen}, {size:.1f} eenheid erbij)")
    print(f"          stop-loss  op {stop_price:.6f}  ({abs(stop_price/entry-1)*100:.2f}% {tegen} van de instap)")

    quantity, invested, biggest = 1.0, entry, 1.0
    reserved = 1.0 + sum(size for _, size, _ in ladder)
    pending = list(ladder)
    break_even = entry
    target_price = break_even * (1.0 - sign * target_pct / 100.0)
    print(f"          doel       op {target_price:.6f}  ({target_pct}% {mee} van break-even)\n")

    for j in range(fill + 1, len(adverse)):
        if not np.isfinite(adverse[j]):
            break
        worst = price_of(adverse[j])
        best = price_of(-favourable[j])
        note = ""

        still = []
        for level_price, size, pct in pending:
            reached = worst >= level_price if sign > 0 else worst <= level_price
            if reached:
                quantity += size
                invested += size * level_price
                biggest = max(biggest, quantity)
                break_even = invested / quantity
                target_price = break_even * (1.0 - sign * target_pct / 100.0)
                note += (f"  <- DCA {pct}% gevuld, positie nu {quantity:.1f} eenheden, "
                         f"break-even {break_even:.6f}, doel schuift naar {target_price:.6f}")
            else:
                still.append((level_price, size, pct))
        pending = still

        hit_stop = worst >= stop_price if sign > 0 else worst <= stop_price
        hit_target = best <= target_price if sign > 0 else best >= target_price

        if j - fill <= 6 or note or hit_stop or hit_target:
            print(f"candle {j:>2}: uiterste {tegen} {worst:.6f}   uiterste {mee} {best:.6f}{note}")

        if hit_stop:
            verlies = (stop_price / break_even - 1.0) * -sign * 100.0 * quantity
            print(f"\n  STOP-LOSS geraakt op {stop_price:.6f}")
            print(f"  break-even was {break_even:.6f}, positie {quantity:.1f} eenheden")
            print(f"  resultaat: {verlies:+.2f}% van een enkele inzet "
                  f"= {verlies/reserved:+.2f}% van het gereserveerde bedrag ({reserved:.0f} eenheden)")
            return
        if hit_target:
            winst = target_pct * quantity
            print(f"\n  DOEL geraakt op {target_price:.6f}")
            print(f"  break-even was {break_even:.6f}, positie {quantity:.1f} eenheden")
            print(f"  resultaat: {winst:+.2f}% van een enkele inzet "
                  f"= {winst/reserved:+.2f}% van het gereserveerde bedrag ({reserved:.0f} eenheden)")
            return

    last = price_of(-close_pct[-1])
    rest = (last / break_even - 1.0) * -sign * 100.0 * quantity
    print(f"\n  nog open aan het eind van het venster, laatste koers {last:.6f}")
    print(f"  resultaat: {rest/reserved:+.2f}% van het gereserveerde bedrag ({reserved:.0f} eenheden)")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--data", required=True)
    parser.add_argument("--side", default="short", choices=["short", "long"])
    parser.add_argument("--n", type=int, default=1, help="how many examples")
    parser.add_argument("--window", type=int, default=7)
    parser.add_argument("--dca", default="5:200")
    parser.add_argument("--stop", type=float, default=2.5)
    parser.add_argument("--target", type=float, default=1.8)
    parser.add_argument("--entry", default="limit", choices=["limit", "riding", "market"])
    parser.add_argument("--stop-mode", default="trader", choices=["trader", "altrady"])
    parser.add_argument("--symbol", default="")
    args = parser.parse_args()

    events, panel = core.load(args.data)
    matrices, _ = core.to_matrices(events, panel, core.COLUMNS)
    dca_levels = simulate_dca.parse_dca(args.dca)

    mask = events["side"] == args.side
    if args.symbol:
        mask &= events["symbol"] == args.symbol
    picks = list(np.flatnonzero(mask.to_numpy()))[:: max(len(np.flatnonzero(mask.to_numpy())) // max(args.n, 1), 1)]
    for index in picks[:args.n]:
        walk(events, matrices, int(index), dca_levels, args.stop, args.target,
             args.window, args.entry, args.stop_mode)
        print("\n" + "=" * 100 + "\n")


if __name__ == "__main__":
    sys.exit(main())
