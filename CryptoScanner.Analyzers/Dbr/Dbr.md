# Donchian Breakout Reversion (DBR)

## Overview

The **DBR** strategy is a mean-reversion band strategy based on Donchian channels. It constructs outer bands from the highest high and lowest low over a configurable period and fires when price breaks beyond these bands. This is a **production** strategy — described in the source as "a band strategy which does rather well."

## How it works

### Phase 1: Band calculation

The Donchian channel is computed over `BandLength` bars (default 20):
- `middle = (highestHigh + lowestLow) / 2`
- `halfRange = (highestHigh − lowestLow) / 2`
- `band = halfRange × (OuterMult / 2.5)`
- Upper band = `middle + band`
- Lower band = `middle − band`

The default `OuterMult` of 3.2 places the outer bands roughly 1.28× the half-range from the middle.

### Phase 2: Optional RSI / Stochastic filter

- When `UseRsiFilter = true` (default): RSI must be oversold for longs, overbought for shorts.
- When `RequireStochOsOb = true`: Stochastic must also be at an extreme.

### Phase 3: Band break

- **Long**: candle Low must break below the lower band.
- **Short**: candle High must break above the upper band.

### Stacking behaviour

When `AllowStack = true` (default), consecutive signals can fire if price stretches further:
- First candle to break the band fires immediately.
- Subsequent candles only fire if they make a new extreme (lower Low for long, higher High for short).

### Entry price

Wick-only touches enter at the band price; body breaks enter at the close. Stop-loss width is set to the band width percentage.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | RSI oversold (when UseRsiFilter) | RSI confirms oversold |
| 2 | Stochastic oversold (optional) | When RequireStochOsOb is enabled |
| 3 | Low < lower Donchian band | Price breaks below the calculated lower band |
| 4 | Stacking: new extreme or first break | AllowStack allows consecutive signals on deeper breaks |

### Short entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | RSI overbought (when UseRsiFilter) | RSI confirms overbought |
| 2 | Stochastic overbought (optional) | When RequireStochOsOb is enabled |
| 3 | High > upper Donchian band | Price breaks above the calculated upper band |
| 4 | Stacking: new extreme or first break | AllowStack allows consecutive signals on higher highs |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `BandLength` | 20 | Donchian channel lookback (highest high / lowest low period) |
| `OuterMult` | 3.2 | Outer band multiplier (scaled by /2.5 internally) |
| `UseRsiFilter` | true | Require RSI oversold/overbought for signals |
| `RequireStochOsOb` | false | Require Stochastic oversold/overbought |
| `AllowStack` | true | Allow consecutive signals on deeper band breaks |
| `UseStopLoss` | false | Enable band-width-based stop-loss |

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| Donchian Channel (20) | Highest High / Lowest Low for band calculation |
| RSI | Oversold/overbought filter (enabled by default) |
| Stochastic-RSI | Optional oversold/overbought filter |

## Strategy type

- **Mean-reversion / band-break**
- Production strategy

## File structure

```
CryptoScanner.Analyzers/Dbr/
├── DbrPlugin.cs                      # Plugin registration
├── DbrSettings.cs                    # Strategy-specific settings
├── Dbr.md                            # This document
├── Config/
│   ├── StrategyDbrTabView.axaml      # Settings tab UI
│   └── StrategyDbrTabViewModel.cs    # Settings viewmodel
└── Signal/
    ├── DbrBandsHelper.cs             # Donchian band calculation
    ├── DbrSignalLong.cs              # Long signal: Low below lower band
    └── DbrSignalShort.cs             # Short signal: High above upper band
```

Enum value: `CryptoSignalStrategy.Dbr = 30`

## Registration

Registered as a **production** strategy in `AnalyzerRegistration.cs`. Strategy name in the UI: **dbr**.
