using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using System.Text;

namespace CryptoScanBot.Core.Telegram;

public class TelegramShowTrend
{
    public static void ShowTrend(string arguments, StringBuilder stringbuilder)
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
                MarketTrend.CalculateMarketTrendAsync(GlobalData.ActiveAccount, symbol, 0, 0);

                AccountSymbolData accountSymbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
                foreach (AccountSymbolIntervalData accountSymbolIntervalData in accountSymbolData.SymbolTrendDataList)
                {
                    string s;
                    if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bullish)
                        s = "trend=bullish";
                    else if (accountSymbolIntervalData.TrendIndicator == CryptoTrendIndicator.Bearish)
                        s = "trend=bearish";
                    else
                        s = "trend=sideway's?";
                    stringbuilder.AppendLine($"{accountSymbolIntervalData.Interval.Name} {s}");
                }

                float marketTrend = (float)accountSymbolData.MarketTrendPercentage!;
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
