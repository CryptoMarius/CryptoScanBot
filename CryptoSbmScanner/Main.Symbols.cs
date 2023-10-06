using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner;

public partial class FrmMain
{
    private ContextMenuStrip ListBoxSymbolContextMenuStrip;

    private ToolStripMenuItem CommandSymbolsActivateTradingApp;
    private ToolStripMenuItem CommandSymbolsActivateTradingViewInternal;
    private ToolStripMenuItem CommandSymbolsActivateTradingViewExternal;
    private ToolStripMenuItem CommandSymbolsTrendInformation;
    private ToolStripMenuItem CommandSymbolsCopyInformation;
    private ToolStripMenuItem CommandSymbolsInformationExcelDump;
    private ToolStripMenuItem CommandSymbolsExchangeInformationExcelDump;


    private void ListViewSymbolsConstructor()
    {
        ListBoxSymbolContextMenuStrip = new();

        CommandSymbolsActivateTradingApp = new ToolStripMenuItem();
        CommandSymbolsActivateTradingApp.Text = "Activate trading app";
        CommandSymbolsActivateTradingApp.Tag = Command.ActivateTradingApp;
        CommandSymbolsActivateTradingApp.Click += Commands.ExecuteCommandCommandViaTag;
        //CommandSymbolsActivateTradingApp.Tag = new CommandEventArgs(Command.ActivateTradingApp);
        ListBoxSymbolContextMenuStrip.Items.Add(CommandSymbolsActivateTradingApp);

        CommandSymbolsActivateTradingViewInternal = new ToolStripMenuItem();
        CommandSymbolsActivateTradingViewInternal.Text = "TradingView browser";
        CommandSymbolsActivateTradingViewInternal.Tag = Command.ActivateTradingviewIntern;
        CommandSymbolsActivateTradingViewInternal.Click += Commands.ExecuteCommandCommandViaTag;
        //CommandSymbolsActivateTradingViewInternal.Tag = new CommandEventArgs(Command.ActivateTradingviewIntern);
        ListBoxSymbolContextMenuStrip.Items.Add(CommandSymbolsActivateTradingViewInternal);

        CommandSymbolsActivateTradingViewExternal = new ToolStripMenuItem();
        CommandSymbolsActivateTradingViewExternal.Text = "TradingView extern";
        CommandSymbolsActivateTradingViewExternal.Tag = Command.ActivateTradingviewExtern;
        CommandSymbolsActivateTradingViewExternal.Click += Commands.ExecuteCommandCommandViaTag;
        //CommandSymbolsActivateTradingViewExternal.Tag = new CommandEventArgs(Command.ActivateTradingviewExtern);
        ListBoxSymbolContextMenuStrip.Items.Add(CommandSymbolsActivateTradingViewExternal);

        ListBoxSymbolContextMenuStrip.Items.Add(new ToolStripSeparator());

        CommandSymbolsCopyInformation = new ToolStripMenuItem();
        CommandSymbolsCopyInformation.Text = "Kopiëer informatie";
        CommandSymbolsCopyInformation.Click += CommandSymbolsCopyInformationExecute;
        ListBoxSymbolContextMenuStrip.Items.Add(CommandSymbolsCopyInformation);

        CommandSymbolsTrendInformation = new ToolStripMenuItem();
        CommandSymbolsTrendInformation.Text = "Trend informatie (zie log)";
        CommandSymbolsTrendInformation.Tag = Command.ShowTrendInformation;
        CommandSymbolsTrendInformation.Click += Commands.ExecuteCommandCommandViaTag;
        //CommandSymbolsTrendInformation.Tag = new CommandEventArgs(Command.ShowTrendInformation);
        ListBoxSymbolContextMenuStrip.Items.Add(CommandSymbolsTrendInformation);

        CommandSymbolsInformationExcelDump = new ToolStripMenuItem();
        CommandSymbolsInformationExcelDump.Text = "Symbol informatie (Excel)";
        CommandSymbolsInformationExcelDump.Tag = Command.ExcelSymbolInformation;
        CommandSymbolsInformationExcelDump.Click += Commands.ExecuteCommandCommandViaTag;
        //CommandSymbolsInformationExcelDump.Tag = new CommandEventArgs(Command.ExcelSymbolInformation);
        ListBoxSymbolContextMenuStrip.Items.Add(CommandSymbolsInformationExcelDump);

        CommandSymbolsExchangeInformationExcelDump = new ToolStripMenuItem();
        CommandSymbolsExchangeInformationExcelDump.Text = "Exchange informatie (Excel)";
        CommandSymbolsExchangeInformationExcelDump.Tag = Command.ExcelExchangeInformation;
        CommandSymbolsExchangeInformationExcelDump.Click += Commands.ExecuteCommandCommandViaTag;
        //CommandSymbolsExchangeInformationExcelDump.Tag = new CommandEventArgs(Command.ExcelExchangeInformation);
        ListBoxSymbolContextMenuStrip.Items.Add(CommandSymbolsExchangeInformationExcelDump);

        listBoxSymbols.ContextMenuStrip = ListBoxSymbolContextMenuStrip;

        listBoxSymbols.Tag = Command.ActivateTradingApp; // Default command, niet heel netjes
        listBoxSymbols.DoubleClick += Commands.ExecuteCommandCommandViaTag;
    }

