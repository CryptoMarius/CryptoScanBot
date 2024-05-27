using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using NPOI.SS.UserModel;

namespace CryptoScanBot.Core.Excel;

public class ExcelPositionDump(CryptoPosition position) : ExcelBase(position.Symbol.Name)
{
    readonly Dictionary<string, bool> OrderList = [];

    public void DumpParts()
    {
        ISheet sheet = Book.CreateSheet("Parts");

        // Er zijn 2 rijen met headers
        int row = 0;
        int column = 0;

        // Headers
        int columns = 0;
        WriteCell(sheet, columns++, row, "Id");
        WriteCell(sheet, columns++, row, "Order");
        WriteCell(sheet, columns++, row, "Side");
        WriteCell(sheet, columns++, row, "Create");
        WriteCell(sheet, columns++, row, "Close");
        WriteCell(sheet, columns++, row, "Type");
        WriteCell(sheet, columns++, row, "Status");
        WriteCell(sheet, columns++, row, "Trailing");
        WriteCell(sheet, columns++, row, "Quantity");
        WriteCell(sheet, columns++, row, "Q.Filled");
        WriteCell(sheet, columns++, row, "Price");
        WriteCell(sheet, columns++, row, "Stop");
        WriteCell(sheet, columns++, row, "Limit");
        WriteCell(sheet, columns++, row, "Value");
        WriteCell(sheet, columns++, row, "Commission");
        WriteCell(sheet, columns++, row, "Asset");
        WriteCell(sheet, columns++, row, "CommissionB");
        WriteCell(sheet, columns++, row, "CommissionQ");
        WriteCell(sheet, columns++, row, "");
        WriteCell(sheet, columns++, row, "");
        WriteCell(sheet, columns++, row, "Profit");
        WriteCell(sheet, columns++, row, "Percent");

        var partList = position.Parts.Values.ToList();
        partList.Sort((x, y) => x.PartNumber.CompareTo(y.PartNumber));
        foreach (CryptoPositionPart part in partList)
        {
            ++row;
            column = 0;

            WriteCell(sheet, column++, row, part.Id);
            string text = part.Purpose.ToString();
            if (part.Purpose == CryptoPartPurpose.Dca)
            {
                text += " " + part.PartNumber.ToString();
                if (part.ManualOrder)
                    text += " manual";
            }
            WriteCell(sheet, column++, row, text); // 0 = entry and >= 1 is dca
            WriteCell(sheet, column++, row, part.Purpose.ToString());
            WriteCell(sheet, column++, row, part.CreateTime.ToLocalTime(), CellStyleDate);
            WriteCell(sheet, column, row, part.CloseTime?.ToLocalTime(), CellStyleDate);

            //cell = WriteCell(sheet, column, row, part.Status.ToString());

            column = 8;
            WriteCell(sheet, column, row, part.Quantity, CellStyleDecimalNormal);

            column = 14;
            WriteCell(sheet, column++, row, part.Commission, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, part.CommissionBase, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, part.CommissionQuote, CellStyleDecimalNormal);

            // Past er niet echt tussen..
            column++;

            WriteCell(sheet, column++, row, part.EntryMethod == CryptoEntryOrProfitMethod.AfterNextSignal ? part.Strategy.ToString() : "Fixed");
            WriteCell(sheet, column++, row, part.Interval != null ? part.Interval.Name : position.Interval.Name);

            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                ++row;
                column = 0;

                if (!string.IsNullOrEmpty(step.OrderId))
                    OrderList.TryAdd(step.OrderId, false);

                WriteCell(sheet, column++, row, step.Id);
                WriteCell(sheet, column++, row, string.IsNullOrEmpty(step.OrderId) ? "?" : step.OrderId);
                WriteCell(sheet, column++, row, step.Side.ToString(), step.Side == CryptoOrderSide.Buy ? CellStyleStringGreen : CellStyleStringRed);
                WriteCell(sheet, column++, row, step.CreateTime.ToLocalTime(), CellStyleDate);
                WriteCell(sheet, column++, row, step.CloseTime?.ToLocalTime(), CellStyleDate);
                WriteCell(sheet, column++, row, step.OrderType.ToString());
                WriteCell(sheet, column++, row, step.Status.ToString());
                WriteCell(sheet, column++, row, step.Trailing.ToString());
                WriteCell(sheet, column++, row, step.Quantity, CellStyleDecimalNormal);
                WriteCell(sheet, column++, row, step.QuantityFilled, CellStyleDecimalNormal);

                // wat is de werkelijke prijs (stopprice of normale price)?
                // Gekozen om dit ter plekke uit te rekenen (is tevens beter met market orders die over meerdere trades gaan)
                //cell = WriteCell(sheet, column++, row, (double)step.QuoteQuantityFilled / (double)step.Quantity);
                WriteCell(sheet, column++, row, step.Price, CellStyleDecimalNormal);
                WriteCell(sheet, column++, row, step.StopPrice, CellStyleDecimalNormal);
                WriteCell(sheet, column++, row, step.Quantity * step.StopLimitPrice, CellStyleDecimalNormal);
                WriteCell(sheet, column++, row, step.QuoteQuantityFilled, CellStyleDecimalNormal);
                WriteCell(sheet, column++, row, step.Commission, CellStyleDecimalNormal);
                WriteCell(sheet, column++, row, step.CommissionAsset, CellStyleDecimalNormal);
                WriteCell(sheet, column++, row, step.CommissionBase, CellStyleDecimalNormal);
                WriteCell(sheet, column++, row, step.CommissionQuote, CellStyleDecimalNormal);
            }

            if (part.CloseTime.HasValue) // && part.Status == CryptopositionStatus.Ready
            {
                row++;
                column++;
                column++;
                WriteCell(sheet, column++, row, part.Profit, part.Profit >= 0 ? CellStyleDecimalGreen : CellStyleDecimalRed);
                WriteCell(sheet, column++, row, part.Percentage, part.Percentage >= 100 ? CellStylePercentageGreen : CellStylePercentageRed);
            }

            ++row;
            ++row;
        }

