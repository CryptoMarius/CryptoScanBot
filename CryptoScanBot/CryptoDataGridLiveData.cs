using CryptoScanBot.Commands;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot;

public class CryptoDataGridLiveData<T>() : CryptoDataGrid<T>() where T : CryptoLiveData
{
    private enum ColumnsForGrid
    {
        Id,
        Date,
        Exchange,
        Symbol,
        Interval,
        Price,
        Volume,
        BB,
        Rsi,
        MacdValue,
        MacdSignal,
        MacdHistogram,
        Stoch,
        Signal,
        Sma200,
        Sma50,
        Sma20,
        PSar,
        Lux5mLong,
        Lux5mShort,
    }

    private System.Windows.Forms.Timer? TimerRefreshInformation = null;


    public override void InitializeCommands(ContextMenuStrip menuStrip)
    {
        InitializeStandardCommands(menuStrip);

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Copy symbol name", Command.CopySymbolInformation);
        menuStrip.AddCommand(this, "Copy data cells", Command.CopyDataGridCells);
        menuStrip.AddCommand(this, "Show symbol chart", Command.ShowSymbolGraph);
        menuStrip.AddCommand(this, "Calculate liquidity zones", Command.CalculateSymbolLiquidityZones);
        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Export trend information to log", Command.ShowTrendInformation);
        menuStrip.AddCommand(this, "Export signal information to Excel", Command.ExcelSignalInformation);
        menuStrip.AddCommand(this, "Export symbol information to Excel", Command.ExcelSymbolInformation);
        menuStrip.AddCommand(this, "Export all signal information to Excel", Command.ExcelSignalsInformation);

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Hide grid selection", Command.None, ClearSelection);
        //#if DEBUG
        //        menuStrip.AddCommand(this, "Test - Open position", Command.None, CreatePosition);
        //#endif


        TimerRefreshInformation = new()
        {
            Enabled = true,
            Interval = 15 * 1000,
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
                case ColumnsForGrid.Id:
                    CreateColumn("Id", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 50).Visible = false;
                    break;
                case ColumnsForGrid.Date:
                    CreateColumn("Candle date", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 140);
                    break;
                case ColumnsForGrid.Exchange:
                    CreateColumn("Exchange", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 125).Visible = false;
                    break;
                case ColumnsForGrid.Symbol:
                    CreateColumn("Symbol", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 100, true);
                    break;
                case ColumnsForGrid.Interval:
                    CreateColumn("Interval", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 45);
                    break;
                case ColumnsForGrid.Price:
                    CreateColumn("Price", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight, 70);
                    break;
                case ColumnsForGrid.Volume:
                    CreateColumn("Volume", typeof(decimal), "#,##0", DataGridViewContentAlignment.MiddleRight, 80);
                    break;
                case ColumnsForGrid.BB:
                    CreateColumn("BB%", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                //case ColumnsForGrid.AvgBB:
                //    CreateColumn("AvgBB%", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                //    break;
                case ColumnsForGrid.Rsi:
                    CreateColumn("Rsi", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.MacdValue:
                    CreateColumn("Macd Value", typeof(decimal), string.Empty, DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.MacdSignal:
                    CreateColumn("Macd Signal", typeof(decimal), string.Empty, DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.MacdHistogram:
                    CreateColumn("Macd Histo", typeof(decimal), string.Empty, DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.Stoch:
                    CreateColumn("Stoch", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.Signal:
                    CreateColumn("Signal", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.Sma200:
                    CreateColumn("Sma200", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.Sma50:
                    CreateColumn("Sma50", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.Sma20:
                    CreateColumn("Sma20", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.PSar:
                    CreateColumn("PSar", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.Lux5mLong:
                    CreateColumn("Lux 5m long", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 45);
                    break;
                case ColumnsForGrid.Lux5mShort:
                    CreateColumn("Lux 5m short", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 45);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }


    private int Compare(CryptoLiveData a, CryptoLiveData b)
    {
        try
        {
            int compareResult = (ColumnsForGrid)SortColumn switch
            {
                ColumnsForGrid.Id => ObjectCompare.Compare(a.Symbol.Id, b.Symbol.Id),
                ColumnsForGrid.Date => ObjectCompare.Compare(a.Candle.Date, b.Candle.Date),
                ColumnsForGrid.Exchange => ObjectCompare.Compare(a.Symbol.Exchange.Name, b.Symbol.Exchange.Name),
                ColumnsForGrid.Symbol => ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name),
                ColumnsForGrid.Interval => ObjectCompare.Compare(a.Interval.IntervalPeriod, b.Interval.IntervalPeriod),
                ColumnsForGrid.Price => ObjectCompare.Compare(a.Symbol.LastPrice, b.Symbol.LastPrice),
                ColumnsForGrid.Volume => ObjectCompare.Compare(a.Symbol.Volume, b.Symbol.Volume),
                ColumnsForGrid.BB => ObjectCompare.Compare(a.Candle.CandleData?.BollingerBandsPercentage, b.Candle.CandleData?.BollingerBandsPercentage),
                //ColumnsForGrid.AvgBB => ObjectCompare.Compare(a.Candle.CandleData?.AvgBB, b.Candle.CandleData?.AvgBB),
                ColumnsForGrid.MacdValue => ObjectCompare.Compare(a.Candle.CandleData?.MacdValue, b.Candle.CandleData?.MacdValue),
                ColumnsForGrid.MacdSignal => ObjectCompare.Compare(a.Candle.CandleData?.MacdSignal, b.Candle.CandleData?.MacdSignal),
                ColumnsForGrid.MacdHistogram => ObjectCompare.Compare(a.Candle.CandleData?.MacdHistogram, b.Candle.CandleData?.MacdHistogram),
                ColumnsForGrid.Rsi => ObjectCompare.Compare(a.Candle.CandleData?.Rsi, b.Candle.CandleData?.Rsi),
                ColumnsForGrid.Stoch => ObjectCompare.Compare(a.Candle.CandleData?.StochOscillator, b.Candle.CandleData?.StochOscillator),
                ColumnsForGrid.Signal => ObjectCompare.Compare(a.Candle.CandleData?.StochSignal, b.Candle.CandleData?.StochSignal),
                ColumnsForGrid.Sma200 => ObjectCompare.Compare(a.Candle.CandleData?.Sma200, b.Candle.CandleData?.Sma200),
                ColumnsForGrid.Sma50 => ObjectCompare.Compare(a.Candle.CandleData?.Sma50, b.Candle.CandleData?.Sma50),
                ColumnsForGrid.Sma20 => ObjectCompare.Compare(a.Candle.CandleData?.Sma20, b.Candle.CandleData?.Sma20),
                ColumnsForGrid.PSar => ObjectCompare.Compare(a.Candle.CandleData?.PSar, b.Candle.CandleData?.PSar),
                ColumnsForGrid.Lux5mLong => ObjectCompare.Compare(a.Candle.CandleData?.LuxIndicator5mLong, b.Candle.CandleData?.LuxIndicator5mLong),
                ColumnsForGrid.Lux5mShort => ObjectCompare.Compare(a.Candle.CandleData?.LuxIndicator5mShort, b.Candle.CandleData?.LuxIndicator5mShort),
                _ => 0
            };


            // extend if still the same
            if (compareResult == 0)
            {
                compareResult = ObjectCompare.Compare(a.Candle.Date, b.Candle.Date);
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
                        compareResult = ObjectCompare.Compare(a.Interval!.IntervalPeriod, b.Interval!.IntervalPeriod);
                    else
                        compareResult = ObjectCompare.Compare(b.Interval!.IntervalPeriod, a.Interval!.IntervalPeriod);
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
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR " + error.ToString());
            return 0;
        }
    }


    public override void SortFunction()
    {
        Monitor.Enter(List);
        try
        {
            List.Sort(Compare);
        }
        finally
        {
            Monitor.Exit(List);
        }
    }


    public override void GetTextFunction(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        CryptoLiveData? liveData = GetCellObject(e.RowIndex);
        if (liveData != null)
        {
            try
            {
                switch ((ColumnsForGrid)e.ColumnIndex)
                {
                    case ColumnsForGrid.Id:
                        e.Value = liveData.Symbol.Id;
                        break;
                    case ColumnsForGrid.Date:
                        DateTime date2 = liveData.Candle.Date.AddSeconds(liveData.Interval.Duration);
                        e.Value = liveData.Candle.Date.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + date2.ToLocalTime().ToString("HH:mm");
                        break;
                    case ColumnsForGrid.Exchange:
                        e.Value = liveData.Symbol.Exchange.Name;
                        break;
                    case ColumnsForGrid.Symbol:
                        e.Value = liveData.Symbol.Name;
                        break;
                    case ColumnsForGrid.Interval:
                        e.Value = liveData.Interval.Name;
                        break;
                    case ColumnsForGrid.Price:
                        e.Value = liveData.Symbol.LastPrice?.ToString(liveData.Symbol.PriceDisplayFormat);
                        break;
                    case ColumnsForGrid.Volume:
                        e.Value = liveData.Symbol.Volume;
                        break;
                    case ColumnsForGrid.BB:
                        e.Value = liveData.Candle.CandleData?.BollingerBandsPercentage;
                        break;
                    //case ColumnsForGrid.AvgBB:
                    //    e.Value = liveData.Candle.CandleData?.AvgBB;
                    //    break;
                    case ColumnsForGrid.Rsi:
                        e.Value = liveData.Candle.CandleData?.Rsi;
                        break;
                    case ColumnsForGrid.MacdValue:
                        e.Value = liveData.Candle.CandleData?.MacdValue.ToString0(liveData.Symbol.PriceDisplayFormat);
                        break;
                    case ColumnsForGrid.MacdSignal:
                        e.Value = liveData.Candle.CandleData?.MacdSignal.ToString0(liveData.Symbol.PriceDisplayFormat);
                        break;
                    case ColumnsForGrid.MacdHistogram:
                        e.Value = liveData.Candle.CandleData?.MacdHistogram.ToString0(liveData.Symbol.PriceDisplayFormat);
                        break;
                    case ColumnsForGrid.Stoch:
                        e.Value = liveData.Candle.CandleData?.StochOscillator;
                        break;
                    case ColumnsForGrid.Signal:
                        e.Value = liveData.Candle.CandleData?.StochSignal;
                        break;
                    case ColumnsForGrid.Sma200:
                        e.Value = liveData.Candle.CandleData?.Sma200;
                        break;
                    case ColumnsForGrid.Sma50:
                        e.Value = liveData.Candle.CandleData?.Sma50;
                        break;
                    case ColumnsForGrid.Sma20:
                        e.Value = liveData.Candle.CandleData?.Sma20;
                        break;
                    case ColumnsForGrid.PSar:
                        e.Value = liveData.Candle.CandleData?.PSar;
                        break;
                    case ColumnsForGrid.Lux5mLong:
                        e.Value = liveData.Candle.CandleData?.LuxIndicator5mLong;
                        break;
                    case ColumnsForGrid.Lux5mShort:
                        e.Value = liveData.Candle.CandleData?.LuxIndicator5mShort;
                        break;
                    default:
                        e.Value = '?';
                        break;
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab("ERROR " + error.ToString());
            }
        }
    }


    public override void CellFormattingEvent(object? sender, DataGridViewCellFormattingEventArgs e)
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
        CryptoLiveData? liveData = GetCellObject(e.RowIndex);
        if (liveData != null)
        {

            switch ((ColumnsForGrid)e.ColumnIndex)
            {
                case ColumnsForGrid.Symbol:
                    {
                            Color displayColor = liveData.Symbol.QuoteData!.DisplayColor;
                            if (displayColor != Color.White)
                                backColor = displayColor;
                    }
                    break;

                case ColumnsForGrid.Rsi:
                    {
                        // Oversold/overbougt
                        double? value = liveData.Candle.CandleData?.Rsi;
                        if (value < GlobalData.Settings.General.RsiValueOversold)
                            foreColor = Color.Red;
                        else if (value > GlobalData.Settings.General.RsiValueOverbought)
                            foreColor = Color.Green;
                    }
                    break;

                //case ColumnsForGrid.AvgBB:
                //    {
                //        // Oversold/overbougt
                //        double? value = liveData.Candle.CandleData?.AvgBB;
                //        if (value < 1.5)
                //            foreColor = Color.Red;
                //        else if (value > 1.5)
                //            foreColor = Color.Green;
                //    }
                //    break;

                case ColumnsForGrid.Stoch:
                    {
                        // Oversold/overbougt
                        double? value = liveData.Candle.CandleData?.StochOscillator;
                        if (value < GlobalData.Settings.General.StochValueOversold)
                            foreColor = Color.Red;
                        else if (value > GlobalData.Settings.General.StochValueOverbought)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.Signal:
                    {
                        // Oversold/overbougt
                        double? value = liveData.Candle.CandleData?.StochSignal;
                        if (value < 20f)
                            foreColor = Color.Red;
                        else if (value > 80f)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.Sma50:
                    {
                        double? value = liveData.Candle.CandleData?.Sma50;
                        if (value < liveData.Candle.CandleData?.Sma200)
                            foreColor = Color.Green;
                        else if (value > liveData.Candle.CandleData?.Sma200)
                            foreColor = Color.Red;
                    }
                    break;

                case ColumnsForGrid.Sma20:
                    {
                        double? value = liveData.Candle.CandleData?.Sma20;
                        if (value < liveData.Candle.CandleData?.Sma50)
                            foreColor = Color.Green;
                        else if (value > liveData.Candle.CandleData?.Sma50)
                            foreColor = Color.Red;
                    }
                    break;

                case ColumnsForGrid.PSar:
                    {
                        double? value = liveData.Candle.CandleData?.PSar;
                        if (value <= liveData.Candle.CandleData?.Sma20)
                            foreColor = Color.Green;
                        else if (value > liveData.Candle.CandleData?.Sma20)
                            foreColor = Color.Red;
                    }
                    break;
            }

            DataGridViewCell cell = Grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            cell.Style.BackColor = backColor;
            cell.Style.ForeColor = foreColor;
        }
    }


    static long LastCheck = 0;

    private void RemoveOldObjects()
    {
        // Avoid frequent updates
        long x = CandleTools.GetUnixTime(DateTime.UtcNow, 15 * 60);
        if (x == LastCheck)
            return;
 
        if (Monitor.TryEnter(List))
        {            
            LastCheck = x;
            try
            {
                if (List.Count > 0)
                {
                    for (int index = List.Count - 1; index >= 0; index--)
                    {
                        CryptoLiveData liveData = List[index];
                        if (liveData.Candle.CandleData == null)
                        {
                            List.Remove((T)liveData);
                            if (GlobalData.LiveDataQueueAdded.ContainsKey((liveData.Symbol.Name, liveData.Interval.IntervalPeriod)))
                                GlobalData.LiveDataQueueAdded.Remove((liveData.Symbol.Name, liveData.Interval.IntervalPeriod));
                        }
                    }
                }
            }
            finally
            {
                Monitor.Exit(List);
            }
        }
    }


    private void RefreshInformation(object? sender, EventArgs? e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        RemoveOldObjects();

        if (WinFormTools.IsControlVisibleToUser(Grid))
        {
            try
            {
                Grid.SuspendDrawing();
                try
                {
                    Grid.InvalidateColumn((int)ColumnsForGrid.Price);
                }
                finally
                {
                    Grid.ResumeDrawing();
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"Error RefreshInformation {error}");
            }
        }
    }


}