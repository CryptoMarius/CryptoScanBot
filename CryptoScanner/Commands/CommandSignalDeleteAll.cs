using Avalonia.Controls;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Views;

using Dapper;

namespace CryptoScanner.Commands;

public class CommandSignalDeleteAll : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"CommandSignalDeleteAll");
        if (!GetObjectInformation(parameter, out ParameterObjects dto) || dto.parentWindow == null)
            return;

        var dialog = new ConfirmDialog("Delete all signals from the database?", "Delete signals")
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
            databaseThread.Connection.Execute("delete from signal", transaction);
            transaction.Commit();

            foreach (CryptoSymbol symbol in GlobalData.ActiveExchange!.SymbolListId.Values)
            {
                foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
                {
                    symbolInterval.SignalList.Clear();
                }
            }

            GlobalData.SendMvvmMessage(new SignalDeleteAllMessage());
            GlobalData.AddTextToLogTab("Manually deleted all signals from the database");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"Error deleting signals: {error.Message}");
        }
    }
}
