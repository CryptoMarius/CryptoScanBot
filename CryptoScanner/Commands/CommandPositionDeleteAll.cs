using Avalonia.Controls;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Trader;
using CryptoScanner.Views;

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

        var dialog = new ConfirmDialog(
            "Delete all positions from the database, and hand out the start capital again?", "Delete positions")
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

            // Steps, parts, the positions themselves AND the orders and trades that hang off them
            PositionTools.DeleteAllFromDatabase(databaseThread);

            GlobalData.ActiveExchange!.Data.PositionList.Clear();

            foreach (var symbol in GlobalData.ActiveExchange.SymbolListId.Values)
            {
                symbol.LastTradeDate = null;
                symbol.LastLossDate = null;
                GlobalData.ThreadSaveObjects!.AddToQueue(symbol);
            }

            // Remove the position from open or closed positions
            GlobalData.SendMvvmMessage(new PositionDeleteAllMessage());
            GlobalData.AddTextToLogTab($"Manually deleted all positions from the database");

            // The balances carry the result of the positions that were just deleted, so they have to
            // go back to the start as well - see PaperAssetsEditor.ResetAfterDeletingAllPositions.
            PaperAssetsEditor.ResetAfterDeletingAllPositions(GlobalData.ActiveExchange);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"error deleting positions {error.Message}");
        }
    }
}
