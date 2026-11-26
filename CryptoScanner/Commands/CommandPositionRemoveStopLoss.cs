using Avalonia.Threading;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Commands;

/// <summary>
/// Manual override: removes the stop loss of an open position without closing it (same action as
/// the Blazor GridCommandService.PositionRemoveStopLoss). Sets Position.StopLossDisabled and
/// immediately runs HandlePosition so the current exit order (stop + take profit combined on one
/// order) is replaced by a take-profit-only order right away.
/// </summary>
public class CommandPositionRemoveStopLoss : CommandBase
{
    public override bool CanExecute(object? parameter)
    {
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null && dto.position != null)
        {
            return dto.position.CloseTime == null && !dto.position.StopLossDisabled;
        }
        return false;
    }

    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null && dto.position != null)
        {
            try
            {
                var position = dto.position;

                using CryptoDatabase databaseThread = new();
                databaseThread.Connection.Open();

                // Controleer de orders, en herbereken het geheel
                PositionTools.LoadPosition(databaseThread, position);
                await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position, forceCalculation: true);

                position.StopLossDisabled = true;
                databaseThread.Connection.Update<CryptoPosition>(position);

                var symbolPeriod = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                if (symbolPeriod.CandleList.Count > 0)
                {
                    var lastCandle1m = symbolPeriod.CandleList.Values.Last();
                    using PositionMonitor positionMonitor = new(position.Symbol, lastCandle1m);
                    await positionMonitor.HandlePosition(position);
                }

                GlobalData.AddTextToLogTab($"{position.Symbol.Name} position {position.Id} manually removed the stop loss (position stays open)");

                if (dto.PositionViewModel != null)
                    await Dispatcher.UIThread.InvokeAsync(dto.PositionViewModel.Refresh);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"error removing stop loss {dto.symbol.Name} {error.Message}");
            }
        }
    }
}
