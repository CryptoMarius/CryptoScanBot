"""Shared core for reading a reference frame once the coin is known."""
import datetime, sqlite3, warnings
import numpy as np
from PIL import Image
from collections import deque
warnings.filterwarnings('ignore')
EPOCH = datetime.datetime(2010, 1, 4)


def column_profile(path):
    """Per pixel column the top and bottom of the candles, with lines and header removed."""
    image = np.asarray(Image.open(path).convert('RGB')).astype(int)
    area = image[200:960, 100:1180]
    red, green, blue = area[:, :, 0], area[:, :, 1], area[:, :, 2]
    mask = (((green > 125) & (red < 95) & (green - blue > 18) & (green < 215))
            | ((red > 170) & (green < 90) & (red - blue > 60)))
    width = mask.shape[1]
    columns = np.arange(width)
    for y in range(mask.shape[0]):
        lit = columns[mask[y]]
        if len(lit) < 0.08 * width:
            continue
        if lit[-1] - lit[0] > 0.6 * width and len(lit) < 0.75 * width:
            mask[y, :] = False          # a level line, not candles
    rows = np.arange(mask.shape[0])
    top = np.full(width, np.nan)
    bottom = np.full(width, np.nan)
    for c in range(width):
        lit = rows[mask[:, c]]
        if len(lit):
            top[c], bottom[c] = lit[0], lit[-1]
    return top, bottom, area


def daily_series(symbol, database=r"E:\CryptoScanBot\Data\Binance\Perpetual\Binance Perpetual.db"):
    connection = sqlite3.connect("file:" + database.replace("\\", "/") + "?mode=ro", uri=True)
    row = connection.execute("select SymbolId from Symbol where Name=?", (symbol,)).fetchone()
    if row is None:
        return None
    dates, highs, lows = [], [], []
    for open_time, ticks, high, low in connection.execute(
            "select OpenTime,Ticks,High,Low from Candle where SymbolId=? and IntervalId=15 order by OpenTime",
            (row[0],)):
        scale = 10 ** (ticks & 15)
        dates.append((EPOCH + datetime.timedelta(minutes=open_time)).date())
        highs.append(high / scale)
        lows.append(low / scale)
    return dates, np.array(highs), np.array(lows)


def fit(top, bottom, series, pitches=None):
    """Which days this frame shows: searches pitch, offset and start date for ONE coin.

    Scored per pixel column rather than per aggregated day. Bucketing the columns into days first
    loses exactly the detail that tells one alignment from another, and the search then settles on a
    pitch that is a few percent off - which puts every date a week out by the end of the chart.
    """
    dates, highs, lows = series
    log_high, log_low = np.log(highs), np.log(lows)
    width = len(top)
    columns = np.arange(width)
    frame = np.concatenate([-top, -bottom])
    if pitches is None:
        pitches = np.arange(3.0, 14.01, 0.05)

    best = None
    for pitch in pitches:
        for offset in (0.0, pitch / 3, 2 * pitch / 3):
            day = np.floor((columns - offset) / pitch).astype(int)
            count = int(day.max() - day.min() + 1)
            if not 40 <= count <= 400 or count > len(dates):
                continue
            base = day - day.min()
            for start in range(0, len(dates) - count + 1):
                index = start + base
                window = np.concatenate([log_high[index], log_low[index]])
                keep = ~np.isnan(frame)
                x = frame[keep] - frame[keep].mean()
                y = window[keep] - window[keep].mean()
                denominator = np.sqrt((x * x).sum() * (y * y).sum())
                if denominator <= 0:
                    continue
                score = float((x * y).sum() / denominator)
                if best is None or score > best[0]:
                    best = (score, pitch, offset, start - day.min(), count)
    return best


