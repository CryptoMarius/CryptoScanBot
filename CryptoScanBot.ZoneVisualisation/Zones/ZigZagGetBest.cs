using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Zones;

public class ZigZagGetBest
{
    public static ZigZagIndicator9 CalculateBestIndicator(CryptoSymbolInterval symbolInterval, long maxUnixTime = -1)
    {
//        long startTime = Stopwatch.GetTimestamp();
//        ScannerLog.Logger.Info("CalculateBestIndicator.Start");
        AccountSymbolIntervalData accountSymbolIntervalData = new()
        {
            Interval = symbolInterval.Interval,
            IntervalPeriod = symbolInterval.IntervalPeriod,
        };

        // We cache the ZigZag indicator and we create a lot of them with different deviations
        if (accountSymbolIntervalData.ZigZagIndicators == null)
        {
            accountSymbolIntervalData.ZigZagIndicators = [];
            for (decimal deviation = 2m; deviation >= 0.5m; deviation -= 0.25m)
            {
                ZigZagIndicator9 indicator = new(symbolInterval.CandleList, GlobalData.Settings.General.UseHighLowInTrendCalculation, deviation);
                accountSymbolIntervalData.ZigZagIndicators.Add(indicator);
            }
        }

        // Add candles to the ZigZag indicators
        //foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
        //    indicator.PostponeFinish = true;
        foreach (var candle in symbolInterval.CandleList.Values)
        {
            if (maxUnixTime > 0 && candle.OpenTime > maxUnixTime)
                break;
            foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
            {
                indicator.Calculate(candle);
            }
        }
        foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
            indicator.FinishJob();




        // Calculate the average amount of pivots for the valid zigzag indicators
        int countX = 0;
        double sum = 0;
        foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
        {
            if (indicator.ZigZagList.Count > 4)
            {
                countX++;
                sum += indicator.ZigZagList.Count;
            }
        }
        double avg = sum / countX;




        // What is the best? Technically we need at least 4 pivot points, would be nice if we have a lot of pivots.
        // On the other hand, we do not want small percentages als this can give fake trend & reversal signals.
        ZigZagIndicator9? bestIndicator = null;
        foreach (var indicator in accountSymbolIntervalData.ZigZagIndicators)
        {
            int zigZagCount = indicator.ZigZagList.Count;
            if (indicator.ZigZagList.Count > 4 && zigZagCount > avg)
            {
                bestIndicator = indicator;
                break;
            }
        }
        // Fallback on the last ZigZag with deviation=1%, which should have the most pivots (not alway's the case!)
        bestIndicator ??= accountSymbolIntervalData.ZigZagIndicators.Last();
        accountSymbolIntervalData.BestIndicator = bestIndicator;

//        ScannerLog.Logger.Info("CalculateBestIndicator.Stop " + Stopwatch.GetElapsedTime(startTime).TotalSeconds.ToString());
        return bestIndicator;
    }
}
