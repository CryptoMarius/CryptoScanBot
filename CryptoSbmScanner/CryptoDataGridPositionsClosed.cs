using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Trader;

using Dapper;

namespace CryptoSbmScanner;

public class CryptoDataGridPositionsClosed<T>(DataGridView grid, List<T> list) : CryptoDataGrid<T>(grid, list) where T : CryptoPosition
{
    public override void InitializeCommands(ContextMenuStrip menuStrip)
    {
        AddStandardSymbolCommands(menuStrip, false);

        menuStrip.Items.Add(new ToolStripSeparator());

        ToolStripMenuItem menuCommand;
        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Positie herberekenen";
        menuCommand.Click += CommandPositionsClosedRecalculateExecute;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Positie verwijderen uit database";
        menuCommand.Click += CommandPositionsClosedDeleteFromDatabase;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Positie informatie (Excel)";
        menuCommand.Tag = Command.ExcelPositionInformation;
        menuCommand.Click += Commands.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);


    }

    public override void InitializeHeaders()
    {
        SortColumn = 2;
        SortOrder = SortOrder.Descending;

        CreateColumn("Id", typeof(int), string.Empty, DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Datum", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Closed", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);

        CreateColumn("Duration", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Account", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Exchange", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Symbol", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Interval", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Strategie", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Mode", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Status", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);

        CreateColumn("Invested", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Returned", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Commission", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);

        CreateColumn("Profit", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Percentage", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Parts", typeof(int), string.Empty, DataGridViewContentAlignment.MiddleRight);
        CreateColumn("EntryPrice", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("SellPrice", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
    }


    private int Compare(CryptoPosition a, CryptoPosition b)
    {
        int compareResult = SortColumn switch
        {
            00 => ObjectCompare.Compare(a.Id, b.Id),
            01 => ObjectCompare.Compare(a.CreateTime, b.CreateTime),
            02 => ObjectCompare.Compare(a.CloseTime, b.CloseTime),
            03 => ObjectCompare.Compare(a.Duration().TotalSeconds, b.Duration().TotalSeconds),
            04 => ObjectCompare.Compare(a.TradeAccount.Name, b.TradeAccount.Name),
            05 => ObjectCompare.Compare(a.Exchange.Name, b.Exchange.Name),
            06 => ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name),
            07 => ObjectCompare.Compare(a.Interval.IntervalPeriod, b.Interval.IntervalPeriod),
            08 => ObjectCompare.Compare(a.StrategyText, b.StrategyText),
            09 => ObjectCompare.Compare(a.SideText, b.SideText),
            10 => ObjectCompare.Compare(a.Status, b.Status),

            11 => ObjectCompare.Compare(a.Invested, b.Invested),
            12 => ObjectCompare.Compare(a.Returned, b.Returned),
            13 => ObjectCompare.Compare(a.Commission, b.Commission),

            14 => ObjectCompare.Compare(a.Profit, b.Profit),
            15 => ObjectCompare.Compare(a.Percentage, b.Percentage),
            16 => ObjectCompare.Compare(a.PartCount, b.PartCount),
            17 => ObjectCompare.Compare(a.EntryPrice, b.EntryPrice),
            18 => ObjectCompare.Compare(a.ProfitPrice, b.ProfitPrice),

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
            e.Value = e.ColumnIndex switch
            {
                0 => position.Id.ToString(),
                1 => position.CreateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                2 => position.CloseTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                3 => position.DurationText(),
                4 => position.TradeAccount.Name,
                5 => position.Symbol.Exchange.Name,
                6 => position.Symbol.Name,
                7 => position.Interval.Name,
                8 => position.StrategyText,
                9 => position.SideText,
                10 => position.Status.ToString(),

                11 => position.Invested.ToString(position.Symbol.QuoteData.DisplayFormat),
                12 => position.Returned.ToString(position.Symbol.QuoteData.DisplayFormat),
                13 => position.Commission.ToString(position.Symbol.QuoteData.DisplayFormat),

                14 => position.Profit.ToString(position.Symbol.QuoteData.DisplayFormat),
                15 => position.Percentage.ToString("N2"),
                16 => position.PartCountText(),
                17 => position.EntryPrice?.ToString(position.Symbol.PriceDisplayFormat),
                18 => position.ProfitPrice?.ToString(position.Symbol.PriceDisplayFormat),
                _ => '?',
            };
        }
    }


    public override void CellFormattingEvent(object sender, DataGridViewCellFormattingEventArgs e)
    {
    }

    private async void CommandPositionsClosedRecalculateExecute(object sender, EventArgs e)
    {
        CryptoPosition position = GetSelectedObject(out int rowIndex);
        if (position != null)
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await TradeTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
            TradeTools.CalculatePositionResultsViaTrades(databaseThread, position, saveChangesAnywhay: true);

            Grid.InvalidateRow(rowIndex);
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig positie herberekend");
        }

    }


    private void CommandPositionsClosedDeleteFromDatabase(object sender, EventArgs e)
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
                //await TradeTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
                //TradeTools.CalculatePositionResultsViaTrades(databaseThread, position, saveChangesAnywhay: true);
                //FillItemClosed(position, item);


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
