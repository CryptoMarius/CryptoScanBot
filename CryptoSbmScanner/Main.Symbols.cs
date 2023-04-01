using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner;

public partial class FrmMain
{
    private void ListboxSymbolsInitCaptions()
    {
        switch (GlobalData.Settings.General.TradingApp)
        {
            case TradingApp.Altrady:
            case TradingApp.AltradyWeb:
                listBoxSymbolsMenuItemActivateTradingApp.Text = "Altrady";
                listBoxSymbolsMenuItemActivateTradingApps.Text = "Altrady + TradingView";
                break;
            case TradingApp.Hypertrader:
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

        if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
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
        if ((components != null) && (!ProgramExit) && (IsHandleCreated))
        {
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
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

        var href = Intern.TradingView.GetRef(symbol, interval);
        Uri uri = new(href);
        webViewTradingView.Source = uri;
        tabControl.SelectedTab = tabPageBrowser;
    }


    private void ListBoxSymbolsMenuItemActivateTradingviewExternal_Click(object sender, EventArgs e)
    {
        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval interval))
            return;

        CryptoSymbol symbol = GetSymbolFromListBox();
        if (symbol == null)
            return;

        var href = Intern.TradingView.GetRef(symbol, interval);
        System.Diagnostics.Process.Start(href);
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
}
