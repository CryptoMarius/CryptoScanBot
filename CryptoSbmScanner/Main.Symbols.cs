using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner;

public partial class FrmMain
{
    private ContextMenuStrip ListViewSymbolsColumns;
    private ContextMenuStrip ListViewSymbolsMenuStrip;
    private ListViewHeaderContext ListViewSymbols;
    private System.Windows.Forms.Timer TimerModifyVolume;

    private ToolStripMenuItem CommandSymbolsActivateTradingApp;
    private ToolStripMenuItem CommandSymbolsActivateTradingViewInternal;
    private ToolStripMenuItem CommandSymbolsActivateTradingViewExternal;
    private ToolStripMenuItem CommandSymbolsTrendInformation;
    private ToolStripMenuItem CommandSymbolsCopyInformation;
    private ToolStripMenuItem CommandSymbolsInformationExcelDump;
    private ToolStripMenuItem CommandSymbolsExchangeInformationExcelDump;


    private void ListViewSymbolsConstructor()
    {
        ListViewSymbolsColumns = new();
        ListViewSymbolsMenuStrip = new();

        CommandSymbolsActivateTradingApp = new ToolStripMenuItem();
        CommandSymbolsActivateTradingApp.Text = "Activate trading app";
        CommandSymbolsActivateTradingApp.Tag = Command.ActivateTradingApp;
        CommandSymbolsActivateTradingApp.Click += Commands.ExecuteCommandCommandViaTag;
        ListViewSymbolsMenuStrip.Items.Add(CommandSymbolsActivateTradingApp);

        CommandSymbolsActivateTradingViewInternal = new ToolStripMenuItem();
        CommandSymbolsActivateTradingViewInternal.Text = "TradingView browser";
        CommandSymbolsActivateTradingViewInternal.Tag = Command.ActivateTradingviewIntern;
        CommandSymbolsActivateTradingViewInternal.Click += Commands.ExecuteCommandCommandViaTag;
        ListViewSymbolsMenuStrip.Items.Add(CommandSymbolsActivateTradingViewInternal);

        CommandSymbolsActivateTradingViewExternal = new ToolStripMenuItem();
        CommandSymbolsActivateTradingViewExternal.Text = "TradingView extern";
        CommandSymbolsActivateTradingViewExternal.Tag = Command.ActivateTradingviewExtern;
        CommandSymbolsActivateTradingViewExternal.Click += Commands.ExecuteCommandCommandViaTag;
        ListViewSymbolsMenuStrip.Items.Add(CommandSymbolsActivateTradingViewExternal);

        ListViewSymbolsMenuStrip.Items.Add(new ToolStripSeparator());

        CommandSymbolsCopyInformation = new ToolStripMenuItem();
        CommandSymbolsCopyInformation.Text = "Kopiëer informatie";
        CommandSymbolsCopyInformation.Click += CommandSymbolsCopyInformationExecute;
        ListViewSymbolsMenuStrip.Items.Add(CommandSymbolsCopyInformation);

        CommandSymbolsTrendInformation = new ToolStripMenuItem();
        CommandSymbolsTrendInformation.Text = "Trend informatie (zie log)";
        CommandSymbolsTrendInformation.Tag = Command.ShowTrendInformation;
        CommandSymbolsTrendInformation.Click += Commands.ExecuteCommandCommandViaTag;
        ListViewSymbolsMenuStrip.Items.Add(CommandSymbolsTrendInformation);

        CommandSymbolsInformationExcelDump = new ToolStripMenuItem();
        CommandSymbolsInformationExcelDump.Text = "Symbol informatie (Excel)";
        CommandSymbolsInformationExcelDump.Tag = Command.ExcelSymbolInformation;
        CommandSymbolsInformationExcelDump.Click += Commands.ExecuteCommandCommandViaTag;
        ListViewSymbolsMenuStrip.Items.Add(CommandSymbolsInformationExcelDump);

        CommandSymbolsExchangeInformationExcelDump = new ToolStripMenuItem();
        CommandSymbolsExchangeInformationExcelDump.Text = "Exchange informatie (Excel)";
        CommandSymbolsExchangeInformationExcelDump.Tag = Command.ExcelExchangeInformation;
        CommandSymbolsExchangeInformationExcelDump.Click += Commands.ExecuteCommandCommandViaTag;
        ListViewSymbolsMenuStrip.Items.Add(CommandSymbolsExchangeInformationExcelDump);


        ListViewSymbols = new()
        {
            Dock = DockStyle.Fill,
            Location = new Point(4, 3),
            ListViewItemSorter = new ListViewColumnSorterSymbol(),
        };
        ListViewSymbols.ColumnClick += ListViewSymbols_ColumnClick;

        ListViewSymbols.Tag = Command.ActivateTradingApp;
        ListViewSymbols.DoubleClick += Commands.ExecuteCommandCommandViaTag;
        ListViewSymbols.ContextMenuStrip = ListViewSymbolsMenuStrip;
        ListViewSymbols.HeaderContextMenuStrip = ListViewSymbolsColumns;

        ListViewColumnSorterSymbol bla = (ListViewColumnSorterSymbol)ListViewSymbols.ListViewItemSorter;
        bla.SortOrder = SortOrder.Ascending;


        tabPageSymbols.Controls.Add(ListViewSymbols);

        TimerModifyVolume = new()
        {
            Enabled = true,
            Interval = 1 * 60 * 1000,
        };
        TimerModifyVolume.Tick += TimerModifyVolumes_Tick;

        ListViewSymbolsInitColumns();

        // Wellicht later?
        //ListViewSymbolsColumns.Items.Clear();
        //foreach (ColumnHeader columnHeader in ListViewSymbols.Columns)
        //{
        //    if (columnHeader.Text != "")
        //    {
        //        ToolStripMenuItem item = new()
        //        {
        //            Tag = columnHeader,
        //            Text = columnHeader.Text,
        //            Size = new Size(100, 22),
        //            CheckState = CheckState.Unchecked,
        //            Checked = !GlobalData.Settings.HiddenSignalColumns.Contains(columnHeader.Text),
        //        };
        //        item.Click += CheckColumn;
        //        ListViewSymbolsColumns.Items.Add(item);
        //    }
        //}
    }


