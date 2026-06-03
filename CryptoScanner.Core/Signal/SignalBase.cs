using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal;


// Het draait allemaal om de status van het algoritme
// (het algoritme zet die status zelf alsmede delay enz.):
// -None, candle aanbieden voor signaal detectie
// -WarmingUp (voor de indicators)
// -Delaying: Een (optionele) delay
// -TryStepIn: Na een OK van het algoritme om in te stappen

public class MyData
{
    public required CryptoCandle Candle { get; set; }
    public required CryptoData CandleData { get; set; }
}

public class SignalCreateBase
{
    // RegisterAlgorithms.GetAlgorithm
    public required CryptoSymbol Symbol { get; set; }
    public required CryptoInterval Interval { get; set; }
    public required CryptoSymbolInterval SymbolInterval { get; set; }

    // The requested strategy and side
    public required CryptoTradeSide SignalSide { get; set; }
    public required CryptoSignalStrategy SignalStrategy { get; set; }

    // The requested candle and its indicator data (grouped)
    public required MyData CandleLast { get; set; }

    // Prepared indicator data
    public required CryptoIndicatorData IndicatorData { get; set; }
    public required CryptoIndicatorDataList IndicatorDataList { get; set; }

    public string ExtraText = "";

    /// <summary>
    /// Zijn de indicatoren aanwezig
    /// </summary>
    public virtual bool IndicatorsOkay(MyData data) => data.Candle.OpenTime != 0 && data.CandleData != null;

    /// <summary>
    /// Is het een signaal?
    /// </summary>
    public virtual bool IsSignal() => false;


    /// <summary>
    /// Optional override for the price stored on the signal. Return null to use
    /// the default (last candle close). Use this when the signal references an
    /// earlier candle than CandleLast — for example BOS/CHoCH breaks, which
    /// happen at a swing pivot, not at the candle the check is running on.
    /// </summary>
    public virtual decimal? OverrideSignalPrice => null;

    /// <summary>
    /// Optional per-signal stop-loss price. When non-null the trader uses this value
    /// instead of the default percentage-based SL from Settings.Trading. Strategies that
    /// anchor their SL on a structural level (swing high/low, BB band, etc.) populate this.
    /// </summary>
    public virtual decimal? OverrideSlPrice => null;

    /// <summary>
    /// Optional per-signal take-profit price. When non-null the trader uses this value
    /// instead of the default percentage-based TP. Typically derived from <see cref="OverrideSlPrice"/>
    /// via a risk:reward multiple.
    /// </summary>
    public virtual decimal? OverrideTpPrice => null;


    public virtual bool AdditionalChecks(MyData candle, out string response)
    {
        response = "";
        return true;
    }


