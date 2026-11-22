using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>reset: restart the scanner (the refresh of the scanner session).</summary>
public class TelegramCommandReset : TelegramCommand
{
    public override string Name => "reset";
    public override string Description => "reset the scanner (restart)";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        IScannerSession _scannerSession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("IScannerSession not registered in services");
        _scannerSession.ScheduleRefresh();
        context.Output.AppendLine("Scheduled restart of the scanner");
        return Task.CompletedTask;
    }
}
