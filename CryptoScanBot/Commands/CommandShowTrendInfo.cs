using System.Text;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Commands;

public class CommandShowTrendInfo : CommandBase
{
    public override string CommandName()
        => "Show trend";


    public override void Execute(object sender)
    {
        if (sender is CryptoSymbol symbol)
        {

            StringBuilder log = new();
            log.AppendLine("Trend " + symbol.Name);

            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab("Trend " + symbol.Name);

            long percentageSum = 0;
            long maxPercentageSum = 0;
            AccountSymbolData accountSymbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
            foreach (AccountSymbolIntervalData accountSymbolIntervalData in accountSymbolData.SymbolTrendDataList)
            {
                log.AppendLine("");
                log.AppendLine("----");
                log.AppendLine($"Interval {accountSymbolIntervalData.Interval.Name}");

                // Wat is het maximale som (voor de eindberekening)
                maxPercentageSum += accountSymbolIntervalData.Interval.Duration;

                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(accountSymbolIntervalData.IntervalPeriod);
                log.AppendLine($"Candles {symbolInterval.CandleList.Count}");
                
                TrendInterval.Calculate(symbol, symbolInterval.CandleList, accountSymbolIntervalData, 0, 0, log);
                if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bullish)
                    percentageSum += accountSymbolIntervalData.Interval.Duration;
                else if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bearish)
                    percentageSum -= accountSymbolIntervalData.Interval.Duration;

                string s = "";
                if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bullish)
                    s = string.Format("{0} {1}, trend=bullish", symbol.Name, accountSymbolIntervalData.Interval.IntervalPeriod);
                else if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bearish)
                    s = string.Format("{0} {1}, trend=bearish", symbol.Name, accountSymbolIntervalData.Interval.IntervalPeriod);
                else
                    s = string.Format("{0} {1}, trend=sideway's", symbol.Name, accountSymbolIntervalData.Interval.IntervalPeriod);
                GlobalData.AddTextToLogTab(s);
                log.AppendLine(s);
            }

            if (maxPercentageSum > 0)
            {
                decimal trendPercentage = 100 * (decimal)percentageSum / maxPercentageSum;
                string t = string.Format("{0} {1:N2}", symbol.Name, trendPercentage);
                GlobalData.AddTextToLogTab(t);
                log.AppendLine(t);
            }



            //Laad de gecachte (langere historie, minder overhad)
            string filename = GlobalData.GetBaseDir() + "Trend information.txt";
            File.WriteAllText(filename, log.ToString());
        }
    }

}