    private void ListViewSymbolsInitCaptions()
    {
        string text = GlobalData.ExternalUrls.GetTradingAppName(GlobalData.Settings.General.TradingApp, GlobalData.Settings.General.ExchangeName);
        CommandSymbolsActivateTradingApp.Text = text;
    }

    private void ListViewSymbolsInitColumns()
    {
        // Create columns and subitems. Width of -2 indicates auto-size
        ListViewSymbols.Columns.Add("Name", 150, HorizontalAlignment.Left);
        ListViewSymbols.Columns.Add("Volume", 80, HorizontalAlignment.Right);
        //ListViewSymbols.Columns.Add("Price", -2, HorizontalAlignment.Right);

        //ListViewSymbols.Columns.Add("", -2, HorizontalAlignment.Right); // filler

        ListViewSymbols.SetSortIcon(
              ((ListViewColumnSorterSymbol)ListViewSymbols.ListViewItemSorter).SortColumn,
              ((ListViewColumnSorterSymbol)ListViewSymbols.ListViewItemSorter).SortOrder);

        ResizeColumnsSymbol();
    }


    private void ResizeColumnsSymbol()
    {
        for (int i = 0; i <= ListViewSymbols.Columns.Count - 1; i++)
        {
            ColumnHeader columnHeader = ListViewSymbols.Columns[i];
            //if (GlobalData.Settings.HiddenSignalColumns.Contains(columnHeader.Text))
            //    columnHeader.Width = 0;
            //else
            {
                if (i != columnForText)
                {
                    if (columnHeader.Width != 0)
                        columnHeader.Width = -2;
                }
            }
        }
    }
    private static void FillSymbolItem(CryptoSymbol symbol, ListViewItem item1)
    {
        item1.SubItems.Clear();
        item1.Text = symbol.Name;
        item1.SubItems.Add(symbol.Volume.ToString("N0"));
        //item1.SubItems.Add(symbol.LastPrice.ToString0(symbol.PriceDisplayFormat));
    }

