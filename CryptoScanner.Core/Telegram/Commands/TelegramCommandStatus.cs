using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>status: which parts of the scanner are switched on.</summary>
public class TelegramCommandStatus : TelegramCommand
{
    public override string Name => "status";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Overview;
    public override string Description => "show the status of trader, signals and sounds";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        var output = context.Output;

        //Bot status
        if (GlobalData.Settings.Trading.Active)
            output.AppendLine($"Trader is active (slots long={GlobalData.Settings.Trading.SlotsMaximalLong}, slots short={GlobalData.Settings.Trading.SlotsMaximalShort})");
        else
            output.AppendLine("Trader is not active");

        // Create signals
        if (GlobalData.Settings.Signal.Active)
            output.AppendLine("Signals are active");
        else
            output.AppendLine("Signals are not active");

        //// Create sound
        //if (GlobalData.Settings.Signal.SoundSignalNotification)
        //    stringbuilder.AppendLine("Signal sound is active!");
        //else
        //    stringbuilder.AppendLine("Signal sound is not active!");
        // The setting that "start sound" / "stop sound" and the sound icon of the dashboard switch
        if (GlobalData.Settings.Signal.SoundsActive)
            output.AppendLine("Signal sound is active");
        else
            output.AppendLine("Signal sound is not active");

        // Trade sound
        if (GlobalData.Settings.General.SoundTradeNotification)
            output.AppendLine("Trade sound is active");
        else
            output.AppendLine("Trade sound is not active");

        return Task.CompletedTask;
    }
}
