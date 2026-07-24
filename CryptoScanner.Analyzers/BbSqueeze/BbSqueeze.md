# Bollinger Bands Squeeze + MACD Breakout (BbSqueeze)

## Overview

The **BbSqueeze** strategy detects moments when Bollinger Bands have been compressed (squeezed) for an extended period and then suddenly expand, signaling a potential breakout. The MACD histogram is used to confirm the direction of the breakout.

The concept is based on the observation that periods of low volatility (tight Bollinger Bands) are often followed by sharp price moves. By combining the squeeze detection with MACD momentum confirmation, the strategy aims to catch the beginning of these explosive moves.

## How it works

### 1. Squeeze detection

The strategy monitors the **Bollinger Bands width percentage** (`BB%`). When the BB width stays below a configurable threshold for a minimum number of consecutive candles, the market is considered to be in a "squeeze" — a period of unusually low volatility.

### 2. Breakout detection

A breakout is detected when the current candle's BB width **exceeds** the squeeze threshold after having been below it for the required number of candles. This expansion signals that volatility is returning.

### 3. MACD confirmation

To determine the breakout **direction**, the strategy looks at the MACD histogram:

- **Long signal**: The MACD histogram must be **rising** for a configurable number of candles AND be **positive** (above the zero line). This confirms bullish momentum.
- **Short signal**: The MACD histogram must be **falling** for a configurable number of candles AND be **negative** (below the zero line). This confirms bearish momentum.

### 4. Give-up condition

After a signal is generated, if the Bollinger Bands **re-squeeze** (width drops back below the threshold), the signal is abandoned. This prevents holding a position when the expected breakout fizzles out.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | `BB% > BBSqueezeMaxPercentage` | BB is currently expanding |
| 2 | Previous N candles all had `BB% ≤ BBSqueezeMaxPercentage` | Prior squeeze confirmed |
| 3 | MACD histogram rising for M candles | Bullish momentum building |
| 4 | MACD histogram > 0 | Momentum is positive |

### Short entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | `BB% > BBSqueezeMaxPercentage` | BB is currently expanding |
| 2 | Previous N candles all had `BB% ≤ BBSqueezeMaxPercentage` | Prior squeeze confirmed |
| 3 | MACD histogram falling for M candles | Bearish momentum building |
| 4 | MACD histogram < 0 | Momentum is negative |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `BBSqueezeMaxPercentage` | 2.0 | Maximum BB width (%) to qualify as a squeeze. Lower values require tighter squeezes. |
| `SqueezeMinCandles` | 6 | Minimum number of consecutive candles the BB must stay squeezed before a breakout qualifies. |
| `MacdConfirmCandles` | 2 | Number of consecutive MACD histogram bars that must confirm the breakout direction (rising for long, falling for short). |

## Indicators used

- **SMA(20)** — Middle Bollinger Band (also the basis for BB width calculation)
- **Bollinger Bands deviation** — Standard deviation used for the upper/lower bands
- **Bollinger Bands percentage (BB%)** — Width of the bands expressed as a percentage of the middle band
- **MACD histogram** — Difference between the MACD line and signal line, used for momentum confirmation

## Visual example

```
Price
  │
  │         ╱ ── Upper BB
  │    ════╱════════╗
  │    ║  Squeeze   ║──→ Breakout! (BB expands + MACD confirms)
  │    ════╲════════╝
  │         ╲ ── Lower BB
  │
  └──────────────────── Time
        ◄─────────►
        SqueezeMinCandles
```

## Tuning tips

- **Tighter squeeze** (`BBSqueezeMaxPercentage` < 2.0): Fewer but potentially stronger breakout signals.
- **Longer squeeze** (`SqueezeMinCandles` > 6): Requires more patience but targets bigger moves.
- **More confirmation** (`MacdConfirmCandles` > 2): Reduces false signals but may enter later in the move.
- On **higher timeframes** (4h, 1d), squeezes tend to produce more reliable breakouts.
- On **lower timeframes** (5m, 15m), consider using tighter thresholds to filter noise.
