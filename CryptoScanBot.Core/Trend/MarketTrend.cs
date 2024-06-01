using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

public class MarketTrend
{
    public static void Calculate(CryptoSignal signal, long lastCandle1mCloseTime)
    {
        //if (GlobalData.BackTest)
        //    return;

        long percentageSum = 0;
        long maxPercentageSum = 0;
        try
        {
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                CryptoSymbolInterval symbolInterval = signal.Symbol.GetSymbolInterval(interval.IntervalPeriod);
                TrendInterval.Calculate(symbolInterval, 0, lastCandle1mCloseTime, null);
                CryptoTrendIndicator trendIndicator = symbolInterval.TrendIndicator;

                // save to the signal
                if (interval.IntervalPeriod == signal.Interval.IntervalPeriod)
                    signal.TrendIndicator = trendIndicator;

                // save to the other interval trends
                switch (interval.IntervalPeriod)
                {
                    case CryptoIntervalPeriod.interval15m:
                        signal.Trend15m = trendIndicator;
                        break;
                    case CryptoIntervalPeriod.interval30m:
                        signal.Trend30m = trendIndicator;
                        break;
                    case CryptoIntervalPeriod.interval1h:
                        signal.Trend1h = trendIndicator;
                        break;
                    case CryptoIntervalPeriod.interval4h:
                        signal.Trend4h = trendIndicator;
                        break;
                    case CryptoIntervalPeriod.interval12h:
                        signal.Trend12h = trendIndicator;
                        break;
                }

                if (GlobalData.Settings.General.IntervalForMarketTrend.Contains(interval.Name))
                {
                    // MarketTrend: add/substract the weight
                    if (trendIndicator == CryptoTrendIndicator.Bullish)
                        percentageSum += interval.Duration;
                    else if (trendIndicator == CryptoTrendIndicator.Bearish)
                        percentageSum -= interval.Duration;
                    // MarketTrend: Max weight
                    maxPercentageSum += interval.Duration;
                }
            }


            float trendPercentage = 100 * (float)percentageSum / maxPercentageSum;
            signal.TrendPercentage = trendPercentage;
            signal.Symbol.TrendPercentage = trendPercentage;
            signal.Symbol.TrendInfoDate = CandleTools.GetUnixDate(signal.EventTime);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString(), true);

            signal.TrendPercentage = 0;
            signal.Symbol.TrendPercentage = 0;
            signal.Symbol.TrendInfoDate = CandleTools.GetUnixDate(signal.EventTime);
        }
    }
}
