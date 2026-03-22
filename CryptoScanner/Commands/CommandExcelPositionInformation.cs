using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Excel;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Commands;

public class CommandExcelPositionInformation : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"CommandExcelPositionInformation");
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null && dto.position != null)
        {
            var position = dto.position;
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();
            if (position.Status >= CryptoPositionStatus.Ready)
            {
                PositionTools.LoadPosition(databaseThread, position);
            }
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} position {position.Id} manual for Excel");
            await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position, forceCalculation: true);
            _ = Task.Run(() => { new ExcelPositionDump(position).ExportToExcel(); });
        }
    }
}
