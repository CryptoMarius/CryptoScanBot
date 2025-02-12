using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;
using CryptoScanBot.Core.Zones;

using System.Text;

namespace CryptoScanBot.Core.Telegram;

public class TelegramShowZones
{
    public static void Execute(string arguments, StringBuilder stringbuilder)
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

        stringbuilder.AppendLine($"Zones top {zoneCount}");

        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {
            SortedList<Decimal, CryptoSymbol> list = [];
            foreach (var symbol in exchange.SymbolListName.Values)
            {
                decimal? distance = ZoneTools.ZoneDistance(symbol);
                if (distance != null && distance != 100)
                    list.Add(distance.Value, symbol);
            }

            foreach (var zone in list)
            {
                zoneCount--;
                if (zoneCount < 0)
                    break;
                stringbuilder.AppendLine($"{zone.Value.Name} {zone.Key:N2}");
            }
        }
    }
}
