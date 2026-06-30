using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

using System.Diagnostics;

namespace CryptoScanner.Core.Signal;

public static class IndicatorEngine
{
    // How many candles of CryptoData to keep per symbol+interval (≈ the old 260 calculation window).
    private const int CacheCandles = 300;

    /// <summary>
    /// Ensures indicator data exists for <paramref name="openTime"/> in the requested higher interval and
    /// returns it as a MyData (candle + indicator data). Used for the multi-timeframe (MTF/HTF) strategies.
    /// </summary>
    public static (bool success, CryptoSymbolInterval higherInterval, MyData? candle)
        CalculateIndicatorsForInterval(CryptoSymbol symbol, CryptoInterval interval,
            CandleTime openTime, CryptoIntervalPeriod higherIntervalPeriod)
    {
        CryptoSymbolInterval symbolHigherInterval = symbol.GetSymbolInterval(higherIntervalPeriod);
        CryptoInterval higherInterval = symbolHigherInterval.Interval;
        CandleTime targetStart = openTime;

        if (interval.IntervalPeriod != higherIntervalPeriod)
        {
            // Check the candle in the higher interval, is it present?
            var result = IntervalTools.StartOfIntervalCandle3(openTime, interval.Duration, higherInterval.Duration);
            if (!result.targetComplete)
                result.targetStart -= higherInterval.Duration;
            if (!symbolHigherInterval.CandleList.TryGetValue(result.targetStart, out CryptoCandle _))
                return (false, symbolHigherInterval, null);
            targetStart = result.targetStart;
        }

        // Calculate the indicators if needed
        if (!PrepareIndicators(symbol, higherInterval, targetStart))
            return (false, symbolHigherInterval, null);

        // Combine candle + indicator data from the (persistent) higher-interval state.
        if (!symbolHigherInterval.TryGetCandle(targetStart, out MyData? higherCandle))
            return (false, symbolHigherInterval, null);

        return (true, symbolHigherInterval, higherCandle);
    }


