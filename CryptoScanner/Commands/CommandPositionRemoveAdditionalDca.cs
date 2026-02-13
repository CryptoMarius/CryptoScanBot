using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Commands;

public class CommandPositionRemoveAdditionalDca : CommandBase
{
    public override bool CanExecute(object? parameter)
    {
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null && dto.position != null)
        {
            return dto.position.CloseTime == null;
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
        System.Diagnostics.Debug.WriteLine($"CommandShowGraph");
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

                // Er is een 1m candle gearriveerd, acties adhv deze candle..
                var symbolPeriod = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                if (symbolPeriod.CandleList.Count > 0)
                {
                    var lastCandle1m = symbolPeriod.CandleList.Values.Last();
                    CandleTime lastCandle1mCloseTime = lastCandle1m.OpenTime + 1;
                    DateTime lastCandle1mCloseTimeDate = lastCandle1mCloseTime.ToDateTime();

                    PositionMonitor positionMonitor = new(position.Symbol, lastCandle1m);
                    await positionMonitor.HandlePosition(position);


                    var entryOrderSide = position.GetEntryOrderSide();
                    foreach (CryptoPositionPart part in position.PartList.Values.ToList())
                    {
                        if (!part.CloseTime.HasValue && part.Purpose == CryptoPartPurpose.Dca)
                        {
                            foreach (CryptoPositionStep step in part.StepList.Values.ToList())
                            {
                                if (!step.CloseTime.HasValue && step.Side == entryOrderSide)
                                {
                                    string cancelReason = $"annuleren vanwege handmatig annuleren DCA positie {position.Id}";
                                    var (success, _) = await TradeTools.CancelOrder(databaseThread, position, part, step,
                                        lastCandle1mCloseTimeDate, CryptoOrderStatus.ManuallyByUser, cancelReason);
                                    if (success)
                                    {
                                        part.CloseTime = DateTime.UtcNow;
                                        databaseThread.Connection.Update<CryptoPositionPart>(part);

                                        position.ActiveDca = false;
                                        databaseThread.Connection.Update<CryptoPosition>(position);

                                        GlobalData.AddTextToLogTab($"{position.Symbol.Name} positie {position.Id} handmatig de openstaande DCA {part.PartNumber} annuleren");
                                    }
                                }
                            }
                        }
                    }

                    // TODO: i'm afraid the view wil not be updated ...
                    // We need a reference to the view model to update the binding (still there, but need to parse the damned parameter again)
                    //Grid.InvalidateRow(rowIndex);
                    //if (dto.PositionViewModel != null)
                    //    dto.PositionViewModel.Refresh();
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"error adding dca {dto.symbol.Name} {error.Message}");
            }
        }
    }
}
