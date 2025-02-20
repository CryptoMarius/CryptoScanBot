using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using System.Text;

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
            AccountSymbol accountSymbol = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
            SymbolTrend symbolTrend = accountSymbol.TrendPrimary;
            _ = MarketTrend.CalculateMarketTrendAsync(symbol, accountSymbol, symbolTrend, TrendType.Primary, 0, 0, log);

            log.AppendLine("");
            log.AppendLine("");

            foreach (var interval in GlobalData.IntervalList)
            {
                var intervalTrend = symbolTrend.Get(interval.IntervalPeriod);

                string s;
                if (intervalTrend.Trend == CryptoTrendIndicator.Bullish)
                    s = $"{symbol.Name} {interval.Name} trend=bullish";
                else if (intervalTrend.Trend == CryptoTrendIndicator.Bearish)
                    s = $"{symbol.Name} {interval.Name} trend=bearish";
                else
                    s = $"{symbol.Name} {interval.Name} trend=sideway's";
                GlobalData.AddTextToLogTab(s);
                log.AppendLine(s);
            }

            string t;
            float marketTrend = (float)symbolTrend.Percentage!;
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