        ++row;
        ++row;

        if (position.CloseTime.HasValue && position.PartCount > 1)
        {
            row++;
            column++;
            column++;
            WriteCell(sheet, column++, row, position.Profit, position.Profit >= 0 ? CellStyleStringGreen : CellStyleStringRed);
            WriteCell(sheet, column++, row, position.Percentage, position.Percentage >= 100 ? CellStyleStringGreen : CellStyleStringRed);
        }

        column = 8;
        if (!position.CloseTime.HasValue)
        {
            WriteCell(sheet, column, row, "Break Even");
            WriteCell(sheet, column + 1, row, position.BreakEvenPrice, CellStyleDecimalNormal);
        }

        ++row;
        WriteCell(sheet, column, row, "Last Price");
        WriteCell(sheet, column + 1, row, position.Symbol.LastPrice, CellStyleDecimalNormal);


        if (position.RemainingDust != 0)
        {
            ++row;
            WriteCell(sheet, column, row, "Dust Value");
            WriteCell(sheet, column + 1, row, position.Symbol.LastPrice * position.RemainingDust, CellStyleDecimalNormal);
        }

        columns = 22;
        AutoSize(sheet, columns);
    }

    public void DumpBreakEven()
    {
        ISheet sheet = Book.CreateSheet("BE");
        int row = 0;

        int columns = 0;
        WriteCell(sheet, columns++, row, "Side");
        WriteCell(sheet, columns++, row, "Create");
        WriteCell(sheet, columns++, row, "Closed");
        WriteCell(sheet, columns++, row, "Price");
        WriteCell(sheet, columns++, row, "Quantity");
        WriteCell(sheet, columns++, row, "QuoteQuantity");
        WriteCell(sheet, columns++, row, "Commission");
        WriteCell(sheet, columns++, row, "BreakEven");
        WriteCell(sheet, columns++, row, "Percentage");

        decimal be;
        decimal firstValue = 0;
        decimal totalValue = 0;
        decimal totalQuantity = 0;
        decimal totalCommission = 0;
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                // Geannuleerde order of openstaande orders overslagen
                if (step.Status >= CryptoOrderStatus.Canceled || !step.CloseTime.HasValue)
                    continue;

                ++row;
                int column = 0;
                int factor = 1;
                if (step.Side == CryptoOrderSide.Buy)
                    factor = -1;

                WriteCell(sheet, column++, row, step.Side.ToString());
                WriteCell(sheet, column++, row, step.CreateTime.ToLocalTime(), CellStyleDate);
                WriteCell(sheet, column++, row, step.CloseTime?.ToLocalTime(), CellStyleDate);
                WriteCell(sheet, column++, row, (double)(step.QuoteQuantityFilled / step.Quantity), CellStyleDecimalNormal); // gem. prijs
                WriteCell(sheet, column++, row, (double)step.Quantity, CellStyleDecimalNormal);
                totalQuantity += factor * step.QuantityFilled;
                WriteCell(sheet, column++, row, factor * (double)step.QuoteQuantityFilled, CellStyleDecimalNormal);
                totalValue += factor * step.QuoteQuantityFilled;
                WriteCell(sheet, column++, row, (double)step.Commission, CellStyleDecimalNormal);
                totalCommission += step.Commission;

                be = 0;
                if (totalQuantity != 0)
                    be = (totalValue - totalCommission) / totalQuantity;
                WriteCell(sheet, column++, row, (double)be, CellStyleDecimalNormal);

                // Percentage
                if (firstValue == 0)
                    firstValue = be;
                decimal perc = 0;
                if (firstValue != 0)
                    perc = (100 * be / firstValue) - 100;
                WriteCell(sheet, column++, row, (double)perc, CellStyleDecimalNormal);
            }
        }
        AutoSize(sheet, columns);
    }


    public void DumpOrders()
    {
        ISheet sheet = Book.CreateSheet("Orders");
        int row = 0;

        int columns = 0;
        WriteCell(sheet, columns++, row, "Id");
        WriteCell(sheet, columns++, row, "OrderId");
        WriteCell(sheet, columns++, row, "Status");

        WriteCell(sheet, columns++, row, "Side");
        WriteCell(sheet, columns++, row, "Created");
        WriteCell(sheet, columns++, row, "Updated");

        WriteCell(sheet, columns++, row, "Price");
        WriteCell(sheet, columns++, row, "Quantity");
        WriteCell(sheet, columns++, row, "QuoteQuantity");

        WriteCell(sheet, columns++, row, "Avg Price");
        WriteCell(sheet, columns++, row, "Quantity filled");
        WriteCell(sheet, columns++, row, "QuoteQuantity filled");

        WriteCell(sheet, columns++, row, "Commission");
        WriteCell(sheet, columns++, row, "C. Asset");

        List<CryptoOrder> orderList = [.. position.Symbol.OrderList.Values];
        orderList.Sort((x, y) => x.CreateTime.CompareTo(y.CreateTime));
        foreach (CryptoOrder order in orderList)
        {
            if (!OrderList.ContainsKey(order.OrderId))
                continue;

            ++row;
            int column = 0;

            WriteCell(sheet, column++, row, order.Id);
            WriteCell(sheet, column++, row, order.OrderId);
            WriteCell(sheet, column++, row, order.Status.ToString());
            WriteCell(sheet, column++, row, order.Side.ToString());
            WriteCell(sheet, column++, row, order.CreateTime.ToLocalTime(), CellStyleDate);
            WriteCell(sheet, column++, row, order.UpdateTime.ToLocalTime(), CellStyleDate);
            WriteCell(sheet, column++, row, order.Price, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, order.Quantity, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, order.QuoteQuantity, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, order.AveragePrice, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, order.QuantityFilled, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, order.QuoteQuantityFilled, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, order.Commission, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, order.CommissionAsset, CellStyleDecimalNormal);
        }

        AutoSize(sheet, columns);
    }

    public void DumpTrades()
    {
        ISheet sheet = Book.CreateSheet("Trades");
        int row = 0;

        int columns = 0;
        WriteCell(sheet, columns++, row, "Id");
        WriteCell(sheet, columns++, row, "TradeId");
        WriteCell(sheet, columns++, row, "OrderId");
        WriteCell(sheet, columns++, row, "Time");
        WriteCell(sheet, columns++, row, "Price");
        WriteCell(sheet, columns++, row, "Quantity");
        WriteCell(sheet, columns++, row, "QuoteQuantity");
        WriteCell(sheet, columns++, row, "Commission");
        WriteCell(sheet, columns++, row, "C. Asset");

        List<CryptoTrade> tradelist = [.. position.Symbol.TradeList.Values];
        tradelist.Sort((x, y) => x.TradeTime.CompareTo(y.TradeTime));
        foreach (CryptoTrade trade in tradelist)
        {
            if (!OrderList.ContainsKey(trade.OrderId))
                continue;

            ++row;
            int column = 0;

            WriteCell(sheet, column++, row, trade.Id);
            WriteCell(sheet, column++, row, trade.TradeId);
            WriteCell(sheet, column++, row, trade.OrderId);
            WriteCell(sheet, column++, row, trade.TradeTime.ToLocalTime(), CellStyleDate);
            WriteCell(sheet, column++, row, trade.Price, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, trade.Quantity, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, (double)trade.QuoteQuantity, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, (double)trade.Commission, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, trade.CommissionAsset, CellStyleDecimalNormal);
        }

        AutoSize(sheet, columns);
    }

    private void DumpInformation()
    {
        ISheet sheet = Book.CreateSheet("Information");

        int row = 0;
        int column = 0;
        WriteCell(sheet, row, column++, "Positie ID");
        WriteCell(sheet, row, column++, "Exchange");
        WriteCell(sheet, row, column++, "Account");
        WriteCell(sheet, row, column++, "Symbol");
        WriteCell(sheet, row, column++, "Symbol Id");
        WriteCell(sheet, row, column++, "Price ticksize");
        WriteCell(sheet, row, column++, "Quantity ticksize");
        WriteCell(sheet, row, column++, "Direction");
        WriteCell(sheet, row, column++, "Geinvesteeerd");
        WriteCell(sheet, row, column++, "Geretourneerd");
        WriteCell(sheet, row, column++, position.Status == CryptoPositionStatus.Ready ? "Winst/Verlies" : "Nu geinvesteerd");
        WriteCell(sheet, row, column++, "Totale commissie");
        WriteCell(sheet, row, column++, "Markt waarde");
        WriteCell(sheet, row, column++, "Markt percentage");
        WriteCell(sheet, row, column++, "Geopend");
        WriteCell(sheet, row, column++, "Gesloten");
        WriteCell(sheet, row, column++, "Status");
        WriteCell(sheet, row, column++, "DCA count");

        row++;
        column = 0;
        WriteCell(sheet, row, column++, position.Id);
        WriteCell(sheet, row, column++, position.Exchange.Name);
        WriteCell(sheet, row, column++, position.TradeAccount.Name);
        WriteCell(sheet, row, column++, position.Symbol.Name);
        WriteCell(sheet, row, column++, position.Symbol.Id);
        WriteCell(sheet, row, column++, position.Symbol.PriceTickSize, CellStyleDecimalNormal);
        WriteCell(sheet, row, column++, position.Symbol.QuantityTickSize, CellStyleDecimalNormal);
        WriteCell(sheet, row, column++, position.SideText);
        WriteCell(sheet, row, column++, position.Invested, CellStyleDecimalNormal);
        WriteCell(sheet, row, column++, position.Returned, CellStyleDecimalNormal);

        decimal investedInTrades = position.Invested - position.Returned - position.Commission;
        WriteCell(sheet, row, column++, investedInTrades, CellStyleDecimalNormal);
        WriteCell(sheet, row, column++, position.Commission, CellStyleDecimalNormal);
        WriteCell(sheet, row, column++, position.CurrentProfit(), CellStyleDecimalNormal);

        WriteCell(sheet, row, column++, (double)position.CurrentProfitPercentage(), CellStyleDecimalNormal);
        WriteCell(sheet, row, column++, position.CreateTime.ToLocalTime(), CellStyleDate);
        WriteCell(sheet, row, column++, position.CloseTime?.ToLocalTime(), CellStyleDate);
        WriteCell(sheet, row, column++, position.Status.ToString() ?? string.Empty);
        WriteCell(sheet, row, column++, position.PartCount);

        AutoSize(sheet, 6);
    }

    //private void DumpSignals()
    //{
    //    HSSFSheet sheet = (HSSFSheet)Book.CreateSheet("Signals");

    //    int row = 0;

    //    // Headers
    //    int columns = 0;

    //    WriteCell(sheet, columns++, row, "Id");
    //    WriteCell(sheet, columns++, row, "Order");
    //    WriteCell(sheet, columns++, row, "Side");
    //    WriteCell(sheet, columns++, row, "Create");
    //    WriteCell(sheet, columns++, row, "Close");
    //    WriteCell(sheet, columns++, row, "Type");
    //    WriteCell(sheet, columns++, row, "Status");
    //    WriteCell(sheet, columns++, row, "Trailing");
    //    WriteCell(sheet, columns++, row, "Quantity");
    //    WriteCell(sheet, columns++, row, "Price");
    //    WriteCell(sheet, columns++, row, "StopLimit");
    //    WriteCell(sheet, columns++, row, "Value");
    //    WriteCell(sheet, columns++, row, "Commission");


    //    row++;
    //    WriteCell(sheet, 0, row, "TODO");

    //    //        // De signalen laden
    //    //        string sql = "select * from signal order by id desc limit 50";
    //    //        //sql = string.Format("select * from signal where exchangeid={0} order by id desc limit 50", exchange.Id);
    //    //        using var database = new CryptoDatabase();

    //    //        foreach (Cryptoposition position in databaseThread.Connection.Query<Cryptoposition>("select * from position " +
    //    //            "where CreateTime >= @fromDate and status=2", new { fromDate = DateTime.Today }))

    //    //            foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql))
    //    //        {
    //    //            ?

    //    //            if (GlobalData.ExchangeListId.TryGetValue(signal.ExchangeId, out Model.CryptoExchange exchange2))
    //    //            {
    //    //                signal.Exchange = exchange2;

    //    //                if (exchange2.SymbolListId.TryGetValue(signal.SymbolId, out CryptoSymbol symbol))
    //    //                {
    //    //                    signal.Symbol = symbol;

    //    //                    if (GlobalData.IntervalListId.TryGetValue((int)signal.IntervalId, out CryptoInterval interval))
    //    //                        signal.Interval = interval;

    //    //                    GlobalData.SignalQueue.Enqueue(signal);
    //    //                }
    //    //            }
    //    //        }
    //}

    public void ExportToExcel()
    {
        try
        {
            DumpParts();
            DumpBreakEven();
            DumpOrders();
            DumpTrades();
            //DumpSignals();
            DumpInformation();

            StartExcell("position", position.Symbol.Name);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR postion dump " + error.ToString());
        }
    }
}