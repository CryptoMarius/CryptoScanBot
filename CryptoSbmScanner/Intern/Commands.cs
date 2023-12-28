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
    ExcelPositionInformation
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



    public static (bool succes, CryptoSymbol symbol, CryptoInterval interval, CryptoPosition position) GetAttributesFromSender(object sender)
    {
        // Vanuit de signalen of de open of gesloten posities
        if (sender is ListViewHeaderContext listview && listview.SelectedItems.Count > 0)
        {
            ListViewItem listviewItem = listview.SelectedItems[0];
            if (listviewItem.Tag is CryptoSignal signal)
                return (true, signal.Symbol, signal.Interval, null);
            else if (listviewItem.Tag is CryptoPosition position)
                return (true, position.Symbol, position.Interval, position);
        }
        else
        // Vanuit de lijst met symbols
        if (sender is ListBox listbox && listbox.SelectedItems.Count > 0)
        {
            if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
            {
                // Neem de door de gebruiker geselecteerde coin
                string symbolName = listbox.Text.ToString();
                if (!string.IsNullOrEmpty(symbolName))
                {
                    if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
                    {
                        if (symbol.QuoteData.FetchCandles && symbol.Status == 1)
                            return (true, symbol, GlobalData.IntervalList[0], null);
                    }
                }

            }
        }

        return (false, null, null, null);
    }


    public static async void ExecuteSomethingViaTag(object sender, Command cmd)
    {
        var (succes, symbol, interval, position) = GetAttributesFromSender(sender);
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
                case Command.ExcelExchangeInformation:
                    // Die valt qua parameters buiten de boot
                    _ = Task.Run(() => { new Excel.ExcelExchangeDump().ExportToExcel(GlobalData.Settings.General.Exchange); });
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

        if (sender is ToolStripMenuItem tsitem && tsitem.Tag is Command cmd)
        {
            if (sender is ToolStripMenuItem item && item.Owner is ContextMenuStrip strip)
            {
                ExecuteSomethingViaTag(strip.SourceControl, cmd);
            }
        }
        else if (sender is ListView listview)
        {
            if (listview.Tag is Command cmd2)
            {
                ExecuteSomethingViaTag(listview, cmd2);
            }
        }
        else if (sender is ListBox listbox)
        {
            if (listbox.Tag is Command cmd3)
            {
                ExecuteSomethingViaTag(listbox, cmd3);
            }
        }
    }

}
