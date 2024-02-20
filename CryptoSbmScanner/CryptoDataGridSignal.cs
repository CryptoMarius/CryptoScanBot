using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner;

public class CryptoDataGridSignal<T>(DataGridView grid, List<T> list) : CryptoDataGrid<T>(grid, list) where T : CryptoSignal
{

    private System.Windows.Forms.Timer TimerClearEvents;

    private void InitializeTimers()
    {
        TimerClearEvents = new()
        {
            Enabled = true,
            Interval = 1 * 60 * 1000,
        };
        TimerClearEvents.Tick += ClearOldSignals;

    }

    public override void InitializeCommands(ContextMenuStrip menuStrip)
    {
        AddStandardSymbolCommands(menuStrip, true);
    }


    public override void InitializeHeaders()
    {
        SortColumn = 2;
        SortOrder = SortOrder.Ascending;

        // wellicht een idee om de sort van de column toe te voegen en dan wordt de compare makkelijk (maar hoe?)

        CreateColumn("Candle datum", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Exchange", typeof(string), "", DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Symbol", typeof(string), "", DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Interval", typeof(string), "", DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Mode", typeof(string), "", DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Strategie", typeof(string), "", DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Text", typeof(string), "", DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Price", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Stijging", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Volume", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("TF-Trend", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Markttrend%", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("24h Change", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("24h Beweging", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("BB%", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("RSI", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Stoch", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Signal", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Sma200", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Sma50", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Sma20", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("PSar", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Flux 5m", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("FundingRate", typeof(decimal), "##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("15m", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("30m", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("1h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("4h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("12h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);

        // Make the font italic for row four.
        //Grid.Columns[4].DefaultCellStyle.Font = new Font(DataGridView.DefaultFont, FontStyle.Italic);
    }


    private int Compare(CryptoSignal a, CryptoSignal b)
    {
        int compareResult = SortColumn switch
        {
            00 => ObjectCompare.Compare(a.CloseDate, b.CloseDate),
            01 => ObjectCompare.Compare(a.Exchange.Name, b.Exchange.Name),
            02 => ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name),
            03 => ObjectCompare.Compare(a.Interval.IntervalPeriod, b.Interval.IntervalPeriod),
            04 => ObjectCompare.Compare(a.SideText, b.SideText),
            05 => ObjectCompare.Compare(a.StrategyText, b.StrategyText),
            06 => ObjectCompare.Compare(a.EventText, b.EventText),
            07 => ObjectCompare.Compare(a.Price, b.Price),
            08 => ObjectCompare.Compare(a.PriceDiff, b.PriceDiff),
            09 => ObjectCompare.Compare(a.Volume, b.Volume),
            10 => ObjectCompare.Compare(a.TrendIndicator, b.TrendIndicator),
            11 => ObjectCompare.Compare(a.TrendPercentage, b.TrendPercentage),
            12 => ObjectCompare.Compare(a.Last24HoursChange, b.Last24HoursChange),
            13 => ObjectCompare.Compare(a.Last24HoursEffective, b.Last24HoursEffective),
            14 => ObjectCompare.Compare(a.BollingerBandsPercentage, b.BollingerBandsPercentage),
            15 => ObjectCompare.Compare(a.Rsi, b.Rsi),
            16 => ObjectCompare.Compare(a.StochOscillator, b.StochOscillator),
            17 => ObjectCompare.Compare(a.StochSignal, b.StochSignal),
            18 => ObjectCompare.Compare(a.Sma200, b.Sma200),
            19 => ObjectCompare.Compare(a.Sma50, b.Sma50),
            20 => ObjectCompare.Compare(a.Sma20, b.Sma20),
            21 => ObjectCompare.Compare(a.PSar, b.PSar),
            22 => ObjectCompare.Compare(a.FluxIndicator5m, b.FluxIndicator5m),
            23 => ObjectCompare.Compare(a.Symbol.FundingRate, b.Symbol.FundingRate),
            24 => ObjectCompare.Compare(a.Trend15m, b.Trend15m),
            25 => ObjectCompare.Compare(a.Trend30m, b.Trend30m),
            26 => ObjectCompare.Compare(a.Trend1h, b.Trend1h),
            27 => ObjectCompare.Compare(a.Trend4h, b.Trend4h),
            28 => ObjectCompare.Compare(a.Trend12h, b.Trend12h),
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


    private static string TrendIndicatorText(CryptoTrendIndicator trend)
    {
        return trend switch
        {
            CryptoTrendIndicator.Bullish => "Up",
            CryptoTrendIndicator.Bearish => "Down",
            _ => "",
        };
    }

    public override void GetTextFunction(object sender, DataGridViewCellValueEventArgs e)
    {
        CryptoSignal signal = List[e.RowIndex];
        e.Value = e.ColumnIndex switch
        {
            0 => signal.OpenDate,
            1 => signal.Exchange.Name,
            2 => signal.Symbol.Name,
            3 => signal.Interval.Name,
            4 => signal.Side,
            5 => signal.StrategyText,
            6 => signal.EventText,
            7 => signal.Price.ToString(signal.Symbol.PriceDisplayFormat),
            8 => signal.PriceDiff,
            9 => signal.Volume,
            10 => signal.TrendIndicator,
            11 => signal.TrendPercentage,
            12 => signal.Last24HoursChange,
            13 => signal.Last24HoursEffective,
            14 => signal.BollingerBandsPercentage,
            15 => signal.Rsi,
            16 => signal.StochOscillator,
            17 => signal.StochSignal,
            18 => signal.Sma200,
            19 => signal.Sma50,
            20 => signal.Sma20,
            21 => signal.PSar,
            22 => signal.FluxIndicator5m,
            23 => signal.Symbol.FundingRate,
            24 => TrendIndicatorText(signal.Trend15m.Value),
            25 => TrendIndicatorText(signal.Trend30m.Value),
            26 => TrendIndicatorText(signal.Trend1h.Value),
            27 => TrendIndicatorText(signal.Trend4h.Value),
            28 => TrendIndicatorText(signal.Trend12h.Value),
            _ => '?',
        };
    }

    public override void RowSetDefaultColor(object sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex % 2 == 0)
        {
            Grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = VeryLightGray;
        }
    }


    private Color TrendIndicatorColor(CryptoTrendIndicator trend)
    {
        return trend switch
        {
            CryptoTrendIndicator.Bullish => Color.Green,
            CryptoTrendIndicator.Bearish => Color.Red,
            _ => Grid.DefaultCellStyle.BackColor,
        };
    }

    public override void CellFormattingEvent(object sender, DataGridViewCellFormattingEventArgs e)
    {
        // done by column so it happens once per row

        Color? backColor = null;
        Color? foreColor = null;
        CryptoSignal signal = List[e.RowIndex];

        switch (e.ColumnIndex)
        {
            case 2:
                {
                    Color displayColor = signal.Symbol.QuoteData.DisplayColor;
                    if (displayColor != Color.White)
                        backColor = displayColor;
                }
                break;
            case 4:
                {
                    if (signal.Side == CryptoTradeSide.Long)
                        foreColor = Color.Green;
                    else
                        foreColor = Color.Red;
                }
                break;
            case 5:
                {
                    // todo - configuratie per strategie introduceren?

                    if (!signal.IsInvalid)
                    {
                        switch (signal.Strategy)
                        {
                            case CryptoSignalStrategy.Jump:
                                if (GlobalData.Settings.Signal.Jump.ColorLong != Color.White && signal.Side == CryptoTradeSide.Long)
                                    backColor = GlobalData.Settings.Signal.Jump.ColorLong;
                                else if (GlobalData.Settings.Signal.Jump.ColorShort != Color.White && signal.Side == CryptoTradeSide.Short)
                                    backColor = GlobalData.Settings.Signal.Jump.ColorShort;
                                break;

                            case CryptoSignalStrategy.Stobb:
                                if (GlobalData.Settings.Signal.Stobb.ColorLong != Color.White && signal.Side == CryptoTradeSide.Long)
                                    backColor = GlobalData.Settings.Signal.Stobb.ColorLong;
                                else if (GlobalData.Settings.Signal.Stobb.ColorShort != Color.White && signal.Side == CryptoTradeSide.Short)
                                    backColor = GlobalData.Settings.Signal.Stobb.ColorShort;
                                break;

                            case CryptoSignalStrategy.Sbm1:
                            case CryptoSignalStrategy.Sbm2:
                            case CryptoSignalStrategy.Sbm3:
                            case CryptoSignalStrategy.Sbm4:
                                if (GlobalData.Settings.Signal.Sbm.ColorLong != Color.White && signal.Side == CryptoTradeSide.Long)
                                    backColor = GlobalData.Settings.Signal.Sbm.ColorLong;
                                else if (GlobalData.Settings.Signal.Sbm.ColorShort != Color.White && signal.Side == CryptoTradeSide.Short)
                                    backColor = GlobalData.Settings.Signal.Sbm.ColorShort;
                                break;
                        }
                    }
                }
                break;
            case 7:
                {
                    if (signal.Symbol.LastPrice > signal.Price)
                        foreColor = Color.Green;
                    else if (signal.Symbol.LastPrice < signal.Price)
                        foreColor = Color.Red;
                }
                break;
            case 8:
                {
                    double x = signal.PriceDiff.Value;
                    if (x > 0)
                        foreColor = Color.Green;
                    else if (x < 0)
                        foreColor = Color.Red;
                }
                break;

            case 11:
                {
                    double value = signal.Last24HoursChange;
                    if (value < 0)
                        foreColor = Color.Red;
                    else
                        foreColor = Color.Green;
                }
                break;

            case 12:
                {
                    double value = signal.TrendPercentage;
                    if (value < 0)
                        foreColor = Color.Red;
                    else
                        foreColor = Color.Green;
                }
                break;

            case 15:
                {
                    // Oversold/overbougt
                    double? value = signal.Rsi; // 0..100
                    if (value < 30f)
                        foreColor = Color.Red;
                    else if (value > 70f)
                        foreColor = Color.Green;
                }
                break;

            case 16:
                {
                    // Oversold/overbougt
                    double? value = signal.StochOscillator;
                    if (value < 20f)
                        foreColor = Color.Red;
                    else if (value > 80f)
                        foreColor = Color.Green;
                }
                break;

            case 17:
                {
                    // Oversold/overbougt
                    double? value = signal.StochSignal;
                    if (value < 20f)
                        foreColor = Color.Red;
                    else if (value > 80f)
                        foreColor = Color.Green;
                }
                break;

            case 19:
                {
                    double? value = signal.Sma50;
                    if (value < signal.Sma200)
                        foreColor = Color.Green;
                    else if (value > signal.Sma200)
                        foreColor = Color.Red;
                }
                break;

            case 20:
                {
                    double? value = signal.Sma20;
                    if (value < signal.Sma50)
                        foreColor = Color.Green;
                    else if (value > signal.Sma50)
                        foreColor = Color.Red;
                }
                break;

            case 21:
                {
                    double? value = signal.PSar;
                    if (value <= signal.Sma20)
                        foreColor = Color.Green;
                    else if (value > signal.Sma20)
                        foreColor = Color.Red;
                }
                break;

            case 23:
                {
                    if (signal.Symbol.FundingRate > 0)
                        foreColor = Color.Green;
                    else if (signal.Symbol.FundingRate < 0)
                        foreColor = Color.Red;
                }
                break;
            case 24:
                foreColor = TrendIndicatorColor(signal.Trend15m.Value);
                break;
            case 25:
                foreColor = TrendIndicatorColor(signal.Trend30m.Value);
                break;
            case 26:
                foreColor = TrendIndicatorColor(signal.Trend1h.Value);
                break;
            case 27:
                foreColor = TrendIndicatorColor(signal.Trend4h.Value);
                break;
            case 28:
                foreColor = TrendIndicatorColor(signal.Trend12h.Value);
                break;
        }

        if (backColor.HasValue)
            Grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = backColor.Value;
        if (foreColor.HasValue)
            Grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = foreColor.Value;
    }


    public void UpdatePriceDifferences()
    {
        foreach (var x in List)
        {
            x.Price += 0.1m;
        }
        Grid.InvalidateColumn(7);
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
                    bool startedUpdating = false;
                    for (int index = List.Count - 1; index >= 0; index--)
                    {
                        CryptoSignal signal = List[index];

                        DateTime expirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.General.RemoveSignalAfterxCandles * signal.Interval.Duration);
                        if (expirationDate < DateTime.UtcNow)
                        {
                            List.RemoveAt(index);
                            startedUpdating = true;
                        }

                    }
                    if (startedUpdating)
                        AdjustObjectCount();
                }
            }
            finally
            {
                Monitor.Exit(List);
            }
        }
    }

}
