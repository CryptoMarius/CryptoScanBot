using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoSbmScanner
{
    public partial class FrmMain
    {
        private void listboxSymbolsInitCaptions()
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

            CryptoExchange exchange;
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
            {
                // Bestaat de coin? (uiteraard, net geladen)
                CryptoSymbol symbol;
                if (exchange.SymbolListName.TryGetValue(symbolName, out symbol))
                {
                    if (CandleTools.MatchingQuote(symbol) && (symbol.Status == 1))
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
                CryptoSbmScanner.CryptoExchange exchange;
                if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
                {
                    string filter = "";
                    symbolFilter.Invoke((MethodInvoker)(() => filter = symbolFilter.Text.ToUpper()));

                    //De muntparen toevoegen aan de userinterface
                    foreach (var symbol in exchange.SymbolListName.Values)
                    {
                        if ((CandleTools.MatchingQuote(symbol)) && (symbol.Status == 1))
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
                    Invoke((MethodInvoker)(async() => ActivateTradingViewBrowser()));
                }

            }
        }




        private void listBoxSymbolsMenuItemActivateTradingApp_Click(object sender, EventArgs e)
        {
            CryptoInterval interval;
            if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out interval))
                return;

            CryptoSymbol symbol = GetSymbolFromListBox();
            if (symbol == null)
                return;

            ActivateTradingApp(symbol, interval);
        }


        private void listBoxSymbolsMenuItemActivateTradingApps_Click(object sender, EventArgs e)
        {
            listBoxSymbolsMenuItemActivateTradingApp_Click(sender, e);
            listBoxSymbolsMenuItemActivateTradingviewInternal_Click(sender, e);
        }

        private void listBoxSymbolsMenuItemActivateTradingviewInternal_Click(object sender, EventArgs e)
        {
            CryptoInterval interval;
            if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out interval))
                return;

            CryptoSymbol symbol = GetSymbolFromListBox();
            if (symbol == null)
                return;

            var href = TradingView.GetRef(symbol, interval);
            Uri uri = new Uri(href);
            webViewTradingView.Source = uri;
            tabControl.SelectedTab = tabPageBrowser;
        }


        private void listBoxSymbolsMenuItemActivateTradingviewExternal_Click(object sender, EventArgs e)
        {
            CryptoInterval interval;
            if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out interval))
                return;

            CryptoSymbol symbol = GetSymbolFromListBox();
            if (symbol == null)
                return;

            var href = TradingView.GetRef(symbol, interval);
            System.Diagnostics.Process.Start(href);
        }


        private void listBoxSymbols_DoubleClick(object sender, EventArgs e)
        {
            switch (GlobalData.Settings.General.DoubleClickAction)
            {
                case DoubleClickAction.activateTradingApp:
                    listBoxSymbolsMenuItemActivateTradingApp_Click(sender, e);
                    break;
                case DoubleClickAction.activateTradingAppAndTradingViewInternal:
                    listBoxSymbolsMenuItemActivateTradingApps_Click(sender, e);
                    break;
                case DoubleClickAction.activateTradingViewBrowerInternal:
                    listBoxSymbolsMenuItemActivateTradingviewInternal_Click(sender, e);
                    break;
                case DoubleClickAction.activateTradingViewBrowerExternal:
                    listBoxSymbolsMenuItemActivateTradingviewExternal_Click(sender, e);
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


        private void listBoxSymbolsMenuItemCopy_Click(object sender, EventArgs e)
        {
            // Show trend information
            CryptoSymbol symbol = GetSymbolFromListBox();
            if (symbol == null)
                return;

            Clipboard.SetText(symbol.Name, TextDataFormat.UnicodeText);
        }
    }
}
