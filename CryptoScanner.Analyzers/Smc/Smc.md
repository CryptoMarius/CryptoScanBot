# Smart Money Concepts — Order Blocks (SMC)

## Overview

The **SMC** strategy identifies supply and demand zones using the Smart Money Concepts (ICT) Order Block methodology. Zones are detected where a base of small-range candles precedes an expansion (large-range) candle, indicating institutional accumulation or distribution. Two signal variants exist:

- **smc** — fires on zone touch (wick enters the zone).
- **smc.rejection** — fires on confirmed bounce (candle closes beyond the proximal edge after a recent zone test).

This is a **production** strategy.

## How it works

### Zone detection (ZoneSmc)

Order Block zones are identified using a base + expansion pattern:

1. **Base candles**: up to `BaseMaxCandles` (default 6) consecutive candles with range ≤ `BaseMaxRangeFactor` × average range (default 0.8×). These represent consolidation.
2. **Expansion candle**: a candle with range ≥ `ExpansionMinRangeFactor` × average range (default 1.5×) and body ≥ `ExpansionBodyFraction` × range (default 50%). This represents the institutional move.
3. Zone boundaries are set to the base candles' high/low.
4. Zone strength: expansion ≥ `StrongExpansionFactor` (default 2.5×) = Strong; below = Weak.
5. Optional `RequireOppositeBaseColor`: requires the last base candle to be opposite colour to the expansion (classical ICT definition).

### Signal: SMC touch (smc)

Iterates over `IntervalList` (default: `["1h"]`):

- **Long (demand zone)**: candle wick intersects zone (Low ≤ zone.Top AND High ≥ zone.Bottom). Zone must not be closed, TouchCount must be ≤ MaxTouches. If `OnlyStrong`: zone must be Strong.
- **Short (supply zone)**: mirror logic.

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
| `MaxTouches` | 1 | Max touches before zone exhaustion |
| `RejectionLookback` | 3 | Candles to look back for zone test (rejection variant) |

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

Settings class: `CryptoScanner.Core/Settings/Strategy/SettingsSignalStrategySmc.cs`
Enum values: `CryptoSignalStrategy.OrderBlock = 1004`, `CryptoSignalStrategy.OrderBlockRejection = 1006`

## Registration

Registered as a **production** strategy in `AnalyzerRegistration.cs`. Strategy names in the UI: **smc**, **smc.rejection**.
