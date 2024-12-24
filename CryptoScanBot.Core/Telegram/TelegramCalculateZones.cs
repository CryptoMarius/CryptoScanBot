using CryptoScanBot.Core.Zones;

using System.Text;

namespace CryptoScanBot.Core.Telegram;

public class TelegramCalculateZones
{
    public static void Execute(string arguments, StringBuilder stringbuilder)
    {
        LiquidityZones.CalculateZonesForAllSymbolsAsync(null);
        stringbuilder.AppendLine("Started calculations of zones");
    }

}
