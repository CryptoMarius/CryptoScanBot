# Smart Money Concepts — Order Blocks (SMC)

## Overview

The **SMC** strategy identifies supply and demand zones using the Smart Money Concepts (ICT) Order Block methodology. Zones are detected where a base of small-range candles precedes an expansion (large-range) candle, indicating institutional accumulation or distribution. Two signal variants exist:

- **smc** — fires on zone touch (wick enters the zone).
- **smc.rejection** — fires on confirmed bounce (candle closes beyond the proximal edge after a recent zone test).

This is a **production** strategy.

The signals do not close zones themselves: when a zone closes is decided by the shared closing rules in `ZoneInvalidation` (see [Zone lifetime](#zone-lifetime)). For SMC a visit only counts once the wick reaches the **middle** of the zone (`TouchLevel = Midpoint`); for DLZ and FVG the edge is enough.

## How it works

### Zone detection (ZoneSmc)

Order Block zones are identified using a base + expansion pattern:

1. **Base candles**: up to `BaseMaxCandles` (default 6) consecutive candles with range ≤ `BaseMaxRangeFactor` × average range (default 0.8×). These represent consolidation.
2. **Expansion candle**: a candle with range ≥ `ExpansionMinRangeFactor` × average range (default 1.5×) and body ≥ `ExpansionBodyFraction` × range (default 50%). This represents the institutional move.
3. Zone boundaries are set to the base candles' high/low.
4. Zone strength: expansion ≥ `StrongExpansionFactor` (default 2.5×) = Strong; below = Weak.
5. Optional `RequireOppositeBaseColor`: requires the last base candle to be opposite colour to the expansion (classical ICT definition).

The base candles and the expansion candle's own wick do not count as a visit: counting starts after the expansion (`TouchCountingFrom`).

### Signal: SMC touch (smc)

Iterates over `IntervalList` (default: `["1h"]`):

- **Long (demand zone)**: candle wick intersects zone (Low ≤ zone.Top AND High ≥ zone.Bottom). Zone must not be closed. If `OnlyStrong`: zone must be Strong.
- **Short (supply zone)**: mirror logic.

The signal also skips zones whose `TouchCount` is above `MaxTouches`. With the defaults that cannot happen, because the closing rules already close the zone at `MaxTouches` visits. With `MaxTouches = 0` (never used up) this check means only zones without any visit still produce a signal.

Alarm rate-limited to once per zone per hour.

### Signal: SMC rejection (smc.rejection)

Confirmed bounce with lookback:

- **Long**: current candle closes **above** zone.Top (proximal edge). Within the last `RejectionLookback` candles (default 3), at least one candle must have wicked into the zone. Signal price is set to zone.Top for limit-order entry.
- **Short**: current candle closes **below** zone.Bottom. Lookback must show a candle that tested the zone.

## Signal conditions summary

### Long entry (smc — touch)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Open demand zone exists | Zone with Side = Long, not closed |
| 2 | Zone not exhausted | TouchCount ≤ MaxTouches |
| 3 | Wick intersects zone | Low ≤ zone.Top AND High ≥ zone.Bottom |
| 4 | Zone is Strong (optional) | When OnlyStrong is enabled |
| 5 | Not already alarmed this hour | Rate-limited to once per zone per hour |

### Long entry (smc.rejection — bounce)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | Open demand zone exists | Zone with Side = Long, not closed |
| 2 | Close > zone.Top | Price bounced above proximal edge |
| 3 | Recent zone test | A candle within RejectionLookback wicked into the zone |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `IntervalList` | ["1h"] | Timeframe intervals to monitor |
| `AverageWindow` | 20 | Trailing window for average candle range |
| `BaseMaxRangeFactor` | 0.8 | Base candle max range as fraction of average |
| `ExpansionMinRangeFactor` | 1.5 | Expansion candle min range multiple |
| `ExpansionBodyFraction` | 0.5 | Expansion body must be this fraction of range |
| `StrongExpansionFactor` | 2.5 | Expansion factor for Strong classification |
| `BaseMaxCandles` | 6 | Max consecutive small candles in one base |
| `MaxBlocksPerInterval` | 50 | Max zones tracked per interval |
| `RequireOppositeBaseColor` | false | Classical ICT: last base candle opposite colour |
| `OnlyStrong` | false | Only fire on Strong zones |
| `MaxTouches` | 2 | Visits a zone survives; it closes after that. 0 = never used up, only a break closes it (see [Zone lifetime](#zone-lifetime)) |
| `TouchLevel` | Midpoint | How far price must come in before a visit counts: Edge (wick reaches the near edge) or Midpoint (wick reaches the middle) |
| `CloseZonesPastMidpoint` | false | Close the zone as soon as price has ever reached its middle. With `TouchLevel = Midpoint` this equals `MaxTouches = 1` |
| `RejectionLookback` | 3 | Candles to look back for zone test (rejection variant) |

The `MaxTouches` default was 1 while the signal counted by itself (0 and 1 touches allowed, zone stayed open). Under the shared closing rules 2 means the same thing: the zone closes on its second visit.

## Zone lifetime

Zones of all three kinds (DLZ, FVG, SMC order blocks) are closed by one implementation: `CryptoScanner.Core/Zones/ZoneInvalidation.cs`. The signal classes only read the open zones and throttle their alarm (`AlarmDate`); they never set `CloseTime`.

Every closed candle of the zone interval is applied to every open zone, in this order:

1. **Broken** — the candle body closes through the far side (Long: Close < Bottom, Short: Close > Top). The zone closes. A wick through the zone does not count, only the close.
2. **Entered** — the candle reaches the touch level (`TouchLevel`) while price was outside. That is one visit: `TouchCount` + 1. Reaching the middle also sets `ReachedMidpoint`.
3. **Left** — price was inside and this candle no longer reaches the near edge. The zone is open for a next visit.
4. **Used up** — `TouchCount` reaches `MaxTouches`. The zone closes. `MaxTouches = 0` disables this rule.
5. **Past the midpoint** — optional (`CloseZonesPastMidpoint`, default off): the zone closes once price has ever reached its middle, whatever the visit count. With `TouchLevel = Midpoint` this equals `MaxTouches = 1`.

`MaxTouches` counts **visits**, not candles: a visit is one continuous stay inside the zone, so three consecutive candles inside count as one touch. Whether price is inside is measured against the edge, even when the touch level is the midpoint. For order blocks counting starts after the expansion candle (`TouchCountingFrom`), so the base candles and the impulse's own wick never count.

The rules run realtime after every closed zone-interval candle, again on a zone recalculation, and over the candle history at startup (`ZoneBroken.CheckAndMarkBrokenZones`). `TouchCount` and `ReachedMidpoint` are persisted; the visit bookkeeping (`LastInsideCandle`) is not, so after a restart the first candle inside a zone counts as a new visit (over-counts by at most one per zone).

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| SMC zones (pre-calculated) | Base + expansion pattern detection |
| Average candle range | Base/expansion classification |

## Strategy type

- **Zone-based supply/demand (ICT Order Block)**
- Production strategy

## File structure

```
CryptoScanner.Analyzers/Smc/
├── SmcPlugin.cs                                  # Plugin registration (smc + smc.rejection)
├── Smc.md                                        # This document
├── Config/
│   ├── StrategySmcTabView.axaml                  # Settings tab UI
│   └── StrategySmcTabViewModel.cs                # Settings viewmodel
└── Signal/
    ├── SignalOrderBlockLong.cs                    # Long touch: wick into demand zone
    ├── SignalOrderBlockShort.cs                   # Short touch: wick into supply zone
    ├── SignalOrderBlockRejectionLong.cs           # Long rejection: confirmed bounce above zone
    └── SignalOrderBlockRejectionShort.cs          # Short rejection: confirmed bounce below zone
```

Zone closing rules: `CryptoScanner.Core/Zones/ZoneInvalidation.cs`
Settings class: `CryptoScanner.Core/Settings/Strategy/SettingsSignalStrategySmc.cs`
Enum values: `CryptoSignalStrategy.OrderBlock = 1004`, `CryptoSignalStrategy.OrderBlockRejection = 1006`

## Registration

Registered as a **production** strategy in `AnalyzerRegistration.cs`. Strategy names in the UI: **smc**, **smc.rejection**.
