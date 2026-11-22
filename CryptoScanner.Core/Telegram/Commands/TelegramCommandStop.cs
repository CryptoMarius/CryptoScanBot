using CryptoScanner.Core.Core;

using System.Text;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>stop [trading|signals|sound]; without an argument the trader is stopped.</summary>
public class TelegramCommandStop : TelegramCommand
{
    public override string Name => "stop";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Trading;
    public override string Arguments => "[trading|signals|sound]";
    public override string Description => "switch the trader (default), the signals or the signal sound off";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        // A bare "stop" keeps stopping the trader: stopping is the safe direction, and in a hurry
        // one word is what you type.
        string target = context.Argument(0);
        if (target == "")
            target = "trading";
        Stop(target, context.Output);
        return Task.CompletedTask;
    }


    internal static void Stop(string target, StringBuilder stringbuilder)
    {
        //bool balanceBot = false;
        //bool adviceOnly = false;
        //adviceOnly = parameters[1].Trim().ToLower().Equals("advice");
        //balanceBot = parameters[1].Trim().ToLower().Equals("balancing");

        if (Is(target, "sound"))
        {
            if (GlobalData.Settings.Signal.SoundsActive)
            {
                GlobalData.Settings.Signal.SoundsActive = false;
                stringbuilder.AppendLine("Sound stopped");
                SettingsChanged();
            }
            else
                stringbuilder.AppendLine("Sound is already inactive");
        }
        //else if (balanceBot)
        //{
        //    if (GlobalData.Settings.BalanceBot.Active)
        //    {
        //        GlobalData.Settings.BalanceBot.Active = false;
        //        stringbuilder.AppendLine("Balance bot stopped!");
        //        GlobalData.SaveConfiguration();
        //GlobalData.TelegramHasChanged("");
        //    }
        //    else
        //        stringbuilder.AppendLine("Balance bot already inactive!");
        //}
        //else if (adviceOnly)
        //{
        //    if (GlobalData.Settings.BalanceBot.ShowAdviceOnly)
        //    {
        //        GlobalData.Settings.BalanceBot.ShowAdviceOnly = false;
        //        stringbuilder.AppendLine("Balance bot advice only stopped!");
        //        GlobalData.SaveConfiguration();
        //GlobalData.TelegramHasChanged("");
        //    }
        //    else
        //        stringbuilder.AppendLine("Balance bot advice only inactive!");
        //}
        else if (Is(target, "signals"))
        {
            if (GlobalData.Settings.Signal.Active)
            {
                // TODO: User interface ook updaten
                // (done: SettingsChanged sends the StatusesHaveChangedMessage both scanners listen to)
                GlobalData.Settings.Signal.Active = false;
                stringbuilder.AppendLine("Signals stopped");
                SettingsChanged();
            }
            else
                stringbuilder.AppendLine("Signals are already inactive");
        }
        else if (Is(target, "trading"))
        {
            if (GlobalData.Settings.Trading.Active)
            {
                // TODO: User interface ook updaten
                // (done: SettingsChanged sends the StatusesHaveChangedMessage both scanners listen to)
                GlobalData.Settings.Trading.Active = false;
                stringbuilder.AppendLine("Trader stopped");
                SettingsChanged();
            }
            else
                stringbuilder.AppendLine("Trader is already inactive");
        }
        else
            // An unknown word used to stop the trader; a typo now changes nothing
            stringbuilder.AppendLine($"Unknown argument '{target}', use stop trading|signals|sound");
    }
}
