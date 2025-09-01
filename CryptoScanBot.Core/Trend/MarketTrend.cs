using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;

using System.Text;

namespace CryptoScanBot.Core.Trend;

public class MarketTrend
{
    public static async Task<CryptoTrendData> CalculateMarketTrendAsync(CryptoSymbol symbol,
        SettingsZigZag trend, long candleIntervalStart, long candleIntervalEnd,
        StringBuilder? log = null)
    {
        CryptoTrendData symbolTrend = trend.TrendType == TrendType.Primary ? symbol.Data.TrendPrimary : symbol.Data.TrendSecondary;
        try
        {
            await symbol.Data.TrendLock.WaitAsync();
            try
            {
                if (symbolTrend.Time == null || symbolTrend.Time < candleIntervalEnd || log != null)
                {
                    string text;
                    int weightSum1 = 0;
                    int weightMax1 = 0;
                    //int weightSum2 = 0;
                    //int weightMax2 = 0;
                    //int iterarator = 0;

                    foreach (var interval in GlobalData.IntervalList)
                    {
                        // Exclude the 1 week interval (its a new interval)
                        if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1w)
                            continue;
                        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                        CryptoTrendData intervalTrend = trend.TrendType == TrendType.Primary ? symbolInterval.TrendPrimary : symbolInterval.TrendSecondary;

                        //iterarator++;
                        await TrendInterval.CalculateAsync(symbol, interval, symbolInterval.CandleList, intervalTrend, trend, candleIntervalStart, candleIntervalEnd, log);

                        int weight1 = interval.Duration;
                        if (intervalTrend.Trend == CryptoTrendIndicator.Bullish)
                            weightSum1 += weight1;
                        else if (intervalTrend.Trend == CryptoTrendIndicator.Bearish)
                            weightSum1 -= weight1;
                        weightMax1 += weight1;

                        //int weight2 = (int)intervalTrend.IntervalPeriod * iterarator;
                        //if (intervalTrend.TrendInterval == CryptoTrendIndicator.Bullish)
                        //    weightSum2 += weight2;
                        //else if (intervalTrend.TrendInterval == CryptoTrendIndicator.Bearish)
                        //    weightSum2 -= weight2;
                        //weightMax2 += weight2;

                        text = $"{symbol.Name} {interval.Name} weight={weight1} sum={weightSum1}";
                        log?.AppendLine(text);
                        ScannerLog.Logger.Trace("MarketTrend.Calculate " + text);
                    }

                    //float marketTrendPercentage1 = 100 * (float)weightSum1 / weightMax1;
                    //float marketTrendPercentage2 = 100 * (float)weightSum2 / weightMax2;
                    //GlobalData.AddTextToLogTab($"Markettrend debug {symbol.Name} {marketTrendPercentage1:N2}={weightSum1}/{weightMax1}  {marketTrendPercentage2:N2}={weightSum2}/{weightMax2}");
                    symbolTrend.Time = candleIntervalEnd;
                    symbolTrend.Percentage = 100 * (float)weightSum1 / weightMax1; // marketTrendPercentage1; // 

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