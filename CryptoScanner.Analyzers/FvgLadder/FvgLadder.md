# FVG Ladder (FVG & DCA, no stop-loss)

## Overview

Long-only strategy for BTC spot, and for the other coins of its list, built from the rules in "BTC FVG DCA - Regels ter afstemming". Buy limits on the tops of bullish fair value gaps while the day close is above its EMA200, DCA's on doubling sizes, a take profit just above the break-even price and no stop loss. A trade can stay open for a long time; new trades are started next to it.

Registered as a production strategy. Paper trading and the emulator only.

## Trader properties

The registration (`FvgLadderPlugin`) sets three properties no other strategy uses (see `StrategyRegistration`):

| Property | Effect |
|---|---|
| `ParallelPositions` | Several positions on one coin. Each is filed under `symbol#id` in `PositionList` (`PositionTools.PositionListKey`); every other strategy keeps one position per coin. Each position takes a long slot. |
| `StandingEntry` | The entry is a limit order on the signal price (offset 0, whatever `EntryOrderType` says), with no `EntryRemoveTime` timeout. The strategy withdraws it itself (`WithdrawEntry`). |
| `WithoutStopLoss` | `Trading.StopLossPercentage` is not applied. |

## Rules

| # | Rule | Where |
|---|---|---|
| 0 | Only the coins of `Coins` (default `BTC`), matched on the base asset, so `BTC` covers BTCUSDT, BTC-EUR and BTCUSDT.PERP. Empty means every coin the strategy is switched on for. Each coin is judged on its own chart. | `FvgLadderSettings.TradesCoin` |
| 1.1 | A new position only while the last closed day candle closed above the EMA200 of the day closes. Not for DCA's. (1.2 "EMA200 rising" follows from it and is left out.) A flip of the trend is written to the log once. | `FvgLadderTrend` |
| 2.x | The gaps are the scanner's fvg zones (`ZoneFvg`, 1h and 4h, three green candles) of at least `MinimumGapPercentage` (0.4), measured as (top - bottom) / bottom. A 1h gap overlapping a 4h gap gives way to it. When a gap closes is the scanner's rule (`ZoneInvalidation`, `MaxTouches`). | `FvgLadderRules.UsableTops` |
| 3.x | ONE buy limit at a time, on the highest usable gap top under the price among the `OrderZones` (4) nearest, placed once the price is within `PlaceOrderDistancePercentage` (1.0) of it. It is withdrawn when the trend goes, when another gap becomes the target, when the coin leaves the list, or when the price is more than `WithdrawOrderDistancePercentage` (2.0) above it. A withdrawn order is a position with status Timeout that bought nothing. | `FvgLadderLong.IsSignal`, `WithdrawEntry` |
| 4.x | `MaxDca` (2) DCA's, each at least `StepPercentage` (1.5) under the previous buy, on the highest gap top between that and `DcaFvgMaxPercentage` (3) under it if there is one. Size x `DcaFactor` (2): 100 / 200 / 400. The ladder is fixed when the entry is placed. | `FvgLadderRules.DcaLevels` |
| 5.1 | Take profit `TakeProfitPercentage` (1) above the break-even of the whole position including fees (`TpGridBreakEvenPrice`, moves with each DCA). Trailing: `Trading.TakeProfitTrailPercentage`. | trader |
| 6.x | A new position next to the open ones only once the lowest one (lowest last buy) has bought all of its DCA's, and at least `StepPercentage` under that last buy. | `FvgLadderRules.NewTradeAllowed` |

### Grid preset

Every 1h gap (no 4h) is a position of its own: no DCA (`DcaLadder.None`), take profit `TakeProfitPercentage` above the entry, at least `StepPercentage` away from the first buy of every open position.

## Points to know

- The paper trader fills a buy limit when the price goes THROUGH it (low < price), not on a touch. The same holds for the take profit.
- The trader reserves money for the entry and all of its DCA's before it opens a position.
- The EMA200 starts on the day candles in memory (about 270 in the emulator) and is carried forward from there, so it can differ a little in the first months of a run.
- `GlobalBuyCooldownTime`, `SignalCooldownAfterTradeTime`, the barometer and the other entry filters of the trader still apply; set them to 0 / off in the run to measure the bare rules.
- The scanner has to calculate fvg zones on 1h and 4h (`Signal.ZonesFvg.IntervalList`), with a minimum at or below `MinimumGapPercentage`.

## File structure

```
CryptoScanner.Analyzers/FvgLadder/
├── FvgLadderPlugin.cs        # Registration (parallel positions, standing entry, no stop loss)
├── FvgLadderSettings.cs      # Settings, presets and the coin list
├── FvgLadderRules.cs         # The rules as plain calculations (unit tested)
├── FvgLadder.md              # This document
├── Config/                   # Avalonia settings tab (Photino reads the settings class)
└── Signal/
    ├── FvgLadderLong.cs      # Signal and withdrawal of the standing entry
    └── FvgLadderTrend.cs     # Day close above EMA200
```

Tests: `CryptoScanner.CoreTests/Analyzer/FvgLadder/FvgLadderRulesTests.cs`, `CryptoScanner.CoreTests/Trader/ParallelPositionsTests.cs`.
