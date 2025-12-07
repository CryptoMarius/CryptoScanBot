using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trend;

using System.Text;

namespace CryptoScanner.Core.Telegram;

public class TelegramShowTrend
{
    public static async Task ShowTrendAsync(string arguments, SettingsZigZag trend, StringBuilder stringbuilder)
    {
        string symbolName = "";
        string[] parameters = arguments.Split(' ');
        if (parameters.Length > 1)
            symbolName = parameters[1].Trim().ToUpper();
        stringbuilder.AppendLine($"Trend {symbolName}");

        var exchange = GlobalData.ActiveExchange;
        if (exchange != null)
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            {
                CryptoTrendData symbolTrend = await MarketTrend.CalculateMarketTrendAsync(symbol, trend);

                foreach (CryptoInterval interval in GlobalData.IntervalList)
                {
                    CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                    CryptoTrendData intervalTrend = trend.TrendType == TrendType.Primary ? symbolInterval.TrendPrimary : symbolInterval.TrendSecondary;

                    string s;
                    if (intervalTrend.Trend == CryptoTrendIndicator.Bullish)
                        s = "trend=bullish";
                    else if (intervalTrend.Trend == CryptoTrendIndicator.Bearish)
                        s = "trend=bearish";
                    else
                        s = "trend=sideway's?";
                    stringbuilder.AppendLine($"{interval.Name} {s}");
                }

                float marketTrend = (float)symbolTrend.Percentage!;
                if (marketTrend < 0)
                    stringbuilder.AppendLine($"Symbol trend {marketTrend:N2}% bearish");
                else if (marketTrend > 0)
                    stringbuilder.AppendLine($"Symbol trend {marketTrend:N2}% bullish");
                else
                    stringbuilder.AppendLine($"Symbol trend {marketTrend:N2}% unknown");
            }
        }
    }

}

