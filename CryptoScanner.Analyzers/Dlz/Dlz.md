# Dominant Liquidity Zones (DLZ)

## Overview

The **DLZ** strategy monitors pre-calculated support/resistance zones derived from ZigZag analysis. When price touches an open zone, the strategy fires an alarm. The signal itself does not close zones: when a zone closes is decided by the shared closing rules in `ZoneInvalidation` (see [Zone lifetime](#zone-lifetime)). A secondary "near" variant fires an early warning when price approaches a zone. This is a **production** strategy and one of the core zone-based strategies in the scanner.

Two signal variants:
- **dlz** — fires when price touches a zone (at most once per zone per hour; the zone stays open until the closing rules close it).
- **dlz.near** — fires an early warning when price approaches within `WarnPercentage` of a zone.

## How it works

### Zone source

Zones are pre-calculated by `ZoneDlz` using ZigZag pivot analysis over a configurable candle lookback. Each zone has:
- `Top` / `Bottom` — price boundaries
- `Side` — Long (demand/support) or Short (supply/resistance)
- `Strength` — Strong or Weak (based on zone characteristics)
- `TouchCount` — visits so far; the zone closes when it reaches `MaxTouches` (see [Zone lifetime](#zone-lifetime))

Zones can be zoomed from higher timeframes to lower ones for additional precision. Unzoomed zones apply at their original interval; zoomed zones inherit the parent zone's boundaries scaled to the lower timeframe.

### Signal: DLZ touch (dlz)

Iterates over the configured `IntervalList` (default: `["1h"]`):

- **Long**: for each open demand zone (sorted by Top descending), if candle Low ≤ zone.Top → signal fires. The zone stays open; whether it closes afterwards (body close below zone.Bottom, or `MaxTouches` visits reached) is decided by the closing rules on every closed candle.
- **Short**: for each open supply zone, if candle High ≥ zone.Bottom → signal fires. Mirror logic for closing.

The alarm is throttled to once per zone per hour (`AlarmDate`): a zone survives its first touch, so without this every candle of the same test would report again.

Weak zones can be skipped when `ZoneStartApply = true`.

### Signal: DLZ near (dlz.near)

Alarm-only — does not close zones:

- **Long**: `alarmPrice = zone.Top × (100 + WarnPercentage) / 100`. Fires if candle Low ≤ alarmPrice.
- **Short**: `alarmPrice = zone.Bottom × (100 − WarnPercentage) / 100`. Fires if candle High ≥ alarmPrice.

At most one alarm per zone per hour.

## Signal conditions summary

### Long entry (dlz)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Open demand zone exists | Zone with Side = Long in the configured intervals |
| 2 | Candle Low ≤ zone.Top | Price touches the zone from above |
| 3 | Zone not Weak (optional) | When ZoneStartApply is enabled |
| 4 | Not already alarmed this hour | Rate-limited to once per zone per hour |

### Short entry (dlz)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Open supply zone exists | Zone with Side = Short in the configured intervals |
| 2 | Candle High ≥ zone.Bottom | Price touches the zone from below |
| 3 | Zone not Weak (optional) | When ZoneStartApply is enabled |
| 4 | Not already alarmed this hour | Rate-limited to once per zone per hour |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `IntervalList` | ["1h"] | Timeframe intervals to monitor for zones |
| `CandleCount` | 500 | Lookback period for zone calculation |
| `CandleCountZoom` | 125 | Lookback for zoomed zones |
| `ZonesApplyUnzoomed` | false | Apply unzoomed (original interval) zones |
| `MinimumUnZoomedPercentage` | 0.0 | Min zone width % for unzoomed zones |
| `MaximumUnZoomedPercentage` | 0.0 | Max zone width % for unzoomed zones |
| `ZoomLowerTimeFrames` | true | Zoom zones to lower timeframes |
| `MinimumZoomedPercentage` | 0.2 | Min zone width % for zoomed zones |
| `MaximumZoomedPercentage` | 0.7 | Max zone width % for zoomed zones |
| `WarnPercentage` | 0.25 | Distance % for "near" warning signals |
| `NearZonePercentage` | 0.25 | Proximity % for combined strategy checks |
| `MaxTouches` | 2 | Visits a zone survives; it closes after that. 0 = never used up, only a break closes it (see [Zone lifetime](#zone-lifetime)) |
| `TouchLevel` | Edge | How far price must come in before a visit counts: Edge (wick reaches the near edge) or Midpoint (wick reaches the middle) |
| `CloseZonesPastMidpoint` | false | Close the zone as soon as price has ever reached its middle, whatever the visit count (was `DisqualifyOnMitigation`) |
| `RejectionLookback` | 1 | Candles to look back for zone rejection |
| `ZoneStartApply` | false | Skip Weak zones |
| `ZoneStartCandleCount` | 5 | Zone start lookback for strength |
| `ZoneStartPercentage` | 2.5 | Zone start percentage threshold |

## Zone lifetime

Zones of all three kinds (DLZ, FVG, SMC order blocks) are closed by one implementation: `CryptoScanner.Core/Zones/ZoneInvalidation.cs`. The signal classes only read the open zones and throttle their alarm (`AlarmDate`); they never set `CloseTime`.

Every closed candle of the zone interval is applied to every open zone, in this order:

1. **Broken** — the candle body closes through the far side (Long: Close < Bottom, Short: Close > Top). The zone closes. A wick through the zone does not count, only the close.
2. **Entered** — the candle reaches the touch level (`TouchLevel`) while price was outside. That is one visit: `TouchCount` + 1. Reaching the middle also sets `ReachedMidpoint`.
3. **Left** — price was inside and this candle no longer reaches the near edge. The zone is open for a next visit.
4. **Used up** — `TouchCount` reaches `MaxTouches`. The zone closes. `MaxTouches = 0` disables this rule.
5. **Past the midpoint** — optional (`CloseZonesPastMidpoint`, default off): the zone closes once price has ever reached its middle, whatever the visit count.

`MaxTouches` counts **visits**, not candles: a visit is one continuous stay inside the zone, so three consecutive candles inside count as one touch. Whether price is inside is measured against the edge, even when the touch level is the midpoint.

The rules run realtime after every closed zone-interval candle (`ZoneDlz.InvalidateRealtime`), again on a zone recalculation, and over the candle history at startup (`ZoneBroken.CheckAndMarkBrokenZones`). `TouchCount` and `ReachedMidpoint` are persisted; the visit bookkeeping (`LastInsideCandle`) is not, so after a restart the first candle inside a zone counts as a new visit (over-counts by at most one per zone).

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| ZigZag (Primary, configurable) | Zone boundary detection via pivot analysis |

## Strategy type

- **Zone-based supply/demand**
- Production strategy

## File structure

```
CryptoScanner.Analyzers/Dlz/
├── DlzPlugin.cs                              # Plugin registration (dlz + dlz.near)
├── Dlz.md                                    # This document
├── Config/
│   ├── StrategyDlzTabView.axaml              # Settings tab UI (complex layout with zone filters)
│   └── StrategyDlzTabViewModel.cs            # Settings viewmodel
└── Signal/
    ├── SignalDominantLevelLong.cs             # Long: alarm when candle touches demand zone
    ├── SignalDominantLevelShort.cs            # Short: alarm when candle touches supply zone
    ├── SignalDominantLevelNearLong.cs         # Long near: alarm when approaching demand zone
    └── SignalDominantLevelNearShort.cs        # Short near: alarm when approaching supply zone
```

Zone closing rules: `CryptoScanner.Core/Zones/ZoneInvalidation.cs`
Settings class: `CryptoScanner.Core/Settings/Strategy/SettingsSignalStrategyDlz.cs`
Enum values: `CryptoSignalStrategy.DominantLevel = 1000`, `CryptoSignalStrategy.DominantLevelNear = 1001`

## Registration

Registered as a **production** strategy in `AnalyzerRegistration.cs`. Strategy names in the UI: **dlz**, **dlz.near**.
