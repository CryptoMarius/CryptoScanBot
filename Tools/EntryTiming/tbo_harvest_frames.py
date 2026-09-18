"""
Harvesting labelled days from video frames of the reference indicator.

A frame of the reference chart is a complete verdict for every day it shows: the days it marks AND
the days it leaves alone. That is what the search for the missing condition needs, and it is what a
handful of remembered dates can never give.

WHAT WORKS, AND WHAT DOES NOT (measured 18-09-2026)

    frames      pulling the frames out and grouping them into chart segments works; one stacked
                image of the segment headers names every coin in the video in a single look
    dating      correlating the frame's price curve against ONE known coin lands within a few
                percent of the right pitch - not enough. Six percent off puts the last candle a
                week out, and a date that is a week out is a label that lies. The trustworthy
                anchor is the OHLC the chart prints for the candle under the crosshair: four
                numbers that match exactly one day in the database.
    identifying  correlating against ALL coins does NOT work. Crypto moves together: over six
                months NEAR scores 0.724 and INJ 0.737 on the same frame, so the runner-up wins.
                The coin has to be read from the header, which is one look per segment.
    dots        the near-white blob detector finds some of the marks and misses others, depending
                on whether the mark overlaps a wick. It needs checking by eye per frame.

So this is a helper for a frame-by-frame read, not an unattended harvester. The one window that was
harvested with it (NEAR, april to september 2026) was verified by hand against the crosshair date
before any of it went into the labels.

Usage:
    python tbo_harvest_frames.py --frame <file> --symbol NEARUSDT.PERP
"""

import argparse
import datetime
import json
import sqlite3
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

DEFAULT_DB = r"E:\CryptoScanBot\Data\Binance\Perpetual\Binance Perpetual.db"
EPOCH = datetime.datetime(2010, 1, 4)
DAILY_INTERVAL_ID = 15

# The candle colours of the reference's theme, sampled from its own frames.
GREEN = dict(green_min=110, red_max=115, blue_low=60, blue_high=190, margin=55)
RED = dict(red_min=155, green_max=105, blue_max=140, margin=70)

# A frame is only used when its best match is this much better than the second best coin.
MATCH_MINIMUM = 0.97
MATCH_MARGIN = 0.010


def candle_mask(image):
    """Which pixels belong to a candle body or wick."""
    red, green, blue = image[:, :, 0], image[:, :, 1], image[:, :, 2]
    up = ((green > GREEN["green_min"]) & (red < GREEN["red_max"])
          & (blue > GREEN["blue_low"]) & (blue < GREEN["blue_high"])
          & (green - red > GREEN["margin"]))
    down = ((red > RED["red_min"]) & (green < RED["green_max"])
            & (blue < RED["blue_max"]) & (red - green > RED["margin"]))
    return up | down


def find_candles(mask, minimum_columns=2):
    """Group the columns that hold candle pixels into candles.

    Returns a list of (centre_x, top_y, bottom_y). The top and bottom are the wick ends, which is
    what the daily high and low are drawn as.
    """
    filled = mask.any(axis=0)
    candles, start = [], None
    for x in range(len(filled) + 1):
        here = filled[x] if x < len(filled) else False
        if here and start is None:
            start = x
        elif not here and start is not None:
            if x - start >= minimum_columns:
                column = mask[:, start:x]
                rows = np.nonzero(column.any(axis=1))[0]
                candles.append(((start + x - 1) / 2.0, int(rows[0]), int(rows[-1])))
            start = None
    return candles


def round_blobs(mask, smallest=6, largest=200):
    """Centres of the round blobs in the mask - the breakout dots."""
    height, width = mask.shape
    seen = np.zeros_like(mask, bool)
    found = []
    for y0, x0 in zip(*np.nonzero(mask)):
        if seen[y0, x0]:
            continue
        queue, pixels = deque([(y0, x0)]), []
        seen[y0, x0] = True
        while queue:
            y, x = queue.popleft()
            pixels.append((y, x))
            if len(pixels) > largest * 3:
                break
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < height and 0 <= nx < width and not seen[ny, nx] and mask[ny, nx]:
                    seen[ny, nx] = True
                    queue.append((ny, nx))
        if not smallest <= len(pixels) <= largest:
            continue
        ys = [p[0] for p in pixels]
        xs = [p[1] for p in pixels]
        tall, wide = max(ys) - min(ys) + 1, max(xs) - min(xs) + 1
        if tall < 3 or wide < 3 or tall > 18 or wide > 18 or abs(tall - wide) > 4:
            continue
        if len(pixels) / (tall * wide) < 0.45:
            continue
        found.append((sum(xs) / len(xs), sum(ys) / len(ys)))
    return found


