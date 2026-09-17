# TBO – Trending breakout (Tbo)

## Overview

A trend-following breakout strategy built on a **cloud of four moving averages**. The cloud gives
the direction and the strength of the trend; the entry is one of three events inside that trend.

Each trigger has its own switch, so a run measures them apart or together:

- **Breakout** — the candle closes through the last confirmed pivot level, with the cloud pointing
  the way of the trade. On by default.
- **Cloud cross** — EMA(20) closes on the other side of EMA(40). Off by default.
- **Springboard bounce** — inside a running trend the price dips to the fast line and closes back
  above it. Off by default.

An optional RSI filter, an optional volume filter and an optional slow-line filter each ask for more
confirmation, and an optional exit leaves the position once the cloud flips against it.

## The four lines

| Line | What it is | Role |
| --- | --- | --- |
| 1 | **EMA(20)** on the close | the fast line: a pullback bounces off it |
| 2 | **EMA(40)** on the close | its crossing with the fast line is the cross trigger |
| 3 | **SMA(50)** on the close | middle of the cloud |
| 4 | **SMA(150)** on the close | the slow line: its slope is the trend filter |

The highest and the lowest of those four are the edges of the cloud. Price beyond both edges is a
trend running; price between them is consolidation.

**These four lengths are fixed on purpose.** They are not four numbers that happened to look good:
the set belongs together, and changing one of them makes this a different strategy rather than a
tuned one. `TheMeasuredLineLengthsAreTheDefaults` in the tests fails when a default moves, so that
is a deliberate act and never an accident. A run that wants a faster or slower cloud should scale
*all four* in proportion — the queue does that in NX2 (10/20/25/75) and NX3 (50/100/125/375).

## How it works

The values come from `TboIndicatorExtension`, which runs once per symbol and interval and writes
them into the plugin slot of the candle (`TboCandleData`), so a candle with both sides active pays
for them once:

- **The cloud** — the four moving averages, taken from the shared registry, so a length another
  plugin already asked for is not computed twice.
- **The slope** — how far the slow line moved over `SlowLineLookbackCandles` candles, as a
  percentage of its own value. Read straight from the hub results, so it costs no candle walk.
- **The levels** — pivot highs and lows over a window of `PivotLeftCandles` + `PivotRightCandles`
  + 1 candles, kept in a small ring buffer. A pivot is only confirmed once its right-hand candles
  are in, so the level always predates the candle that breaks it.

`TboBase.IsSignal` then tests, cheapest first (most candles never get past the first test):

1. the cloud points the way of the trade (fast EMA above the second EMA for a long) — this holds
   for every trigger;
2. the cloud is at least `MinimumCloudWidthPercentage` wide, measured top to bottom as a percentage
   of the price;
3. the slow line has moved at least `MinimumSlowLineSlopePercentage` in the direction of the trade;
4. the RSI agrees, when `UseRsiFilter` is on;
5. one step back to the previous candle, shared by everything below it;
6. **the trigger**, and any one of them is enough:
   - *cloud cross*: the cloud was **not** on our side on the previous candle;
   - *springboard*: the candle reached the fast line and closed back on our side of it, with the
     previous candle already on that side;
   - *breakout*: the close is outside the cloud (when `RequirePriceOutsideCloud` is on), there is a
     level no older than `PivotMaximumAgeCandles`, the close cleared it by
     `BreakoutBufferPercentage`, and the previous close had **not**;
7. the cloud is wider than on the previous candle, when `RequireCloudWidening` is on;
8. the volume is at least `VolumeMultiplier` times the average of the `VolumeAverageCandles` candles
   before this one, when `UseVolumeFilter` is on. Last, because it is the only test that walks a
   stretch of candles.

## Exit