    /// <summary>
    /// Ensures <see cref="CryptoSymbolInterval.Data"/> holds the indicator data for
    /// <paramref name="candleOpenTime"/>. Filled either incrementally via the per-interval QuoteHub
    /// (UseIndicatorHub) or via the per-candle batch — both produce identical CryptoData. Returns false
    /// when there is not enough history yet.
    /// </summary>
    public static bool PrepareIndicators(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime candleOpenTime, int calculateCandles = -1)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.Data.ContainsKey(candleOpenTime))
            return true;

        if (GlobalData.Settings.Signal.UseIndicatorHub)
            return PrepareViaHub(symbol, interval, symbolInterval, candleOpenTime);
        return PrepareViaBatch(symbol, interval, symbolInterval, candleOpenTime, calculateCandles);
    }


    /// <summary>
    /// Incremental path: feed candles into the per-interval <see cref="IntervalIndicatorHub"/>. A full
    /// warm-up (the whole CollectCandles window) runs on first use or when the hub fell out of sync (a gap);
    /// otherwise only the single new candle is fed. The latest CryptoData lands in Data[candleOpenTime].
    /// </summary>
    private static bool PrepareViaHub(CryptoSymbol symbol, CryptoInterval interval,
        CryptoSymbolInterval symbolInterval, CandleTime candleOpenTime)
    {
        bool warmup = symbolInterval.IndicatorHub == null
            || symbolInterval.IndicatorHubLastAdded == null
            || symbolInterval.IndicatorHubLastAdded.Value + interval.Duration != candleOpenTime;

        if (warmup)
        {
            long profCollectStart = Stopwatch.GetTimestamp();
            List<IQuote>? history = CollectCandles(symbol, interval, candleOpenTime, out _);
            PipelineProfiler.RecordPrepCollect(Stopwatch.GetTimestamp() - profCollectStart);
            if (history == null)
                return false;

            var hub = new IntervalIndicatorHub();
            foreach (IQuote quote in history)
            {
                hub.Add(quote);
                if (quote is CryptoCandle candle)
                    lock (symbolInterval.Data)
                        symbolInterval.Data[candle.OpenTime] = hub.BuildCurrent();
            }
            symbolInterval.IndicatorHub = hub;
            symbolInterval.IndicatorHubLastAdded = candleOpenTime;
        }
        else
        {
            if (!symbolInterval.CandleList.TryGetValue(candleOpenTime, out CryptoCandle candle))
                return false;
            symbolInterval.IndicatorHub!.Add(candle);
            lock (symbolInterval.Data)
                symbolInterval.Data[candleOpenTime] = symbolInterval.IndicatorHub.BuildCurrent();
            symbolInterval.IndicatorHubLastAdded = candleOpenTime;
        }

        ApplyLux(symbol, symbolInterval, candleOpenTime);
        return true;
    }

    // For the SMA 200 we want at least 200 + 60 (we calculate the last 60 entries)
    //private const int maxCandles = 260;

    /// <summary>
    /// Make a list of candles up to openTime with at least maxCandles(260) candles
    /// </summary>
    public static List<IQuote>? CollectCandles(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime openTime, out string errorstr, int calculateCandles = -1)
    {
        // Retrieve the last candle in the requested interval
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList intervalCandles = symbolPeriod.CandleList;

        int maxCandles = calculateCandles > 0 ? calculateCandles : 260;
        if (intervalCandles.Count < maxCandles || intervalCandles.Count < 260)
        {
            errorstr = $"{symbol.Name} Not enough candles available for interval {interval.Name} count={intervalCandles.Count} requested={maxCandles}";
            return null;
        }

        // this would normally be enough, but we need to fill in the missing candles (afaics)
        //var x = intervalCandles.Values.TakeLast(maxCandles);

        // A fix for calculating indicators for the barometer symbol..
        uint duration = interval.Duration;
        if (symbol.IsBarometerSymbol())
            duration = 1; // alway's 1m!

        List<IQuote> candlesForHistory = [];

        // The time is already aligned (but it wont hurt to do it again)
        CandleTime periodEndTime = openTime - openTime % duration;
        CandleTime periodStartTime = periodEndTime - (maxCandles - 1) * duration;

        CryptoCandle candleLast = default;
        CandleTime candleLoop = periodStartTime;
        while (candleLoop <= periodEndTime)
        {
            if (intervalCandles.TryGetValue(candleLoop, out CryptoCandle candle))
            {
                candlesForHistory.Add(candle);
            }
            else
            {
                // Generate a dummy for the calculation
                if (candleLast.OpenTime != 0)
                {
                    candle = new()
                    {
                        TickDecimals = symbol.PriceDecimals,
                        OpenTime = candleLoop,
                        Open = candleLast.Close,
                        Low = candleLast.Close,
                        High = candleLast.Close,
                        Close = candleLast.Close,
                        Volume = 0,
                    };
                    candlesForHistory.Add(candle);
                }
                //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Missing candle information (recreated) " + CandleTools.GetUnixDate(candleLoop.ToDateTime()).ToLocalTime());
            }

            candleLoop += duration;
            candleLast = candle;
        }


        // Its still possible we dont have enough candles
        if (candlesForHistory.Count < maxCandles)
        {
            errorstr = $"{symbol.Name} Not enough candles available for interval {interval.Name} count={candlesForHistory.Count} requested={maxCandles}";
            if (candlesForHistory.Count != 0)
            {
                var x = candlesForHistory[^1]; //.Last();
                errorstr += " last in history = " + x.Timestamp.ToLocalTime().ToString();

                x = intervalCandles.Values.Last();
                errorstr += " last in candlelist = " + x.Timestamp.ToLocalTime().ToString();
            }
            return null;
        }

        errorstr = "";
        return candlesForHistory;
    }


    /// <summary>
    /// Calculate all the indicators, we want to have data for the last 60 candles
    /// </summary>
    /// <summary>
    /// Batch path: collect the candle window and (re)compute every indicator with the Skender batch calls,
    /// writing one CryptoData per candle into <paramref name="symbolInterval"/>.Data. Field-for-field
    /// identical to the hub path (UseIndicatorHub).
    /// </summary>
    private static bool PrepareViaBatch(CryptoSymbol symbol, CryptoInterval interval,
        CryptoSymbolInterval symbolInterval, CandleTime candleOpenTime, int calculateCandles = -1)
    {
        long profCollectStart = Stopwatch.GetTimestamp();
        List<IQuote>? quotes = CollectCandles(symbol, interval, candleOpenTime, out _, calculateCandles);
        PipelineProfiler.RecordPrepCollect(Stopwatch.GetTimestamp() - profCollectStart);
        if (quotes == null)
            return false;

        var candle = quotes[^1];

        // Profiling: start of the Skender batch-calculation block (see PipelineProfiler).
        long profSkenderStart = Stopwatch.GetTimestamp();

        //IReadOnlyList<TemaResult> temaList = quotes.ToTema(9);
        //IReadOnlyList<EmaResult> emaList9 = quotes.ToEma(9);
#if EXTRASTRATEGIES
        IReadOnlyList<EmaResult> emaList5 = quotes.ToEma(5);
        //IReadOnlyList<EmaResult> emaList8 = quotes.ToEma(8);
        IReadOnlyList<EmaResult> emaList26 = quotes.ToEma(26);
        //IReadOnlyList<EmaResult> emaList100 = quotes.ToEma(100);
        //IReadOnlyList<EmaResult> emaList200 = quotes.ToEma(200);
#endif

#if DEBUG
        // EMA 20 / 50 — required by the trend filter and several strategies (was conditional, now standard).
        IReadOnlyList<EmaResult> emaList20 = quotes.ToEma(20);
#endif

#if EXTRASTRATEGIESSLOPEEMA
        IReadOnlyList<SlopeResult> slopeEma20List = emaList20.GetSlope(SlopeCount);
        IReadOnlyList<SlopeResult> slopeEma50List = emaList50.GetSlope(SlopeCount);
#endif

#if DEBUG
        // Linear Weighted Moving Average — used by BBMA experiments.
        // https://dotnet.stockindicators.dev/indicators/Wma/#content
        IReadOnlyList<EmaResult> emaList50 = quotes.ToEma(50);
        IReadOnlyList<WmaResult> wmaList05Low = quotes.Use(CandlePart.Low).ToWma(05);
        IReadOnlyList<WmaResult> wmaList05High = quotes.Use(CandlePart.High).ToWma(05);
        IReadOnlyList<WmaResult> wmaList10Low = quotes.Use(CandlePart.Low).ToWma(10);
        IReadOnlyList<WmaResult> wmaList10High = quotes.Use(CandlePart.High).ToWma(10);
        // ATR(14) — BBMA Omni: RejectedEMA50 big-body filter, MHV gap sizing.
        IReadOnlyList<AtrResult> atrList14 = quotes.ToAtr(14);
#endif

        //IReadOnlyList<SmaResult> smaList08 = quotes.GetSma(08);
        IReadOnlyList<SmaResult> smaList20 = quotes.ToSma(20);
        IReadOnlyList<SmaResult> smaList50 = quotes.ToSma(50);
        IReadOnlyList<SmaResult> smaList100 = quotes.ToSma(100);
        IReadOnlyList<SmaResult> smaList200 = quotes.ToSma(200);

        //// GetSlope looks buggy? (specially with sma(200) and count <> 200)
        //List<SlopeResult>? slopeSma20List = null;
        //List<SlopeResult>? slopeSma50List = null;
        //List<SlopeResult>? slopeSma100List = null;
        //List<SlopeResult>? slopeSma200List = null;
        //try
        //{
        //    slopeSma20List = (IReadOnlyList<SlopeResult>)smaList20.GetSlope(SlopeCount);
        //    slopeSma50List = (IReadOnlyList<SlopeResult>)smaList50.GetSlope(SlopeCount);
        //    slopeSma100List = (IReadOnlyList<SlopeResult>)smaList100.GetSlope(SlopeCount);
        //    slopeSma200List = (IReadOnlyList<SlopeResult>)smaList200.GetSlope(SlopeCount);
        //}
        //catch (Exception)
        //{
        //    //ignore
        //}


        //IReadOnlyList<WmaResult> wmaList30 = quotes.GetWma(30);

#if DEBUG
        // Keltner Channel: EMA20 centerline +/- ATR(10) * 2 (Skender defaults). Used by
        // the TTM Squeeze family (BB inside KC = squeeze). Matches the chart drawer.
        //IReadOnlyList<KeltnerResult> keltnerList = quotes.GetKeltner();
#endif

        //IReadOnlyList<AtrResult> atrList = Indicator.GetAtr(History);
        IReadOnlyList<RsiResult> rsiList = quotes.ToRsi(
            lookbackPeriods: GlobalData.Settings.General.SettingsRsi.Length);
        IReadOnlyList<MacdResult> macdList = quotes.ToMacd();

        //IReadOnlyList<SlopeResult> slopeMacdList = macdList.GetSlope(SlopeCount);
        //IReadOnlyList<VwapResult> vwapList = History.GetVwap();
        //#if EXTRASTRATEGIES
        //        IReadOnlyList<MacdResult> macdLtList = quotes.GetMacd(34, 144);
        //#endif

        //IReadOnlyList<SlopeResult> slopeRsiList = rsiList.GetSlope(SlopeCount);

        // (volgens de telegram groepen op 14,3,1 ipv de standaard 14,3,3)
        IReadOnlyList<StochResult> stochList = quotes.ToStoch(
            lookbackPeriods: GlobalData.Settings.General.SettingsStoch.Length,
            signalPeriods: GlobalData.Settings.General.SettingsStoch.SmoothingD,
            smoothPeriods: GlobalData.Settings.General.SettingsStoch.SmoothingK);
        //14, 3, 1); // 18-11-22: omgedraaid naar 1, 3...
        //IReadOnlyList<SlopeResult> slopeStochList = stochList.GetSlope(SlopeCount);

        IReadOnlyList<ParabolicSarResult> psarList = quotes.ToParabolicSar();

        // dan kan nu ook met de stdDev * setting.... Maar komt het wel overeen?
        IReadOnlyList<BollingerBandsResult> bollingerBandsList = quotes.ToBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length,
            standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);

        // Baba VWAP bands — same BabaBandsHelper.ComputeBands the chart and IntervalIndicatorHub use, so
        // the batch path and the hub path (UseIndicatorHub) agree field-for-field.
        var baba = GlobalData.Settings.Signal.Baba;
        BabaBandsHelper.BandValue[] babaBands = BabaBandsHelper.ComputeBands(quotes.Cast<CryptoCandle>().ToList());
        IReadOnlyList<AtrResult> atrBabaFastList = quotes.ToAtr(baba.AtrLength);
        IReadOnlyList<AtrResult> atrBabaSlList = quotes.ToAtr(baba.Length);

        //AccountSymbolData symbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
        //AccountSymbolIntervalData symbolIntervalData = symbolData.GetSymbolData(interval.IntervalPeriod);

        // Profiling: end of the Skender batch block, start of the per-candle fill loop.
        long profFillStart = Stopwatch.GetTimestamp();

        // Fill the last 60 candles with the indicator data
        int iteration = 0;
        for (int index = quotes.Count - 1; index >= 0; index--)
        {
            // Maximaal 60 records aanvullen
            iteration++;
            candle = quotes[index];

            CryptoData candleData = new();
            try
            {
                // EMA's
#if EXTRASTRATEGIES
                //candleData.Ema5 = emaList5[index].Ema;
                //candleData.Ema8 = emaList8[index].Ema;
                //candleData.Ema20 = emaList20[index].Ema;
                candleData.Ema26 = emaList26[index].Ema;
                //candleData.Ema100 = emaList100[index].Ema;
                //candleData.Ema200 = emaList200[index].Ema;
#endif
#if EXTRASTRATEGIESSLOPEEMA
                candleData.SlopeEma20 = slopeEma20List[index].Slope;
                candleData.SlopeEma50 = slopeEma50List[index].Slope;
#endif


                // SMA's
                //candleData.Sma8 = smaList8[index].Sma;
                candleData.Sma20 = bollingerBandsList[index].Sma;
                candleData.Sma50 = smaList50[index].Sma;
                candleData.Sma100 = smaList100[index].Sma;
                candleData.Sma200 = smaList200[index].Sma;

                //if (slopeSma20List != null && index < slopeSma20List.Count)
                //    candleData.SlopeSma20 = slopeSma20List[index].Slope;
                //if (slopeSma50List != null && index < slopeSma50List.Count)
                //    candleData.SlopeSma50 = slopeSma50List[index].Slope;
                //if (slopeSma100List != null && index < slopeSma100List.Count)
                //    candleData.SlopeSma100 = slopeSma100List[index].Slope;
                //if (slopeSma200List != null && index < slopeSma200List.Count)
                //    candleData.SlopeSma200 = slopeSma200List[index].Slope;

#if DEBUG
                candleData.Ema50 = emaList50[index].Ema;
                candleData.Wma05Low = wmaList05Low[index].Wma;
                candleData.Wma05High = wmaList05High[index].Wma;
                candleData.Wma10Low = wmaList10Low[index].Wma;
                candleData.Wma10High = wmaList10High[index].Wma;
                candleData.Atr14 = atrList14[index].Atr;
#endif

#if DEBUG
                //candleData.KeltnerUpperBand = keltnerList[index].UpperBand;
                //candleData.KeltnerCenterLine = keltnerList[index].Centerline;
                //candleData.KeltnerLowerBand = keltnerList[index].LowerBand;
#endif


                candleData.Rsi = rsiList[index].Rsi;
                //if (slopeRsiList != null && index < slopeRsiList.Count)
                //    candleData.SlopeRsi = slopeRsiList[index].Slope;

                candleData.MacdValue = macdList[index].Macd;
                candleData.MacdSignal = macdList[index].Signal;
                candleData.MacdHistogram = macdList[index].Histogram;

                //#if DEBUG
                //                // Test
                //                //candleData.Ema9 = emaList9[index].Ema;
                //                //candleData.Tema = temaList[index].Tema;
                //                //candleData.Wma30 = wmaList30[index].Wma;
                //                //candleData.Vwap = vwapList[index].Vwap;
                //#endif

                //#if EXTRASTRATEGIES
                //                //candleData.MacdLtValue = macdLtList[index].Macd;
                //                //candleData.MacdLtSignal = macdLtList[index].Signal;
                //                candleData.MacdTestHistogram = macdLtList[index].Histogram;
                //#endif

                candleData.StochSignal = stochList[index].Signal;
                candleData.StochOscillator = stochList[index].Oscillator;
                //candleData.SlopeStoch = slopeStochList[index].Slope;

                double? BollingerBandsLowerBand = bollingerBandsList[index].LowerBand;
                double? BollingerBandsUpperBand = bollingerBandsList[index].UpperBand;
                candleData.BollingerBandsDeviation = 0.5 * (BollingerBandsUpperBand - BollingerBandsLowerBand);
                candleData.BollingerBandsPercentage = 100 * (BollingerBandsUpperBand / BollingerBandsLowerBand - 1);

                if (psarList[index].Sar != null)
                    candleData.PSar = psarList[index].Sar;

                if (babaBands[index].HasValue)
                {
                    candleData.BabaBasis = babaBands[index].Basis;
                    candleData.BabaUpper = babaBands[index].Upper;
                    candleData.BabaLower = babaBands[index].Lower;
                }
                candleData.AtrBaba = atrBabaFastList[index].Atr;
                candleData.BabaAtrSl = atrBabaSlList[index].Atr;

                if (candle is CryptoCandle x)
                    lock (symbolInterval.Data)
                        symbolInterval.Data[x.OpenTime] = candleData;
            }
            catch (Exception error)
            {
                // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab("error indicators");
                GlobalData.AddTextToLogTab(error.ToString());
                GlobalData.AddTextToLogTab("");
                //GlobalData.AddTextToLogTab(History.ToString());
                throw;
            }

        }


        // Profiling: end of the fill loop, start of the Lux calculation.
        long profLuxStart = Stopwatch.GetTimestamp();

        // Lux indicator (non-Skender) for the latest candle, same as before.
        ApplyLux(symbol, symbolInterval, candleOpenTime);

        // Profiling: attribute the three sub-buckets of this method to the profiler (thread-safe).
        PipelineProfiler.RecordIndicatorPhases(
            skender: profFillStart - profSkenderStart,
            fill: profLuxStart - profFillStart,
            lux: Stopwatch.GetTimestamp() - profLuxStart);

        return true;
    }


    /// <summary>
    /// Applies the Lux 5m value to the CryptoData of the latest candle (the non-Skender, recursive indicator
    /// that the hub does not produce). Mirrors the original tail of CalculateIndicators.
    /// </summary>
    private static void ApplyLux(CryptoSymbol symbol, CryptoSymbolInterval symbolInterval, CandleTime candleOpenTime)
    {
        if (!symbolInterval.Data.TryGetValue(candleOpenTime, out CryptoData? data))
            return;

        LuxIndicator.Calculate(symbol, out int luxOverSold, out int luxOverBought,
            CryptoIntervalPeriod.interval5m, candleOpenTime + 5);

        int luxValue = 0;
        if (luxOverBought > 0)
            luxValue += luxOverBought;
        if (luxOverSold > 0)
            luxValue -= luxOverSold;
        data.Lux5mValue = (short)luxValue;
    }

}