def load_daily_series(database):
    """Every symbol's daily highs and lows, at real prices."""
    connection = sqlite3.connect("file:" + database.replace("\\", "/") + "?mode=ro", uri=True)
    series = {}
    for symbol_id, name in connection.execute("select SymbolId, Name from Symbol"):
        dates, highs, lows = [], [], []
        for open_time, ticks, high, low in connection.execute(
                "select OpenTime, Ticks, High, Low from Candle "
                "where SymbolId = ? and IntervalId = ? order by OpenTime",
                (symbol_id, DAILY_INTERVAL_ID)):
            scale = 10 ** (ticks & 15)
            dates.append((EPOCH + datetime.timedelta(minutes=open_time)).date())
            highs.append(high / scale)
            lows.append(low / scale)
        if len(dates) > 60:
            series[name] = (dates, np.array(highs), np.array(lows))
    connection.close()
    return series


def correlate(frame_values, real_values):
    """Correlation of two equally long sequences, or -1 when one of them is flat."""
    a = frame_values - frame_values.mean()
    b = real_values - real_values.mean()
    denominator = np.sqrt((a * a).sum() * (b * b).sum())
    if denominator <= 0:
        return -1.0
    return float((a * b).sum() / denominator)


def identify(candles, series):
    """Which coin and which days this frame shows.

    The pixel Y axis runs downward, so a HIGH price is a SMALL y. Both a linear and a logarithmic
    price axis are tried, because the reference uses whichever suits the chart.
    """
    count = len(candles)
    tops = np.array([-c[1] for c in candles], dtype=float)
    bottoms = np.array([-c[2] for c in candles], dtype=float)
    shape = np.concatenate([tops, bottoms])

    scored = []
    for name, (dates, highs, lows) in series.items():
        if len(dates) < count:
            continue
        best = (-1.0, None, None)
        for scale in ("linear", "log"):
            high_values = np.log(highs) if scale == "log" else highs
            low_values = np.log(lows) if scale == "log" else lows
            for start in range(0, len(dates) - count + 1):
                window = np.concatenate([high_values[start:start + count],
                                         low_values[start:start + count]])
                score = correlate(shape, window)
                if score > best[0]:
                    best = (score, start, scale)
        scored.append((best[0], name, best[1], best[2]))

    scored.sort(reverse=True)
    if not scored:
        return None
    best = scored[0]
    runner_up = scored[1][0] if len(scored) > 1 else -1.0
    if best[0] < MATCH_MINIMUM or best[0] - runner_up < MATCH_MARGIN:
        return None
    dates = series[best[1]][0]
    return {
        "symbol": best[1],
        "score": best[0],
        "runner_up": runner_up,
        "scale": best[3],
        "dates": dates[best[2]:best[2] + count],
    }


def read_frame(path, series, verbose=False):
    """One frame: which coin, which days, and which of those days carry a breakout dot."""
    image = np.asarray(Image.open(path).convert("RGB")).astype(int)
    # The plot area, away from the toolbar on the left, the watchlist and price axis on the right,
    # the header at the top and the date axis with the webcam circle at the bottom.
    area = image[140:960, 95:1185]

    mask = candle_mask(area)
    if mask.sum() < 4000:
        return None
    candles = find_candles(mask)
    if not 40 <= len(candles) <= 260:
        return None

    match = identify(candles, series)
    if match is None:
        if verbose:
            print(f"  {Path(path).name}: {len(candles)} candles, geen overtuigende match")
        return None

    red, green, blue = area[:, :, 0], area[:, :, 1], area[:, :, 2]
    white = ((red > 195) & (green > 195) & (blue > 195)
             & (abs(red - green) < 26) & (abs(green - blue) < 26) & (abs(red - blue) < 26))
    centres = [c[0] for c in candles]
    marked = []
    for x, _y in round_blobs(white):
        nearest = int(np.argmin([abs(x - c) for c in centres]))
        if abs(x - centres[nearest]) <= 6:
            marked.append(match["dates"][nearest])

    return {
        "frame": Path(path).name,
        "symbol": match["symbol"],
        "score": round(match["score"], 4),
        "runner_up": round(match["runner_up"], 4),
        "scale": match["scale"],
        "from": str(match["dates"][0]),
        "to": str(match["dates"][-1]),
        "candles": len(candles),
        "dots": sorted({str(d) for d in marked}),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--frames", required=True)
    parser.add_argument("--db", default=DEFAULT_DB)
    parser.add_argument("--out")
    parser.add_argument("--only")
    parser.add_argument("--verbose", action="store_true")
    arguments = parser.parse_args()

    series = load_daily_series(arguments.db)
    print(f"{len(series)} munten met dagcandles geladen")

    paths = sorted(Path(arguments.frames).glob("*.jpg"))
    if arguments.only:
        paths = [p for p in paths if p.name == arguments.only]

    harvested = []
    for path in paths:
        result = read_frame(str(path), series, arguments.verbose)
        if result is None:
            continue
        harvested.append(result)
        print(f"  {result['frame']}: {result['symbol']:16} {result['from']} t/m {result['to']} "
              f"({result['candles']} candles, match {result['score']:.4f} tegen "
              f"{result['runner_up']:.4f}) stippen: {', '.join(result['dots']) or 'geen'}")

    if arguments.out:
        Path(arguments.out).write_text(json.dumps(harvested, indent=1, ensure_ascii=False),
                                       encoding="utf-8")
        print(f"\n{len(harvested)} frames geoogst -> {arguments.out}")


if __name__ == "__main__":
    main()
