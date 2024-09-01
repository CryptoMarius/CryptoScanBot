using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using System.Text;

namespace CryptoScanBot.Core.Telegram;
public class TelegramShowValue
{
    public static void ShowValue(string arguments, StringBuilder stringbuilder)
    {
        //string value;
        //string[] parameters = arguments.Split(' ');
        //if (parameters.Length >= 2)
        //    value = parameters[1];
        //else
        //    value = GlobalData.Settings.ShowSymbolInformation.ToList();
        //        //"BTCUSDT,ETHUSDT,PAXGUSDT,BNBUSDT";
        //parameters = value.Split(',');
        var parameters = GlobalData.Settings.ShowSymbolInformation;

        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange? exchange))
        {
            foreach (string symbolName in parameters)
            {
                if (exchange.SymbolListName.TryGetValue(symbolName + "USDT", out CryptoSymbol? symbol))
                {
                    if (symbol.LastPrice.HasValue)
                    {
                        string text = string.Format("{0} waarde {1:N2}", symbolName, (decimal)symbol.LastPrice);
                        stringbuilder.AppendLine(text);
                    }
                }

            }
        }
    }

}
