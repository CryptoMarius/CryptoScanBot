# VWAP Mean Reversion (VwapMr)

## Overview

A mean-reversion band strategy around the **day VWAP**. When the price touches a band and the candle turns, the strategy expects the price to return to the VWAP and leaves there. Experimental and registered in Debug only. Measured in October 2026 and not a winner: see [Measured results](#measured-results).

## How it works

### Phase 1: Bands

- **VWAP** = Σ(HLC3 × volume) / Σ(volume), summed from 00:00 UTC of the candle's day. It starts again every day (a session VWAP).
- **stdev** = population standard deviation of the last `StdevLength` closes (default 20). Plain, not volume-weighted, and it does not restart at midnight.
- **Upper band** = VWAP + `Mult` × stdev (default 2.0)
- **Lower band** = VWAP − `Mult` × stdev

A day VWAP needs every candle since midnight. The indicator warm-up feeds 260 candles, which on 1m covers only the last 4h20 of a day. A day that was already running when the first candle came in is therefore marked incomplete and gets **no** value: no signal and no exit until the next day starts. From then on every day is complete.

### Phase 2: Entry

- **Long**: the low of this candle touches its lower band, or the low of the previous candle touched the previous lower band; and this candle is green (close > open).
- **Short**: the high of this candle touches its upper band, or the high of the previous candle touched the previous upper band; and this candle is red (close < open).

The reversal candle can be switched off (`RequireReversalCandle`).

### Phase 3: Exit

- **Exit at the VWAP** (`ExitAtVwap`, `SignalCreateBase.IsExitSignal`): a long leaves when a candle closes at or above the VWAP, a short when a candle closes at or under it. The exit is at market after that close, not a limit order on the VWAP during the candle.
- **Stop loss** = `StopLossPercentage` from the entry (default 1.5%). Zero leaves the position to the stop loss of the trading settings.
- The take profit of the trading settings keeps working next to the VWAP exit. A run that wants to measure the VWAP exit alone sets it wide.

Right after midnight the VWAP starts again close to the price, so a position that is still open then usually leaves on the first candles of the new day.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Green candle (optional) | Close > open |
| 2 | Complete day VWAP | The calculation saw the day start |
| 3 | Lower band touched | Low ≤ lower band, on this candle or the previous one |

### Short entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Red candle (optional) | Close < open |
| 2 | Complete day VWAP | The calculation saw the day start |
| 3 | Upper band touched | High ≥ upper band, on this candle or the previous one |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `StdevLength` | 20 | Number of closes for the standard deviation |
| `Mult` | 2.0 | Band distance in standard deviations |
| `RequireReversalCandle` | true | Long wants a green candle, short a red one |
| `StopLossPercentage` | 1.5 | Stop loss % from the entry, 0 = trading settings |
| `ExitAtVwap` | true | Leave when a candle closes back at the VWAP |

## Intervals

The VWAP restarts every day, so the strategy is meant for intraday intervals (1m to 1h). On 4h a day has six candles; on 1d every candle is its own day and the VWAP is that candle's HLC3.

## Measured results

Emulator, Binance Perpetual, 1 January to 1 October 2026, about 100 USDT per position, one position per coin, market entry, no DCA's, MA200 condition off. Costs are 0.1% per position (0.05% per side). Amounts in USDT.

### 1. Defaults: exit at the VWAP, stop 1.5%

| Interval | Positions | Won | Profit | Per position |
|---|---|---|---|---|
| 5m | 141,989 | 55.0% | −16,127 | −0.113% |
| 15m | 91,003 | 51.2% | −11,942 | −0.131% |
| 1h | 28,918 | 47.6% | −3,272 | −0.113% |
| 4h | 3,164 | 40.7% | −617 | −0.195% |
| 8h | 1,083 | 34.2% | −44 | −0.041% |
| 12h | 720 | 29.6% | −24 | −0.034% |
| 1d | 395 | 22.3% | +191 | +0.484% |

- **Before costs the entry is worth nothing.** On 5m and 15m a position makes −0.013% and −0.031% before costs. Split by the distance from the entry to the VWAP (0–0.5%, 0.5–1%, … above 3%), every group stays between −0.09% and +0.05% before costs. A larger distance means a larger target, but fewer positions reach it, and the two cancel out.
- **The target is too small for the stop.** On 15m half of the positions had the VWAP 1.00% or less away, against a stop of 1.5%. The average winner made 1.00 USDT, the average loser cost 1.32 USDT. Break-even needs 1.32 / (1.00 + 1.32) = 56.8% winners; 51.2% were reached.
- **Many signals come from a narrowing band, not from a stretched price.** In a quiet market the stdev of the closes shrinks and the band moves up to a price that goes sideways. The low then touches the band candle after candle, without any stretch to revert from.
- **The 1d profit is noise.** Without its best 10 positions it is −21.63.

Without the reversal candle (5m) the result is the same: −16,492 over 147,811 positions.

### 2. A wider stop (exit at the VWAP)

| Interval | Stop 1.5% | Stop 3% | Stop 5% | Stop 5%, without best 10 |
|---|---|---|---|---|
| 8h | −44 | +5 | +28 | −201 |
| 12h | −24 | −42 | −26 | −233 |
| 1d | +191 | +211 | +334 | +28 |

On 8h to 1d the positions were open for about a day: the VWAP starts again at the price every midnight, so the VWAP exit there means "leave after one day". A wider stop helps a little; the take profit is the real limit.

### 3. A fixed take profit instead of the VWAP exit (stop 5%)

`ExitAtVwap` off, the take profit of the trading settings at 10, 20 or 30%.

| Interval | Exit | Positions | Won | Profit | Without best 10 | First / second half | Months in profit |
|---|---|---|---|---|---|---|---|
| 8h | VWAP | 976 | 54.6% | +28 | −201 | +146 / −118 | 3 of 9 |
| 8h | TP 10% | 821 | 38.1% | +550 | +446 | +152 / +398 | 7 of 9 |
| 8h | TP 20% | 724 | 24.4% | +758 | +549 | +421 / +337 | 5 of 9 |
| 8h | TP 30% | 662 | 18.6% | +955 | +644 | +559 / +396 | 4 of 9 |
| 12h | VWAP | 599 | 52.1% | −26 | −233 | +17 / −43 | 5 of 9 |
| 12h | TP 10% | 557 | 39.1% | +452 | +349 | +283 / +168 | 6 of 9 |
| 12h | TP 20% | 498 | 22.5% | +266 | +62 | +270 / −4 | 5 of 9 |
| 12h | TP 30% | 471 | 17.0% | +411 | +106 | +469 / −58 | 5 of 9 |

- Only 8h with a fixed take profit holds up without its best 10 positions and in both halves of the period. Longs carry it (+628 to +699); shorts range from −78 to +256.
- It is no longer mean reversion: a position is held for days to weeks until +10–30% or −5%. The band touch is only the moment of entry.
- The amounts stay small for nine months: +955 at a peak of 4,418 USDT invested at the same time.

### Conclusion

The VWAP exit does not work on any interval: the target is too close, and before costs the entry is worth nothing. A far fixed take profit on 8h gives a small, fairly steady profit, but not enough to make this a strategy for live trading. It stays in the Debug build as a reference.

## File structure

```
CryptoScanner.Analyzers/VwapMr/
├── VwapMrPlugin.cs                      # Plugin registration
├── VwapMrSettings.cs                    # Strategy-specific settings
├── VwapMrBandCalculator.cs              # Day VWAP + stdev bands (hub and chart)
├── VwapMrCandleData.cs                  # Band values per candle
├── VwapMr.md                            # This document
├── Indicators/VwapMrIndicatorExtension.cs
├── Chart/VwapMrChartOverlay.cs
├── Config/                              # Avalonia settings tab
└── Signal/VwapMrBase.cs                 # Long/short entry and the VWAP exit
```

Strategy name in the UI: **vwapmr**.
