using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Bbma;

/* https://share.google/aimode/cb5CF0MrCDCKw2JCS

Het Beslissingsschema (De Cyclus)

De cyclus beweegt zich van een oververhitte markt(Extreme) naar een nieuwe trend(Momentum)
en biedt daartussen verschillende instapmomenten.

    Extreme: Een Moving Average(MA5 / 10) steekt buiten de Bollinger Bands(BB) uit.
             Dit is het eerste signaal van uitputting.
    TPW(Take Profit Wajib): De prijs keert terug naar de MA5 / 10 of Mid BB.
             Winstneming is hier verplicht ("Wajib").
    MHV(Market Has No Volume) : De prijs probeert de trend te hervatten maar slaagt er
             niet in de buitenste BB te doorbreken.Dit toont zwakte aan.
    CSD / CSAK(Candlestick Direction / Arah Kukuh) : Een "sterke" kaars die de Mid BB en MA5 / 10
             doorbreekt, wat de nieuwe richting bevestigt.
    Re-entry(na CSD) : De prijs trekt tijdelijk terug naar de MA5/10 zone voor een veilige instap in de nieuwe trend.
    CSM(Candlestick Momentum): De prijs breekt met kracht door de buitenste BB, wat een sterke trendbevestiging is.
    Re-entry(na CSM) : Na een momentum - uitbraak keert de prijs vaak terug naar de MA5/10 voor een tweede instapkans.
*/

#if DEBUG
public class SignalBbmaLong : SignalBbmaBase
{
    /// <summary>
    /// Classifies the BBMA state of a candle for Long setups (uses WMA5/10 on lows).
    /// Priority: MagicExtreme → Extreme → Extreme(Advance) → MHV → Reentry → None
    ///
    /// Extreme (Pine-aligned): MA5(low) below BB.Lower AND wick rejection —
    /// low pierced the band, close recovered inside.
    /// MHV (Pine-aligned): same wick condition as Extreme, but MA5 is inside the band —
    /// a failed second breakout attempt after a previous Extreme.
    /// </summary>
    public static BbmaState GetBbmaState(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal low = data.Candle.Low;
        decimal close = data.Candle.Close;
        decimal ema50 = (decimal)data.CandleData!.Ema50!.Value;
        decimal wma5Low = (decimal)data.CandleData!.Wma05Low!.Value;
        decimal wma10Low = (decimal)data.CandleData!.Wma10Low!.Value;
        decimal middleBand = (decimal)data.CandleData!.Sma20!.Value;
        decimal bbLower = (decimal)data.CandleData!.BollingerBandsLowerBand!.Value;

        if (wma5Low < bbLower)
        {
            // MagicExtreme (both MAs below BB.Lower) — merged into Extreme.
            // No downstream code uses the distinction, and keeping a distinct state
            // only complicated the TF-code matching (TfStateCode returns "EE" for it).
            if (wma10Low < bbLower)
                return BbmaState.Extreme;

            // Extreme (Pine-aligned): MA5(low) below BB.Lower AND wick rejection
            if (low < bbLower && close > bbLower)
                return BbmaState.Extreme;
        }

        // MHV (Mlv): wick pierced lower band, close recovered
        if (low < bbLower && close > bbLower)
            return BbmaState.Mlv;

        // Extreme (Advance): wick rejection of EMA50 (not in Pine, but valid extension)
        // This is only present in the PDF from str8v4lu3, not in the pine script
        // Disabled it for not, not sure what todo with it
        if (false && low < ema50 && close > ema50 && open > ema50)
            return BbmaState.Extreme;

        if (open > bbLower && close < bbLower)
            return BbmaState.Csm;

        // Reentry: local uptrend, close above mid, low touched the MA5/10 zone
        var upTrend = close > ema50;
        if (upTrend && close >= middleBand && low <= Math.Max(wma5Low, wma10Low) && close >= Math.Min(wma5Low, wma10Low))
            return BbmaState.Reentry;

        return BbmaState.None;
    }


