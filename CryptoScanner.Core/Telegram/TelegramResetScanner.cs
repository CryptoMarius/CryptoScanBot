using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

using System.Text;

namespace CryptoScanner.Core.Telegram;

public class TelegramResetScanner
{
    public static void Execute(string arguments, StringBuilder stringbuilder)
    {
        IScannerSession _scannerSession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("IScannerSession not registered in services");
        _scannerSession.ScheduleRefresh();
        stringbuilder.AppendLine("Scheduled restart of the scanner");
    }

}
