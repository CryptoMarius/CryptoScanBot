using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner;

public partial class FrmMain
{

    private void ListViewSignalsInitCaptions()
    {
        switch (GlobalData.Settings.General.TradingApp)
        {
            case TradingApp.Altrady:
                listViewSignalsMenuItemActivateTradingApp.Text = "Altrady";
                listViewSignalsMenuItemActivateTradingApps.Text = "Altrady + TradingView";
                break;
            case TradingApp.Hypertrader:
                listViewSignalsMenuItemActivateTradingApp.Text = "Hypertrader";
                listViewSignalsMenuItemActivateTradingApps.Text = "Hypertrader + TradingView";
                break;
        }
    }

    private void ListViewSignalsInitColumns()
    {


        ListViewColumnSorter listViewColumnSorter = new()
        {
            SortOrder = SortOrder.Descending
        };
        this.listViewSignals.ListViewItemSorter = listViewColumnSorter;
        this.listViewSignals.ColumnClick += ListViewSignals_ColumnClick;
        //this.listViewSignals.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.Order);


        // Create columns and subitems. Width of -2 indicates auto-size
        listViewSignals.Columns.Add("Candle date", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Symbol", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Interval", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Mode", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Text", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Event", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Price", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Volume", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Trend", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Trend%", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("Last24", -2, HorizontalAlignment.Right);
        listViewSignals.Columns.Add("BB%", -2, HorizontalAlignment.Right);
        if (!GlobalData.Settings.Signal.HideTechnicalStuffSignals)
        {
            listViewSignals.Columns.Add("", 20, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("RSI", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("Stoch", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("Signal", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("Sma200", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("Sma50", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("Sma20", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("PSar", -2, HorizontalAlignment.Right);
#if DEBUG
            listViewSignals.Columns.Add("PSarDave", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("PSarJason", -2, HorizontalAlignment.Right);
            listViewSignals.Columns.Add("PSarTulip", -2, HorizontalAlignment.Right);
#endif
        }
        listViewSignals.Columns.Add("", -2, HorizontalAlignment.Right); // filler

        for (int i = 0; i <= listViewSignals.Columns.Count - 1; i++)
        {
            listViewSignals.Columns[i].Width = -2;
        }
    }


    private static ListViewItem AddSignalItem(CryptoSignal signal)
    {
        ListViewItem.ListViewSubItem subItem;

        string s = signal.OpenDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + signal.OpenDate.AddSeconds(signal.Interval.Duration).ToLocalTime().ToString("HH:mm");
        ListViewItem item1 = new(s, -1)
        {
            UseItemStyleForSubItems = false,
            Tag = signal
        };


        s = signal.Symbol.Base + "/" + @signal.Symbol.Quote;
        if (GlobalData.Settings.Signal.LogMinimumTickPercentage)
        {
            decimal tickPercentage = 100 * signal.Symbol.PriceTickSize / signal.Price;
            if (tickPercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
            {
                s += " " + tickPercentage.ToString("N2");
                subItem = item1.SubItems.Add(s);
                subItem.ForeColor = Color.Red;
            }
            else subItem = item1.SubItems.Add(s);
        }
        else subItem = item1.SubItems.Add(s);

        Color displayColor = signal.Symbol.QuoteData.DisplayColor;
        if (displayColor != Color.White)
            subItem.BackColor = displayColor;



        item1.SubItems.Add(signal.Interval.Name);


        subItem = item1.SubItems.Add(signal.ModeText);
        if (signal.Mode == SignalMode.modeLong)
            subItem.ForeColor = Color.Green;
        else if (signal.Mode == SignalMode.modeShort)
            subItem.ForeColor = Color.Red;


        subItem = item1.SubItems.Add(signal.StrategyText);
        switch (signal.Strategy)
        {
            case SignalStrategy.strategyCandlesJumpUp:
            case SignalStrategy.strategyCandlesJumpDown:
                if (GlobalData.Settings.Signal.ColorJump != Color.White)
                    subItem.BackColor = GlobalData.Settings.Signal.ColorJump;
                break;

            case SignalStrategy.strategyStobbOverbought:
            case SignalStrategy.strategyStobbOversold:
                if (GlobalData.Settings.Signal.ColorStobb != Color.White)
                    subItem.BackColor = GlobalData.Settings.Signal.ColorStobb;
                break;

            case SignalStrategy.strategySbm1Overbought:
            case SignalStrategy.strategySbm1Oversold:
            case SignalStrategy.strategySbm2Overbought:
            case SignalStrategy.strategySbm2Oversold:
            case SignalStrategy.strategySbm3Overbought:
            case SignalStrategy.strategySbm3Oversold:
                if (GlobalData.Settings.Signal.ColorSbm != Color.White)
                    subItem.BackColor = GlobalData.Settings.Signal.ColorSbm;
                break;

            case SignalStrategy.strategyPriceCrossedMa20:
            case SignalStrategy.strategyPriceCrossedMa50:
                if (GlobalData.Settings.Signal.ColorJump != Color.White)
                    subItem.BackColor = GlobalData.Settings.Signal.ColorJump;
                break;
        }


        item1.SubItems.Add(signal.EventText);
        item1.SubItems.Add(signal.Price.ToString(signal.Symbol.DisplayFormat));
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

        if (signal.TrendPercentage < 0)
            item1.SubItems.Add(signal.TrendPercentage.ToString("N2")).ForeColor = Color.Red;
        else
            item1.SubItems.Add(signal.TrendPercentage.ToString("N2")).ForeColor = Color.Green;

        if (signal.Last24Hours < 0)
            item1.SubItems.Add(signal.Last24Hours.ToString("N2")).ForeColor = Color.Red;
        else
            item1.SubItems.Add(signal.Last24Hours.ToString("N2")).ForeColor = Color.Green;


        item1.SubItems.Add(signal.BollingerBandsPercentage.ToString("N2"));


        if (!GlobalData.Settings.Signal.HideTechnicalStuffSignals)
        {
            item1.SubItems.Add(" ");
            float value;


            // Oversold/overbougt
            value = signal.Rsi; // 0..100
            if (value < 30f)
                item1.SubItems.Add(value.ToString("N2")).ForeColor = Color.Red;
            else if (value > 70f)
                item1.SubItems.Add(value.ToString("N2")).ForeColor = Color.Green;
            else
                item1.SubItems.Add(value.ToString("N2"));

            // Oversold/overbougt
            value = signal.StochOscillator;
            if (value < 20f)
                item1.SubItems.Add(value.ToString("N2")).ForeColor = Color.Red;
            else if (value > 80f)
                item1.SubItems.Add(value.ToString("N2")).ForeColor = Color.Green;
            else
                item1.SubItems.Add(value.ToString("N2"));

            // Oversold/overbougt
            value = signal.StochSignal;
            if (value < 20f)
                item1.SubItems.Add(value.ToString("N2")).ForeColor = Color.Red;
            else if (value > 80f)
                item1.SubItems.Add(value.ToString("N2")).ForeColor = Color.Green;
            else
                item1.SubItems.Add(value.ToString("N2"));


            value = signal.Sma200;
            item1.SubItems.Add(value.ToString(signal.Symbol.DisplayFormat));

            value = signal.Sma50;
            if (value < signal.Sma200)
                item1.SubItems.Add(value.ToString(signal.Symbol.DisplayFormat)).ForeColor = Color.Green;
            else if (value > signal.Sma200)
                item1.SubItems.Add(value.ToString(signal.Symbol.DisplayFormat)).ForeColor = Color.Red;
            else
                item1.SubItems.Add(value.ToString(signal.Symbol.DisplayFormat));

            value = signal.Sma20;
            if (value < signal.Sma50)
                item1.SubItems.Add(value.ToString(signal.Symbol.DisplayFormat)).ForeColor = Color.Green;
            else if (value > signal.Sma50)
                item1.SubItems.Add(value.ToString(signal.Symbol.DisplayFormat)).ForeColor = Color.Red;
            else
                item1.SubItems.Add(value.ToString(signal.Symbol.DisplayFormat));

            // de psar zou je wel mogen "clampen"???
            value = signal.PSar; //.Clamp(signal.Symbol.PriceMinimum, signal.Symbol.PriceMaximum, signal.Symbol.PriceTickSize);
            string orgValue = ((decimal?)value)?.ToString(signal.Symbol.DisplayFormat);
            if (value <= signal.Sma20)
                item1.SubItems.Add(orgValue).ForeColor = Color.Green;
            else if (value > signal.Sma20)
                item1.SubItems.Add(orgValue).ForeColor = Color.Red;
            else
                item1.SubItems.Add(orgValue);

#if DEBUG
            string newValue;

            value = signal.PSarDave; //.Clamp(signal.Symbol.PriceMinimum, signal.Symbol.PriceMaximum, signal.Symbol.PriceTickSize);
            newValue = ((decimal?)value)?.ToString(signal.Symbol.DisplayFormat);
            if (orgValue != newValue)
                item1.SubItems.Add(newValue).ForeColor = Color.Red;
            else
                item1.SubItems.Add(newValue);

            value = signal.PSarJason; //.Clamp(signal.Symbol.PriceMinimum, signal.Symbol.PriceMaximum, signal.Symbol.PriceTickSize);
            newValue = ((decimal?)value)?.ToString(signal.Symbol.DisplayFormat);
            if (orgValue != newValue)
                item1.SubItems.Add(newValue).ForeColor = Color.Red;
            else
                item1.SubItems.Add(newValue);

            value = signal.PSarTulip; //.Clamp(signal.Symbol.PriceMinimum, signal.Symbol.PriceMaximum, signal.Symbol.PriceTickSize);
            newValue = ((decimal?)value)?.ToString(signal.Symbol.DisplayFormat);
            if (orgValue != newValue)
                item1.SubItems.Add(newValue).ForeColor = Color.Red;
            else
                item1.SubItems.Add(newValue);
#endif

        }
        return item1;
    }


    private void ListViewSignalsAddSignalRange(List<CryptoSignal> signals)
    {

        listViewSignals.BeginUpdate();
        timerClearEvents.Enabled = false;
        try
        {
            // Wordt nu misschien te vaak gedaan, maar is relatief "goedkoop"
            for (int index = listViewSignals.Items.Count - 1; index >= 0; index--)
            {
                ListViewItem item = listViewSignals.Items[index];
                CryptoSignal signal = (CryptoSignal)item.Tag;
                DateTime expirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.Signal.RemoveSignalAfterxCandles * signal.Interval.Duration); // 15 candles further (display)
                if (expirationDate < DateTime.UtcNow)
                    listViewSignals.Items.RemoveAt(index);
                else break;

            }

            List<ListViewItem> range = new();
            foreach (CryptoSignal signal in signals)
            {
                ListViewItem item = AddSignalItem(signal);
                range.Add(item);
            }

            listViewSignals.Items.AddRange(range.ToArray());

            // Deze redelijk kostbaar? (alles moet gecontroleerd worden)
            for (int i = 0; i <= listViewSignals.Columns.Count - 1; i++)
            {
                listViewSignals.Columns[i].Width = -2;
            }

            //if (signals.Count > 1)
            //    GlobalData.AddTextToLogTab(signals.Count + " signalen als een range toegevoegd");
        }
        finally
        {
            listViewSignals.EndUpdate();
            timerClearEvents.Enabled = true;
        }
    }


    private void TimerClearOldSignals_Tick(object sender, EventArgs e)
    {
        if (listViewSignals.Items.Count > 0)
        {
            bool startUpdating = false;
            try
            {

                for (int index = listViewSignals.Items.Count - 1; index >= 0; index--)
                {
                    ListViewItem item = listViewSignals.Items[index];
                    CryptoSignal signal = (CryptoSignal)item.Tag;
                    DateTime expirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.Signal.RemoveSignalAfterxCandles * signal.Interval.Duration); // 15 candles further (display)
                    if (expirationDate < DateTime.UtcNow)
                    {
                        if (!startUpdating)
                        {
                            listViewSignals.BeginUpdate();
                            startUpdating = true;
                        }
                        listViewSignals.Items.RemoveAt(index);
                    }
                    else break;

                }
            }
            finally
            {
                if (startUpdating)
                    listViewSignals.EndUpdate();
            }
        }
    }


    private void ListViewSignalsMenuItemClearSignals_Click(object sender, EventArgs e)
    {
        listViewSignals.BeginUpdate();
        try
        {
            listViewSignals.Clear();
            ListViewSignalsInitColumns();
        }
        finally
        {
            listViewSignals.EndUpdate();
        }
    }


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

            string href = Intern.TradingView.GetRef(signal.Symbol, signal.Interval);
            Uri uri = new(href);
            webViewTradingView.Source = uri;

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
                string href = Intern.TradingView.GetRef(signal.Symbol, signal.Interval);
                System.Diagnostics.Process.Start(href);

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
        ListViewColumnSorter listViewColumnSorter = (ListViewColumnSorter)listViewSignals.ListViewItemSorter;
        
        // Determine if clicked column is already the column that is being sorted.
        if (e.Column == listViewColumnSorter.SortColumn)
        {
            // Reverse the current sort direction for this column.
            if (listViewColumnSorter.Order == SortOrder.Ascending)
            {
                listViewColumnSorter.Order = SortOrder.Descending;
            }
            else
            {
                listViewColumnSorter.Order = SortOrder.Ascending;
            }
        }
        else
        {
            // Set the column number that is to be sorted; default to ascending.
            listViewColumnSorter.SortColumn = e.Column;
            listViewColumnSorter.Order = SortOrder.Ascending;
        }

        // Perform the sort with these new sort options.
        this.listViewSignals.Sort();
        //this.listViewSignals.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.Order);

    }
}