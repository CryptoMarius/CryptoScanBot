"""
Indicator re-implementations that match the C# scanner (Skender.Stock.Indicators) closely enough
to reason about entry timing.

Parameters are the ones the scanner actually uses (IntervalIndicatorHub.Init plus the saved
settings of the emulator session):
    Bollinger 20 / 2      RSI 14 (30/70)      Stochastic 14/3/3 (20/80)
    MACD 12/26/9          Parabolic SAR 0.02/0.2
    VBS bands: VWMA(hlc3, 50) +/- 2.5 * volume-weighted stdev
    ACS%     : 2.17 * SMA((high-low)/close*100, 50)

Keltner is NOT computed by the scanner today (the fields are commented out in CryptoData); it is
included here as a candidate, using the Skender defaults EMA 20 / ATR 10 / multiplier 2.

Everything works on numpy float arrays so a whole symbol can be processed in one pass.
"""

import numpy as np
import pandas as pd


def sma(values, length):
    return pd.Series(values, dtype=float).rolling(length).mean().to_numpy()


def ema(values, length):
    """EMA seeded with the SMA of the first `length` values, the way Skender does it."""
    values = np.asarray(values, dtype=float)
    out = np.full(values.shape, np.nan)
    if len(values) < length:
        return out
    k = 2.0 / (length + 1.0)
    prev = float(np.mean(values[:length]))
    out[length - 1] = prev
    for i in range(length, len(values)):
        prev = (values[i] - prev) * k + prev
        out[i] = prev
    return out


def wilder(values, length):
    """Wilder smoothing (used by ATR): seeded with the simple average."""
    values = np.asarray(values, dtype=float)
    out = np.full(values.shape, np.nan)
    if len(values) < length:
        return out
    prev = float(np.mean(values[:length]))
    out[length - 1] = prev
    for i in range(length, len(values)):
        prev = (prev * (length - 1) + values[i]) / length
        out[i] = prev
    return out


def rsi(close, length=14):
    """Wilder RSI. The price changes start at index 1, so the first value lands on index `length`."""
    close = np.asarray(close, dtype=float)
    n = len(close)
    out = np.full(n, np.nan)
    if n <= length:
        return out

    delta = np.diff(close)
    gain = np.where(delta > 0, delta, 0.0)
    loss = np.where(delta < 0, -delta, 0.0)

    avg_gain = float(np.mean(gain[:length]))
    avg_loss = float(np.mean(loss[:length]))
    out[length] = 100.0 if avg_loss == 0 else 100.0 - 100.0 / (1.0 + avg_gain / avg_loss)

    for i in range(length + 1, n):
        avg_gain = (avg_gain * (length - 1) + gain[i - 1]) / length
        avg_loss = (avg_loss * (length - 1) + loss[i - 1]) / length
        out[i] = 100.0 if avg_loss == 0 else 100.0 - 100.0 / (1.0 + avg_gain / avg_loss)
    return out


def stochastic(high, low, close, length=14, smooth_k=3, smooth_d=3):
    """Slow stochastic: raw %K smoothed by `smooth_k`, %D = SMA of %K over `smooth_d`."""
    hh = pd.Series(high, dtype=float).rolling(length).max().to_numpy()
    ll = pd.Series(low, dtype=float).rolling(length).min().to_numpy()
    span = hh - ll
    with np.errstate(divide="ignore", invalid="ignore"):
        raw_k = 100.0 * (np.asarray(close, dtype=float) - ll) / span
    raw_k = np.where(span == 0, 100.0, raw_k)
    k = sma(raw_k, smooth_k)
    d = sma(k, smooth_d)
    return k, d


def macd(close, fast=12, slow=26, signal=9):
    line = ema(close, fast) - ema(close, slow)
    valid = ~np.isnan(line)
    sig = np.full(line.shape, np.nan)
    if valid.any():
        first = int(np.argmax(valid))
        sig[first:] = ema(line[first:], signal)
    return line, sig, line - sig


