using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Excel;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Commands;

public class CommandExcelPositionInformation : CommandBase
{
    public override async void Execute(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"CommandExcelPositionInformation");
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null && dto.position != null)
        {
            var position = dto.position;
            using CryptoDatabase databaseThread = new();
            if (position.Status >= CryptoPositionStatus.Ready)
            {
                databaseThread.Open();
                PositionTools.LoadPosition(databaseThread, position);
            }
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} position {position.Id} manual for Excel");
            await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position, forceCalculation: true);
            _ = Task.Run(() => { new ExcelPositionDump(position).ExportToExcel(); });
        }
    }
}
