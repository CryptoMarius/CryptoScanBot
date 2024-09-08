using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using System.Text;

namespace CryptoScanBot.Core.Trend;

public class MarketTrend
{
    public static void CalculateMarketTrend(CryptoAccount? tradeAccount, CryptoSymbol symbol, long candleIntervalStart, long candleIntervalEnd, StringBuilder? log = null)
    {
        AccountSymbolData accountSymbolData = tradeAccount!.Data.GetSymbolData(symbol.Name);
        try
        {
            lock (accountSymbolData.SymbolTrendDataList);
            {
                if (accountSymbolData.MarketTrendDate == null || accountSymbolData.MarketTrendDate < candleIntervalEnd)
                {
                    int weightSum1 = 0;
                    int weightMax1 = 0;
                    //int weightSum2 = 0;
                    //int weightMax2 = 0;
                    //int iterarator = 0;
                    foreach (AccountSymbolIntervalData accountSymbolIntervalData in accountSymbolData.SymbolTrendDataList)
                    {
                        //iterarator++;
                        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(accountSymbolIntervalData.IntervalPeriod);
                        TrendInterval.Calculate(symbol, symbolInterval.CandleList, accountSymbolIntervalData, candleIntervalStart, candleIntervalEnd, log);

                        int weight1 = accountSymbolIntervalData.Interval.Duration;
                        if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bullish)
                            weightSum1 += weight1;
                        else if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bearish)
                            weightSum1 -= weight1;
                        weightMax1 += weight1;

                        //int weight2 = (int)accountSymbolIntervalData.IntervalPeriod * iterarator;
                        //if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bullish)
                        //    weightSum2 += weight2;
                        //else if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bearish)
                        //    weightSum2 -= weight2;
                        //weightMax2 += weight2;
                    }

                    //float marketTrendPercentage1 = 100 * (float)weightSum1 / weightMax1;
                    //float marketTrendPercentage2 = 100 * (float)weightSum2 / weightMax2;
                    //GlobalData.AddTextToLogTab($"Markettrend debug {symbol.Name} {marketTrendPercentage1:N2}={weightSum1}/{weightMax1}  {marketTrendPercentage2:N2}={weightSum2}/{weightMax2}");
                    accountSymbolData.MarketTrendDate = candleIntervalEnd;
                    accountSymbolData.MarketTrendPercentage = 100 * (float)weightSum1 / weightMax1; // marketTrendPercentage1; // 
                }
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
