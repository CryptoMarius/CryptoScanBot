using CryptoScanBot.Commands;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.Core.Trader;

using Dapper;

namespace CryptoScanBot;

public class CryptoDataGridPositionsClosed<T>(DataGridView grid, List<T> list, SortedList<string, ColumnSetting> columnList) : 
    CryptoDataGrid<T>(grid, list, columnList) where T : CryptoPosition
{
    private enum ColumnsForGrid
    {
        Id,
        Created,
        Closed,
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
        Profit,
        Percentage,
        Parts,
        //EntryPrice,
        //ProfitPrice,
        QuantityTick,
        RemainingDust,
        DustValue,
    }

    public override void InitializeCommands(ContextMenuStrip menuStrip)
    {
        menuStrip.AddCommand(this, "Activate trading app", Command.ActivateTradingApp);
        menuStrip.AddCommand(this, "TradingView internal", Command.ActivateTradingviewIntern);
        menuStrip.AddCommand(this, "TradingView external", Command.ActivateTradingviewExtern);

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Copy symbol name", Command.CopySymbolInformation);
        menuStrip.AddCommand(this, "Trend information (log)", Command.ShowTrendInformation);
        menuStrip.AddCommand(this, "Symbol information (Excel)", Command.ExcelSymbolInformation);

#if TRADEBOT
        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Position recalculate", Command.None, CommandPositionRecalculateExecute);
        //menuStrip.AddCommand(this, "Position recalculate2", Command.PositionCalculate);
        menuStrip.AddCommand(this, "Position delete from database", Command.None, CommandPositionDeleteFromDatabase);
        menuStrip.AddCommand(this, "Position information (Excel)", Command.ExcelPositionInformation);
#endif

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Hide selection", Command.None, ClearSelection);
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
                    CreateColumn("Id", typeof(int), string.Empty, DataGridViewContentAlignment.MiddleCenter, 42);
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
                    CreateColumn("Symbol", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 100);
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
                //case ColumnsForGrid.EntryPrice:
                //    CreateColumn("Entry Price", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                //    break;
                //case ColumnsForGrid.ProfitPrice:
                //    CreateColumn("Profit Price", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                //    break;
                case ColumnsForGrid.QuantityTick:
                    CreateColumn("Q Tick", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                case ColumnsForGrid.RemainingDust:
                    CreateColumn("Dust", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                case ColumnsForGrid.DustValue:
                    CreateColumn("DustValue", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight, 75);
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
            ColumnsForGrid.Closed => ObjectCompare.Compare(a.CloseTime, b.CloseTime),
            ColumnsForGrid.Duration => ObjectCompare.Compare(a.Duration().TotalSeconds, b.Duration().TotalSeconds),
            ColumnsForGrid.Account => ObjectCompare.Compare(a.TradeAccount.Name, b.TradeAccount.Name),
            ColumnsForGrid.Exchange => ObjectCompare.Compare(a.Exchange.Name, b.Exchange.Name),
            ColumnsForGrid.Symbol => ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name),
            ColumnsForGrid.Interval => ObjectCompare.Compare(a.Interval.IntervalPeriod, b.Interval.IntervalPeriod),
            ColumnsForGrid.Strategy => ObjectCompare.Compare(a.StrategyText, b.StrategyText),
            ColumnsForGrid.Side => ObjectCompare.Compare(a.SideText, b.SideText),
            ColumnsForGrid.Status => ObjectCompare.Compare(a.Status, b.Status),
            ColumnsForGrid.Invested => ObjectCompare.Compare(a.Invested, b.Invested),
            ColumnsForGrid.Returned => ObjectCompare.Compare(a.Returned, b.Returned),
            ColumnsForGrid.Commission => ObjectCompare.Compare(a.Commission, b.Commission),
            ColumnsForGrid.Profit => ObjectCompare.Compare(a.Profit, b.Profit),
            ColumnsForGrid.Percentage => ObjectCompare.Compare(a.Percentage, b.Percentage),
            ColumnsForGrid.Parts => ObjectCompare.Compare(a.PartCount - Convert.ToInt32(a.ActiveDca), b.PartCount - Convert.ToInt32(b.ActiveDca)),
            //ColumnsForGrid.EntryPrice => ObjectCompare.Compare(a.EntryPrice, b.EntryPrice),
            //ColumnsForGrid.ProfitPrice => ObjectCompare.Compare(a.ProfitPrice, b.ProfitPrice),
            ColumnsForGrid.QuantityTick => ObjectCompare.Compare(a.Symbol.QuantityTickSize, b.Symbol.QuantityTickSize),
            ColumnsForGrid.RemainingDust => ObjectCompare.Compare(a.RemainingDust, b.RemainingDust),
            ColumnsForGrid.DustValue => ObjectCompare.Compare(a.RemainingDust * a.Symbol.LastPrice, b.RemainingDust * b.Symbol.LastPrice),
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


    public override void GetTextFunction(object sender, DataGridViewCellValueEventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        CryptoPosition position = GetCellObject(e.RowIndex);
        if (position != null)
        {
            e.Value = (ColumnsForGrid)e.ColumnIndex switch
            {
                ColumnsForGrid.Id => position.Id.ToString(),
                ColumnsForGrid.Created => position.CreateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                ColumnsForGrid.Closed => position.CloseTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                ColumnsForGrid.Duration => position.DurationText(),
                ColumnsForGrid.Account => position.TradeAccount.Name,
                ColumnsForGrid .Exchange => position.Symbol.Exchange.Name,
                ColumnsForGrid.Symbol => position.Symbol.Name,
                ColumnsForGrid.Interval => position.Interval.Name,
                ColumnsForGrid.Strategy => position.StrategyText,
                ColumnsForGrid.Side => position.SideText,
                ColumnsForGrid.Status => position.Status.ToString(),
                ColumnsForGrid.Invested => position.Invested.ToString(position.Symbol.QuoteData.DisplayFormat),
                ColumnsForGrid.Returned => position.Returned.ToString(position.Symbol.QuoteData.DisplayFormat),
                ColumnsForGrid.Commission => position.Commission.ToString(position.Symbol.QuoteData.DisplayFormat),
                ColumnsForGrid.Profit => position.Profit.ToString(position.Symbol.QuoteData.DisplayFormat),
                ColumnsForGrid.Percentage => position.Percentage.ToString("N2"),
                ColumnsForGrid.Parts => position.PartCountText(false),
                //ColumnsForGrid.EntryPrice => position.EntryPrice?.ToString(position.Symbol.PriceDisplayFormat),
                //ColumnsForGrid.ProfitPrice => position.ProfitPrice?.ToString(position.Symbol.PriceDisplayFormat),
                // ter debug..
                ColumnsForGrid.QuantityTick => position.Symbol?.QuantityTickSize.ToString0(),
                ColumnsForGrid.RemainingDust => position.RemainingDust.ToString("N8"),
                ColumnsForGrid.DustValue => (position.RemainingDust * position.Symbol.LastPrice).ToString0("N8"),
                _ => '?',
            };
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
                    Color displayColor = position.Symbol.QuoteData.DisplayColor;
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

                case ColumnsForGrid.Profit:
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

            }
        }

        DataGridViewCell cell = Grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        cell.Style.BackColor = backColor;
        cell.Style.ForeColor = foreColor;
    }


#if TRADEBOT
    private async void CommandPositionRecalculateExecute(object sender, EventArgs e)
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
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig positie herberekend");
        }

    }


    private void CommandPositionDeleteFromDatabase(object sender, EventArgs e)
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
#endif

}
