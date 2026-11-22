using CryptoScanner.Core.Core;

using System.Text;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>start trading|signals|sound.</summary>
public class TelegramCommandStart : TelegramCommand
{
    public override string Name => "start";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Trading;
    public override string Arguments => "trading|signals|sound";
    public override string Description => "switch the trader, the signals or the signal sound on";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        // Without an argument nothing is switched on. Telegram itself sends "/start" when the chat
        // with the bot is opened for the first time or after it was cleared, and now that a leading
        // slash is accepted that must not start the trader.
        string target = context.Argument(0);
        if (target == "")
            context.Output.AppendLine($"Usage: start {Arguments}");
        else
            Start(target, context.Output);
        return Task.CompletedTask;
    }


    internal static void Start(string target, StringBuilder stringbuilder)
    {
        //bool balanceBot = false;
        //bool adviceOnly = false;
        //adviceOnly = parameters[1].Trim().ToLower().Equals("advice");
        //balanceBot = parameters[1].Trim().ToLower().Equals("balancing");

        if (Is(target, "sound"))
        {
            if (!GlobalData.Settings.Signal.SoundsActive)
            {
                GlobalData.Settings.Signal.SoundsActive = true;
                stringbuilder.AppendLine("Sound started");
                SettingsChanged();
            }
            else
                stringbuilder.AppendLine("Sound is already active");
        }
        //else if (balanceBot)
        //{
        //    if (!GlobalData.Settings.BalanceBot.Active)
        //    {
        //        GlobalData.Settings.BalanceBot.Active = true;
        //        stringbuilder.AppendLine("Balance bot started!");
        //        GlobalData.SaveConfiguration();
        //GlobalData.TelegramHasChanged("");
        //    }
        //    else
        //        stringbuilder.AppendLine("Balance bot already active!");
        //}
        //else if (adviceOnly)
        //{
        //    if (!GlobalData.Settings.BalanceBot.ShowAdviceOnly)
        //    {
        //        GlobalData.Settings.BalanceBot.ShowAdviceOnly = true;
        //        stringbuilder.AppendLine("Balance bot advice only started!");
        //        GlobalData.SaveConfiguration();
        //GlobalData.TelegramHasChanged("");
        //    }
        //    else
        //        stringbuilder.AppendLine("Balance bot advice only already active!");
        //}
        else if (Is(target, "signals"))
        {
            if (!GlobalData.Settings.Signal.Active)
            {
                GlobalData.Settings.Signal.Active = true;
                stringbuilder.AppendLine("Signals started");
                SettingsChanged();
            }
            else
                stringbuilder.AppendLine("Signals are already active");
        }
        else if (Is(target, "trading"))
        {
            if (!GlobalData.Settings.Trading.Active)
            {
                GlobalData.Settings.Trading.Active = true;
                stringbuilder.AppendLine("Trader started");
                SettingsChanged();
            }
            else
                stringbuilder.AppendLine("Trader is already active");
        }
        else
            // An unknown word used to start the trader; a typo now changes nothing
            stringbuilder.AppendLine($"Unknown argument '{target}', use start trading|signals|sound");
    }
}
