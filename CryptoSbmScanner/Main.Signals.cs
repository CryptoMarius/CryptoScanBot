using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner;

public partial class FrmMain
{
    private readonly int columnForText = 6;
    private readonly int columnForPriceDiff = 8;
    private ListViewDoubleBuffered listViewSignals;
    private System.Windows.Forms.Timer TimerClearEvents;

    //public void Dispose()
    //{
    //    if (TimerClearEvents != null) { TimerClearEvents.Dispose(); TimerClearEvents = null; }
    //}

    private void ListViewSignalsConstructor()
    {
        ListViewColumnSorterSignal listViewColumnSorter = new()
        {
            SortOrder = SortOrder.Descending
        };

        // ruzie (component of events raken weg), dan maar dynamisch
        listViewSignals = new()
        {
            CheckBoxes = false
        };
        listViewSignals.CheckBoxes = false;
        listViewSignals.AllowColumnReorder = false;
        listViewSignals.Dock = DockStyle.Fill;
        listViewSignals.Location = new Point(4, 3);
        listViewSignals.GridLines = true;
        listViewSignals.View = View.Details;
        listViewSignals.FullRowSelect = true;
        listViewSignals.HideSelection = true;
        listViewSignals.BorderStyle = BorderStyle.None;
        listViewSignals.ContextMenuStrip = listViewSignalsMenuStrip;
        listViewSignals.ListViewItemSorter = listViewColumnSorter;
        listViewSignals.ColumnClick += ListViewSignals_ColumnClick;
        listViewSignals.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.SortOrder);
        listViewSignals.DoubleClick += ListViewSignalsMenuItem_DoubleClick;
        tabPageSignals.Controls.Add(listViewSignals);

        TimerClearEvents = new()
        {
            Enabled = true,
            Interval = 1 * 60 * 1000,
        };
        TimerClearEvents.Tick += TimerClearOldSignals_Tick;

