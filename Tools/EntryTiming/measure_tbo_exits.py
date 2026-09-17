"""
Scoring the TBO entries against the candle database, to find out which stop loss and take profit
fit them on the higher intervals.

The strategy itself is rebuilt here in a few lines, exactly as the scanner computes it
(TboIndicatorExtension + TboBase): four moving averages on the close, a pivot high/low with a fixed
number of candles left and right, and two triggers - the cloud cross (the triangles on the chart)
and the break through the last confirmed level (the dots).

Every signal is then replayed forward on the candles of its own interval: entry at the open of the
next candle, and the first of stop / target that the candle range touches decides the trade. When a
candle touches both, the stop wins - the pessimistic reading, because a candle does not say in which
order it went there. A trade that reaches neither within the horizon is closed at the last close, so
it counts for what it was worth instead of disappearing.

This is not the emulator: there is no order book, no DCA ladder, no capital limit and no competing
position per symbol. It answers one question only - which stop/target pair a signal is worth - and
it answers it in seconds instead of hours.

Usage:
    python measure_tbo_exits.py --intervals 1h 2h 4h 1d --from 2026-01-01 --to 2026-09-01
"""

import argparse
import datetime
import json
import sys
from pathlib import Path

import numpy as np
import pandas as pd

import candledb

DEFAULT_DB = Path(r"E:\CryptoScanBot\Data\Emulator\Binance Perpetual.db")
DEFAULT_RUN_CONFIG = Path(r"E:\CryptoScanBot\Data\Emulator\SessionSwing\CryptoScanBot-Emulator.json")
DEFAULT_OUTPUT = Path(r"E:\CryptoScanBot\Data\Reports\EntryTiming")

# The four lengths of the cloud and the pivot window, straight from TboSettings.
FAST_EMA = 20
SECOND_EMA = 40
MEDIUM_SMA = 50
SLOW_SMA = 150
PIVOT_LEFT = 5
PIVOT_RIGHT = 5

# The grid that is scored. Percentages of the entry price, like the scanner's own settings.
STOPS = [1.0, 1.5, 2.0, 3.0, 4.0, 6.0, 9.0, 12.0, 15.0, 20.0]
TARGETS = [1.0, 1.5, 2.0, 3.0, 4.0, 6.0, 8.0, 12.0, 18.0, 24.0, 30.0]

# Round trip cost in percent: Binance perpetual taker is 0.04% per side.
ROUND_TRIP_COST = 0.08

# How long a position may stay open, in days - MaxPositionDurationDays of the swing session.
MAX_DURATION_DAYS = 14


def ema(values, length):
    """EMA on the close with an SMA seed, the way Skender.Stock.Indicators computes it."""
    result = np.full(len(values), np.nan)
    if len(values) < length:
        return result
    seed = values[:length].mean()
    result[length - 1] = seed
    factor = 2.0 / (length + 1.0)
    previous = seed
    for i in range(length, len(values)):
        previous = (values[i] - previous) * factor + previous
        result[i] = previous
    return result


def sma(values, length):
    return pd.Series(values).rolling(length).mean().to_numpy()


def pivot_levels(highs, lows, left, right):
    """The price of the last CONFIRMED pivot high and low at every candle.

    A pivot sits `right` candles back and is strictly higher (lower) than every other candle in the
    window, so the level is only known once its right-hand candles are in - which is why the arrays
    below are filled forward from the confirming candle, never from the pivot itself.
    """
    count = len(highs)
    pivot_high = np.full(count, np.nan)
    pivot_low = np.full(count, np.nan)
    window = left + right + 1

    last_high = np.nan
    last_low = np.nan
    for i in range(count):
        if i + 1 >= window:
            candidate = i - right
            window_high = highs[i + 1 - window:i + 1]
            window_low = lows[i + 1 - window:i + 1]
            value_high = highs[candidate]
            value_low = lows[candidate]
            others = np.arange(i + 1 - window, i + 1) != candidate
            if np.all(window_high[others] < value_high):
                last_high = value_high
            if np.all(window_low[others] > value_low):
                last_low = value_low
        pivot_high[i] = last_high
        pivot_low[i] = last_low
    return pivot_high, pivot_low


