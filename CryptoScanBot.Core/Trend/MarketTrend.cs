using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

public class MarketTrend
{
    public static void Calculate(CryptoAccount? tradeAccount, CryptoSignal signal, long lastCandle1mCloseTime)
    {
        // TODO: Remove Signal (it introduces noise, caller is responsible for that)
        // We have multiple calls to calculate trends (Telegram, right mouse click etc)
#if DEBUG
        DateTime lastCandle1mCloseTimeDebug = CandleTools.GetUnixDate(lastCandle1mCloseTime);
#endif

        AccountSymbolData accountSymbolData = tradeAccount!.Data.GetSymbolData(signal.Symbol.Name);

        long percentageSum = 0;
        long maxPercentageSum = 0;
        try
        {

            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                // debug this shit..
                //if (interval.IntervalPeriod != CryptoIntervalPeriod.interval1h)
                //    continue;

                CryptoSymbolInterval symbolInterval = signal.Symbol.GetSymbolInterval(interval.IntervalPeriod);
                AccountSymbolIntervalData accountSymbolIntervalData = accountSymbolData.GetAccountSymbolIntervalData(interval.IntervalPeriod);
                TrendInterval.Calculate(signal.Symbol, symbolInterval.CandleList, accountSymbolIntervalData, 0, lastCandle1mCloseTime, null);
                CryptoTrendIndicator trendIndicator = accountSymbolIntervalData.TrendIndicator;

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
            accountSymbolData.TrendPercentage = trendPercentage;
            accountSymbolData.TrendInfoDate = CandleTools.GetUnixDate(signal.EventTime);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString(), true);

            signal.TrendPercentage = 0;
            accountSymbolData.TrendPercentage = 0;
            accountSymbolData.TrendInfoDate = CandleTools.GetUnixDate(signal.EventTime);
        }
    }
}
