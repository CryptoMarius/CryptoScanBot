using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner;

public partial class FrmMain
{
    private static int columnForPriceDiff;
    private ContextMenuStrip ListViewSignalsColumns;
    private ContextMenuStrip ListViewSignalsMenuStrip;
    private ListViewHeaderContext listViewSignals;
    private System.Windows.Forms.Timer TimerClearEvents;

    private ToolStripMenuItem CommandSignalsActivateTradingApp;
    private ToolStripMenuItem CommandSignalsActivateTradingViewInternal;
    private ToolStripMenuItem CommandSignalsActivateTradingViewExternal;
    private ToolStripMenuItem CommandSignalTrendInformation;
    private ToolStripMenuItem CommandSignalCopyInformation;
    private ToolStripMenuItem CommandSignalSymbolInformationExcelDump;


    private void ListViewSignalsConstructor()
    {
        ListViewSignalsColumns = new ContextMenuStrip();
        ListViewSignalsMenuStrip = new ContextMenuStrip();

        CommandSignalsActivateTradingApp = new ToolStripMenuItem();
        CommandSignalsActivateTradingApp.Text = "Activate trading app";
        CommandSignalsActivateTradingApp.Tag = Command.ActivateTradingApp;
        CommandSignalsActivateTradingApp.Click += Commands.ExecuteCommandCommandViaTag;
        ListViewSignalsMenuStrip.Items.Add(CommandSignalsActivateTradingApp);

        CommandSignalsActivateTradingViewInternal = new ToolStripMenuItem();
        CommandSignalsActivateTradingViewInternal.Text = "TradingView browser";
        CommandSignalsActivateTradingViewInternal.Tag = Command.ActivateTradingviewIntern;
        CommandSignalsActivateTradingViewInternal.Click += Commands.ExecuteCommandCommandViaTag;
        ListViewSignalsMenuStrip.Items.Add(CommandSignalsActivateTradingViewInternal);

        CommandSignalsActivateTradingViewExternal = new ToolStripMenuItem();
        CommandSignalsActivateTradingViewExternal.Text = "TradingView extern";
        CommandSignalsActivateTradingViewExternal.Tag = Command.ActivateTradingviewExtern;
        CommandSignalsActivateTradingViewExternal.Click += Commands.ExecuteCommandCommandViaTag;
        ListViewSignalsMenuStrip.Items.Add(CommandSignalsActivateTradingViewExternal);

        ListViewSignalsMenuStrip.Items.Add(new ToolStripSeparator());

        CommandSignalCopyInformation = new ToolStripMenuItem();
        CommandSignalCopyInformation.Text = "Kopiëer informatie";
        CommandSignalCopyInformation.Click += CommandSignalCopyInformationExecute;
        ListViewSignalsMenuStrip.Items.Add(CommandSignalCopyInformation);

        CommandSignalTrendInformation = new ToolStripMenuItem();
        CommandSignalTrendInformation.Text = "Trend informatie (zie log)";
        CommandSignalTrendInformation.Tag = Command.ShowTrendInformation;
        CommandSignalTrendInformation.Click += Commands.ExecuteCommandCommandViaTag;
        ListViewSignalsMenuStrip.Items.Add(CommandSignalTrendInformation);

        CommandSignalSymbolInformationExcelDump = new ToolStripMenuItem();
        CommandSignalSymbolInformationExcelDump.Text = "Symbol informatie (Excel)";
        CommandSignalSymbolInformationExcelDump.Tag = Command.ExcelSymbolInformation;
        CommandSignalSymbolInformationExcelDump.Click += Commands.ExecuteCommandCommandViaTag;
        ListViewSignalsMenuStrip.Items.Add(CommandSignalSymbolInformationExcelDump);


        //ListViewColumnSorterSignal listViewColumnSorter = new();

        // ruzie (component of events raken weg), dan maar dynamisch
        listViewSignals = new()
        {
            Dock = DockStyle.Fill,
            Location = new Point(4, 3),
            ListViewItemSorter = new ListViewColumnSorterSignal(),
        };
        listViewSignals.ColumnClick += ListViewSignals_ColumnClick;

        listViewSignals.Tag = Command.ActivateTradingApp;
        listViewSignals.DoubleClick += Commands.ExecuteCommandCommandViaTag;
        //listViewSignals.DoubleClick += CommandSignalsActivateTradingAppExecute;

        listViewSignals.ContextMenuStrip = ListViewSignalsMenuStrip;
        listViewSignals.HeaderContextMenuStrip = ListViewSignalsColumns;


        tabPageSignals.Controls.Add(listViewSignals);

        TimerClearEvents = new()
        {
            Enabled = true,
            Interval = 1 * 60 * 1000,
        };
        TimerClearEvents.Tick += TimerClearOldSignals_Tick;

        ListViewSignalsInitColumns();

        ListViewSignalsColumns.Items.Clear();
        foreach (ColumnHeader columnHeader in listViewSignals.Columns)
        {
            if (columnHeader.Text != "")
            {
                //x.Width = 0;
                //hide: LVW.Columns.Item(0).Width = 0
                //show again: LVW.Columns.Item(0).AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent)
                //Alleen een dictionary nodig voor de opslag?

                ToolStripMenuItem item = new()
                {
                    Tag = columnHeader,
                    Text = columnHeader.Text,
                    Size = new Size(100, 22),
                    CheckState = CheckState.Unchecked,
                    Checked = !GlobalData.Settings.HiddenSignalColumns.Contains(columnHeader.Text),
                };
                item.Click += CheckColumn;
                ListViewSignalsColumns.Items.Add(item);
            }
        }
    }

