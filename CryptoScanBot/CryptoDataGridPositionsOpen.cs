using CryptoScanBot.Commands;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Trader;

using Dapper;
using Dapper.Contrib.Extensions;
using CryptoScanBot.Core.Excel;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot;

public class CryptoDataGridPositionsOpen<T>(DataGridView grid, List<T> list, SortedList<string, ColumnSetting> columnList) :
    CryptoDataGrid<T>(grid, list, columnList) where T : CryptoPosition
{
    private enum ColumnsForGrid
    {
        Id,
        Created,
        Updated,
        Duration,
        Account,
        Exchange,
        Symbol,
        Interval,
        Strategy,
        Side,
        Status,
        Invested,
        Returned,
        Commission,
        BreakEven,
        Quantity,
        Open,
        Profit,
        Percentage,
        BreakEvenPercent,
        Parts,
        EntryPrice,
        ProfitPrice,
        FundingRate,
        QuantityTick,
        RemainingDust,
        DustValue,

        //Signal information
        SignalDate,
        SignalPrice,
        SignalVolume,
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
        Lux5m,
        //FundingRate,
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
    }

    private System.Windows.Forms.Timer? TimerRefreshInformation = null;


    public override void InitializeCommands(ContextMenuStrip menuStrip)
    {
        menuStrip.AddCommand(this, "Activate trading app", Command.ActivateTradingApp);
        menuStrip.AddCommand(this, "TradingView internal", Command.ActivateTradingviewIntern);
        menuStrip.AddCommand(this, "TradingView external", Command.ActivateTradingviewExtern);
        //menuStrip.AddCommand(this, "Exchange ", Command.ActivateActiveExchange);

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Position recalculate", Command.None, CommandPositionRecalculateExecute);
        menuStrip.AddCommand(this, "Position delete from database", Command.None, CommandPositionDeleteFromDatabaseAsync);
        menuStrip.AddCommand(this, "Position add additional DCA", Command.None, CommandPositionCreateAdditionalDca);
        menuStrip.AddCommand(this, "Position cancel open DCA", Command.None, CommandPositionRemoveAdditionalDca);
        //menuStrip.AddCommand(this, "Position take profit (if possible)", Command.None, CommandPositionLastPartTakeProfit);
        menuStrip.AddCommand(this, "Position information (Excel)", Command.ExcelPositionInformation);
        menuStrip.AddCommand(this, "Positions information (Excel)", Command.None, DumpPositions);

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Copy symbol name", Command.CopySymbolInformation);
        menuStrip.AddCommand(this, "Trend information (log)", Command.ShowTrendInformation);
        menuStrip.AddCommand(this, "Symbol information (Excel)", Command.ExcelSymbolInformation);

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Hide selection", Command.None, ClearSelection);

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
        SortColumn = (int)ColumnsForGrid.Created;

        var columns = Enum.GetValues(typeof(ColumnsForGrid));
        foreach (ColumnsForGrid column in columns)
        {
            switch (column)
            {
                case ColumnsForGrid.Id:
                    CreateColumn("Id", typeof(int), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
                    break;
                case ColumnsForGrid.Created:
                    CreateColumn("Created", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 100);
                    break;
                case ColumnsForGrid.Updated:
                    CreateColumn("Updated", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 100);
                    break;
                case ColumnsForGrid.Duration:
                    CreateColumn("Duration", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight, 55);
                    break;
                case ColumnsForGrid.Account:
                    CreateColumn("Account", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 100);
                    break;
                case ColumnsForGrid.Exchange:
                    CreateColumn("Exchange", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 125).Visible = false;
                    break;
                case ColumnsForGrid.Symbol:
                    CreateColumn("Symbol", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 100, true);
                    break;
                case ColumnsForGrid.Interval:
                    CreateColumn("Interval", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 55);
                    break;
                case ColumnsForGrid.Strategy:
                    CreateColumn("Strategy", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 65);
                    break;
                case ColumnsForGrid.Side:
                    CreateColumn("Mode", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 65);
                    break;
                case ColumnsForGrid.Status:
                    CreateColumn("Status", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 55);
                    break;
                case ColumnsForGrid.Invested:
                    CreateColumn("Invested", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 65);
                    break;
                case ColumnsForGrid.Returned:
                    CreateColumn("Returned", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 65);
                    break;
                case ColumnsForGrid.Commission:
                    CreateColumn("Fee", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 65);
                    break;
                case ColumnsForGrid.BreakEven:
                    CreateColumn("BreakEven", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                case ColumnsForGrid.Quantity:
                    CreateColumn("Quantity", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                case ColumnsForGrid.Open:
                    CreateColumn("Open", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                case ColumnsForGrid.Profit:
                    CreateColumn("Net NPL", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                case ColumnsForGrid.Percentage:
                    CreateColumn("Net NPL%", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 55);
                    break;
                case ColumnsForGrid.BreakEvenPercent:
                    CreateColumn("BE perc", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 55);
                    break;
                case ColumnsForGrid.Parts:
                    CreateColumn("Parts", typeof(int), string.Empty, DataGridViewContentAlignment.MiddleCenter, 34);
                    break;
                case ColumnsForGrid.EntryPrice:
                    CreateColumn("Entry Price", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                case ColumnsForGrid.ProfitPrice:
                    CreateColumn("Profit Price", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                case ColumnsForGrid.FundingRate:
                    CreateColumn("Funding Rate", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 55);
                    break;
                case ColumnsForGrid.QuantityTick:
                    CreateColumn("Q Tick", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                case ColumnsForGrid.RemainingDust:
                    CreateColumn("Dust", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                case ColumnsForGrid.DustValue:
                    CreateColumn("DustValue", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;


                case ColumnsForGrid.SignalDate:
                    CreateColumn("Candle date", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 140).Visible = false;
                    break;
                case ColumnsForGrid.SignalPrice:
                    CreateColumn("Price", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight, 70).Visible = false;
                    break;
                case ColumnsForGrid.SignalVolume:
                    CreateColumn("Volume", typeof(decimal), "#,##0", DataGridViewContentAlignment.MiddleRight, 80).Visible = false;
                    break;
                case ColumnsForGrid.TfTrend:
                    CreateColumn("TF trend", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 50).Visible = false;
                    break;
                case ColumnsForGrid.MarketTrend:
                    CreateColumn("Market trend%", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.Change24h:
                    CreateColumn("24h Change", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.Move24h:
                    CreateColumn("24h Move", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
                    break;
                case ColumnsForGrid.BB:
                    CreateColumn("BB%", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 50).Visible = false;
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
                case ColumnsForGrid.Lux5m:
                    CreateColumn("Lux 5m", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 45).Visible = false;
                    break;
                case ColumnsForGrid.Trend15m:
                    CreateColumn("15m", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.Trend30m:
                    CreateColumn("30m", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.Trend1h:
                    CreateColumn("1h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.Trend4h:
                    CreateColumn("4h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.Trend1d:
                    CreateColumn("1d", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
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

                default:
                    throw new NotImplementedException();
            };
        }
    }


    private int Compare(CryptoPosition a, CryptoPosition b)
    {
        int compareResult = (ColumnsForGrid)SortColumn switch
        {
            ColumnsForGrid.Id => ObjectCompare.Compare(a.Id, b.Id),
            ColumnsForGrid.Created => ObjectCompare.Compare(a.CreateTime, b.CreateTime),
            ColumnsForGrid.Updated => ObjectCompare.Compare(a.UpdateTime, b.UpdateTime),
            ColumnsForGrid.Duration => ObjectCompare.Compare(a.Duration().TotalSeconds, b.Duration().TotalSeconds),
            ColumnsForGrid.Account => ObjectCompare.Compare(a.Account.AccountType, b.Account.AccountType),
            ColumnsForGrid.Exchange => ObjectCompare.Compare(a.Exchange.Name, b.Exchange.Name),
            ColumnsForGrid.Symbol => ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name),
            ColumnsForGrid.Interval => ObjectCompare.Compare(a.Interval!.IntervalPeriod, b.Interval!.IntervalPeriod),
            ColumnsForGrid.Strategy => ObjectCompare.Compare(a.StrategyText, b.StrategyText),
            ColumnsForGrid.Side => ObjectCompare.Compare(a.SideText, b.SideText),
            ColumnsForGrid.Status => ObjectCompare.Compare(a.Status, b.Status),
            ColumnsForGrid.Invested => ObjectCompare.Compare(a.Invested, b.Invested),
            ColumnsForGrid.Returned => ObjectCompare.Compare(a.Returned, b.Returned),
            ColumnsForGrid.Commission => ObjectCompare.Compare(a.Commission, b.Commission),
            ColumnsForGrid.BreakEven => ObjectCompare.Compare(a.BreakEvenPrice, b.BreakEvenPrice),
            ColumnsForGrid.Quantity => ObjectCompare.Compare(a.Quantity, b.Quantity),
            ColumnsForGrid.Open => ObjectCompare.Compare(a.Invested - a.Returned - a.Commission, b.Invested - b.Returned - b.Commission),
            ColumnsForGrid.Profit => ObjectCompare.Compare(a.CurrentProfit(), b.CurrentProfit()),
            ColumnsForGrid.BreakEvenPercent => ObjectCompare.Compare(a.CurrentBreakEvenPercentage(), b.CurrentBreakEvenPercentage()),
            ColumnsForGrid.Parts => ObjectCompare.Compare(a.PartCount, b.PartCount),
            ColumnsForGrid.EntryPrice => ObjectCompare.Compare(a.EntryPrice, b.EntryPrice),
            ColumnsForGrid.ProfitPrice => ObjectCompare.Compare(a.ProfitPrice, b.ProfitPrice),
            ColumnsForGrid.Percentage => ObjectCompare.Compare(a.CurrentProfitPercentage(), b.CurrentProfitPercentage()),
            ColumnsForGrid.FundingRate => ObjectCompare.Compare(a.Symbol.FundingRate, b.Symbol.FundingRate),
            ColumnsForGrid.QuantityTick => ObjectCompare.Compare(a.Symbol.QuantityTickSize, b.Symbol.QuantityTickSize),
            ColumnsForGrid.RemainingDust => ObjectCompare.Compare(a.RemainingDust, b.RemainingDust),
            ColumnsForGrid.DustValue => ObjectCompare.Compare(a.RemainingDust * a.Symbol.LastPrice, b.RemainingDust * b.Symbol.LastPrice),

            // Signal information
            ColumnsForGrid.SignalDate => ObjectCompare.Compare(a.SignalEventTime, b.SignalEventTime),
            ColumnsForGrid.SignalPrice => ObjectCompare.Compare(a.SignalPrice, b.SignalPrice),
            ColumnsForGrid.SignalVolume => ObjectCompare.Compare(a.SignalVolume, b.SignalVolume),
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
            ColumnsForGrid.Lux5m => ObjectCompare.Compare(a.LuxIndicator5m, b.LuxIndicator5m),
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
            _ => 0
        };


        // extend if still the same
        if (compareResult == 0)
        {
            compareResult = ObjectCompare.Compare(a.CreateTime, b.CreateTime);
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


    public override void GetTextFunction(object sender, DataGridViewCellValueEventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        CryptoPosition position = GetCellObject(e.RowIndex);
        if (position != null)
        {
            switch ((ColumnsForGrid)e.ColumnIndex)
            {
                case ColumnsForGrid.Id:
                    e.Value = position.Id.ToString();
                    break;
                case ColumnsForGrid.Created:
                    e.Value = position.CreateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                    break;
                case ColumnsForGrid.Updated:
                    e.Value = position.UpdateTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                    break;
                case ColumnsForGrid.Duration:
                    e.Value = position.DurationText();
                    break;
                case ColumnsForGrid.Account:
                    e.Value = position.Account.AccountType;
                    break;
                case ColumnsForGrid.Exchange:
                    e.Value = position.Symbol.Exchange.Name;
                    break;
                case ColumnsForGrid.Symbol:
                    e.Value = position.Symbol.Name;
                    break;
                case ColumnsForGrid.Interval:
                    e.Value = position.Interval!.Name;
                    break;
                case ColumnsForGrid.Strategy:
                    e.Value = position.StrategyText;
                    break;
                case ColumnsForGrid.Side:
                    e.Value = position.SideText;
                    break;
                case ColumnsForGrid.Status:
                    e.Value = position.Status.ToString();
                    break;
                case ColumnsForGrid.Invested:
                    e.Value = position.Invested.ToString(position.Symbol.QuoteData!.DisplayFormat);
                    break;
                case ColumnsForGrid.Returned:
                    e.Value = position.Returned.ToString(position.Symbol.QuoteData!.DisplayFormat);
                    break;
                case ColumnsForGrid.Commission:
                    e.Value = position.Commission.ToString(position.Symbol.QuoteData!.DisplayFormat);
                    break;
                case ColumnsForGrid.BreakEven:
                    if (position.Status == CryptoPositionStatus.Waiting)
                        e.Value = position.EntryPrice?.ToString(position.Symbol.PriceDisplayFormat);
                    else
                        e.Value = position.BreakEvenPrice.ToString(position.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.Quantity:
                    e.Value = position.Quantity.ToString0(position.Symbol.QuantityDisplayFormat);
                    break;
                case ColumnsForGrid.Open:
                    e.Value = (position.Invested - position.Returned).ToString(position.Symbol.QuoteData!.DisplayFormat);
                    break;
                case ColumnsForGrid.Profit:
                    e.Value = position.CurrentProfit().ToString(position.Symbol.QuoteData!.DisplayFormat);
                    break;
                case ColumnsForGrid.Percentage:
                    e.Value = position.CurrentProfitPercentage().ToString("N2");
                    break;
                case ColumnsForGrid.BreakEvenPercent:
                    e.Value = position.CurrentBreakEvenPercentage().ToString("N2");
                    break;
                case ColumnsForGrid.Parts:
                    e.Value = position.PartCountText();
                    break;
                case ColumnsForGrid.EntryPrice:
                    e.Value = position.EntryPrice?.ToString(position.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.ProfitPrice:
                    e.Value = position.ProfitPrice?.ToString(position.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.FundingRate:
                    if (position.Status == CryptoPositionStatus.Trading && position.Symbol.FundingRate != 0.0m)
                        e.Value = position.Symbol.FundingRate.ToString();
                    break;
                case ColumnsForGrid.QuantityTick:// ter debug..
                    e.Value = position.Symbol?.QuantityTickSize.ToString0();
                    break;
                case ColumnsForGrid.RemainingDust: // ter debug
                    e.Value = position.RemainingDust.ToString("N8");
                    break;
                case ColumnsForGrid.DustValue: // ter debug
                    if (position.Symbol.LastPrice.HasValue)
                    {
                        decimal dustValue = position.RemainingDust * position.Symbol.LastPrice.Value;
                        e.Value = dustValue.ToString("N8");
                    }
                    break;



                case ColumnsForGrid.SignalDate:
                    // there is a signal.CloseDate
                    //+ signal.OpenDate.AddSeconds(signal.Interval.Duration).ToLocalTime().ToString("HH:mm");
                    e.Value = position.SignalEventTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                    break;
                case ColumnsForGrid.SignalPrice:
                    e.Value = position.SignalPrice.ToString(position.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.SignalVolume:
                    e.Value = position.SignalVolume;
                    break;
                case ColumnsForGrid.TfTrend:
                    e.Value = TrendTools.TrendIndicatorText(position.TrendIndicator);
                    break;
                case ColumnsForGrid.MarketTrend:
                    e.Value = position.TrendPercentage;
                    break;
                case ColumnsForGrid.Change24h:
                    e.Value = position.Last24HoursChange;
                    break;
                case ColumnsForGrid.Move24h:
                    e.Value = position.Last24HoursEffective;
                    break;
                case ColumnsForGrid.BB:
                    e.Value = position.BollingerBandsPercentage;
                    break;
                case ColumnsForGrid.RSI:
                    e.Value = position.Rsi;
                    break;
                case ColumnsForGrid.Stoch:
                    e.Value = position.StochOscillator;
                    break;
                case ColumnsForGrid.Signal:
                    e.Value = position.StochSignal;
                    break;
                case ColumnsForGrid.Sma200:
                    e.Value = position.Sma200;
                    break;
                case ColumnsForGrid.Sma50:
                    e.Value = position.Sma50;
                    break;
                case ColumnsForGrid.Sma20:
                    e.Value = position.Sma20;
                    break;
                case ColumnsForGrid.PSar:
                    e.Value = position.PSar;
                    break;
                case ColumnsForGrid.Lux5m:
                    e.Value = position.LuxIndicator5m;
                    break;
                case ColumnsForGrid.Trend15m:
                    e.Value = TrendTools.TrendIndicatorText(position.Trend15m);
                    break;
                case ColumnsForGrid.Trend30m:
                    e.Value = TrendTools.TrendIndicatorText(position.Trend30m);
                    break;
                case ColumnsForGrid.Trend1h:
                    e.Value = TrendTools.TrendIndicatorText(position.Trend1h);
                    break;
                case ColumnsForGrid.Trend4h:
                    e.Value = TrendTools.TrendIndicatorText(position.Trend4h);
                    break;
                case ColumnsForGrid.Trend1d:
                    e.Value = TrendTools.TrendIndicatorText(position.Trend1d);
                    break;
                case ColumnsForGrid.Barometer15m:
                    e.Value = position.Barometer15m?.ToString("N2");
                    break;
                case ColumnsForGrid.Barometer30m:
                    e.Value = position.Barometer30m?.ToString("N2");
                    break;
                case ColumnsForGrid.Barometer1h:
                    e.Value = position.Barometer1h?.ToString("N2");
                    break;
                case ColumnsForGrid.Barometer4h:
                    e.Value = position.Barometer4h?.ToString("N2");
                    break;
                case ColumnsForGrid.Barometer1d:
                    e.Value = position.Barometer1d?.ToString("N2");
                    break;

                default:
                    e.Value = '?';
                    break;
            }
        }
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
        CryptoPosition position = GetCellObject(e.RowIndex);
        if (position != null)
        {
            switch ((ColumnsForGrid)e.ColumnIndex)
            {
                case ColumnsForGrid.Symbol: // symbol
                    Color displayColor = position.Symbol.QuoteData!.DisplayColor;
                    if (displayColor != Color.White)
                        backColor = displayColor;
                    break;

                case ColumnsForGrid.Strategy: // strategy
                    Color color = GetBackgroudColorForStrategy(position.Strategy, position.Side);
                    if (color != Color.White)
                        backColor = color;
                    break;

                case ColumnsForGrid.Side:
                    if (position.Side == CryptoTradeSide.Long)
                        foreColor = Color.Green;
                    else
                        foreColor = Color.Red;
                    break;

                case ColumnsForGrid.Status:
                    if (position.Status == CryptoPositionStatus.Waiting)
                        foreColor = Color.Red;
                    break;

                case ColumnsForGrid.Open: // Open value
                    if (position.Invested - position.Returned > 7 * position.Symbol.QuoteData!.EntryAmount) // just an indication how deep we are in a trade
                        foreColor = Color.Red;
                    break;

                case ColumnsForGrid.Profit: // Current profit
                case ColumnsForGrid.Percentage:
                    decimal NetPnlPerc1 = position.CurrentProfitPercentage();
                    if (NetPnlPerc1 > 0)
                        foreColor = Color.Green;
                    else if (NetPnlPerc1 < 0)
                        foreColor = Color.Red;
                    break;

                case ColumnsForGrid.BreakEvenPercent: // Percentage opposite to BreakEven
                    decimal bePerc = position.CurrentBreakEvenPercentage();
                    if (bePerc > 0)
                        foreColor = Color.Green;
                    else if (bePerc < 0)
                        foreColor = Color.Red;
                    break;

                case ColumnsForGrid.FundingRate: // fundingrate
                    if (position.Symbol.FundingRate > 0)
                        foreColor = Color.Green;
                    else if (position.Symbol.FundingRate < 0)
                        foreColor = Color.Red;
                    break;

                case ColumnsForGrid.RSI:
                    {
                        // Oversold/overbougt
                        double? value = position.Rsi; // 0..100
                        if (value < GlobalData.Settings.General.RsiValueOversold)
                            foreColor = Color.Red;
                        else if (value > GlobalData.Settings.General.RsiValueOverbought)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.Stoch:
                    {
                        // Oversold/overbougt
                        double? value = position.StochOscillator;
                        if (value < GlobalData.Settings.General.StochValueOversold)
                            foreColor = Color.Red;
                        else if (value > GlobalData.Settings.General.StochValueOverbought)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.Trend15m:
                    foreColor = TrendIndicatorColor(position.Trend15m);
                    break;
                case ColumnsForGrid.Trend30m:
                    foreColor = TrendIndicatorColor(position.Trend30m);
                    break;
                case ColumnsForGrid.Trend1h:
                    foreColor = TrendIndicatorColor(position.Trend1h);
                    break;
                case ColumnsForGrid.Trend4h:
                    foreColor = TrendIndicatorColor(position.Trend4h);
                    break;
                case ColumnsForGrid.Trend1d:
                    foreColor = TrendIndicatorColor(position.Trend1d);
                    break;

                case ColumnsForGrid.Barometer15m:
                    foreColor = SimpleColor(position.Barometer15m);
                    break;
                case ColumnsForGrid.Barometer30m:
                    foreColor = SimpleColor(position.Barometer30m);
                    break;
                case ColumnsForGrid.Barometer1h:
                    foreColor = SimpleColor(position.Barometer1h);
                    break;
                case ColumnsForGrid.Barometer4h:
                    foreColor = SimpleColor(position.Barometer4h);
                    break;
                case ColumnsForGrid.Barometer1d:
                    foreColor = SimpleColor(position.Barometer1d);
                    break;

            }
        }

        DataGridViewCell cell = Grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        cell.Style.BackColor = backColor;
        cell.Style.ForeColor = foreColor;
    }


    private async void CommandPositionRecalculateExecute(object? sender, EventArgs? e)
    {
        CryptoPosition position = GetSelectedObject(out int rowIndex);
        if (position != null)
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position, forceCalculation: true);

            Grid.InvalidateRow(rowIndex);
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig positie {position.Id} herberekend");
        }

    }


    private void CommandPositionDeleteFromDatabaseAsync(object? sender, EventArgs? e)
    {
        CryptoPosition position = GetSelectedObject(out int _);
        if (position != null)
        {

            if (MessageBox.Show($"Delete position {position.Symbol.Name}", "Delete position?", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            try
            {
                using CryptoDatabase databaseThread = new();
                databaseThread.Connection.Open();
                PositionTools.LoadPosition(databaseThread, position);

                using var transaction = databaseThread.BeginTransaction();
                databaseThread.Connection.Execute($"delete from positionstep where positionid={position.Id}", transaction);
                databaseThread.Connection.Execute($"delete from positionpart where positionid={position.Id}", transaction);
                databaseThread.Connection.Execute($"delete from position where id={position.Id}", transaction);
                transaction.Commit();

                List.Remove((T)position);
                PositionTools.RemovePosition(position.Account, position, false);
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig positie {position.Id} uit de database verwijderd");
                GlobalData.PositionsHaveChanged("");
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"error deleting position {position.Id} {error.Message}");
            }
        }
    }


    private async void CommandPositionCreateAdditionalDca(object? sender, EventArgs? e)
    {
        CryptoPosition position = GetSelectedObject(out int rowIndex);
        if (position != null)
        {

            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position, forceCalculation: true);
            //FillItemOpen(position, item);

            // Op welke prijs? Actueel, of nog X% eronder?
            //TradeTools.
            // todo...
            //TradeTools.CalculatePositionResultsViaTrades(databaseThread, position);
            //FillItemOpen(position, item);

            //decimal adjust = GlobalData.Settings.Trading.DcaPercentage * step.Price / 100m;

            if (position.Symbol.LastPrice.HasValue)
            {

                // Corrigeer de prijs indien de koers ondertussen lager of hoger ligt
                decimal price = (decimal)position.Symbol.LastPrice;
                if (position.Side == CryptoTradeSide.Long)
                {
                    //price = step.Price - adjust;
                    if (position.Symbol.LastPrice.HasValue && position.Symbol.LastPrice < price)
                        price = (decimal)position.Symbol.LastPrice - position.Symbol.PriceTickSize;
                }
                else
                {
                    //price = step.Price + adjust;
                    if (position.Symbol.LastPrice.HasValue && position.Symbol.LastPrice > price)
                        price = (decimal)position.Symbol.LastPrice + position.Symbol.PriceTickSize;
                }


                // Zo laat mogelijk controleren vanwege extra calls naar de exchange
                //var resultCheckAssets = await CheckApiAndAssetsAsync(position.TradeAccount);
                //if (!resultCheckAssets.success)
                //{
                //    string text = $"{position.Symbol.Name} + DCA bijplaatsen op {price.ToString0(position.Symbol.PriceDisplayFormat)}";
                //    GlobalData.AddTextToLogTab(text + " " + resultCheckAssets.reaction);
                //    Symbol.ClearSignals();
                //    return;
                //}


                // De positie uitbreiden nalv een nieuw signaal (de xe bijkoop wordt altijd een aparte DCA)
                PositionTools.ExtendPosition(databaseThread, position, CryptoPartPurpose.Dca, position.Interval!, position.Strategy,
                    CryptoEntryOrDcaStrategy.FixedPercentage, price, GlobalData.GetCurrentDateTime(position.Account), true);
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig een DCA toegevoegd aan positie {position.Id}");

                //FillItemOpen(position, item);
                Grid.InvalidateRow(rowIndex);


                // Er is een 1m candle gearriveerd, acties adhv deze candle..
                var symbolPeriod = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                if (symbolPeriod.CandleList.Count > 0)
                {
                    var lastCandle1m = symbolPeriod.CandleList.Values.Last();
                    PositionMonitor positionMonitor = new(position.Account, position.Symbol, lastCandle1m);
                    await positionMonitor.HandlePosition(position);
                }
            }
        }

    }


    private async void CommandPositionRemoveAdditionalDca(object? sender, EventArgs? e)
    {
        CryptoPosition position = GetSelectedObject(out int rowIndex);
        if (position != null)
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position, forceCalculation: true);

            // Er is een 1m candle gearriveerd, acties adhv deze candle..
            var symbolPeriod = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
            if (symbolPeriod.CandleList.Count > 0)
            {
                var lastCandle1m = symbolPeriod.CandleList.Values.Last();
                long lastCandle1mCloseTime = lastCandle1m.OpenTime + 60;
                DateTime lastCandle1mCloseTimeDate = CandleTools.GetUnixDate(lastCandle1mCloseTime);

                PositionMonitor positionMonitor = new(position.Account, position.Symbol, lastCandle1m);
                await positionMonitor.HandlePosition(position);


                var entryOrderSide = position.GetEntryOrderSide();
                foreach (CryptoPositionPart part in position.PartList.Values.ToList())
                {
                    if (!part.CloseTime.HasValue && part.Purpose == CryptoPartPurpose.Dca)
                    {
                        foreach (CryptoPositionStep step in part.StepList.Values.ToList())
                        {
                            if (!step.CloseTime.HasValue && step.Side == entryOrderSide)
                            {
                                string cancelReason = $"annuleren vanwege handmatig annuleren DCA positie {position.Id}";
                                var (success, _) = await TradeTools.CancelOrder(databaseThread, position, part, step,
                                    lastCandle1mCloseTimeDate, CryptoOrderStatus.ManuallyByUser, cancelReason);
                                if (success)
                                {
                                    part.CloseTime = DateTime.UtcNow;
                                    databaseThread.Connection.Update<CryptoPositionPart>(part);

                                    position.ActiveDca = false;
                                    databaseThread.Connection.Update<CryptoPosition>(position);

                                    GlobalData.AddTextToLogTab($"{position.Symbol.Name} positie {position.Id} handmatig de openstaande DCA {part.PartNumber} annuleren");
                                }
                            }
                        }
                    }
                }

                Grid.InvalidateRow(rowIndex);
            }
        }

    }


    //private async void CommandPositionLastPartTakeProfit(object? sender, EventArgs? e)
    //{

    //    CryptoPosition position = GetSelectedObject(out int rowIndex);
    //    if (position != null)
    //    {
    //        if (!position.Symbol.LastPrice.HasValue)
    //            return;

    //        using CryptoDatabase databaseThread = new();
    //        databaseThread.Connection.Open();

    //        // Controleer de orders, en herbereken het geheel
    //        PositionTools.LoadPosition(databaseThread, position);
    //        await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position, forceCalculation: true);
    //        Grid.InvalidateRow(rowIndex);


    //        // Itereer de openstaande parts
    //        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
    //        {
    //            // voor de niet afgesloten parts...
    //            if (!part.CloseTime.HasValue && part.Quantity > 0)
    //            {
    //                CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();

    //                // Is er een entry order in zijn geheel gevuld (lastig indien er meerdere entries komen)
    //                CryptoPositionStep step = PositionTools.FindPositionPartStep(part, entryOrderSide, true);
    //                if (step != null && step.Status.IsFilled())
    //                {

    //                    // Is de entry prijs wel hoger/lager dan de actuele prijs?
    //                    //if (position.Side == CryptoTradeSide.Long && step.Price < part.BreakEvenPrice + 5 * position.Symbol.PriceTickSize)
    //                    //    continue;
    //                    //else if (position.Side == CryptoTradeSide.Short && step.Price > part.BreakEvenPrice - 5 * position.Symbol.PriceTickSize)
    //                    //    continue;

    //                    // Is de laatste prijs wel hoger/lager dan de actuele prijs?
    //                    if (position.Side == CryptoTradeSide.Long && position.Symbol.LastPrice.Value < part.BreakEvenPrice + 25 * position.Symbol.PriceTickSize)
    //                        continue;
    //                    else if (position.Side == CryptoTradeSide.Short && position.Symbol.LastPrice.Value > part.BreakEvenPrice - 25 * position.Symbol.PriceTickSize)
    //                        continue;

    //                    // Is er een openstaande TP die niet gevuld is?
    //                    CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
    //                    step = PositionTools.FindPositionPartStep(part, takeProfitOrderSide, false);
    //                    if (step != null && part.Quantity > 0)
    //                    {
    //                        decimal price = (decimal)position.Symbol.LastPrice;
    //                        // controle is waarschijnlijk overbodig, de extra tick is prima
    //                        // (tenzij het een market order moet zijn? Maar dan moet er meer aangepast worden)
    //                        if (position.Side == CryptoTradeSide.Long)
    //                            price += position.Symbol.PriceTickSize;
    //                        else
    //                            price -= position.Symbol.PriceTickSize;

    //                        string cancelReason = $"positie {position.Id} annuleren vanwege handmatige plaatsing TP";
    //                        var (success, _) = await TradeTools.CancelOrder(databaseThread, position, part, step, 
    //                            DateTime.UtcNow, CryptoOrderStatus.ManuallyByUser, cancelReason);
    //                        if (success)
    //                        {
    //                            price = price.Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);
    //                            await TradeTools.PlaceTakeProfitOrderAtPrice(databaseThread, position, part, price, DateTime.UtcNow, "manually placing");
    //                            Grid.InvalidateRow(rowIndex);
    //                            GlobalData.AddTextToLogTab($"{position.Symbol.Name} positie {position.Id} part={part.PartNumber} handmatig profit genomen");

    //                            break;
    //                        }

    //                    }
    //                }

    //            }
    //        }
    //    }
    //}


    private void RefreshInformation(object? sender, EventArgs? e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        try
        {
            Grid.SuspendDrawing();
            try
            {
                Grid.InvalidateColumn((int)ColumnsForGrid.Duration);
                Grid.InvalidateColumn((int)ColumnsForGrid.Status);
                Grid.InvalidateColumn((int)ColumnsForGrid.Profit);
                Grid.InvalidateColumn((int)ColumnsForGrid.Percentage);
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

    private void DumpPositions(object? sender, EventArgs? e)
    {
        List<CryptoPosition>? a = List as List<CryptoPosition>;
        _ = Task.Run(() => { new ExcelPostionsDump(a!).ExportToExcel(); });
    }
}