`ExitOnCloudFlip` is **off by default**: the position is left to the stop loss and the take profit,
which is the setup this strategy is measured in first. Switching it on adds `IsExitSignal`: the
position leaves once the cloud has been against it for `ExitConfirmationCandles` closed candles. It
is written as a state ("the cloud is against us") rather than an event ("it flipped on this
candle"), so a restart does not lose the exit.

## Chart overlay

`TboChartOverlay` draws the four lines, the two pivot levels and an "open long" / "open short"
marker at every crossing of the two EMAs, in both the Avalonia and the web chart.

The cloud is three bands, one between each neighbouring pair of lines, each coloured by its own
pair's direction. The three are painted at 55%, 78% and 100% of the configured opacity, the outer
band being the firm one: measured off a reference chart, where the three bands lift the background
by 26, 36 and 47 counts of the same hue. That is what makes it read as one shape with a near and a
far edge instead of three stripes. The four lines are
ordinary indicators, so the same picture can be reproduced on any charting package: EMA 20, EMA 40,
SMA 50 and SMA 150 on the close.

The overlay computes the lines a **second time**, over a whole candle list, where the strategy reads
them one candle at a time from the indicator hub. Two paths to the same numbers drift apart sooner
or later, so the pivot rule in `TboLinesHelper` is deliberately the same walk as the one in the
extension, and `TheOverlayAndTheStrategySeeTheSameLines` feeds one series through both and compares
every candle.

### The breakout marker

The reference chart puts a white dot under a candle now and then, and its own settings screen names
the markers: **Open Long** (green triangle, below bar), **Breakout** (white, below bar), **Close
Short** (orange diamond, below bar), **Cross Up** (green cross, above bar), **Open Short** (magenta
triangle, above bar) and **Breakdown** (yellow, above bar). The overlay draws the two triangles and
the two dots; Close Short and Cross Up are not reconstructed.

The dot is drawn when four things hold, and each of them is measured against marks read off
reference charts rather than chosen:

1. the **high** trades through the last confirmed level - not the close;
2. the cloud points the way of the break;
3. it is not the candle the cloud turned on - the maker says twice in his videos that a breakout is
   always printed *after* the entry;
4. it sits between 8 and 60 candles after that turn and makes a new extreme over the last 5 candles.

The nine known marks (24 October 2023, 11 to 13 February 2024, 28 and 29 October 2024, 6 November
2024, and 12, 18 and 19 May 2025) all satisfy this, so the band in rule 4 has room on both sides of
what was measured: they sit between 11 and 46 candles after their turn.

It is **still wider than the reference, by about three to one**: some thirty marks a year on a daily
chart where the reference draws six to nine. Containing every real mark is the property worth
keeping until the missing condition is found, and that condition is not guessed at here. The maker
says the marks print "based off of multiple factors", and that his paid version can change which
factors print a mark and even *how many* are printed.

What is ruled out as that condition, each on a measurement:

- **volume** - three of the nine known marks sit below the average volume of the twenty candles
  before them, down to 0.71 times;
- **a squeeze** (Bollinger Bands breaking out of Keltner Channels) - it releases seven times in the
  year around the known marks and never on one of them;
- **a margin above the level** - 28 October 2024 clears its level by 0.64% and is marked;
- **a close through a Donchian range** - 12 and 19 May 2025 close *under* the previous highs and are
  marked, while 13 November 2024 closes above them and is not.

`TboBreakoutOnRealCandlesTests` holds those days as real candles, so a narrower rule can be tried
without losing one of them by accident.

One difference is expected and harmless: a crossing marker only needs the two EMAs and therefore
appears after 40 candles, while the strategy waits for all four lines and so needs 150. During that
warm-up the chart can show a cross that no signal was ever produced for.

## Settings

| Setting | Default | What it does |
| --- | --- | --- |
| Entry on breakout | on | Fire on the break through the last pivot level |
| Entry on cloud cross | off | Fire on EMA(20) crossing EMA(40) |
| Entry on springboard bounce | off | Fire on the pullback to the fast line inside a trend |
| Fast EMA | 20 | The fast line |
| Second EMA | 40 | The crossing partner |
| Medium SMA | 50 | Third line |
| Slow SMA | 150 | The slow line the trend is judged by |
| Price outside the cloud | on | Close beyond all four lines — breakout trigger only |
| Minimum cloud width % | 0 | Top to bottom, as a percentage of the price |
| Cloud must be widening | off | Trend gaining strength rather than losing it |
| Minimum slow line slope % | 0 | How far the slow line moved over the lookback |
| Pivot candles left / right | 5 / 5 | How a pivot is defined; right is also the confirmation delay |
| Maximum level age | 0 | How old the level may be, in candles; 0 accepts any age |
| Breakout buffer % | 0 | How far beyond the level the candle has to close |
| Use RSI filter | off | RSI ≥ minimum for a long, ≤ maximum for a short |
| Use volume filter | off | Signal candle at `VolumeMultiplier` × the average before it |
| Exit on cloud flip | off | The strategy's own exit, next to stop loss and take profit |

## What to measure first

The bare rule first (every filter off), then one filter at a time, so the emulator says what each
one is worth on its own. Four things worth the most attention:

- **The three triggers apart.** A breakout, a cross and a pullback are different ideas that happen
  to share a cloud. Measuring them together first would hide which one carries the result.
- **The timeframe.** A cloud of 20 to 150 candles behaves very differently on 5m than on the daily,
  and neither has been shown to work yet. The queue runs both.
- **Minimum cloud width** — the flat market is where a breakout rule bleeds, and the width is the
  cheapest way to sit it out.
- **The volume filter** — the one filter that asks whether anybody is actually trading the break.
