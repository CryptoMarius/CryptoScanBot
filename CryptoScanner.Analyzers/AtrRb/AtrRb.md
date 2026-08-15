# ATR Range Breakout (AtrRb)

## Overview

The **AtrRb** strategy detects mean-reversion setups by identifying candles that pierce ATR-based outer bands. It builds Keltner-style envelopes around an EMA basis and fires when price breaks beyond the outer band at a new local extreme. Currently **disabled** (registration is commented out) because it does not perform well enough in backtests.

## How it works

### Phase 1: Band calculation

An EMA of configurable `Length` (default 20) forms the centre line. An ATR of the same length measures volatility. Outer bands are placed at `EMA ± ATR × OuterMult` (default 4.2).

### Phase 2: Bollinger Bands width gate

The strategy reuses the global BB width percentage to ensure minimum volatility. The BB width must be at least `BBMinPercentage` (default 1.50%). An optional `BBMaxPercentage` caps extreme volatility (0 = no cap).

### Phase 3: Break detection

- **Long**: candle Low must break below the lower band **and** be the lowest Low within a trailing `BreakLookback` window (default 5 candles).
- **Short**: candle High must break above the upper band **and** be the highest High within the same window.

### Optional filters

- **RSI**: when `RequireRsiOsOb = true`, RSI must confirm oversold (long) or overbought (short).
- **Stochastic**: when `RequireStochOsOb = true`, Stochastic must also be at an extreme.
- **Band break confirmation on higher timeframes**: if `BandBreakConfirmationCount > 0`, that many consecutive higher timeframes must show the same band break.

### Entry price

Wick-only touches enter at the band price; full body breaks enter at the candle close. A stop-loss can optionally be set at `ATR × StopLossAtrFactor` distance.

## Signal conditions summary

### Long entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | BB width ≥ BBMinPercentage | Minimum volatility filter |
| 2 | Low < lower band | Price pierces EMA − ATR × OuterMult |
| 3 | Lowest Low in BreakLookback | Must be a new local low within the trailing window |
| 4 | RSI oversold (optional) | Only when RequireRsiOsOb is enabled |
| 5 | Stochastic oversold (optional) | Only when RequireStochOsOb is enabled |

### Short entry

| # | Condition | Description |
|---|-----------|-------------|
| 1 | BB width ≥ BBMinPercentage | Minimum volatility filter |
| 2 | High > upper band | Price pierces EMA + ATR × OuterMult |
| 3 | Highest High in BreakLookback | Must be a new local high within the trailing window |
| 4 | RSI overbought (optional) | Only when RequireRsiOsOb is enabled |
| 5 | Stochastic overbought (optional) | Only when RequireStochOsOb is enabled |

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Length` | 20 | EMA and ATR calculation period |
| `OuterMult` | 4.2 | Band distance multiplier (basis ± ATR × OuterMult) |
| `BreakLookback` | 5 | The break must be the most extreme price in this many candles |
| `BBMinPercentage` | 1.50 | Minimum Bollinger Bands width percentage |
| `BBMaxPercentage` | 0.0 | Maximum BB width percentage (0 = disabled) |
| `RequireRsiOsOb` | false | Require RSI oversold/overbought confirmation |
| `RequireStochOsOb` | false | Require Stochastic oversold/overbought confirmation |
| `UseStopLoss` | false | Enable ATR-based stop-loss |
| `StopLossAtrFactor` | 2.0 | Stop-loss distance in ATR multiples |

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| EMA(20) | Band centre line |
| ATR(20) | Band width calculation |
| Bollinger Bands | Width gate (min/max filter) |
| RSI | Optional oversold/overbought confirmation |
| Stochastic | Optional oversold/overbought confirmation |

## Strategy type

- **Mean-reversion / band-break**
- Currently disabled — registration is commented out in `AnalyzerRegistration.cs`.

## File structure

```
CryptoScanner.Analyzers/AtrRb/
├── AtrRbPlugin.cs                    # Plugin registration
├── AtrRbSettings.cs                  # Strategy-specific settings
├── AtrRb.md                          # This document
├── Config/
│   ├── StrategyAtrRbTabView.axaml    # Settings tab UI
│   └── StrategyAtrRbTabViewModel.cs  # Settings viewmodel
└── Signal/
    ├── AtrRbBandsHelper.cs           # Band calculation (EMA ± ATR × mult)
    ├── AtrRbSignalLong.cs            # Long signal: Low pierces lower band at new local low
    └── AtrRbSignalShort.cs           # Short signal: High pierces upper band at new local high
```

Enum value: `CryptoSignalStrategy.AtrRb = 29`

## Registration

Currently **disabled** (commented out in `AnalyzerRegistration.cs`). Strategy name in the UI: **atrrb**.