    /// <summary>
    /// HTF validation for Long Re-entry. Checks for two setups (in priority order):
    ///
    ///   Path 1 — CSM/Reentry:  a bullish CSM candle (open inside band, close above upper BB) within the last 20 HTF bars.
    ///            The classic momentum breakout followed by a pullback to the MA zone.
    ///
    ///   Path 2 — MHV/Reentry:  an EXT or MHV candle (wick below lower BB, close back above)
    ///            within the last 10 HTF bars. The market failed to resume the downtrend,
    ///            now pulling back to the MA zone for a safe re-entry.
    ///
    /// Both paths are checked in a single pass (most recent event wins).
    ///
    /// Sets <paramref name="htfSetup"/> to "CSM" or "MHV" so the caller can show which path fired.
    /// </summary>
    private bool CheckHtf(CryptoInterval interval, MyData current, out string htfSetup)
    {
        htfSetup = "";
        //decimal ema50 = (decimal)current.CandleData.Ema50!.Value;
        //decimal sma20 = (decimal)current.CandleData.Sma20!.Value;
        //decimal wma5Low = (decimal)current.CandleData.Wma05Low!.Value;
        //decimal wma10Low = (decimal)current.CandleData.Wma10Low!.Value;


        // Check if BB is expanding, not a ranging chart https://youtu.be/tOQb6RRhbLA?t=102
        //if (wma10Low < sma20 || wma10Low < ema50)
        //{
        //    ExtraText = $"HTF Wma10Low not above mid-BB - ranging?";
        //    ScannerLog.Logger.Trace($"BBMA {Symbol.Name} {interval.Name} {SignalSide} {ExtraText}");
        //    return false;
        //}


        // Walk back through HTF history in one pass, tracking the most recent CSM and MHV positions.
        // Positions are expressed as bars-back-from-current (larger = further in the past).
        //
        // MHV/Reentry requires:
        //   1. An MHV/EXT within the last 10 bars (failed second downside breakout attempt)
        //   2. A prior bullish CSM further back, with at least MinGap bars between them
        //      — that gap represents the TPW phase that must occur between CSM and MHV.
        //      Without it the "MHV" is just noise immediately after the CSM breakout candle.
        // CSM/Reentry requires: a bullish CSM within the last 20 bars (and no valid MHV setup).
        const int MinGap = 3;
        int csmIndex = -1; // bars back where the most recent bullish CSM was found
        int mhvIndex = -1; // bars back where the most recent MHV/EXT was found
        MyData? prev = current;

        for (int i = 0; i < 20; i++)
        {
            if (!GetPrevCandle(interval, prev, out prev))
                break;

            // Track most recent bullish CSM.
            // SignalBbmaShort.GetBbmaState classifies this as Csm (open < bbUpper && close > bbUpper).
            // We cannot use this class's own GetBbmaState: its Csm = bearish CSM (opposite direction).
            if (csmIndex < 0 && SignalBbmaShort.GetBbmaState(prev!) == BbmaState.Csm)
                csmIndex = i;

            // Track most recent MHV within the MHV lookback window.
            // Only Mlv qualifies: wick pierced lower BB, close recovered, MA5 still inside band.
            // MagicExtreme and Extreme are intentionally excluded: they belong to the Extreme phase
            // (first step in the BBMA cycle), not the MHV phase (third step / failed second attempt).
            if (i < 10 && mhvIndex < 0)
            {
                BbmaState state = GetBbmaState(prev!);
                if (state == BbmaState.Mlv)
                    mhvIndex = i;
            }
        }

        // Path 2: MHV/Reentry
        // Conditions: MHV found, preceded by a bullish CSM (csmIndex > mhvIndex),
        // with at least MinGap bars between them, AND a proven TPW candle in that gap.
        // TPW for Long (after bullish breakout above upper BB): price must have pulled back
        // far enough to touch the mid-BB (SMA20) before attempting the MHV.
        if (mhvIndex >= 0 && csmIndex > mhvIndex && csmIndex - mhvIndex >= MinGap)
        {
            bool hadTpw = false;
            prev = current;
            for (int i = 0; i <= csmIndex; i++)
            {
                if (!GetPrevCandle(interval, prev, out prev))
                    break;

                // Only inspect candles strictly between MHV and CSM
                if (i > mhvIndex && i < csmIndex)
                {
                    decimal midBb = (decimal)prev!.CandleData.Sma20!.Value;
                    if (prev.Candle.Low <= midBb)
                    {
                        hadTpw = true;
                        break;
                    }
                }
            }

            if (hadTpw)
            {
                htfSetup = "MHV";
                return true;
            }
        }

        // Path 1: CSM/Reentry — bullish CSM found but MHV/Reentry did not qualify
        if (csmIndex >= 0)
        {
            htfSetup = "CSM";
            return true;
        }

        return false;
    }