def find_signals(frame, trigger):
    """Indices of the candles that fire, per side.

    Returns two arrays of candle indices: longs and shorts. The rules are TboBase with everything
    that is off by default left off - no RSI, no volume, no cloud width, no slow line slope, no
    level age and no breakout buffer.
    """
    close = frame["close"].to_numpy(dtype=float)
    high = frame["high"].to_numpy(dtype=float)
    low = frame["low"].to_numpy(dtype=float)

    fast = ema(close, FAST_EMA)
    second = ema(close, SECOND_EMA)
    medium = sma(close, MEDIUM_SMA)
    slow = sma(close, SLOW_SMA)

    ready = ~(np.isnan(fast) | np.isnan(second) | np.isnan(medium) | np.isnan(slow))
    lines = np.vstack([fast, second, medium, slow])
    cloud_top = np.where(ready, np.max(np.nan_to_num(lines, nan=-np.inf), axis=0), np.nan)
    cloud_bottom = np.where(ready, np.min(np.nan_to_num(lines, nan=np.inf), axis=0), np.nan)

    cloud_up = ready & (fast > second)
    cloud_down = ready & (fast < second)

    previous_up = np.zeros(len(close), dtype=bool)
    previous_down = np.zeros(len(close), dtype=bool)
    previous_up[1:] = cloud_up[:-1]
    previous_down[1:] = cloud_down[:-1]
    previous_ready = np.zeros(len(close), dtype=bool)
    previous_ready[1:] = ready[:-1]

    if trigger == "cross":
        longs = cloud_up & previous_ready & ~previous_up
        shorts = cloud_down & previous_ready & ~previous_down
        return np.flatnonzero(longs), np.flatnonzero(shorts)

    pivot_high, pivot_low = pivot_levels(high, low, PIVOT_LEFT, PIVOT_RIGHT)
    close_previous = np.full(len(close), np.nan)
    close_previous[1:] = close[:-1]

    outside_top = close > cloud_top
    outside_bottom = close < cloud_bottom

    longs = (cloud_up & outside_top & ~np.isnan(pivot_high)
             & (close > pivot_high) & (close_previous <= pivot_high))
    shorts = (cloud_down & outside_bottom & ~np.isnan(pivot_low)
              & (close < pivot_low) & (close_previous >= pivot_low))
    return np.flatnonzero(longs), np.flatnonzero(shorts)


def collect_paths(frame, indices, side, horizon):
    """For every signal: the entry price and the running best/worst excursion after it, in percent.

    The two matrices are cumulative maxima, so the first candle that reaches a given stop or target
    is a searchsorted away instead of a walk.
    """
    open_price = frame["open"].to_numpy(dtype=float)
    high = frame["high"].to_numpy(dtype=float)
    low = frame["low"].to_numpy(dtype=float)
    close = frame["close"].to_numpy(dtype=float)
    count = len(close)

    usable = [i for i in indices if i + 1 < count]
    if not usable:
        return None

    entries = np.array([open_price[i + 1] for i in usable], dtype=float)
    favourable = np.zeros((len(usable), horizon))
    adverse = np.zeros((len(usable), horizon))
    step_up = np.zeros((len(usable), horizon))
    step_down = np.zeros((len(usable), horizon))
    final = np.zeros(len(usable))
    length = np.zeros(len(usable), dtype=int)

    for row, i in enumerate(usable):
        start = i + 1
        stop = min(count, start + horizon)
        span = stop - start
        entry = entries[row]
        if entry <= 0:
            favourable[row, :] = 0
            adverse[row, :] = 0
            continue
        window_high = high[start:stop]
        window_low = low[start:stop]
        if side == "long":
            up = 100.0 * (window_high - entry) / entry
            down = 100.0 * (entry - window_low) / entry
            last = 100.0 * (close[stop - 1] - entry) / entry
        else:
            up = 100.0 * (entry - window_low) / entry
            down = 100.0 * (window_high - entry) / entry
            last = 100.0 * (entry - close[stop - 1]) / entry
        favourable[row, :span] = np.maximum.accumulate(up)
        adverse[row, :span] = np.maximum.accumulate(down)
        step_up[row, :span] = up
        step_down[row, :span] = down
        if span < horizon:
            favourable[row, span:] = favourable[row, span - 1]
            adverse[row, span:] = adverse[row, span - 1]
            step_up[row, span:] = np.nan
            step_down[row, span:] = np.nan
        final[row] = last
        length[row] = span

    return {
        "entries": entries,
        "favourable": favourable,
        "adverse": adverse,
        "step_up": step_up,
        "step_down": step_down,
        "final": final,
        "length": length,
    }


