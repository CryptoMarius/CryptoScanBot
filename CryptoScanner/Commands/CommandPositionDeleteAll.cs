using Avalonia.Controls;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Trader;
using CryptoScanner.Views;

using Dapper;

namespace CryptoScanner.Commands;

public class CommandPositionDeleteAll : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"CommandPositionDeleteAll");
        if (!GetObjectInformation(parameter, out ParameterObjects dto) || dto.parentWindow == null)
            return;

        var dialog = new ConfirmDialog("Delete all positions from the database?", "Delete positions")
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var confirmed = await dialog.ShowDialog<bool?>(dto.parentWindow);
        if (confirmed != true)
            return;

        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            using var transaction = databaseThread.BeginTransaction();
            databaseThread.Connection.Execute($"delete from positionstep", transaction);
            databaseThread.Connection.Execute($"delete from positionpart", transaction);
            databaseThread.Connection.Execute($"delete from position", transaction);
            transaction.Commit();

            // Remove the position from open or closed positions
            GlobalData.SendMvvmMessage(new PositionDeleteAllMessage());
            GlobalData.AddTextToLogTab($"Manually deleted all positions from the database");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"error deleting positions {error.Message}");
        }
    }
}