def dot_dates(area, top, pitch, offset, start_date_index, dates):
    """The dates of the near-white round markers in this frame."""
    red, green, blue = area[:, :, 0], area[:, :, 1], area[:, :, 2]
    white = ((red > 195) & (green > 195) & (blue > 195)
             & (abs(red - green) < 26) & (abs(green - blue) < 26) & (abs(red - blue) < 26))
    height, width = white.shape
    seen = np.zeros_like(white, bool)
    found = []
    for y0, x0 in zip(*np.nonzero(white)):
        if seen[y0, x0]:
            continue
        queue, pixels = deque([(y0, x0)]), []
        seen[y0, x0] = True
        while queue:
            y, x = queue.popleft()
            pixels.append((y, x))
            if len(pixels) > 400:
                break
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < height and 0 <= nx < width and not seen[ny, nx] and white[ny, nx]:
                    seen[ny, nx] = True
                    queue.append((ny, nx))
        if not 6 <= len(pixels) <= 200:
            continue
        ys = [p[0] for p in pixels]
        xs = [p[1] for p in pixels]
        tall, wide = max(ys) - min(ys) + 1, max(xs) - min(xs) + 1
        if tall < 3 or wide < 3 or tall > 18 or wide > 18 or abs(tall - wide) > 4:
            continue
        if len(pixels) / (tall * wide) < 0.45:
            continue
        day = int(np.floor((sum(xs) / len(xs) - offset) / pitch))
        index = start_date_index + day
        if 0 <= index < len(dates):
            found.append(dates[index])
    return sorted(set(found))


def colour_columns(area):
    """Per pixel column how much green and how much red candle there is."""
    red, green, blue = area[:, :, 0], area[:, :, 1], area[:, :, 2]
    up = (green > 125) & (red < 95) & (green - blue > 18) & (green < 215)
    down = (red > 170) & (green < 90) & (red - blue > 60)
    return up.sum(axis=0), down.sum(axis=0)


def detrend(values, window=61):
    """Take the slow trend out, so the daily ripple decides the score instead of the shape."""
    present = ~np.isnan(values)
    filled = np.where(present, values, np.nanmean(values))
    result = values - np.convolve(filled, np.ones(window) / window, mode="same")
    result[~present] = np.nan
    return result


def align(top, bottom, area, series, opens, closes, anchor_x=905,
          pitches=np.arange(5.0, 9.01, 0.05)):
    """Which day sits under which pixel column.

    Two steps, because neither alone is enough. The detrended price curve picks the candidates -
    it is sharp on the pitch but lands a day either side on the phase. The candle COLOURS then
    decide: at the right alignment eight or nine days in ten are the right colour, at a one-day
    shift barely six, because a green day next to a red one is a coin flip.
    """
    dates, highs, lows = series
    log_high, log_low = np.log(highs), np.log(lows)
    columns = np.arange(len(top))
    present = ~np.isnan(top)
    frame_top, frame_bottom = detrend(-top), detrend(-bottom)
    green_columns, red_columns = colour_columns(area)
    really_green = closes >= opens

    candidates = []
    for pitch in pitches:
        day = np.floor((columns - anchor_x) / pitch).astype(int)
        for anchor in range(len(dates)):
            index = anchor + day
            usable = present & (index >= 0) & (index < len(dates))
            if usable.sum() < 300:
                continue
            safe = np.where(usable, index, 0)
            window_top = detrend(np.where(usable, log_high[safe], np.nan))
            window_bottom = detrend(np.where(usable, log_low[safe], np.nan))
            frame = np.concatenate([frame_top[usable], frame_bottom[usable]])
            real = np.concatenate([window_top[usable], window_bottom[usable]])
            keep = ~(np.isnan(frame) | np.isnan(real))
            if keep.sum() < 400:
                continue
            x = frame[keep] - frame[keep].mean()
            y = real[keep] - real[keep].mean()
            denominator = np.sqrt((x * x).sum() * (y * y).sum())
            if denominator <= 0:
                continue
            candidates.append((float((x * y).sum() / denominator), float(pitch), anchor))

    candidates.sort(reverse=True)
    best = None
    for score, pitch, anchor in candidates[:25]:
        day = np.floor((columns - anchor_x) / pitch).astype(int)
        right = total = 0
        for d in range(day.min(), day.max() + 1):
            here = day == d
            index = anchor + d
            if not here.any() or not 0 <= index < len(dates):
                continue
            green, red = green_columns[here].sum(), red_columns[here].sum()
            if green + red < 20:
                continue
            total += 1
            if (green > red) == bool(really_green[index]):
                right += 1
        if total < 40:
            continue
        agreement = right / total
        if best is None or agreement > best[0]:
            best = (agreement, score, pitch, anchor, total)
    return best
