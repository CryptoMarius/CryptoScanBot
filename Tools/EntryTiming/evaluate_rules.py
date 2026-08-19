"""
Score candidate entry rules against the measured VBS events.

Reads the output of measure_entry_timing.py and replays every event under a set of rules.

A rule has two halves:
  WHEN   after the band break, keep the signal armed for at most `window` candles and arm the entry
         on the first candle that satisfies the rule ("now" = the current behaviour, arm immediately)
  HOW    market  - enter at the close of that candle, always filled
         limit   - place a limit order on the VBS band and wait for price to come back to it; if it
                   never does within the armed window there is NO trade. This is what the scanner
                   does today (EntryOrderType = Limit, signal price = the band).
         riding  - same, but the limit price follows the band on every candle (a newer band break
                   supersedes the older signal, so in practice the order rides the band)

The trade then runs to the FIXED percentages from the trading settings, measured from the entry:
    stop-loss   = entry +/- stop%      (SettingsTrading.StopLossPercentage, 2.5)
    take-profit = entry -/+ target%    (SettingsTrading.TpList, 1.8 live / 1.5 in session 1)

DCA is deliberately NOT simulated here: it changes position size, not entry timing, and mixing the
two would hide which of the two is doing the work.

Reported per rule:
    trades      events that produced a filled entry
    fill%       share of all events that got filled (a limit order that never fills = no trade)
    win%        share of the filled trades that reached the take-profit first
    entry gain  average improvement of the entry price over the signal candle close, in percent -
                positive means a better price than a market order would have given
    per trade   average result per filled trade, in percent of the position
    expectancy  average over ALL events, unfilled ones counted as 0.0. This is the number to rank
                on: a rule that trades rarely but well is judged on the same opportunities as one
                that trades often and badly.

Usage:
    python evaluate_rules.py --data "<dir>" --window 9 --stop 2.5 --target 1.8 --entry limit
"""

import argparse
import os
import sys

import numpy as np
import pandas as pd

COLUMNS = ["adverse_pct", "favourable_pct", "close_pct", "close_in_range", "psar_against",
           "stoch_extreme", "stoch_turn", "macd_turn", "rsi_extreme", "vbs_pos", "bb_out",
           "kc_break", "band_pct", "basis_pct", "acs_pct"]


def load(directory):
    events = pd.read_csv(os.path.join(directory, "events.csv"))
    panel = pd.read_csv(os.path.join(directory, "panel.csv.gz"))
    events["event_id"] = (events["symbol"] + "|" + events["side"] + "|"
                          + events["opentime"].astype(str))
    return events, panel


def to_matrices(events, panel, columns):
    """Reshape the long panel into (n_events x n_offsets) matrices, one per column."""
    horizon = int(panel["offset"].max())
    order = {event_id: i for i, event_id in enumerate(events["event_id"])}
    mapped = panel["event_id"].map(order)
    keep = mapped.notna().to_numpy()
    rows = mapped.to_numpy()[keep].astype(int)
    cols = panel["offset"].to_numpy()[keep].astype(int)

    matrices = {}
    for column in columns:
        values = panel[column].to_numpy()[keep].astype(float)
        matrix = np.full((len(events), horizon + 1), np.nan)
        matrix[rows, cols] = values
        matrices[column] = matrix
    return matrices, horizon


def shift_right(matrix):
    shifted = np.full_like(matrix, np.nan)
    shifted[:, 1:] = matrix[:, :-1]
    return shifted


