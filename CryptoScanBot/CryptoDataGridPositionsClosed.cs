using CryptoScanBot.Commands;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.Core.Trader;
using CryptoScanBot.Core.Trend;

using Dapper;

namespace CryptoScanBot;

public class CryptoDataGridPositionsClosed<T>(DataGridView grid, List<T> list, SortedList<string, ColumnSetting> columnList) : 
    CryptoDataGrid<T>(grid, list, columnList) where T : CryptoPosition
{
    private enum ColumnsForGrid
    {
        Id,
        AltradyId,
        Created,
        Closed,
        Duration,
        Account,
        Exchange,
        Symbol,
        Interval,
        Side,
        Strategy,
        Status,
        Invested,
        Returned,
        Commission,
        Profit,
        Percentage,
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

        MinimumEntry,
        // statistics
        PriceMin,
        PriceMax,
        PriceMinPerc,
        PriceMaxPerc,
    }

    public override void InitializeCommands(ContextMenuStrip menuStrip)
    {
        menuStrip.AddCommand(this, "Activate trading app", Command.ActivateTradingApp);
        menuStrip.AddCommand(this, "TradingView internal", Command.ActivateTradingviewIntern);
        menuStrip.AddCommand(this, "TradingView external", Command.ActivateTradingviewExtern);

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Position recalculate", Command.None, CommandPositionRecalculateExecute);
        menuStrip.AddCommand(this, "Position delete from database", Command.None, CommandPositionDeleteFromDatabase);
        menuStrip.AddCommand(this, "Export position information to Excel", Command.ExcelPositionInformation);
        menuStrip.AddCommand(this, "Export all position information to Excel", Command.ExcelPositionsInformation);

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Copy symbol name", Command.CopySymbolInformation);
        menuStrip.AddCommand(this, "Copy all data cells", Command.CopyDataGridCells);
        menuStrip.AddCommand(this, "Export trend information to log", Command.ShowTrendInformation);
        menuStrip.AddCommand(this, "Export symbol information to Excel", Command.ExcelSymbolInformation);

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Hide selection", Command.None, ClearSelection);

#if DEBUG
        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Test - Show graph information", Command.ShowGraph);
#endif
    }


    public override void InitializeHeaders()
    {
        SortColumn = (int)ColumnsForGrid.Closed;
        SortOrder = SortOrder.Descending;

        var columns = Enum.GetValues(typeof(ColumnsForGrid));
        foreach (ColumnsForGrid column in columns)
        {
            switch (column)
            {
                case ColumnsForGrid.Id:
                    CreateColumn("Id", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 50).Visible = false;
                    break;
                case ColumnsForGrid.AltradyId:
                    CreateColumn("AltradyId", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 50).Visible = false;
                    break;                    
                case ColumnsForGrid.Created:
                    CreateColumn("Created", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 100);
                    break;
                case ColumnsForGrid.Closed:
                    CreateColumn("Closed", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 100);
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
                case ColumnsForGrid.Profit:
                    CreateColumn("Profit", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                case ColumnsForGrid.Percentage:
                    CreateColumn("Percent", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                case ColumnsForGrid.Parts:
                    CreateColumn("Parts", typeof(int), string.Empty, DataGridViewContentAlignment.MiddleCenter, 34);
                    break;
                case ColumnsForGrid.EntryPrice:
                    CreateColumn("Entry Price", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75).Visible = false;
                    break;
                case ColumnsForGrid.ProfitPrice:
                    CreateColumn("Profit Price", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75).Visible = false;
                    break;
                case ColumnsForGrid.FundingRate:
                    CreateColumn("Funding Rate", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 55).Visible = false;
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
                case ColumnsForGrid.Trend15m:
                    CreateColumn("Trend 15m", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.Trend30m:
                    CreateColumn("Trend 30m", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.Trend1h:
                    CreateColumn("Trend 1h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.Trend4h:
                    CreateColumn("Trend 4h", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
                    break;
                case ColumnsForGrid.Trend1d:
                    CreateColumn("Trend 1d", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42).Visible = false;
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


    private int Compare(CryptoPosition a, CryptoPosition b)
    {
        int compareResult = (ColumnsForGrid)SortColumn switch
        {
            ColumnsForGrid.Id => ObjectCompare.Compare(a.Id, b.Id),
            ColumnsForGrid.AltradyId => ObjectCompare.Compare(a.AltradyPositionId, b.AltradyPositionId),
            ColumnsForGrid.Created => ObjectCompare.Compare(a.CreateTime, b.CreateTime),
            ColumnsForGrid.Closed => ObjectCompare.Compare(a.CloseTime, b.CloseTime),
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
            ColumnsForGrid.Profit => ObjectCompare.Compare(a.Profit, b.Profit),
            ColumnsForGrid.Parts => ObjectCompare.Compare(a.PartCount - Convert.ToInt32(a.ActiveDca), b.PartCount - Convert.ToInt32(b.ActiveDca)),
            ColumnsForGrid.EntryPrice => ObjectCompare.Compare(a.EntryPrice, b.EntryPrice),
            ColumnsForGrid.ProfitPrice => ObjectCompare.Compare(a.ProfitPrice, b.ProfitPrice),
            ColumnsForGrid.Percentage => ObjectCompare.Compare(a.Percentage, b.Percentage),
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


    public override void GetTextFunction(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        CryptoPosition? position = GetCellObject(e.RowIndex);
        if (position != null)
        {
            switch ((ColumnsForGrid)e.ColumnIndex)
            {
                case ColumnsForGrid.Id:
                    e.Value = position.Id.ToString();
                    break;
                case ColumnsForGrid.AltradyId:
                    e.Value = position.AltradyPositionId;
                    break;
                case ColumnsForGrid.Created:
                    e.Value = position.CreateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                    break;
                case ColumnsForGrid.Closed:
                    e.Value = position.CloseTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
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
                case ColumnsForGrid.Profit:
                    e.Value = position.Profit.ToString(position.Symbol.QuoteData!.DisplayFormat);
                    break;
                case ColumnsForGrid.Percentage:
                    e.Value = position.Percentage.ToString("N2");
                    break;
                case ColumnsForGrid.Parts:
                    e.Value = position.PartCountText(false);
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
                    e.Value = (position.RemainingDust * position.Symbol.LastPrice).ToString0("N8");
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
                case ColumnsForGrid.AvgBB:
                    e.Value = position.AvgBB;
                    break;
                case ColumnsForGrid.Rsi:
                    e.Value = position.Rsi;
                    break;
                case ColumnsForGrid.MacdValue:
                    e.Value = position.MacdValue.ToString0(position.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.MacdSignal:
                    e.Value = position.MacdSignal.ToString0(position.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.MacdHistogram:
                    e.Value = position.MacdHistogram.ToString0(position.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.SlopeRsi:
                    e.Value = position.SlopeRsi;
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
                case ColumnsForGrid.MinimumEntry:
                    e.Value = position.MinEntry.ToString(position.Symbol.QuoteData!.DisplayFormat);
                    break;
                case ColumnsForGrid.PriceMin:
                    if (position.PriceMin! != 0m)
                        e.Value = position.PriceMin.ToString(position.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.PriceMax:
                    if (position.PriceMax! != 0m)
                        e.Value = position.PriceMax.ToString(position.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.PriceMinPerc:
                    if (position.PriceMinPerc! != 0)
                        e.Value = position.PriceMinPerc.ToString("N2");
                    break;
                case ColumnsForGrid.PriceMaxPerc:
                    if (position.PriceMaxPerc! != 0)
                        e.Value = position.PriceMaxPerc.ToString("N2");
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
        CryptoPosition? position = GetCellObject(e.RowIndex);
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

                case ColumnsForGrid.Profit: // Current profit
                    decimal NetPnlPerc1 = position.Profit;
                    if (NetPnlPerc1 > 0)
                        foreColor = Color.Green;
                    else if (NetPnlPerc1 < 0)
                        foreColor = Color.Red;
                    break;

                case ColumnsForGrid.Percentage:
                    decimal bePerc = position.Percentage;
                    if (bePerc > 100)
                        foreColor = Color.Green;
                    else if (bePerc < 100)
                        foreColor = Color.Red;
                    break;

                case ColumnsForGrid.FundingRate: // fundingrate
                    if (position.Symbol.FundingRate > 0)
                        foreColor = Color.Green;
                    else if (position.Symbol.FundingRate < 0)
                        foreColor = Color.Red;
                    break;

                case ColumnsForGrid.Rsi:
                    {
                        // Oversold/overbougt
                        double? value = position.Rsi; // 0..100
                        if (value < GlobalData.Settings.General.RsiValueOversold)
                            foreColor = Color.Red;
                        else if (value > GlobalData.Settings.General.RsiValueOverbought)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.AvgBB:
                    {
                        // Oversold/overbougt
                        double? value = position.AvgBB;
                        if (value < 1.5)
                            foreColor = Color.Red;
                        else if (value > 1.5)
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.SlopeRsi:
                    {
                        double? value = position.SlopeRsi;
                        if (value < 0)
                            foreColor = Color.Red;
                        else if (value > 0)
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

                case ColumnsForGrid.PriceMin:
                    {
                        decimal value = position.PriceMin;
                        if (value <= 0)
                            foreColor = Color.Red;
                        else
                            foreColor = Color.Green;
                    }
                    break;
                case ColumnsForGrid.PriceMax:
                    {
                        decimal value = position.PriceMax;
                        if (value <= 0)
                            foreColor = Color.Red;
                        else
                            foreColor = Color.Green;
                    }
                    break;

                case ColumnsForGrid.PriceMinPerc:
                    {
                        double value = position.PriceMinPerc;
                        if (position.Side == CryptoTradeSide.Long)
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
                        double value = position.PriceMaxPerc;
                        if (position.Side == CryptoTradeSide.Long)
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
        }

        DataGridViewCell cell = Grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        cell.Style.BackColor = backColor;
        cell.Style.ForeColor = foreColor;
    }


    private async void CommandPositionRecalculateExecute(object? sender, EventArgs? e)
    {
        CryptoPosition? position = GetSelectedObject(out int rowIndex);
        if (position != null)
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position, forceCalculation: true);

            Grid.InvalidateRow(rowIndex);
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig positie herberekend");
        }

    }


    private void CommandPositionDeleteFromDatabase(object? sender, EventArgs? e)
    {
        CryptoPosition? position = GetSelectedObject(out int _);
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
                GlobalData.PositionsClosed.Remove(position);
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig positie uit database verwijderd");
                GlobalData.PositionsHaveChanged("");
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"error deleteing position {error.Message}");
            }
        }
    }

}
