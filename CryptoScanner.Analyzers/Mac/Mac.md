# MAC – Moving average cloud

## Overview

A trend-following strategy built on a **cloud of four moving averages**. The cloud gives the
direction and the strength of the trend; the entry is one of four events inside that trend.

Each trigger has its own switch, so a run measures them apart or together:

- **Breakout** — the candle closes through the last confirmed pivot level, with the cloud pointing
  the way of the trade. On by default.
- **Cloud cross** — EMA(20) closes on the other side of EMA(40). Off by default.
- **Springboard bounce** — inside a running trend the price dips to the fast line and closes back
  above it. Off by default.
- **Line cross** — EMA(40) closes on the other side of SMA(50). This one runs AHEAD of the cloud
  cross, so it is the only trigger that fires while the cloud still points the other way. Off by
  default.

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

The values come from `MacIndicatorExtension`, which runs once per symbol and interval and writes
them into the plugin slot of the candle (`MacCandleData`), so a candle with both sides active pays
for them once:

- **The cloud** — the four moving averages, taken from the shared registry, so a length another
  plugin already asked for is not computed twice.
- **The slope** — how far the slow line moved over `SlowLineLookbackCandles` candles, as a
  percentage of its own value. Read straight from the hub results, so it costs no candle walk.
- **The levels** — pivot highs and lows over a window of `PivotLeftCandles` + `PivotRightCandles`
  + 1 candles, kept in a small ring buffer. A pivot is only confirmed once its right-hand candles
  are in, so the level always predates the candle that breaks it.

`MacBase.IsSignal` then tests, cheapest first (most candles never get past the first test):

1. the line cross, when `EntryOnLineCross` is on, because it is the one trigger that fires before
   the cloud turns and step 2 would make it unreachable;
2. the cloud points the way of the trade (fast EMA above the second EMA for a long) — this holds
   for every trigger except the line cross;
3. the cloud is at least `MinimumCloudWidthPercentage` wide, measured top to bottom as a percentage
   of the price;
4. the slow line has moved at least `MinimumSlowLineSlopePercentage` in the direction of the trade;
5. the RSI agrees, when `UseRsiFilter` is on;
6. one step back to the previous candle, shared by everything below it;
7. **the trigger**, and any one of them is enough:
   - *line cross*: the second line closed on our side of the third and had not on the candle
     before;
   - *cloud cross*: the cloud was **not** on our side on the previous candle;
   - *springboard*: the candle reached the fast line and closed back on our side of it, with the
     previous candle already on that side;
   - *breakout*: the close is outside the cloud (when `RequirePriceOutsideCloud` is on), there is a
     level no older than `PivotMaximumAgeCandles`, the close cleared it by
     `BreakoutBufferPercentage`, and the previous close had **not**;
8. the cloud is wider than on the previous candle, when `RequireCloudWidening` is on;
9. the volume is at least `VolumeMultiplier` times the average of the `VolumeAverageCandles` candles
   before this one, when `UseVolumeFilter` is on. Last, because it is the only test that walks a
   stretch of candles.

## Exit

