using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using System.Text;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>barometer [quote]: the price barometer per interval.</summary>
public class TelegramCommandBarometer : TelegramCommand
{
    public override string Name => "barometer";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Market;
    public override string Arguments => "[quote]";
    public override string Description => "show the barometer per interval (default USDT)";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        Show(context.Argument(0), context.Output);
        return Task.CompletedTask;
    }


    internal static void Show(string quote, StringBuilder stringbuilder)
    {
        //string text = "\\\Bla /Bla , Bla, hello 'h'";
        quote = quote.Trim().ToUpper();
        if (quote == "")
        {
            // USDT as on the dashboard; an exchange without it (USDC only) gets its first quote
            quote = "USDT";
            if (!GlobalData.Settings.QuoteCoins.ContainsKey(quote) && GlobalData.Settings.QuoteCoins.Count > 0)
                quote = GlobalData.Settings.QuoteCoins.Keys.First();
        }

        // Even een quick fix voor de barometer
        if (!GlobalData.Settings.QuoteCoins.TryGetValue(quote, out CryptoQuoteData? quoteData))
        {
            stringbuilder.AppendLine($"Unknown quote {quote}");
            return;
        }

        var exchange = GlobalData.ActiveExchange ?? throw new InvalidOperationException("no active exchange");
        stringbuilder.AppendLine($"Barometer {quote}");

        // Exactly the intervals the barometer is calculated on. The old list asked for 5m, which
        // is never calculated (always empty), and skipped 10m, 2h, 3h, 8h and 12h.
        foreach (var intervalPeriod in BarometerTools.BarometerIntervals)
        {
            CryptoBarometerData barometerData = exchange.Data.GetBarometer(quoteData.Name, intervalPeriod);
            string name = GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval? interval) ? interval.Name : intervalPeriod.ToString();
            if (barometerData.PriceBarometer.HasValue)
                stringbuilder.AppendLine($"{name} {barometerData.PriceBarometer.Value:N2}");
            else
                stringbuilder.AppendLine($"{name} -");
        }
    }
}
