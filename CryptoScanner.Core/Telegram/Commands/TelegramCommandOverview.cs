using CryptoScanner.Core.Core;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>".": barometer, positions, profits, assets and values in one answer.</summary>
public class TelegramCommandOverview : TelegramCommand
{
    public override string Name => ".";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Overview;
    public override string Arguments => "[quote]";
    public override string Description => "overview: barometer, positions, profits, assets and values";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        var exchange = GlobalData.ActiveExchange ?? throw new InvalidOperationException("no active exchange");
        var output = context.Output;

        TelegramCommandBarometer.Show(context.Argument(0), output);
        output.AppendLine();
        exchange.Data.PositionList.ShowPositions(output);
        output.AppendLine();
        TelegramCommandProfits.Show(output);
        output.AppendLine();
        Helper.ShowAssets(exchange, output, out decimal _, out decimal _);
        output.AppendLine();
        TelegramCommandValue.Show(output);
        return Task.CompletedTask;
    }
}