def first_hit(cumulative, threshold):
    """The first column that reaches the threshold, or -1 when it never does."""
    reached = cumulative >= threshold
    index = reached.argmax(axis=1)
    return np.where(reached.any(axis=1), index, -1)


def score(paths, stops, targets, cost):
    """The grid: per stop/target pair the number of wins, losses and the average result."""
    favourable = paths["favourable"]
    adverse = paths["adverse"]
    final = paths["final"]

    target_hit = {t: first_hit(favourable, t) for t in targets}
    stop_hit = {s: first_hit(adverse, s) for s in stops}

    rows = []
    for s in stops:
        stop_index = stop_hit[s]
        for t in targets:
            target_index = target_hit[t]
            # A candle that touches both counts as the stop: the candle does not say in which order.
            won = (target_index >= 0) & ((stop_index < 0) | (target_index < stop_index))
            lost = (stop_index >= 0) & ((target_index < 0) | (stop_index <= target_index))
            open_end = ~won & ~lost
            result = np.where(won, t, np.where(lost, -s, final)) - cost
            rows.append({
                "stop": s,
                "target": t,
                "trades": len(result),
                "won": int(won.sum()),
                "lost": int(lost.sum()),
                "openend": int(open_end.sum()),
                "winrate": 100.0 * won.sum() / max(1, len(result)),
                "average": float(result.mean()),
                "total": float(result.sum()),
            })
    return rows



# The trailing variants that are scored: the hard stop the position starts with, how far in profit
# the lock arms itself, and how far behind the best price the stop then follows. They are the
# scanner's own MoveSlToBreakEven settings with the method on TrailingPercentage.
TRAIL_STOPS = [3.0, 4.0, 6.0]
TRAIL_TRIGGERS = [3.0, 4.0, 6.0, 8.0, 12.0, 16.0, 20.0]
TRAIL_DISTANCES = [2.0, 3.0, 4.0, 6.0, 8.0]


def trailing_level(best, trail, side):
    """Where the trailing stop sits, as a profit percentage, given the best profit reached so far.

    The scanner keeps the stop `trail` percent of the PRICE behind the best price, so the level is
    computed in price space and handed back as a profit percentage - which for a short is not the
    mirror image of the long.
    """
    if side == "long":
        return 100.0 * ((1.0 + best / 100.0) * (1.0 - trail / 100.0) - 1.0)
    return 100.0 * (1.0 - (1.0 - best / 100.0) * (1.0 + trail / 100.0))


