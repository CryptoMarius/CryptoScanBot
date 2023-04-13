using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;
using System.Collections;

namespace CryptoSbmScanner;

/// <summary>
/// This class is an implementation of the 'IComparer' interface.
/// </summary>
public class ListViewColumnSorter : IComparer
{
    public int SortColumn { set; get; } = 0;
    public SortOrder SortOrder = SortOrder.Descending;

    private readonly CaseInsensitiveComparer ObjectCompare;

    /// <summary>
    /// Class constructor. Initializes various elements
    /// </summary>
    public ListViewColumnSorter()
    {
        // Initialize the CaseInsensitiveComparer object
        ObjectCompare = new CaseInsensitiveComparer();
    }

    /// <summary>
    /// This method is inherited from the IComparer interface. It compares the two objects passed using a case insensitive comparison.
    /// </summary>
    /// <param name="x">First object to be compared</param>
    /// <param name="y">Second object to be compared</param>
    /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
    public int Compare(object x, object y)
    {
        // uit de c# documentatie

        // Cast the objects to be compared to ListViewItem objects
        ListViewItem itemA = (ListViewItem)x;
        CryptoSignal signalA = (CryptoSignal)itemA.Tag;

        ListViewItem itemB = (ListViewItem)y;
        CryptoSignal signalB = (CryptoSignal)itemB.Tag;

        int compareResult = SortColumn switch
        {
            0 => ObjectCompare.Compare(signalA.CloseDate, signalB.CloseDate),
            1 => ObjectCompare.Compare(signalA.Symbol.Name, signalB.Symbol.Name),
            2 => ObjectCompare.Compare(signalA.Interval.IntervalPeriod, signalB.Interval.IntervalPeriod),
            3 => ObjectCompare.Compare(signalA.ModeText, signalB.ModeText),
            4 => ObjectCompare.Compare(signalA.StrategyText, signalB.StrategyText),
            5 => ObjectCompare.Compare(signalA.EventText, signalB.EventText),
            6 => ObjectCompare.Compare(signalA.Price, signalB.Price),
            7 => ObjectCompare.Compare(signalA.Volume, signalB.Volume),
            8 => ObjectCompare.Compare(signalA.TrendIndicator, signalB.TrendIndicator),
            9 => ObjectCompare.Compare(signalA.TrendPercentage, signalB.TrendPercentage),
            10 => ObjectCompare.Compare(signalA.Last24HoursChange, signalB.Last24HoursChange),
            11 => ObjectCompare.Compare(signalA.Last24HoursEffective, signalB.Last24HoursEffective),
            12 => ObjectCompare.Compare(signalA.BollingerBandsPercentage, signalB.BollingerBandsPercentage),
            13 => ObjectCompare.Compare(signalA.Rsi, signalB.Rsi),
            14 => ObjectCompare.Compare(signalA.StochOscillator, signalB.StochOscillator),
            15 => ObjectCompare.Compare(signalA.StochSignal, signalB.StochSignal),
            16 => ObjectCompare.Compare(signalA.Sma200, signalB.Sma200),
            17 => ObjectCompare.Compare(signalA.Sma50, signalB.Sma50),
            18 => ObjectCompare.Compare(signalA.Sma20, signalB.Sma20),
            19 => ObjectCompare.Compare(signalA.PSar, signalB.PSar),
            20 => ObjectCompare.Compare(signalA.FluxIndicator5m, signalB.FluxIndicator5m),
            _ => 0
        };

        // Binnen dezelfde records toch een extra onderverdeling maken, anders is het nog steeds "random"
        if (compareResult == 0)
        {
            compareResult = ObjectCompare.Compare(signalA.CloseDate, signalB.CloseDate);
            if (compareResult == 0)
            {
                if (this.SortOrder == SortOrder.Ascending)
                    compareResult = ObjectCompare.Compare(signalA.Symbol.Name, signalB.Symbol.Name);
                else
                    compareResult = ObjectCompare.Compare(signalB.Symbol.Name, signalA.Symbol.Name);
            }
            if (compareResult == 0)
            {
                if (this.SortOrder == SortOrder.Ascending)
                    compareResult = ObjectCompare.Compare(signalA.Interval.IntervalPeriod, signalB.Interval.IntervalPeriod);
                else
                    compareResult = ObjectCompare.Compare(signalB.Interval.IntervalPeriod, signalA.Interval.IntervalPeriod);
            }
        }


        // Calculate correct return value based on object comparison
        if (SortOrder == SortOrder.Ascending)
            return compareResult;
        else if (SortOrder == SortOrder.Descending)
            return (-compareResult);
        else
            return 0;
    }

    public void ClickedOnColumn(int column)
    {
        // Determine if clicked column is already the column that is being sorted.
        if (column == SortColumn)
        {
            // Reverse the current sort direction for this column.
            if (SortOrder == SortOrder.Ascending)
                SortOrder = SortOrder.Descending;
            else
                SortOrder = SortOrder.Ascending;
        }
        else
        {
            // Set the column number that is to be sorted; default to ascending.
            SortColumn = column;
            SortOrder = SortOrder.Ascending;
        }
    }

}

