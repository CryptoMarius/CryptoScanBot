using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>trend symbol: the trend per interval and the symbol trend.</summary>
public class TelegramCommandTrend : TelegramCommand
{
    public override string Name => "trend";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Market;
    public override string Arguments => "symbol";
    public override string Description => "show the trend per interval (trend ETHUSDT)";

    public override async Task ExecuteAsync(TelegramCommandContext context)
    {
        var output = context.Output;
        string symbolName = context.Argument(0).Trim().ToUpper();
        if (symbolName == "")
        {
            output.AppendLine("Usage: trend symbol, for example trend ETHUSDT");
            return;
        }

        var exchange = GlobalData.ActiveExchange ?? throw new InvalidOperationException("no active exchange");

        // The user types a bare pair; the symbol list is keyed on the product-suffixed name
        if (!exchange.TryGetSymbolByPair(symbolName, out CryptoSymbol? symbol))
        {
            output.AppendLine($"Unknown symbol {symbolName}");
            return;
        }

        SettingsZigZag trend = GlobalData.Settings.Trend.Primary;
        output.AppendLine($"Trend {symbol.Name}");
        CryptoTrendData symbolTrend = await SymbolTrend.CalculateSymbolTrendAsync(symbol, trend);

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
                s = "trend=sideways";
            output.AppendLine($"{interval.Name} {s}");
        }

        // The percentage can be missing (not enough candles); the forced cast used to throw on that
        if (!symbolTrend.Percentage.HasValue)
        {
            output.AppendLine("Symbol trend unknown");
            return;
        }

        float marketTrend = symbolTrend.Percentage.Value;
        if (marketTrend < 0)
            output.AppendLine($"Symbol trend {marketTrend:N2}% bearish");
        else if (marketTrend > 0)
            output.AppendLine($"Symbol trend {marketTrend:N2}% bullish");
        else
            output.AppendLine($"Symbol trend {marketTrend:N2}% unknown");
    }
}
