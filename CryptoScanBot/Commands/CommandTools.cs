using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Excel;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trader;
using CryptoScanBot.Core.Zones;
using CryptoScanBot.Intern;

namespace CryptoScanBot.Commands;

public class CommandTools
{
    public static (bool succes, Core.Model.CryptoExchange? exchange, CryptoSymbol? symbol, CryptoSignal? signal, CryptoInterval? interval, CryptoPosition? position)
        GetAttributesFromSender(object sender)
    {
        return sender switch
        {
            CryptoSymbol symbol => (true, symbol.Exchange!, symbol, null, GlobalData.IntervalList[5], null),
            CryptoSignal signal => (true, signal.Exchange!, signal.Symbol, signal, signal.Interval, null),
            CryptoPosition position => (true, position.Exchange, position.Symbol, null, position.Interval, position),
            CryptoLiveData liveData => (true, liveData.Symbol.Exchange, liveData.Symbol, null, liveData.Interval, null),
            _ => (false, null, null, null, null, null)
        };
    }

    public static async Task ExecuteCommandAsync(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItemCommand item)
        {
            // Global commands
            switch (item.Command)
            {
                case Command.About:
                    new CommandAbout().Execute(item, sender);
                    return;

                case Command.ExcelExchangeInformation:
                    // Die valt qua parameters buiten de boot
                    _ = Task.Run(() => { new ExcelExchangeDump(GlobalData.Settings.General.Exchange!).ExportToExcel(); });
                    return;

                case Command.ExcelSignalsInformation:
                    _ = Task.Run(() => { new ExcelSignalsDump().ExportToExcel(); });
                    return;
                case Command.ExcelPositionsInformation:
                    _ = Task.Run(() => { new ExcelPostionsDump().ExportToExcel(); });
                    return;

                case Command.ScannerSessionDebug:
                    ScannerSession.ScheduleRefresh();
                    return;

                case Command.TradingViewImportList:
                    CommandTradingViewImportList.ExportList();
                    return;

                case Command.CopyDataGridCells:
                    if (item.DataGrid is CryptoDataGrid dataGrid2)
                        new CommandCopyDataCells().Execute(item, dataGrid2);
                    return;

                case Command.CalculateAllLiquidityZones:
                    LiquidityZones.CalculateZonesForAllSymbolsAsync(null);
                    return;

            }


            // All the other command need an object
            if (item.DataGrid is CryptoDataGrid dataGrid && dataGrid.SelectedObject != null)
            {
                var (succes, exchange, symbol, signal, interval, position) = GetAttributesFromSender(dataGrid.SelectedObject);
                if (succes)
                {
                    switch (item.Command)
                    {
                        case Command.CopySymbolInformation:
                            if (symbol != null)
                                new CommandCopySymbolInfo().Execute(item, symbol);
                            break;
                        case Command.ActivateTradingApp:
                            CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;
                            // Voor Altrady en Hypertrader werkt dit kunstje natuurlijk niet
                            if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
                                tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
                            GlobalData.LoadLinkSettings(); // refresh links
                            if (symbol != null && interval != null)
                                LinkTools.ActivateTradingApp(GlobalData.Settings.General.TradingApp, symbol, interval, tradingAppInternExtern);
                            break;
                        case Command.ActivateActiveExchange:
                            GlobalData.LoadLinkSettings(); // refresh links
                            if (symbol != null && interval != null)
                                LinkTools.ActivateTradingApp(CryptoTradingApp.ExchangeUrl, symbol, interval, CryptoExternalUrlType.External);
                            break;
                        case Command.ActivateTradingviewIntern:
                            GlobalData.LoadLinkSettings(); // refresh links
                            if (symbol != null && interval != null)
                                LinkTools.ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.Internal);
                            break;
                        case Command.ActivateTradingviewExtern:
                            GlobalData.LoadLinkSettings(); // refresh links
                            if (symbol != null && interval != null)
                                LinkTools.ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.External);
                            break;
                        case Command.ShowTrendInformation:
                            if (symbol != null)
                                new CommandShowTrendInfo().Execute(item, symbol);
                            break;
                        case Command.ExcelSignalInformation:
                            if (signal != null)
                                _ = Task.Run(() => { new ExcelSignalDump(signal).ExportToExcel(); });
                            break;
                        case Command.ExcelSymbolInformation:
                            if (symbol != null)
                                _ = Task.Run(() => { new ExcelSymbolDump(symbol).ExportToExcel(); });
                            break;
                        case Command.PositionCalculate:
                            if (position != null)
                            {
                                using CryptoDatabase databaseThread = new();
                                if (position.Status >= CryptoPositionStatus.Ready)
                                {
                                    databaseThread.Open();
                                    PositionTools.LoadPosition(databaseThread, position);
                                }
                                GlobalData.AddTextToLogTab($"{position.Symbol.Name} position {position.Id} recalculate manual");
                                await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position, forceCalculation: true);
                            }
                            break;
                        case Command.ExcelPositionInformation:
                            if (position != null)
                            {
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
                            break;
                        case Command.ShowSymbolGraph:
                            if (symbol != null && interval != null)
                            {
                                CommandShowGraph command = new();
                                command.Execute(item, symbol);
                            }
                            break;

                        case Command.CalculateSymbolLiquidityZones:
                            if (symbol != null)
                            {
                                GlobalData.ThreadZoneCalculate?.AddToQueue(symbol);
                            }
                            break;
                    }
                }
            }
        }
    }

    public static void ExecuteCommand(object? sender, EventArgs e)
    {
        _ = ExecuteCommandAsync(sender, e);
    }
}