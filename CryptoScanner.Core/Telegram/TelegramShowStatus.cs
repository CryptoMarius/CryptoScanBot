using CryptoScanner.Core.Core;

using System.Text;

namespace CryptoScanner.Core.Telegram;
public class TelegramShowStatus
{
    public static void ShowStatus(string arguments, StringBuilder stringbuilder)
    {
        if (arguments.Length != 1000)
            stringbuilder.AppendLine("not supported");

        //Bot status
        if (GlobalData.Settings.Options.TraderActive)
            stringbuilder.AppendLine($"Trade bot is active! (slots long={GlobalData.Settings.Trading.SlotsMaximalLong}, slots short={GlobalData.Settings.Trading.SlotsMaximalShort})");
        else
            stringbuilder.AppendLine("Trade bot is not active!");


        // Create signals
        if (GlobalData.Settings.Options.AnalyzerActive)
            stringbuilder.AppendLine("Signal bot is active!");
        else
            stringbuilder.AppendLine("Signal bot is not active!");

        //// Create sound
        //if (GlobalData.Settings.Signal.SoundSignalNotification)
        //    stringbuilder.AppendLine("Signal sound is active!");
        //else
        //    stringbuilder.AppendLine("Signal sound is not active!");

        // Trade sound
        if (GlobalData.Settings.General.SoundTradeNotification)
            stringbuilder.AppendLine("Trade sound is active!");
        else
            stringbuilder.AppendLine("Trade sound is not active!");
    }

}
