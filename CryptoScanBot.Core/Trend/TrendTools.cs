using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using System.Text;

namespace CryptoScanBot.Core.Trend;

public class TrendTools
{
    public static string TrendIndicatorText(CryptoTrendIndicator? trend)
    {
        if (!trend.HasValue)
            return "";

        return trend switch
        {
            CryptoTrendIndicator.Bullish => "up",
            CryptoTrendIndicator.Bearish => "down",
            _ => "",
        };
    }


    public static void CreateAllTrendIndicators(AccountSymbolIntervalData accountSymbolIntervalData, CryptoCandleList candleList)
    {
        // We create a lot of ZigZag indicator with different deviations
        if (accountSymbolIntervalData.ZigZagIndicators == null)
        {
            accountSymbolIntervalData.ZigZagIndicators = [];
            for (decimal deviation = 2.5m; deviation >= 0.25m; deviation -= 0.25m)
            {
                ZigZagIndicator9 indicator = new(candleList, GlobalData.Settings.General.UseHighLowInTrendCalculation, deviation, accountSymbolIntervalData.Interval.Duration)
                {
                    Deviation = deviation,
                };
                accountSymbolIntervalData.ZigZagIndicators.Add(indicator);
            }
        }
    }


    public static long? AddCandlesToTrendIndicators(AccountSymbolIntervalData accountSymbolIntervalData, CryptoCandleList candleList, long minDate, long maxDate)
    {
        long? zigZagLastCandleAdded = null;

        // Add candles to all the ZigZag indicators
        foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators!)
            indicator.StartBatch();

        int added = 0;
        long loop = minDate;
        while (loop <= maxDate)
        {
            if (candleList.TryGetValue(loop, out CryptoCandle? candle))
            {
                foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
                {
                    indicator.Calculate(candle);
                    added++;
                }
                zigZagLastCandleAdded = loop;
            }
            loop += accountSymbolIntervalData.Interval.Duration;
        }
        if (added > 0)
        {
            foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
            {
                indicator.FinishBatch();
            }
        }

        return zigZagLastCandleAdded;
    }


    public static void GetBestTrendIndicator(AccountSymbolIntervalData accountSymbolIntervalData, CryptoSymbol symbol, CryptoCandleList candleList, StringBuilder? log = null)
    {
        const int MinimumPivots = 4;

        var interval = accountSymbolIntervalData.Interval;

        // Calculate the average amount of pivots for the valid zigzag indicators
        int countX = 0;
        double sum = 0;
        foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators!)
        {
            if (indicator.ZigZagList.Count > MinimumPivots)
            {
                countX++;
                sum += indicator.ZigZagList.Count;
                if (GlobalData.Settings.General.DebugTrendCalculation)
                {
                    log?.AppendLine($"{symbol.Name} {interval.Name} candles={candleList.Count} deviation={indicator.Deviation}% candlecount={indicator.CandleCount} zigzagcount={indicator.ZigZagList.Count}");
                    //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} candles={candleList.Count} deviation={indicator.Deviation}% candlecount={indicator.CandleCount} zigzagcount={indicator.ZigZagList.Count}");
                }
            }
        }
        double avg = sum / countX;

        // What is the best? Technically we need at least 4 pivot points, would be nice if we have a lot of pivots.
        // On the other hand, we do not want small percentages als this can give fake trend & reversal signals.
        ZigZagIndicator9? bestIndicator = null;
        foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
        {
            int zigZagCount = indicator.ZigZagList.Count;
            if (indicator.ZigZagList.Count > MinimumPivots && zigZagCount > avg)
            {
                bestIndicator = indicator;
                break;
            }
        }
        // Fallback on the last ZigZag with deviation=1%, which should have the most pivots (not alway's the case!)
        bestIndicator ??= accountSymbolIntervalData.ZigZagIndicators.Last();
        accountSymbolIntervalData.BestIndicator = bestIndicator;



        if (log != null)
        {
            log.AppendLine("");
            log.AppendLine($"Best indicator with deviation={bestIndicator.Deviation} {bestIndicator.ZigZagList.Count} pivots");
            foreach (var zigZag in bestIndicator.ZigZagList)
            {
                string s = string.Format("date={0} H {1:N8} rsi={2:N8}", zigZag.Candle.Date.ToLocalTime(), zigZag.Value, zigZag.Candle.CandleData?.Rsi);
                log.AppendLine(s);
            }
        }



    }
}
