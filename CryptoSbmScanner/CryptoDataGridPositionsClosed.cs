using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner;

public class CryptoDataGridPositionsClosed<T>(DataGridView grid, List<T> list) : CryptoDataGrid<T>(grid, list) where T : CryptoPosition
{
    public override void InitializeHeaders()
    {
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
        // TODO: Een paar kleine verschillen tov de Main.Positions.Open (vanwege de switch notatie)
        //CryptoPosition position = ListBla[e.RowIndex];
        //switch (e.ColumnIndex)
        //{
        //    case 0 :
        //        e.Value = position.Id.ToString();
        //        break;
        //    case 1:
        //        if (position.EntryAmount > 0)
        //        e.Value = position.EntryAmount.ToString();
        //        else
        //            e.Value = position.ProfitPrice.ToString();

        //        break;
        //};

        CryptoPosition position = List[e.RowIndex];
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

    public override void RowSetDefaultColor(object sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex % 2 == 0)
        {
            Grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = VeryLightGray;
        }
    }


    public override void CellFormattingEvent(object sender, DataGridViewCellFormattingEventArgs e)
    {
        // done by column so it happens once per row
        //if (e.ColumnIndex == Grid.Columns["Interval"].Index)
        //{
        //    //Grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Red;
        //    Grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Style = styleRed;
        //}
    }


}