public partial class FrmMain
{
    private void ListViewSignalsInitOnce()
    {
        ListViewColumnSorter listViewColumnSorter = new()
        {
            SortOrder = SortOrder.Descending
        };

        listViewSignals.GridLines = true;
        listViewSignals.View = View.Details;
        listViewSignals.FullRowSelect = true;
        listViewSignals.HideSelection = true;

        listViewSignals.ListViewItemSorter = listViewColumnSorter;
        listViewSignals.ColumnClick += ListViewSignals_ColumnClick;
        listViewSignals.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.SortOrder);
    }

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
        // Create columns and subitems. Width of -2 indicates auto-size
        listViewSignals.Columns.Add("Candle datum", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Symbol", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Interval", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Mode", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Strategie", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Text", -2, HorizontalAlignment.Left);
        listViewSignals.Columns.Add("Price", -2, HorizontalAlignment.Right);
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
            listViewSignals.Columns[i].Width = -2;
        }
    }


    private static ListViewItem AddSignalItem(CryptoSignal signal)
    {
        string s = signal.OpenDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + signal.OpenDate.AddSeconds(signal.Interval.Duration).ToLocalTime().ToString("HH:mm");
        ListViewItem item1 = new(s, -1)
        {
            UseItemStyleForSubItems = false
        };

        ListViewItem.ListViewSubItem subItem;

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
        if (signal.Mode != SignalMode.modeInfo2)
        {
            switch (signal.Strategy)
            {
                case SignalStrategy.candlesJumpUp:
                case SignalStrategy.candlesJumpDown:
                    if (GlobalData.Settings.Signal.ColorJump != Color.White)
                        subItem.BackColor = GlobalData.Settings.Signal.ColorJump;
                    break;

                case SignalStrategy.stobbOverbought:
                case SignalStrategy.stobbOversold:
                    if (GlobalData.Settings.Signal.ColorStobb != Color.White)
                        subItem.BackColor = GlobalData.Settings.Signal.ColorStobb;
                    break;

                case SignalStrategy.sbm1Overbought:
                case SignalStrategy.sbm1Oversold:
                case SignalStrategy.sbm2Overbought:
                case SignalStrategy.sbm2Oversold:
                case SignalStrategy.sbm3Overbought:
                case SignalStrategy.sbm3Oversold:
                case SignalStrategy.sbm4Overbought:
                case SignalStrategy.sbm4Oversold:
                case SignalStrategy.sbm5Overbought:
                case SignalStrategy.sbm5Oversold:
                    if (GlobalData.Settings.Signal.ColorSbm != Color.White)
                        subItem.BackColor = GlobalData.Settings.Signal.ColorSbm;
                    break;

                case SignalStrategy.priceCrossedEma20:
                case SignalStrategy.priceCrossedEma50:
                case SignalStrategy.priceCrossedSma20:
                case SignalStrategy.priceCrossedSma50:
                    //if (GlobalData.Settings.Signal.ColorJump != Color.White)
                    //    subItem.BackColor = GlobalData.Settings.Signal.ColorJump;
                    break;
            }
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
        if (signal.Last24HoursEffective < 0)
            item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Red;
        else
            item1.SubItems.Add(value?.ToString("N2")).ForeColor = Color.Green;

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
            item1.SubItems.Add(value?.ToString(signal.Symbol.DisplayFormat));

            value = signal.Sma50;
            if (value < signal.Sma200)
                item1.SubItems.Add(value?.ToString(signal.Symbol.DisplayFormat)).ForeColor = Color.Green;
            else if (value > signal.Sma200)
                item1.SubItems.Add(value?.ToString(signal.Symbol.DisplayFormat)).ForeColor = Color.Red;
            else
                item1.SubItems.Add(value?.ToString(signal.Symbol.DisplayFormat));

            value = signal.Sma20;
            if (value < signal.Sma50)
                item1.SubItems.Add(value?.ToString(signal.Symbol.DisplayFormat)).ForeColor = Color.Green;
            else if (value > signal.Sma50)
                item1.SubItems.Add(value?.ToString(signal.Symbol.DisplayFormat)).ForeColor = Color.Red;
            else
                item1.SubItems.Add(value?.ToString(signal.Symbol.DisplayFormat));

            // de psar zou je wel mogen "clampen"???
            value = signal.PSar; //.Clamp(signal.Symbol.PriceMinimum, signal.Symbol.PriceMaximum, signal.Symbol.PriceTickSize);
            string orgValue = value?.ToString(signal.Symbol.DisplayFormat);
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
                DateTime expirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.General.RemoveSignalAfterxCandles * signal.Interval.Duration); // 15 candles further (display)
                if (expirationDate < DateTime.UtcNow)
                    listViewSignals.Items.RemoveAt(index);
                else break;

            }

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
                    DateTime expirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.General.RemoveSignalAfterxCandles * signal.Interval.Duration); // 15 candles further (display)
                    if (expirationDate < DateTime.UtcNow)
                    {
                        if (!startUpdating)
                        {
                            listViewSignals.BeginUpdate();
                            startUpdating = true;
                        }
                        listViewSignals.Items.RemoveAt(index);
                    }
                    //else break; // Mhhh, werkt niet als de sortering anders is!

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
        // Perform the sort with these new sort options.
        ListViewColumnSorter listViewColumnSorter = (ListViewColumnSorter)listViewSignals.ListViewItemSorter;
        listViewColumnSorter.ClickedOnColumn(e.Column);
        listViewSignals.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.SortOrder);
        listViewSignals.Sort();
    }
}
