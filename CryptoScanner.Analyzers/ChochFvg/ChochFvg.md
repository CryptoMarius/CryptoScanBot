# Change of character into a fair value gap (choch.fvg)

## Overview

After a downtrend (uptrend) the first candle that closes above the last lower high (below the last
higher low) is a **change of character**. The leg that made it usually leaves a **fair value gap**.
choch.fvg places a limit order on the middle of that gap when the middle lies in the 50..61.8%
retracement of the leg, with a stop behind the candle that made the gap and a take profit at a fixed
risk/reward. Built for the 1 minute candle, optionally only around the opening of the New York stock
market.

It is a plugin of its own next to Choch: Choch keeps 1m out for all its variants and enters on a
zigzag pivot; choch.fvg finds its own swings on the candles and has its own entry, stop and target.

## Rules (long; the short is the mirror image)

1. **Downtrend.** The lowest low of the last `LookbackCandles` candles is lower than every low between
   the two swing highs before it, and the last of those swing highs is lower than the one before
   (a lower high). A swing high needs `SwingStrength` lower highs on each side.
2. **Change of character.** The last closed candle is the first close above that lower high since
   the low.
3. **Leg and zone.** The leg runs from the low to the highest high since. The zone lies between the
   `FibZoneStart` and `FibZoneEnd` retracement of the leg, measured from its end back to its start.
4. **Fair value gap.** A bullish gap inside the leg: the high of candle 1 is under the low of
   candle 3. Its middle has to lie in the zone and no candle may have traded at the middle yet. With
   several, the highest one (the price reaches it first).
5. **Order.** A limit on the middle of the gap. The price has to be above it still.
6. **Exit.** Stop under the low of the middle candle of the gap, take profit `RiskReward` times the
   risk above the entry, no DCA. Stop and target go to the trader as percentages of the entry
   (`OverrideSlPercentage`, `OverrideProfitPercentage`).
6a. **Minimum stop.** A setup whose stop lies closer to the entry than `MinimumStopPercentage` is
   dropped. A count on 121 Binance perpetuals (Jan..Sep 2026, 1m) gave a median stop of 0.15% for
   the orders that fill, and about 30% under 0.1%; with fees of 0.1% for the round trip a stop of
   0.08% needs a win rate of (0.08 + 0.1) / (5 x 0.08) = 45% to break even.
7. **Session.** With `UseSessionWindow` only a change of character on a candle that closes inside
   the window (New York time, summer and winter time included) places an order.
8. **Withdrawal.** The order is a standing entry: it stays until it fills, unless it waited
   `EntryExpiryCandles` candles or the price reached the target without a fill.

The global entry conditions (watch candles, RSI recovery) do not apply; the rules decide the entry.

## Settings

| Setting | Default | Meaning |
|---|---|---|
| `SwingStrength` | 3 | candles with a lower high (higher low) on each side of a swing |
| `LookbackCandles` | 60 | window of the low (high) the leg starts from |
| `FibZoneStart` | 50 | start of the entry zone, retracement of the leg (0..100) |
| `FibZoneEnd` | 61.8 | end of the entry zone, retracement of the leg (0..100) |
| `EntryExpiryCandles` | 60 | the limit is withdrawn after this many candles without a fill |
| `RiskReward` | 4 | take profit as a multiple of the distance from entry to stop |
| `MinimumStopPercentage` | 0.2 | a setup with a closer stop is dropped (0 takes every setup) |
| `UseSessionWindow` | true | only a change of character inside the session window |
| `SessionStart` | 09:30 | start of the window, New York time (HH:mm) |
| `SessionEnd` | 11:30 | end of the window, New York time (HH:mm) |
| Intervals | 1m | |

## Break-even

Moving the stop to the entry once a candle closes beyond the end of the leg is not part of it: the
trader has no stop move on a price level of the strategy. The profit lock of the trading settings
(break-even at a percentage) is the nearest form and can be measured as a variant.

## Break-even win rate

Win rate p needed to break even with a stop of s% and fees f% for the round trip:
p = (s + f) / ((1 + RiskReward) x s). With RiskReward 4 and f = 0.1: s = 0.2% needs 30%, s = 0.4%
needs 25%, without fees 20%.

## Measured

Binance Perpetual, 121 coins, 2026-01-01..2026-10-01, 1m, 100 USDT per entry, long and short, no DCA,
no MA200 (Session2 runs 2124-2126, build of 22-11-2026). Filled = won + lost; the rest are limits
withdrawn without a fill.

| Run | Variant | Filled | Won | Win rate | Needed* | Profit (USDT) | Long / short | Months in profit |
|---|---|---|---|---|---|---|---|---|
| 2124 | session window, no minimum stop | 1,165 | 213 | 18.3% | 23.1% | -74.61 | -28.30 / -46.31 | 2 of 9 |
| 2125 | whole day, no minimum stop | 12,465 | 2,029 | 16.3% | 24.7% | -1,052.60 | -492.85 / -559.74 | 0 of 9 |
| 2126 | session window, minimum stop 0.2% | 549 | 102 | 18.6% | 21.1% | -28.67 | -16.02 / -12.64 | 4 of 9 |

\* Break-even win rate from the realised averages: average loss / (average win + average loss).

All three lose on both sides; the win rate stays under the break-even rate in every variant.

## File structure

```
CryptoScanner.Analyzers/ChochFvg/
├── ChochFvgPlugin.cs          # Registration (standing entry)
├── ChochFvgSettings.cs        # Settings
├── ChochFvgRules.cs           # The rules on a plain candle list (tested)
├── ChochFvg.md                # This document
├── Config/                    # Avalonia tab; Photino builds its tab from the SettingCaption attributes
└── Signal/ChochFvgBase.cs     # Signal: candles, session window, withdrawal
```

Tests: `CryptoScanner.CoreTests/Signal/ChochFvgTests.cs`.