    private void ListViewSignalsInitCaptions()
    {
        string text = GlobalData.ExternalUrls.GetTradingAppName(GlobalData.Settings.General.TradingApp, GlobalData.Settings.General.ExchangeName);
        CommandSignalsActivateTradingApp.Text = text;
    }


    private void CheckColumn(object sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            item.Checked = !item.Checked;
            if (item.Checked)
                GlobalData.Settings.HiddenSignalColumns.Remove(item.Text);
            else
                GlobalData.Settings.HiddenSignalColumns.Add(item.Text);
            GlobalData.SaveSettings();


            listViewSignals.BeginUpdate();
            try
            {
                if (item.Tag is ColumnHeader columnHeader)
                {
                    if (item.Checked)
                    {
                        //listViewSignals.AutoResizeColumn(columnHeader.Index, ColumnHeaderAutoResizeStyle.HeaderSize);
                        listViewSignals.AutoResizeColumn(columnHeader.Index, ColumnHeaderAutoResizeStyle.ColumnContent);
                    }
                }
                ResizeColumns();
            }
            finally
            {
                listViewSignals.EndUpdate();
            }
        }
    }

    private void ResizeColumns()
    {
        for (int i = 0; i <= listViewSignals.Columns.Count - 1; i++)
        {
            ColumnHeader columnHeader = listViewSignals.Columns[i];
            if (GlobalData.Settings.HiddenSignalColumns.Contains(columnHeader.Text))
                columnHeader.Width = 0;
            else
            {
                //if (i != columnForText)
                {
                    if (columnHeader.Width != 0)
                        columnHeader.Width = -2;
                }
            }
        }
    }

    private void ListViewSignalsInitColumns()
    {
        // Create columns and subitems. Width of -2 indicates auto-size
        listViewSignals.Columns.Add("Candle datum", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Exchange", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Symbol", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Interval", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Mode", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Strategie", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Text", 125, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Price", -2, HorizontalAlignment.Right);
        columnForPriceDiff = listViewSignals.Columns.Count;
        listViewSignals.Columns.Add("Stijging", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Volume", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Trend", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Trend%", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("24h Change", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("24h Beweging", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("BB%", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("RSI", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Stoch", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Signal", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Sma200", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Sma50", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Sma20", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("PSar", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Flux 5m", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("FundingRate", -2, HorizontalAlignment.Right);


        listViewSignals.Columns.Add("1h", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("4h", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("12h", -2, HorizontalAlignment.Left);

        listViewSignals.Columns.Add("", -2, HorizontalAlignment.Right); // filler

        listViewSignals.SetSortIcon(
              ((ListViewColumnSorterSignal)listViewSignals.ListViewItemSorter).SortColumn,
              ((ListViewColumnSorterSignal)listViewSignals.ListViewItemSorter).SortOrder);

        ResizeColumns();
    }



    private static void FillSignalItem(CryptoSignal signal, ListViewItem item1)
    {
        item1.SubItems.Clear();


        item1.Text = signal.OpenDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + signal.OpenDate.AddSeconds(signal.Interval.Duration).ToLocalTime().ToString("HH:mm");

        ListViewItem.ListViewSubItem subItem;

        item1.SubItems.Add(signal.Exchange.Name);


        string s = signal.Symbol.Base + "/" + @signal.Symbol.Quote;
        //if (GlobalData.Settings.Signal.LogMinimumTickPercentage)
        //{
        decimal tickPercentage = 100 * signal.Symbol.PriceTickSize / signal.Price;
        if (tickPercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
        {
            s += " " + tickPercentage.ToString("N2");
            subItem = item1.SubItems.Add(s);
            subItem.ForeColor = Color.Red;
        }
        else subItem = item1.SubItems.Add(s);
        //}
        //else subItem = item1.SubItems.Add(s);

        Color displayColor = signal.Symbol.QuoteData.DisplayColor;
        if (displayColor != Color.White)
            subItem.BackColor = displayColor;



        item1.SubItems.Add(signal.Interval.Name);


        subItem = item1.SubItems.Add(signal.SideText);
        if (!signal.IsInvalid)
        {
            if (signal.Side == CryptoOrderSide.Buy)
                subItem.ForeColor = Color.Green;
            else if (signal.Side == CryptoOrderSide.Sell)
                subItem.ForeColor = Color.Red;
        }


        subItem = item1.SubItems.Add(signal.StrategyText);
        if (!signal.IsInvalid)
        {
            switch (signal.Strategy)
            {
                case CryptoSignalStrategy.Jump:
                    if (GlobalData.Settings.Signal.ColorJump != Color.White)
                        subItem.BackColor = GlobalData.Settings.Signal.ColorJump;
                    break;

                case CryptoSignalStrategy.Stobb:
                    if (GlobalData.Settings.Signal.ColorStobb != Color.White)
                        subItem.BackColor = GlobalData.Settings.Signal.ColorStobb;
                    break;

                case CryptoSignalStrategy.Sbm1:
                case CryptoSignalStrategy.Sbm2:
                case CryptoSignalStrategy.Sbm3:
                case CryptoSignalStrategy.Sbm4:
                case CryptoSignalStrategy.Sbm5:
                    if (GlobalData.Settings.Signal.ColorSbm != Color.White)
                        subItem.BackColor = GlobalData.Settings.Signal.ColorSbm;
                    break;

            }
        }

        item1.SubItems.Add(signal.EventText);
        item1.SubItems.Add(signal.Price.ToString(signal.Symbol.PriceDisplayFormat));

        //subItem = item1.SubItems.Add(signal.Symbol.LastPrice.ToString0(signal.Symbol.DisplayFormat));
        subItem = item1.SubItems.Add(signal.PriceDiff?.ToString("N2"));
        if (signal.Symbol.LastPrice > signal.Price)
            subItem.ForeColor = Color.Green;
        else if (signal.Symbol.LastPrice < signal.Price)
            subItem.ForeColor = Color.Red;

        item1.SubItems.Add(signal.Volume.ToString("N0"));

        switch (signal.TrendIndicator)
        {
            case CryptoTrendIndicator.trendBullish:
                item1.SubItems.Add("Bullish");
                break;
            case CryptoTrendIndicator.trendBearish:
                item1.SubItems.Add("Bearish");
                break;
            default:
                item1.SubItems.Add("Zijwaarts");
                break;
        }


        double? value;
        value = signal.TrendPercentage;
        if (value < 0)
            item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Red;
        else
            item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Green;


        value = signal.Last24HoursChange;
        if (signal.Last24HoursChange < 0)
            item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Red;
        else
            item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Green;


        value = signal.Last24HoursEffective;
        //if (signal.Last24HoursEffective < 0)
        //    item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Red;
        //else
        //    item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Green;
        //if (signal.Last24HoursEffective > 25)
        //  item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Red;
        //else
        item1.SubItems.Add(value?.ToString("N2"));

        //value = signal.Last48HoursChange;
        //if (signal.Last48HoursChange < 0)
        //    item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Red;
        //else
        //    item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Green;


        //value = signal.Last48HoursEffective;
        //if (signal.Last48HoursEffective < 0)
        //    item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Red;
        //else
        //    item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Green;

        value = signal.BollingerBandsPercentage;
        item1.SubItems.Add(value?.ToString("N2"));


        //item1.SubItems.Add(" ");


        // Oversold/overbougt
        value = signal.Rsi; // 0..100
        if (value < 30f)
            item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Red;
        else if (value > 70f)
            item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Green;
        else
            item1.SubItems.Add(value?.ToString("N2"));

        // Oversold/overbougt
        value = signal.StochOscillator;
        if (value < 20f)
            item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Red;
        else if (value > 80f)
            item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Green;
        else
            item1.SubItems.Add(value?.ToString("N2"));

        // Oversold/overbougt
        value = signal.StochSignal;
        if (value < 20f)
            item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Red;
        else if (value > 80f)
            item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Green;
        else
            item1.SubItems.Add(value?.ToString("N2"));


        value = signal.Sma200;
        item1.SubItems.Add(value?.ToString(signal.Symbol.PriceDisplayFormat));

        value = signal.Sma50;
        if (value < signal.Sma200)
            item1.SubItems.Add(value?.ToString(signal.Symbol.PriceDisplayFormat)).ForeColor = Color.Green;
        else if (value > signal.Sma200)
            item1.SubItems.Add(value?.ToString(signal.Symbol.PriceDisplayFormat)).ForeColor = Color.Red;
        else
            item1.SubItems.Add(value?.ToString(signal.Symbol.PriceDisplayFormat));

        value = signal.Sma20;
        if (value < signal.Sma50)
            item1.SubItems.Add(value?.ToString(signal.Symbol.PriceDisplayFormat)).ForeColor = Color.Green;
        else if (value > signal.Sma50)
            item1.SubItems.Add(value?.ToString(signal.Symbol.PriceDisplayFormat)).ForeColor = Color.Red;
        else
            item1.SubItems.Add(value?.ToString(signal.Symbol.PriceDisplayFormat));

        // de psar zou je wel mogen "clampen"???
        value = signal.PSar; //.Clamp(signal.Symbol.PriceMinimum, signal.Symbol.PriceMaximum, signal.Symbol.PriceTickSize);
        string orgValue = value?.ToString(signal.Symbol.PriceDisplayFormat);
        if (value <= signal.Sma20)
            item1.SubItems.Add(orgValue).ForeColor = Color.Green;
        else if (value > signal.Sma20)
            item1.SubItems.Add(orgValue).ForeColor = Color.Red;
        else
            item1.SubItems.Add(orgValue);

        value = signal.FluxIndicator5m;
        if (value != 0)
            item1.SubItems.Add(value?.ToString("N0"));
        else
            item1.SubItems.Add("");

        if (signal.Symbol.FundingRate != 0.0m)
        {
            subItem = item1.SubItems.Add(signal.Symbol.FundingRate.ToString());
            if (signal.Symbol.FundingRate > 0)
                subItem.ForeColor = Color.Green;
            else if (signal.Symbol.FundingRate < 0)
                subItem.ForeColor = Color.Red;
        }
        else
            item1.SubItems.Add("");


        // Voor Eric (experiment)
        var trend = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h).TrendIndicator;
        switch (trend)
        {
            case CryptoTrendIndicator.trendBullish:
                subItem = item1.SubItems.Add("Up");
                subItem.ForeColor = Color.Green;
                break;
            case CryptoTrendIndicator.trendBearish:
                subItem = item1.SubItems.Add("Down");
                subItem.ForeColor = Color.Red;
                break;
            default:
                item1.SubItems.Add("None");
                break;
        }

        trend = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval4h).TrendIndicator;
        switch (trend)
        {
            case CryptoTrendIndicator.trendBullish:
                subItem = item1.SubItems.Add("Up");
                subItem.ForeColor = Color.Green;
                break;
            case CryptoTrendIndicator.trendBearish:
                subItem = item1.SubItems.Add("Down");
                subItem.ForeColor = Color.Red;
                break;
            default:
                item1.SubItems.Add("None");
                break;
        }

        trend = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval12h).TrendIndicator;
        switch (trend)
        {
            case CryptoTrendIndicator.trendBullish:
                subItem = item1.SubItems.Add("Up");
                subItem.ForeColor = Color.Green;
                break;
            case CryptoTrendIndicator.trendBearish:
                subItem = item1.SubItems.Add("Down");
                subItem.ForeColor = Color.Red;
                break;
            default:
                item1.SubItems.Add("None");
                break;
        }
    }

    private static ListViewItem AddSignalItem(CryptoSignal signal)
    {
        ListViewItem item = new("", -1)
        {
            UseItemStyleForSubItems = false
        };
        FillSignalItem(signal, item);

        return item;
    }


    private void ListViewSignalsAddSignalRange(List<CryptoSignal> signals)
    {
        listViewSignals.BeginUpdate();
        try
        {
            List<ListViewItem> range = new();
            foreach (CryptoSignal signal in signals)
            {
                ListViewItem item = AddSignalItem(signal);
                item.Tag = signal;
                range.Add(item);
            }

            listViewSignals.Items.AddRange(range.ToArray());
            SetBackGroundColorsSignals();

            // Deze redelijk kostbaar? (alles moet gecontroleerd worden)
            ResizeColumns();

            //if (signals.Count > 1)
            //    GlobalData.AddTextToLogTab(signals.Count + " signalen als een range toegevoegd");
        }
        finally
        {
            listViewSignals.EndUpdate();
        }
    }


    private void TimerClearOldSignals_Tick(object sender, EventArgs e)
    {
        // Circa 1x per minuut de verouderde signalen opruimen
        if (listViewSignals.Items.Count > 0)
        {
            bool startedUpdating = false;
            try
            {
                ListViewItem.ListViewSubItem subItem;

                for (int index = listViewSignals.Items.Count - 1; index >= 0; index--)
                {
                    ListViewItem item = listViewSignals.Items[index];
                    CryptoSignal signal = (CryptoSignal)item.Tag;

                    DateTime expirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.General.RemoveSignalAfterxCandles * signal.Interval.Duration);
                    if (expirationDate < DateTime.UtcNow)
                    {
                        if (!startedUpdating)
                        {
                            listViewSignals.BeginUpdate();
                            startedUpdating = true;
                        }
                        listViewSignals.Items.RemoveAt(index);
                    }
                    else
                    {
                        subItem = item.SubItems[columnForPriceDiff];
                        //subItem.Text = signal.Symbol.LastPrice.ToString0(signal.Symbol.DisplayFormat);
                        subItem.Text = signal.PriceDiff?.ToString("N2");
                        if (signal.Symbol.LastPrice > signal.Price)
                            subItem.ForeColor = Color.Green;
                        else if (signal.Symbol.LastPrice < signal.Price)
                            subItem.ForeColor = Color.Red;
                    }

                }
            }
            finally
            {
                if (startedUpdating)
                    listViewSignals.EndUpdate();
            }
        }
    }

    private void CommandSignalCopyInformationExecute(object sender, EventArgs e)
    {
        string text = "";
        if (listViewSignals.SelectedItems.Count > 0)
        {
            for (int index = 0; index < listViewSignals.SelectedItems.Count; index++)
            {
                ListViewItem item = listViewSignals.SelectedItems[index];

                text += item.Text + ";";
                foreach (ListViewItem.ListViewSubItem i in item.SubItems)
                {
                    text += i.Text + ";";
                }
                text += "\r\n";

            }
        }
        Clipboard.SetText(text, TextDataFormat.UnicodeText);
    }

    /// <summary>
    /// Roulerend de regels een iets andere achtergrond kleur geven
    /// </summary>
    private void SetBackGroundColorsSignal(ListViewItem item, CryptoSignal signal)
    {
        // https://www.color-hex.com/color/d3d3d3

        signal.ItemIndex = listViewSignals.Items.IndexOf(item);

        Color veryLightGray = Color.FromArgb(0xf1, 0xf1, 0xf1);

        if (signal.ItemIndex % 2 == 0)
        {
            if (item.BackColor == Color.White)
                item.BackColor = veryLightGray;
        }
        else if (item.BackColor == veryLightGray)
            item.BackColor = Color.White;

        foreach (ListViewItem.ListViewSubItem subItem in item.SubItems)
        {
            if (signal.ItemIndex % 2 == 0)
            {
                if (subItem.BackColor == Color.White)
                    subItem.BackColor = veryLightGray;
            }
            else if (subItem.BackColor == veryLightGray)
                subItem.BackColor = Color.White;
        }
    }

    private void SetBackGroundColorsSignals()
    {
        listViewSignals.BeginUpdate();
        try
        {
            foreach (ListViewItem item in listViewSignals.Items)
            {
                CryptoSignal signal = (CryptoSignal)item.Tag;
                SetBackGroundColorsSignal(item, signal);
            }
        }
        finally
        {
            listViewSignals.EndUpdate();
        }
    }

    private void ListViewSignals_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        listViewSignals.BeginUpdate();
        try
        {
            // Perform the sort with these new sort options.
            ListViewColumnSorterSignal listViewColumnSorter = (ListViewColumnSorterSignal)listViewSignals.ListViewItemSorter;
            listViewColumnSorter.ClickedOnColumn(e.Column);
            listViewSignals.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.SortOrder);
            listViewSignals.Sort();

            SetBackGroundColorsSignals();
        }
        finally
        {
            listViewSignals.EndUpdate();
        }
    }

}
