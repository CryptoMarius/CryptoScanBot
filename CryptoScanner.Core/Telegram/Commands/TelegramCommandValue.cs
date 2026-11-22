using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using System.Text;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>value: the last price of the coins of the dashboard (setting ShowSymbolInformation).</summary>
public class TelegramCommandValue : TelegramCommand
{
    public override string Name => "value";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Market;
    public override string Arguments => "[coin ..]";
    public override string Description => "show the prices of the given coins, or of the coins on the dashboard";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        // "value btc sol" shows those coins; without arguments the coins of the dashboard
        List<string> names = context.Arguments.Select(a => a.Trim().ToUpper()).Where(a => a != "").ToList();
        Show(context.Output, names.Count > 0 ? names : null);
        return Task.CompletedTask;
    }


    internal static void Show(StringBuilder stringbuilder, IList<string>? names = null)
    {
        //string value;
        //string[] parameters = arguments.Split(' ');
        //if (parameters.Length >= 2)
        //    value = parameters[1];
        //else
        //    value = GlobalData.Settings.ShowSymbolInformation.ToList();
        //        //"BTCUSDT,ETHUSDT,PAXGUSDT,BNBUSDT";
        //parameters = value.Split(',');

        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
            return;

        // The same pairs as the dashboard (DashboardDataCollector.GetSymbolPrices): base + every
        // quote, so an exchange that quotes in USDC instead of USDT shows its prices as well. The
        // old code glued USDT behind the coin, which found nothing on HyperLiquid (BTCUSDC.PERP).
        // The product behind the dot is added by TryGetSymbolByPair.
        int count = 0;
        foreach (string baseName in names ?? GlobalData.Settings.ShowSymbolInformation)
        {
            // A full pair typed by the user (value btcusdc) is found as it is
            if (names != null && exchange.TryGetSymbolByPair(baseName, out CryptoSymbol? pairSymbol) && pairSymbol.LastPrice.HasValue)
            {
                stringbuilder.AppendLine($"{baseName} {pairSymbol.LastPrice.Value.ToString0()}");
                count++;
                continue;
            }

            foreach (string quoteName in GlobalData.Settings.QuoteCoins.Keys)
            {
                if (exchange.TryGetSymbolByPair(baseName + quoteName, out CryptoSymbol? symbol) && symbol.LastPrice.HasValue)
                {
                    stringbuilder.AppendLine($"{baseName}{quoteName} {symbol.LastPrice.Value.ToString0()}");
                    count++;
                }
            }
        }
        if (count == 0)
            stringbuilder.AppendLine("no prices available");
    }
}
