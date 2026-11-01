using Avalonia.Controls;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;
using CryptoScanner.ViewModels;
using CryptoScanner.Views;

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

            // Every selected row (the grid allows a multi selection), the clicked one when the
            // selection somehow does not contain it.
            List<CryptoPosition> positions = [];
            if (dto.datagrid != null)
            {
                foreach (var item in dto.datagrid.SelectedItems.OfType<PositionViewModel>())
                    positions.Add(item.Object);
            }
            if (!positions.Contains(dto.position))
                positions = [dto.position];

            if (dto.parentWindow != null)
            {
                var dialog = new ConfirmDialog(PositionTools.DeleteConfirmationText(positions),
                    positions.Count == 1 ? "Delete position" : "Delete positions")
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                var confirmed = await dialog.ShowDialog<bool?>(dto.parentWindow);
                if (confirmed != true)
                    return;
            }

            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();
            foreach (CryptoPosition position in positions)
            {
                try
                {
                    PositionTools.LoadPosition(databaseThread, position);

                    // Steps, parts, the position itself AND the orders and trades that hang off it
                    PositionTools.DeleteFromDatabase(databaseThread, position);

                    position.Symbol.LastTradeDate = null;
                    position.Symbol.LastLossDate = null;
                    GlobalData.ThreadSaveObjects!.AddToQueue(position.Symbol);

                    // Remove the position from open or closed positions
                    GlobalData.SendMvvmMessage(new PositionIsDeletedMessage(position));
                    PositionTools.RemovePosition(GlobalData.ActiveExchange!, position, false);

                    // The position is gone, so what it did to the balances has to go with it. After
                    // RemovePosition on purpose: the reservation of its open orders is then already
                    // released, so the free balance comes out right in one go.
                    PaperAssets.ReversePosition(GlobalData.ActiveExchange!, position);
                    GlobalData.AddTextToLogTab($"{position.Symbol.Name} manually deleted position {position.Id} from the database");
                }
                catch (Exception error)
                {
                    ScannerLog.Logger.Error(error, "");
                    GlobalData.AddTextToLogTab($"error deleting position {position.Id} {error.Message}");
                }
            }
        }
    }
}