    /// <summary>
    /// Give up when the trader fails to pick up the signal within EntryRemoveTime bars
    /// after it fired (for example when no trading slot is free).
    /// </summary>
    public virtual bool GiveUp(CryptoSignal signal)
    {
        // BUGFIX: the previous condition was
        //     signal.CloseDate.Minutes + N * Duration < CandleLast.OpenTime.Minutes
        // which combined two off-by-ones: (a) signal.CloseDate already includes one
        // Duration past signal.OpenDate, and (b) the strict "<" requires another full
        // candle past the threshold. Result: a 15m signal with EntryRemoveTime=5 was
        // only removed 7 candles after signal close.
        //
        // Correct: signal expires once N full candles have elapsed since the signal
        // candle's OPEN time, i.e. CandleLast (the just-closed signal-interval candle)
        // sits at or beyond the N-th candle after signal.OpenDate.
        long expiryTime = CandleTime.FromDateTime(signal.CloseDate).Minutes + GlobalData.Settings.Trading.EntryRemoveTime * signal.Interval.Duration;
        if (CandleLast.Candle.OpenTime.Minutes >= expiryTime)
        {
            ExtraText = $"Stop after {GlobalData.Settings.Trading.EntryRemoveTime} candles";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Extra controles nadat we het accepteren
    /// </summary>
    public virtual bool AllowStepIn(CryptoSignal signal)
    {
        if (!GetPrevCandle(CandleLast!, out MyData? candlePrev) || candlePrev == null)
            return false;

        var settings = GlobalData.Settings.Trading;


        // ********************************************************************
        // Price going into the right direction
        if (settings.CheckFurtherPriceMove)
        {
            switch (SignalSide)
            {
                case CryptoTradeSide.Long:
                    if (CandleLast.Candle.Close < candlePrev!.Candle.Close)
                    {
                        ExtraText = $"Price {candlePrev!.Candle.Close:N8} goes down even more {CandleLast.Candle.Close:N8}";
                        return false;
                    }
                    break;
                case CryptoTradeSide.Short:
                    if (CandleLast.Candle.Close > candlePrev!.Candle.Close)
                    {
                        ExtraText = $"Price {candlePrev!.Candle.Close:N8} goes up even more {CandleLast.Candle.Close:N8}";
                        return false;
                    }
                    break;
            }
        }


        // ********************************************************************
        // MACD recovering
        if (settings.CheckIncreasingMacd)
        {
            int barCount = 1;
            if (SignalStrategy == CryptoSignalStrategy.Sbm1 ||
                SignalStrategy == CryptoSignalStrategy.Sbm2 ||
                SignalStrategy == CryptoSignalStrategy.Sbm3)
                barCount = GlobalData.Settings.Signal.Sbm.CandlesForMacdRecovery;

            switch (SignalSide)
            {
                case CryptoTradeSide.Long:
                    if (!this.IsMacdRecoveryOversold(barCount))
                        return false;
                    break;
                case CryptoTradeSide.Short:
                    if (!this.IsMacdRecoveryOverbought(barCount))
                        return false;
                    break;
            }
        }


        // ********************************************************************
        // RSI recovering
        if (settings.CheckIncreasingRsi)
        {
            switch (SignalSide)
            {
                case CryptoTradeSide.Long:
                    if (CandleLast?.CandleData?.Rsi < candlePrev?.CandleData?.Rsi)
                    {
                        ExtraText = $"Rsi {candlePrev.CandleData.Rsi:N8} not recovering <= {CandleLast.CandleData.Rsi:N8}";
                        return false;
                    }
                    break;
                case CryptoTradeSide.Short:
                    if (CandleLast?.CandleData?.Rsi > candlePrev?.CandleData?.Rsi)
                    {
                        ExtraText = $"Rsi {candlePrev.CandleData.Rsi:N8} not recovering >= {CandleLast.CandleData.Rsi:N8}";
                        return false;
                    }
                    break;
            }
        }

        // ********************************************************************
        // STOCH recovering (Stochastic)
        // Red %D = signal, average from the last 3 %K values
        // Blue %K = Oscilator calculated from the last 14 candles
        if (settings.CheckIncreasingStoch)
        {
            switch (SignalSide)
            {
                case CryptoTradeSide.Long:
                    // %K should recover
                    if (CandleLast?.CandleData?.StochOscillator < candlePrev?.CandleData?.StochOscillator)
                    {
                        ExtraText = $"Stoch.K {candlePrev.CandleData.StochOscillator:N8} not recovering < {CandleLast.CandleData.StochOscillator:N8}";
                        return false;
                    }

                    // %D and %K should have crossed, %K(quick/blue) > %D(slow/red)
                    if (CandleLast?.CandleData?.StochOscillator < CandleLast?.CandleData?.StochSignal)
                    {
                        ExtraText = $"Stoch.%D {candlePrev?.CandleData?.StochSignal:N8} not above %K {candlePrev?.CandleData?.StochOscillator:N8}";
                        return false;
                    }
                    break;
                case CryptoTradeSide.Short:
                    // %K should recover (= fall) for a short — refuse while it is still rising.
                    if (CandleLast?.CandleData!.StochOscillator > candlePrev?.CandleData?.StochOscillator)
                    {
                        ExtraText = $"Stoch.K {candlePrev.CandleData.StochOscillator:N8} not recovering > {CandleLast.CandleData?.StochOscillator:N8}";
                        return false;
                    }

                    // %D and %K should have crossed, %K(quick/blue) < %D(slow/red).
                    // BUGFIX: the previous condition was StochSignal > StochOscillator (= %D > %K
                    // = %K < %D), which is the DESIRED short state — so the check refused exactly
                    // when it should have allowed and vice versa, letting bullish %K-above-%D
                    // setups through on shorts. Correct test: refuse while %K is still above %D
                    // (cross has not yet happened in the short direction).
                    if (CandleLast?.CandleData?.StochOscillator > CandleLast?.CandleData?.StochSignal)
                    {
                        ExtraText = $"Stoch.%K {CandleLast?.CandleData?.StochOscillator:N8} not below %D {CandleLast?.CandleData?.StochSignal:N8}";
                        return false;
                    }
                    break;
            }
        }


        // ********************************************************************
        // Dont trade against the trend (only check current interval)
        if (settings.CheckTrendPrimaryDirection && !CheckTrendPrimary(settings.TrendPrimaryDirectionCount))
            return false;
        if (settings.CheckTrendSecondaryDirection && !CheckTrendSecondary(settings.TrendSecondaryDirectionCount))
            return false;


        // ********************************************************************
        // Wait for stoch %K (blue line) to exit the OS/OB zone before stepping in.
        // Catches the actual bounce/fade candle instead of an extended oscillator extreme.
        if (settings.WaitForStochRecovery)
        {
            var k = CandleLast!.CandleData?.StochOscillator;
            if (k == null)
                return false;

            switch (SignalSide)
            {
                case CryptoTradeSide.Long:
                    if (k < GlobalData.Settings.General.SettingsStoch.Oversold)
                    {
                        ExtraText = "waiting for stoch %K to exit os zone";
                        return false;
                    }
                    break;
                case CryptoTradeSide.Short:
                    if (k > GlobalData.Settings.General.SettingsStoch.Overbought)
                    {
                        ExtraText = "waiting for stoch %K to exit ob zone";
                        return false;
                    }
                    break;
            }
        }

        // ********************************************************************
        // Wait for RSI to exit the OS/OB zone before stepping in.
        // Catches the actual bounce/fade candle instead of an extended oscillator extreme.
        if (settings.WaitForRsiRecovery)
        {
            var rsi = CandleLast!.CandleData?.Rsi;
            if (rsi == null)
                return false;

            switch (SignalSide)
            {
                case CryptoTradeSide.Long:
                    if (rsi < GlobalData.Settings.General.SettingsRsi.Oversold)
                    {
                        ExtraText = "waiting for rsi to exit os zone";
                        return false;
                    }
                    break;
                case CryptoTradeSide.Short:
                    if (rsi > GlobalData.Settings.General.SettingsRsi.Overbought)
                    {
                        ExtraText = "waiting for rsi to exit ob zone";
                        return false;
                    }
                    break;
            }
        }

        return true;
    }



    // Get the candle and indicator data from the signal interval
    public bool GetPrevCandle(MyData? oldCandle, out MyData? newCandle)
    {
        if (oldCandle == null)
        {
            ExtraText = $"Candle = null";
            newCandle = null;
            return false;
        }

        CandleTime targetTime = oldCandle.Candle.OpenTime - Interval.Duration;
        if (!IndicatorData.TryGetCandle(targetTime, out newCandle))
        {
            ExtraText = $"No prev candle or data! {targetTime.ToDateTime().ToLocalTime()}";
            newCandle = null;
            return false;
        }


        if (!IndicatorsOkay(newCandle!))
        {
            ExtraText = $"Prev problem indicators! {targetTime.ToDateTime().ToLocalTime()}";
            return false;
        }

        return true;
    }

    // Get the previous candle and indicator data from a DIFFERENT interval
    public bool GetPrevCandle(CryptoInterval interval, MyData? oldData, out MyData? newData)
    {
        if (oldData == null)
        {
            ExtraText = $"Candle = null";
            newData = null;
            return false;
        }

        CandleTime targetTime = oldData.Candle.OpenTime - interval.Duration;
        if (!IndicatorDataList.TryGetCandle(interval, targetTime, out newData))
        {
            ExtraText = $"No prev candle or data! {targetTime.ToDateTime().ToLocalTime()}";
            newData = null;
            return false;
        }

        if (!IndicatorsOkay(newData!))
        {
            ExtraText = $"Prev problem indicators! {targetTime.ToDateTime().ToLocalTime()}";
            return false;
        }

        return true;
    }


    // Get the candle and indicator data from a DIFFERENT interval
    public bool GetNextCandle(CryptoInterval interval, MyData? oldData, out MyData? newData)
    {
        if (oldData == null)
        {
            newData = null;
            return false;
        }

        CandleTime targetTime = oldData.Candle.OpenTime + interval.Duration;
        if (!IndicatorDataList.TryGetCandle(interval, targetTime, out newData))
        {
            ExtraText = $"No next candle or data! {targetTime.ToDateTime().ToLocalTime()}";
            newData = null;
            return false;
        }

        if (!IndicatorsOkay(newData!))
        {
            ExtraText = $"Next problem indicators! {targetTime.ToDateTime().ToLocalTime()}";
            return false;
        }

        return true;
    }


    protected MyData? HadStobbInThelastXCandles(CryptoTradeSide side, int skipCandleCount, int candleCount)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        MyData? candle = CandleLast;
        while (candleCount > 0)
        {
            skipCandleCount--;
            bool isOverSold = candle is not null && candle.IsBelowBollingerBands(GlobalData.Settings.Signal.Stobb.UseLowHigh) && candle.StochOversold();
            bool isOverBought = candle is not null && candle.IsAboveBollingerBands(GlobalData.Settings.Signal.Stobb.UseLowHigh) && candle.StochOverbought();

            if (side == CryptoTradeSide.Long)
            {
                if (isOverBought) // Een short melding! Nee ongeldig!
                    return null;
                if (skipCandleCount < 0 && isOverSold)
                    return candle;
            }
            else
            {
                if (isOverSold) // Een long melding! Nee ongeldig!
                    return null;
                if (skipCandleCount < 0 && isOverBought)
                    return candle;
            }

            if (!GetPrevCandle(candle, out candle))
                return null;
            candleCount--;
        }

        return null;
    }



    protected MyData? HadStorsiInThelastXCandles(CryptoTradeSide side, int skipCandleCount, int candleCount, int correction = 0)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        MyData? candle = CandleLast;
        while (candleCount > 0)
        {
            skipCandleCount--; // GlobalData.Settings.Signal.StoRsi.AddRsiAmount
            bool isOverSold = candle is not null && candle.RsiOversold(correction) && candle.StochOversold();
            bool isOverBought = candle is not null && candle.RsiOverbought(correction) && candle.StochOverbought();

            if (side == CryptoTradeSide.Long)
            {
                if (isOverBought) // Een short melding! Nee ongeldig!
                    return null;
                if (skipCandleCount < 0 && isOverSold)
                    return candle;
            }
            else
            {
                if (isOverSold) // Een long melding! Nee ongeldig!
                    return null;
                if (skipCandleCount < 0 && isOverBought)
                    return candle;
            }

            if (!GetPrevCandle(candle, out candle))
                return null;
            candleCount--;
        }

        return null;
    }

