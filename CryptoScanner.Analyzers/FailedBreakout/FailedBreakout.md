# Failed Breakout (FailedBreakout)

## Overview

The **FailedBreakout** strategy trades the break that did not hold. Price sets a new high (or low)
over a lookback window and then closes back inside that window. Going up this is an *upthrust* or
bull trap, going down a *spring* or bear trap.

It reads nothing but candles — no indicators — and is built as a strategy of its own rather than as
a filter on purpose. Everything added as a filter cost money, while the candlestick shapes measured
as strategies in their own right did make money, at full trade volume. So this competes with the
other strategies, it does not filter them.

## Background

This is one of the oldest ideas in technical analysis. It appears under four different names, all
describing the same event:

| Name | Source | Rule |
|------|--------|------|
| Spring / upthrust | Richard Wyckoff, 1930s | A push through support or resistance that immediately reverses back into the trading range. Wyckoff's reading: the break is deliberate, to pick up the stops and breakout orders sitting beyond the level. |
| 2B rule / failure test | Victor Sperandeo (*Trader Vic*), Adam Grimes | "In an uptrend, if a higher high is made but fails to carry through, and prices then drop below the previous high, the trend is apt to reverse." The stop is defined by the pattern itself: just beyond the failed break. |
| Turtle Soup | Linda Raschke & Laurence Connors, *Street Smarts*, 1996 | Price breaks the 20-day low and returns above it within two sessions → long. The deliberate mirror image of the Turtle breakout system: it earns when the trend followers take their losses. |
| Liquidity sweep / stop hunt / fakeout | Modern order-flow vocabulary | Same pattern, newer wording. |

The default settings here (20 lookback candles, a 3 candle break window) are the Turtle Soup rule.

Published estimates put the share of breakouts that fail at 60–80%, and that share depends heavily
on the interval: around 70% on a 1 minute chart down to the low 40s on daily. Lower intervals show
more failed breakouts because spread, slippage and a single large order sweeping resting stops are
already enough to produce a break. These figures come from trading blogs rather than peer-reviewed
work, so treat the numbers as indicative and the direction as the useful part.

What the literature prescribes and this implementation does **not** do (yet):

- Stop-loss just beyond the extreme of the failed break, instead of a fixed percentage.
- Target the opposite side of the range, instead of a fixed profit percentage.
- Volume: a breakout on low volume fails more often. Volume is not looked at here.

## How it works

### Phase 1: Collect the candles

Everything from the candle being evaluated back through the level window is collected once, oldest
last — `BreakWithinCandles + LookbackCandles` candles in total. Walking the same candles twice is the
sort of thing that becomes the dominant cost when it runs for every symbol on every candle. If fewer
candles are available the signal is rejected.

### Phase 2: Determine the level

The level is the highest high (short) or the lowest low (long) over the candles **before** the break
window, so the break itself can never set the level it is supposed to have broken. Getting this off
by one candle would mean nothing ever fires.

### Phase 3: The break

At least one candle inside the break window must have pushed past the level. `MinimumBreakPercentage`
is measured as a percentage **of the level**, not as an absolute amount — an absolute threshold would
measure the price of the coin instead of the size of the move (the same reasoning as in
`CandlePatternHelper` and `Tools/PatternScan/README.md`).

### Phase 4: The return

The candle being evaluated must close back on the original side of the level. A close still beyond
the level means the break is holding and no signal fires. `ExtraText` reports the level and how far
price closed back inside it, as a percentage.

### Optional: the failed break has to happen at a zone

`RequireZone` adds a second requirement on top of the level the strategy builds itself: the breaking
candle must also touch an open zone of the same side, produced by one or more of the three zone
strategies (`dlz`, `fvg`, `smc`). Ticking nothing switches it off, which is the default and what
every run made before this setting existed did.

The two are not the same thing. The level is the highest high or lowest low of the lookback window —
what the candles did — while a zone is a level one of the zone strategies found and holds on to.
Asking for both is asking for the failed break to have happened **at** a zone, which is the failed
zone this setting was built to measure.

Checked before the candles are collected: most candles are nowhere near a zone, and the level window
costs `BreakWithinCandles + LookbackCandles` lookups. "Touching" is the same test the zone strategies
use (`ZoneTools.Touches`), so a candle that only pokes into the zone with its wick counts, and the
zone must already have existed at that candle — without that check a replay would read a zone that
was only detected later, which is look-ahead. `ZoneTolerancePercentage` widens the band on both sides.

