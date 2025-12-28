using CryptoScanner.Core.Core;

using System.Text;

namespace CryptoScanner.Core.Telegram;

public class TelegramBotStop
{
    public static void Execute(string arguments, StringBuilder stringbuilder)
    {
        bool sound = false;
        //bool balanceBot = false;
        bool signalsBot = false;
        //bool adviceOnly = false;
        string[] parameters = arguments.Split(' ');
        if (parameters.Length > 1)
        {
            sound = parameters[1].Trim().ToLower().Equals("sound");
            //adviceOnly = parameters[1].Trim().ToLower().Equals("advice");
            signalsBot = parameters[1].Trim().ToLower().Equals("signals");
            //balanceBot = parameters[1].Trim().ToLower().Equals("balancing");
        }

        if (sound)
        {
            if (GlobalData.Settings.Options.SoundsActive)
            {
                GlobalData.Settings.Options.SoundsActive = false;
                stringbuilder.AppendLine("Sound stopped!");
                GlobalData.SaveSettings();
                GlobalData.TelegramHasChanged("");
            }
            else
                stringbuilder.AppendLine("Sound is already inactive!");
        }
        //else if (balanceBot)
        //{
        //    if (GlobalData.Settings.BalanceBot.Active)
        //    {
        //        GlobalData.Settings.BalanceBot.Active = false;
        //        stringbuilder.AppendLine("Balance bot stopped!");
        //        GlobalData.SaveSettings();
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
        //        GlobalData.SaveSettings();
        //GlobalData.TelegramHasChanged("");
        //    }
        //    else
        //        stringbuilder.AppendLine("Balance bot advice only inactive!");
        //}
        else if (signalsBot)
        {
            if (GlobalData.Settings.Options.AnalyzerActive)
            {
                // TODO: User interface ook updaten
                GlobalData.Settings.Options.AnalyzerActive = false;
                stringbuilder.AppendLine("Signal bot stopped!");
                GlobalData.SaveSettings();
                GlobalData.TelegramHasChanged("");
            }
            else
                stringbuilder.AppendLine("Signal bot already inactive!");
        }
        else
        {
            if (GlobalData.Settings.Options.TraderActive)
            {
                // TODO: User interface ook updaten
                GlobalData.Settings.Options.TraderActive = false;
                stringbuilder.AppendLine("Bot stopped!");
                GlobalData.SaveSettings();
                GlobalData.TelegramHasChanged("");
            }
            else
                stringbuilder.AppendLine("Bot already inactive!");
        }
    }
}
