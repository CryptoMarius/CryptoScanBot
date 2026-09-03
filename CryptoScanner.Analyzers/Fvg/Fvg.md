# Fair Value Gap (FVG)

## Overview

The **FVG** strategy detects price interaction with Fair Value Gaps — unfilled price gaps identified by the ICT (Inner Circle Trader) methodology. Zones are pre-calculated by `ZoneFvg` and the strategy fires alarm signals when price wicks into an open FVG zone. This is a **production** strategy.

The signal does not close zones itself: when a zone closes is decided by the shared closing rules in `ZoneInvalidation` (see [Zone lifetime](#zone-lifetime)).

## How it works

### Zone source

FVG zones are identified by `ZoneFvg` using the classic three-candle gap pattern:
- **Bullish FVG**: candle[2].High < candle[0].Low — a gap below current price (demand zone).
- **Bearish FVG**: candle[2].Low > candle[0].High — a gap above current price (supply zone).

Each zone has Top/Bottom boundaries, a Side (Long/Short), a TouchCount (visits so far), and is tracked in the open/closed zone lists.

### Signal: FVG touch

The strategy iterates over the configured `IntervalList` (default: `["1h", "4h", "1d"]`):

- **Long**: for each open bullish FVG zone, if the candle wick intersects the zone (Low ≤ zone.Top AND High ≥ zone.Bottom), fire an alarm.
- **Short**: for each open bearish FVG zone, if the candle wick intersects the zone (High ≥ zone.Bottom AND Low ≤ zone.Top), fire an alarm.

This is an **alarm-only** strategy — zone closure is handled by `ZoneFvg` via `ZoneInvalidation` (see [Zone lifetime](#zone-lifetime)), not by the signal itself. At most one alarm per zone per hour.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Open bullish FVG zone exists | Zone with Side = Long in configured intervals |
| 2 | Candle wick enters zone | Low ≤ zone.Top AND High ≥ zone.Bottom |
| 3 | Not already alarmed this hour | Rate-limited to once per zone per hour |

### Short entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Open bearish FVG zone exists | Zone with Side = Short in configured intervals |
| 2 | Candle wick enters zone | High ≥ zone.Bottom AND Low ≤ zone.Top |
| 3 | Not already alarmed this hour | Rate-limited to once per zone per hour |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `IntervalList` | ["1h", "4h", "1d"] | Timeframe intervals to monitor |
| `MinimumPercentage` | 0.25 | Minimum FVG zone width percentage |
| `NearZonePercentage` | 0.25 | Proximity percentage for combined strategy checks |
| `MaxTouches` | 2 | Visits a zone survives; it closes after that. 0 = never used up, only a break closes it (see [Zone lifetime](#zone-lifetime)) |
| `TouchLevel` | Edge | How far price must come in before a visit counts: Edge (wick reaches the near edge) or Midpoint (wick reaches the middle) |
| `CloseZonesPastMidpoint` | false | Close the zone as soon as price has ever reached its middle, whatever the visit count (was `DisqualifyOnMitigation`) |
| `RejectionLookback` | 2 | Candles to look back for zone rejection |

## Zone lifetime

Zones of all three kinds (DLZ, FVG, SMC order blocks) are closed by one implementation: `CryptoScanner.Core/Zones/ZoneInvalidation.cs`. The signal classes only read the open zones and throttle their alarm (`AlarmDate`); they never set `CloseTime`.

Every closed candle of the zone interval is applied to every open zone, in this order:

1. **Broken** — the candle body closes through the far side (Long: Close < Bottom, Short: Close > Top). The zone closes. A wick through the zone does not count, only the close.
2. **Entered** — the candle reaches the touch level (`TouchLevel`) while price was outside. That is one visit: `TouchCount` + 1. Reaching the middle also sets `ReachedMidpoint`.
3. **Left** — price was inside and this candle no longer reaches the near edge. The zone is open for a next visit.
4. **Used up** — `TouchCount` reaches `MaxTouches`. The zone closes. `MaxTouches = 0` disables this rule.
5. **Past the midpoint** — optional (`CloseZonesPastMidpoint`, default off): the zone closes once price has ever reached its middle, whatever the visit count.

`MaxTouches` counts **visits**, not candles: a visit is one continuous stay inside the zone, so three consecutive candles inside count as one touch. Whether price is inside is measured against the edge, even when the touch level is the midpoint.

The rules run realtime after every closed zone-interval candle, again on a zone recalculation, and over the candle history at startup (`ZoneBroken.CheckAndMarkBrokenZones`). `TouchCount` and `ReachedMidpoint` are persisted; the visit bookkeeping (`LastInsideCandle`) is not, so after a restart the first candle inside a zone counts as a new visit (over-counts by at most one per zone).

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| FVG zones (pre-calculated) | Three-candle gap pattern detection |

## Strategy type

- **Zone-based (ICT Fair Value Gap)**
- Production strategy

## File structure

```
CryptoScanner.Analyzers/Fvg/
├── FvgPlugin.cs                          # Plugin registration
├── Fvg.md                                # This document
├── Config/
│   ├── StrategyFvgTabView.axaml          # Settings tab UI
│   └── StrategyFvgTabViewModel.cs        # Settings viewmodel
└── Signal/
    ├── SignalFairValueGapLong.cs          # Long: alarm on bullish FVG zone touch
    └── SignalFairValueGapShort.cs         # Short: alarm on bearish FVG zone touch
```

Zone closing rules: `CryptoScanner.Core/Zones/ZoneInvalidation.cs`
Settings class: `CryptoScanner.Core/Settings/Strategy/SettingsSignalStrategyFvg.cs`
Enum value: `CryptoSignalStrategy.FairValueGap = 1003`

## Registration

Registered as a **production** strategy in `AnalyzerRegistration.cs`. Strategy name in the UI: **fvg**.
