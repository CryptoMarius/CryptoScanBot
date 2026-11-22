using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>slots [long|short count]: show or change the maximum number of positions per side.</summary>
public class TelegramCommandSlots : TelegramCommand
{
    public override string Name => "slots";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Trading;
    public override string Arguments => "[long|short count]";
    public override string Description => "show or change the maximum number of positions per side";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        var output = context.Output;
        string side = context.Argument(0);

        // Without arguments the command used to answer nothing at all
        if (side == "")
        {
            output.AppendLine($"Slots long = {GlobalData.Settings.Trading.SlotsMaximalLong}, slots short = {GlobalData.Settings.Trading.SlotsMaximalShort}");
            return Task.CompletedTask;
        }

        bool slotsLong = Is(side, "long");
        bool slotsShort = Is(side, "short");
        if (!slotsLong && !slotsShort)
        {
            output.AppendLine($"Unknown side '{side}', use slots long|short count");
            return Task.CompletedTask;
        }

        // int.Parse used to throw on a typo, which left the chat without an answer
        if (!int.TryParse(context.Argument(1), out int slots) || slots < 0)
        {
            output.AppendLine("Not a valid number!");
            return Task.CompletedTask;
        }

        if (slotsLong)
        {
            GlobalData.Settings.Trading.SlotsMaximalLong = slots;
            output.AppendLine($"Slots long = {slots}");
        }
        else
        {
            GlobalData.Settings.Trading.SlotsMaximalShort = slots;
            output.AppendLine($"Slots short = {slots}");
        }
        SettingsChanged();
        return Task.CompletedTask;
    }
}
