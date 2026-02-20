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
                        candleIntervalEnd = symbolInterval.LastCandle.OpenTime;
                        if (intervalTrend.Time == null || candleIntervalEnd > intervalTrend.Time || log != null)
                        {
                            intervalTrend.Time = candleIntervalEnd;
                            await TrendInterval.CalculateAsync(symbol, interval, symbolInterval.CandleList, intervalTrend, trend, log);
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
                        ScannerLog.Logger.Trace("MarketTrend.Calculate " + text);
                    }
                    symbolTrend.Percentage = 100 * (float)weightSum / weightMax;

                    log?.AppendLine("");
                    ScannerLog.Logger.Trace("");
                    text = $"{symbol.Name} sum ={weightSum} / {weightMax} = {symbolTrend.Percentage:N2}";
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