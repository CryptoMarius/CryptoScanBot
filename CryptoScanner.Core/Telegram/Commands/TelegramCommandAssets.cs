using CryptoScanner.Core.Core;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>assets: the balances and their value.</summary>
public class TelegramCommandAssets : TelegramCommand
{
    public override string Name => "assets";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Overview;
    public override string Description => "show the assets and their value";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        var exchange = GlobalData.ActiveExchange ?? throw new InvalidOperationException("no active exchange");

        // The reason used to be thrown away, so a list that was not refreshed looked current
        (bool success, string reaction) = AssetTools.FetchAssets(exchange);
        if (!success && reaction != "")
            context.Output.AppendLine($"Note: {reaction}, the list below may be out of date");

        Helper.ShowAssets(exchange, context.Output, out decimal _, out decimal _);
        return Task.CompletedTask;
    }
}