        ListViewSignalsInitColumns();
    }


    private void ListViewSignalsInitCaptions()
    {
        switch (GlobalData.Settings.General.TradingApp)
        {
            case CryptoTradingApp.Altrady:
                listViewSignalsMenuItemActivateTradingApp.Text = "Altrady";
                listViewSignalsMenuItemActivateTradingApps.Text = "Altrady + TradingView";
                break;
            case CryptoTradingApp.Hypertrader:
                listViewSignalsMenuItemActivateTradingApp.Text = "Hypertrader";
                listViewSignalsMenuItemActivateTradingApps.Text = "Hypertrader + TradingView";
                break;
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
        listViewSignals.Columns.Add("Text", 250, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Price", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Stijging", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Volume", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Trend", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Trend%", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("24h Change", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("24h Beweging", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("BB%", -2, HorizontalAlignment.Right);
        if (!GlobalData.Settings.General.HideTechnicalStuffSignals)
        {
            listViewSignals.Columns.Add("", 20, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("RSI", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("Stoch", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("Signal", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("Sma200", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("Sma50", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("Sma20", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("PSar", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("Flux 5m", -2, HorizontalAlignment.Right);
        }
        listViewSignals.Columns.Add("", -2, HorizontalAlignment.Right); // filler

        for (int i = 0; i <= listViewSignals.Columns.Count - 1; i++)
        {
            if (i != columnForText)
                listViewSignals.Columns[i].Width = -2;
        }
    }

    private static void FillSignalItem(CryptoSignal signal, ListViewItem item1)
    {
        item1.SubItems.Clear();

        if (signal.ItemIndex % 1 > 0)
            item1.BackColor = Color.LightGray;
        
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
                item1.SubItems.Add("Bearisch");
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


        if (!GlobalData.Settings.General.HideTechnicalStuffSignals)
        {
            item1.SubItems.Add(" ");


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

            if (GlobalData.Settings.General.ShowFluxIndicator5m)
            {
                value = signal.FluxIndicator5m;
                if (value != 0)
                    item1.SubItems.Add(value?.ToString("N0"));
            }
            else
                item1.SubItems.Add("");

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

            // Deze redelijk kostbaar? (alles moet gecontroleerd worden)
            for (int i = 0; i <= listViewSignals.Columns.Count - 1; i++)
            {
                if (i != columnForText) // text
                    listViewSignals.Columns[i].Width = -2;
            }

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
                    signal.ItemIndex = index;

                    DateTime expirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.General.RemoveSignalAfterxCandles * signal.Interval.Duration); // 15 candles further (display)
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


    //private void ListViewSignalsMenuItemClearSignals_Click(object sender, EventArgs e)
    //{
    //    listViewSignals.BeginUpdate();
    //    try
    //    {
    //        listViewSignals.Clear();
    //        ListViewSignalsInitColumns();
    //    }
    //    finally
    //    {
    //        listViewSignals.EndUpdate();
    //    }
    //}


    private void ListViewSignalsMenuItemActivateTradingApp_Click(object sender, EventArgs e)
    {
        if (listViewSignals.SelectedItems.Count > 0)
        {
            for (int index = 0; index < listViewSignals.SelectedItems.Count; index++)
            {
                ListViewItem item = listViewSignals.SelectedItems[index];
                CryptoSignal signal = (CryptoSignal)item.Tag;
                ActivateTradingApp(signal.Symbol, signal.Interval);
            }
        }
    }


    private void ListViewSignalsMenuItemActivateTradingApps_Click(object sender, EventArgs e)
    {
        ListViewSignalsMenuItemActivateTradingApp_Click(sender, e);
        ListViewSignalsMenuItemActivateTradingViewInternal_Click(sender, e);
    }


    private void ListViewSignalsMenuItemActivateTradingViewInternal_Click(object sender, EventArgs e)
    {
        if (listViewSignals.Items.Count > 0)
        {
            ListViewItem item = listViewSignals.SelectedItems[0];
            CryptoSignal signal = (CryptoSignal)item.Tag;

            (string Url, bool Execute) refInfo;
            refInfo = ExchangeHelper.GetExternalRef(CryptoTradingApp.TradingView, false, signal.Symbol, signal.Interval);
            webViewTradingView.Source = new(refInfo.Url);

            tabControl.SelectedTab = tabPageBrowser;
        }
    }


    private void ListViewSignalsMenuItemActivateTradingviewExternal_Click(object sender, EventArgs e)
    {
        if (listViewSignals.SelectedItems.Count > 0)
        {
            for (int index = 0; index < listViewSignals.SelectedItems.Count; index++)
            {
                ListViewItem item = listViewSignals.SelectedItems[index];
                CryptoSignal signal = (CryptoSignal)item.Tag;
                (string Url, bool Execute) refInfo;
                refInfo = ExchangeHelper.GetExternalRef(CryptoTradingApp.TradingView, false, signal.Symbol, signal.Interval);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(refInfo.Url) { UseShellExecute = true });
            }
        }
    }


    private void ListViewSignalsMenuItem_DoubleClick(object sender, EventArgs e)
    {
        switch (GlobalData.Settings.General.DoubleClickAction)
        {
            case DoubleClickAction.activateTradingApp:
                ListViewSignalsMenuItemActivateTradingApp_Click(sender, e);
                break;
            case DoubleClickAction.activateTradingAppAndTradingViewInternal:
                ListViewSignalsMenuItemActivateTradingApps_Click(sender, e);
                break;
            case DoubleClickAction.activateTradingViewBrowerInternal:
                ListViewSignalsMenuItemActivateTradingViewInternal_Click(sender, e);
                break;
            case DoubleClickAction.activateTradingViewBrowerExternal:
                ListViewSignalsMenuItemActivateTradingviewExternal_Click(sender, e);
                break;
        }
    }

    private void MenuSignalsShowTrendInformation_Click(object sender, EventArgs e)
    {
        // Show trend information
        if (listViewSignals.SelectedItems.Count > 0)
        {
            for (int index = 0; index < listViewSignals.SelectedItems.Count; index++)
            {
                ListViewItem item = listViewSignals.SelectedItems[index];
                CryptoSignal signal = (CryptoSignal)item.Tag;

                ShowTrendInformation(signal.Symbol);
            }
        }
    }

    private void ListViewSignalsMenuItemCopySignal_Click(object sender, EventArgs e)
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

    private void ListViewSignals_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        // Perform the sort with these new sort options.
        ListViewColumnSorterSignal listViewColumnSorter = (ListViewColumnSorterSignal)listViewSignals.ListViewItemSorter;
        listViewColumnSorter.ClickedOnColumn(e.Column);
        listViewSignals.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.SortOrder);
        listViewSignals.Sort();
    }
}
