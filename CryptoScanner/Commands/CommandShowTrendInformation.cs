using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trend;

using System.Text;

namespace CryptoScanner.Commands;

public class CommandShowTrendInformation : CommandBase
{
    public override async void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null && dto.interval != null)
        {
            System.Diagnostics.Debug.WriteLine($"Show trend {dto.symbol.Name}");

            SettingsZigZag trend = GlobalData.Settings.Trend.Primary;

            StringBuilder log = new();
            log.AppendLine($"Markettrend {dto.symbol.Name}");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab($"Markettrend {dto.symbol.Name}");

            CryptoTrendData symbolTrend = await MarketTrend.CalculateMarketTrendAsync(dto.symbol, trend, log);

            log.AppendLine("");
            log.AppendLine("");

            foreach (var interval in GlobalData.IntervalList)
            {
                CryptoSymbolInterval symbolInterval = dto.symbol.GetSymbolInterval(interval.IntervalPeriod);
                CryptoTrendData intervalTrend = trend.TrendType == TrendType.Primary ? symbolInterval.TrendPrimary : symbolInterval.TrendSecondary;

                string s;
                if (intervalTrend.Trend == CryptoTrendIndicator.Bullish)
                    s = $"{dto.symbol.Name} {interval.Name} trend=bullish";
                else if (intervalTrend.Trend == CryptoTrendIndicator.Bearish)
                    s = $"{dto.symbol.Name} {interval.Name} trend=bearish";
                else
                    s = $"{dto.symbol.Name} {interval.Name} trend=sideway's";
                GlobalData.AddTextToLogTab(s);
                log.AppendLine(s);
            }


            string t;
            if (symbolTrend.Percentage == null)
                t = $"{dto.symbol.Name} Markettrend unknown";
            else
            {
                var marketTrend = symbolTrend.Percentage;
                if (marketTrend < 0)
                    t = $"{dto.symbol.Name} Markettrend={marketTrend:N2}% bearish";
                else if (marketTrend > 0)
                    t = $"{dto.symbol.Name} Markettrend={marketTrend:N2}% bullish";
                else
                    t = $"{dto.symbol.Name} Markettrend={marketTrend:N2}% unknown";
            }
            GlobalData.AddTextToLogTab(t);
            log.AppendLine(t);


            // debug
            string filename = Path.Combine(GlobalData.AppDataFolder, "Trend information.txt");
            File.WriteAllText(filename, log.ToString());
        }
    }
}