`ExitOnCloudFlip` is **off by default**: the position is left to the stop loss and the take profit,
which is the setup this strategy is measured in first. Switching it on adds `IsExitSignal`: the
position leaves once the cloud has been against it for `ExitConfirmationCandles` closed candles. It
is written as a state ("the cloud is against us") rather than an event ("it flipped on this
candle"), so a restart does not lose the exit.

## Chart overlay

`MacChartOverlay` draws the four lines, the two pivot levels and an "open long" / "open short"
marker at every crossing of the two EMAs, in both the Avalonia and the web chart.

The cloud is three bands, one between each neighbouring pair of lines, each coloured by its own
pair's direction. The three are painted at 55%, 78% and 100% of the configured opacity, the outer
band being the firm one: measured off a chart image, where the three bands lift the background
by 26, 36 and 47 counts of the same hue. That is what makes it read as one shape with a near and a
far edge instead of three stripes. The four lines are
ordinary indicators, so the same picture can be reproduced on any charting package: EMA 20, EMA 40,
SMA 50 and SMA 150 on the close.

The overlay computes the lines a **second time**, over a whole candle list, where the strategy reads
them one candle at a time from the indicator hub. Two paths to the same numbers drift apart sooner
or later, so the pivot rule in `MacLinesHelper` is deliberately the same walk as the one in the
extension, and `TheOverlayAndTheStrategySeeTheSameLines` feeds one series through both and compares
every candle.

### Entering on the close crossing the second line

`MacSettings.EntryOnSecondLineCross` is the fifth entry trigger, added 23 September 2026. It fires
on the crossing the EXIT reads, taken from the other side: what the reference draws as "Close Long"
is the close falling through the second line, and entering a SHORT on that is entering on what
closes a long. A long entry wants the mirror, its "Close Short".

Two things about it are deliberate:

- **It is exempt from the cloud check**, like the line cross and for the same reason. This marker
  exists to close the opposite position, so the cloud is by definition still pointing that opposite
  way; asking for the cloud first would make the trigger unreachable.
- **The three guards the EXIT carries are NOT repeated** - the cloud pointing the way of the
  position, the close on that side of the slow line, the cloud stacked. Those exist to stop an exit
  firing against a trend that is still running, which is the opposite of what an entry on this
  crossing is for. Everything `IsSignal` asks of every trigger does still apply: cloud width, the
  slope of the slow line, and the rest.

It is off by default and has not been measured on money yet.

### The breakout marker

Six marker kinds belong to this picture: **Open Long** (green triangle, below bar), **Breakout**
(white, below bar), **Close Short** (orange diamond, below bar), **Cross Up** (green cross, above
bar), **Open Short** (magenta triangle, above bar) and **Breakdown** (yellow, above bar). The
overlay draws the two triangles and the two dots; Close Short and Cross Up are not built.

The dot is drawn when four things hold, and each of them is measured against the catalogued marks
rather than chosen:

1. the **high** trades through the last confirmed level - not the close;
2. the cloud points the way of the break;
3. it is not the candle the cloud turned on - a mark prints *after* the entry, never on it;
4. it sits between 8 and 60 candles after that turn and makes a new extreme over the last 5 candles.

The nine catalogued marks (24 October 2023, 11 to 13 February 2024, 28 and 29 October 2024,
6 November 2024, and 12, 18 and 19 May 2025) all satisfy this, so the band in rule 4 has room on
both sides of what was measured: they sit between 11 and 46 candles after their turn.

It is **deliberately wide, by about three to one**: some thirty marks a year on a daily chart where
six to nine are wanted. Containing every catalogued mark is the property worth keeping until the
missing condition is found, and that condition is not guessed at here. Distance from the cloud is
the one candidate that separates them at all, and every setting of it measured so far costs money.

What is ruled out as that condition, each on a measurement:

- **volume** - three of the nine known marks sit below the average volume of the twenty candles
  before them, down to 0.71 times;
- **a squeeze** (Bollinger Bands breaking out of Keltner Channels) - it releases seven times in the
  year around the known marks and never on one of them;
- **a margin above the level** - 28 October 2024 clears its level by 0.64% and is marked;
- **a close through a Donchian range** - 12 and 19 May 2025 close *under* the previous highs and are
  marked, while 13 November 2024 closes above them and is not.

`MacBreakoutOnRealCandlesTests` holds those days as real candles, so a narrower rule can be tried
without losing one of them by accident.

One difference is expected and harmless: a crossing marker only needs the two EMAs and therefore
appears after 40 candles, while the strategy waits for all four lines and so needs 150. During that
warm-up the chart can show a cross that no signal was ever produced for.

## The eight markers

The reference draws eight markers and names them in its own settings screen. Which of the eight a
candle carries is readable from its status line, and 64 of those readings on four coins pin every
one of them down:

| Marker | What it is | Built |
| --- | --- | --- |
| **Open Long** (green triangle) | the fast line crossing above the second | yes, the cloud cross |
| **Open Short** (magenta triangle) | the fast line crossing below the second | yes, the cloud cross |
| **Breakout** (white) | price beyond the resistance | yes, the breakout trigger |
| **Breakdown** (yellow) | price beyond the support | yes, the same trigger on the short side |
| **Close Long** (blue diamond) | the close crossing DOWN through the second line | yes, `ExitOnSecondLineCross` |
| **Close Short** (orange diamond) | the close crossing UP through the second line | yes, the same setting |
| **Cross Up** (green cross) | the second line crossing above the third | yes, `EntryOnLineCross` |
| **Cross Down** (red cross) | the second line crossing below the third | yes, the same setting |

**The reference has exactly ONE setting, and it only moves the lengths.** `getInputsInfo()` on the
study says so: a single input called "TBO Speed" with the values Standard, Fast and Slow, and
nothing else. Read off its status line on two candles of thirty minute bitcoin and fitted against
our own candles, every number lands on one length to the cent:

| line | Fast | Standard | Slow |
| --- | --- | --- | --- |
| first | EMA 20 | EMA 20 | EMA 20 |
| second | EMA 30 | EMA 40 | EMA 50 |
| third | SMA 40 | SMA 50 | SMA 100 |
| fourth | SMA 80 | SMA 150 | SMA 200 |

The FIRST line does not move: it is EMA(20) on all three speeds, identical to the cent. So the
speed is three lengths and not four, and everything else about the indicator - the rules, the
markers, the levels - is the same. Reproducing Fast or Slow is a matter of the four lengths being
settings, which they already are in `MacSettings`; nothing else has to change.

**Counted both ways.** Hitting every marker is only half of it: a rule that fires four times as
often as the reference draws the marker is not the same rule. The measurement that carries the most
weight is on FIVE MINUTE charts of four coins - eleven snapshots per coin, 483 markers drawn -
compared on the exact candle:

| Marker | it draws | we fire | same candle | missed | too many |
| --- | --- | --- | --- | --- | --- |
| Open Long | 68 | 68 | 68 | 0 | 0 |
| Open Short | 66 | 67 | 66 | 0 | 1 |
| Cross Up | 83 | 85 | 83 | 0 | 2 |
| Cross Down | 83 | 84 | 83 | 0 | 1 |
| Close Long | 131 | 128 | 128 | 3 | 0 |
| Close Short | 52 | 52 | 50 | 2 | 2 |
| Breakout | 95 | 229 | 17 | 78 | 212 |
| Breakdown | 45 | 89 | 11 | 34 | 78 |

**Confirmed on a second interval, over ELEVEN coins.** ada, avax, bitcoin, doge, ethereum, hype,
link, near, solana, sui and xrp on FIFTEEN minute candles, 4 to 14 September 2026, 1083 markers
harvested straight off the live chart through its own API - a different timeframe, a different
stretch of market, and a different way of reading it:

| Marker | it draws | we fire | same candle | missed | too many |
| --- | --- | --- | --- | --- | --- |
| Open Long | 132 | 132 | 132 | 0 | 0 |
| Open Short | 130 | 130 | 130 | 0 | 0 |
| Cross Up | 149 | 150 | 149 | 0 | 1 |
| Cross Down | 152 | 152 | 152 | 0 | 0 |
| Close Long | 122 | 121 | 114 | 8 | 7 |
| Close Short | 145 | 150 | 140 | 5 | 10 |
| Breakout, as a run | 91 | 165 | 63 | 28 | 102 |
| Breakdown, as a run | 112 | 238 | 76 | 36 | 162 |

And on ONE HOUR candles of bitcoin, 7 to 21 September, the six are 26 of 26 with one exit too many.

Three of the six are EXACT on all eleven coins - Open Long, Open Short and Cross Down, on the
candle, both ways - and the six together are 817 right of 830 with eighteen fired that are not
drawn. The run reading of the break beats the crossing on both sides again, which is now three
independent sets saying the same thing.

**Two coins were added on purpose.** On bitcoin alone the run reading fired NOTHING false, which
would have been a fine result and was not one: with hype and xrp beside it the false count is 44
and 62. A rule that is clean on one coin has been fitted to that coin.

**What the extra coins bought.** Over six of them the two distances that pick a stretch barely
separate the marked from the unmarked - medians of 3.40 against 2.47, and 1.88 against 1.75 - while
the VOLUME of the candle does: 2.4 and 3.1 against 1.6. Asking the marked candle for at least the
average volume of the last twenty cuts the false marks by a quarter on the fifteen minute set it
was found on (60 to 44 and 84 to 62) and by a fifth on the five minute set it was not (200 to 158
and 45 to 36), for two or three markers. It holds on both, so it is a rule and not a curve through
the noise.

Six of the eight are as good as exact: eleven differences over 483 markers. Three of those eleven
have been checked by eye and the marker WAS on the chart - the reading missed it and the scanner was
right. What is genuinely left is small and understood:

- One crossing on near where the second and the third line stand 0.00012% apart. That is a tie, not
  a crossing, and the reversal one candle later belongs to it. The smallest crossing the reference
  does draw stands 0.00028% apart, so a floor between the two would remove both - on the evidence of
  a single case, which is why it has not been built.
- Three exits the reference draws where the price has just passed the slow line, twice by less than
  a twentieth of a percent, and one that is a SECOND exit on the candle after the first. A rule
  built on a crossing cannot give two in a row.
- Two short exits on near where the price stands well past the slow line.

**Where the exits still differ, and why it is not a threshold (23 September 2026).** Over all five
harvests the reference draws 543 exits; we get 519 of them with 26 fired that are not drawn. Every
single miss is blocked by the SLOW LINE guard and every single extra passes it - both by a few
hundredths of a percent. So the whole disagreement sits ON that line, and four ways of moving it
were measured:

- **Another line as the guard.** The fast and second lines kill every exit (the crossing IS against
  the second line), the medium line costs half of them. The slow line is not just best, it is the
  only one that works.
- **Slack against the line.** Every step trades about one marker for three to seven false ones -
  0.02% buys two and costs six, 0.05% buys four and costs fourteen. The boundary is already where
  it belongs.
- **The wick instead of the close.** Recovers 23 of the 24 misses and pays 122 extra false ones.
- **An open position.** "Close Long" closing something is the natural reading and it is WRONG:
  of the exits the reference draws only 37% (long) and 47% (short) have a matching entry open,
  while of the ones only we fire 75% and 94% do. The reference keeps no position state; it draws
  the exit as a plain indicator condition.

So the four conditions in the code are the best of their family, the boundary is right, and the
residual is a band a few hundredths of a percent around the slow line where our guard and its
criterion disagree for a reason none of the above explains. 95.6% right is where this stands.

The exit conditions were weighed again over this reading, each switched on and off. The four in the
code are the best of them: dropping the slow line gains three markers and costs thirty-seven,
giving that line a tenth of a percent of slack gains two and costs three, and reading the exit as a
state instead of a crossing costs two hundred and thirty-eight.

The breaks are on the right level - the dotted lines were read off the chart as prices and agree
with the RSI rule to the cent - but not on the right candle. The reference draws them well AFTER the
level gives way: every candle beyond the level catches 98 of his 105, and fires 1276 times.

**What the break DOES look like, found on 23 September 2026.** Stop looking at single candles and
look at STRETCHES: unbroken runs of candles whose close stands beyond the level. Then a shape
appears that no single-candle search could see.

- Of 251 such stretches the reference marks 54 and leaves 197 alone.
- Inside a marked stretch the marks sit on candles that make a NEW EXTREME of that stretch: 87 of
  its 98, which is 35% of such candles against 4% of all the others.
- And on the first, second or third of them - 63 of the 98 - never more than three in one stretch.
- Stretches that earn marks carry more than twice the volume of those that do not (a median 3.35
  times the twenty candle average against 1.67) and bigger candles (2.02 against 1.43).

So the hard question is WHICH STRETCH, not which candle. That is a question about 251 things
instead of 7339, and a search at that level reaches 37% agreement on Breakout and 49% on Breakdown
- the highest of the whole investigation, and still not the rule.

`MacSettings.EntryOnBreakoutRun` builds the best of it: a stretch earns marks when its first candle
stands more than two average candle ranges past the SLOW LINE, and then the first three new extremes
of that stretch are marked, each of which has to carry at least the average volume of the last
twenty candles. One rule for both sides, which a rule fitted per side is not.

**The floor became a BAND on 23 September 2026, and that is the real repair.** Searched over 384
runs on eleven coins at fifteen minutes and judged on 322 runs at five, a condition may also be an
UPPER bound - and that is what was missing. The distance from the CLOSE to the slow line, which the
code asked for, barely separates the runs the reference marks from the ones it leaves alone: a
median of 3.98 against 3.89. What does separate them is the distance from the LEVEL to the slow
line, and only inside a band: too close and the level sits on the trend, too far and the move has
already happened and the reference does not mark it either.

| | drawn | hit with the floor | false | hit with the band | false |
| --- | --- | --- | --- | --- | --- |
| fifteen minutes, eleven coins | 203 | 139 | 272 | 126 | 179 |
| five minutes, four coins | 140 | 79 | 283 | 73 | 162 |

Thirteen markers given up for ninety-three fewer false ones, and six for a hundred and twenty-one.
The thresholds are the MIDDLE of the two searches (1.59 and 5.55 on fifteen minutes, 1.29 and 5.14
on five), so neither set got its own optimum.

**And the band takes the direction skew with it.** False marks per drawn marker were 1.19 up
against 1.46 down; they are now 0.87 against 0.89. By construction: the runs that stood absurdly
far under the slow line in a falling market are exactly what an upper bound removes. The normalised
measure that was built for that skew is no longer needed.

**Our stretch boundary is not the error - measured, and it defends itself.** The obvious remaining
explanation for the late starts was that the reference tracks its extreme per LEVEL rather than per
stretch, so a second visit to the same level has to clear the first visit's peak close. Counted both
ways over the same harvests, the per-level reading is WORSE on every set: the first marker sits on
the first extreme 62% against 64% on fifteen minutes, 36 against 41 on five, 39 against 45 on four
hours, 58 against 60 on daily. The stretch, reset whenever the close dips back inside the level, is
the better of the two definitions.

**The late start is still unexplained.** Of the stretches whose first marker is not our first new
extreme, the share where EVERY skipped extreme is blocked:

| candidate | 15m | 5m | 4h | 1d |
| --- | --- | --- | --- | --- |
| the second line and the volume floor now in the code | 53% | 41% | 18% | 14% |
| volume under the highest since the level was set | 60% | 59% | 47% | 57% |
| the close under the peak close of the run-up | 21% | 39% | 35% | 43% |
| volume under that of the level candle | 33% | 34% | 0% | 29% |
| the fast line not past the second | 0% | 0% | 0% | 0% |

The best of them was then tried as a real condition rather than as an explanation, on top of
everything already in the rule: asking for at least three tenths of the highest volume since the
level was set buys 27 false marks over the four sets and costs three markers - but it costs TWO of
those on daily while buying only two there, so it does not pull the same way everywhere. Not taken.

**The second line against the slow one, found 23 September 2026.** Of every break marker the
reference draws - 203 on fifteen minutes, 140 on five, 76 on four hours, 82 on daily - NOT ONE has
the second line on the wrong side of the slow line, read in the break direction. Of the candles it
does not mark, between 9% and 23% do. It is the only condition in this whole investigation that
reaches a hundred percent on all four sets AND separates.

Against the rule without it: 62 false marks fewer for one marker given up, pulling the same way on
every set (195 false becomes 172, 189 becomes 177, 105 becomes 83, 49 becomes 44). The FAST line
against the slow one says the same thing more weakly - free, no marker given up, but 26 false marks
instead of 62 - and is not in.

This came out of the second round with Fable, whose own top two hypotheses both died in the same
pass: the labels are not truncated by a Pine label cap (never more than 41 per chart, and only 2 to
17% of the unmarked candidates are older than the oldest drawn marker), and an RSI re-entry window
does not separate (at three candles: 71% of drawn against 64% of the rest on fifteen minutes, and
on four hours no separation at all).

**The reference marks AT MOST THREE per stretch - of ITS stretch, not ours.** Counted over the
drawn markers that are a new extreme: one, two or three per marked stretch on every timeframe, with
a single exception of four in 121 marked stretches on fifteen minutes. And they are almost always
ADJACENT: the next new extreme of the same stretch is the very next candle for 38% of them on
fifteen minutes, 55% on five, 57% on four hours and 71% on daily.

That kills the whole family of hindsight placements - the "keep only the newest label" idiom, one
label per stretch, a pivot with a right hand lookback - because every one of them predicts that the
candle after a marker is never itself a qualifying candle, and here it usually is.

What is left is sharper than what went before: the reference marks the first one to three
consecutive qualifying candles of a stretch, and **its stretches are not ours**. On four hours its
three markers land on our fifth, sixth and seventh new extreme, which can only mean its stretch
starts later than ours - something re-anchors the count inside what we read as one stretch. That,
and not the band, is now the open question.

**Four marks per stretch, not three.** Three was read off the five minute charts, where the
reference does not seem to go beyond it. Four harvests together say otherwise, and four is better on
every one of them - fifteen minutes 126 hit and 179 false becomes 138 and 195, five minutes 73 and
162 becomes 84 and 189, four hours 29 and 90 becomes 35 and 105, daily 30 and 39 becomes 34 and 49.
Five is better again on fifteen minutes and flat everywhere else, so it is not taken.

**Seven weeks of FOUR HOUR candles say the cap is what hurts, not the band.** Harvested 23
September over the eleven coins, 4 August to 23 September 2026: 281 markers, of which 91 breaks -
and this set leans the other way round from the daily one, 68 Breakouts against 23 Breakdowns. Here
too the four markers that should be exact ARE exact (Open Long 46 of 46, Open Short 38 of 38).

What it shows is that the run MODEL is sound everywhere and the counting is not. Of the break
markers the reference draws, the share that sits beyond the level is 100% on fifteen minutes, four
hours and daily, and 95% on five minutes; the share that also makes a new extreme of its stretch
runs from 79% to 94%. But WHERE in the stretch they sit differs wildly:

| | first three new extremes | median new extremes in a marked stretch |
| --- | --- | --- |
| fifteen minutes | 155 of 184 | 2 |
| daily | 58 of 72 | 4 |
| five minutes | 87 of 119 | 3 |
| four hours | 33 of 68 | 7 |

The level does not go stale any longer on four hours (a median of 15 candles against 14), so this is
the market and not the rule: over that stretch of 2026 a level that gave way kept making new
extremes for a long time, and a cap counted from the start of the stretch throws away the tail.
Raising the cap to four buys six of them back; the rest needs a different idea about where a stretch
ends.

**The crossings on the coarse intervals were a READER fault, and it is fixed.** They looked like
a scanner error and they were not: Cross Up 29 drawn against 43 fired and Cross Down 26 against 37
on four hours, 33 against 41 and 20 against 30 on daily, and never a miss. A rule that only ever
fires TOO OFTEN and never too seldom is a rule the reference holds and we do not - except here the
reference held nothing.

Two of the eight markers are an XCROSS: two diagonal strokes that touch only at the corners. The
reader flood filled FOUR way, which breaks such a shape into fragments of two to four pixels, each
one under its size floor - so the marker vanished. Only where the anti-aliasing thins the strokes,
which is over the CLOUD fill, and the cloud covers most of the pane on a coarse chart. Proven on
bitcoin at four hours, 9 September 04:00: four way returns six markers and not that one, eight way
returns it at 13 pixels while every other marker in the same view is 26 to 42 pixels. Eight way
connectivity and a floor of eight pixels, and after re-harvesting both coarse sets:

| | four hours | daily |
| --- | --- | --- |
| Cross Up | 29 of 43 becomes **42 of 43** | 33 of 41 becomes **41 of 41** |
| Cross Down | 26 of 37 becomes **37 of 37** | 20 of 30 becomes **30 of 30** |

The lesson is the one this project keeps relearning: when the scanner looks wrong, distrust the
reader first. See the tradingview-chart-api skill.

**A YEAR of daily candles says the thresholds do not travel.** Harvested 23 September over the same
eleven coins, 26 November 2025 to 22 September 2026: 213 markers, of which 91 breaks - almost half
again what the fifteen minute set holds. On that set the four markers that should be exact ARE
exact (Open Long 31 of 31, Open Short 20 of 20, Close Short 20 of 20), which is what proves the
harvest. But the break band as tuned keeps only 30 of its 82 break markers, while the old floor
kept 56. The daily set's own best band is 1.95 to 12.25 - more than twice as wide.

| band | fifteen minutes | five minutes | daily |
| --- | --- | --- | --- |
| 1.4 to 5.35 (in the code) | 126 hit, 179 false | 73 hit, 162 false | 30 hit, 39 false |
| 1.4 to 12 | 137 hit, 266 false | 80 hit, 275 false | 56 hit, 86 false |
| no upper bound | 137 hit, 266 false | 80 hit, 285 false | 56 hit, 93 false |

So the measure is not scale free after all, even though it is counted in average candle ranges, and
no single cap wins on all three. The band stays at 1.4 to 5.35 because that is where the two sets
the strategy actually trades on agree; the daily disagreement is the next thing to solve, not a
reason to undo the repair.

**The cloud condition was thrown out the same day, and it is a WASH, not a win.** It asked
for one and a half candle ranges past the far edge of the cloud on top of the distance to the slow
line. Measured both ways with the same code and the same harness:

| | drawn | hit with the cloud | false with it | hit without | false without |
| --- | --- | --- | --- | --- | --- |
| fifteen minutes, eleven coins | 203 | 134 | 253 | 139 | 264 |
| five minutes, four coins | 140 | 76 | 252 | 79 | 274 |

Five markers bought with eleven false ones on the one set, three bought with twenty-two on the
other. It is out because one condition beats two when they perform the same, not because it
measured better. An earlier note here claimed a gain of one and a half points of agreement; that
came from a different measure and does not survive counting the markers themselves.

**Known bias of what is left, measured and not repaired.** The reference is direction blind: it
marks 0.54 of the stretches up and 0.53 of the stretches down. Our filter is an ABSOLUTE distance
past the slow line, and in the falling market of 4 to 14 September the median stretch stood 4.50
candle ranges under the line against 2.99 over it. So it lets 65% of the down stretches through
against 54% of the up ones: 1.26 (more stretches) times 1.20 (more passed) against 1.24 more drawn
is the 22% excess that shows in the table - 1.45 false per drawn Breakdown against 1.12 per drawn
Breakout. Dividing the distance by its own hundred candle average removes the bias (0.76 against
0.82) and costs a third of the five minute agreement, so it was not adopted.

| | crossing: same | crossing: too many | run: same | run: too many |
| --- | --- | --- | --- | --- |
| Breakout, five minutes | 17 of 95 | 211 | 52 of 95 | 222 |
| Breakdown, five minutes | 11 of 45 | 81 | 27 of 45 | 52 |
| Breakout, fifteen minutes | 28 of 91 | 78 | 63 of 91 | 102 |
| Breakdown, fifteen minutes | 32 of 112 | 89 | 76 of 112 | 162 |

**A percentile criterion was built and measured too.** Instead of a fixed number of candle ranges
it asked the distance to stand in the top part of its own last hundred stretches. Measured side by
side against the fixed distance it is WORSE on both the fifteen and the five minute set, so the
fixed distance stays.

Searched on the five minute charts the best thresholds ARE the two above, and on the daily charts -
which that search never saw - they lift agreement from 21.5% to 27.5%. Searched the other way round
the daily charts ask for 2.5 and 1.5, and those lift the five minute charts from 14.0% to 16.1%. So
the thresholds are stable across two independent sets. Most of the gain is in the counting layer
though, not in the thresholds: the first-three-new-extremes reading alone takes the five minute
agreement from 5.6% to 14.0%.

What Breakout is NOT, each ruled out on this reading:

- Not the whole candle body beyond the level. Thirty of his hundred-and-five stand with their body
  still astride it, so the stricter reading costs markers instead of gaining them.
- Not the four lines standing in order. Twenty-eight of his are drawn while they are not.
- Not the moment of breaking through. Read as a crossing rather than a state it catches twenty of a
  hundred.
- Not a level that flips to the other side once broken, though the course material says the colour
  flips: that catches twenty of a hundred-and-five.
- Not a channel break. No lookback from five to a hundred candles stands out from the rest.
- Not a higher timeframe handed back to this chart. His markers fall evenly over all twelve minutes
  of the hour; a higher timeframe would pile them on a few.
- Not a level that expires after a while: no lifetime from five to a hundred and fifty candles
  stands out, and his marks sit at every level age.
- Not several levels at once. Every dotted line on the snapshots was found and counted: it is one
  support and one resistance at a time, which is what was modelled all along.
- Not the strength index breaking its own ceiling, and not the moving averages rather than the price
  crossing the level. Both under 10%.
- Not any pair or triple of thresholds over twenty-three measurable properties. An exhaustive search
  over four hundred and fourteen conditions reaches 38% agreement on Breakdown and 27% on Breakout,
  against 13% for marking everything beyond the level.

Volume and candle size do come into it - the drawn ones carry 2.3 times the average volume against
1.07 for the rest, and 30% of candles over three ATR are marked against 2% of those under one - but
nothing built on them separates his from the others.

A note on how the reading is kept honest, because earlier counts said something else. The snapshot
reader has been wrong five times in ways that looked like scanner errors:

1. It took the legend at the top of the chart for markers - a letter is seven by eleven pixels in
   the very colours the markers use. The baseline gives a word away, but asking for two neighbours
   within three hundred pixels was far too loose and threw out three real exits of one afternoon
   that happened to line up. It now asks for three neighbours of the SAME marker within sixty.
2. It took the chart's own furniture for markers: an empty blue square of the toolbar, three hundred
   pixels under the candles, was read as an exit for days on end. A marker belongs to its candle in
   the height as well, within a dozen candle widths.
3. It lost the crosses. A dotted level line five pixels thick rubs out the middle of a cross lying
   on it and leaves four arms of two to four pixels, under every floor there is. The lines are now
   wiped out of the mask first and the arms find each other over five pixels.
4. It lost markers split by a grid line of the chart - the magenta triangle of an Open Short came
   back as two halves four pixels wide. Pieces may now be glued side by side over a gap of two.
5. It lost a cross covered by an Open Short triangle drawn on the same candle, where only the four
   tips stick out: ten pixels in all, against a floor of twenty-four.

Every one of those cost signals the scanner fired and the reading did not have. Together they
account for 34 of the 45 differences the six working markers showed before they were found.

## The exit on the second line

`ExitOnSecondLineCross` is the reference's Close Long and Close Short. Four things have to hold
together, and only the first of them was there until 22-09-2026:

1. the close crosses the second line against the position;
2. the cloud still points the way of the position - the fast line on our side of the second one.
   Without this the same candle is an exit for a long AND for a short, which is how it fired 314
   times against the 134 the reference draws;
3. the close is still on the position's side of the SLOW line. Past that line the trend itself is
   gone and the reference stops warning;
4. the cloud is STACKED that way too - the second line on our side of the third. This is the same
   relation the line crossing marks, so an exit only prints while that crossing stands.

The fourth is the sharpest. Of 629 crossings over four coins the reference marks 178, and **not one
of those 178** has the second line on the wrong side of the third.

Counted against the chart, on the candle, over four coins on five-minute candles:

| | it draws | the scanner fires | same candle | missed | too many |
| --- | --- | --- | --- | --- | --- |
| Close Long | 134 | 128 | 124 | 10 | 4 |
| Close Short | 52 | 52 | 50 | 2 | 2 |

Before the three conditions that was 314 and 315 fired, 16 missed and 437 too many. Of what is left
over, three of the misses are a hair's breadth on the slow line - the close sits 0.004%, 0.051% and
0.066% on the wrong side of it. A tolerance of 0.05 ATR would recover one of them and is left out
on purpose: one signal is not worth a knob that has to be tuned.

`MacChartOverlay` draws these markers with the SAME four conditions. A chart that draws what the
strategy would never fire is worse than no chart.

## Settings

| Setting | Default | What it does |
| --- | --- | --- |
| Entry on breakout | on | Fire on the break through the last pivot level |
| Entry on cloud cross | off | Fire on EMA(20) crossing EMA(40) |
| Entry on springboard bounce | off | Fire on the pullback to the fast line inside a trend |
| Entry on line cross | off | Fire on EMA(40) crossing SMA(50), before the cloud turns |
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

## Support and resistance

Two ways to put a level down, and the setting `UseRsiLevels` picks between them.

**The price pivot** (default): a candle higher than the `PivotLeftCandles` before it and the
`PivotRightCandles` after it. Simple, and it puts a level down every few candles.

**The RSI crossing**: a support is the LOW of the candle on which RSI(14) crosses back up through
`RsiLevelSupportCross`, a resistance the HIGH of the candle on which it crosses back down through
`RsiLevelResistanceCross`. One of each is carried forward until the next crossing replaces it.

The second is measured rather than invented. Over 45 level readings on three coins it reproduces
every one of them to the cent, and the two bounds are not free: at 30/70 it reproduces none of them
and at 40/60 half. It also puts far fewer levels down - ten to sixteen per five hundred candles
against one every few candles - which is what a chart actually shows.

A level is only handed to the strategy from the candle AFTER the one that set it. A level set on
the candle in hand would be broken by that same candle, because the low of a candle always sits
under its own close.

## What has been measured

Every number below is the sum of two period halves of 2026 (January to April and May to August) on
66 coins with 10,000 USDT of start capital, each half judged on its own. A cell only counts when
both halves agree: the halving test is what sorted the real effects from the noise, and the noise
floor on one half is about 15 USDT.

Settled so far:

- **The daily candle carries the result.** 4h loses in the second half whatever is done to it, and
  1h worse. 8h and 12h have not been measured yet.
- **The two triggers together beat either one apart** - the breakout and the cloud cross fire on
  different days and their profits add up rather than overlap.
- **A breakout buffer helps**, and it works by leaving trades out: the count drops from 171 to 143
  and the profit rises. It flattens off from 2%, with 2.5% the best measured.
- **A wide target with a small stop**, and the stop wants more room than first thought: stop 5 with
  target 24 beats stop 4 with target 24, which beats stop 3. Targets of 18 and below lose ground.
- **The position duration limit does nothing.** 7, 14 and 28 days give the same result to the cent,
  because positions close well before that.

Ruled out, each on a measurement in both halves:

- **Minimum cloud width** - 6% and up costs money, and it gets worse the tighter it is set.
- **The MA200 entry condition** - it costs money here three times over, unlike in dbr.
- **Moving the stop to break-even** - it turns 79 of 94 trades into winners and nearly all the
  profit into nothing.
- **A trailing stop on the daily candle** - it costs money on the bare breakout.

Measured since, on both halves:

- **The pivot lengths and the maximum level age barely matter** - 3, 7 and 10 candles either side,
  and ages of 10, 20, 40 and 200 candles, all land within noise of the default.
- **The volume filter empties the strategy**: at three times the average it leaves four trades in a
  half, which is no measurement at all.
- **The springboard on its own loses money** whatever slope is asked of it; added to the two other
  triggers it costs about a third of the result.
- **8h and 12h both lose in the first half**, like 4h before them. The daily candle stays the one.
- **Exit on cloud flip is a small plus**, one or three candles alike.
- **Bijkopen stays bad**: one level costs a third, two levels lose money outright.

Still open: the RSI crossing levels, and Breakout and Breakdown - the only two of the eight markers
that are not reconstructed. Because `EntryOnBreakout` is on by default and `EntryOnBreakoutRun` is
off, the strategy currently enters on the WORSE of the two readings: the crossing agrees with the
reference on 28 of its 140 five minute marks and 60 of its 203 fifteen minute ones, the run on 79
and 139 of the same. Which of the two earns more MONEY has not been measured yet - that needs emulator runs,
and until they are done the default stays where it is.

## The break, searched to its ceiling (24 September 2026)

The six markers that are reconstructed sit at 1689 of 1717 on the exact candle, and every one of
the 28 misses is a Cross Up on the first two four hour candles of the judged window - before our
own history is 200 candles deep, so they can never be hit. On comparable ground the six are exact.

The other two were taken apart properly this time, with a measuring instrument built for it:
`MacAgainstTheReferenceTests.WriteTheCandleFacts` writes every candle of all five sets with the
numbers the rule is allowed to see, and `Tools/EntryTiming/_doorbraak_speeltuin.py` rebuilds the
stretches from that file. The rebuild is checked against the shipped code first - the same ranks on
96 443 of 96 446 candles, the same verdict on 3971 of 3973 stretches - so a candidate rule can be
tried in a second instead of by rebuilding the project.

**What the reference actually does, measured on 990 stretches over eleven coins and four
timeframes.** It marks 273 of them, so three in four have to be turned away, and that selection is
the whole problem: of every false mark, half sits more than ten candles from anything the reference
drew - a stretch it never touched at all - and only a third sits within three candles of one.

- It never draws more than **three** marks in one stretch: 141 of 142 up, 130 of 131 down.
- Its marks sit at the START of the stretch: a quarter on the first candle, 71% within four.
- The stretches it marks **run long** - a median of 7 candles against 2 for the ones it ignores.
  That is not knowable when a stretch starts, which is exactly why it cannot be used.

**Three readings of the selection, each fitted on the fifteen minute set and judged on the four
sets that took no part in the fitting, and then the other way round as a check.** The number is the
harmonic mean of hit rate and false rate.

| the selection | fitted set | the four others |
|---|---|---|
| the distance from the LEVEL to the slow line (what shipped) | 0.535 | 0.413 |
| the thickness of the cloud, with the close clear of it | 0.548 | 0.437 |
| a fitted model over all eighteen numbers we can measure | 0.511 | 0.395 |

The middle one is now in the code: at the stretch's first candle the four lines may stand at most
**4.1** average candle ranges apart, and the close has to stand at least **0.75** past the edge of
them. Both searches landed on the same pair (4.0 and 4.25 for the thickness, 0.75 for the clear),
so the middle of the two is used and neither set got its own optimum. Over all five sets: 297
markers on the exact candle becomes 307, 212 missed becomes 202, 480 false becomes 462.

**The last line of that table is the important one.** A model with a free hand and every number we
measure does WORSE than a two-threshold rule. Whatever the reference uses to pick its stretches is
not a combination of what we can see. Three more routes were tried and all of them land at or below
what already ships:

- **Refitting the old band** on all five sets: 0.476 at best against 0.462, and only by raising the
  marks per stretch, which buys hits with false marks.
- **Marking the first candles of the stretch** instead of its first new extremes: worse on every
  variant (0.370 to 0.433 against 0.462), with or without the volume condition.
- **Dropping the level entirely** and marking a close that betters the last N. This looked the most
  promising of all - 85% of the up markers sit on a close that betters the last fifty, against 7%
  of all candles - but the precision is not there: 0.45 fitted, 0.38 on the rest.

Per candle the separation is real and consistent across all four sets - volume against its twenty
candle average separates at 0.79, candle size at 0.77, body at 0.75, the close's distance past the
cloud at 0.75 - but it is already spent. Adding thresholds on top of the rule that ships buys 0.011
on the set it is fitted on and 0.002 on the rest, which is nothing.

So the break markers are not a tuning problem any more. They are at the ceiling of what these
measurements can express, and the next step is not another threshold but a different kind of
evidence about the reference itself.

## The picture reader was lying, and every break number with it (24 September 2026)

The reference's markers are PLOTS, and a plot is a series of numbers the chart will hand over:
`getStudyById(id)._study._data` carries, per bar, the four lines, the eight markers as 0 or 1, and
its own Support and Resistance. See the tradingview-chart-api skill for the mapping.

Held against the picture reader on bitcoin at fifteen minutes, over the window every earlier
measurement used:

| marker | the indicator's own plots | the picture reader |
|---|---|---|
| Open Long, Open Short, Close Short, Cross Up, Cross Down | 13, 13, 15, 16, 16 | the same, exactly |
| Close Long | 13 | 15 |
| **Breakout** | **9** | **14** |
| **Breakdown** | **12** | **27** |

Not one drawn marker was missed - the reader found all 107 - but it INVENTED 22, and 20 of those
were the two break markers. So every break figure in the sections above was fitted against targets
that were part noise, and the ones that matter are re-measured here. The six that were exact stay
exact, which is why nothing else moves.

**Everything was harvested again from the plot values**: eleven coins at fifteen minutes, four
hours and daily, four at five minutes, one at an hour - 4067 markers against 2226 before, and
exact by construction.

| marker | drawn | fired | same candle | missed | too many |
|---|---|---|---|---|---|
| Open Long | 488 | 488 | **488** | 0 | 0 |
| Open Short | 483 | 483 | **483** | 0 | 0 |
| Cross Up | 583 | 584 | **583** | 0 | 1 |
| Cross Down | 578 | 579 | **578** | 0 | 1 |
| Close Long | 598 | 589 | 567 | 31 | 22 |
| Close Short | 484 | 491 | 464 | 20 | 27 |
| Breakout | 477 | 687 | 282 | 195 | 405 |
| Breakdown | 376 | 451 | 205 | 171 | 246 |

**The four entry and crossing markers are now exact on 2132 of 2132**, on five timeframes and
eleven coins, with two extra between them.

**What the exact targets changed about the break.** The volume a candle carries turns out to be by
far the strongest single number: over 9082 candles beyond a level it separates a marked candle
from an unmarked one 0.80 of the time, and it does so on every set on its own (0.80 / 0.79 / 0.81 /
0.81). The candles the reference marks sit at a median of 2.06 times the twenty candle average,
the ones it ignores at 1.07 - and the rule asked for 1.0, which turns almost nobody away.

Raising it was measured before and looked worthless. Against the real markers it is the best move
available: fitted on the fifteen minute set and judged on the four others, 1.3 wins on BOTH.

| volume share | fitted set | the four others | same | missed | too many |
|---|---|---|---|---|---|
| 1.0 (what shipped) | 0.504 | 0.439 | 540 | 313 | 885 |
| **1.3** | **0.511** | **0.459** | 486 | 367 | 656 |
| 1.6 | 0.503 | 0.427 | 416 | 437 | 508 |

The cloud rule that replaced the distance band earlier the same day survives the correction as
well - 0.474 against 0.456 for the band - so that change stands.

**What did NOT change.** The break is still the weakest of the eight by a wide margin, and the
shape of the failure is now clearer, not better: the reference draws 477 Breakouts and we fire 687.
Three routes were searched again against the exact targets - the distance band refitted, the size
and body of the candle added, the level dropped for a Donchian break - and none of them beats what
is in the code.

## The level is live on the candle that sets it (24 September 2026)

Until now the level was held back one candle: what a candle was allowed to see was the level as it
stood before it. The reason given was that a candle must not break a level of its own making.

**That cannot happen.** A resistance IS the high of its candle and a close never exceeds its own
high; a support IS the low and a close never falls below it. The guard was protecting against
something arithmetic already rules out, and it cost a candle everywhere.

The reference does not wait. Its Support and Resistance are plots, so the numbers can simply be
read, and held against ours over 71 changes of both levels on bitcoin at fifteen minutes:

| our rule | support matched | resistance matched |
|---|---|---|
| the level of the PREVIOUS candle | 55 of 71 | 59 of 71 |
| the level of THIS candle | **71 of 71** | **71 of 71** |

Every difference sat on the very candle the level moved. After the change, checked again against
the reference's plots on bitcoin at one hour: **47 of 47 supports and 47 of 47 resistances**, exact.

**What it costs in markers: nothing.** Measured over all five sets before deciding, and again after
the change - 282 Breakouts and 205 Breakdowns on the exact candle either way. It is in because the
reference's own numbers say what the right answer is, not because it earns anything.

The four fields that carried the delayed copy are gone with it; `_rsiLevelHigh` and `_rsiLevelLow`
are now what gets published, and `RsiLevelHighAge` is zero on the candle that sets the level.

## The exit is the FULL stack, and it is exact (25 September 2026)

Close Long and Close Short sat at 95% for months, and the reason turns out to be one condition that
was nearly right.

The exit asks that the close crosses back through the second line, and then three things about the
lines. Two of them were right: the fast line on the position's side of the second, and the second on
its side of the third. The third asked that **the close** was still on the position's side of the
slow line. It should have asked that **the third line** was.

Put together that is the full stack - `fast > second > medium > slow` for a long, the mirror for a
short - and it is exact. Over **4282 crossings of the second line** on eleven coins and five
timeframes the reference draws 1083 close markers:

| rule | hit | missed | too many |
|---|---|---|---|
| close still on our side of the slow line (what shipped) | 1032 | 51 | 52 |
| **the full stack** | **1083** | **0** | 3 |
| both together | 1032 | 51 | 3 |

Not a single miss on any set or either side: 335 and 300 at fifteen minutes, 210 and 123 at five,
42 and 32 at four hours, 7 and 20 daily, 4 and 10 at an hour.

**Both halves of the old error are explained by the same swap.** The 51 markers it missed all had
the third line still stacked while the close had slipped past the slow line; the 49 false ones all
had the close on the right side while the third line had already given way. That is what a proxy
does when it is close to the real thing but not it.

**How it was found.** The 51 misses and 52 false ones were put side by side and every measurable
number scored on how well it told the two groups apart. Two stood out: how far the close went
through the second line (0.98) and the third line against the slow one (0.94) - while on the full
population of 4282 crossings the first says nothing at all (0.47). That is the signature of an
interaction, and following it landed on the stack, after which the depth of the crossing was no
longer needed for anything.

**The three that remain** are all fifteen minute Close Shorts, all far from the slow line (4.7 to
6.5 average candle sizes against a median of 1.96) with a steeply sloping slow line. Three in 4282
is left alone rather than fitted.

With this in, **six of the eight markers are exact**: 488 Open Longs, 483 Open Shorts, 583 Cross
Ups, 578 Cross Downs, 598 Close Longs and 484 Close Shorts, all on the candle, with two extra
crossings in 1161. Only Breakout and Breakdown are left.

**One measurement note.** The last bar of a harvest is the one still RUNNING when it was read, so
the reference's values on it come from a partial close. Four differences sat there - one miss and
three extras, all four on 24 September 13:00 on four different coins - and the comparison now leaves
that bar out.

## The two crossings left over, and why they are not a rule (25 September 2026)

Cross Up and Cross Down hit 583 of 583 and 578 of 578, with one surplus each. Both come from ONE
event: on NEAR at five minutes, 21 September 02:45, our second line crosses the third by
**0.0000049646** on a price of 4.18 - 0.00012% - and crosses back the candle after, which produces
the second surplus. The reference has no crossing there at all.

That is a tie at the seventh decimal of two moving averages, not a difference in the rule. The
proof that no threshold can fix it: of the 1160 crossings the reference DOES draw, ten are tighter
than this one. Any margin that removed it would remove ten real markers with it.

What would settle it is matching TradingView's EMA seeding exactly, which is worth doing only if
something else ever depends on it.

## The break, after everything (25 September 2026)

With exact targets and the full harvest, the break markers were attacked once more and the answer
is the same, now proven three ways over 7976 candles beyond a level carrying 838 markers.

**What IS established, and it is not nothing:**

- **The level is right.** 838 of the 853 drawn markers - 98.2% - sit on a candle that closes beyond
  our level. The 15 that do not are all outside any stretch.
- **The stretch reading is only three quarters right.** Just 724 of 853 (84.9%) sit on a candle that
  makes a new extreme of its stretch. The other 129 are inside a stretch without bettering it, and
  no amount of tuning the counter reaches them.
- **The reference never marks a weak candle.** Of 838 markers not one sits under a strength index of
  56.99, and of the 209 candidates under 55 it marks none. It is a real property - and useless to
  us, because the conditions already in the rule exclude every one of those candidates anyway.

**Three ways of looking for the rule, all landing in the same place** (harmonic mean of hit rate and
false rate, fitted on the fifteen minute set and judged on the four that took no part):

| | fitted set | the four others |
|---|---|---|
| the rule as it ships | 0.508 | 0.462 |
| the sharpest number separating the misses from the false ones | 0.757 on the errors, 0.389 on everything | - |
| a decision tree with a free hand over all seventeen numbers | 0.567 | **0.461** |

The last line is the one that decides it. A tree that may combine anything it likes, at any depth,
does not beat two hand written thresholds out of sample. Six structural readings were measured
beside it - every candle beyond the level, the first candle of the stretch, the first four candles,
new extremes with and without the stack, a big candle with the stack - and all of them are worse.

**The method that cracked the exit does not work here.** There, putting the 51 misses beside the 52
false ones showed one number separating them at 0.94 while saying nothing on the population as a
whole, and that pointed straight at the missing condition. Here the same comparison produces 0.757
at best, and the numbers that separate the errors are the ones already in the rule.

So the break markers are not reproducible from the four lines, the two levels, the candle and its
volume. Whatever TBO uses is something else, and the next step is evidence about the indicator, not
another threshold.

## The break hardly uses the four lines at all (25 September 2026)

TBO has one input, "TBO Speed", and it only moves the LENGTHS of lines two, three and four -
Standard 20/40/50/150, Fast 20/30/40/80, Slow 20/50/100/200. The first line never moves, and the
two levels do not move either, which is the proof that they are computed from the RSI and not from
the cloud.

So switching the speed is a controlled experiment: it changes the lines and nothing else. If a
marker moves, it is built on the lines; if it stays, it is not. Measured over six coins at fifteen
minutes, 1320 markers, on identical windows - how many of the Standard markers still sit on exactly
the same candle after the switch:

| marker | Standard | still there on Fast | still there on Slow |
|---|---|---|---|
| Cross Down | 192 | 4% | 1% |
| Cross Up | 195 | 8% | 2% |
| Open Short | 155 | 26% | 20% |
| Open Long | 156 | 22% | 28% |
| Close Short | 154 | 37% | 40% |
| Close Long | 199 | 40% | 25% |
| **Breakdown** | 113 | **77%** | **63%** |
| **Breakout** | 156 | **90%** | **68%** |

The six markers that are solved move almost completely, which is exactly right: every one of them
IS a statement about the lines. **The two break markers barely move.** Nine in ten Breakouts survive
a change that redraws three of the four lines.

**That says our whole approach to the break is built on the wrong foundation.** The conditions in
the code - the thickness of the cloud, the close clear of its edge, the second line against the
slow one - are all statements about lines the reference hardly consults here. It also explains why
nothing helped: we were tuning a filter on the wrong quantity.

What the numbers say the shape is:

- There is a **core** that is line independent - the level, the price and the volume - and it
  decides which candles are candidates. That is where the reconstruction is weakest: only 84.9% of
  drawn markers make a new extreme of their stretch, so the counting model itself is wrong for one
  in six.
- There is a **filter** on top that does use the lines, and it is mild. Going to Fast, 141 of 156
  Breakouts stay, 15 fall away and 37 new ones appear; the count goes UP with faster lines and DOWN
  with slower ones, which is what a filter does that is easier to pass when the cloud is tighter and
  the slow line nearer.

So the shape of the current rule is not wrong - a core plus a line-based filter is right - but the
core is. The next work is on the core, and it may not use the four lines at all.

## The break is solved: the wick, a hundred candles, and a counter on the POSITION (25 September 2026)

Three things were wrong at once, which is why it resisted for so long.

**1. The extreme is on the WICK, not the close.** Every one of the 747 markers outside the warm-up
betters the highest high (lowest low) of the candles before it on the wick. On the close only 85%
do — that gap was "fact 2", the 15% ceiling that no amount of tuning could pass, and it was an
artefact of measuring the wrong series.

**2. The window is a hundred candles, and it is literal.** 747 of 747 at N=100, 743 at N=101, 620
at N=130. An edge that sharp is a number typed into the source. It is not one of the four line
lengths, and it does not move when the indicator's Speed input does.

| N | markers hit | candidates that pass |
|---|---|---|
| 60 | 747 of 747 | 33% |
| 99 | 747 of 747 | 28% |
| **100** | **747 of 747** | **28%** |
| 101 | 743 of 747 | 27% |
| 110 | 713 of 747 | 27% |

**3. The counter belongs to the POSITION, not to the stretch.** It restarts on the candle the fast
line crosses the second — the same candle that draws Open Long or Open Short — and lets three
through, and nothing is drawn in the first five candles of a position.

| | hit | precision |
|---|---|---|
| counter per stretch, cap 3 | 93% | 43% |
| counter per position, cap 3 | 89% | 81% |
| and five candles after the entry | **91%** | **87%** |

**And volume is not a condition at all.** It had the strongest separation of any single number
(0.80 on every timeframe) and it was standing in for the hundred-candle extreme the whole time.
With the extreme in, asking for the twenty-candle average costs six points of agreement and asking
a third more costs fourteen. It is out.

### What it does to the measurement

| | before | after |
|---|---|---|
| Breakout | 282 of 477, 411 false | **430 of 477 (90.1%), 57 false** |
| Breakdown | 205 of 376, 248 false | **347 of 376 (92.3%), 51 false** |

Held apart: the fifteen minute set gives 91% hit at 87% precision, the four sets that took no part
in any choice give 92% at 88%. The out-of-sample half is the better one, so nothing here is fitted
to noise.

### All eight, over eleven coins and five timeframes

| marker | drawn | fired | same candle | missed | too many |
|---|---|---|---|---|---|
| Open Long | 487 | 487 | **487** | 0 | 0 |
| Open Short | 482 | 482 | **482** | 0 | 0 |
| Cross Up | 583 | 584 | **583** | 0 | 1 |
| Cross Down | 577 | 578 | **577** | 0 | 1 |
| Close Long | 598 | 598 | **598** | 0 | 0 |
| Close Short | 484 | 484 | **484** | 0 | 0 |
| Breakout | 477 | 487 | 430 | 47 | 57 |
| Breakdown | 376 | 398 | 347 | 29 | 51 |
| **together** | **4064** | **4098** | **3988** | **76** | **110** |

**98.1% of everything TBO draws, on the exact candle.**

### What is left

76 missed and 110 false, all in the two break markers. What is known about the residual: every
position that contains a miss also contains a false fire, so the reference is skipping candidates
that we count and its third mark is our fourth. The five-candle wait is a crude stand-in for
whatever does that skipping. Per timeframe the model is weakest on four hours and five minutes.
