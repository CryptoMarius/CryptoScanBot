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

public class SignalBbmaShort : SignalBbmaBase
{
    /// <summary>
    /// Classifies the BBMA state of a candle for Short setups (uses WMA5/10 on highs).
    /// Priority: MagicExtreme → Extreme → Extreme(Advance) → MHV → Reentry → None
    ///
    /// Extreme (Pine-aligned): MA5(high) above BB.Upper AND wick rejection —
    /// high pierced the band, close recovered inside.
    /// MHV (Pine-aligned): same wick condition as Extreme, but MA5 is inside the band —
    /// a failed second breakout attempt after a previous Extreme.
    /// </summary>
    public static BbmaState GetBbmaState(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal high = data.Candle.High;
        decimal close = data.Candle.Close;
        decimal ema50 = (decimal)data.CandleData!.Ema50!.Value;
        decimal wma5High = (decimal)data.CandleData!.Wma05High!.Value;
        decimal wma10High = (decimal)data.CandleData!.Wma10High!.Value;
        decimal middleBand = (decimal)data.CandleData!.Sma20!.Value;
        decimal bbUpper = (decimal)data.CandleData!.BollingerBandsUpperBand!.Value;

        if (wma5High > bbUpper)
        {
            // MagicExtreme (EE): both MAs above BB.Upper
            if (wma10High > bbUpper)
                return BbmaState.MagicExtreme;

            // Extreme (Pine-aligned): MA5(high) above BB.Upper AND wick rejection
            if (high > bbUpper && close < bbUpper)
                return BbmaState.Extreme;
        }

        // Extreme type B wick rejection of upper bb
        if (high > bbUpper && close < bbUpper)
            return BbmaState.Extreme;

        // Extreme (Advance): wick rejection of EMA50 (not in Pine, but valid extension)
        if (high > ema50 && close < ema50 && open < ema50)
            return BbmaState.Extreme;

        // MHV (Market Has No Volume): wick pierced upper band, close recovered, MA5 still inside band
        // Priority above Reentry per Pine: EXT > MHV > RE
        if (high > bbUpper && close < bbUpper)
            return BbmaState.Mlv;

        if (open < bbUpper && close > bbUpper)
            return BbmaState.Csm;

        // Reentry: local downtrend, close below mid, high touched the MA5/10 zone
        var downTrend = close < ema50;
        if (downTrend && close <= middleBand && high >= Math.Min(wma5High, wma10High) && close <= Math.Max(wma5High, wma10High))
            return BbmaState.Reentry;

        return BbmaState.None;
    }


    /// <summary>
    /// HTF validation for Short Re-entry. Checks for two setups (in priority order):
    ///
    ///   Path 1 — CSM/Reentry:  a bearish CSM candle (open inside band, close below lower BB) within the last 20 HTF bars.
    ///            The classic momentum breakdown followed by a pullback to the MA zone.
    ///
    ///   Path 2 — MHV/Reentry:  an EXT or MHV candle (wick above upper BB, close back below)
    ///            within the last 10 HTF bars. The market failed to resume the uptrend,
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
        //decimal wma5High = (decimal)current.CandleData.Wma05High!.Value;
        //decimal wma10High = (decimal)current.CandleData.Wma10High!.Value;


        // Check if BB is expanding, not a ranging chart https://youtu.be/tOQb6RRhbLA?t=102
        //if (wma10High > sma20 || wma10High > ema50)
        //{
        //    ExtraText = $"HTF Wma10Low not below mid-BB - ranging";
        //    ScannerLog.Logger.Trace($"BBMA {Symbol.Name} {interval.Name} {SignalSide} {ExtraText}");
        //    return false;
        //}


