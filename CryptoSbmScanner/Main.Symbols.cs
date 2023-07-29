using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner;

public partial class FrmMain
{
    private void ListViewSymbolsConstructor()
    {
        listBoxSymbols.DoubleClick += new System.EventHandler(ListBoxSymbols_DoubleClick);
    }

    private void ListboxSymbolsInitCaptions()
    {
        string text = GlobalData.ExternalUrls.GetTradingAppName(GlobalData.Settings.General.TradingApp, GlobalData.Settings.General.ExchangeName);
        listBoxSymbolsMenuItemActivateTradingApp.Text = text;
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
        if (components != null && IsHandleCreated)
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

            }

            if (webViewTradingView.Source == null)
            {
                Invoke((MethodInvoker)(() => LinkTools.ActivateTradingViewBrowser()));
            }

        }
    }

    
    private void ListBoxSymbolsMenuItemActivateTradingApp_Click(object sender, EventArgs e)
    {
        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval interval))
            return;

        CryptoSymbol symbol = GetSymbolFromListBox();
        if (symbol == null)
            return;

        LinkTools.ActivateExternalTradingApp(GlobalData.Settings.General.TradingApp, symbol, interval);
    }


    private void ListBoxSymbolsMenuItemActivateTradingviewInternal_Click(object sender, EventArgs e)
    {
        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval interval))
            return;

        CryptoSymbol symbol = GetSymbolFromListBox();
        if (symbol == null)
            return;

        LinkTools.ActivateInternalTradingApp(CryptoTradingApp.TradingView, symbol, interval);
    }


    private void ListBoxSymbolsMenuItemActivateTradingviewExternal_Click(object sender, EventArgs e)
    {
        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval interval))
            return;

        CryptoSymbol symbol = GetSymbolFromListBox();
        if (symbol == null)
            return;

        LinkTools.ActivateExternalTradingApp(CryptoTradingApp.TradingView, symbol, interval);
    }


    private void ListBoxSymbols_DoubleClick(object sender, EventArgs e)
    {
        ListBoxSymbolsMenuItemActivateTradingApp_Click(sender, e);
    }

    private void MenuSymbolsShowTrendInformation_Click(object sender, EventArgs e)
    {
        // Show trend information
        CryptoSymbol symbol = GetSymbolFromListBox();
        if (symbol == null)
            return;

        ShowTrendInformation(symbol);
    }


    private void ListBoxSymbolsMenuItemCopy_Click(object sender, EventArgs e)
    {
        // Show trend information
        CryptoSymbol symbol = GetSymbolFromListBox();
        if (symbol == null)
            return;

        Clipboard.SetText(symbol.Name, TextDataFormat.UnicodeText);
    }

    private void ListBoxSymbolsMenuItemCandleDump_Click(object sender, EventArgs e)
    {
        // Show trend information
        CryptoSymbol symbol = GetSymbolFromListBox();
        if (symbol == null)
            return;

        Task.Run(() => {
            string filename = CandleDumpDebug.ExportToExcell(symbol);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filename) { UseShellExecute = true });
        });
    }

    private void ListBoxSymbolsMenuItemCreateSignal_Click(object sender, EventArgs e)
    {
        //    // Neem de door de gebruiker geselecteerde coin
        //    string symbolName = listBoxSymbols.Text.ToString();
        //    if (string.IsNullOrEmpty(symbolName))
        //        return;

        //    DateTime eventTimeStart = DateTime.UtcNow;

        //    //todo: Multi exchange (nah)
        //    CryptoExchange exchange = null;
        //    if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out exchange))
        //    {
        //        CryptoSymbol symbol = null;
        //        if (exchange.SymbolListName.TryGetValue(symbolName, out symbol))
        //        {
        //            //Op het gekozen interval
        //            CryptoInterval interval = (CryptoInterval)comboBoxInterval.SelectedItem;

        //            SignalCreate algoritm = new SignalCreate(symbol, interval, null);

        //            unix time = bi
        //            algoritm.BinanceAnalyseSymbol(, true);
        //            algoritm.CreateSignalg
        //            CryptoSignal signal = algoritm.CreateSignal();
        //        }
        //    }
    }

    private void SignalsNegerenToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // Neem de door de gebruiker geselecteerde coin
        string symbolName = listBoxSymbols.Text.ToString();
        if (string.IsNullOrEmpty(symbolName))
            return;

        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out CryptoSbmScanner.Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
            {
                foreach (CryptoSymbolInterval cryptoSymbolInterval in symbol.IntervalPeriodList)
                {
                    CryptoSignal signal = cryptoSymbolInterval.Signal;
                    if (signal != null)
                    {
                        string lastPrice = symbol.LastPrice?.ToString(symbol.PriceDisplayFormat);
                        string text = "Monitor " + symbol.Name + " " + signal.Interval.Name + " signal from=" + signal.OpenDate.ToLocalTime() + " " + signal.Strategy.ToString() + " price=" + lastPrice;
                        GlobalData.AddTextToLogTab(text + " cancelled (removed)");

                        cryptoSymbolInterval.Signal = null;
                    }
                }
            }
        }
    }

}