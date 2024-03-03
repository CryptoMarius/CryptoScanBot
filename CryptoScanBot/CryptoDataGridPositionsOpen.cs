using CryptoScanBot.Commands;
using CryptoScanBot.Context;
using CryptoScanBot.Enums;
using CryptoScanBot.Exchange;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using CryptoScanBot.Settings;
using CryptoScanBot.Trader;

using Dapper;
using Dapper.Contrib.Extensions;

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
    }

    private System.Windows.Forms.Timer TimerRefreshSomething;


    private void InitializeTimers()
    {
        TimerRefreshSomething = new()
        {
            Interval = 15 * 1000
        };
        TimerRefreshSomething.Tick += TimerRefreshSomething_Tick;
        TimerRefreshSomething.Enabled = true;
    }

    public override void InitializeCommands(ContextMenuStrip menuStrip)
    {
        AddStandardSymbolCommands(menuStrip, false);

        menuStrip.Items.Add(new ToolStripSeparator());

        ToolStripMenuItemCommand menuCommand;
        menuCommand = new();
        menuCommand.DataGrid = this;
        menuCommand.Text = "Positie herberekenen";
        menuCommand.Click += CommandPositionsOpenRecalculateExecute;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new();
        menuCommand.DataGrid = this;
        menuCommand.Text = "Positie verwijderen uit database";
        menuCommand.Click += CommandPositionsOpenDeleteFromDatabaseAsync;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new();
        menuCommand.DataGrid = this;
        menuCommand.Text = "Extra DCA toevoegen aan de positie";
        menuCommand.Click += CommandPositionsOpenCreateAdditionalDca;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new();
        menuCommand.DataGrid = this;
        menuCommand.Text = "Openstaande DCA van positie annuleren";
        menuCommand.Click += CommandPositionsOpenRemoveAdditionalDca;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new();
        menuCommand.DataGrid = this;
        menuCommand.Text = "Positie profit nemen (indien mogelijk)";
        menuCommand.Click += CommandPositionsOpenLastPartTakeProfit;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new();
        menuCommand.DataGrid = this;
        menuCommand.Text = "Positie informatie (Excel)";
        menuCommand.Command = Command.ExcelPositionInformation;
        menuCommand.Click += CommandTools.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);
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
                    e.Value = position.TradeAccount.Name;
                    break;
                case ColumnsForGrid.Exchange:
                    e.Value = position.Symbol.Exchange.Name;
                    break;
                case ColumnsForGrid.Symbol:
                    e.Value = position.Symbol.Name;
                    break;
                case ColumnsForGrid.Interval:
                    e.Value = position.Interval.Name;
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
                    e.Value = position.Invested.ToString(position.Symbol.QuoteData.DisplayFormat);
                    break;
                case ColumnsForGrid.Returned:
                    e.Value = position.Returned.ToString(position.Symbol.QuoteData.DisplayFormat);
                    break;
                case ColumnsForGrid.Commission:
                    e.Value = position.Commission.ToString(position.Symbol.QuoteData.DisplayFormat);
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
                    e.Value = (position.Invested - position.Returned).ToString(position.Symbol.QuoteData.DisplayFormat);
                    break;
                case ColumnsForGrid.Profit:
                    e.Value = position.CurrentProfit().ToString(position.Symbol.QuoteData.DisplayFormat);
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
                    e.Value = position.Symbol?.QuantityTickSize.ToString(position.Symbol.PriceDisplayFormat);
                    break;
                case ColumnsForGrid.RemainingDust: // ter debug
                    e.Value = position.RemainingDust.ToString("N8");
                    break;
                case ColumnsForGrid.DustValue: // ter debug
                    decimal dustValue = position.RemainingDust * position.Symbol.LastPrice.Value;
                    e.Value = dustValue.ToString("N2");
                    break;
                default:
                    e.Value = '?';
                    break;
            }
        }
    }


    public override void CellFormattingEvent(object sender, DataGridViewCellFormattingEventArgs e)
    {
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

                    switch (position.Strategy)
                    {
                        case CryptoSignalStrategy.Jump:
                            if (GlobalData.Settings.Signal.Jump.ColorLong != Color.White && position.Side == CryptoTradeSide.Long)
                                backColor = GlobalData.Settings.Signal.Jump.ColorLong;
                            else if (GlobalData.Settings.Signal.Jump.ColorShort != Color.White && position.Side == CryptoTradeSide.Short)
                                backColor = GlobalData.Settings.Signal.Jump.ColorShort;
                            break;

                        case CryptoSignalStrategy.Stobb:
                            if (GlobalData.Settings.Signal.Stobb.ColorLong != Color.White && position.Side == CryptoTradeSide.Long)
                                backColor = GlobalData.Settings.Signal.Stobb.ColorLong;
                            else if (GlobalData.Settings.Signal.Stobb.ColorShort != Color.White && position.Side == CryptoTradeSide.Short)
                                backColor = GlobalData.Settings.Signal.Stobb.ColorShort;
                            break;

                        case CryptoSignalStrategy.Sbm1:
                        case CryptoSignalStrategy.Sbm2:
                        case CryptoSignalStrategy.Sbm3:
                        case CryptoSignalStrategy.Sbm4:
                            if (GlobalData.Settings.Signal.Sbm.ColorLong != Color.White && position.Side == CryptoTradeSide.Long)
                                backColor = GlobalData.Settings.Signal.Sbm.ColorLong;
                            else if (GlobalData.Settings.Signal.Sbm.ColorShort != Color.White && position.Side == CryptoTradeSide.Short)
                                backColor = GlobalData.Settings.Signal.Sbm.ColorShort;
                            break;
                    }
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
                    if (position.Invested - position.Returned > 7 * position.Symbol.QuoteData.EntryAmount) // just an indication how deep we are in a trade
                        foreColor = Color.Red;
                    break;

                case ColumnsForGrid.Profit: // Current profit
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
            }
        }

        DataGridViewCell cell = Grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        cell.Style.BackColor = backColor;
        cell.Style.ForeColor = foreColor;
    }

    private async void CommandPositionsOpenRecalculateExecute(object sender, EventArgs e)
    {
        CryptoPosition position = GetSelectedObject(out int rowIndex);
        if (position != null)
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await TradeTools.LoadOrdersFromDatabaseAndExchangeAsync(databaseThread, position);
            await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position, forceCalculation: true);

            Grid.InvalidateRow(rowIndex);
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig positie {position.Id} herberekend");
        }

    }


    private async void CommandPositionsOpenDeleteFromDatabaseAsync(object sender, EventArgs e)
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

                // Controleer de orders, en herbereken het geheel
                PositionTools.LoadPosition(databaseThread, position);
                await TradeTools.LoadOrdersFromDatabaseAndExchangeAsync(databaseThread, position);
                await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position);


                using var transaction = databaseThread.BeginTransaction();
                databaseThread.Connection.Execute($"delete from positionstep where positionid={position.Id}", transaction);
                databaseThread.Connection.Execute($"delete from positionpart where positionid={position.Id}", transaction);
                databaseThread.Connection.Execute($"delete from position where id={position.Id}", transaction);
                transaction.Commit();

                List.Remove((T)position);
                PositionTools.RemovePosition(position.TradeAccount, position, false);
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


    private async void CommandPositionsOpenCreateAdditionalDca(object sender, EventArgs e)
    {
        CryptoPosition position = GetSelectedObject(out int rowIndex);
        if (position != null)
        {

            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await TradeTools.LoadOrdersFromDatabaseAndExchangeAsync(databaseThread, position);
            await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position);
            //FillItemOpen(position, item);

            // Op welke prijs? Actueel, of nog X% eronder?
            //TradeTools.
            // todo...
            //TradeTools.CalculatePositionResultsViaTrades(databaseThread, position);
            //FillItemOpen(position, item);

            //decimal adjust = GlobalData.Settings.Trading.DcaPercentage * step.Price / 100m;

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
            PositionTools.ExtendPosition(databaseThread, position, CryptoPartPurpose.Dca, position.Interval, position.Strategy,
                CryptoEntryOrProfitMethod.FixedPercentage, price, DateTime.UtcNow, true);
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig een DCA toegevoegd aan positie {position.Id}");

            //FillItemOpen(position, item);
            Grid.InvalidateRow(rowIndex);


            // Er is een 1m candle gearriveerd, acties adhv deze candle..
            var symbolPeriod = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
            if (symbolPeriod.CandleList.Count > 0)
            {
                var lastCandle1m = symbolPeriod.CandleList.Values.Last();
                PositionMonitor positionMonitor = new(position.Symbol, lastCandle1m);
                await positionMonitor.HandlePosition(position);
            }
        }

    }


    private async void CommandPositionsOpenRemoveAdditionalDca(object sender, EventArgs e)
    {
        CryptoPosition position = GetSelectedObject(out int rowIndex);
        if (position != null)
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await TradeTools.LoadOrdersFromDatabaseAndExchangeAsync(databaseThread, position);
            await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position);

            // Er is een 1m candle gearriveerd, acties adhv deze candle..
            var symbolPeriod = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
            if (symbolPeriod.CandleList.Count > 0)
            {
                var lastCandle1m = symbolPeriod.CandleList.Values.Last();
                long lastCandle1mCloseTime = lastCandle1m.OpenTime + 60;
                DateTime lastCandle1mCloseTimeDate = CandleTools.GetUnixDate(lastCandle1mCloseTime);

                PositionMonitor positionMonitor = new(position.Symbol, lastCandle1m);
                await positionMonitor.HandlePosition(position);


                var entryOrderSide = position.GetEntryOrderSide();
                foreach (CryptoPositionPart part in position.Parts.Values.ToList())
                {
                    if (!part.CloseTime.HasValue && part.Purpose == CryptoPartPurpose.Dca)
                    {
                        foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                        {
                            if (!step.CloseTime.HasValue && step.Side == entryOrderSide)
                            {
                                var (cancelled, tradeParams) = await TradeTools.CancelOrder(databaseThread, position, part, step, lastCandle1mCloseTimeDate, CryptoOrderStatus.TrailingChange);
                                if (!cancelled || GlobalData.Settings.Trading.LogCanceledOrders)
                                    ExchangeBase.Dump(position, cancelled, tradeParams, $"annuleren vanwege annuleren van DCA positie {position.Id}");
                                else
                                {
                                    step.CloseTime = DateTime.UtcNow;
                                    step.Status = CryptoOrderStatus.ManuallyByUser;
                                    databaseThread.Connection.Update<CryptoPositionStep>(step);

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


    private async void CommandPositionsOpenLastPartTakeProfit(object sender, EventArgs e)
    {

        CryptoPosition position = GetSelectedObject(out int rowIndex);
        if (position != null)
        {
            if (!position.Symbol.LastPrice.HasValue)
                return;

            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await TradeTools.LoadOrdersFromDatabaseAndExchangeAsync(databaseThread, position);
            await TradeTools.CalculatePositionResultsViaOrders(databaseThread, position);
            Grid.InvalidateRow(rowIndex);


            // Itereer de openstaande parts
            foreach (CryptoPositionPart part in position.Parts.Values.ToList())
            {
                // voor de niet afgesloten parts...
                if (!part.CloseTime.HasValue && part.Quantity > 0)
                {
                    CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();

                    // Is er een entry order in zijn geheel gevuld (lastig indien er meerdere entries komen)
                    CryptoPositionStep step = PositionTools.FindPositionPartStep(part, entryOrderSide, true);
                    if (step != null && step.Status == CryptoOrderStatus.Filled)
                    {

                        // Is de entry prijs wel hoger/lager dan de actuele prijs?
                        //if (position.Side == CryptoTradeSide.Long && step.Price < part.BreakEvenPrice + 5 * position.Symbol.PriceTickSize)
                        //    continue;
                        //else if (position.Side == CryptoTradeSide.Short && step.Price > part.BreakEvenPrice - 5 * position.Symbol.PriceTickSize)
                        //    continue;

                        // Is de laatste prijs wel hoger/lager dan de actuele prijs?
                        if (position.Side == CryptoTradeSide.Long && position.Symbol.LastPrice.Value < part.BreakEvenPrice + 25 * position.Symbol.PriceTickSize)
                            continue;
                        else if (position.Side == CryptoTradeSide.Short && position.Symbol.LastPrice.Value > part.BreakEvenPrice - 25 * position.Symbol.PriceTickSize)
                            continue;

                        // Is er een openstaande TP die niet gevuld is?
                        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
                        step = PositionTools.FindPositionPartStep(part, takeProfitOrderSide, false);
                        if (step != null && part.Quantity > 0)
                        {
                            decimal price = (decimal)position.Symbol.LastPrice;
                            // controle is waarschijnlijk overbodig, de extra tick is prima
                            // (tenzij het een market order moet zijn? Maar dan moet er meer aangepast worden)
                            if (position.Side == CryptoTradeSide.Long)
                                price += position.Symbol.PriceTickSize;
                            else
                                price -= position.Symbol.PriceTickSize;

                            var (cancelled, cancelParams) = await TradeTools.CancelOrder(databaseThread, position, part, step, DateTime.UtcNow, CryptoOrderStatus.ManuallyByUser);
                            if (!cancelled || GlobalData.Settings.Trading.LogCanceledOrders)
                                ExchangeBase.Dump(position, cancelled, cancelParams, $"positie {position.Id} annuleren vanwege handmatige TP");

                            if (cancelled)
                            {
                                price = price.Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);
                                await TradeTools.PlaceTakeProfitOrderAtPrice(databaseThread, position, part, price, DateTime.UtcNow, "manually placing");
                                Grid.InvalidateRow(rowIndex);
                                GlobalData.AddTextToLogTab($"{position.Symbol.Name} positie {position.Id} part={part.PartNumber} handmatig profit genomen");

                                break;
                            }

                        }
                    }

                }
            }
        }
    }


    private void TimerRefreshSomething_Tick(object sender, EventArgs e)
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

}
