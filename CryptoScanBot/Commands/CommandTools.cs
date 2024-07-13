using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Excel;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trader;
using CryptoScanBot.Intern;

namespace CryptoScanBot.Commands;

public class CommandTools
{
    public static (bool succes, Core.Model.CryptoExchange exchange, CryptoSymbol symbol, CryptoSignal signal, CryptoInterval interval, CryptoPosition position) GetAttributesFromSender(object sender) => sender switch
    {
        CryptoSymbol symbol => (true, symbol.Exchange, symbol, null, GlobalData.IntervalList[5], null),
        CryptoSignal signal => (true, signal.Exchange, signal.Symbol, signal, signal.Interval, null),
        CryptoPosition position => (true, position.Exchange, position.Symbol, null, position.Interval, position),
        _ => (false, null, null, null, null, null)
    };

    public static async void ExecuteSomething(object sender, int index, Command cmd)
    {
        // Global commands
        switch (cmd)
        {
            case Command.About:
                new CommandAbout().Execute(null);
                return;

            case Command.ExcelExchangeInformation:
                // Die valt qua parameters buiten de boot
                _ = Task.Run(() => { new ExcelExchangeDump(GlobalData.Settings.General.Exchange).ExportToExcel(); });
                return;

            case Command.ScannerSessionDebug:
                ScannerSession.ScheduleRefresh();
                return;
        }


        // De rest van de commando's heeft een object nodig
        var (succes, exchange, symbol, signal, interval, position) = GetAttributesFromSender(sender);
        if (succes)
        {
            switch (cmd)
            {
                case Command.CopySymbolInformation:
                    new CommandCopySymbolInfo().Execute(symbol);
                    break;
                //case Command.CopySignalInformation:
                //   new CommandCopySignalInfo().Execute(sender);
                //    break;
                case Command.ActivateTradingApp:
                    CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;
                    // Voor Altrady en Hypertrader werkt dit kunstje natuurlijk niet
                    if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
                        tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
                    LinkTools.ActivateTradingApp(GlobalData.Settings.General.TradingApp, symbol, interval, tradingAppInternExtern);
                    break;
                case Command.ActivateActiveExchange:
                    LinkTools.ActivateTradingApp(CryptoTradingApp.ExchangeUrl, symbol, interval, CryptoExternalUrlType.External);
                    break;
                case Command.ActivateTradingviewIntern:
                    LinkTools.ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.Internal);
                    break;
                case Command.ActivateTradingviewExtern:
                    LinkTools.ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.External);
                    break;
                case Command.ShowTrendInformation:
                    new CommandShowTrendInfo().Execute(symbol);
                    break;
                case Command.ExcelSignalInformation:
                    _ = Task.Run(() => { new ExcelSignalDump(signal).ExportToExcel(); });
                    break;
                case Command.ExcelSymbolInformation:
                    _ = Task.Run(() => { new ExcelSymbolDump(symbol).ExportToExcel(); });
                    break;
#if TRADEBOT
                case Command.PositionCalculate:
                    using (CryptoDatabase databaseThread = new())
                    {
                        if (position.Status >= CryptoPositionStatus.Ready)
                        {
                            databaseThread.Open();
                            PositionTools.LoadPosition(databaseThread, position);
                        }
                        GlobalData.AddTextToLogTab($"{position.Symbol.Name} positie {position.Id} handmatig herberekenen");
                        await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position, forceCalculation: true);
                    }
                    break;
                case Command.ExcelPositionInformation:
                    using (CryptoDatabase databaseThread = new())
                    {
                        if (position.Status >= CryptoPositionStatus.Ready)
                        {
                            databaseThread.Open();
                            PositionTools.LoadPosition(databaseThread, position);
                        }
                        GlobalData.AddTextToLogTab($"{position.Symbol.Name} positie {position.Id} herberekenen voor Excel");
                        await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position, forceCalculation: true);
                        _ = Task.Run(() => { new ExcelPositionDump(position).ExportToExcel(); });
                    }
                    break;
#endif
            }
        }
    }

    public static void ExecuteCommand(object? sender, EventArgs? e)
    {
        // Een poging om de meest gebruikte menu items te centraliseren
        if (sender is ToolStripMenuItemCommand item)
        {
            if (item.DataGrid is CryptoDataGrid dataGrid)
                ExecuteSomething(dataGrid.SelectedObject, dataGrid.SelectedObjectIndex, item.Command);
            else
                ExecuteSomething(sender, -1, item.Command);
        }
    }
}