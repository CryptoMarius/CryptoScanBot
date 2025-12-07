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
                long candleIntervalEnd = symbolInterval.LastCandle == null ? 0 : symbolInterval.LastCandle.OpenTime + symbolInterval.Interval.Duration;

                // the log parameter is only present when called from the CommandShowTrendInfo()
                if (symbolTrend.Time == null || symbolTrend.Time < candleIntervalEnd || log != null)
                {
                    string text;
                    int weightSum1 = 0;
                    int weightMax1 = 0;
                    symbolTrend.Time = candleIntervalEnd;

                    foreach (var interval in GlobalData.IntervalList)
                    {
                        // Exclude the 1 week interval (a new interval)
                        if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1w)
                            continue;

                        bool isCached = false;
                        symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                        CryptoTrendData intervalTrend = trend.TrendType == TrendType.Primary ? symbolInterval.TrendPrimary : symbolInterval.TrendSecondary;
                        candleIntervalEnd = symbolInterval.LastCandle == null ? 0 : symbolInterval.LastCandle.OpenTime + symbolInterval.Interval.Duration;
                        if (intervalTrend.Time == null || intervalTrend.Time < candleIntervalEnd || log != null)
                        {
                            intervalTrend.Time = candleIntervalEnd;
                            await TrendInterval.CalculateAsync(symbol, interval, symbolInterval.CandleList, intervalTrend, trend, log);
                        }
                        else isCached = true;

                        int weight1 = interval.Duration;
                        if (intervalTrend.Trend == CryptoTrendIndicator.Bullish)
                            weightSum1 += weight1;
                        else if (intervalTrend.Trend == CryptoTrendIndicator.Bearish)
                            weightSum1 -= weight1;
                        weightMax1 += weight1;

                        text = $"{symbol.Name} {interval.Name} weight={weight1} sum={weightSum1}";
                        if (isCached) text += " (cached)";
                        log?.AppendLine(text);
                        ScannerLog.Logger.Trace("MarketTrend.Calculate " + text);
                    }
                    symbolTrend.Percentage = 100 * (float)weightSum1 / weightMax1;

                    log?.AppendLine("");
                    ScannerLog.Logger.Trace("");
                    text = $"{symbol.Name} sum ={weightSum1} / {weightMax1} = {symbolTrend.Percentage:N2}";
                    log?.AppendLine(text);
                    ScannerLog.Logger.Trace("MarketTrend.Calculate " + text);
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