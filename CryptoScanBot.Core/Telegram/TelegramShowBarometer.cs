using CryptoScanBot.Core.Barometer;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using System.Text;

namespace CryptoScanBot.Core.Telegram;
public class TelegramShowBarometer
{
    public static void ShowBarometer(string arguments, StringBuilder stringbuilder)
    {
        //string text = "\\\Bla /Bla , Bla, hello 'h'";
        string quote = "USDT";
        string[] parameters = arguments.Split(' ');
        if (parameters.Length > 1)
            quote = parameters[1].Trim().ToUpper();

        stringbuilder.AppendLine(string.Format("Barometer {0}", quote));

        // Even een quick fix voor de barometer
        if (GlobalData.Settings.QuoteCoins.TryGetValue(quote, out CryptoQuoteData? quoteData))
        {
            for (CryptoIntervalPeriod intervalPeriod = CryptoIntervalPeriod.interval5m; intervalPeriod <= CryptoIntervalPeriod.interval1d; intervalPeriod++)
            {
                if (intervalPeriod == CryptoIntervalPeriod.interval5m || intervalPeriod == CryptoIntervalPeriod.interval15m || intervalPeriod == CryptoIntervalPeriod.interval30m ||
                     intervalPeriod == CryptoIntervalPeriod.interval1h || intervalPeriod == CryptoIntervalPeriod.interval4h || intervalPeriod == CryptoIntervalPeriod.interval1d)
                {
                    BarometerData? barometerData = GlobalData.ActiveAccount!.Data.GetBarometer(quoteData.Name, intervalPeriod);
                    stringbuilder.AppendLine(string.Format("{0} {1:N2}", intervalPeriod, barometerData.PriceBarometer));
                }
            }
        }
    }
}
