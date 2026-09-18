"""Read one frame of the reference chart for a coin we know, and date its breakout dots.

The colour agreement is the acceptance test, and it is not a formality. At the right alignment
roughly nine days in ten carry the right colour (NEAR: 87.2%); at a one-day shift it falls to about
six in ten, because a green day next to a red one is a coin flip. Anything under ACCEPT is a frame
whose dates cannot be trusted, and a date that is a week out is a label that lies - so it is thrown
away rather than written down. Of the nine coins in the video of 18-09-2026 exactly one passed; the
others sit at 54 to 79 percent, mostly because the chart is covered in drawings.
"""
import datetime, sqlite3, sys
import numpy as np
from PIL import Image
from collections import deque
import _harvest_kern as K

ACCEPT = 0.80
DB = "E:/CryptoScanBot/Data/Binance/Perpetual/Binance Perpetual.db"
EPOCH = datetime.datetime(2010, 1, 4)


def opens_closes(symbol):
    connection = sqlite3.connect(f"file:{DB}?mode=ro", uri=True)
    symbol_id = connection.execute("select SymbolId from Symbol where Name=?", (symbol,)).fetchone()[0]
    opens, closes = [], []
    for ticks, open_price, close in connection.execute(
            "select Ticks,Open,Close from Candle where SymbolId=? and IntervalId=15 order by OpenTime",
            (symbol_id,)):
        scale = 10 ** (ticks & 15)
        opens.append(open_price / scale)
        closes.append(close / scale)
    return np.array(opens), np.array(closes)


def dots(area, pitch, anchor_x, anchor_date, threshold=185):
    """White round markers, dated. The far left holds the TradingView logo and the crosshair holds
    the mouse cursor, and both are white and round enough to pass for a mark."""
    red, green, blue = area[:, :, 0], area[:, :, 1], area[:, :, 2]
    white = ((red > threshold) & (green > threshold) & (blue > threshold)
             & (abs(red - green) < 30) & (abs(green - blue) < 30) & (abs(red - blue) < 30))
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
            if len(pixels) > 800:
                break
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < height and 0 <= nx < width and not seen[ny, nx] and white[ny, nx]:
                    seen[ny, nx] = True
                    queue.append((ny, nx))
        if not 8 <= len(pixels) <= 260:
            continue
        ys = [p[0] for p in pixels]
        xs = [p[1] for p in pixels]
        tall, wide = max(ys) - min(ys) + 1, max(xs) - min(xs) + 1
        if tall < 4 or wide < 4 or tall > 22 or wide > 22:
            continue
        if wide > tall * 2.6 or tall > wide * 2.6:
            continue
        if len(pixels) / (tall * wide) < 0.40:
            continue
        centre_x = sum(xs) / len(xs)
        if centre_x < 45 or centre_x > width - 20:
            continue                                   # chart edge and the logo
        if abs(centre_x - anchor_x) < 12 and min(ys) > height - 700:
            continue                                   # the mouse cursor on the crosshair
        found.append(anchor_date + datetime.timedelta(days=int(np.floor((centre_x - anchor_x) / pitch))))
    return sorted(set(found))


def read(frame, symbol, verbose=True):
    top, bottom, area = K.column_profile(frame)
    series = K.daily_series(symbol)
    o, c = opens_closes(symbol)
    best = K.align(top, bottom, area, series, o, c, pitches=np.arange(3.5, 12.01, 0.05))
    if best is None:
        return None
    agreement, score, pitch, anchor, days = best
    anchor_date = series[0][anchor]
    if agreement < ACCEPT:
        if verbose:
            print(f"  {symbol:14} VERWORPEN: kleur maar {agreement*100:.1f}%, de datums zijn niet te vertrouwen")
        return None
    marks = dots(area, pitch, 905, anchor_date)
    first = anchor_date + datetime.timedelta(days=int(np.floor((0 - 905) / pitch)))
    last = anchor_date + datetime.timedelta(days=int(np.floor((len(top) - 905) / pitch)))
    if verbose:
        print(f"  {symbol:14} kleur {agreement*100:5.1f}% over {days:3} dagen, pitch {pitch:5.2f}, "
              f"beeld {first} t/m {last}")
        print(f"     stippen: {', '.join(str(d) for d in marks) or 'geen'}")
    return dict(symbol=symbol, agreement=agreement, pitch=pitch, first=str(first), last=str(last),
                dots=[str(d) for d in marks])


if __name__ == "__main__":
    read(sys.argv[1], sys.argv[2])
