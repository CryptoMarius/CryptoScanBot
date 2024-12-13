using CryptoScanBot.Commands;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot;

public class CryptoDataGridSignal<T>() : CryptoDataGrid<T>() where T : CryptoSignal
{
    private enum ColumnsForGrid
    {
        Id,
        Date,
        Exchange,
        Symbol,
        Interval,
        Side,
        Strategy,
        Text,
        SignalPrice,
        PriceChange,
        SignalVolume,
        TfTrend,
        MarketTrend,
        Change24h,
        Move24h,
        BB,
        AvgBB,
        Rsi,
        SlopeRsi,
        MacdValue,
        MacdSignal,
        MacdHistogram,
        Stoch,
        Signal,
        Sma200,
        Sma50,
        Sma20,
        PSar,
        Lux5m,
        FundingRate,

        Trend15m,
        Trend30m,
        Trend1h,
        Trend4h,
        Trend1d,

        Barometer15m,
        Barometer30m,
        Barometer1h,
        Barometer4h,
        Barometer1d,

        MinimumEntry,
        // statistics
        PriceMin,
        PriceMax,
        PriceMinPerc,
        PriceMaxPerc,
    }

    private System.Windows.Forms.Timer? TimerClearOldSignals = null;
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


        TimerClearOldSignals = new()
        {
            Enabled = true,
            Interval = 1 * 60 * 1000,
        };
        TimerClearOldSignals.Tick += ClearOldSignals;

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
                case ColumnsForGrid.Side:
                    CreateColumn("Side", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 40);
                    break;
                case ColumnsForGrid.Strategy:
                    CreateColumn("Strategy", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 70);
                    break;
                case ColumnsForGrid.Text:
                    CreateColumn("Text", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 50).Visible = false;
                    break;
                case ColumnsForGrid.SignalPrice:
                    CreateColumn("Price", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight, 70);
                    break;
                case ColumnsForGrid.PriceChange:
                    CreateColumn("Change", typeof(decimal), "#,##0.#0", DataGridViewContentAlignment.MiddleRight, 50);
                    break;
                case ColumnsForGrid.SignalVolume:
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
                case ColumnsForGrid.SlopeRsi:
                    CreateColumn("Slope RSI", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
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
                case ColumnsForGrid.Lux5m:
                    CreateColumn("Lux 5m", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 45).Visible = false;
                    break;
                case ColumnsForGrid.FundingRate:
                    //DataGridViewTextBoxColumn c = 
                    CreateColumn("Funding Rate", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    //if (GlobalData.Settings.General.ActivateExchange. Futures ...  disable the column...
                    break;
                case ColumnsForGrid.Trend15m:
                    CreateColumn("Trend 15m", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
                    break;
                case ColumnsForGrid.Trend30m:
                    CreateColumn("Trend 30m", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
                    break;
                case ColumnsForGrid.Trend1h:
                    CreateColumn("Trend 1h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
                    break;
                case ColumnsForGrid.Trend4h:
                    CreateColumn("Trend 4h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
                    break;
                case ColumnsForGrid.Trend1d:
                    CreateColumn("Trend 1d", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
                    break;

                case ColumnsForGrid.Barometer15m:
                    CreateColumn("Bm 15m", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.Barometer30m:
                    CreateColumn("Bm 30m", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.Barometer1h:
                    CreateColumn("Bm 1h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.Barometer4h:
                    CreateColumn("Bm 4h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.Barometer1d:
                    CreateColumn("Bm 1d", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;

                case ColumnsForGrid.MinimumEntry:
                    CreateColumn("M.Entry", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.PriceMin:
                    CreateColumn("MinPrice", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight, 70).Visible = false;
                    break;
                case ColumnsForGrid.PriceMax:
                    CreateColumn("MaxPrice", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight, 70).Visible = false;
                    break;
                case ColumnsForGrid.PriceMinPerc:
                    CreateColumn("MinPerc", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight, 70).Visible = false;
                    break;
                case ColumnsForGrid.PriceMaxPerc:
                    CreateColumn("MaxPerc", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight, 70).Visible = false;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }


    private int Compare(CryptoSignal a, CryptoSignal b)
    {
        int compareResult = (ColumnsForGrid)SortColumn switch
        {
            ColumnsForGrid.Id => ObjectCompare.Compare(a.Id, b.Id),
            ColumnsForGrid.Date => ObjectCompare.Compare(a.CloseDate, b.CloseDate),
            ColumnsForGrid.Exchange => ObjectCompare.Compare(a.Exchange.Name, b.Exchange.Name),
            ColumnsForGrid.Symbol => ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name),
            ColumnsForGrid.Interval => ObjectCompare.Compare(a.Interval.IntervalPeriod, b.Interval.IntervalPeriod),
            ColumnsForGrid.Side => ObjectCompare.Compare(a.SideText, b.SideText),
            ColumnsForGrid.Strategy => ObjectCompare.Compare(a.StrategyText, b.StrategyText),
            ColumnsForGrid.Text => ObjectCompare.Compare(a.EventText, b.EventText),
            ColumnsForGrid.SignalPrice => ObjectCompare.Compare(a.SignalPrice, b.SignalPrice),
            ColumnsForGrid.PriceChange => ObjectCompare.Compare(a.PriceDiff, b.PriceDiff),
            ColumnsForGrid.SignalVolume => ObjectCompare.Compare(a.SignalVolume, b.SignalVolume),
            ColumnsForGrid.TfTrend => ObjectCompare.Compare(a.TrendIndicator, b.TrendIndicator),
            ColumnsForGrid.MarketTrend => ObjectCompare.Compare(a.TrendPercentage, b.TrendPercentage),
            ColumnsForGrid.Change24h => ObjectCompare.Compare(a.Last24HoursChange, b.Last24HoursChange),
            ColumnsForGrid.Move24h => ObjectCompare.Compare(a.Last24HoursEffective, b.Last24HoursEffective),
            ColumnsForGrid.BB => ObjectCompare.Compare(a.BollingerBandsPercentage, b.BollingerBandsPercentage),
            ColumnsForGrid.AvgBB => ObjectCompare.Compare(a.AvgBB, b.AvgBB),
            ColumnsForGrid.MacdValue => ObjectCompare.Compare(a.MacdValue, b.MacdValue),
            ColumnsForGrid.MacdSignal => ObjectCompare.Compare(a.MacdSignal, b.MacdSignal),
            ColumnsForGrid.MacdHistogram => ObjectCompare.Compare(a.MacdHistogram, b.MacdHistogram),
            ColumnsForGrid.Rsi => ObjectCompare.Compare(a.Rsi, b.Rsi),
            ColumnsForGrid.SlopeRsi => ObjectCompare.Compare(a.SlopeRsi, b.SlopeRsi),
            ColumnsForGrid.Stoch => ObjectCompare.Compare(a.StochOscillator, b.StochOscillator),
            ColumnsForGrid.Signal => ObjectCompare.Compare(a.StochSignal, b.StochSignal),
            ColumnsForGrid.Sma200 => ObjectCompare.Compare(a.Sma200, b.Sma200),
            ColumnsForGrid.Sma50 => ObjectCompare.Compare(a.Sma50, b.Sma50),
            ColumnsForGrid.Sma20 => ObjectCompare.Compare(a.Sma20, b.Sma20),
            ColumnsForGrid.PSar => ObjectCompare.Compare(a.PSar, b.PSar),
            ColumnsForGrid.Lux5m => ObjectCompare.Compare(a.LuxIndicator5m, b.LuxIndicator5m),
            ColumnsForGrid.FundingRate => ObjectCompare.Compare(a.Symbol.FundingRate, b.Symbol.FundingRate),
            ColumnsForGrid.Trend15m => ObjectCompare.Compare(a.Trend15m, b.Trend15m),
            ColumnsForGrid.Trend30m => ObjectCompare.Compare(a.Trend30m, b.Trend30m),
            ColumnsForGrid.Trend1h => ObjectCompare.Compare(a.Trend1h, b.Trend1h),
            ColumnsForGrid.Trend4h => ObjectCompare.Compare(a.Trend4h, b.Trend4h),
            ColumnsForGrid.Trend1d => ObjectCompare.Compare(a.Trend1d, b.Trend1d),
            ColumnsForGrid.Barometer15m => ObjectCompare.Compare(a.Barometer15m, b.Barometer15m),
            ColumnsForGrid.Barometer30m => ObjectCompare.Compare(a.Barometer30m, b.Barometer30m),
            ColumnsForGrid.Barometer1h => ObjectCompare.Compare(a.Barometer1h, b.Barometer1h),
            ColumnsForGrid.Barometer4h => ObjectCompare.Compare(a.Barometer4h, b.Barometer4h),
            ColumnsForGrid.Barometer1d => ObjectCompare.Compare(a.Barometer1d, b.Barometer1d),
            ColumnsForGrid.MinimumEntry => ObjectCompare.Compare(a.MinEntry, b.MinEntry),
            ColumnsForGrid.PriceMin => ObjectCompare.Compare(a.PriceMin, b.PriceMin),
            ColumnsForGrid.PriceMax => ObjectCompare.Compare(a.PriceMax, b.PriceMax),
            ColumnsForGrid.PriceMinPerc => ObjectCompare.Compare(a.PriceMinPerc, b.PriceMinPerc),
            ColumnsForGrid.PriceMaxPerc => ObjectCompare.Compare(a.PriceMaxPerc, b.PriceMaxPerc),
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


    public override void SortFunction()
    {
        List.Sort(Compare);
    }



    private static string SymbolName(CryptoSignal signal)
    {
        string s = signal.Symbol.Base + "/" + @signal.Symbol.Quote;
        decimal tickPercentage = 100 * signal.Symbol.PriceTickSize / signal.SignalPrice;
        if (tickPercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
            s += " " + tickPercentage.ToString("N2");
        return s;
    }

    public override void GetTextFunction(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        CryptoSignal? signal = GetCellObject(e.RowIndex);
        if (signal != null)
        {
            switch ((ColumnsForGrid)e.ColumnIndex)
            {
                case ColumnsForGrid.Id:
                    e.Value = signal.Id;
                    break;
                case ColumnsForGrid.Date:
                    // there is a signal.CloseTime
                    //+ signal.OpenDate.AddSeconds(signal.Interval.Duration).ToLocalTime().ToString("HH:mm");
                    e.Value = signal.OpenDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + signal.CloseDate.ToLocalTime().ToString("HH:mm");
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
                case ColumnsForGrid.Side:
                    e.Value = signal.SideText;
                    break;
                case ColumnsForGrid.Strategy:
                    e.Value = signal.StrategyText;
                    break;
                case ColumnsForGrid.Text:
                    e.Value = signal.EventText;
                    break;
                case ColumnsForGrid.SignalPrice:
                    e.Value = signal.SignalPrice.ToString(signal.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.PriceChange:
                    e.Value = signal.PriceDiff;
                    break;
                case ColumnsForGrid.SignalVolume:
                    e.Value = signal.SignalVolume;
                    break;
                case ColumnsForGrid.TfTrend:
                    e.Value = TrendTools.TrendIndicatorText(signal.TrendIndicator);
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
                case ColumnsForGrid.AvgBB:
                    e.Value = signal.AvgBB;
                    break;
                case ColumnsForGrid.Rsi:
                    e.Value = signal.Rsi;
                    break;
                case ColumnsForGrid.MacdValue:
                    e.Value = signal.MacdValue.ToString0(signal.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.MacdSignal:
                    e.Value = signal.MacdSignal.ToString0(signal.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.MacdHistogram:
                    e.Value = signal.MacdHistogram.ToString0(signal.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.SlopeRsi:
                    e.Value = signal.SlopeRsi;
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
                case ColumnsForGrid.Lux5m:
                    e.Value = signal.LuxIndicator5m;
                    break;
                case ColumnsForGrid.FundingRate: // Only relevant for Bybit Futures..
                    if (signal.Symbol.FundingRate != 0.0m)
                        e.Value = signal.Symbol.FundingRate;
                    break;
                case ColumnsForGrid.Trend15m:
                    e.Value = TrendTools.TrendIndicatorText(signal.Trend15m);
                    break;
                case ColumnsForGrid.Trend30m:
                    e.Value = TrendTools.TrendIndicatorText(signal.Trend30m);
                    break;
                case ColumnsForGrid.Trend1h:
                    e.Value = TrendTools.TrendIndicatorText(signal.Trend1h);
                    break;
                case ColumnsForGrid.Trend4h:
                    e.Value = TrendTools.TrendIndicatorText(signal.Trend4h);
                    break;
                case ColumnsForGrid.Trend1d:
                    e.Value = TrendTools.TrendIndicatorText(signal.Trend1d);
                    break;
                case ColumnsForGrid.Barometer15m:
                    e.Value = signal.Barometer15m?.ToString("N2");
                    break;
                case ColumnsForGrid.Barometer30m:
                    e.Value = signal.Barometer30m?.ToString("N2");
                    break;
                case ColumnsForGrid.Barometer1h:
                    e.Value = signal.Barometer1h?.ToString("N2");
                    break;
                case ColumnsForGrid.Barometer4h:
                    e.Value = signal.Barometer4h?.ToString("N2");
                    break;
                case ColumnsForGrid.Barometer1d:
                    e.Value = signal.Barometer1d?.ToString("N2");
                    break;
                case ColumnsForGrid.MinimumEntry:
                    e.Value = signal.MinEntry.ToString(signal.Symbol.QuoteData!.DisplayFormat);
                    break;
                case ColumnsForGrid.PriceMin:
                    if (signal.PriceMin! != 0m)
                        e.Value = signal.PriceMin.ToString(signal.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.PriceMax:
                    if (signal.PriceMax! != 0m)
                        e.Value = signal.PriceMax.ToString(signal.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.PriceMinPerc:
                    if (signal.PriceMinPerc! != 0)
                        e.Value = signal.PriceMinPerc.ToString("N2");
                    break;
                case ColumnsForGrid.PriceMaxPerc:
                    if (signal.PriceMaxPerc! != 0)
                        e.Value = signal.PriceMaxPerc.ToString("N2");
                    break;
                default:
                    e.Value = '?';
                    break;
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
        CryptoSignal? signal = GetCellObject(e.RowIndex);
        if (signal != null)
        {

            switch ((ColumnsForGrid)e.ColumnIndex)
            {
                case ColumnsForGrid.Symbol:
                    {
                        if (!signal.IsInvalid)
                        {
                            Color displayColor = signal.Symbol.QuoteData!.DisplayColor;
                            if (displayColor != Color.White)
                                backColor = displayColor;
                        }
                        //else
                        //  foreColor = Color.LightGray;
                    }
                    break;

                case ColumnsForGrid.Side:
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
                case ColumnsForGrid.SignalPrice:
                    {
                        if (signal.Symbol.LastPrice > signal.SignalPrice)
                            foreColor = Color.Green;
                        else if (signal.Symbol.LastPrice < signal.SignalPrice)
                            foreColor = Color.Red;
                    }
                    break;
                case ColumnsForGrid.PriceChange:
                    {
                        if (signal.PriceDiff.HasValue)
                        {
                            double x = signal.PriceDiff!.Value;
                            if (signal.Side == CryptoTradeSide.Long)
                            {
                                if (x > 0)
                                    foreColor = Color.Green;
                                else if (x < 0)
                                    foreColor = Color.Red;
                            }
                            else
                            {
                                if (x < 0)
                                    foreColor = Color.Green;
                                else if (x > 0)
                                    foreColor = Color.Red;
                            }
                        }
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

                case ColumnsForGrid.Rsi:
                    {
                        // Oversold/overbougt
                        double? value = signal.Rsi;
                        if (value < GlobalData.Settings.General.RsiValueOversold)
                            foreColor = Color.Red;
                        else if (value > GlobalData.Settings.General.RsiValueOverbought)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.AvgBB:
                    {
                        // Oversold/overbougt
                        double? value = signal.AvgBB;
                        if (value < 1.5)
                            foreColor = Color.Red;
                        else if (value > 1.5)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.SlopeRsi:
                    {
                        double? value = signal.SlopeRsi;
                        if (value < 0)
                            foreColor = Color.Red;
                        else if (value > 0)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.Stoch:
                    {
                        // Oversold/overbougt
                        double? value = signal.StochOscillator;
                        if (value < GlobalData.Settings.General.StochValueOversold)
                            foreColor = Color.Red;
                        else if (value > GlobalData.Settings.General.StochValueOverbought)
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
                case ColumnsForGrid.Trend1d:
                    foreColor = TrendIndicatorColor(signal.Trend1d);
                    break;

                case ColumnsForGrid.Barometer15m:
                    foreColor = SimpleColor(signal.Barometer15m);
                    break;
                case ColumnsForGrid.Barometer30m:
                    foreColor = SimpleColor(signal.Barometer30m);
                    break;
                case ColumnsForGrid.Barometer1h:
                    foreColor = SimpleColor(signal.Barometer1h);
                    break;
                case ColumnsForGrid.Barometer4h:
                    foreColor = SimpleColor(signal.Barometer4h);
                    break;
                case ColumnsForGrid.Barometer1d:
                    foreColor = SimpleColor(signal.Barometer1d);
                    break;

                case ColumnsForGrid.PriceMin:
                    {
                        decimal value = signal.PriceMin;
                        if (value <= 0)
                            foreColor = Color.Red;
                        else
                            foreColor = Color.Green;
                    }
                    break;
                case ColumnsForGrid.PriceMax:
                    {
                        decimal value = signal.PriceMax;
                        if (value <= 0)
                            foreColor = Color.Red;
                        else
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.PriceMinPerc:
                    {
                        double value = signal.PriceMinPerc;
                        if (signal.Side == CryptoTradeSide.Long)
                        {
                            if (value <= 0)
                                foreColor = Color.Red;
                            else
                                foreColor = Color.Green;
                        }
                        else
                        {
                            if (value <= 0)
                                foreColor = Color.Green;
                            else
                                foreColor = Color.Red;
                        }
                    }
                    break;
                case ColumnsForGrid.PriceMaxPerc:
                    {
                        double value = signal.PriceMaxPerc;
                        if (signal.Side == CryptoTradeSide.Long)
                        {
                            if (value <= 0)
                                foreColor = Color.Red;
                            else
                                foreColor = Color.Green;
                        }
                        else
                        {
                            if (value <= 0)
                                foreColor = Color.Green;
                            else
                                foreColor = Color.Red;
                        }
            }
            break;
            }

            DataGridViewCell cell = Grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            cell.Style.BackColor = backColor;
            cell.Style.ForeColor = foreColor;
        }
    }


    private static bool UpdateSignalStatistics(CryptoSignal signal)
    {
        if (!signal.BackTest) //  && signal.Strategy != CryptoSignalStrategy.Jump
        {

            CryptoSymbolInterval symbolInterval = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
            if (symbolInterval.CandleList.Count > 0)
            {
                CryptoCandle candle = symbolInterval.CandleList.Values[^1]; // todo, not working for emulator!
                try
                {
                    if (candle.Low < signal.PriceMin || signal.PriceMin == 0)
                    {
                        signal.PriceMin = candle.Low;
                        signal.PriceMinPerc = (double)(100 * (signal.PriceMin / signal.SignalPrice - 1));
                        return true;
                    }

                    if (candle.High > signal.PriceMax || signal.PriceMax == 0)
                    {
                        signal.PriceMax = candle.High;
                        signal.PriceMaxPerc = (double)(100 * (signal.PriceMax / signal.SignalPrice - 1));
                        return true;
                    }
                }
                catch
                {
                    // ignore (sometimes low of high value not set, need locking?)
                }
            }
        }
        return false;
    }


    private void ClearOldSignals(object? sender, EventArgs? e)
    {
        if (GlobalData.BackTest)
            return;

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

                        DateTime expirationDate = signal.GetExpirationDate(signal.Interval);
                        if (expirationDate < DateTime.UtcNow)
                        {
                            if (GlobalData.Settings.General.DebugSignalStrength)
                            {
                                if (UpdateSignalStatistics(signal))
                                    GlobalData.ThreadSaveObjects!.AddToQueue(signal);
                            }
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


#if DEBUG
    static long LastStatisticUpdate = 0;

    private void UpdateStatistics()
    {
        // statistics (not sure where to put it right now)
        if (GlobalData.BackTest)
            return;

        // Avoid needless updates
        long x = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
        if (x == LastStatisticUpdate)
            return;
        LastStatisticUpdate = x;

        // Avoid duplicate calls (when the list is serious long)
        if (Monitor.TryEnter(List))
        {
            try
            {
                if (List.Count > 0)
                {
                    for (int index = List.Count - 1; index >= 0; index--)
                    {
                        CryptoSignal signal = List[index];
                        if (GlobalData.Settings.General.DebugSignalStrength)
                        {
                            if (UpdateSignalStatistics(signal))
                                GlobalData.ThreadSaveObjects!.AddToQueue(signal);
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
#endif

    private void RefreshInformation(object? sender, EventArgs? e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        try
        {
            Grid.SuspendDrawing();
            try
            {
                Grid.InvalidateColumn((int)ColumnsForGrid.SignalPrice);
                Grid.InvalidateColumn((int)ColumnsForGrid.PriceChange);

#if DEBUG
                if (GlobalData.Settings.General.DebugSignalStrength)
                {
                    UpdateStatistics();
                    Grid.InvalidateColumn((int)ColumnsForGrid.PriceMin);
                    Grid.InvalidateColumn((int)ColumnsForGrid.PriceMax);
                    Grid.InvalidateColumn((int)ColumnsForGrid.PriceMinPerc);
                    Grid.InvalidateColumn((int)ColumnsForGrid.PriceMaxPerc);
                }
#endif
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

    //internal void CreatePosition(object? sender, EventArgs? e)
    //{
    //    if (GlobalData.ApplicationIsClosing)
    //        return;

    //    // De rest van de commando's heeft een object nodig
    //    var (succes, _, _, signal, _, _) = CommandTools.GetAttributesFromSender(SelectedObject);
    //    if (succes && (GlobalData.ActiveAccount!.AccountType == CryptoAccountType.Altrady))
    //    {
    //        CryptoSymbolInterval symbolInterval = signal.Symbol.GetSymbolInterval(signal.Interval.IntervalPeriod);
    //        CryptoPosition position = PositionTools.CreatePosition(GlobalData.ActiveAccount!, signal.Symbol, signal.Strategy, signal.Side, symbolInterval, signal.CloseTime);
    //        PositionTools.AddSignalProperties(position, signal);

    //        CryptoDatabase database = new();
    //        try
    //        {
    //            database.Open();
    //            database.Connection.Insert(position);
    //            PositionTools.AddPosition(GlobalData.ActiveAccount!, position);
    //            var part = PositionTools.ExtendPosition(database, position, CryptoPartPurpose.Entry, signal.Interval, signal.Strategy,
    //                CryptoEntryOrDcaStrategy.AfterNextSignal, signal.SignalPrice, signal.CloseTime);

                 
    //            {
    //                decimal entryValue = signal.Symbol.QuoteData!.EntryAmount;

    //                // Voor market en limit nemen we de actionprice (quantiry berekenen)
    //                decimal price = signal.SignalPrice;
    //                if (price == 0)
    //                    price = signal.Symbol.LastPrice ?? 0;

    //                if (position.Side == CryptoTradeSide.Long)
    //                    price *= 0.97m;
    //                if (position.Side == CryptoTradeSide.Short)
    //                    price *= 1.04m;
    //                price = price.Clamp(signal.Symbol.PriceMinimum, signal.Symbol.PriceMaximum, signal.Symbol.PriceTickSize);

    //                decimal entryQuantity = entryValue / price; // "afgerond"
    //                entryQuantity = entryQuantity.Clamp(signal.Symbol.QuantityMinimum, signal.Symbol.QuantityMaximum, signal.Symbol.QuantityTickSize);
    //                if (position.Invested == 0)
    //                    entryQuantity = TradeTools.CorrectEntryQuantityIfWayLess(signal.Symbol, entryValue, entryQuantity, price);

    //                part.CloseTime = signal.CloseTime;
    //                database.Connection.Update(part);

    //                position.Reposition = false;
    //                position.EntryPrice = signal.SignalPrice;
    //                position.EntryAmount = entryQuantity;
    //                position.CloseTime = signal.CloseTime;
    //                position.Status = CryptoPositionStatus.Altrady;
    //                database.Connection.Update(position);

    //                GlobalData.PositionsHaveChanged("");


    //                AltradyWebhook.DelegateControlToAltrady(position);
    //                database.Connection.Update(position);
    //            }

    //        }
    //        finally
    //        {
    //            database.Close();
    //        }
    //    }
    //}

}