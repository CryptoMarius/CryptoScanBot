# Support/Resistance Flip (SrFlip)

## Overview

A **support** is a price where the market turned up a few times, a **resistance** a price where it
turned down. A **flip** is what happens when such a level breaks and is then retested from the other
side and holds: the old resistance has become support, or the old support resistance.

The **SrFlip** strategy signals on the candle that closes a flip:

- **Long**: a resistance broke upwards, price came back down to it and closed above it again.
- **Short**: a support broke downwards, price came back up to it and closed below it again.

It reads nothing but candles. The levels and lines come from the Core building block
`CryptoScanner.Core.Trend.SupportResistance`, the same one the chart draws when "Support/resistance"
is ticked.

## How the levels are found

1. **Pivots.** A candle whose high is the single highest of the five candles on either side is a
   pivot high; the same for lows. A pivot only counts from the candle that confirms it, five candles
   later, so nothing looks into the future.
2. **Horizontal levels.** Pivots (highs and lows) of the last 300 candles whose prices lie within a
   quarter of the average candle size (ATR 14) of each other form one level. Two pivots is the
   minimum; the number of pivots is the level's *touches*.
3. **Sloped lines.** A falling line through the last two pivot highs (the second one lower) is a
   resistance line; a rising line through the last two pivot lows (the second one higher) a support
   line. The line is straight in log price, so its slope is a percentage per candle.

## Breakout and flip

- **Breakout**: a close more than half an average candle beyond the level or line, straight from the
  other side (the candle before was still on the original side). A price that creeps through the
  level does not count as a breakout.
- **Flip**: within 30 candles after the breakout, a candle whose wick comes back to within a quarter
  of an average candle of the level and that closes on the breakout side again. A close back through
  the level (more than a quarter candle) before that cancels the breakout.

## Settings

| Setting | Default | Meaning |
|---|---|---|
| Flip on horizontal levels | on | Signal on flips of horizontal levels |
| Minimum touches | 2 | How many pivots a level needs before its flip counts |
| Flip on sloped lines | on | Signal on flips of sloped lines |
| History candles | 500 | How many candles of the signal interval are scanned |
| Stop beyond the level | on | Stop loss half an average candle beyond the level that flipped |
| Take profit (x the stop) | 2 | Take profit as a multiple of that stop distance; 0 keeps the trader's own |

## What was measured before it was built

Offline, on 40 coins of the emulator's candle database, 15m and 1h: a trade at every flip, with the
stop beyond the level and a target of twice that distance, earned **nothing** on average - the same
as a trade at the breakout itself and the same as a trade at any random candle. Stronger levels
(three or four touches) and a clear rejection wick on the retest did not change that.

So the strategy exists to **see and hear** the flip and to measure it in the emulator, not as a
proven money maker. Switch it on for trading only after an emulator run says otherwise.

What the same measurement did find: a short that opened just above a support did worse than the
other shorts. That became a trader entry condition for every strategy, not part of this one:
SettingsEntryConditions.SkipShortAboveSupport ("No short within ... ATR above a 1h support").

## Performance

The scan runs over the last *History candles* candles of the signal interval on every closed candle.
The result is kept per symbol and interval for that candle, so the long and the short share one scan.