    private void ListboxSymbolsInitCaptions()
    {
        string text = GlobalData.ExternalUrls.GetTradingAppName(GlobalData.Settings.General.TradingApp, GlobalData.Settings.General.ExchangeName);
        CommandSymbolsActivateTradingApp.Text = text;
    }


    private CryptoSymbol GetSymbolFromListBox()
    {
        // Neem de door de gebruiker geselecteerde coin
        string symbolName = listBoxSymbols.Text.ToString();
        if (string.IsNullOrEmpty(symbolName))
            return null;

        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            // Bestaat de coin? (uiteraard, net geladen)
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
            {
                if (symbol.QuoteData.FetchCandles && symbol.Status == 1)
                {
                    return symbol;
                }
            }
        }
        return null;
    }


    private void SymbolFilter_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            SymbolsHaveChangedEvent("");
        }
    }


    /// <summary>
    /// Dan is er in de achtergrond een verversing actie geweest, display bijwerken!
    /// </summary>
    private void SymbolsHaveChangedEvent(string text, bool extraLineFeed = false)
    {
        if (IsHandleCreated)
        {
            if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
            {
                string filter = "";
                symbolFilter.Invoke((MethodInvoker)(() => filter = symbolFilter.Text.ToUpper()));

                //De muntparen toevoegen aan de userinterface
                foreach (var symbol in exchange.SymbolListName.Values)
                {
                    if (symbol.QuoteData.FetchCandles && symbol.Status == 1)
                    {
                        if (!symbol.IsSpotTradingAllowed || symbol.IsBarometerSymbol())
                            continue;

                        bool addSymbol = true;
                        if ((filter != "") && (!symbol.Name.Contains(filter)))
                            addSymbol = false;

                        int indexSymbol = 0;
                        listBoxSymbols.Invoke((MethodInvoker)(() => indexSymbol = listBoxSymbols.Items.IndexOf(symbol.Name)));

                        if ((indexSymbol < 0) && (addSymbol))
                            listBoxSymbols.Invoke((MethodInvoker)(() => listBoxSymbols.Items.Add(symbol.Name)));
                        if ((indexSymbol >= 0) && (!addSymbol))
                            listBoxSymbols.Invoke((MethodInvoker)(() => listBoxSymbols.Items.RemoveAt(indexSymbol)));
                    }
                }


                if (webViewTradingView.Source == null)
                {
                    string symbolname = "BTCUSDT";
                    CryptoInterval interval = GlobalData.IntervalListPeriod[0];
                    if (exchange.SymbolListName.TryGetValue(symbolname, out CryptoSymbol symbol))
                        Invoke((MethodInvoker)(() => LinkTools.ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.Internal, false)));
                }
            }

        }
    }

    
    private void CommandSymbolsCopyInformationExecute(object sender, EventArgs e)
    {
        CryptoSymbol symbol = GetSymbolFromListBox();
        if (symbol == null)
            return;

        Clipboard.SetText(symbol.Name, TextDataFormat.UnicodeText);
    }

    //private void ListBoxSymbolsMenuItemCreateSignal_Click(object sender, EventArgs e)
    //{
    //    //    // Neem de door de gebruiker geselecteerde coin
    //    //    string symbolName = listBoxSymbols.Text.ToString();
    //    //    if (string.IsNullOrEmpty(symbolName))
    //    //        return;

    //    //    DateTime eventTimeStart = DateTime.UtcNow;

    //    //    //todo: Multi exchange (nah)
    //    //    CryptoExchange exchange = null;
    //    //    if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out exchange))
    //    //    {
    //    //        CryptoSymbol symbol = null;
    //    //        if (exchange.SymbolListName.TryGetValue(symbolName, out symbol))
    //    //        {
    //    //            //Op het gekozen interval
    //    //            CryptoInterval interval = (CryptoInterval)comboBoxInterval.SelectedItem;

    //    //            SignalCreate algoritm = new SignalCreate(symbol, interval, null);

    //    //            unix time = bi
    //    //            algoritm.BinanceAnalyseSymbol(, true);
    //    //            algoritm.CreateSignalg
    //    //            CryptoSignal signal = algoritm.CreateSignal();
    //    //        }
    //    //    }
    //}
    //
    //private void SignalsNegerenToolStripMenuItem_Click(object sender, EventArgs e)
    //{
    //    // Neem de door de gebruiker geselecteerde coin
    //    string symbolName = listBoxSymbols.Text.ToString();
    //    if (string.IsNullOrEmpty(symbolName))
    //        return;

    //    if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
    //    {
    //        if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
    //        {
    //            foreach (CryptoSymbolInterval cryptoSymbolInterval in symbol.IntervalPeriodList)
    //            {
    //                CryptoSignal signal = cryptoSymbolInterval.Signal;
    //                if (signal != null)
    //                {
    //                    string lastPrice = symbol.LastPrice?.ToString(symbol.PriceDisplayFormat);
    //                    string text = "Monitor " + symbol.Name + " " + signal.Interval.Name + " signal from=" + signal.OpenDate.ToLocalTime() + " " + signal.Strategy.ToString() + " price=" + lastPrice;
    //                    GlobalData.AddTextToLogTab(text + " cancelled (removed)");

    //                    cryptoSymbolInterval.Signal = null;
    //                }
    //            }
    //        }
    //    }
    //}

}