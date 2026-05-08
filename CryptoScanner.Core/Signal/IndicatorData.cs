using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Core.Signal;

public class CryptoIndicatorData
{
    public required CryptoCandle LastCandle;
    public required CryptoData LastCandleData;

    public required CryptoCandleList CandleList;
    public Dictionary<CandleTime, CryptoData> Data = [];

    public bool TryGetCandle(CandleTime time, out MyData? myData)
    {
        if (CandleList.TryGetValue(time, out CryptoCandle candle) &&
            Data.TryGetValue(time, out CryptoData? indicator))
        {
            myData = new()
            {
                Candle = candle!,
                CandleData = indicator!
            };
            return true;
        }
        else
        {
            myData = null;
            return false;
        }
    }
}


public class CryptoIndicatorDataList : Dictionary<CryptoIntervalPeriod, CryptoIndicatorData?>
{
    /// <summary>
    /// Calculate all the indicators
    /// </summary>
    public (bool success, CryptoSymbolInterval higherInterval, MyData? candle, CryptoIndicatorData? indicatorData)
        CalculateIndicatorsForInterval(
            CryptoSymbol symbol, CryptoInterval interval,
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
                return (false, symbolHigherInterval, null, null);
            targetStart = result.targetStart;
        }

        // Calculate the indicators if needed
        if (!PrepareIndicators(symbol, higherInterval, targetStart))
            return (false, symbolHigherInterval, null, null);

        // Get the candle in the higher interval (combination of candle and indicator data)
        if (!TryGetCandle(higherInterval, targetStart, out MyData? higherCandle))
            return (false, symbolHigherInterval, null, null);

        // We need a reference to the indicatorData for some methods
        if (!TryGetValue(higherInterval.IntervalPeriod, out CryptoIndicatorData? indicatorData))
            return (false, symbolHigherInterval, null, null);

