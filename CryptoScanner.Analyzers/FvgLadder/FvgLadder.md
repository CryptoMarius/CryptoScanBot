# FVG Ladder (FVG & DCA, no stop-loss)

## Overview

Long-only strategy for BTC spot, and for the other coins of its list, built from the rules in "BTC FVG DCA - Regels ter afstemming". Buy limits on the tops of bullish fair value gaps while the trend is up, optional DCA's on growing sizes, a take profit just above the break-even price and no stop loss. A trade can stay open for a long time; new trades are started under it.

Registered as a production strategy. Paper trading and the emulator only.

## Trader properties

The registration (`FvgLadderPlugin`) sets three properties no other strategy uses (see `StrategyRegistration`):

| Property | Effect |
|---|---|
| `ParallelPositions` | Several positions on one coin (up to the Trader setting `MaxPositionsPerCoin`). Every position is filed under `symbol#id` in `PositionList` (`PositionTools.PositionListKey`). The ladder rules below are this strategy's own; the other strategies use the same maximum without them. Each position takes a long slot. |
| `StandingEntry` | The entry is a limit order on the signal price (offset 0, whatever `EntryOrderType` says), with no `EntryRemoveTime` timeout. The strategy withdraws it itself (`WithdrawEntry`). |
| `WithoutStopLoss` | `Trading.StopLossPercentage` is not applied. |

## Flow (every minute, per coin)

1. The coin has to be in `Coins` (default `BTC`, matched on the base asset; empty = every coin).
2. A waiting buy limit stays until it fills, unless the coin left the list, the trend went, or the target changed (the gap closed, a higher gap became the target, or no new position is allowed any more). The distance of the price does not withdraw it, so a wick into the gap is still caught. A withdrawn order is a position with status Timeout that bought nothing.
3. No waiting order: is the trend up? (Only for a new position, never for a DCA.)
4. May a position be added? Not when `MaxPositionsPerCoin` (the Trader setting, default 1) are open. With none open: yes. Otherwise only under the lowest open position, at least `DcaStepPercentage` under its last buy (never above it or in between), and with DCA only once that position bought all of its DCA's.
5. The target is the highest gap top under the price among the `GapsInView` (4) nearest usable gaps (1h and 4h fvg zones of the scanner, at least `MinimumGapPercentage` wide, a 4h gap wins over an overlapping 1h gap), under the limit of step 4.
6. Once the price is within `PlaceOrderWithinPercentage` (5) above that top, the buy limit is placed on it, with the DCA ladder fixed.
7. Filled: the trader places all DCA orders and the take profit at once. Each DCA fill lowers the break-even and moves the take profit with it. The take profit closing the position frees room for a new entry.

## Rules

| # | Rule | Where |
|---|---|---|
| 1 | Trend: `UseKalmanFilter` off (default): the last closed day candle closed above the EMA200 of the day closes. On: the Kalman filter on the strategy's own interval (the first of `IntervalList`, default 1h) has a slope above zero. A flip of the trend is written to the log once. | `FvgLadderTrend` |
| 2 | Gaps: the scanner's fvg zones on 1h and 4h (`Signal.ZonesFvg`: shape, colours, `MaxTouches`; when a gap closes is `ZoneInvalidation`), at least `MinimumGapPercentage` (0.4) wide, measured as (top - bottom) / bottom. A 1h gap overlapping a 4h gap gives way to it. | `FvgLadderRules.UsableTops` |
| 4 | DCA (`UseDca`, default on): `MaxDca` (2) DCA's, each at least `DcaStepPercentage` (1.5) under the previous buy, on the highest gap top between that and `DcaFvgMaxPercentage` (3) under it if there is one. Size x `DcaFactor` (2): 100 / 200 / 400. The ladder is fixed when the entry is placed. | `FvgLadderRules.DcaLevels` |
| 5 | Take profit: always `TakeProfitPercentage` (1) above the break-even of the whole position, fees included (`TpGridBreakEvenPrice`, moves with each DCA). Trailing: `Trading.TakeProfitTrailPercentage`. | trader |
| 6 | New position: see step 4 of the flow. | `FvgLadderRules.NewTradeAllowed` |

### Kalman filter

The common constant-velocity model on the log of the close: it estimates the level and the slope (change per candle). The trend is up while the slope is above zero. The noise values are fixed in `FvgLadderRules` (measurement 1%, level 0.1%, slope 0.01% per candle): it follows a turn within a few candles and ignores a single spike.

## Points to know

- The paper trader fills a buy limit when the price goes THROUGH it (low < price), not on a touch. The same holds for the take profit.
- The trader reserves money for the entry and all of its DCA's before it opens a position.
- The trend filters start on the candles in memory (about 270 per interval in the emulator) and are carried forward from there, so they can differ a little in the first part of a run.
- `GlobalBuyCooldownTime`, `SignalCooldownAfterTradeTime`, the barometer and the other entry filters of the trader still apply; set them to 0 / off in the run to measure the bare rules.
- The scanner has to calculate fvg zones on 1h and 4h (`Signal.ZonesFvg.IntervalList`), with a minimum at or below `MinimumGapPercentage`.

## File structure

```
CryptoScanner.Analyzers/FvgLadder/
├── FvgLadderPlugin.cs        # Registration (parallel positions, standing entry, no stop loss)
├── FvgLadderSettings.cs      # Settings and the coin list
├── FvgLadderRules.cs         # The rules as plain calculations, the EMA and the Kalman filter (unit tested)
├── FvgLadder.md              # This document
├── Config/                   # Avalonia settings tab (Photino reads the settings class)
└── Signal/
    ├── FvgLadderLong.cs      # Signal and withdrawal of the standing entry
    └── FvgLadderTrend.cs     # Trend: day EMA200 or Kalman on the own interval
```

Tests: `CryptoScanner.CoreTests/Analyzer/FvgLadder/FvgLadderRulesTests.cs`, `CryptoScanner.CoreTests/Trader/ParallelPositionsTests.cs`.
