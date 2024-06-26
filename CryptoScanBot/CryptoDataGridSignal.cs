﻿using CryptoScanBot.Commands;
using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using CryptoScanBot.Settings;

namespace CryptoScanBot;

public class CryptoDataGridSignal<T>(DataGridView grid, List<T> list, SortedList<string, ColumnSetting> columnList) : 
    CryptoDataGrid<T>(grid, list, columnList) where T : CryptoSignal
{
    private enum ColumnsForGrid
    {
        //Id,
        Date,
        Exchange,
        Symbol,
        Interval,
        Mode,
        Strategy,
        Text,
        Price,
        PriceChange,
        Volume,
        TfTrend,
        MarketTrend,
        Change24h,
        Move24h,
        BB,
        RSI,
        Stoch,
        Signal,
        Sma200,
        Sma50,
        Sma20,
        PSar,
        Flux5m,
        FundingRate,
        Trend15m,
        Trend30m,
        Trend1h,
        Trend4h,
        Trend12h,
#if TRADEBOT
        MinimumEntry,
#endif
    }

    private System.Windows.Forms.Timer TimerClearOldSignals;
    private System.Windows.Forms.Timer TimerRefreshInformation;


    public override void InitializeCommands(ContextMenuStrip menuStrip)
    {
        menuStrip.AddCommand(this, "Activate trading app", Command.ActivateTradingApp);
        menuStrip.AddCommand(this, "TradingView internal", Command.ActivateTradingviewIntern);
        menuStrip.AddCommand(this, "TradingView external", Command.ActivateTradingviewExtern);
        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Copy symbol name", Command.CopySymbolInformation);
        menuStrip.AddCommand(this, "Trend information (log)", Command.ShowTrendInformation);
        menuStrip.AddCommand(this, "Signal information (Excel)", Command.ExcelSignalInformation);
        menuStrip.AddCommand(this, "Symbol information (Excel)", Command.ExcelSymbolInformation);

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Hide grid selection", Command.None, ClearSelection);

        TimerClearOldSignals = new()
        {
            Enabled = true,
            Interval = 15 * 1000
        };
        TimerClearOldSignals.Tick += ClearOldSignals;

        TimerRefreshInformation = new()
        {
            Enabled = true,
            Interval = 120 * 1000
        };
        TimerRefreshInformation.Tick += RefreshInformation;
    }


    public override void InitializeHeaders()
    {
        SortOrder = SortOrder.Descending;
        SortColumn = (int)ColumnsForGrid.Date;

        var columns = Enum.GetValues(typeof(ColumnsForGrid));
        foreach (ColumnsForGrid column in columns)
        {
            switch (column)
            {
                //CreateColumn("Id", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 50);
                case ColumnsForGrid.Date:
                    CreateColumn("Candle date", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 140);
                    break;
                case ColumnsForGrid.Exchange:
                    CreateColumn("Exchange", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 125).Visible = false;
                    break;
                case ColumnsForGrid.Symbol:
                    CreateColumn("Symbol", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 100);
                    break;
                case ColumnsForGrid.Interval:
                    CreateColumn("Interval", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 45);
                    break;
                case ColumnsForGrid.Mode:
                    CreateColumn("Side", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 40);
                    break;
                case ColumnsForGrid.Strategy:
                    CreateColumn("Strategy", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 70);
                    break;
                case ColumnsForGrid.Text:
                    CreateColumn("Text", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 50).Visible = false;
                    break;
                case ColumnsForGrid.Price:
                    CreateColumn("Price", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight, 70);
                    break;
                case ColumnsForGrid.PriceChange:
                    CreateColumn("Change", typeof(decimal), "#,##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.Volume:
                    CreateColumn("Volume", typeof(decimal), "#,##0", DataGridViewContentAlignment.MiddleRight, 80);
                    break;
                case ColumnsForGrid.TfTrend:
                    CreateColumn("TF trend", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 50);
                    break;
                case ColumnsForGrid.MarketTrend:
                    CreateColumn("Market trend%", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.Change24h:
                    CreateColumn("24h Change", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.Move24h:
                    CreateColumn("24h Move", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.BB:
                    CreateColumn("BB%", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.RSI:
                    CreateColumn("RSI", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.Stoch:
                    CreateColumn("Stoch", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.Signal:
                    CreateColumn("Signal", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.Sma200:
                    CreateColumn("Sma200", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.Sma50:
                    CreateColumn("Sma50", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.Sma20:
                    CreateColumn("Sma20", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.PSar:
                    CreateColumn("PSar", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.Flux5m:
                    CreateColumn("Flux 5m", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 45).Visible = false;
                    break;
                case ColumnsForGrid.FundingRate:
                    //DataGridViewTextBoxColumn c = 
                    CreateColumn("Funding Rate", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    //if (GlobalData.Settings.General.ActivateExchange. Futures ...  disable the column...
                    break;
                case ColumnsForGrid.Trend15m:
                    CreateColumn("15m", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
                    break;
                case ColumnsForGrid.Trend30m:
                    CreateColumn("30m", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
                    break;
                case ColumnsForGrid.Trend1h:
                    CreateColumn("1h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
                    break;
                case ColumnsForGrid.Trend4h:
                    CreateColumn("4h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
                    break;
                case ColumnsForGrid.Trend12h:
                    CreateColumn("12h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
                    break;
#if TRADEBOT
                case ColumnsForGrid.MinimumEntry:
                    CreateColumn("M.Entry", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
                    break;
#endif

            }
        }
        
    }

    private int Compare(CryptoSignal a, CryptoSignal b)
    {
        int compareResult = (ColumnsForGrid)SortColumn switch
        {
            ColumnsForGrid.Date => ObjectCompare.Compare(a.CloseDate, b.CloseDate),
            ColumnsForGrid.Exchange => ObjectCompare.Compare(a.Exchange.Name, b.Exchange.Name),
            ColumnsForGrid.Symbol => ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name),
            ColumnsForGrid.Interval => ObjectCompare.Compare(a.Interval.IntervalPeriod, b.Interval.IntervalPeriod),
            ColumnsForGrid.Mode => ObjectCompare.Compare(a.SideText, b.SideText),
            ColumnsForGrid.Strategy => ObjectCompare.Compare(a.StrategyText, b.StrategyText),
            ColumnsForGrid.Text => ObjectCompare.Compare(a.EventText, b.EventText),
            ColumnsForGrid.Price => ObjectCompare.Compare(a.Price, b.Price),
            ColumnsForGrid.PriceChange => ObjectCompare.Compare(a.PriceDiff, b.PriceDiff),
            ColumnsForGrid.Volume => ObjectCompare.Compare(a.Volume, b.Volume),
            ColumnsForGrid.TfTrend => ObjectCompare.Compare(a.TrendIndicator, b.TrendIndicator),
            ColumnsForGrid.MarketTrend => ObjectCompare.Compare(a.TrendPercentage, b.TrendPercentage),
            ColumnsForGrid.Change24h => ObjectCompare.Compare(a.Last24HoursChange, b.Last24HoursChange),
            ColumnsForGrid.Move24h => ObjectCompare.Compare(a.Last24HoursEffective, b.Last24HoursEffective),
            ColumnsForGrid.BB => ObjectCompare.Compare(a.BollingerBandsPercentage, b.BollingerBandsPercentage),
            ColumnsForGrid.RSI => ObjectCompare.Compare(a.Rsi, b.Rsi),
            ColumnsForGrid.Stoch => ObjectCompare.Compare(a.StochOscillator, b.StochOscillator),
            ColumnsForGrid.Signal => ObjectCompare.Compare(a.StochSignal, b.StochSignal),
            ColumnsForGrid.Sma200 => ObjectCompare.Compare(a.Sma200, b.Sma200),
            ColumnsForGrid.Sma50 => ObjectCompare.Compare(a.Sma50, b.Sma50),
            ColumnsForGrid.Sma20 => ObjectCompare.Compare(a.Sma20, b.Sma20),
            ColumnsForGrid.PSar => ObjectCompare.Compare(a.PSar, b.PSar),
            ColumnsForGrid.Flux5m => ObjectCompare.Compare(a.FluxIndicator5m, b.FluxIndicator5m),
            ColumnsForGrid.FundingRate => ObjectCompare.Compare(a.Symbol.FundingRate, b.Symbol.FundingRate),
            ColumnsForGrid.Trend15m => ObjectCompare.Compare(a.Trend15m, b.Trend15m),
            ColumnsForGrid.Trend30m => ObjectCompare.Compare(a.Trend30m, b.Trend30m),
            ColumnsForGrid.Trend1h => ObjectCompare.Compare(a.Trend1h, b.Trend1h),
            ColumnsForGrid.Trend4h => ObjectCompare.Compare(a.Trend4h, b.Trend4h),
            ColumnsForGrid.Trend12h => ObjectCompare.Compare(a.Trend12h, b.Trend12h),
#if TRADEBOT
            ColumnsForGrid.MinimumEntry => ObjectCompare.Compare(a.MinEntry, b.MinEntry),
#endif
            _ => 0
        };


        // extend if still the same
        if (compareResult == 0)
        {
            compareResult = ObjectCompare.Compare(a.CloseDate, b.CloseDate);
            if (compareResult == 0)
            {
                if (SortOrder == SortOrder.Ascending)
                    compareResult = ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name);
                else
                    compareResult = ObjectCompare.Compare(b.Symbol.Name, a.Symbol.Name);
            }
            if (compareResult == 0)
            {
                if (SortOrder == SortOrder.Ascending)
                    compareResult = ObjectCompare.Compare(a.Interval.IntervalPeriod, b.Interval.IntervalPeriod);
                else
                    compareResult = ObjectCompare.Compare(b.Interval.IntervalPeriod, a.Interval.IntervalPeriod);
            }
        }


        // Calculate correct return value based on object comparison
        if (SortOrder == SortOrder.Ascending)
            return +compareResult;
        else if (SortOrder == SortOrder.Descending)
            return -compareResult;
        else
            return 0;
    }


    public override void SortFunction()
    {
        List.Sort(Compare);
    }


    private static string TrendIndicatorText(CryptoTrendIndicator? trend)
    {
        if (!trend.HasValue)
            return "";

        return trend switch
        {
            CryptoTrendIndicator.Bullish => "up",
            CryptoTrendIndicator.Bearish => "down",
            _ => "",
        };
    }

    private static string SymbolName(CryptoSignal signal)
    {
        string s = signal.Symbol.Base + "/" + @signal.Symbol.Quote;
        decimal tickPercentage = 100 * signal.Symbol.PriceTickSize / signal.Price;
        if (tickPercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
            s += " " + tickPercentage.ToString("N2");
        return s;
    }

    public override void GetTextFunction(object sender, DataGridViewCellValueEventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        CryptoSignal signal = GetCellObject(e.RowIndex);
        if (signal != null)
        {
            switch ((ColumnsForGrid)e.ColumnIndex)
            {
                case ColumnsForGrid.Date:
                    e.Value = signal.OpenDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + signal.OpenDate.AddSeconds(signal.Interval.Duration).ToLocalTime().ToString("HH:mm");
                    break;
                case ColumnsForGrid.Exchange:
                    e.Value = signal.Exchange.Name;
                    break;
                case ColumnsForGrid.Symbol:
                    e.Value = SymbolName(signal);
                    break;
                case ColumnsForGrid.Interval:
                    e.Value = signal.Interval.Name;
                    break;
                case ColumnsForGrid.Mode:
                    e.Value = signal.SideText;
                    break;
                case ColumnsForGrid.Strategy:
                    e.Value = signal.StrategyText;
                    break;
                case ColumnsForGrid.Text:
                    e.Value = signal.EventText;
                    break;
                case ColumnsForGrid.Price:
                    e.Value = signal.Price.ToString(signal.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.PriceChange:
                    e.Value = signal.PriceDiff;
                    break;
                case ColumnsForGrid.Volume:
                    e.Value = signal.Volume;
                    break;
                case ColumnsForGrid.TfTrend:
                    e.Value = TrendIndicatorText(signal.TrendIndicator);
                    break;
                case ColumnsForGrid.MarketTrend:
                    e.Value = signal.TrendPercentage;
                    break;
                case ColumnsForGrid.Change24h:
                    e.Value = signal.Last24HoursChange;
                    break;
                case ColumnsForGrid.Move24h:
                    e.Value = signal.Last24HoursEffective;
                    break;
                case ColumnsForGrid.BB:
                    e.Value = signal.BollingerBandsPercentage;
                    break;
                case ColumnsForGrid.RSI:
                    e.Value = signal.Rsi;
                    break;
                case ColumnsForGrid.Stoch:
                    e.Value = signal.StochOscillator;
                    break;
                case ColumnsForGrid.Signal:
                    e.Value = signal.StochSignal;
                    break;
                case ColumnsForGrid.Sma200:
                    e.Value = signal.Sma200;
                    break;
                case ColumnsForGrid.Sma50:
                    e.Value = signal.Sma50;
                    break;
                case ColumnsForGrid.Sma20:
                    e.Value = signal.Sma20;
                    break;
                case ColumnsForGrid.PSar:
                    e.Value = signal.PSar;
                    break;
                case ColumnsForGrid.Flux5m:
                    e.Value = signal.FluxIndicator5m;
                    break;
                case ColumnsForGrid.FundingRate: // Only relevant for Bybit Futures..
                    if (signal.Symbol.FundingRate != 0.0m)
                        e.Value = signal.Symbol.FundingRate;
                    break;
                case ColumnsForGrid.Trend15m:
                    e.Value = TrendIndicatorText(signal.Trend15m);
                    break;
                case ColumnsForGrid.Trend30m:
                    e.Value = TrendIndicatorText(signal.Trend30m);
                    break;
                case ColumnsForGrid.Trend1h:
                    e.Value = TrendIndicatorText(signal.Trend1h);
                    break;
                case ColumnsForGrid.Trend4h:
                    e.Value = TrendIndicatorText(signal.Trend4h);
                    break;
                case ColumnsForGrid.Trend12h:
                    e.Value = TrendIndicatorText(signal.Trend12h);
                    break;
#if TRADEBOT
                case ColumnsForGrid.MinimumEntry:
                    e.Value = signal.MinEntry.ToString(signal.Symbol.QuoteData.DisplayFormat);
                    break;
#endif
                default:
                    e.Value = '?';
                    break;
            }
        }
    }



    private Color TrendIndicatorColor(CryptoTrendIndicator? trend)
    {
        if (!trend.HasValue)
            return Grid.DefaultCellStyle.BackColor;

        return trend switch
        {
            CryptoTrendIndicator.Bullish => Color.Green,
            CryptoTrendIndicator.Bearish => Color.Red,
            _ => Grid.DefaultCellStyle.BackColor,
        };
    }


    public override void CellFormattingEvent(object sender, DataGridViewCellFormattingEventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        // Standard background for the cell (with alternating line color)
        Color backColor;
        if (e.RowIndex % 2 == 0)
        {
            if (GlobalData.Settings.General.BlackTheming)
                backColor = VeryLightGray2;
            else
                backColor = VeryLightGray1;
        }
        else
            backColor = Grid.DefaultCellStyle.BackColor;

        Color foreColor = Color.Black;
        CryptoSignal signal = GetCellObject(e.RowIndex);
        if (signal != null)
        {

            switch ((ColumnsForGrid)e.ColumnIndex)
            {
                case ColumnsForGrid.Symbol:
                    {
                        if (!signal.IsInvalid)
                        {
                            Color displayColor = signal.Symbol.QuoteData.DisplayColor;
                            if (displayColor != Color.White)
                                backColor = displayColor;
                        }
                        //else
                        //  foreColor = Color.LightGray;
                    }
                    break;

                case ColumnsForGrid.Mode:
                    {
                        if (!signal.IsInvalid)
                        {
                            if (signal.Side == CryptoTradeSide.Long)
                                foreColor = Color.Green;
                            else
                                foreColor = Color.Red;
                        }
                        else
                            foreColor = Color.LightGray;
                    }
                    break;

                case ColumnsForGrid.Strategy:
                    {
                        if (!signal.IsInvalid)
                        {
                            Color color = GetBackgroudColorForStrategy(signal.Strategy, signal.Side);
                            if (color != Color.White)
                                backColor = color;
                        }
                    }
                    break;
                case ColumnsForGrid.Price:
                    {
                        if (signal.Symbol.LastPrice > signal.Price)
                            foreColor = Color.Green;
                        else if (signal.Symbol.LastPrice < signal.Price)
                            foreColor = Color.Red;
                    }
                    break;
                case ColumnsForGrid.PriceChange:
                    {
                        double x = signal.PriceDiff.Value;
                        if (x > 0)
                            foreColor = Color.Green;
                        else if (x < 0)
                            foreColor = Color.Red;
                    }
                    break;

                case ColumnsForGrid.TfTrend:
                    foreColor = TrendIndicatorColor(signal.TrendIndicator);
                    break;

                case ColumnsForGrid.MarketTrend:
                    {
                        double value = signal.TrendPercentage;
                        if (value < 0)
                            foreColor = Color.Red;
                        else
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.Change24h:
                    {
                        double value = signal.Last24HoursChange;
                        if (value < 0)
                            foreColor = Color.Red;
                        else
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.RSI:
                    {
                        // Oversold/overbougt
                        double? value = signal.Rsi; // 0..100
                        if (value < 30f)
                            foreColor = Color.Red;
                        else if (value > 70f)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.Stoch:
                    {
                        // Oversold/overbougt
                        double? value = signal.StochOscillator;
                        if (value < 20f)
                            foreColor = Color.Red;
                        else if (value > 80f)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.Signal:
                    {
                        // Oversold/overbougt
                        double? value = signal.StochSignal;
                        if (value < 20f)
                            foreColor = Color.Red;
                        else if (value > 80f)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.Sma50:
                    {
                        double? value = signal.Sma50;
                        if (value < signal.Sma200)
                            foreColor = Color.Green;
                        else if (value > signal.Sma200)
                            foreColor = Color.Red;
                    }
                    break;

                case ColumnsForGrid.Sma20:
                    {
                        double? value = signal.Sma20;
                        if (value < signal.Sma50)
                            foreColor = Color.Green;
                        else if (value > signal.Sma50)
                            foreColor = Color.Red;
                    }
                    break;

                case ColumnsForGrid.PSar:
                    {
                        double? value = signal.PSar;
                        if (value <= signal.Sma20)
                            foreColor = Color.Green;
                        else if (value > signal.Sma20)
                            foreColor = Color.Red;
                    }
                    break;

                case ColumnsForGrid.FundingRate:
                    {
                        if (signal.Symbol.FundingRate > 0)
                            foreColor = Color.Green;
                        else if (signal.Symbol.FundingRate < 0)
                            foreColor = Color.Red;
                    }
                    break;
                case ColumnsForGrid.Trend15m:
                    foreColor = TrendIndicatorColor(signal.Trend15m);
                    break;
                case ColumnsForGrid.Trend30m:
                    foreColor = TrendIndicatorColor(signal.Trend30m);
                    break;
                case ColumnsForGrid.Trend1h:
                    foreColor = TrendIndicatorColor(signal.Trend1h);
                    break;
                case ColumnsForGrid.Trend4h:
                    foreColor = TrendIndicatorColor(signal.Trend4h);
                    break;
                case ColumnsForGrid.Trend12h:
                    foreColor = TrendIndicatorColor(signal.Trend12h);
                    break;
            }

            DataGridViewCell cell = Grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            cell.Style.BackColor = backColor;
            cell.Style.ForeColor = foreColor;
        }
    }


    private void ClearOldSignals(object sender, EventArgs e)
    {
        // Avoid duplicate calls (when the list is serious long)
        if (Monitor.TryEnter(List))
        {
            try
            {
                // Circa 1x per minuut de verouderde signalen opruimen
                if (List.Count > 0)
                {
                    bool removedObject = false;
                    for (int index = List.Count - 1; index >= 0; index--)
                    {
                        CryptoSignal signal = List[index];

                        DateTime expirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.General.RemoveSignalAfterxCandles * signal.Interval.Duration);
                        if (expirationDate < DateTime.UtcNow)
                        {
                            List.RemoveAt(index);
                            removedObject = true;
                        }

                    }
                    if (removedObject)
                        AdjustObjectCount();
                }
            }
            finally
            {
                Monitor.Exit(List);
            }
        }
    }

    private void RefreshInformation(object sender, EventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        Grid.SuspendDrawing();
        try
        {
            Grid.InvalidateColumn((int)ColumnsForGrid.Price);
            Grid.InvalidateColumn((int)ColumnsForGrid.PriceChange);
        }
        finally
        {
            Grid.ResumeDrawing();
        }
    }

}
