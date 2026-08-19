"""
The band range index, rebuilt from BandRangeTracker.cs so the measurement can use the same number
the scanner uses.

    index = median band width over the last 500 candles  x  excursion ratio over the last 25 excursions

An excursion starts on a candle that CLOSES outside a Bollinger band, runs for at most 48 candles,
and ends when the close is back at the middle line. Favourable is how far price moved in the
intended direction, adverse how far it moved against it; the ratio is the sum of one over the sum
of the other. At most one excursion per side is open at a time.

The index only exists once there are at least 100 widths and 10 completed excursions; before that
it is NaN, exactly as the C# version returns null.

Returned as a value PER CANDLE, using only excursions that were already complete at that candle -
so it can be used as a filter without looking into the future.
"""

import numpy as np
import pandas as pd

WIDTH_WINDOW = 500
EXCURSION_WINDOW = 25
MAXIMUM_HOLD = 48
MINIMUM_MEASUREMENTS = 10
MINIMUM_WIDTHS = 100


def band_range_index(high, low, close, bb_middle, bb_upper, bb_lower):
    high = np.asarray(high, dtype=float)
    low = np.asarray(low, dtype=float)
    close = np.asarray(close, dtype=float)
    middle = np.asarray(bb_middle, dtype=float)
    upper = np.asarray(bb_upper, dtype=float)
    lower = np.asarray(bb_lower, dtype=float)
    n = len(close)

    index = np.full(n, np.nan)
    width_out = np.full(n, np.nan)
    ratio_out = np.full(n, np.nan)
    count_out = np.zeros(n, dtype=int)

    widths = []
    favourable, adverse = [], []
    open_long = None      # dict(entry, bars_left, best, worst)
    open_short = None

    for i in range(n):
        if not (np.isfinite(middle[i]) and np.isfinite(upper[i]) and np.isfinite(lower[i])):
            continue
        if middle[i] <= 0 or upper[i] <= lower[i] or lower[i] <= 0:
            continue

        widths.append(100.0 * (upper[i] / lower[i] - 1.0))
        if len(widths) > WIDTH_WINDOW:
            widths.pop(0)

        # Advance the running measurements first; the candle that closes one still counts.
        closed_long = closed_short = False
        if open_long is not None:
            open_long["best"] = max(open_long["best"], 100.0 * (high[i] / open_long["entry"] - 1.0))
            open_long["worst"] = min(open_long["worst"], 100.0 * (low[i] / open_long["entry"] - 1.0))
            open_long["bars_left"] -= 1
            if close[i] >= middle[i] or open_long["bars_left"] <= 0:
                favourable.append(abs(open_long["best"]))
                adverse.append(abs(open_long["worst"]))
                open_long = None
                closed_long = True

        if open_short is not None:
            open_short["best"] = min(open_short["best"], 100.0 * (low[i] / open_short["entry"] - 1.0))
            open_short["worst"] = max(open_short["worst"], 100.0 * (high[i] / open_short["entry"] - 1.0))
            open_short["bars_left"] -= 1
            if close[i] <= middle[i] or open_short["bars_left"] <= 0:
                favourable.append(abs(open_short["best"]))
                adverse.append(abs(open_short["worst"]))
                open_short = None
                closed_short = True

        if len(favourable) > EXCURSION_WINDOW:
            favourable.pop(0)
            adverse.pop(0)

        # Then look for a new touch, but not on a candle that just closed one on that side.
        if open_long is None and not closed_long and close[i] <= lower[i]:
            open_long = {"entry": close[i], "bars_left": MAXIMUM_HOLD, "best": 0.0, "worst": 0.0}
        if open_short is None and not closed_short and close[i] >= upper[i]:
            open_short = {"entry": close[i], "bars_left": MAXIMUM_HOLD, "best": 0.0, "worst": 0.0}

        if len(widths) >= MINIMUM_WIDTHS:
            width_out[i] = float(np.median(widths))
        if len(favourable) >= MINIMUM_MEASUREMENTS:
            total_adverse = float(np.sum(adverse))
            if total_adverse > 0:
                ratio_out[i] = float(np.sum(favourable)) / total_adverse
        count_out[i] = len(favourable)
        if np.isfinite(width_out[i]) and np.isfinite(ratio_out[i]):
            index[i] = width_out[i] * ratio_out[i]

    return index, width_out, ratio_out, count_out
