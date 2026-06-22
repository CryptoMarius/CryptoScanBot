using Avalonia.Threading;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Commands;

public class CommandPositionCreateAdditionalDca : CommandBase
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
        System.Diagnostics.Debug.WriteLine($"CommandPositionCreateAdditionalDca");
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
                //FillItemOpen(position, item);

                // Op welke prijs? Actueel, of nog X% eronder?
                //TradeTools.
                // todo...
                //TradeTools.CalculatePositionResultsViaTrades(databaseThread, position);
                //FillItemOpen(position, item);

                //decimal adjust = GlobalData.Settings.Trading.DcaPercentage * step.SignalPrice / 100m;

                if (position.Symbol.LastPrice.HasValue)
                {

                    // Corrigeer de prijs indien de koers ondertussen lager of hoger ligt
                    decimal price = (decimal)position.Symbol.LastPrice;
                    if (position.Side == CryptoTradeSide.Long)
                    {
                        //price = step.SignalPrice - adjust;
                        if (position.Symbol.LastPrice.HasValue && position.Symbol.LastPrice < price)
                            price = (decimal)position.Symbol.LastPrice - position.Symbol.PriceTickSize;
                    }
                    else
                    {
                        //price = step.SignalPrice + adjust;
                        if (position.Symbol.LastPrice.HasValue && position.Symbol.LastPrice > price)
                            price = (decimal)position.Symbol.LastPrice + position.Symbol.PriceTickSize;
                    }


                    // Zo laat mogelijk controleren vanwege extra calls naar de exchange
                    //var resultCheckAssets = await CheckApiAndAssetsAsync(position.TradeAccount);
                    //if (!resultCheckAssets.success)
                    //{
                    //    string text = $"{position.Symbol.Name} + DCA bijplaatsen op {price.ToString0(position.Symbol.PriceFormat)}";
                    //    GlobalData.AddTextToLogTab(text + " " + resultCheckAssets.reaction);
                    //    Symbol.ClearSignals();
                    //    return;
                    //}


                    // De positie uitbreiden nalv een nieuw signaal (de xe bijkoop wordt altijd een aparte DCA)
                    PositionTools.ExtendPosition(databaseThread, position, CryptoPartPurpose.Dca, position.Interval!, position.Strategy,
                        CryptoEntryOrDcaStrategy.FixedPercentage, price, GlobalData.Clock.UtcNow, true);
                    GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig een DCA toegevoegd aan positie {position.Id}");

                    //Grid.InvalidateRow(rowIndex);


                    // Er is een 1m candle gearriveerd, acties adhv deze candle..
                    var symbolPeriod = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                    if (symbolPeriod.CandleList.Count > 0)
                    {
                        var lastCandle1m = symbolPeriod.CandleList.Values.Last();
                        PositionMonitor positionMonitor = new(position.Symbol, lastCandle1m);
                        await positionMonitor.HandlePosition(position);
                    }

                    // Refresh() raises INotifyPropertyChanged, which Avalonia's bindings must receive on the UI thread.
                    // This command runs fire-and-forget on a background thread, so the call needs to be dispatched explicitly.
                    if (dto.PositionViewModel != null)
                        await Dispatcher.UIThread.InvokeAsync(dto.PositionViewModel.Refresh);
                }

            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"error removing dca {dto.symbol.Name} {error.Message}");
            }
        }
    }
}