def build_rules(matrices, window):
    """Boolean matrix per rule: True on the offsets where the rule says 'arm the entry now'.

    Every input is already normalised to the direction of the position (see measure_entry_timing),
    so one expression covers long and short: a high value means the move that caused the signal is
    still running, a low or negative one means it is turning in our favour.
    """
    adverse = matrices["adverse_pct"]
    close_in_range = matrices["close_in_range"]
    psar_against = matrices["psar_against"]
    stoch_turn = matrices["stoch_turn"]
    macd_turn = matrices["macd_turn"]
    vbs_pos, bb_out = matrices["vbs_pos"], matrices["bb_out"]
    kc_break = matrices["kc_break"]

    n, width = adverse.shape
    later = np.zeros((n, width), dtype=bool)
    later[:, 1:window + 1] = True

    # A new extreme against the position on this candle means the move is still running.
    still_running = adverse > shift_right(adverse)
    exhausted = later & ~still_running

    rules = {}
    now = np.zeros((n, width), dtype=bool)
    now[:, 0] = True
    rules["now (current behaviour)"] = now

    rules["lower high"] = exhausted
    rules["rejection candle (close < 33%)"] = later & (close_in_range < 33.0)
    rules["lower high + close < 50%"] = exhausted & (close_in_range < 50.0)
    rules["parabolic sar flip"] = later & (psar_against > 0.5) & (shift_right(psar_against) < 0.5)
    rules["parabolic sar against"] = later & (psar_against > 0.5)
    rules["stochastic cross"] = later & (stoch_turn < 0) & (shift_right(stoch_turn) >= 0)
    rules["stochastic turning"] = later & (stoch_turn < 0)
    rules["macd histogram turns"] = later & (macd_turn < shift_right(macd_turn)) & (shift_right(macd_turn) > 0)
    rules["back inside vbs band"] = later & (vbs_pos < 100.0)
    rules["back inside bollinger"] = later & (bb_out < 100.0)
    rules["back inside keltner"] = later & (kc_break < 0.5)
    rules["lower high + stochastic turning"] = exhausted & (stoch_turn < 0)
    rules["lower high + parabolic sar"] = exhausted & (psar_against > 0.5)
    rules["lower high + back inside vbs"] = exhausted & (vbs_pos < 100.0)
    rules["rejection + back inside vbs"] = later & (close_in_range < 33.0) & (vbs_pos < 100.0)
    rules["two candles inside the band"] = later & (vbs_pos < 100.0) & (shift_right(vbs_pos) < 100.0)
    rules["back inside vbs + stochastic turning"] = later & (vbs_pos < 100.0) & (stoch_turn < 0)
    rules["back inside vbs + parabolic sar"] = later & (vbs_pos < 100.0) & (psar_against > 0.5)
    return rules


def evaluate(events, matrices, triggers, window, stop_pct, target_pct, entry_mode):
    """Result per event, in percent of the position. Unfilled events score 0.0.

    Everything is kept in "distance against the position" units (u): u = +1% means the price is 1%
    further in the direction the position does not want - up for a short, down for a long. A price
    with distance u sits at E0 * (1 + direction * u / 100), where direction is +1 for a short and
    -1 for a long. Converting a measurement from the original entry E0 to a later entry E1 is then
    the same formula for both sides.
    """
    adverse = matrices["adverse_pct"]           # high (short) / low (long): distance against us
    favourable = matrices["favourable_pct"]     # low  (short) / high (long): distance in our favour
    close_pct = matrices["close_pct"]           # close, positive = in profit
    band_pct = matrices["band_pct"]             # the VBS band on that candle, as a distance
    n, width = adverse.shape
    direction = np.where(events["side"].to_numpy() == "short", 1.0, -1.0)

    armable = triggers & ~np.isnan(adverse)
    armed_at = np.where(armable.any(axis=1), armable.argmax(axis=1), -1)

    filled = np.zeros(n, dtype=bool)
    result = np.zeros(n)
    outcome = np.full(n, "no fill", dtype=object)
    entry_gain = np.full(n, np.nan)
    wait = np.full(n, np.nan)

    for i in range(n):
        k = armed_at[i]
        if k < 0:
            outcome[i] = "no trigger"
            continue
        sign = direction[i]

        def to_ratio(distance):
            """Price of a point at `distance` against us, relative to the original entry."""
            return 1.0 + sign * distance / 100.0

        if entry_mode == "market":
            fill = k
            entry_distance = -close_pct[i, k]           # entering at the close of that candle
        else:
            # The signal price is the more extreme of the close and the band (VbsSignalShort), and
            # a limit order can only fill from the NEXT candle on.
            fill = -1
            entry_distance = np.nan
            for j in range(k + 1, min(window, width - 1) + 1):
                source = j if entry_mode == "riding" else k
                limit = np.nanmax([band_pct[i, source], -close_pct[i, source]])
                if not np.isfinite(limit):
                    continue
                if adverse[i, j] >= limit:      # price came back to the order
                    fill = j
                    entry_distance = limit
                    break
            if fill < 0:
                continue

        if not np.isfinite(entry_distance):
            continue
        base = to_ratio(entry_distance)
        if base <= 0:
            continue

        rest = slice(fill + 1, width)
        if rest.start >= width:
            continue
        new_adverse = (to_ratio(adverse[i, rest]) / base - 1.0) * sign * 100.0
        new_favourable = (1.0 - to_ratio(-favourable[i, rest]) / base) * sign * 100.0

        stop_hits = np.flatnonzero(new_adverse >= stop_pct)
        target_hits = np.flatnonzero(new_favourable >= target_pct)
        first_stop = stop_hits[0] if len(stop_hits) else None
        first_target = target_hits[0] if len(target_hits) else None

        filled[i] = True
        wait[i] = fill
        # Positive = entered further against the position than the band break did, i.e. a better price.
        entry_gain[i] = entry_distance
        if first_target is not None and (first_stop is None or first_target < first_stop):
            result[i] = target_pct
            outcome[i] = "target"
        elif first_stop is not None:
            result[i] = -stop_pct
            outcome[i] = "stop"
        else:
            result[i] = (1.0 - to_ratio(-close_pct[i, -1]) / base) * sign * 100.0
            outcome[i] = "open"

    return pd.DataFrame({
        "event_id": events["event_id"].to_numpy(),
        "side": events["side"].to_numpy(),
        "filled": filled,
        "wait": wait,
        "entry_gain_pct": entry_gain,
        "outcome": outcome,
        "result_pct": np.where(filled, result, 0.0),
    })


