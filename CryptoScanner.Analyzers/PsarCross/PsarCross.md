# Parabolic SAR Cross (PsarCross)

## Overview

The **PsarCross** strategy trades the flip of the parabolic SAR. A long opens when the SAR closes
under the price after having been above it, a short when it closes above the price after having
been under it. The position is closed again when the SAR flips back.

It reads the standard parabolic SAR (step 0.02, maximum 0.2) every indicator hub already computes.
There is no ADX filter: the ADX only exists in a Debug build, and this strategy is registered in
Release too. Like MacdCross it implements
`SignalCreateBase.IsExitSignal`, which the position monitor asks on every close of the position's
own interval. Stop loss and take profit keep working next to that exit.

There is no chart overlay of its own: the SAR dots already exist in both charts.

## Background

The parabolic SAR ("stop and reverse") trails the price: under it in a rising market, above it in a
falling one. It accelerates towards the price the longer the move lasts, and flips to the other side
as soon as the price touches it. Two things are worth knowing before reading the results.

- **It is always in the market.** The SAR is either under or above the price, so every flip is a new
  trade in the other direction. It wins when a move continues after the flip and loses a little on
  every flip that does not.
- **It works best in a parabolic market.** The SAR was made for a clear, accelerating trend: the
  longer the move lasts, the faster the SAR closes in, so a strong run is followed closely and left
  soon after it ends.
- **The weakness is the flat market.** When the price goes nowhere the SAR flips every few candles,
  each flip a small loss. That is why the filters below exist, all off by default.
- **Higher intervals work better.** On the low intervals every bit of noise touches the SAR and flips
  it; on 4h, 8h and 1d a flip needs a real move. The daily is the natural interval for it.

## How it works

The side is read against the **close** of the candle. The SAR itself flips during the candle as soon
as the high or low touches it; the scanner evaluates closed candles, so the signal comes at the
close of the candle on which the flip happened. On a daily interval that is 00:00 UTC, and the price
can be some way past the flip level by then.

Per candle of the signal interval, for the side being evaluated, cheapest test first:

1. The SAR has to be on the side of the price that favours the trade (under the close for a long,
   above it for a short) on the candle being evaluated and on the `ConfirmationCandles` before it.
2. The candle before those has to be on the other side - otherwise there was no flip.
3. Optionally the SAR has to be at least `MinimumDistancePercentage` of the price away from the close
   at the signal candle.
4. Optionally the average volume of the last `RelativeVolumeCandles` candles has to be at least
   `RelativeVolumeMinimum` times the average of the `RelativeVolumeAverageCandles` before them.

The exit, asked by the position monitor on every close of the position's interval:

1. `ExitOnFlipBack` has to be on.
2. The SAR has to be on the wrong side of the price for the position on the last closed candle and
   on the `ExitConfirmationCandles` before it.

## Settings

| Setting | Default | Meaning |
|---|---|---|
| Confirmation candles | 0 | Closed candles the SAR has to stay on the new side before the signal fires |
| Minimum distance % | 0 | Distance between SAR and close at the signal candle, as a percentage of the price (0 = off) |
| Minimum relative volume | 0 | Recent volume against the usual volume (0 = off) |
| Recent candles | 3 | The recent volume window |
| Average over candles | 20 | The baseline volume window before it |
| Exit on flip back | on | Close the position when the SAR flips back |
| Exit confirmation candles | 0 | Closed candles the SAR has to be against the position before it leaves |

The intervals are chosen in the interval box of the strategy; empty follows the side.

## Results

Not measured yet.
