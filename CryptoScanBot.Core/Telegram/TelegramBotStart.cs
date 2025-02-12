using CryptoScanBot.Core.Core;

using System.Text;

namespace CryptoScanBot.Core.Telegram;

public class TelegramBotStart
{
    public static void Execute(string arguments, StringBuilder stringbuilder)
    {
        bool soundSignal = false;
        //bool balanceBot = false;
        bool signalsBot = false;
        //bool adviceOnly = false;
        bool tradingBot = false;
        string[] parameters = arguments.Split(' ');
        if (parameters.Length > 1)
        {
            soundSignal = parameters[1].Trim().ToLower().Equals("sound");
            //adviceOnly = parameters[1].Trim().ToLower().Equals("advice");
            signalsBot = parameters[1].Trim().ToLower().Equals("signals");
            tradingBot = parameters[1].Trim().ToLower().Equals("trading");
            //balanceBot = parameters[1].Trim().ToLower().Equals("balancing");
        }

        if (soundSignal)
        {
            if (!GlobalData.Settings.Signal.SoundsActive)
            {
                GlobalData.Settings.Signal.SoundsActive = true;
                stringbuilder.AppendLine("Sound started!");
                GlobalData.SaveSettings();
                GlobalData.TelegramHasChanged("");
            }
            else
                stringbuilder.AppendLine("Sound is already active!");
        }
        //else if (balanceBot)
        //{
        //    if (!GlobalData.Settings.BalanceBot.Active)
        //    {
        //        GlobalData.Settings.BalanceBot.Active = true;
        //        stringbuilder.AppendLine("Balance bot started!");
        //        GlobalData.SaveSettings();
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
        //        GlobalData.SaveSettings();
        //GlobalData.TelegramHasChanged("");
        //    }
        //    else
        //        stringbuilder.AppendLine("Balance bot advice only already active!");
        //}
        else if (signalsBot)
        {
            if (!GlobalData.Settings.Signal.Active)
            {
                GlobalData.Settings.Signal.Active = true;
                stringbuilder.AppendLine("Signal bot started!");
                GlobalData.SaveSettings();
                GlobalData.TelegramHasChanged("");
            }
            else
                stringbuilder.AppendLine("Signal bot already active!");
        }
        else if (tradingBot)
        {
            if (!GlobalData.Settings.Trading.Active)
            {
                GlobalData.Settings.Trading.Active = true;
                stringbuilder.AppendLine("Trading bot started!");
                GlobalData.SaveSettings();
                GlobalData.TelegramHasChanged("");
            }
            else
                stringbuilder.AppendLine("Trading bot already active!");
        }
        else
        {
            if (!GlobalData.Settings.Trading.Active)
            {
                GlobalData.Settings.Trading.Active = true;
                stringbuilder.AppendLine("Bot started!");
                GlobalData.SaveSettings();
                GlobalData.TelegramHasChanged("");
            }
            else
                stringbuilder.AppendLine("Bot already active!");
        }
    }
}
