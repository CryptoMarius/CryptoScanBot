using CryptoScanBot.Commands;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot;

public class CryptoDataGridWhatever<T>() : CryptoDataGrid<T>() where T : CryptoWhatever
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
        AvgBB,
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
        menuStrip.AddCommand(this, "Activate trading app", Command.ActivateTradingApp);
        menuStrip.AddCommand(this, "TradingView internal", Command.ActivateTradingviewIntern);
        menuStrip.AddCommand(this, "TradingView external", Command.ActivateTradingviewExtern);
        //menuStrip.AddCommand(this, "Exchange ", Command.ActivateActiveExchange); // todo direct link?
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
                case ColumnsForGrid.AvgBB:
                    CreateColumn("AvgBB%", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.Rsi:
                    CreateColumn("Rsi", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.MacdValue:
                    CreateColumn("Macd Value", typeof(decimal), string.Empty, DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.MacdSignal:
                    CreateColumn("Macd Signal", typeof(decimal), string.Empty, DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.MacdHistogram:
                    CreateColumn("Macd Histo", typeof(decimal), string.Empty, DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
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
                case ColumnsForGrid.Lux5mLong:
                    CreateColumn("Lux 5m long", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 45).Visible = false;
                    break;
                case ColumnsForGrid.Lux5mShort:
                    CreateColumn("Lux 5m short", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 45).Visible = false;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }


    private int Compare(CryptoWhatever a, CryptoWhatever b)
    {
        try
        {
            CryptoSymbolInterval symbolIntervalA = a.Symbol.GetSymbolInterval(a.Interval.IntervalPeriod);
            CryptoSymbolInterval symbolIntervalB = b.Symbol.GetSymbolInterval(b.Interval.IntervalPeriod);
            if (symbolIntervalA.CandleList.Count == 0 || symbolIntervalB.CandleList.Count == 0)
                return 0;
            CryptoCandle candleA = symbolIntervalA.CandleList.Values.Last();
            CryptoCandle candleB = symbolIntervalB.CandleList.Values.Last();

            int compareResult = (ColumnsForGrid)SortColumn switch
            {
                ColumnsForGrid.Id => ObjectCompare.Compare(a.Symbol.Id, b.Symbol.Id),
                ColumnsForGrid.Date => ObjectCompare.Compare(candleA.Date, candleB.Date),
                ColumnsForGrid.Exchange => ObjectCompare.Compare(a.Symbol.Exchange.Name, b.Symbol.Exchange.Name),
                ColumnsForGrid.Symbol => ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name),
                ColumnsForGrid.Interval => ObjectCompare.Compare(a.Interval.IntervalPeriod, b.Interval.IntervalPeriod),
                ColumnsForGrid.Price => ObjectCompare.Compare(a.Symbol.LastPrice, b.Symbol.LastPrice),
                ColumnsForGrid.Volume => ObjectCompare.Compare(a.Symbol.Volume, b.Symbol.Volume),
                ColumnsForGrid.BB => ObjectCompare.Compare(candleA.CandleData?.BollingerBandsPercentage, candleB.CandleData?.BollingerBandsPercentage),
                //ColumnsForGrid.AvgBB => ObjectCompare.Compare(candleA.CandleData?.AvgBB, candleB.CandleData?.AvgBB),
                ColumnsForGrid.MacdValue => ObjectCompare.Compare(candleA.CandleData?.MacdValue, candleB.CandleData?.MacdValue),
                ColumnsForGrid.MacdSignal => ObjectCompare.Compare(candleA.CandleData?.MacdSignal, candleB.CandleData?.MacdSignal),
                ColumnsForGrid.MacdHistogram => ObjectCompare.Compare(candleA.CandleData?.MacdHistogram, candleB.CandleData?.MacdHistogram),
                ColumnsForGrid.Rsi => ObjectCompare.Compare(candleA.CandleData?.Rsi, candleB.CandleData?.Rsi),
                ColumnsForGrid.Stoch => ObjectCompare.Compare(candleA.CandleData?.StochOscillator, candleB.CandleData?.StochOscillator),
                ColumnsForGrid.Signal => ObjectCompare.Compare(candleA.CandleData?.StochSignal, candleB.CandleData?.StochSignal),
                ColumnsForGrid.Sma200 => ObjectCompare.Compare(candleA.CandleData?.Sma200, candleB.CandleData?.Sma200),
                ColumnsForGrid.Sma50 => ObjectCompare.Compare(candleA.CandleData?.Sma50, candleB.CandleData?.Sma50),
                ColumnsForGrid.Sma20 => ObjectCompare.Compare(candleA.CandleData?.Sma20, candleB.CandleData?.Sma20),
                ColumnsForGrid.PSar => ObjectCompare.Compare(candleA.CandleData?.PSar, candleB.CandleData?.PSar),
                ColumnsForGrid.Lux5mLong => ObjectCompare.Compare(candleA.CandleData?.LuxIndicator5mLong, candleB.CandleData?.LuxIndicator5mLong),
                ColumnsForGrid.Lux5mShort => ObjectCompare.Compare(candleA.CandleData?.LuxIndicator5mShort, candleB.CandleData?.LuxIndicator5mShort),
                _ => 0
            };


            // extend if still the same
            if (compareResult == 0)
            {
                compareResult = ObjectCompare.Compare(candleA.Date, candleB.Date);
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
        List.Sort(Compare);
    }



    public override void GetTextFunction(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        CryptoWhatever? whatever = GetCellObject(e.RowIndex);
        if (whatever != null)
        {
            try
            {
                CryptoSymbolInterval symbolInterval = whatever.Symbol.GetSymbolInterval(whatever.Interval.IntervalPeriod);
                if (symbolInterval.CandleList.Count == 0)
                    return;
                CryptoCandle candle = symbolInterval.CandleList.Values.Last();

                switch ((ColumnsForGrid)e.ColumnIndex)
                {
                    case ColumnsForGrid.Id:
                        e.Value = whatever.Symbol.Id;
                        break;
                    case ColumnsForGrid.Date:
                        DateTime date2 = candle.Date.AddSeconds(whatever.Interval.Duration);
                        e.Value = candle.Date.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + date2.ToLocalTime().ToString("HH:mm");
                        break;
                    case ColumnsForGrid.Exchange:
                        e.Value = whatever.Symbol.Exchange.Name;
                        break;
                    case ColumnsForGrid.Symbol:
                        e.Value = whatever.Symbol.Name;
                        break;
                    case ColumnsForGrid.Interval:
                        e.Value = whatever.Interval.Name;
                        break;
                    case ColumnsForGrid.Price:
                        e.Value = whatever.Symbol.LastPrice?.ToString(whatever.Symbol.PriceDisplayFormat);
                        break;
                    case ColumnsForGrid.Volume:
                        e.Value = whatever.Symbol.Volume;
                        break;
                    case ColumnsForGrid.BB:
                        e.Value = candle.CandleData?.BollingerBandsPercentage;
                        break;
                    //case ColumnsForGrid.AvgBB:
                    //    e.Value = candle.CandleData?.AvgBB;
                    //    break;
                    case ColumnsForGrid.Rsi:
                        e.Value = candle.CandleData?.Rsi;
                        break;
                    case ColumnsForGrid.MacdValue:
                        e.Value = candle.CandleData?.MacdValue.ToString0(whatever.Symbol.PriceDisplayFormat);
                        break;
                    case ColumnsForGrid.MacdSignal:
                        e.Value = candle.CandleData?.MacdSignal.ToString0(whatever.Symbol.PriceDisplayFormat);
                        break;
                    case ColumnsForGrid.MacdHistogram:
                        e.Value = candle.CandleData?.MacdHistogram.ToString0(whatever.Symbol.PriceDisplayFormat);
                        break;
                    case ColumnsForGrid.Stoch:
                        e.Value = candle.CandleData?.StochOscillator;
                        break;
                    case ColumnsForGrid.Signal:
                        e.Value = candle.CandleData?.StochSignal;
                        break;
                    case ColumnsForGrid.Sma200:
                        e.Value = candle.CandleData?.Sma200;
                        break;
                    case ColumnsForGrid.Sma50:
                        e.Value = candle.CandleData?.Sma50;
                        break;
                    case ColumnsForGrid.Sma20:
                        e.Value = candle.CandleData?.Sma20;
                        break;
                    case ColumnsForGrid.PSar:
                        e.Value = candle.CandleData?.PSar;
                        break;
                    case ColumnsForGrid.Lux5mLong:
                        e.Value = candle.CandleData?.LuxIndicator5mLong;
                        break;
                    case ColumnsForGrid.Lux5mShort:
                        e.Value = candle.CandleData?.LuxIndicator5mShort;
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
        CryptoWhatever? whatever = GetCellObject(e.RowIndex);
        if (whatever != null)
        {
            CryptoSymbolInterval symbolInterval = whatever.Symbol.GetSymbolInterval(whatever.Interval.IntervalPeriod);
            if (symbolInterval.CandleList.Count == 0)
                return;
            CryptoCandle candle = symbolInterval.CandleList.Values.Last();

            switch ((ColumnsForGrid)e.ColumnIndex)
            {
                case ColumnsForGrid.Symbol:
                    {
                            Color displayColor = whatever.Symbol.QuoteData!.DisplayColor;
                            if (displayColor != Color.White)
                                backColor = displayColor;
                    }
                    break;

                case ColumnsForGrid.Rsi:
                    {
                        // Oversold/overbougt
                        double? value = candle.CandleData?.Rsi;
                        if (value < GlobalData.Settings.General.RsiValueOversold)
                            foreColor = Color.Red;
                        else if (value > GlobalData.Settings.General.RsiValueOverbought)
                            foreColor = Color.Green;
                    }
                    break;

                //case ColumnsForGrid.AvgBB:
                //    {
                //        // Oversold/overbougt
                //        double? value = candle.CandleData?.AvgBB;
                //        if (value < 1.5)
                //            foreColor = Color.Red;
                //        else if (value > 1.5)
                //            foreColor = Color.Green;
                //    }
                //    break;

                case ColumnsForGrid.Stoch:
                    {
                        // Oversold/overbougt
                        double? value = candle.CandleData?.StochOscillator;
                        if (value < GlobalData.Settings.General.StochValueOversold)
                            foreColor = Color.Red;
                        else if (value > GlobalData.Settings.General.StochValueOverbought)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.Signal:
                    {
                        // Oversold/overbougt
                        double? value = candle.CandleData?.StochSignal;
                        if (value < 20f)
                            foreColor = Color.Red;
                        else if (value > 80f)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.Sma50:
                    {
                        double? value = candle.CandleData?.Sma50;
                        if (value < candle.CandleData?.Sma200)
                            foreColor = Color.Green;
                        else if (value > candle.CandleData?.Sma200)
                            foreColor = Color.Red;
                    }
                    break;

                case ColumnsForGrid.Sma20:
                    {
                        double? value = candle.CandleData?.Sma20;
                        if (value < candle.CandleData?.Sma50)
                            foreColor = Color.Green;
                        else if (value > candle.CandleData?.Sma50)
                            foreColor = Color.Red;
                    }
                    break;

                case ColumnsForGrid.PSar:
                    {
                        double? value = candle.CandleData?.PSar;
                        if (value <= candle.CandleData?.Sma20)
                            foreColor = Color.Green;
                        else if (value > candle.CandleData?.Sma20)
                            foreColor = Color.Red;
                    }
                    break;
            }

            DataGridViewCell cell = Grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            cell.Style.BackColor = backColor;
            cell.Style.ForeColor = foreColor;
        }
    }



    private void RefreshInformation(object? sender, EventArgs? e)
    {
        if (GlobalData.ApplicationIsClosing || !WinFormTools.IsControlVisibleToUser(Grid))
            return;

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