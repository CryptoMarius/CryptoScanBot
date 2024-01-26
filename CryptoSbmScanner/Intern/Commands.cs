using System.Text;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Trader;

namespace CryptoSbmScanner.Intern;

public enum Command
{
    ActivateTradingApp,
    ActivateTradingviewIntern,
    ActivateTradingviewExtern,
    ShowTrendInformation,
    ExcelSymbolInformation,
    ExcelExchangeInformation,
    ExcelPositionInformation,
    About
}

//class CommandEventArgs : EventArgs
//{
//    public Command Command { get; private set; }

//    public CommandEventArgs(Command command)
//    {
//        Command = command;
//    }
//}



public class Commands
{
    public Commands()
    {
    }


    //public static void ExecuteCommandCommandViaTag(object sender, EventArgs e)
    //{
    //    if (sender is ToolStripMenuItem item && e is CommandEventArgs cmd)
    //    {
    //        var (succes, symbol, interval) = GetAttributesFromSender(sender);
    //        if (succes)
    //        {
    //            switch (cmd.Command)
    //            {
    //                case Command.ActivateTradingApp:
    //                    LinkTools.ActivateTradingApp(GlobalData.Settings.General.TradingApp, symbol, interval, CryptoExternalUrlType.External);
    //                    break;
    //                case Command.ActivateTradingviewIntern:
    //                    LinkTools.ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.Internal);
    //                    break;
    //                case Command.ActivateTradingviewExtern:
    //                    LinkTools.ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.External);
    //                    break;
    //                case Command.ShowTrendInformation:
    //                    ShowTrendInformation(symbol);
    //                    break;
    //                case Command.ExcelSymbolInformation:
    //                    Task.Run(() => { new Excel.ExcelSymbolDump().ExportToExcell(symbol); });
    //                    break;
    //                case Command.ExcelExchangeInformation:
    //                    // Die valt qua parameters buiten de boot
    //                    Task.Run(() => { new Excel.ExcelExchangeDump().ExportToExcell(GlobalData.Settings.General.Exchange); });
    //                    break;
    //            }
    //        }
    //    }
    //}


//    private static async void CommandPositionExcelDumpExecute(object sender, EventArgs e)
//    {
//#if TRADEBOT
//        if (sender is ListView listview)
//        {
//            if (listview.SelectedItems.Count > 0)
//            {
//                ListViewItem item = listview.SelectedItems[0];
//                CryptoPosition position = (CryptoPosition)item.Tag;

//                using CryptoDatabase databaseThread = new();
//                databaseThread.Open();
//                PositionTools.LoadPosition(databaseThread, position);

//                await PositionTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
//                PositionTools.CalculatePositionResultsViaTrades(databaseThread, position);

//                //Task.Run(() => { Invoke(new Action(() => { new PositionDumpDebug().ExportToExcell(position); })); });
//                _ = Task.Run(() => { new Excel.ExcelPositionDump().ExportToExcel(position); });
//            }
//        }
//#endif
//    }

    private static void ShowTrendInformation(CryptoSymbol symbol)
    {
        StringBuilder log = new();
        log.AppendLine("Trend " + symbol.Name);

        GlobalData.AddTextToLogTab("");
        GlobalData.AddTextToLogTab("Trend " + symbol.Name);

        long percentageSum = 0;
        long maxPercentageSum = 0;
        foreach (CryptoSymbolInterval cryptoSymbolInterval in symbol.IntervalPeriodList)
        {
            log.AppendLine("");
            log.AppendLine("----");
            log.AppendLine("Interval " + cryptoSymbolInterval.Interval.Name);

            // Wat is het maximale som (voor de eindberekening)
            maxPercentageSum += cryptoSymbolInterval.Interval.Duration;

            TrendIndicator trendIndicatorClass = new(symbol, cryptoSymbolInterval)
            {
                Log = log
            };
            // TODO Parameter voor de trendIndicatorClass.CalculateTrend goed invullen
            CryptoTrendIndicator trendIndicator = trendIndicatorClass.CalculateTrend(0);
            if (trendIndicator == CryptoTrendIndicator.Bullish)
                percentageSum += cryptoSymbolInterval.Interval.Duration;
            else if (trendIndicator == CryptoTrendIndicator.Bearish)
                percentageSum -= cryptoSymbolInterval.Interval.Duration;


            // Ahh, dat gaat niet naar een tabel (zoals ik eerst dacht)
            //CryptoSymbolInterval symbolInterval = signal.Symbol.GetSymbolInterval(interval.IntervalPeriod);
            //symbolInterval.TrendIndicator = trendIndicator;
            //symbolInterval.TrendInfoDate = DateTime.UtcNow;

            string s = "";
            if (trendIndicator == CryptoTrendIndicator.Bullish)
                s = string.Format("{0} {1}, trend=bullish", symbol.Name, cryptoSymbolInterval.Interval.IntervalPeriod);
            else if (trendIndicator == CryptoTrendIndicator.Bearish)
                s = string.Format("{0} {1}, trend=bearish", symbol.Name, cryptoSymbolInterval.Interval.IntervalPeriod);
            else
                s = string.Format("{0} {1}, trend=sideway's", symbol.Name, cryptoSymbolInterval.Interval.IntervalPeriod);
            GlobalData.AddTextToLogTab(s);
            log.AppendLine(s);
        }

        if (maxPercentageSum > 0)
        {
            decimal trendPercentage = 100 * (decimal)percentageSum / (decimal)maxPercentageSum;
            string t = string.Format("{0} {1:N2}", symbol.Name, trendPercentage);
            GlobalData.AddTextToLogTab(t);
            log.AppendLine(t);
        }



        //Laad de gecachte (langere historie, minder overhad)
        string filename = GlobalData.GetBaseDir() + "Trend information.txt";
        File.WriteAllText(filename, log.ToString());
    }