    /// <summary>
    /// Entry timing filter: checks the 5m chart for WMA05High crossing below WMA10High.
    /// This indicates the pullback has just entered the MA zone on the 5m — the optimal Long re-entry moment.
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal)
    {
        CryptoInterval interval5m = Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval5m).Interval;

        // Ensure 5m indicator data is available in IndicatorDataList
        if (!IndicatorDataList.PrepareIndicators(Symbol, interval5m, CandleLast.Candle.OpenTime))
            return false;

        // Align current time down to the nearest 5m boundary
        CandleTime time5m = CandleLast.Candle.OpenTime - (CandleLast.Candle.OpenTime % interval5m.Duration);

        if (!IndicatorDataList.TryGetCandle(interval5m, time5m, out MyData? current5m) || current5m == null)
            return false;

        if (!GetPrevCandle(interval5m, current5m, out MyData? prev5m) || prev5m == null)
            return false;

        if (current5m.CandleData.Wma05High == null || current5m.CandleData.Wma10High == null ||
            prev5m.CandleData.Wma05High == null || prev5m.CandleData.Wma10High == null)
            return false;

        decimal wma05HighNow  = (decimal)current5m.CandleData.Wma05High.Value;
        decimal wma10HighNow  = (decimal)current5m.CandleData.Wma10High.Value;
        decimal wma05HighPrev = (decimal)prev5m.CandleData.Wma05High.Value;
        decimal wma10HighPrev = (decimal)prev5m.CandleData.Wma10High.Value;

        // Crossover on 5m: WMA05High just dropped below WMA10High
        return wma05HighNow < wma10HighNow && wma05HighPrev >= wma10HighPrev;
    }


    /// <summary>
    /// Invalidates the setup when the current candle is a Short Extreme.
    /// A bearish extreme after a bullish CSM means the setup has been overridden — give up.
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        BbmaState state = GetBbmaState(CandleLast);
        return state == BbmaState.Extreme || state == BbmaState.MagicExtreme;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        MyData? candleLtf = CandleLast;
        BbmaState stateLtf = GetBbmaState(candleLtf);
        BbmaState stateMtf = BbmaState.None;
        BbmaState stateHtf = BbmaState.None;

        // LTF must be in Reentry state - entry
        if (stateLtf != BbmaState.Reentry)
        {
            ExtraText = $"LTF not in Reentry ({TfStateCode(stateLtf)})";
            return false;
        }

        // Resolve fixed BBMA higher timeframe pair
        if (!GetIntervals(out CryptoIntervalPeriod mtf, out CryptoIntervalPeriod htf))
            return false;


        // Walk back through LTF candles to find the preceding extreme (or otherwise)
        for (int i = 0; i < 30; i++)
        {
            if (!GetPrevCandle(candleLtf, out candleLtf))
            {
                ExtraText = $"insufficient LTF history for lookback ({i} candles checked)";
                return false;
            }

            // Not the band-crossing moment yet (e.g. None) — keep walking back
            stateLtf = GetBbmaState(candleLtf!);
            if (stateLtf == BbmaState.Extreme || stateLtf == BbmaState.MagicExtreme
                || stateLtf == BbmaState.Mlv || stateLtf == BbmaState.Csm)
                break;
        }
        string code = TfStateCode(stateHtf) + TfStateCode(stateMtf) + TfStateCode(stateLtf);

        if (!(stateLtf == BbmaState.Extreme || stateLtf == BbmaState.MagicExtreme
            || stateLtf == BbmaState.Mlv || stateLtf == BbmaState.Csm))
        {
            ExtraText = $"LTF unexpected state";
            ScannerLog.Logger.Trace($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }


        // --------------------------
        // Middle timeframe (MTF)
        // Use the current reentry candle time (CandleLast), not the extreme candle time (candleLtf).
        // When the 5m reentry candle at e.g. 17:55 closes at 18:00, the 15m candle 17:45→18:00
        // and the 1h candle 17:00→18:00 also close simultaneously — those are the correct MTF/HTF candles.
        var resultMtf = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, mtf);
        if (!resultMtf.success || resultMtf.candle == null || !IndicatorsOkay(resultMtf.candle))
        {
            ExtraText = $"no data for MTF ({resultMtf.higherInterval.Interval.Name})";
            ScannerLog.Logger.Trace($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }
        stateMtf = GetBbmaState(resultMtf.candle);
        code = TfStateCode(stateHtf) + TfStateCode(stateMtf) + TfStateCode(stateLtf);



        // --------------------------
        // Highest timeframe (HTF)
        var resultHtf = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, htf);
        if (!resultHtf.success || resultHtf.candle == null || !IndicatorsOkay(resultHtf.candle))
        {
            ExtraText = $"no data for HTF";
            ScannerLog.Logger.Trace($"BBMA {Symbol.Name} {resultHtf.higherInterval.Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }
        stateHtf = GetBbmaState(resultHtf.candle); // just to show something
        code = TfStateCode(stateHtf) + TfStateCode(stateMtf) + TfStateCode(stateLtf);


        // MTF-state validation is no longer a pre-filter here — the final code-match below
        // is the authoritative gate. A separate pre-filter rejected MTF=Reentry before the
        // match could evaluate, which made code "RRE" unreachable.



        // --------------------------
        // Highest timeframe (HTF)

        // Zit de prijs boven de EMA 50? (Trendfilter)
        // Trend filter on TF3 EMA50 below mid-BB (SMA20) = bullish bias
        double wma05LowHtf = resultHtf.candle.CandleData!.Wma05Low!.Value;
        double ema50Htf = resultHtf.candle.CandleData!.Ema50!.Value;
        double midBbHtf = resultHtf.candle.CandleData!.Sma20!.Value;
        if (ema50Htf >= midBbHtf || wma05LowHtf >= midBbHtf)
        {
            ExtraText = $"HTF ema50 not below mid-BB - bearish bias";
            ScannerLog.Logger.Trace($"BBMA {Symbol.Name} {resultHtf.higherInterval.Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }


        stateHtf = GetBbmaState(resultHtf.candle);
        if (stateHtf != BbmaState.Reentry)
        {
            ExtraText = $"HTF not in Reentry ({TfStateCode(stateHtf)})";
            ScannerLog.Logger.Trace($"BBMA {Symbol.Name} {resultHtf.higherInterval.Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }

        if (!CheckHtf(resultHtf.higherInterval.Interval, resultHtf.candle, out string htfSetup))
        {
            ExtraText = $"HTF not in CSM/MHV reentry state";
            ScannerLog.Logger.Trace($"BBMA {Symbol.Name} {resultHtf.higherInterval.Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }




        // --------------------------
        // Check...
        // MTF code: TF3→TF2→TF1 (highest to lowest).
        // Because TF1 is always R (entry condition) and TF3 is always R (HTF anchor),
        // the entry-phase codes are the PDF alert codes with TF1 replaced by R:
        //   PDF alert RRE  → entry code RRR  (TF2=Reentry)
        //   PDF alert REM  → entry code RER  (TF2=Extreme, from M alert)
        //   PDF alert REE  → entry code RER  (TF2=Extreme, from E alert)
        //   PDF alert RMEE → entry code RMR  (TF2=MLV, from MagicExtreme alert — MagicExtreme merged into Extreme, so match string is "RME")
        code = TfStateCode(stateHtf) + TfStateCode(stateMtf) + TfStateCode(stateLtf);
        if (code == "RRE" || code == "REM" || code == "REE" || code == "RME")
        {
            ExtraText = $"{code} [{htfSetup}] {resultHtf.higherInterval.Interval.Name}/{resultMtf.higherInterval.Interval.Name}/{Interval.Name}";

            // Debug to see if the right candles are selected
            ScannerLog.Logger.Trace($"BBMA HIT {Symbol.Name} {resultHtf.higherInterval.Interval.Name} {code} {SignalSide} HTF {resultHtf.candle.Candle.OpenTime.ToLocalTime()} {resultHtf.candle.Candle.Close} {ExtraText}");
            ScannerLog.Logger.Trace($"BBMA HIT {Symbol.Name} {resultMtf.higherInterval.Interval.Name} {code} {SignalSide} MTF {resultMtf.candle.Candle.OpenTime.ToLocalTime()} {resultMtf.candle.Candle.Close} {ExtraText}");
            ScannerLog.Logger.Trace($"BBMA HIT {Symbol.Name} {Interval.Name} {code} {SignalSide} LTF {CandleLast.Candle.OpenTime.ToLocalTime()} {CandleLast.Candle.Close} {ExtraText}");
            return true;
        }

        ExtraText = $"code {code} not valid ({resultHtf.higherInterval.Interval.Name}/{resultMtf.higherInterval.Interval.Name}/{Interval.Name})";
        return false;
    }
}
#endif
