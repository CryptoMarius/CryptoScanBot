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

            MarketTrend.CalculateMarketTrend(GlobalData.ActiveAccount, symbol, 0, 0, log);

            AccountSymbolData accountSymbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
            foreach (AccountSymbolIntervalData accountSymbolIntervalData in accountSymbolData.SymbolTrendDataList)
            {
                log.AppendLine("");
                log.AppendLine("----");
                log.AppendLine($"Interval {accountSymbolIntervalData.Interval.Name}");

                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(accountSymbolIntervalData.IntervalPeriod);
                log.AppendLine($"Candles {symbolInterval.CandleList.Count}");

                string s;
                if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bullish)
                    s = string.Format("{0} {1}, trend=bullish", symbol.Name, accountSymbolIntervalData.Interval.IntervalPeriod);
                else if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bearish)
                    s = string.Format("{0} {1}, trend=bearish", symbol.Name, accountSymbolIntervalData.Interval.IntervalPeriod);
                else
                    s = string.Format("{0} {1}, trend=sideway's", symbol.Name, accountSymbolIntervalData.Interval.IntervalPeriod);
                GlobalData.AddTextToLogTab(s);
                log.AppendLine(s);
            }

            string t = $"{symbol.Name} {accountSymbolData.MarketTrendPercentage:N2}";
            GlobalData.AddTextToLogTab(t);
            log.AppendLine(t);



            //Laad de gecachte (langere historie, minder overhad)
            string filename = GlobalData.GetBaseDir() + "Trend information.txt";
            File.WriteAllText(filename, log.ToString());
        }
    }

}
