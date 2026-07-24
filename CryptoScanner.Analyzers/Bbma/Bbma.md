# Bollinger Bands + Moving Average — BBMA Omni (Bbma)

## Overview

The **BBMA Omni** strategy is a port of the MQL5 "BBMA Oma Ally OmniView" indicator. It is a complex multi-timeframe hybrid strategy that classifies price action into discrete states (Extreme, CSD, CSM, TPW, MHV, Reentry, etc.) using Bollinger Bands, WMA zones, and EMA50. The signal fires on **Reentry** — a pullback to the WMA zone after a prior setup event — confirmed across three timeframes (LTF, MTF, HTF).

This is an experimental strategy (DEBUG-only).

## How it works

### Three-timeframe framework

The strategy operates on a fixed triplet of timeframes derived from the signal interval:

| Role | Example (LTF = 1h) |
|------|---------------------|
| LTF (Low Timeframe) | 1h — current interval, where the signal fires |
| MTF (Mid Timeframe) | 4h — confirmation layer |
| HTF (High Timeframe) | 1d — trend bias and setup detection |

### OmniState classification

Each candle is classified into one of these states:

| State | Description |
|-------|-------------|
| **Extreme** | WMA zone pokes outside BB + wick rejection (exhaustion signal) |
| **CSD (CSAK)** | BB-mid cross with price extending beyond WMA5/10 zone |
| **CSAK2** | Continuation — price beyond mid/WMA without reaching the outer band |
| **CSM (Momentum)** | Close beyond outer Bollinger Band |
| **CSAA** | WMA zone pullback through mid-BB |
| **Cross** | BB-mid or EMA50 crossover confirmed by the other level |
| **TPW** | First WMA-zone touch after an Extreme |
| **MHV** | Fractal pivot confirmed after TPW phase |
| **RejectedEma50** | EMA50 wick rejection filtered by ATR body size |
| **GapBbEma50** | EMA50 drifts outside BB, price returns inside |
| **Reentry** | Pullback to WMA zone, close on the correct side of BB-mid |

### Signal flow

1. **LTF** must be in **Reentry** state.
2. Walk back up to 30 LTF bars to find the preceding trigger (Extreme, TPW, MHV, CSM, CSD, RejectedEma50, or GapBbEma50).
3. **HTF** must also be in Reentry with a confirmed setup (priority: MHV > TPW > CSM > CSD).
4. **HTF trend filter** (long): EMA50 below BB-mid AND WMA05Low below BB-mid.
5. Code match: HTF = 'R' (Reentry) AND LTF lookback code ≠ '-' or 'R'.

### Give-up condition

An opposite-side **Extreme** on the current candle abandons the signal (e.g., bearish Extreme kills a waiting long signal).

## Signal conditions summary

### Long entry (bbma.omni)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | LTF state = Reentry | Pullback to WMA zone, close above BB-mid |
| 2 | LTF trigger found (≤30 bars back) | Preceding Extreme, TPW, MHV, CSM, CSD, etc. |
| 3 | HTF trend bullish | EMA50 < BB-mid AND WMA05Low < BB-mid |
| 4 | HTF state = Reentry + setup | Reentry confirmed by MHV/TPW/CSM/CSD lookback |
| 5 | Code match | HTF code = 'R', LTF trigger code ≠ '-' or 'R' |

### Short entry (bbma.omni)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | LTF state = Reentry | Pullback to WMA zone, close below BB-mid |
| 2 | LTF trigger found (≤30 bars back) | Preceding bearish setup event |
| 3 | HTF trend bearish | EMA50 > BB-mid AND WMA05High > BB-mid |
| 4 | HTF state = Reentry + setup | Reentry confirmed by matching setup lookback |
| 5 | Code match | HTF code = 'R', LTF trigger code ≠ '-' or 'R' |

## Settings

No strategy-specific settings beyond the base class. Sound files: `sound-bbma-long.wav` / `sound-bbma-short.wav`.

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| SMA(20) | BB mid-line (basis) |
| Bollinger Bands (20, 2σ) | Band classification (Extreme, CSM, CSD) |
| WMA05High / WMA05Low | WMA zone boundaries (Reentry, TPW detection) |
| WMA10High / WMA10Low | Extended WMA zone for CSD/CSAK classification |
| EMA(50) | Trend filter + RejectedEma50 / GapBbEma50 states |
| ATR(14) | Body-size filter for EMA50 rejection |

## Strategy type

- **Multi-timeframe hybrid** (mean-reversion entries within a trend-following framework)
- Experimental (DEBUG-only)

## File structure

```
CryptoScanner.Analyzers/Bbma/
├── BbmaPlugin.cs                         # Plugin registration (bbma.omni)
├── BbmaSettings.cs                       # Settings (base only)
├── Bbma.md                               # This document
├── Config/
│   ├── StrategyBbmaTabView.axaml         # Settings tab UI
│   └── StrategyBbmaTabViewModel.cs       # Settings viewmodel
└── Signal/
    ├── SignalBbmaOmniBase.cs             # OmniState classifier + 3-TF logic
    ├── SignalBbmaOmniLong.cs             # Long: bullish Reentry + HTF trend
    └── SignalBbmaOmniShort.cs            # Short: bearish Reentry + HTF trend
```

Enum value: `CryptoSignalStrategy.BbmaOmni = 43`

## Registration

Registered as a DEBUG-only strategy in `AnalyzerRegistration.cs`. Strategy name in the UI: **bbma.omni**.