    private CryptoSymbol GetSymbolFromListBox()
    {
        if (ListViewSymbols.SelectedItems.Count > 0)
        {
            for (int index = 0; index < ListViewSymbols.SelectedItems.Count; index++)
            {
                ListViewItem item = ListViewSymbols.SelectedItems[index];

                // Neem de door de gebruiker geselecteerde coin
                string symbolName = item.SubItems[0].Text;
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

            }
        }

        return null;
    }

    private static ListViewItem AddSymbolItem(CryptoSymbol symbol)
    {
        ListViewItem item = new("", -1)
        {
            UseItemStyleForSubItems = false
        };
        FillSymbolItem(symbol, item);

        return item;
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
                ListViewSymbols.Invoke((MethodInvoker)(() => ListViewSymbols.BeginUpdate()));
                try
                {
                    string filter = "";
                    symbolFilter.Invoke((MethodInvoker)(() => filter = symbolFilter.Text.ToUpper()));

                    // De muntparen toevoegen aan de userinterface
                    foreach (var symbol in exchange.SymbolListName.Values)
                    {
                        if (symbol.QuoteData.FetchCandles && symbol.Status == 1)
                        {
                            if (!symbol.IsSpotTradingAllowed || symbol.IsBarometerSymbol())
                                continue;

                            bool addSymbol = true;
                            if (filter != "" && !symbol.Name.Contains(filter))
                                addSymbol = false;

                            ListViewItem item = null;
                            ListViewSymbols.Invoke((MethodInvoker)(() => item = ListViewSymbols.FindItemWithText(symbol.Name)));

                            if (item == null && addSymbol)
                            {
                                item = AddSymbolItem(symbol);
                                item.Tag = symbol;
                                FillSymbolItem(symbol, item);

                               ListViewSymbols.Invoke((MethodInvoker)(() => ListViewSymbols.Items.Add(item)));
                            }
                            if (item != null)
                            {
                                if (!addSymbol)
                                    ListViewSymbols.Invoke((MethodInvoker)(() => ListViewSymbols.Items.Remove(item)));
                                else
                                    ListViewSymbols.Invoke((MethodInvoker)(() => FillSymbolItem(symbol, item)));
                            }
                        }
                    }

                    Invoke((MethodInvoker)(() => ResizeColumnsSymbol()));

                }
                finally
                {
                    ListViewSymbols.Invoke((MethodInvoker)(() => ListViewSymbols.EndUpdate()));
                }



                // Beetje rare plek om deze te initialiseren, voila...
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

    
    private void TimerModifyVolumes_Tick(object sender, EventArgs e)
    {
        ListViewSymbols.BeginUpdate();
        try
        {
            for (int index = 0; index < ListViewSymbols.Items.Count; index++)
            {
                ListViewItem item = ListViewSymbols.Items[index];
                if (item.Tag is CryptoSymbol symbol)
                {
                    FillSymbolItem(symbol, item);
                }
            }
        }
        finally
        {
            ListViewSymbols.EndUpdate();
        }
    }


    private void CommandSymbolsCopyInformationExecute(object sender, EventArgs e)
    {
        CryptoSymbol symbol = GetSymbolFromListBox();
        if (symbol == null)
            Clipboard.Clear();
        else 
            Clipboard.SetText(symbol.Name, TextDataFormat.UnicodeText);
    }

    //private void ListViewSymbolsMenuItemCreateSignal_Click(object sender, EventArgs e)
    //{
    //    //    // Neem de door de gebruiker geselecteerde coin
    //    //    string symbolName = ListViewSymbols.Text.ToString();
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


    private void ListViewSymbols_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        ListViewSymbols.BeginUpdate();
        try
        {
            // Perform the sort with these new sort options.
            ListViewColumnSorterSymbol listViewColumnSorter = (ListViewColumnSorterSymbol)ListViewSymbols.ListViewItemSorter;
            listViewColumnSorter.ClickedOnColumn(e.Column);
            ListViewSymbols.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.SortOrder);
            ListViewSymbols.Sort();

            SetBackGroundColorsSignals();
        }
        finally
        {
            ListViewSymbols.EndUpdate();
        }
    }
}