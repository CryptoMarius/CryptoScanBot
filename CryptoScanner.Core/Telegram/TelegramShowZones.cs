using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

using System.Text;

namespace CryptoScanner.Core.Telegram;

public class TelegramShowZones
{
    public static bool Execute(string arguments, StringBuilder builder)
    {
        int zoneCount = 10;
        string[] parameters = arguments.Split(' ');
        if (parameters.Length > 1)
        {
            if (!int.TryParse(parameters[1].Trim(), out zoneCount))
                zoneCount = 10;
        }

        if (zoneCount > 50)
            zoneCount = 50;

        builder.AppendLine($"Zones top {zoneCount}");

        var exchange = GlobalData.ActiveExchange;
        if (exchange != null)
        {
            SortedList<decimal, (CryptoTradeSide side, CryptoSymbol symbol)> list = [];
            foreach (var symbol in exchange.SymbolListName.Values)
            {
                decimal? distance = ZoneTools.ZoneDistance(symbol);
                CryptoTradeSide? side = ZoneTools.ZoneTradeSide(symbol);
                if (distance != null && distance != 100 && side != null)
                {
                    list.Add(distance.Value, (side.Value, symbol));
                }
            }


            if (list.Count == 0)
            {
                builder.AppendLine("not calculated?");
            }
            else
            {
                foreach (var zone in list)
                {
                    zoneCount--;
                    if (zoneCount < 0)
                        break;

                    var symbol = zone.Value.symbol;
                    string c = zone.Value.side == CryptoTradeSide.Long ? "green" : "red";

                    var interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
                    //string text = Settings.CryptoExternalUrlList.GetTradingAppName(GlobalData.Settings.General.TradingApp, symbol.Exchange.Name);
                    (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(GlobalData.Settings.General.TradingApp, true, symbol, interval);
                    if (Url == "")
                        builder.Append($"{symbol.Name}");
                    else
                        builder.Append($"<a href='{Url}'>{symbol.Name}</a>");


                    builder.Append(' ');
                    builder.Append($"{zone.Key:N2}");
                    builder.Append(' ');

                    if (zone.Value.side == CryptoTradeSide.Long)
                        builder.Append("\U0001f7e2");
                    else
                        builder.Append("\U0001F534");

                    builder.AppendLine();

                }
            }
        }

        return true;
    }
}