def summarise(name, frame, total_events):
    taken = frame[frame["filled"]]
    if not len(taken):
        return {"rule": name, "trades": 0, "fill%": 0.0, "win%": np.nan, "avg wait": np.nan,
                "entry gain%": np.nan, "per trade%": np.nan, "expectancy%": 0.0}
    wins = int((taken["outcome"] == "target").sum())
    losses = int((taken["outcome"] == "stop").sum())
    return {
        "rule": name,
        "trades": len(taken),
        "fill%": 100.0 * len(taken) / total_events,
        "win%": 100.0 * wins / max(wins + losses, 1),
        "avg wait": taken["wait"].mean(),
        "entry gain%": taken["entry_gain_pct"].mean(),
        "per trade%": taken["result_pct"].mean(),
        "expectancy%": frame["result_pct"].mean(),
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--data", required=True)
    parser.add_argument("--window", type=int, default=9, help="candles the signal stays armed (EntryRemoveTime)")
    parser.add_argument("--stop", type=float, default=2.5, help="stop-loss percentage")
    parser.add_argument("--target", type=float, default=1.8, help="take-profit percentage")
    parser.add_argument("--entry", default="limit", choices=["limit", "riding", "market"])
    parser.add_argument("--side", default="both", choices=["both", "short", "long"])
    parser.add_argument("--out", default="")
    args = parser.parse_args()

    events, panel = load(args.data)
    if args.side != "both":
        events = events[events["side"] == args.side].reset_index(drop=True)
        panel = panel[panel["event_id"].isin(set(events["event_id"]))]

    matrices, horizon = to_matrices(events, panel, COLUMNS)
    print(f"{len(events)} events | horizon {horizon} candles | armed window {args.window} | "
          f"stop {args.stop}% | target {args.target}% | entry {args.entry} | side {args.side}\n")

    rows = []
    for name, triggers in build_rules(matrices, args.window).items():
        frame = evaluate(events, matrices, triggers, args.window, args.stop, args.target, args.entry)
        rows.append(summarise(name, frame, len(events)))

    table = pd.DataFrame(rows).sort_values("expectancy%", ascending=False)
    with pd.option_context("display.width", 220, "display.max_columns", 20):
        print(table.to_string(index=False, float_format=lambda v: f"{v:8.3f}"))

    if args.out:
        table.to_csv(args.out, index=False)
        print(f"\nwritten to {args.out}")


if __name__ == "__main__":
    sys.exit(main())
