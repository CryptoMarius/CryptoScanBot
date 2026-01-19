using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Commands;

public class CommandPositionCalculate : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"CommandPositionCalculate");
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null && dto.position != null)
        {
            try
            {

                // Implement your external program logic here
                System.Diagnostics.Debug.WriteLine($"Opening {dto.symbol.Name} in internal program");

                using CryptoDatabase databaseThread = new();
                databaseThread.Connection.Open();

                // Controleer de orders, en herbereken het geheel
                PositionTools.LoadPosition(databaseThread, dto.position);
                await TradeTools.CalculatePositionResultsViaOrders(databaseThread, dto.position, forceCalculation: true);

                //Grid.InvalidateRow(rowIndex);
                GlobalData.AddTextToLogTab($"{dto.position.Symbol.Name} handmatig positie {dto.position.Id} herberekend");

                // i'm afraid the view wil not be updated ...
                // We need a reference to the view model to update the binding (still there, but need to parse the damned parameter again)
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"error calculating position {dto.symbol.Name} {error.Message}");
            }
        }
    }
}