        // ── Path 1: CSM/Reentry ──────────────────────────────────────────────────────
        // Did we have a bearish CSM (open inside band, close below lower BB) within the last 20 HTF bars?
        // SignalBbmaLong.GetBbmaState classifies exactly this condition as Csm (open > bbLower && close < bbLower).
        MyData? prev = current;
        for (int i = 0; i < 20; i++)
        {
            if (!GetPrevCandle(interval, prev, out prev))
                break;

            // We cannot use this class's own GetBbmaState here because its Csm = bullish CSM (opposite direction).
            if (SignalBbmaLong.GetBbmaState(prev!) == BbmaState.Csm)
            {
                htfSetup = "CSM";
                return true;
            }


            // ── Path 2: MHV/Reentry (Pine extComboBuy) ───────────────────────────────────
            // Did we have an EXT or MHV candle (wick rejection of the upper BB) within the last 10 HTF bars?
            // Use GetBbmaState so all classification logic lives in one place (easier for unit tests).
            if (i < 10) // Pine used only 6 candles
            {
                BbmaState state = GetBbmaState(prev!);
                if (state == BbmaState.Mlv || state == BbmaState.Extreme || state == BbmaState.MagicExtreme)
                {
                    htfSetup = "MHV";
                    return true;
                }
            }
        }

        //// ── Path 2: MHV/Reentry (Pine extComboBuy) ───────────────────────────────────
        //// Did we have an EXT or MHV candle (wick rejection of the upper BB) within the last 6 HTF bars?
        //// Use GetBbmaState so all classification logic lives in one place (easier for unit tests).
        //prev = current;
        //for (int i = 0; i < 10; i++)
        //{
        //    if (!GetPrevCandle(interval, prev, out prev))
        //        break;

        //    BbmaState state = GetBbmaState(prev!);
        //    if (state == BbmaState.Mlv || state == BbmaState.Extreme || state == BbmaState.MagicExtreme)
        //    {
        //        htfSetup = "MHV";
        //        return true;
        //    }
        //}

        return false;
    }


    /// <summary>
    /// Entry timing filter: checks the 5m chart for WMA05Low crossing above WMA10Low.
    /// This indicates the pullback has just entered the MA zone on the 5m — the optimal Short re-entry moment.
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

        if (current5m.CandleData.Wma05Low == null || current5m.CandleData.Wma10Low == null ||
            prev5m.CandleData.Wma05Low == null || prev5m.CandleData.Wma10Low == null)
            return false;

        decimal wma05LowNow  = (decimal)current5m.CandleData.Wma05Low.Value;
        decimal wma10LowNow  = (decimal)current5m.CandleData.Wma10Low.Value;
        decimal wma05LowPrev = (decimal)prev5m.CandleData.Wma05Low.Value;
        decimal wma10LowPrev = (decimal)prev5m.CandleData.Wma10Low.Value;

        // Crossover on 5m: WMA05Low just rose above WMA10Low
        return wma05LowNow > wma10LowNow && wma05LowPrev <= wma10LowPrev;
    }


    /// <summary>
    /// Invalidates the setup when the current candle is a Long Extreme.
    /// A bullish extreme after a bearish CSM means the setup has been overridden — give up.
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


        // MTF must have a relevant BBMA state (Extreme, MagicExtreme or MHV)
        if (!(stateMtf == BbmaState.Extreme || stateMtf == BbmaState.MagicExtreme || stateMtf == BbmaState.Mlv))
        {
            ExtraText = $"MTF state not valid";
            //ScannerLog.Logger.Trace($"BBMA {Symbol.Name} {resultMtf.higherInterval.Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }



        // --------------------------
        // Highest timeframe (HTF)

        // Zit de prijs boven de EMA 50? (Trendfilter)
        // Trend filter on TF3: EMA50 above mid-BB (SMA20) = bearish bias
        double wma05HighHtf = resultHtf.candle.CandleData!.Wma05High!.Value;
        double ema50Htf = resultHtf.candle.CandleData!.Ema50!.Value;
        double midBbHtf = resultHtf.candle.CandleData!.Sma20!.Value;
        if (ema50Htf <= midBbHtf || wma05HighHtf >= midBbHtf)
        {
            ExtraText = $"HTF EMA50 not above mid-BB — bullish bias, no Short";
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
        //   PDF alert RMEE → entry code RMR  (TF2=MLV, from MagicExtreme alert)
        code = TfStateCode(stateHtf) + TfStateCode(stateMtf) + TfStateCode(stateLtf);
        if (code == "RRE" || code == "REM" || code == "REE" || code == "RMEE")
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
