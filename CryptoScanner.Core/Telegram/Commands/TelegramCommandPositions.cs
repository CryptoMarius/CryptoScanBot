using CryptoScanner.Core.Core;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>positions: the open positions, best first.</summary>
public class TelegramCommandPositions : TelegramCommand
{
    public override string Name => "positions";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Overview;
    public override string Description => "show the open positions";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        var exchange = GlobalData.ActiveExchange ?? throw new InvalidOperationException("no active exchange");
        exchange.Data.PositionList.ShowPositions(context.Output);
        return Task.CompletedTask;
    }
}
