using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Trader;

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
        System.Diagnostics.Debug.WriteLine($"CommandPositionDelete");
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

                // Steps, parts, the position itself AND the orders and trades that hang off it
                PositionTools.DeleteFromDatabase(databaseThread, dto.position);

                dto.position.Symbol.LastTradeDate = null;
                dto.position.Symbol.LastLossDate = null;
                GlobalData.ThreadSaveObjects!.AddToQueue(dto.position.Symbol);

                // Remove the position from open or closed positions
                GlobalData.SendMvvmMessage(new PositionIsDeletedMessage(dto.position));
                PositionTools.RemovePosition(GlobalData.ActiveExchange!, dto.position, false);

                // The position is gone, so what it did to the balances has to go with it. After
                // RemovePosition on purpose: the reservation of its open orders is then already
                // released, so the free balance comes out right in one go.
                PaperAssets.ReversePosition(GlobalData.ActiveExchange!, dto.position);
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
