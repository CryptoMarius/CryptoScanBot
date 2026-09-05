# MACD Crossover after a band break (MacdCrossBand)

## Overview

**MacdCrossBand** is a variant of [MacdCross](../MacdCross/MacdCross.md). The entry and the exit are
the same rule - in when the MACD line crosses its signal line, out when the two cross back - with
one extra question asked last: *was the price at a band shortly before the cross?*

Three band strategies can be looked back at, each ticked separately:

| Ticked | What is looked for |
|--------|--------------------|
| `LookbackVbs` | A VBS band break: the volume-weighted VWAP band. |
| `LookbackAtrRb` | An AtrRb band break: an EMA basis with ATR bands, Keltner style. |
| `LookbackDbr` | A DBR band break: Donchian based outer bands, including the rule that only the first break of a run counts. |

A break on **any** of the ticked strategies is enough. All of them are walked anyway, so the signal
text names every band that was broken and how many candles ago - which is the point of the strategy:
it says where on the chart to look.

It is written as an **attention filter**, not as a trading rule. The bare crossover fires often; a
cross that comes right after price stretched to a band is the rarer situation that is worth opening
the chart for.

## What "a band break" means here

What is looked for is the **band break** the band strategy is built on, not its complete signal. The
RSI, stochastic and Bollinger-width filters those strategies carry are not replayed, and neither is
their higher-timeframe confirmation. Those settings describe the moment *they* would enter; the
question here is only whether the price has been at the band at all.

Per strategy, on one candle:

- **Vbs** - the Low under the lower band or the Close under it (a long), the High or the Close above
  the upper band (a short). The same test `VbsSignalLong` / `VbsSignalShort` use. With
  `VbsRequireCloseBeyondBand` the Close alone decides, so a wick through the band no longer counts.
- **AtrRb** - the Low under `EMA - ATR * OuterMult` and the lowest Low of the trailing
  `BreakLookback` window (a long); the mirror image for a short.
- **Dbr** - the Low under the Donchian lower band plus the stacking rule: only the first break
  candle of a run fires, unless `AllowStack` is on and this candle has a lower Low.

The break is **recomputed from the candles**, not looked up in the signal list. That way the answer
is the same in the scanner, in a rescan and in the emulator, and it does not depend on the band
strategy being enabled or on its signal having survived in the list.

The bands themselves are not configured here. Each of the three keeps using its own settings tab
(length, multiplier, lookback), so this strategy always sees exactly the bands the original strategy
and the chart overlay draw. Ticking the "Vbs Bands", "AtrRb Bands" or "Dbr Bands" overlay in the
chart therefore shows exactly what the lookback found.

## Which band, and when

The window **ends at the signal candle** and reaches `LookbackWithinCandles` candles back, that
candle included. The break therefore always lies at or before the cross, which is the order that
makes sense: price stretches to the band first, momentum turns afterwards.

By default the band on the side of the trade is asked for - a long wants the **lower** band broken,
a short the **upper** one. That is the situation the filter is looking for: price stretched away and
momentum turning back. `AcceptEitherBand` widens it to "this coin has been at its bands lately",
which is the reading that also catches a long after a blow-off top.

## Cost

The lookback runs **last**, after the cross, the zero-line filter, the distance, the ADX and the
volume have all said yes - so it is only paid for on the few candles that get that far. Within it
the three are asked cheapest first:

- **Vbs** reads a value that is already on the candle (the VBS indicator extension computed it), so
  it is a walk over the window and nothing else.
- **AtrRb** costs one EMA and one ATR over the recent candles, computed once for the whole window
  rather than once per candle.
- **Dbr** costs one Donchian pass over the recent candles, likewise once for the whole window.

For the VBS lookback the plugin declares the VBS indicator extension itself, so the bands are there
even when no VBS strategy is enabled. When VBS *is* enabled the extension is left to the VBS plugin,
because a second copy would compute the same VWMA pair twice into the same slot.

## Settings

Everything MacdCross has (`ConfirmationCandles`, `MinimumDistancePercentage`,
`RequireCrossBeyondZeroLine`, the ADX pair, the relative volume trio, `ExitOnCrossBack` and
`ExitConfirmationCandles`) is inherited unchanged and documented in
[MacdCross.md](../MacdCross/MacdCross.md). On top of that:

| Setting | Default | Meaning |
|---------|---------|---------|
| `LookbackWithinCandles` | 10 | How many candles back the band break is looked for, the signal candle included. |
| `LookbackVbs` | on | Look for a VBS band break. |
| `LookbackAtrRb` | off | Look for an AtrRb band break. |
| `LookbackDbr` | off | Look for a DBR band break. |
| `AcceptEitherBand` | off | Off asks for the band on the side of the trade; on accepts a break of either band. |
| `VbsRequireCloseBeyondBand` | off | For the VBS lookback only: on, the Close has to be beyond the band, so a wick through it does not count. |

With none of the three ticked nothing is looked up and the strategy is the plain crossover again.
That is deliberate: it is the baseline a run compares the lookback against.

## Reading a signal

The signal text is the MacdCross text with the lookback appended, for example:

```
macd crossed above the signal line, 0.031% apart, vbs lower band 3 candle(s) ago
```

and with more than one strategy ticked, every hit is named:

```
macd crossed above the signal line, 0.028% apart, vbs lower band 2 candle(s) ago, dbr lower band 6 candle(s) ago
```

A refusal says which half said no: `no band break in the last 10 candle(s)` is the lookback,
anything about the MACD, the ADX or the volume is the crossover underneath it. When the VBS bands
were never computed for a single candle in the window - the indicator is still warming up - the text
says `(vbs bands not available yet)`, because a silent no would read as a verdict about a price that
nothing measured.

## Measuring it in the emulator

- The strategy name in the queue is `macdcrossband`; the settings are addressed by the property
  names above.
- `macdcross` and `macdcrossband` next to each other in one run is the comparison that matters: the
  same crosses, one list filtered by the lookback. The difference in the number of signals says how
  rare the combination is, the difference in result says whether the rarer ones are the better ones.
- `LookbackWithinCandles` is the knob to sweep first: 3, 5, 10, 20. Short means the two events have
  to be almost on top of each other, long approaches the plain crossover again.
- The three band strategies are worth ticking one at a time before ticking them together, or the
  result cannot say which band did the work.

## What it does not do

- Replay the RSI, stochastic, Bollinger-width or higher-timeframe filters of the band strategies.
  Only the band break itself, plus the structural rule that belongs to it (the AtrRb lowest-low
  window, the DBR stacking rule).
- Look forward. The window ends at the signal candle, always.
- Change the entry price or the stop loss. The band strategies place their entry on the band; this
  one keeps the crossover entry and only uses the band as a reason to look.