def score_trailing(paths, side, stops, triggers, distances, target, cost):
    """The same grid, but with the profit lock on: a hard stop until the trigger is reached, a stop
    that follows the price after it.

    The order inside a candle is the pessimistic one, like everywhere else in this tool: the stop is
    tested before the target, and the lock only arms AFTER the candle that reached the trigger, so a
    candle can never both arm the lock and be saved by it.
    """
    step_up = paths["step_up"]
    step_down = paths["step_down"]
    final = paths["final"]
    count, horizon = step_up.shape

    rows = []
    for stop in stops:
        for trigger in triggers:
            for trail in distances:
                open_position = np.ones(count, dtype=bool)
                armed = np.zeros(count, dtype=bool)
                best = np.zeros(count)
                level = np.full(count, -stop)
                result = np.zeros(count)
                exit_reason = np.zeros(count, dtype=int)   # 0 = still open, 1 = stop, 2 = target

                for k in range(horizon):
                    up = step_up[:, k]
                    down = step_down[:, k]
                    worst = -down

                    effective = np.where(armed, np.maximum(-stop, level), -stop)
                    hit_stop = open_position & (worst <= effective)
                    result = np.where(hit_stop, effective, result)
                    exit_reason = np.where(hit_stop, 1, exit_reason)
                    open_position = open_position & ~hit_stop

                    hit_target = open_position & (up >= target)
                    result = np.where(hit_target, target, result)
                    exit_reason = np.where(hit_target, 2, exit_reason)
                    open_position = open_position & ~hit_target

                    best = np.where(open_position, np.maximum(best, np.nan_to_num(up, nan=-1e9)), best)
                    armed = armed | (open_position & (worst >= trigger))
                    level = trailing_level(best, trail, side)

                result = np.where(open_position, final, result) - cost
                rows.append({
                    "stop": stop, "arm": trigger, "trail": trail, "target": target,
                    "trades": count,
                    "won": int((result > 0).sum()),
                    "stopped": int((exit_reason == 1).sum()),
                    "target_hit": int((exit_reason == 2).sum()),
                    "openend": int(open_position.sum()),
                    "winrate": 100.0 * (result > 0).sum() / max(1, count),
                    "average": float(result.mean()),
                    "total": float(result.sum()),
                })
    return rows


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--db", default=str(DEFAULT_DB))
    parser.add_argument("--run-config", default=str(DEFAULT_RUN_CONFIG))
    parser.add_argument("--intervals", nargs="+", default=["1h", "2h", "4h", "1d"])
    parser.add_argument("--triggers", nargs="+", default=["cross", "breakout"])
    parser.add_argument("--from", dest="date_from", default="2026-01-01")
    parser.add_argument("--to", dest="date_to", default="2026-09-01")
    parser.add_argument("--cost", type=float, default=ROUND_TRIP_COST)
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT))
    parser.add_argument("--tag", default="tbo")
    parser.add_argument("--mode", choices=["grid", "trailing"], default="grid")
    parser.add_argument("--trailing-target", type=float, default=30.0)
    arguments = parser.parse_args()

    start = datetime.datetime.strptime(arguments.date_from, "%Y-%m-%d")
    end = datetime.datetime.strptime(arguments.date_to, "%Y-%m-%d")

    symbols = None
    run_config = Path(arguments.run_config)
    if run_config.exists():
        with open(run_config, encoding="utf-8-sig") as stream:
            symbols = set(json.load(stream).get("Symbols", []))

    connection = candledb.open_readonly(arguments.db)
    output = Path(arguments.output)
    output.mkdir(parents=True, exist_ok=True)

    grid_rows = []
    excursion_rows = []

    for interval in arguments.intervals:
        interval_id = candledb.INTERVAL_IDS[interval]
        minutes = candledb.INTERVAL_MINUTES[interval]
        horizon = max(2, int(MAX_DURATION_DAYS * 1440 / minutes))
        available = candledb.list_symbols(connection, interval_id, minimum_candles=SLOW_SMA + 50)
        if symbols:
            available = [row for row in available if row[1] in symbols]

        for trigger in arguments.triggers:
            collected = {"long": [], "short": []}
            for symbol_id, name, _ in available:
                # The moving averages need history before the first signal, so the candles start
                # well before the measured window and the signals are cut back to it afterwards.
                warmup = datetime.timedelta(minutes=minutes * (SLOW_SMA + PIVOT_LEFT + PIVOT_RIGHT + 5))
                frame = candledb.load_candles(connection, symbol_id, interval_id, start - warmup, end)
                if len(frame) < SLOW_SMA + 10:
                    continue
                opentime = frame["opentime"].to_numpy()
                inside = opentime >= candledb.datetime_to_minutes(start)

                longs, shorts = find_signals(frame, trigger)
                longs = longs[inside[longs]]
                shorts = shorts[inside[shorts]]

                for side, indices in (("long", longs), ("short", shorts)):
                    if len(indices) == 0:
                        continue
                    paths = collect_paths(frame, indices, side, horizon)
                    if paths:
                        collected[side].append(paths)

            for side in ("long", "short"):
                if not collected[side]:
                    print(f"{interval} {trigger} {side}: no signals")
                    continue
                merged = {
                    "favourable": np.vstack([p["favourable"] for p in collected[side]]),
                    "adverse": np.vstack([p["adverse"] for p in collected[side]]),
                    "step_up": np.vstack([p["step_up"] for p in collected[side]]),
                    "step_down": np.vstack([p["step_down"] for p in collected[side]]),
                    "final": np.concatenate([p["final"] for p in collected[side]]),
                }
                count = len(merged["final"])
                best_favourable = merged["favourable"][:, -1]
                worst_adverse = merged["adverse"][:, -1]
                excursion_rows.append({
                    "interval": interval, "trigger": trigger, "side": side, "signals": count,
                    "favourable_p25": float(np.percentile(best_favourable, 25)),
                    "favourable_p50": float(np.percentile(best_favourable, 50)),
                    "favourable_p75": float(np.percentile(best_favourable, 75)),
                    "adverse_p25": float(np.percentile(worst_adverse, 25)),
                    "adverse_p50": float(np.percentile(worst_adverse, 50)),
                    "adverse_p75": float(np.percentile(worst_adverse, 75)),
                })
                print(f"{interval} {trigger} {side}: {count} signals, "
                      f"median best {np.median(best_favourable):.2f}% / worst {np.median(worst_adverse):.2f}%")

                if arguments.mode == "grid":
                    scored = score(merged, STOPS, TARGETS, arguments.cost)
                else:
                    scored = score_trailing(merged, side, TRAIL_STOPS, TRAIL_TRIGGERS,
                                            TRAIL_DISTANCES, arguments.trailing_target, arguments.cost)
                for row in scored:
                    row.update({"interval": interval, "trigger": trigger, "side": side})
                    grid_rows.append(row)

    grid = pd.DataFrame(grid_rows)
    excursions = pd.DataFrame(excursion_rows)
    grid_file = output / f"{arguments.tag}-grid.csv"
    excursion_file = output / f"{arguments.tag}-excursions.csv"
    grid.to_csv(grid_file, index=False)
    excursions.to_csv(excursion_file, index=False)
    print(f"\nWritten: {grid_file}\n         {excursion_file}")

    if not grid.empty and arguments.mode == "grid":
        both = grid.groupby(["interval", "trigger", "stop", "target"], as_index=False).agg(
            trades=("trades", "sum"), won=("won", "sum"), total=("total", "sum"))
        both["average"] = both["total"] / both["trades"]
        both["winrate"] = 100.0 * both["won"] / both["trades"]
        for (interval, trigger), block in both.groupby(["interval", "trigger"]):
            best = block.sort_values("average", ascending=False).head(5)
            print(f"\n{interval} {trigger} - best five of the grid (both sides together):")
            for _, row in best.iterrows():
                print(f"  stop {row['stop']:>5}  target {row['target']:>5}  "
                      f"{int(row['trades']):>6} trades  win {row['winrate']:5.1f}%  "
                      f"average {row['average']:+.2f}% per trade")


if __name__ == "__main__":
    sys.exit(main())