        return (true, symbolHigherInterval, higherCandle, indicatorData);
    }


    public bool PrepareIndicators(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime candleOpenTime, int calculateCandles = -1)
    {
        if (!TryGetValue(interval.IntervalPeriod, out CryptoIndicatorData? _))
        {
            List<CryptoCandle>? History = CollectCandles(symbol, interval, candleOpenTime, out string response, calculateCandles);
            if (History == null)
            {
                //GlobalData.AddTextToLogTab($"Analyse {response} {symbol.Name} Candle {interval.Name} {candleOpenTime.ToDateTime().ToLocalTime()} not calculated? {response}");
                TryAdd(interval.IntervalPeriod, null);
                return false;
            }

            CryptoIndicatorData? indicatorData = CalculateIndicators(symbol, interval, History, calculateCandles);
            TryAdd(interval.IntervalPeriod, indicatorData);
        }
        return true;
    }

    // Get the candle and indicator data from a DIFFERENT interval
    public bool TryGetCandle(CryptoInterval interval, CandleTime time, out MyData? myData)
    {
        // indicatorData can be null: PrepareIndicators stores null when candle collection fails (line 87).
        // TryGetValue returns true (key exists) but the value is null — guard explicitly.
        if (TryGetValue(interval.IntervalPeriod, out CryptoIndicatorData? indicatorData) && indicatorData != null)
        {
            return indicatorData.TryGetCandle(time, out myData);
        }
        myData = null;
        return false;
    }

    // For the SMA 200 we want at least 200 + 60 (we calculate the last 60 entries)
    //private const int maxCandles = 260;

    /// <summary>
    /// Make a list of candles up to openTime with at least maxCandles(260) candles
    /// </summary>
    public static List<CryptoCandle>? CollectCandles(
        CryptoSymbol symbol, CryptoInterval interval,
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

        List<CryptoCandle> candlesForHistory = [];

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
                CryptoCandle x = candlesForHistory.Last();
                errorstr += " last in history = " + x.DateLocal.ToString();

                x = intervalCandles.Values.Last();
                errorstr += " last in candlelist = " + x.DateLocal.ToString();
            }
            return null;
        }

        errorstr = "";
        return candlesForHistory;
    }


    /// <summary>
    /// Calculate all the indicators, we want to have data for the last 60 candles
    /// </summary>
    private static CryptoIndicatorData? CalculateIndicators(CryptoSymbol symbol,
        CryptoInterval interval, List<CryptoCandle> history, int calculateCandles = -1)
    {
        CryptoCandle candle = history[^1];
        CryptoIndicatorData? indicatorData = null;
        int fillMax = calculateCandles > 0 ? calculateCandles : 61;

        List<TemaResult> temaList = (List<TemaResult>)history.GetTema(9);
        List<EmaResult> emaList9 = (List<EmaResult>)history.GetEma(9);
#if EXTRASTRATEGIES
        List<EmaResult> emaList5 = (List<EmaResult>)history.GetEma(5);
        //List<EmaResult> emaList8 = (List<EmaResult>)history.GetEma(8);
        List<EmaResult> emaList26 = (List<EmaResult>)history.GetEma(26);
        List<EmaResult> emaList50 = (List<EmaResult>)history.GetEma(50);
        //List<EmaResult> emaList100 = (List<EmaResult>)history.GetEma(100);
        //List<EmaResult> emaList200 = (List<EmaResult>)history.GetEma(200);
#endif
#if EXTRASTRATEGIESSLOPEEMA
        List<EmaResult> emaList20 = (List<EmaResult>)history.GetEma(20);
        List<SlopeResult> slopeEma20List = (List<SlopeResult>)emaList20.GetSlope(SlopeCount);
        List<SlopeResult> slopeEma50List = (List<SlopeResult>)emaList50.GetSlope(SlopeCount);
#endif

        // https://dotnet.stockindicators.dev/utilities/#content
        // Weighted Moving Average is the linear weighted average of price over a lookback window.
        // This also called Linear Weighted Moving Average(LWMA).
        // https://dotnet.stockindicators.dev/indicators/Wma/#content
        List<EmaResult> emaList50 = (List<EmaResult>)history.GetEma(50);
        List<WmaResult> wmaList05Low = (List<WmaResult>)history.Use(CandlePart.Low).GetWma(05);
        List<WmaResult> wmaList05High = (List<WmaResult>)history.Use(CandlePart.High).GetWma(05);
        List<WmaResult> wmaList10Low = (List<WmaResult>)history.Use(CandlePart.Low).GetWma(10);
        List<WmaResult> wmaList10High = (List<WmaResult>)history.Use(CandlePart.High).GetWma(10);

        // or collect items first (is this faster/better?), a lot more coding)
        //List<CryptoCandle> historyLast05 = (List<CryptoCandle>)history.TakeLast(05);
        //List<WmaResult> wmaList05Low = (List<WmaResult>)historyLast05.Use(CandlePart.Low).GetWma(05);
        //List<WmaResult> wmaList05High = (List<WmaResult>)historyLast05.Use(CandlePart.High).GetWma(05);
        //List<CryptoCandle> historyLast10 = (List<CryptoCandle>)history.TakeLast(10);
        //List<WmaResult> wmaList10Low = (List<WmaResult>)historyLast10.Use(CandlePart.Low).GetWma(10);
        //List<WmaResult> wmaList10High = (List<WmaResult>)historyLast10.Use(CandlePart.High).GetWma(10);

        //List<SmaResult> smaList08 = (List<SmaResult>)history.GetSma(08);
        List<SmaResult> smaList20 = (List<SmaResult>)history.GetSma(20);
        List<SmaResult> smaList50 = (List<SmaResult>)history.GetSma(50);
        List<SmaResult> smaList100 = (List<SmaResult>)history.GetSma(100);
        List<SmaResult> smaList200 = (List<SmaResult>)history.GetSma(200);

        //// GetSlope looks buggy? (specially with sma(200) and count <> 200)
        //List<SlopeResult>? slopeSma20List = null;
        //List<SlopeResult>? slopeSma50List = null;
        //List<SlopeResult>? slopeSma100List = null;
        //List<SlopeResult>? slopeSma200List = null;
        //try
        //{
        //    slopeSma20List = (List<SlopeResult>)smaList20.GetSlope(SlopeCount);
        //    slopeSma50List = (List<SlopeResult>)smaList50.GetSlope(SlopeCount);
        //    slopeSma100List = (List<SlopeResult>)smaList100.GetSlope(SlopeCount);
        //    slopeSma200List = (List<SlopeResult>)smaList200.GetSlope(SlopeCount);
        //}
        //catch (Exception)
        //{
        //    //ignore
        //}


        //List<WmaResult> wmaList30 = (List<WmaResult>)history.GetWma(30);

#if DEBUG
        // Berekend vanuit de EMA 20 en de upper en lowerband ontstaat uit 2x de ATR
        List<KeltnerResult> keltnerList = (List<KeltnerResult>)Skender.Stock.Indicators.Indicator.GetKeltner(history, 20, 1);
#endif

        //List<AtrResult> atrList = (List<AtrResult>)Indicator.GetAtr(History);
        List<RsiResult> rsiList = (List<RsiResult>)history.GetRsi(
            lookbackPeriods: GlobalData.Settings.General.SettingsRsi.Length);
        List<MacdResult> macdList = (List<MacdResult>)history.GetMacd();

        // GaussianScalp strategy: RSI(30) and MACD(24/52/9)
        List<RsiResult> rsiList30 = (List<RsiResult>)history.GetRsi(lookbackPeriods: 30);
        List<MacdResult> macdList24 = (List<MacdResult>)history.GetMacd(fastPeriods: 24, slowPeriods: 52, signalPeriods: 9);
        //List<SlopeResult> slopeMacdList = (List<SlopeResult>)macdList.GetSlope(SlopeCount);
        //List<VwapResult> vwapList = (List<VwapResult>)History.GetVwap();
#if EXTRASTRATEGIES
        List<MacdResult> macdLtList = (List<MacdResult>)history.GetMacd(34, 144);
#endif

        //List<SlopeResult> slopeRsiList = (List<SlopeResult>)rsiList.GetSlope(SlopeCount);

        // (volgens de telegram groepen op 14,3,1 ipv de standaard 14,3,3)
        List<StochResult> stochList = (List<StochResult>)history.GetStoch(
            lookbackPeriods: GlobalData.Settings.General.SettingsStoch.Length,
            signalPeriods: GlobalData.Settings.General.SettingsStoch.SmoothingD,
            smoothPeriods: GlobalData.Settings.General.SettingsStoch.SmoothingK);
        //14, 3, 1); // 18-11-22: omgedraaid naar 1, 3...
        //List<SlopeResult> slopeStochList = (List<SlopeResult>)stochList.GetSlope(SlopeCount);

        List<ParabolicSarResult> psarList = (List<ParabolicSarResult>)history.GetParabolicSar();

        // dan kan nu ook met de stdDev * setting.... Maar komt het wel overeen?
        List<BollingerBandsResult> bollingerBandsList = (List<BollingerBandsResult>)history.GetBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length,
            standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);

        //AccountSymbolData symbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
        //AccountSymbolIntervalData symbolIntervalData = symbolData.GetSymbolData(interval.IntervalPeriod);

        // Fill the last 60 candles with the indicator data
        int iteration = 0;
        for (int index = history.Count - 1; index >= 0; index--)
        {
            // Maximaal 60 records aanvullen
            iteration++;
            if (iteration > fillMax)
                break;


            candle = history[index];

            CryptoData candleData = new();
            try
            {
                // EMA's
#if EXTRASTRATEGIES
                ////candleData.Ema8 = emaList8[index].Ema;
                candleData.Ema26 = emaList26[index].Ema;
                candleData.Ema20 = emaList20[index].Ema;
                candleData.Ema50 = emaList50[index].Ema; --> see wma / bbma
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

                candleData.Ema50 = emaList50[index].Ema;
                candleData.Wma05Low = wmaList05Low[index].Wma;
                candleData.Wma05High = wmaList05High[index].Wma;
                candleData.Wma10Low = wmaList10Low[index].Wma;
                candleData.Wma10High = wmaList10High[index].Wma;

#if DEBUG
                candleData.KeltnerUpperBand = keltnerList[index].UpperBand;
                candleData.KeltnerCenterLine = keltnerList[index].Centerline;
                candleData.KeltnerLowerBand = keltnerList[index].LowerBand;
#endif


                candleData.Rsi = rsiList[index].Rsi;
                //if (slopeRsiList != null && index < slopeRsiList.Count)
                //    candleData.SlopeRsi = slopeRsiList[index].Slope;

                candleData.MacdValue = macdList[index].Macd;
                candleData.MacdSignal = macdList[index].Signal;
                candleData.MacdHistogram = macdList[index].Histogram;

                // GaussianScalp indicators
                candleData.Rsi30 = rsiList30[index].Rsi;
                candleData.MacdValue24 = macdList24[index].Macd;
                candleData.MacdSignal24 = macdList24[index].Signal;
                candleData.MacdHistogram24 = macdList24[index].Histogram;
                //candleData.SlopeMacd = slopeMacdList[index].Slope;

#if DEBUG
                // Test
                candleData.Ema9 = emaList9[index].Ema;
                candleData.Tema = temaList[index].Tema;
                //candleData.Wma30 = wmaList30[index].Wma;
                //candleData.Vwap = vwapList[index].Vwap;
#endif

#if EXTRASTRATEGIES
                //candleData.MacdLtValue = macdLtList[index].Macd;
                //candleData.MacdLtSignal = macdLtList[index].Signal;
                candleData.MacdTestHistogram = macdLtList[index].Histogram;
#endif

                candleData.StochSignal = stochList[index].Signal;
                candleData.StochOscillator = stochList[index].Oscillator;
                //candleData.SlopeStoch = slopeStochList[index].Slope;

                double? BollingerBandsLowerBand = bollingerBandsList[index].LowerBand;
                double? BollingerBandsUpperBand = bollingerBandsList[index].UpperBand;
                candleData.BollingerBandsDeviation = 0.5 * (BollingerBandsUpperBand - BollingerBandsLowerBand);
                candleData.BollingerBandsPercentage = 100 * (BollingerBandsUpperBand / BollingerBandsLowerBand - 1);

                if (psarList[index].Sar != null)
                    candleData.PSar = psarList[index].Sar;

                indicatorData ??= new()
                {
                    LastCandle = candle,
                    LastCandleData = candleData,
                    CandleList = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList,
                };
                indicatorData.Data.Add(candle.OpenTime, candleData);
                //candle.CandleData = candleData; // deprecated, but for now..
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


        //// I use the lux indicator frequently and combine its results in a single value
        //CryptoCandle? lastCandle = history[^1];
        //LuxIndicator.Calculate(symbol, out int luxOverSold, out int luxOverBought, CryptoIntervalPeriod.interval5m, lastCandle!.OpenTime + interval.Duration);

        //int luxValue = 0;
        //if (luxOverBought > 0)
        //    luxValue += luxOverBought;
        //if (luxOverSold > 0)
        //    luxValue -= luxOverSold;
        //lastCandle!.CandleData!.Lux5mValue = luxValue;
        return indicatorData;
    }
}


