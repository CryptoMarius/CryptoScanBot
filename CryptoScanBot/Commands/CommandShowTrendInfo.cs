using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.Core.Trend;

using System.Text;

namespace CryptoScanBot.Commands;

public class CommandShowTrendInfo : CommandBase
{
    public override async void Execute(ToolStripMenuItemCommand item, object sender)
    {
        if (sender is CryptoSymbol symbol)
        {
            SettingsZigZag trend = GlobalData.Settings.Trend.Primary;

            StringBuilder log = new();
            log.AppendLine($"Markettrend {symbol.Name}");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab($"Markettrend {symbol.Name}");

            CryptoTrendData symbolTrend = await MarketTrend.CalculateMarketTrendAsync(symbol, trend, 0, 0, log);

            log.AppendLine("");
            log.AppendLine("");

            foreach (var interval in GlobalData.IntervalList)
            {
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                CryptoTrendData intervalTrend = trend.TrendType == TrendType.Primary ? symbolInterval.TrendPrimary : symbolInterval.TrendSecondary;

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
