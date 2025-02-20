using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using System.Text;

namespace CryptoScanBot.Core.Trend;

public class MarketTrend
{
    public static async Task CalculateMarketTrendAsync(
        CryptoSymbol symbol, AccountSymbol accountSymbol,
        SymbolTrend symbolTrend, TrendType trendType,
        long candleIntervalStart, long candleIntervalEnd,
        StringBuilder? log = null)
    {
        try
        {
            await accountSymbol.TrendLock.WaitAsync();
            try
            {
                if (symbolTrend.Date == null || symbolTrend.Date < candleIntervalEnd || log != null)
                {
                    string text;
                    int weightSum1 = 0;
                    int weightMax1 = 0;
                    //int weightSum2 = 0;
                    //int weightMax2 = 0;
                    //int iterarator = 0;

                    foreach (var interval in GlobalData.IntervalList)
                    {
                        var intervalTrend = symbolTrend.Get(interval.IntervalPeriod);

                        //iterarator++;
                        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                        await TrendInterval.CalculateAsync(symbol, interval, symbolInterval.CandleList, intervalTrend, trendType, candleIntervalStart, candleIntervalEnd, log);

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
                    symbolTrend.Date = candleIntervalEnd;
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
                accountSymbol.TrendLock.Release();
            }

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }
}