def bollinger(close, length=20, deviation=2.0):
    series = pd.Series(close, dtype=float)
    basis = series.rolling(length).mean().to_numpy()
    # Skender uses the population standard deviation
    std = series.rolling(length).std(ddof=0).to_numpy()
    upper = basis + deviation * std
    lower = basis - deviation * std
    with np.errstate(divide="ignore", invalid="ignore"):
        width_pct = 100.0 * (upper / lower - 1.0)
        pct_b = 100.0 * (np.asarray(close, dtype=float) - lower) / (upper - lower)
    return basis, upper, lower, width_pct, pct_b


def true_range(high, low, close):
    high = np.asarray(high, dtype=float)
    low = np.asarray(low, dtype=float)
    close = np.asarray(close, dtype=float)
    prev_close = np.roll(close, 1)
    prev_close[0] = np.nan
    tr = np.maximum(high - low, np.maximum(np.abs(high - prev_close), np.abs(low - prev_close)))
    tr[0] = high[0] - low[0]
    return tr


def atr(high, low, close, length=14):
    return wilder(true_range(high, low, close), length)


def keltner(high, low, close, ema_length=20, atr_length=10, multiplier=2.0):
    centre = ema(close, ema_length)
    band = atr(high, low, close, atr_length)
    return centre, centre + multiplier * band, centre - multiplier * band


def parabolic_sar(high, low, step=0.02, maximum=0.2):
    """Wilder parabolic SAR. Returns (sar, rising); rising[i] is True while the SAR sits BELOW
    price (uptrend). A flip from True to False is the short-side reversal trigger."""
    high = np.asarray(high, dtype=float)
    low = np.asarray(low, dtype=float)
    n = len(high)
    sar = np.full(n, np.nan)
    rising = np.zeros(n, dtype=bool)
    if n < 3:
        return sar, rising

    is_rising = bool(high[1] >= high[0])
    extreme = high[1] if is_rising else low[1]
    current = low[0] if is_rising else high[0]
    accel = step

    for i in range(1, n):
        value = current + accel * (extreme - current)
        if is_rising:
            value = min(value, low[i - 1], low[max(i - 2, 0)])
            if low[i] < value:
                is_rising = False
                value = max(extreme, high[i])
                extreme = low[i]
                accel = step
            elif high[i] > extreme:
                extreme = high[i]
                accel = min(accel + step, maximum)
        else:
            value = max(value, high[i - 1], high[max(i - 2, 0)])
            if high[i] > value:
                is_rising = True
                value = min(extreme, low[i])
                extreme = high[i]
                accel = step
            elif low[i] < extreme:
                extreme = low[i]
                accel = min(accel + step, maximum)
        sar[i] = value
        rising[i] = is_rising
        current = value

    return sar, rising


def vbs_bands(high, low, close, volume, length=50, multiplier=2.5):
    """VWMA(hlc3) +/- multiplier * volume-weighted stdev, as VbsIndicatorExtension computes it."""
    hlc3 = (np.asarray(high, dtype=float) + np.asarray(low, dtype=float)
            + np.asarray(close, dtype=float)) / 3.0
    vol = np.asarray(volume, dtype=float)
    vol_sum = pd.Series(vol).rolling(length).sum().to_numpy()
    with np.errstate(divide="ignore", invalid="ignore"):
        mean = pd.Series(hlc3 * vol).rolling(length).sum().to_numpy() / vol_sum
        second = pd.Series(hlc3 * hlc3 * vol).rolling(length).sum().to_numpy() / vol_sum
    variance = second - mean * mean
    vw_stdev = np.sqrt(np.where(variance > 0, variance, 0.0))
    pad = multiplier * vw_stdev
    return mean, mean + pad, mean - pad, vw_stdev


def acs_percentage(high, low, close, length=50, factor=2.17):
    """Average Candle Size as a percentage: factor * SMA((high-low)/close*100, length)."""
    close = np.asarray(close, dtype=float)
    high = np.asarray(high, dtype=float)
    low = np.asarray(low, dtype=float)
    with np.errstate(divide="ignore", invalid="ignore"):
        range_pct = np.where(close != 0, (high - low) / close * 100.0, 0.0)
    return factor * sma(range_pct, length)
