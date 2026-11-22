using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper;

using System.Text;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>profits: the positions closed today and what they made.</summary>
public class TelegramCommandProfits : TelegramCommand
{
    public override string Name => "profits";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Overview;
    public override string Description => "show the profits of the positions closed today";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        Show(context.Output);
        return Task.CompletedTask;
    }


    internal static void Show(StringBuilder stringbuilder)
    {
        var exchange = GlobalData.ActiveExchange ?? throw new InvalidOperationException("no active exchange");

        decimal sumInvested = 0;
        decimal sumProfit = 0;
        decimal sumPositions = 0;

        using CryptoDatabase databaseThread = new();
        databaseThread.Open();

        // status=2 is CryptoPositionStatus.Ready, a closed position
        foreach (CryptoPosition position in databaseThread.Connection.Query<CryptoPosition>("select * from position " +
            "where CloseTime >= @fromDate and status=2 and exchangeid=@exchangeid",
            new { fromDate = DateTime.Today.ToUniversalTime(), exchangeid = exchange.Id }))
        {
            sumPositions++;
            sumProfit += position.Profit;
            sumInvested += position.Invested;
        }

        decimal percentage = 0;
        if (sumInvested > 0)
            percentage = 100 * sumProfit / sumInvested;

        stringbuilder.AppendLine($"{sumPositions} positions, invested {sumInvested:N2}, profits {sumProfit:N2}, {percentage:N2}%");
    }
}
