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

1. **Pivots.** The tops and bottoms are the points of the scanner's own ZigZag: the secondary
   trend, on the wicks (high/low), swing points after Lance Beggs - the same ZigZag the chart draws.
   The scan feeds it one candle at a time, so a point only counts from the moment the ZigZag shows
   it and nothing looks into the future.
2. **Horizontal levels.** Pivots (tops and bottoms) of the last 300 candles whose prices lie within
   a quarter of the average candle size (ATR 14) of each other form one level. Two pivots is the
   minimum; the number of pivots is the level's *touches*.
3. **Sloped lines**, the way a trader draws them. On every new top the line runs back to the most
   recent HIGHER top from which no candle closed above the line since (bottoms: the mirror). It
   counts - is drawn, can break - from a third touch, and it ends at the first close beyond it. The
   line is straight in log price, so its slope is a percentage per candle.

The offline measurement (CryptoScanBot.tools) used simple fractal pivots to run fast; that source is
still in the code (`SupportResistancePivots.Fractal`) only so the tests can hold the port to it.

## Breakout and flip

- **Breakout**: a close more than half an average candle beyond the level or line, straight from the
  other side (the candle before was still on the original side). A price that creeps through the
  level does not count as a breakout.
- **Flip**: after the breakout the price first runs at least one average candle away from the level;
  then, within 30 candles of the breakout, a candle whose wick comes back to within a quarter of an
  average candle of the level and that closes on the breakout side again. A close back through the
  level (more than a quarter candle) before that cancels the breakout.
- A horizontal level needs **three** pivots before it can break at all, and a level that flipped
  twice or was crossed four times (a chop zone) is done. Closer to the theory of the flip: on the
  first rules SOL 1h gave 1.4 flips a day, now about three a week (28-09-2026).

## Entry: the confirmation candle

A trader does not buy the moment the price touches the flipped level; he waits for the candle that
actually turns (29-09-2026). With *Wait for a confirmation candle* on (the default) the signal comes on
the first candle after the retest that:

- closes in the trade direction: green (close above open) for a long, red for a short;
- closes beyond the retest candle: above its high for a long, below its low for a short;
- trades at least *Volume* times the average volume of the 20 candles before it (0 switches this off);
- comes within *Within candles* candles of the retest. A close back through the level (more than a
  quarter of an average candle) before that drops the flip.

The entry is at the close of that candle. The stop stays half an average candle beyond the level, so a
later entry has a wider stop, and the take profit is measured from the actual entry. With the setting
off the retest candle itself is the entry, whatever its colour.

## Settings

| Setting | Default | Meaning |
|---|---|---|
| Flip on horizontal levels | on | Signal on flips of horizontal levels |
| Minimum touches | 3 | How many pivots a level needs before its flip counts (a level breaks from 3) |
| Flip on sloped lines | on | Signal on flips of sloped lines |
| History candles | 500 | How many candles of the signal interval are scanned |
| Wait for a confirmation candle | on | Enter on the first candle after the retest that turns in the trade direction and closes beyond the retest candle |
| Within candles | 3 | How many candles after the retest the confirmation may take |
| Volume (x the average) | 1.5 | The entry candle's volume against the average of the 20 candles before it; 0 is off |
| Stop beyond the level | on | Stop loss half an average candle beyond the level that flipped |
| Take profit (x the stop) | 2 | Take profit as a multiple of that stop distance; 0 keeps the trader's own |

## What was measured before it was built

Offline, on 40 coins of the emulator's candle database, 15m and 1h: a trade at every flip, with the
stop beyond the level and a target of twice that distance, earned **nothing** on average - the same
as a trade at the breakout itself and the same as a trade at any random candle. Stronger levels
(three or four touches) and a clear rejection wick on the retest did not change that.

Waiting for the confirmation candle was measured the same way (29-09-2026, script
`sr_flip_confirm.py`): it did not change the outcome either. With a target of twice the stop a trade
needs to win one time in three to break even; every variant stayed between 30 and 35 percent - the
retest candle, a green retest candle, the confirmation candle, each with and without extra volume.

So the strategy exists to **see and hear** the flip and to measure it in the emulator, not as a
proven money maker. Switch it on for trading only after an emulator run says otherwise.

What the same measurement did find: a short that opened just above a support did worse than the
other shorts. That became a trader entry condition for every strategy, not part of this one:
SettingsEntryConditions.SkipShortAboveSupport ("No short within ... ATR above a 1h support").

## Performance

The scan runs over the last *History candles* candles of the signal interval on every closed candle.
The result is kept per symbol and interval for that candle, so the long and the short share one scan.
