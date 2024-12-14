using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using System.Text;

namespace CryptoScanBot.Core.Trend;

public class MarketTrend
{
    public static async Task CalculateMarketTrendAsync(CryptoAccount? tradeAccount, CryptoSymbol symbol, long candleIntervalStart, long candleIntervalEnd, StringBuilder? log = null)
    {
        try
        {
            AccountSymbolData accountSymbolData = tradeAccount!.Data.GetSymbolData(symbol.Name);
            await accountSymbolData.TrendLock.WaitAsync();
            try
            {
                if (accountSymbolData.MarketTrendDate == null || accountSymbolData.MarketTrendDate < candleIntervalEnd || log != null)
                {
                    string text;
                    int weightSum1 = 0;
                    int weightMax1 = 0;
                    //int weightSum2 = 0;
                    //int weightMax2 = 0;
                    //int iterarator = 0;
                    foreach (AccountSymbolIntervalData accountSymbolIntervalData in accountSymbolData.SymbolTrendDataList)
                    {
                        //iterarator++;
                        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(accountSymbolIntervalData.IntervalPeriod);
                        await TrendInterval.CalculateAsync(symbol, symbolInterval.CandleList, accountSymbolIntervalData, candleIntervalStart, candleIntervalEnd, log);

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

                        text = $"{symbol.Name} {accountSymbolIntervalData.Interval.Name} weight={weight1} sum={weightSum1}";
                        log?.AppendLine(text);
                        ScannerLog.Logger.Trace("MarketTrend.Calculate " + text);
                    }

                    //float marketTrendPercentage1 = 100 * (float)weightSum1 / weightMax1;
                    //float marketTrendPercentage2 = 100 * (float)weightSum2 / weightMax2;
                    //GlobalData.AddTextToLogTab($"Markettrend debug {symbol.Name} {marketTrendPercentage1:N2}={weightSum1}/{weightMax1}  {marketTrendPercentage2:N2}={weightSum2}/{weightMax2}");
                    accountSymbolData.MarketTrendDate = candleIntervalEnd;
                    accountSymbolData.MarketTrendPercentage = 100 * (float)weightSum1 / weightMax1; // marketTrendPercentage1; // 

                    log?.AppendLine("");
                    ScannerLog.Logger.Trace("");
                    text = $"{symbol.Name} sum ={weightSum1} / {weightMax1} = {accountSymbolData.MarketTrendPercentage:N2}";
                    log?.AppendLine(text);
                    ScannerLog.Logger.Trace("MarketTrend.Calculate " + text);
                }
            }
            finally
            {
                accountSymbolData.TrendLock.Release();
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