using Avalonia.Threading;

using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;

using Dapper;

namespace CryptoScanner.Commands;

public class CommandPositionDelete : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"CommandShowGraph");
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null && dto.position != null)
        {
            // TODO: Confirm dialog
            //if (MessageBox.Show($"Delete position {dto.position.Symbol.Name}", "Delete position?", MessageBoxButtons.YesNo) != DialogResult.Yes)
            //    return;
            try
            {
                using CryptoDatabase databaseThread = new();
                databaseThread.Connection.Open();
                PositionTools.LoadPosition(databaseThread, dto.position);

                using var transaction = databaseThread.BeginTransaction();
                databaseThread.Connection.Execute($"delete from positionstep where positionid={dto.position.Id}", transaction);
                databaseThread.Connection.Execute($"delete from positionpart where positionid={dto.position.Id}", transaction);
                databaseThread.Connection.Execute($"delete from position where id={dto.position.Id}", transaction);
                transaction.Commit();

                // Remove the position from open or closed positions
                Dispatcher.UIThread.Post(() => { WeakReferenceMessenger.Default.Send(new PositionIsDeletedMessage(dto.position)); });
                PositionTools.RemovePosition(GlobalData.ActiveExchange!, dto.position, false);
                GlobalData.AddTextToLogTab($"{dto.position.Symbol.Name} manually deleted position {dto.position.Id} from the database");
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"error deleting position {dto.position.Id} {error.Message}");
            }
        }
    }
}
