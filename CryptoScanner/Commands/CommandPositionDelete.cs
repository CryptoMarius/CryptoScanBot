using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;

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
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null && dto.position != null)
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

                // TODO: remove from observable collection
                //List.Remove((T)dto.position);
                PositionTools.RemovePosition(GlobalData.ActiveExchange!, dto.position, false);
                GlobalData.AddTextToLogTab($"{dto.position.Symbol.Name} handmatig positie {dto.position.Id} uit de database verwijderd");
                GlobalData.PositionsHaveChanged("");
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"error deleting position {dto.position.Id} {error.Message}");
            }
        }
    }
}
