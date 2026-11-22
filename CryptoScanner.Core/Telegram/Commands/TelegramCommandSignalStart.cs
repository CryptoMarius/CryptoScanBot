namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>signalstart: the short form of "start signals".</summary>
public class TelegramCommandSignalStart : TelegramCommand
{
    public override string Name => "signalstart";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Trading;
    public override string Description => "switch the signals on (same as start signals)";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        TelegramCommandStart.Start("signals", context.Output);
        return Task.CompletedTask;
    }
}
