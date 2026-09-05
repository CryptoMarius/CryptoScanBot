# Bollinger Bands + Moving Average — BBMA Omni (Bbma)

## Overview

The **BBMA Omni** strategy is a port of the MQL5 "BBMA Oma Ally OmniView" indicator, with a
three-timeframe reentry signal on top. It classifies price action into discrete states (Extreme,
CSD, CSM, TPW, MHV, Reentry, etc.) using Bollinger Bands, the LWMA 5/10 zones on high and low,
and the EMA50. The signal fires on **Reentry** — a pullback to the MA5/10 zone after a preceding
trigger — confirmed by a Reentry and a trend zone on the higher timeframe (HTF).

The strategy is experimental. It is registered in every build (the `#if DEBUG` around the
registration in `AnalyzerRegistration.cs` is commented out), but it has only been measured in the
emulator, never traded.

**What this is, and what it is not.** BBMA is a discretionary chart-reading method from the
Malaysian trader Oma Ally; there is no published mechanical rule set of his own. OmniView is a
mechanisation of it by a third party (a Forex Factory user). Everything below inherits the
choices of that indicator. The state classifiers are kept as close to the MQL5 source as possible
(`Documentation/BBMA Oma Ally OmniView.mq5`, with line numbers in the code comments) so they can
be cross-referenced; the 3-TF signal, the HTF setup check and the exit are our own layer.

## The BBMA rules (as documented)

From `Documentation/BBMA Summary.pdf` and the sources at the end of this document:

| Element | Rule |
|---------|------|
| Indicators | Bollinger Bands (20, 2), LWMA 5 and 10 on High and on Low, EMA 50 |
| Trend | EMA50 below the mid-band = uptrend, above = downtrend |
| Reentry zones | LWMA 5/10 Low = buy zone, LWMA 5/10 High = sell zone |
| Cycle | Extreme → TPW → MHV → CSD → Reentry → CSM → Reentry |
| Setups | Extreme (counter-trend, TP at the MA5/10 = TPW), Reentry after CSD (reversal), Reentry after CSM (continuation) |
| Strongest setups | Reentry after CSD and Reentry after CSM |
| Reentry | pullback to the MA5/10 zone; the candle must not close beyond the MA5/10; "powerful" when the zone meets the mid-band, "most powerful" when the EMA50 is there too |
| Multi-timeframe | three timeframes (D1/H4/H1 for swing, H4/H1/M15 for day trading), codes R = Reentry, E = Extreme, M = MHV, EE = double Extreme; e.g. REM |
| Exit | stop beyond the MHV or the last swing, take profit at the outer band (of the HTF) |

## How it works

### Three-timeframe framework

The strategy operates on a fixed triplet of timeframes derived from the signal interval
(`SignalBbmaBase.GetIntervals`):

| Role | Example (LTF = 1h) |
|------|---------------------|
| LTF (Low Timeframe) | 1h — current interval, where the signal fires |
| MTF (Mid Timeframe) | 4h — shown in the code string, not a condition |
| HTF (High Timeframe) | 1d — trend zone, Reentry and setup detection |

The HTF candle used is the last **closed** HTF candle at the time of the LTF candle
(`IndicatorEngine.CalculateIndicatorsForInterval`).

### OmniState classification

Each candle is classified into independent per-bar buffers (`OmniBar`), the C# equivalent of the
MQL5 indicator buffers; more than one can be true on the same bar:

| State | Description |
|-------|-------------|
| **Extreme** | WMA zone pokes outside BB + wick rejection (exhaustion signal) |
| **CSD (CSAK)** | BB-mid cross with price extending beyond WMA5/10 zone |
| **CSAK2** | Continuation — price beyond mid/WMA without reaching the outer band |
| **CSM (Momentum)** | Close beyond outer Bollinger Band |
| **CSAA** | WMA zone pullback through mid-BB |
| **Cross** | BB-mid or EMA50 crossover confirmed by the other level |
| **TPW** | First WMA-zone touch after an Extreme (forward-pass state machine) |
| **MHV** | Fractal pivot confirmed after TPW phase (needs the next bar) |
| **RejectedEma50** | EMA50 wick rejection filtered by ATR body size |
| **GapBbEma50** | EMA50 drifts outside BB, price returns inside |
| **Reentry** | Pullback to WMA zone, close on the correct side of BB-mid. Strict (default): close back beyond BOTH MA5 and MA10 and the zone on the trend side of the mid-band; loose: the "AllBBMA" variant of OmniView (MA5 or MA10, zone anywhere) |