    public static (bool succes, Model.CryptoExchange exchange, CryptoSymbol symbol, CryptoInterval interval, CryptoPosition position) GetAttributesFromSender(object sender)
    {
        // Vanuit de symbols, signals of de open of gesloten posities
        if (sender is ListViewHeaderContext listview && listview.SelectedItems.Count > 0)
        {
            ListViewItem listviewItem = listview.SelectedItems[0];
            if (listviewItem.Tag is CryptoSignal signal)
                // Vanuit de lijst met signalen
                return (true, signal.Exchange, signal.Symbol, signal.Interval, null);
            else if (listviewItem.Tag is CryptoPosition position)
                // Vanuit de lijst met posities
                return (true, position.Exchange, position.Symbol, position.Interval, position);
            else if (listviewItem.Tag is CryptoSymbol symbol)
                // Vanuit de lijst met symbols
                return (true, GlobalData.Settings.General.Exchange, null, null, null);
            else if (listviewItem.Tag is CryptoSymbol symbol2) //ehhh....
                // Vanuit de lijst met symbols
                return (true, GlobalData.Settings.General.Exchange, null, null, null);
        }

        return (false, null, null, null, null);
    }


    public static async void ExecuteSomethingViaTag(object sender, Command cmd)
    {
        if (cmd == Command.About)
        {
            AboutBox form = new()
            {
                StartPosition = FormStartPosition.CenterParent
            };
            form.ShowDialog();
            return;
        }
        else if (cmd == Command.ExcelExchangeInformation)
        {
            // Die valt qua parameters buiten de boot
            _ = Task.Run(() => { new Excel.ExcelExchangeDump().ExportToExcel(GlobalData.Settings.General.Exchange); });
            return;
        }

        // De rest van de commando's heeft een object nodig

        var (succes, exchange, symbol, interval, position) = GetAttributesFromSender(sender);
        if (succes)
        {
            switch (cmd)
            {
                case Command.ActivateTradingApp:
                    LinkTools.ActivateTradingApp(GlobalData.Settings.General.TradingApp, symbol, interval, CryptoExternalUrlType.External);
                    break;
                case Command.ActivateTradingviewIntern:
                    LinkTools.ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.Internal);
                    break;
                case Command.ActivateTradingviewExtern:
                    LinkTools.ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.External);
                    break;
                case Command.ShowTrendInformation:
                    ShowTrendInformation(symbol);
                    break;
                case Command.ExcelSymbolInformation:
                    _ = Task.Run(() => { new Excel.ExcelSymbolDump().ExportToExcel(symbol); });
                    break;
#if TRADEBOT
                case Command.ExcelPositionInformation:
                    using (CryptoDatabase databaseThread = new())
                    {
                        databaseThread.Open();
                        PositionTools.LoadPosition(databaseThread, position);

                        await TradeTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
                        TradeTools.CalculatePositionResultsViaTrades(databaseThread, position);

                        _ = Task.Run(() => { new Excel.ExcelPositionDump().ExportToExcel(position); });
                    }
                    break;
#endif
            }
        }
    }

    public static void ExecuteCommandCommandViaTag(object sender, EventArgs e)
    {
        // Een poging om de meest gebruikte menu items te centraliseren

        if (sender is ToolStripMenuItem tsitem && tsitem.Tag is Command cmd1)
        {
            if (sender is ToolStripMenuItem item)
            {
                if (item.Owner is ContextMenuStrip strip)
                {
                    ExecuteSomethingViaTag(strip.SourceControl, cmd1);
                    //applicationMenuStrip = new MenuStrip();
                }
                else if (item.Owner is ToolStripDropDownMenu strip2)
                {
                    ExecuteSomethingViaTag(strip2, cmd1);
                    //applicationMenuStrip = new MenuStrip();
                }
            }
        }
        else if (sender is ListView listview)
        {
            if (listview.Tag is Command cmd2)
            {
                ExecuteSomethingViaTag(listview, cmd2);
            }
        }
    }

}
