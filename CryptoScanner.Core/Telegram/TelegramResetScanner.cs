using CryptoScanner.Core.Core;

using System.Text;

namespace CryptoScanner.Core.Telegram;

public class TelegramResetScanner
{
    public static void Execute(string arguments, StringBuilder stringbuilder)
    {
        ScannerSession.ScheduleRefresh();
        stringbuilder.AppendLine("Scheduled restart of the scanner");
    }

}
