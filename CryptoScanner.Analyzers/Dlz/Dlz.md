# Dominant Liquidity Zones (DLZ)

## Overview

The **DLZ** strategy monitors pre-calculated support/resistance zones derived from ZigZag analysis. When price touches an active zone, the strategy fires a signal and closes the zone. A secondary "near" variant fires an early warning when price approaches a zone. This is a **production** strategy and one of the core zone-based strategies in the scanner.

Two signal variants:
- **dlz** — fires when price touches a zone (zone is then closed).
- **dlz.near** — fires an early warning when price approaches within `WarnPercentage` of a zone.

## How it works

### Zone source

Zones are pre-calculated by `ZoneDlz` using ZigZag pivot analysis over a configurable candle lookback. Each zone has:
- `Top` / `Bottom` — price boundaries
- `Side` — Long (demand/support) or Short (supply/resistance)
- `Strength` — Strong or Weak (based on zone characteristics)
- `TouchCount` — number of wick touches before exhaustion

Zones can be zoomed from higher timeframes to lower ones for additional precision. Unzoomed zones apply at their original interval; zoomed zones inherit the parent zone's boundaries scaled to the lower timeframe.

### Signal: DLZ touch (dlz)

Iterates over the configured `IntervalList` (default: `["1h"]`):

- **Long**: for each open demand zone (sorted by Top descending), if candle Low ≤ zone.Top → signal fires, zone is closed. If price breaks below zone.Bottom → zone is invalidated.
- **Short**: for each open supply zone, if candle High ≥ zone.Bottom → signal fires, zone is closed.

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

### Short entry (dlz)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Open supply zone exists | Zone with Side = Short in the configured intervals |
| 2 | Candle High ≥ zone.Bottom | Price touches the zone from below |
| 3 | Zone not Weak (optional) | When ZoneStartApply is enabled |

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
| `MaxTouches` | 2 | Max wick touches before zone exhaustion |
| `RejectionLookback` | 1 | Candles to look back for zone rejection |
| `DisqualifyOnMitigation` | false | Disqualify zone on 50% midpoint penetration (ICT consequent encroachment) |
| `ZoneStartApply` | false | Skip Weak zones |
| `ZoneStartCandleCount` | 5 | Zone start lookback for strength |
| `ZoneStartPercentage` | 2.5 | Zone start percentage threshold |

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
    ├── SignalDominantLevelLong.cs             # Long: candle touches demand zone → close zone
    ├── SignalDominantLevelShort.cs            # Short: candle touches supply zone → close zone
    ├── SignalDominantLevelNearLong.cs         # Long near: alarm when approaching demand zone
    └── SignalDominantLevelNearShort.cs        # Short near: alarm when approaching supply zone
```

Settings class: `CryptoScanner.Core/Settings/Strategy/SettingsSignalStrategyDlz.cs`
Enum values: `CryptoSignalStrategy.DominantLevel = 1000`, `CryptoSignalStrategy.DominantLevelNear = 1001`

## Registration

Registered as a **production** strategy in `AnalyzerRegistration.cs`. Strategy names in the UI: **dlz**, **dlz.near**.
