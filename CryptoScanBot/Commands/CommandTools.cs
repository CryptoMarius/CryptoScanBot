using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trader;
using CryptoScanBot.Intern;

namespace CryptoScanBot.Commands;


public class ToolStripMenuItemCommand : ToolStripMenuItem
{
    public new Command Command { get; set; }
    public CryptoDataGrid DataGrid { get; set; }
}


public static class CommandHelper
{

    public static void AddSeperator(this ContextMenuStrip menuStrip)
    {
        menuStrip.Items.Add(new ToolStripSeparator());
    }

    public static void AddSeperator(this ToolStripMenuItem menuStrip)
    {
        menuStrip.DropDownItems.Add(new ToolStripSeparator());
    }

    public static ToolStripMenuItemCommand AddCommand(this ContextMenuStrip menuStrip, CryptoDataGrid dataGrid, string text, Command command, EventHandler click = null)
    {
        ToolStripMenuItemCommand menuItem = new()
        {
            Command = command,
            DataGrid = dataGrid,
            Text = text
        };
        if (click == null)
            menuItem.Click += CommandTools.ExecuteCommand;
        else 
            menuItem.Click += click;
        menuStrip.Items.Add(menuItem);
        return menuItem;
    }

    public static ToolStripMenuItemCommand AddCommand(this ToolStripMenuItem menuStrip, CryptoDataGrid dataGrid, string text, Command command, EventHandler click = null)
    {
        ToolStripMenuItemCommand menuItem = new()
        {
            Command = command,
            DataGrid = dataGrid,
            Text = text
        };
        if (click == null)
            menuItem.Click += CommandTools.ExecuteCommand;
        else
            menuItem.Click += click;
        menuStrip.DropDownItems.Add(menuItem);
        return menuItem;
    }
}


public class CommandTools
{
    public static (bool succes, Core.Model.CryptoExchange exchange, CryptoSymbol symbol, CryptoSignal signal, CryptoInterval interval, CryptoPosition position) GetAttributesFromSender(object sender)
    {
        if (sender is CryptoSymbol symbol)
            return (true, symbol.Exchange, symbol, null, GlobalData.IntervalList[5], null);

        if (sender is CryptoSignal signal)
            return (true, signal.Exchange, signal.Symbol, signal, signal.Interval, null);

        if (sender is CryptoPosition position)
            return (true, position.Exchange, position.Symbol, null, position.Interval, position);

        return (false, null, null, null, null, null);
    }



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
                _ = Task.Run(() => { new Excel.ExcelExchangeDump().ExportToExcel(GlobalData.Settings.General.Exchange); });
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
                    _ = Task.Run(() => { new Excel.ExcelSignalDump().ExportToExcel(signal); });
                    break;
                case Command.ExcelSymbolInformation:
                    _ = Task.Run(() => { new Excel.ExcelSymbolDump().ExportToExcel(symbol); });
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
                        _ = Task.Run(() => { new Excel.ExcelPositionDump().ExportToExcel(position); });
                    }
                    break;
#endif
            }
        }
    }

    public static void ExecuteCommand(object sender, EventArgs e)
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