### Signal flow

1. **LTF** must be in **Reentry** state (buffer check, not the derived label).
2. Walk back up to 30 LTF bars to find the preceding trigger (MHV first, then any of Extreme,
   TPW, CSM, CSD, CSAK2, CSAA, Cross, RejectedEma50, GapBbEma50). The trigger has to be at least
   **ReentryMinCandlesAfterTrigger** candles back (default 3, the "minimum of three candles" of
   the rules): a pullback that has not started yet is not a reentry.
3. **HTF trend zone**: the OmniView Green Zone for a long (EMA50 at or below the mid-band AND all
   four WMA's at or above it), the Red Zone for a short (`IsHtfTrendBullish` / `IsHtfTrendBearish`).
4. **HTF** must also be in Reentry, and that reentry needs a setup behind it (`CheckHtf`, run on a
   classifier of the HTF): the most recent CSD (or CSAK2, its early form) or CSM on the trade's
   side within **HtfSetupLookback** HTF candles (default 10). The setup is void when the market
   has since said the other way: an opposite-side CSM (close beyond the far band) always, an
   opposite-side Extreme when **HtfSetupExtremeInvalidates** is on. The "[setup]" text in the
   ExtraText names it ("CSM", "CSD", prefixed with "TPW>" or "MHV>" when that opened the cycle).
   A lookback of zero switches the check off.
5. Code match (`IsCodeMatch`): HTF = 'R' (Reentry) AND LTF lookback code ≠ '-' (nothing found)
   and ≠ 'R' (another Reentry).

Letter codes: E = Extreme, T = Tpw, H = Mhv, J = RejectedEma50, G = GapBbEma50, R = Reentry,
2 = Csak2, A = Csaa, X = Cross, **D = Csd, M = Csm**, - = none.

### Fixes of 2026-09-05

Two conditions of our own layer contradicted the rules and made the strategy fire almost never
(161 signals over all Binance coins in three months in the Session0 runs), and two more were
found on the way:

1. **CSD and CSM were rejected as LTF trigger.** Both mapped to '-', which the code match refuses.
   Those are exactly the two setups the rules call the strongest. They now carry the letters D and M.
2. **The HTF trend filter was inverted.** A long demanded the WMA5-Low BELOW the mid-band, the
   opposite of OmniView's own Green Zone (mq5 line 711) and of a reentry in an uptrend. It is now
   the Green/Red Zone as OmniView draws it.
3. **The HTF setup check never blocked.** Its two priority rules wanted the CSM older than the
   MHV or TPW (the reverse of the cycle) and it fell through to "any CSM or CSD-class candle in
   the last twenty", which every trending HTF satisfies. It is now the rule: a CSD or CSM on the
   trade's side, not voided since (see step 4 above).
4. **HTF and MTF candles were classified by the LTF instance.** Every multi-bar condition (the
   two-bar CSD, the Extreme anti-repeat guard, GapBbEma50, the EMA50 rejection, the TPW backward
   scan) reads "the previous candle" through the instance's own interval, so the previous candle
   of a 1d candle was the 1h candle before its open. MTF and HTF candles now go through a
   classifier made for their interval (`CreateForInterval`), with the opposite-side checkers
   wired up for that interval.

### Exit (the strategy's own)

The rules give a reentry trade a take profit at the outer band and a stop beyond the swing. Both
are settings, on by default:

| Setting | Mechanism |
|---------|-----------|
| **Take profit at the outer band** | `IsExitSignal` (asked by the position monitor on every close of the position's interval): a long leaves once a closed candle has reached the upper band, a short once one has reached the lower band. The trader's stop loss and take profit keep working next to it — set the global take profit wide to measure the pure band exit. |
| **Use the HTF band** | The band aimed at is the outer band of the HTF of the fixed triplet (the 1d band for a 1h entry), read from the last closed HTF candle — the rules give the take profit on the band of the higher timeframe. Off aims at the band of the position's own interval, the nearer target. |
| **Stop beyond the reentry candle** | The signal hands `OverrideSlPercentage` to the trader: the distance from the close to the low (long) or high (short) of the reentry candle, plus the **stop margin %** (default 0.1). Off leaves the global stop loss percentage. |

### Give-up condition

An opposite-side **Extreme** on the current candle abandons a waiting signal (e.g. a bearish
Extreme kills a waiting long signal).

## Signal conditions summary

### Long entry (bbma.omni)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | LTF state = Reentry | Pullback to WMA(low) zone, close above BB-mid |
| 2 | LTF trigger found (≤30 bars back) | Preceding MHV, Extreme, TPW, CSM, CSD, etc. |
| 3 | HTF Green Zone | EMA50 ≤ mid-BB AND WMA 5/10 high and low ≥ mid-BB |
| 4 | HTF state = Reentry + setup | A CSD or CSM buy within the lookback, no CSM sell (or Extreme sell) since |
| 5 | Code match | HTF code = 'R', LTF trigger code ≠ '-' and ≠ 'R' |

### Short entry (bbma.omni)

| # | Condition | Description |
|---|-----------|-------------|
| 1 | LTF state = Reentry | Pullback to WMA(high) zone, close below BB-mid |
| 2 | LTF trigger found (≤30 bars back) | Preceding bearish setup event |
| 3 | HTF Red Zone | EMA50 ≥ mid-BB AND WMA 5/10 high and low ≤ mid-BB |
| 4 | HTF state = Reentry + setup | A CSD or CSM sell within the lookback, no CSM buy (or Extreme buy) since |
| 5 | Code match | HTF code = 'R', LTF trigger code ≠ '-' and ≠ 'R' |

## Settings

| Setting | Default | Meaning |
|---------|---------|---------|
| ReentryStrict | true | Close back beyond both MA5 and MA10, zone on the trend side of the mid-band; off = the loose OmniView form |
| ReentryMinCandlesAfterTrigger | 3 | The LTF trigger has to be at least this many candles behind the reentry candle; 0 = off |
| HtfSetupLookback | 10 | How many HTF candles back the CSD or CSM behind the HTF reentry may lie; 0 = check off |
| HtfSetupExtremeInvalidates | true | An opposite-side Extreme on the HTF after the setup voids it (an opposite CSM always does) |
| TakeProfitAtOuterBand | true | Leave once a closed candle reached the outer band (IsExitSignal) |
| TakeProfitOnHtfBand | true | Aim at the band of the HTF of the triplet instead of the own interval |
| StopBeyondReentryCandle | true | Stop just beyond the far side of the reentry candle (OverrideSlPercentage) |
| StopMarginPercentage | 0.1 | Extra room beyond the reentry candle, as a percentage of the price |

Sound files: `sound-bbma-long.wav` / `sound-bbma-short.wav`. Emulator queue key: `"bbma.omni"`.

## Known deviations from the rules (open)

- The MTF is computed and shown in the code string but is not a condition; the classic codes
  (REM, RRE, REE) do use it.
- The HTF setup takes CSD, CSAK2 and CSM as setups; CSAA and Cross no longer count (they are
  not direction candles in the rules).
- Extreme type B and Magic Extreme from the PDF are not separate states; MHV as "fractal after
  TPW" is the indicator author's approximation.
- The stop uses the reentry candle's own extreme, the tightest reading of "beyond the swing".
- "A reentry occurs for a minimum of three candles" is read as "the trigger is at least three
  candles back"; the source does not say whether it means the length of the pullback or the
  number of reentry candles.

Resolved on 2026-09-05: the loose Reentry detector (now **ReentryStrict**, with
**ReentryMinCandlesAfterTrigger**), the take-profit band of the own interval (now
**TakeProfitOnHtfBand**) and the HTF setup check that never blocked (now **HtfSetupLookback**
with **HtfSetupExtremeInvalidates**). The loose forms stay available as settings so the emulator
can compare.

## Indicators used

| Indicator | Purpose |
|-----------|---------|
| SMA(20) | BB mid-line (basis) |
| Bollinger Bands (20, 2σ) | Band classification (Extreme, CSM, CSD), exit target |
| WMA05High / WMA05Low | WMA zone boundaries (Reentry, TPW detection) |
| WMA10High / WMA10Low | Extended WMA zone for CSD/CSAK classification |
| EMA(50) | Trend zone + RejectedEma50 / GapBbEma50 states |
| ATR(14) | Body-size filter for EMA50 rejection |

## Strategy type

- **Multi-timeframe hybrid** (mean-reversion entries within a trend-following framework)
- Experimental

## File structure

```
CryptoScanner.Analyzers/Bbma/
├── BbmaPlugin.cs                         # Plugin registration (bbma.omni)
├── BbmaSettings.cs                       # Exit settings
├── Bbma.md                               # This document
├── Documentation/
│   ├── BBMA Oma Ally OmniView.mq5        # The MQL5 source the classifiers are ported from
│   └── BBMA Summary.pdf                  # The rules (str8v4lu3's summary of Oma Ally's method)
├── Config/
│   ├── StrategyBbmaTabView.axaml         # Settings tab UI
│   ├── StrategyBbmaSettingsView.axaml    # Exit settings groupbox
│   └── StrategyBbmaTabViewModel.cs       # Settings viewmodel
└── Signal/
    ├── SignalBbmaBase.cs                 # 3-TF interval pairs, IndicatorsOkay
    ├── SignalBbmaOmniBase.cs             # OmniState classifier, HTF zone, HTF setup, code match, reentry, exit
    ├── SignalBbmaOmniLong.cs             # Long: bullish Reentry + HTF Green Zone
    ├── SignalBbmaOmniShort.cs            # Short: bearish Reentry + HTF Red Zone
    └── SignalBbMaLong.cs / Short.cs      # The original Pine-aligned attempt, unregistered
```

Tests: `CryptoScanner.CoreTests/Analyzer/Bbma/BbmaOmniTests.cs` (code match, HTF zone, HTF
setup on a 1h series, strict and loose reentry, exit on the own and the HTF band, stop), `BbmaStateTests.cs` (the original classifier), `BbmaSignalSimulationTests.cs` (candle by
candle on the ADAUSDT data set).

## Registration

Registered in `AnalyzerRegistration.cs`. Strategy name in the UI: **bbma.omni**.

## Sources

- `Documentation/BBMA Summary.pdf` — "BBMA Trading Summary" by str8v4lu3 (18 pages): indicators,
  the seven-component cycle, the five setups, the multi-timeframe codes.
- `Documentation/BBMA Oma Ally OmniView.mq5` — the indicator the state classifiers are ported from.
- [BBMA Oma Ally OmniView (Forex Factory)](https://www.forexfactory.com/thread/1377382-bbma-oma-ally-omniview) — the thread of the OmniView indicator.
- [BBMA System (Forex Factory)](https://www.forexfactory.com/thread/1329529-bbma-system) — the thread the PDF refers to.
- [BBMA Oma Ally Part 8, Reentry (armaila)](https://fx.armaila.com/2024/11/bbma-oma-ally-part-8-mengenal-reentry.html) — the reentry rule: the candle must not close beyond the MA5/10; reentry after CSA and after CSM.
- [BBMA Oma Ally 2022 Trading Strategy (Crewenk Trader)](https://crewenktrader.blogspot.com/2021/10/bbma-oma-ally-2022-trading-strategy.html) — indicator settings, Extreme, MHV, CSAK/CSD, CSM, reentry, the RRE/REE/REM codes.
- [Mengenal Metode BBMA (Kelas Jutawan Trader)](https://kelasjutawantrader.com/mengenal-metode-bbma-strategi-trading-dari-oma-ally/) — the cycle and the three entry types.
- [BBMA Oma Ally forex trading technique (BabyPips)](https://forums.babypips.com/t/bollinger-bands-moving-average-bbma-oma-ally-forex-trading-technique/136067) — powerful / most powerful reentry, minimum of three candles, the timeframe sets.
- [BBMA System Summary (Scribd)](https://www.scribd.com/document/850863544/BBMA-System-Summary) — the same PDF, online.
- [Bollinger Bands + Moving Average (BBMA Oma Ally), TradingView script](https://www.luxalgo.com/library/indicator/azoFriT7-bollinger-bands-moving-average-bbma-oma-ally/) — the TradingView mechanisation (indicator settings: LWMA 5/10 on high and low, EMA 50, BB 20/2).
