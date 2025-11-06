using CryptoScanner.Core.Zones;

using System.Text;

namespace CryptoScanner.Core.Telegram;

public class TelegramCalculateZones
{
    public static void Execute(string arguments, StringBuilder stringbuilder)
    {
        ZoneThreadCalculate.CalculateZonesForAllSymbolsAsync();
        stringbuilder.AppendLine("Started calculations of zones");
    }

}
