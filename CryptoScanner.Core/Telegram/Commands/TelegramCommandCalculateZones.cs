using CryptoScanner.Core.Zones;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>calculatezones: put every symbol in the zone calculation queue.</summary>
public class TelegramCommandCalculateZones : TelegramCommand
{
    public override string Name => "calculatezones";
    public override string Description => "calculate the zones of all symbols again";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        ZoneThreadCalculate.CalculateZonesForAllSymbolsAsync();
        context.Output.AppendLine("Started calculations of zones");
        return Task.CompletedTask;
    }
}
