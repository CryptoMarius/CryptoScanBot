using CryptoScanner.Core.Core;

using System.Text;

namespace CryptoScanner.Core.Telegram;

public class TelegramBotSlots
{
    public static void Execute(string arguments, StringBuilder stringbuilder)
    {
        bool slotsLong = false;
        bool slotsShort = false;
        string[] parameters = arguments.Split(' ');
        if (parameters.Length > 1)
        {
            slotsLong = parameters[1].Trim().ToLower().Equals("long");
            slotsShort = parameters[1].Trim().ToLower().Equals("short");
        }

        if (slotsLong && parameters.Length > 2)
        {
            int slots = int.Parse(parameters[2].Trim());
            if (slots >= 0)
            {
                stringbuilder.AppendLine($"Slots long = {slots}");
                GlobalData.Settings.Trading.SlotsMaximalLong = slots;
                GlobalData.SaveSettings();
                GlobalData.TelegramHasChanged("");
            }
            else
                stringbuilder.AppendLine("Not a valid number!");
        }

        if (slotsShort && parameters.Length > 2)
        {
            int slots = int.Parse(parameters[2].Trim());
            if (slots >= 0)
            {
                stringbuilder.AppendLine($"Slots short = {slots}");
                GlobalData.Settings.Trading.SlotsMaximalShort = slots;
                GlobalData.SaveSettings();
                GlobalData.TelegramHasChanged("");
            }
            else
                stringbuilder.AppendLine("Not a valid number!");
        }
    }
}
