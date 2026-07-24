# Change of Character (Choch)

## Overview

The **Choch** (Change of Character) strategy detects market structure shifts using ZigZag-derived trend analysis. A CHoCH event marks the moment a prevailing trend reverses — the first higher-low in a downtrend (bullish CHoCH) or the first lower-high in an uptrend (bearish CHoCH). Four signal variants exist, combining two ZigZag trend depths with optional pullback confirmation.

This is an experimental strategy (DEBUG-only).

## How it works

### Trend detection

Market structure is determined via ZigZag analysis. Two trend depths are available:
- **Primary** — major market swings (fewer, larger pivots).
- **Secondary** — intermediate swings (more frequent pivots).

A CHoCH event is detected when the ZigZag-derived trend changes direction (e.g., from Bearish to Bullish).

### Direct variant (choch.primary / choch.secondary)

Fires immediately on the CHoCH event. The signal price is set to the CHoCH pivot price. Includes a warm-start guard: on first evaluation the current event is silently adopted without firing, to avoid false signals at startup.

### Pullback variant (choch.primary.pullback / choch.secondary.pullback)

Waits for a confirmed pullback after the CHoCH:

1. A ZigZag pivot must form in the expected direction (Low pivot for long, High pivot for short).
2. The current candle must close beyond the pivot value (above for long, below for short).
3. The candle must be directionally correct (bullish candle for long, bearish for short).
4. Optionally (`RequireBosConfirmation = true`): a Break of Structure (BOS) event must confirm the CHoCH before the pullback signal fires.

### Give-up condition

The signal is abandoned when the BOS trend reverts to the original direction (Bearish for long, Bullish for short).

## Signal conditions summary

### Long entry (direct)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | CHoCH event detected | ZigZag trend changes to Bullish |
| 2 | Not already fired | One signal per CHoCH event |
| 3 | Warm-start passed | First evaluation after startup is skipped |

### Long entry (pullback)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | CHoCH event detected | ZigZag trend changes to Bullish |
| 2 | ZigZag Low pivot formed | Pullback creates a higher low |
| 3 | Close > pivot value | Candle closes above the pullback pivot |
| 4 | Bullish candle | Close > Open |
| 5 | BOS confirmation (optional) | Break of Structure after CHoCH (when RequireBosConfirmation is enabled) |

### Short entry (mirror of long)

Same logic with reversed conditions — bearish CHoCH, ZigZag High pivot, close below pivot, bearish candle.

## Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `RequireBosConfirmation` | false | Pullback variants: require a BOS event between CHoCH and signal |

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| ZigZag (Primary or Secondary) | Trend structure and pivot detection |
| BOS (Break of Structure) | Trend confirmation and give-up condition |
| MarketTrend | Trend state calculation |

## Strategy type

- **Trend-reversal / structural**
- Four variants: 2 trend depths × 2 entry modes (direct / pullback)
- Experimental (DEBUG-only)

## File structure

```
CryptoScanner.Analyzers/Choch/
├── ChochPlugin.cs                                # Plugin registration (4 variants)
├── ChochSettings.cs                              # Settings (RequireBosConfirmation)
├── Choch.md                                      # This document
└── Signal/
    ├── SignalChochLongBase.cs                     # Shared long logic (CHoCH + trend + pullback)
    ├── SignalChochShortBase.cs                    # Shared short logic
    ├── SignalChochPrimaryLong.cs                  # Primary trend, direct entry
    ├── SignalChochPrimaryShort.cs                 # Primary trend, direct entry
    ├── SignalChochPrimaryPullbackLong.cs          # Primary trend, pullback entry
    ├── SignalChochPrimaryPullbackShort.cs         # Primary trend, pullback entry
    ├── SignalChochSecondaryLong.cs                # Secondary trend, direct entry
    ├── SignalChochSecondaryShort.cs               # Secondary trend, direct entry
    ├── SignalChochSecondaryPullbackLong.cs        # Secondary trend, pullback entry
    └── SignalChochSecondaryPullbackShort.cs       # Secondary trend, pullback entry
```

Enum values: `CryptoSignalStrategy.ChochPrimary = 60`, `ChochPrimaryPullback = 61`, `ChochSecondary = 62`, `ChochSecondaryPullback = 63`

## Registration

Registered as a DEBUG-only strategy in `AnalyzerRegistration.cs`. Strategy names in the UI: **choch.primary**, **choch.primary.pullback**, **choch.secondary**, **choch.secondary.pullback**.
