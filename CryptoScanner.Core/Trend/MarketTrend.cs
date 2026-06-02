using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using System.Text;

namespace CryptoScanner.Core.Trend;

public class MarketTrend
{
    public static async Task<CryptoTrendData> CalculateMarketTrendAsync(CryptoSymbol symbol, SettingsZigZag trend, StringBuilder? log = null)
    {
        // MarketTrend is summarized in the Symbol.Data.TrendPrimary/Secondary and is calculated from the Interval.TrendPrimary/Secondary
        CryptoTrendData symbolTrend = trend.TrendType == TrendType.Primary ? symbol.Data.TrendPrimary : symbol.Data.TrendSecondary;
        try
        {
            await symbol.Data.TrendLock.WaitAsync();
            try
            {
                // Take the last 1m endtime as timing (
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                if (symbolInterval.LastCandle.OpenTime == 0)
                    return symbolTrend; // should never happen
                CandleTime candleIntervalEnd = symbolInterval.LastCandle.OpenTime;

                // the log parameter is only present when called from the CommandShowTrendInfo()
                if (symbolTrend.Time == null || candleIntervalEnd > symbolTrend.Time || log != null)
                {
                    string text;
                    int weightSum = 0;
                    int weightMax = 0;
                    symbolTrend.Time = candleIntervalEnd;

                    foreach (var interval in GlobalData.IntervalList)
                    {
                        // Exclude the 1 week interval (its a new interval which adds a lot of weight)
                        if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1w)
                            continue;

                        bool isCached = false;
                        symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                        if (symbolInterval.LastCandle.OpenTime == 0)
                            return symbolTrend; // should never happen
                        CryptoTrendData intervalTrend = trend.TrendType == TrendType.Primary ? symbolInterval.TrendPrimary : symbolInterval.TrendSecondary;
                        CryptoTrendData intervalTrendBos = trend.TrendType == TrendType.Primary ? symbolInterval.TrendBosPrimary : symbolInterval.TrendBosSecondary;
                        candleIntervalEnd = symbolInterval.LastCandle.OpenTime;
                        if (intervalTrend.Time == null || candleIntervalEnd > intervalTrend.Time || log != null)
                        {
                            // Do NOT pre-set intervalTrend.Time here. TrendCalculator saves intervalTrend.Time
                            // into PrevTime before overwriting it with maxDate. Pre-setting it here would cause
                            // PrevTime == Time, making the consecutive-candle check in SignalTrendShort/Long
                            // (PrevTime + Interval.Duration == Time) always fail.
                            //
                            // CalculateBothAsync builds the ZigZag once and feeds it to both the Dow and the
                            // BOS interpretation, writing each to its own trend-data slot.
                            await TrendCalculator.CalculateBothAsync(symbol, interval, symbolInterval.CandleList,
                                intervalTrend, intervalTrendBos, trend, log);
                        }
                        else isCached = true;

                        int intervalWeight = (int)interval.Duration;
                        if (intervalTrend.Trend == CryptoTrendIndicator.Bullish)
                            weightSum += intervalWeight;
                        else if (intervalTrend.Trend == CryptoTrendIndicator.Bearish)
                            weightSum -= intervalWeight;
                        weightMax += intervalWeight;

                        text = $"{symbol.Name} {interval.Name} {intervalTrend.Trend} weight={intervalWeight} sum={weightSum}";
                        if (isCached)
                            text += " (cached)";
                        log?.AppendLine(text);
                        //ScannerLog.Logger.Debug("MarketTrend.Calculate " + text);
                    }
                    symbolTrend.Percentage = 100 * (float)weightSum / weightMax;

                    //// BOS/CHoCH calculation runs on the Primary ZigZag only
                    //if (trend.TrendType == TrendType.Primary)
                    //{
                    //    int weightSumBos = 0;
                    //    int weightMaxBos = 0;
                    //    foreach (var bosInterval in GlobalData.IntervalList)
                    //    {
                    //        if (bosInterval.IntervalPeriod == CryptoIntervalPeriod.interval1w)
                    //            continue;
                    //        CryptoSymbolInterval bosSymbolInterval = symbol.GetSymbolInterval(bosInterval.IntervalPeriod);
                    //        CandleTime bosIntervalEnd = bosSymbolInterval.LastCandle.OpenTime;
                    //        if (bosSymbolInterval.TrendBos.Time == null || bosIntervalEnd > bosSymbolInterval.TrendBos.Time || log != null)
                    //        {
                    //            await TrendIntervalBos.CalculateAsync(symbol, bosInterval, bosSymbolInterval.CandleList,
                    //                bosSymbolInterval.TrendBos, trend, log);
                    //        }

                    //        int weightBos = (int)bosInterval.Duration;
                    //        if (bosSymbolInterval.TrendBos.Trend == CryptoTrendIndicator.Bullish)
                    //            weightSumBos += weightBos;
                    //        else if (bosSymbolInterval.TrendBos.Trend == CryptoTrendIndicator.Bearish)
                    //            weightSumBos -= weightBos;
                    //        weightMaxBos += weightBos;
                    //    }
                    //    symbol.Data.TrendBos.Time = symbolTrend.Time;
                    //    symbol.Data.TrendBos.Percentage = 100 * (float)weightSumBos / weightMaxBos;
                    //}

                    log?.AppendLine("");
                    ScannerLog.Logger.Debug("");
                    text = $"{symbol.Name} sum ={weightSum} / {weightMax} = {symbolTrend.Percentage:N2}";
                    log?.AppendLine(text);
                    //ScannerLog.Logger.Debug("MarketTrend.Calculate " + text);
                }
            }
            finally
            {
                symbol.Data.TrendLock.Release();
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString());
        }
        return symbolTrend;
    }
}