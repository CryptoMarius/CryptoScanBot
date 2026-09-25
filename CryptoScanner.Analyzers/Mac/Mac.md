# MAC

Four moving averages, two levels taken from the strength index, and four kinds of signal that come
out of the combination. The strategy trades the signals; the chart overlay draws them.

## The four lines

| line | default | what it is |
| --- | --- | --- |
| fast | EMA(20) | the line a pullback bounces off |
| second | EMA(40) | the one entries and exits turn on |
| third | SMA(50) | the one the trend crossing goes through |
| slow | SMA(150) | the trend itself |

Together they make the "cloud". It points UP when the fast line is above the second one, and it is
fully STACKED for a long when `fast > second > third > slow` - the mirror of that for a short.

The four lengths move together through one setting, `Speed`:

| speed | lengths |
| --- | --- |
| Standard | 20 / 40 / 50 / 150 |
| Fast | 20 / 30 / 40 / 80 |
| Slow | 20 / 50 / 100 / 200 |
| Custom | whatever the four length settings say |

The fast line stays at 20 in all three: it is the line the price is measured against, not part of
what the speed changes.

## The two levels

A **support** is the LOW of the candle on which RSI(14) crosses back up through 35.
A **resistance** is the HIGH of the candle on which it crosses back down through 65.

One of each is carried forward until the next crossing replaces it, and the level is live on the
candle that sets it - a candle can never break a level of its own making, because a close never
exceeds its own high nor falls below its own low.

These are not price pivots. A symmetric pivot puts a level down every few candles and most of them
carry nothing; this puts ten to sixteen down in five hundred candles, which is what a chart shows.

## The four signals

Each has a long and a short form, so there are eight markers in all. Every one of them is a
statement about a candle that has CLOSED; nothing is drawn or fired on a running candle.

### Open Long / Open Short

The fast line crosses the second one: above it for a long, under it for a short. This is the entry
the strategy is built around.

### Cross Up / Cross Down

The second line crosses the third one. It happens BEFORE the cloud turns, so it runs ahead of the
entry and is not confirmed by it - on the chart it reads as "the trend is turning".

### Close Long / Close Short

The close crosses back through the second line against the position, AND the cloud is still fully
stacked the way of that position.

The stack is the whole rule and both halves of it matter. A close falling back through the second
line only means something while the lines behind it are still in order; once the third line has
given way, the trend it was part of has gone and the crossing is noise.

The strategy uses this in two ways. As an EXIT (`ExitOnSecondLineCross`) it closes the position it
belongs to. As an ENTRY (`EntryOnCloseMarker`) it opens one the other way, which is deliberately
counter-trend: the cloud is by definition still pointing the old way when it fires.

### Breakout / Breakdown

Four conditions, all on the candle in hand unless said otherwise:

1. The CLOSE stands beyond the level - above the resistance for a Breakout, below the support for a
   Breakdown.
2. The WICK betters the highest high (lowest low) of the **hundred** candles before it. The wick and
   not the close: a candle that takes out a hundred candles of highs on its way up has done the work
   even if it gives some back before it closes.
3. The close **nine** candles back already stood past the second line. This keeps the marker out of
   the first candles of a position, where the price has only just crossed and a break means little -
   and it does the same inside a position, where one candle dipping under the second line nine bars
   earlier is enough to skip one.
4. The second line stands on the break's side of the slow one.

A counter runs per POSITION - it restarts on the candle the fast line crosses the second, the same
candle that draws Open Long or Open Short - and the first `BreakoutEntriesPerRun` candidates are
marked. Three by default.

Each of those numbers is a sharp edge rather than a dial: a window of ninety-nine or a hundred and
one both cost signals, a lag of eight or ten is worse in both directions, two marks per position is
too quiet and four brings in a long tail of late ones.

The fast line against the second is NOT tested - conditions 3 and the counter's anchor imply it
between them. Volume is not a condition either: it correlates with the hundred-candle extreme and
adds nothing once that is in.

## Settings

### Which signals open a position

| setting | signal | default |
| --- | --- | --- |
| `EntryOnOpenMarker` | Open Long / Open Short | **on** |
| `EntryOnCrossMarker` | Cross Up / Cross Down | off |
| `EntryOnCloseMarker` | Close Long / Close Short | off |
| `EntryOnBreakMarker` | Breakout / Breakdown | off |

Only the first is on, because it is the only one of the four that is an entry by nature. The other
three are real signals and the scanner fires them on the right candle, but what they EARN is not
known - so they are for the trader to switch on, one at a time, so that a run can be attributed.

### The rest

| setting | default | what it does |
| --- | --- | --- |
| `Speed` | Standard | the four line lengths, see above |
| `BreakoutEntriesPerRun` | 3 | break entries per position |
| `RequirePriceOutsideCloud` | on | the close has to be clear of the cloud for a break |
| `MinimumCloudWidthPercentage` | 0 | a minimum width for the cloud; costs money at every setting tried |
| `RequireCloudWidening` | off | the cloud has to be wider than on the candle before |
| `MinimumSlowLineSlopePercentage` | 0 | how much the slow line has to be moving |
| `UseRsiFilter` / `UseVolumeFilter` | off | extra conditions on the signal candle |
| `ExitOnSecondLineCross` | off | the close marker closes the position |
| `ExitOnCloudFlip` | off | out when the cloud turns against the position |
| `RsiLevelLength` / `RsiLevelSupportCross` / `RsiLevelResistanceCross` | 14 / 35 / 65 | the levels |
| `PivotLeftCandles` / `PivotRightCandles` | 5 / 5 | the price pivot, which only the chart draws |

## Chart overlay

The overlay draws all eight markers with the same rules the strategy fires on - one series of
candles through both paths has to mark exactly the same ones, and a test says so. Two
implementations of one rule is a known way to drift apart.

Shapes and placement: a triangle for the entry, a cross for the trend crossing, a diamond for the
close marker, a circle for the break. Long markers sit under the candle, short ones above it.

## What has been measured on money

Settled over the emulator, each in both halves of a period:

- **The daily candle carries the result.** Four hours loses in the second half whatever is done to
  it, one hour worse. Eight and twelve hours lose in the first half as well.
- **A wide target with a small stop**: stop 5 with target 24 beats stop 4, which beats stop 3.
  Targets of 18 and below lose ground.
- **The position duration limit does nothing.** Seven, fourteen and twenty-eight days give the same
  result to the cent, because positions close well before that.
- **Moving the stop to break-even** turns most trades into winners and nearly all the profit into
  nothing.
- **Minimum cloud width costs money** and it gets worse the tighter it is set.
- **The MA200 entry condition** costs money here three times over.
- **The volume filter empties the strategy**: at three times the average it leaves four trades in a
  half, which is no measurement at all.
- **Buying in costs money**: one level costs a third of the result, two lose outright.
- **The pivot lengths barely matter** - three, seven and ten candles either side all land within
  noise of the default.

Not yet measured: which of the four signals earns anything. That is what the queue is for, one
signal per run so the answer can be attributed.
