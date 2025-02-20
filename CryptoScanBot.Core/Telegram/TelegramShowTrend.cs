using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using System.Text;

namespace CryptoScanBot.Core.Telegram;

public class TelegramShowTrend
{
    public static async Task ShowTrendAsync(string arguments, StringBuilder stringbuilder)
    {
        string symbolName = "";
        string[] parameters = arguments.Split(' ');
        if (parameters.Length > 1)
            symbolName = parameters[1].Trim().ToUpper();
        stringbuilder.AppendLine($"Trend {symbolName}");

        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            {
                AccountSymbol accountSymbol = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
                SymbolTrend symbolTrend = accountSymbol.TrendPrimary;
                await MarketTrend.CalculateMarketTrendAsync(symbol, accountSymbol, symbolTrend, TrendType.Primary, 0, 0);

                foreach (CryptoInterval interval in GlobalData.IntervalList)
                {
                    IntervalTrend intervalTrend = symbolTrend.Get(interval.IntervalPeriod);

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

