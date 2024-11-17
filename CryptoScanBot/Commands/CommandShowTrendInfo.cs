using System.Text;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Commands;

public class CommandShowTrendInfo : CommandBase
{
    public override void Execute(ToolStripMenuItemCommand item, object sender)
    {
        if (sender is CryptoSymbol symbol)
        {
            StringBuilder log = new();
            log.AppendLine($"Markettrend {symbol.Name}");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab($"Markettrend {symbol.Name}");
            MarketTrend.CalculateMarketTrend(GlobalData.ActiveAccount, symbol, 0, 0, log);
            log.AppendLine("");
            log.AppendLine("");

            AccountSymbolData accountSymbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
            foreach (AccountSymbolIntervalData accountSymbolIntervalData in accountSymbolData.SymbolTrendDataList)
            {
                string s;
                if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bullish)
                    s = $"{symbol.Name} {accountSymbolIntervalData.Interval.Name} trend=bullish";
                else if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bearish)
                    s = $"{symbol.Name} {accountSymbolIntervalData.Interval.Name} trend=bearish";
                else
                    s = $"{symbol.Name} {accountSymbolIntervalData.Interval.Name} trend=sideway's";
                GlobalData.AddTextToLogTab(s);
                log.AppendLine(s);
            }

            string t;
            float marketTrend = (float)accountSymbolData.MarketTrendPercentage!;
            if (marketTrend < 0)
                t = $"{symbol.Name} Markettrend={marketTrend:N2}% bearish";
            else if (marketTrend > 0)
                t = $"{symbol.Name} Markettrend={marketTrend:N2}% bullish";
            else
                t = $"{symbol.Name} Markettrend={marketTrend:N2}% unknown";
            GlobalData.AddTextToLogTab(t);
            log.AppendLine(t);


            // debug
            string filename = GlobalData.GetBaseDir() + "Trend information.txt";
            File.WriteAllText(filename, log.ToString());
        }
    }

}
