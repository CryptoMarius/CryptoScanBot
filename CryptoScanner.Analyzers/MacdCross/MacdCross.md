# MACD Crossover (MacdCross)

## Overview

The **MacdCross** strategy trades the crossover of the MACD line and its signal line. A long opens
when the MACD line closes above the signal line after having been under it, a short when it closes
under it after having been above. The position is closed again when the two lines cross back.

It reads the standard MACD (12, 26, 9) every indicator hub already computes, and declares ADX(14)
for its optional trend-strength filters. It is the first strategy that also decides when to
*leave*: it implements `SignalCreateBase.IsExitSignal`, which the position monitor asks on every
close of the position's own interval. Stop loss and take profit keep working next to that exit —
it is an extra way out, not a replacement.

## Background

The MACD (Moving Average Convergence/Divergence) was published by Gerald Appel in the late 1970s.
The MACD line is the difference between a 12 and a 26 period EMA, the signal line a 9 period EMA
of that difference. The crossover of the two is the oldest and simplest way to read it: momentum
turning up or down.

The idea to test here comes from a Ross Cameron video: enter on the cross, exit on the cross back.
Two things are worth knowing before reading the results.

- **It is a trend-following rule.** It wins when a move continues for a while after the cross and
  loses a little on every cross that does not. The expected shape of the result is many small
  losses and a few larger wins, so the win rate on its own says nothing — the average win against
  the average loss does.
- **The known weakness is the flat market.** When price goes nowhere the two lines circle each
  other and cross every few candles, each cross a small loss. Published backtests of the bare
  crossover on daily stock data mostly come out around zero after costs for exactly that reason;
  the strategy earns in trending periods and gives it back in the ranges between them. Cameron
  uses it on momentum stocks that are already moving, which is a pre-selection this strategy only
  makes when its filters are switched on.

That is why every filter below is a setting, off by default: the bare rule is measured first, and
then each filter on its own, so the emulator says what each one is worth.

## How it works

Per candle of the signal interval, for the side being evaluated, cheapest test first:

1. The MACD line has to be on the side of the signal line that favours the trade (above for a
   long, under for a short) on the candle being evaluated and on the `ConfirmationCandles` before
   it.
2. The candle before those has to be on the other side — otherwise there was no cross.
3. Optionally the cross has to lie beyond the zero line (`RequireCrossBeyondZeroLine`), read at
   the cross candle itself.
4. Optionally the two lines have to be at least `MinimumDistancePercentage` of the price apart at
   the signal candle.
5. Optionally the ADX(14) at the signal candle has to be at least `AdxMinimum`, and somewhere in
   the last `AdxRecentlyWithinCandles` candles it has to have been under `AdxRecentlyBelow`.
6. Optionally the average volume of the last `RelativeVolumeCandles` candles has to be at least
   `RelativeVolumeMinimum` times the average of the `RelativeVolumeAverageCandles` before them.

The exit, asked by the position monitor on every close of the position's interval:

1. `ExitOnCrossBack` has to be on.
2. The MACD line has to be on the wrong side of the signal line for the position on the last
   closed candle and on the `ExitConfirmationCandles` before it.

The exit is a *state* ("the lines are against us"), not an *event* ("they crossed on this candle"),
on purpose. The flag the monitor sets lives in memory only, and a state is found again after a
restart where an event would be gone.

Once the strategy asks for the exit, the trader takes the same door a position past
`Trading.MaxPositionDurationDays` takes: the take profit is aimed one tick through the last price,
so the position leaves at whatever the market offers on the next candle. An entry that has not
filled yet is cancelled instead.

## The pre-selection: is the coin moving, and is the move young?

Cameron does not take every cross. He looks at stocks that are already moving on unusual volume,
and the cross is his timing inside that move. Two filters translate that to crypto.

**ADX (Average Directional Index, Wilder 1978).** Measures the strength of a move on a 0..100
scale, regardless of its direction. It is built from how far each candle's high and low reach
beyond the previous candle's, smoothed over 14 candles. Reading it:

| ADX | Meaning |
|-----|---------|
| under 20 | Ranging. Buyers and sellers take turns and nobody wins. This is where the MACD lines circle each other. |
| 20 – 25 | Unclear, something may be starting. |
| above 25 | A trend is running, up or down — the direction is read from the price, not from the ADX. |
| 40 – 50 and up | A strong trend, often already well advanced. |

Because it is smoothed it lags: by the time it passes 25 the move is often under way, and an ADX
that has sat at 45 for an hour is the tail of a move rather than the start of one. That is what the
second ADX setting is for. `AdxRecentlyBelow` asks that the ADX came *out of* the ranging zone
recently, so together with `AdxMinimum` the pair reads as "an ADX climbing out of the range right
now" — a young trend. The textbook thresholds are 20 and 25; crypto on the short intervals runs
hotter, so measure rather than assume.

**Relative volume.** The average volume of the recent candles against the average of the candles
before them. Two means the coin is trading at twice its usual pace: something is happening in it.
The recent candles are left out of the baseline on purpose, so a spike does not dilute itself. The
filter says nothing about the direction, only that the move is carried by something.

## Settings

| Setting | Default | Meaning |
|---------|---------|---------|
| `ConfirmationCandles` | 0 | Closed candles the lines have to stay on the new side after the cross before the signal fires. Zero fires on the cross candle. |
| `MinimumDistancePercentage` | 0 | Minimum separation of the two lines at the signal candle, as a percentage of the price. Zero accepts any separation. |
| `RequireCrossBeyondZeroLine` | off | A long only when the MACD line is still under zero at the cross, a short only when it is above. |
| `AdxMinimum` | 0 | The ADX(14) at the signal candle has to be at least this. Zero is off. |
| `AdxRecentlyBelow` | 0 | Somewhere in the window the ADX has to have been under this. Zero is off. |
| `AdxRecentlyWithinCandles` | 10 | The window for the test above, the signal candle included. |
| `RelativeVolumeMinimum` | 0 | Recent average volume as a multiple of the baseline average. Zero is off. |
| `RelativeVolumeCandles` | 3 | The recent window, the signal candle included. |
| `RelativeVolumeAverageCandles` | 50 | The baseline: how many candles before the recent window the usual volume is averaged over. |
| `ExitOnCrossBack` | on | Close the position when the lines cross back against it. |
| `ExitConfirmationCandles` | 0 | Closed candles the lines have to be against the position before it leaves. |

The distance is a percentage of the price and not an absolute value because the MACD is measured
in price units: the same absolute threshold would mean something different on a coin at 65 000 and
one at 0.01.

## Measuring it in the emulator

- The bare idea is the defaults. Take profit and stop loss are the trader's global settings, so a
  run that wants to see the pure crossover exit sets the take profit wide (the exit then comes
  from the cross back) and leaves the stop loss as the safety net.
- The two entry filters answer the "circling lines" question directly: `ConfirmationCandles` 1..3
  and `MinimumDistancePercentage` in steps of a few hundredths of a percent, one at a time.
- The pre-selection is the second question: `AdxMinimum` 25 with `AdxRecentlyBelow` 20, and
  `RelativeVolumeMinimum` 1.5 or 2, first separately and then together. The ExtraText of every
  signal carries the ADX and the volume ratio, so a run's signals can be split on them afterwards.
- `ExitOnCrossBack` off turns it into a plain crossover *entry* with the normal exits, which is the
  comparison that shows what the exit rule itself contributes.
- The strategy name in the queue is `macdcross`; the settings are addressed by the property names
  above.

## What it does not do (yet)

- Non-standard MACD or ADX periods. The 12/26/9 and the 14 are the only ones read.
- A stop just beyond the swing that preceded the cross, instead of the fixed percentage.
- The direction of the ADX's own components (+DI and −DI). Only the strength is read; the
  direction comes from the cross itself.
