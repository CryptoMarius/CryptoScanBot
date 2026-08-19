"""
Reading candles out of the scanner's candle database ("<Exchange> <Type>.db").

Layout (schema version 3):
    Candle(SymbolId, IntervalId, OpenTime, Ticks, Open, High, Low, Close, Volume)
    Symbol(SymbolId, ExchangeName, Name)

OpenTime is the number of minutes since 2010-01-04. Open/High/Low/Close are integers counting
price ticks; multiply by the symbol's PriceTickSize (kept in the main CryptoScanBot.db, not here)
to get a real price. Every measurement in this tool is relative, so the tick scale cancels out and
only the verification step needs the real prices.

IntervalId equals CryptoIntervalPeriod + 1, so 15m is 6.
"""

import datetime
import sqlite3

import numpy as np
import pandas as pd

EPOCH = datetime.datetime(2010, 1, 4)

INTERVAL_IDS = {
    "1m": 1, "2m": 2, "3m": 3, "5m": 4, "10m": 5, "15m": 6, "30m": 7, "1h": 8,
    "2h": 9, "3h": 10, "4h": 11, "6h": 12, "8h": 13, "12h": 14, "1d": 15, "1w": 16,
}


def open_readonly(path):
    return sqlite3.connect("file:" + str(path).replace("\\", "/") + "?mode=ro", uri=True)


def minutes_to_datetime(minutes):
    return EPOCH + datetime.timedelta(minutes=int(minutes))


def datetime_to_minutes(moment):
    return int((moment - EPOCH).total_seconds() // 60)


def list_symbols(connection, interval_id, minimum_candles=1000):
    """Symbols that have at least `minimum_candles` candles on this interval."""
    rows = connection.execute(
        "select s.SymbolId, s.Name, count(*) as n from Candle c "
        "join Symbol s on s.SymbolId = c.SymbolId "
        "where c.IntervalId = ? group by s.SymbolId, s.Name having n >= ? order by s.Name",
        (interval_id, minimum_candles),
    ).fetchall()
    return [(int(r[0]), r[1], int(r[2])) for r in rows]


def load_candles(connection, symbol_id, interval_id, start=None, end=None):
    """Candles for one symbol+interval as a DataFrame, oldest first.

    Prices stay in ticks (integers); `volume` is the real traded volume.
    """
    sql = ("select OpenTime, Open, High, Low, Close, Volume from Candle "
           "where SymbolId = ? and IntervalId = ?")
    params = [symbol_id, interval_id]
    if start is not None:
        sql += " and OpenTime >= ?"
        params.append(datetime_to_minutes(start))
    if end is not None:
        sql += " and OpenTime <= ?"
        params.append(datetime_to_minutes(end))
    sql += " order by OpenTime"

    rows = connection.execute(sql, params).fetchall()
    if not rows:
        return pd.DataFrame(columns=["opentime", "open", "high", "low", "close", "volume"])

    data = np.array(rows, dtype=float)
    frame = pd.DataFrame({
        "opentime": data[:, 0].astype(np.int64),
        "open": data[:, 1],
        "high": data[:, 2],
        "low": data[:, 3],
        "close": data[:, 4],
        "volume": data[:, 5],
    })
    return frame


def gap_mask(frame, interval_minutes):
    """True where the candle directly follows the previous one. A missing stretch of candles
    (the scanner does not always have an unbroken history) must not be treated as a price move."""
    step = frame["opentime"].diff().to_numpy()
    contiguous = np.ones(len(frame), dtype=bool)
    contiguous[1:] = step[1:] == interval_minutes
    return contiguous


INTERVAL_MINUTES = {
    "1m": 1, "2m": 2, "3m": 3, "5m": 5, "10m": 10, "15m": 15, "30m": 30, "1h": 60,
    "2h": 120, "3h": 180, "4h": 240, "6h": 360, "8h": 480, "12h": 720, "1d": 1440, "1w": 10080,
}
