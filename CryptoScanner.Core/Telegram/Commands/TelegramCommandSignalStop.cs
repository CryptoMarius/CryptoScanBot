namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>signalstop: the short form of "stop signals".</summary>
public class TelegramCommandSignalStop : TelegramCommand
{
    public override string Name => "signalstop";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Trading;
    public override string Description => "switch the signals off (same as stop signals)";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        TelegramCommandStop.Stop("signals", context.Output);
        return Task.CompletedTask;
    }
}