    protected bool InLowerPartOfBollingerBands(int candleCount, decimal percentage)
    {
        // Was the price near the lower bb?

        MyData? last = CandleLast;
        while (candleCount-- > 0)
        {
            decimal band = (decimal)last!.CandleData?.BollingerBandsLowerBand!;
            band += (decimal)last!.CandleData?.BollingerBandsDeviation! * percentage / 100m;

            decimal value;
            if (GlobalData.Settings.Signal.Sbm.Sbm2UseLowHigh)
                value = last.Candle.Low;
            else
                value = Math.Max(last.Candle.Open, last.Candle.Close);

            if (value <= band)
                return true;

            if (!GetPrevCandle(last, out last))
                return false;
        }

        return false;
    }


    protected bool InUpperPartOfBollingerBands(int candleCount, decimal percentage)
    {
        // Was the price near the upper bb?

        MyData? last = CandleLast;
        while (candleCount > 0)
        {
            decimal band = (decimal)last!.CandleData?.BollingerBandsUpperBand!;
            band -= (decimal)last!.CandleData?.BollingerBandsDeviation! * percentage / 100m;

            decimal value;
            if (GlobalData.Settings.Signal.Sbm.Sbm2UseLowHigh)
                value = last.Candle.High;
            else
                value = Math.Max(last.Candle.Open, last.Candle.Close);

            if (value >= band)
                return true;

            if (!GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }


    /// <summary>
    /// Als de ma200 en ma50 elkaar gekruist of geraakt hebben dan is het een nogo
    /// Er geen crosover is geweest van de 200 en 50 in de laatste x candles.
    /// </summary>
    public bool HasCrossed200and50(int candleCount, out int candlesAgo)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        candlesAgo = 0;
        CandleTime time = CandleLast.Candle.OpenTime;
        MyData? prevCandle = null;
        while (candleCount >= 0)
        {
            if (IndicatorData.TryGetCandle(time, out MyData? lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle!) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 200 naar boven
                        if (prevCandle.CandleData!.Sma50 < prevCandle.CandleData.Sma200 &&
                                lastCandle!.CandleData!.Sma50 >= lastCandle.CandleData.Sma200)
                            return true;
                        // de 50 kruist de 200 naar beneden
                        if (prevCandle.CandleData!.Sma50 > prevCandle.CandleData.Sma200 &&
                                lastCandle!.CandleData!.Sma50 <= lastCandle.CandleData.Sma200)
                            return true;
                    }
                }
            }

            candlesAgo++;
            candleCount--;
            prevCandle = lastCandle;
            time -= Interval.Duration;
        }
        return false;
    }


    /// <summary>
    /// Als de ma200 en ma20 elkaar gekruist of geraakt hebben dan is het een nogo
    /// Er geen crosover is geweest van de 200 en 50 in de laatste x candles.
    /// </summary>
    public bool HasCrossed200and20(int candleCount, out int candlesAgo)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        candlesAgo = 0;
        CandleTime time = CandleLast.Candle.OpenTime;
        MyData? prevCandle = null;
        while (candleCount >= 0)
        {
            if (IndicatorData.TryGetCandle(time, out MyData? lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle!) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 200 naar boven
                        if (prevCandle.CandleData!.Sma20 < prevCandle.CandleData.Sma200 &&
                                lastCandle!.CandleData!.Sma20 >= lastCandle.CandleData.Sma200)
                            return true;
                        // de 50 kruist de 200 naar beneden
                        if (prevCandle.CandleData!.Sma20 > prevCandle.CandleData.Sma200 &&
                                lastCandle!.CandleData!.Sma20 <= lastCandle.CandleData.Sma200)
                            return true;
                    }
                }
            }
            candlesAgo++;
            candleCount--;
            prevCandle = lastCandle;
            time -= Interval.Duration;
        }
        return false;
    }


    /// <summary>
    /// Als de ma200 en ma50 elkaar gekruist of geraakt hebben dan is het een nogo
    /// Er geen crosover is geweest van de 200 en 50 in de laatste x candles.
    /// </summary>
    public bool HasCrossed50and20(int candleCount, out int candlesAgo)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        candlesAgo = 0;
        CandleTime time = CandleLast.Candle.OpenTime;
        MyData? prevCandle = null;
        while (candleCount >= 0)
        {
            if (IndicatorData.TryGetCandle(time, out MyData? lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle!) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 20 naar boven
                        if (prevCandle.CandleData!.Sma50 < prevCandle.CandleData.Sma20 &&
                                lastCandle!.CandleData!.Sma50 >= lastCandle.CandleData.Sma20)
                            return true;

                        // de 50 kruist de 20 naar beneden
                        if (prevCandle.CandleData!.Sma50 > prevCandle.CandleData.Sma20 &&
                                lastCandle!.CandleData!.Sma50 <= lastCandle.CandleData.Sma20)
                            return true;
                    }
                }
            }

            candlesAgo++;
            candleCount--;
            prevCandle = lastCandle;
            time -= Interval.Duration;
        }
        return false;
    }


    public bool CheckMaCrossings(out string response)
    {
        if (GlobalData.Settings.Signal.Sbm.Ma200AndMa20Crossing && HasCrossed200and20(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Lookback, out int candlesAgo))
        {
            response = string.Format("ma200 and ma20 crossed ({0} candles)", candlesAgo);
            return false;
        }
        if (GlobalData.Settings.Signal.Sbm.Ma200AndMa50Crossing && HasCrossed200and50(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Lookback, out candlesAgo))
        {
            response = string.Format("ma200 and ma50 crossed ({0} candles)", candlesAgo);
            return false;
        }
        if (GlobalData.Settings.Signal.Sbm.Ma50AndMa20Crossing && HasCrossed50and20(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Lookback, out candlesAgo))
        {
            response = string.Format("ma50 and ma20 crossed ({0} candles)", candlesAgo);
            return false;
        }

        response = "";
        return true;
    }


    private bool CheckTrend(bool primaryTrend, string captionTrend, int intervalCount)
    {
        var trendType = primaryTrend ? GlobalData.Settings.Trend.Primary : GlobalData.Settings.Trend.Secondary;
        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, trendType).Result;

        // Guard against the noise on the lower timeframes
        var period = Interval.IntervalPeriod;
        //if (period < CryptoIntervalPeriod.interval5m)
        //    period = CryptoIntervalPeriod.interval5m;

        while (intervalCount-- > 0)
        {
            var symbolPeriod = Symbol.GetSymbolInterval(period);
            var trendData = primaryTrend ? symbolPeriod.TrendPrimary : symbolPeriod.TrendSecondary;
            var trend = trendData.Trend;

            switch (SignalSide)
            {
                case CryptoTradeSide.Long:
                    if (trend != CryptoTrendIndicator.Bullish)
                    {
                        ExtraText = $"Trend{captionTrend} {trend}, need Bullish";
                        return false;
                    }
                    // Structure check: if current price has broken below the most recent swing-low,
                    // the bullish structure is invalidated even though Trend still reports Bullish.
                    // The most recent Low is either LastPivot (when type='L') or PrevPivot (when
                    // LastPivot is the High that followed the Low). When there are <2 pivots yet,
                    // both lookups return null and we skip the check.
                    decimal? lastLow = trendData.LastPivotType == 'L' ? trendData.LastPivotValue
                                     : trendData.PrevPivotType == 'L' ? trendData.PrevPivotValue
                                     : null;
                    if (lastLow.HasValue && CandleLast.Candle.Close < lastLow.Value)
                    {
                        ExtraText = $"Trend{captionTrend} {period} price {CandleLast.Candle.Close:N8} below last low {lastLow.Value:N8}";
                        return false;
                    }
                    break;
                case CryptoTradeSide.Short:
                    if (trend != CryptoTrendIndicator.Bearish)
                    {
                        ExtraText = $"Trend{captionTrend} {trend}, need Bearish";
                        return false;
                    }

                    // Mirror: if current price has broken above the most recent swing-high, the
                    // bearish structure is invalidated.
                    decimal? lastHigh = trendData.LastPivotType == 'H' ? trendData.LastPivotValue
                                      : trendData.PrevPivotType == 'H' ? trendData.PrevPivotValue
                                      : null;
                    if (lastHigh.HasValue && CandleLast.Candle.Close > lastHigh.Value)
                    {
                        ExtraText = $"Trend{captionTrend} {period} price {CandleLast.Candle.Close:N8} above last high {lastHigh.Value:N8}";
                        return false;
                    }
                    break;
            }
            period++;
        }

        return true;
    }

    public bool CheckTrendPrimary(int intervalCount = 2)
    {
        return CheckTrend(true, "primary", intervalCount);
    }


    public bool CheckTrendSecondary(int intervalCount = 2)
    {
        return CheckTrend(false, "primary", intervalCount);
    }

}