The zones have to exist: a kind whose `IntervalList` under `Signal.ZonesDlz` / `ZonesFvg` / `ZonesSmc`
is empty produces no zones at all, and then every signal is rejected. Only DLZ falls back to 1h on
its own. The shared implementation lives in `ZoneRequirementHelper`, next to the candle-pattern
strategy that asks the same question.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Enough candles | `BreakWithinCandles + LookbackCandles` available |
| 2 | Level | Lowest low over the candles before the break window |
| 3 | Break | Some candle in the break window has a Low below `level − margin` |
| 4 | Return | The evaluated candle closes above the level |

### Short entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Enough candles | `BreakWithinCandles + LookbackCandles` available |
| 2 | Level | Highest high over the candles before the break window |
| 3 | Break | Some candle in the break window has a High above `level + margin` |
| 4 | Return | The evaluated candle closes below the level |

## Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `LookbackCandles` | 20 | How many candles the level is taken from. Longer means a level fewer people would argue with, and fewer signals. Must be at least 2. |
| `BreakWithinCandles` | 3 | How recently the break must have happened, counted back from the evaluated candle and including it. One means the break and the close back inside are the same candle: the classic single-candle upthrust. Must be at least 1. |
| `MinimumBreakPercentage` | 0 | How far beyond the level the break has to have gone, as a percentage of the level. Zero accepts a break by a single tick. |
| `RequireZone` | *(empty)* | Only fire when the breaking candle touches a zone of the same side: `dlz`, `fvg` and/or `smc`. Several at once is an OR. Empty switches the requirement off. |
| `ZoneTolerancePercentage` | 0 | Room around the zone, as a percentage of the zone's own price. Zero is strictly between `Bottom` and `Top`. |

Plus the settings shared by every strategy through `SettingsSignalStrategyBase`.

## Indicators used

None. The strategy reads raw candles only, walking back `BreakWithinCandles + LookbackCandles` bars —
the same order of work as helpers already in production (`HadStorsiInThelastXCandles` walks 25).

## Strategy type

- **Mean-reversion / reversal at a broken level**
- Counter-trend: it fires against the direction of the break

## File structure

```
CryptoScanner.Analyzers/FailedBreakout/
├── FailedBreakoutPlugin.cs                # Plugin registration
├── FailedBreakoutSettings.cs              # Lookback, break window, minimum break percentage, zone
├── FailedBreakout.md                      # This document
├── Config/                                # The Avalonia settings tab
│   ├── FailedBreakoutConfigView.cs
│   ├── StrategyFailedBreakoutTabView.axaml(.cs) + ViewModel
│   └── StrategyFailedBreakoutSettingsView.axaml(.cs) + ViewModel
└── Signal/
    └── FailedBreakoutBase.cs              # Shared logic + FailedBreakoutLong / FailedBreakoutShort
```

The Photino host has no per-plugin control; it builds its editor from the settings class by
reflection, so both hosts show the same fields.

Tests: `CryptoScanner.CoreTests/Signal/FailedBreakoutTests.cs` and
`CryptoScanner.CoreTests/Analyzer/FailedBreakout/FailedBreakoutConfigViewModelTests.cs`

## Registration

Registered unconditionally in `AnalyzerRegistration.cs` (not DEBUG-only). Strategy name in the UI:
**failedbreakout**.

## References

- [Springs and Upthrusts — Power Trading Group](https://www.powertrading.group/options-trading-blog/springs-and-upthrusts-false-breakouts-provide-powerful-signals-in-trading)
- [Wyckoff Spring & Shakeout — Rubén Villahermosa](https://tradingwyckoff.com/en/spring-shakeout/)
- [2B or Not 2B: A Classic Rule Revisited — *Trader Vic on Commodities* (Wiley)](https://onlinelibrary.wiley.com/doi/10.1002/9781119196792.ch4)
- [Trading Pattern: the Failure Test — Adam Grimes](https://www.adamhgrimes.com/trading-pattern-the-failure-test/)
- [Turtle Soup: fading a new 20 day high or low — Netpicks](https://www.netpicks.com/turtle-soup-fading-new-20-day-high-or-lows/)
- [Linda Bradford Raschke — TurtleTrader](https://www.turtletrader.com/trader-raschke/)
- [How to Identify and Avoid False Breakouts, a data-driven approach — ORB Setups](https://orbsetups.com/research/how-to-identify-and-avoid-false-breakouts-a-data-driven-approach/)
- [Breakout or Fakeout? The 3-Point Checklist for Confirmation — Bookmap](https://bookmap.com/blog/breakout-or-fakeout-the-3-point-checklist-for-confirmation)
