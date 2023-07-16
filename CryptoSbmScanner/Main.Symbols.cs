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
        switch (GlobalData.Settings.General.TradingApp)
        {
            case CryptoTradingApp.Altrady:
                listBoxSymbolsMenuItemActivateTradingApp.Text = "Altrady";
                listBoxSymbolsMenuItemActivateTradingApps.Text = "Altrady + TradingView";
                break;
            case CryptoTradingApp.Hypertrader:
                listBoxSymbolsMenuItemActivateTradingApp.Text = "Hypertrader";
                listBoxSymbolsMenuItemActivateTradingApps.Text = "Hypertrader + TradingView";
                break;
        }
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
                Invoke((MethodInvoker)(async () => await ActivateTradingViewBrowser()));
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

        ActivateTradingApp(symbol, interval);
    }


    private void ListBoxSymbolsMenuItemActivateTradingApps_Click(object sender, EventArgs e)
    {
        ListBoxSymbolsMenuItemActivateTradingApp_Click(sender, e);
        ListBoxSymbolsMenuItemActivateTradingviewInternal_Click(sender, e);
    }

    private void ListBoxSymbolsMenuItemActivateTradingviewInternal_Click(object sender, EventArgs e)
    {
        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval interval))
            return;

        CryptoSymbol symbol = GetSymbolFromListBox();
        if (symbol == null)
            return;

        (string Url, bool Execute) refInfo;
        refInfo = ExchangeHelper.GetExternalRef(CryptoTradingApp.TradingView, false, symbol, interval);
        webViewTradingView.Source = new(refInfo.Url);
        tabControl.SelectedTab = tabPageBrowser;
    }


    private void ListBoxSymbolsMenuItemActivateTradingviewExternal_Click(object sender, EventArgs e)
    {
        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval interval))
            return;

        CryptoSymbol symbol = GetSymbolFromListBox();
        if (symbol == null)
            return;

        (string Url, bool Execute) refInfo;
        refInfo = ExchangeHelper.GetExternalRef(CryptoTradingApp.TradingView, false, symbol, interval);
        webViewTradingView.Source = new(refInfo.Url);
        //System.Diagnostics.Process.Start(refInfo.Url);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(refInfo.Url) { UseShellExecute = true });
    }


    private void ListBoxSymbols_DoubleClick(object sender, EventArgs e)
    {
        switch (GlobalData.Settings.General.DoubleClickAction)
        {
            case DoubleClickAction.activateTradingApp:
                ListBoxSymbolsMenuItemActivateTradingApp_Click(sender, e);
                break;
            case DoubleClickAction.activateTradingAppAndTradingViewInternal:
                ListBoxSymbolsMenuItemActivateTradingApps_Click(sender, e);
                break;
            case DoubleClickAction.activateTradingViewBrowerInternal:
                ListBoxSymbolsMenuItemActivateTradingviewInternal_Click(sender, e);
                break;
            case DoubleClickAction.activateTradingViewBrowerExternal:
                ListBoxSymbolsMenuItemActivateTradingviewExternal_Click(sender, e);
                break;
        }

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