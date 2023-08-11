using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace CryptoSbmScanner.Excel;

/*
    static public void DumpPosition(CryptoPosition position, StringBuilder strings)
    {
        // Het is op niet echt super-leesbaar, Excel ding maken wellicht?
        // Zie BackTestExcel.cs, daar wordt een mooi rapportje gemaakt!!
        // Het kan regel georienteerd onder elkaar en de kolommen komen overeen lijkt me.
        // Nog eens een voorbeeld excel ding van maken, kan volgens mij erg mooi zijn.
        // (en in ieder geval begrijpbaarder en overzichtelijker dan onderstaande <g>)

        strings.AppendLine("");
        strings.AppendLine("-------------------");
        strings.AppendLine("Position dump:");
        strings.AppendLine("");
        strings.AppendLine("Position Id:" + position.Id.ToString());
        strings.AppendLine("Account:" + position.TradeAccount.Name);
        strings.AppendLine("Exchange:" + position.Symbol.Exchange.Name);
        strings.AppendLine("Name:" + position.Symbol.Name);
        strings.AppendLine("Strategie:" + position.StrategyText);
        strings.AppendLine("Interval:" + position.Interval.Name);
        strings.AppendLine("Status:" + position.Status.ToString());
        strings.AppendLine("OpenDate:" + position.CreateTime.ToLocalTime());
        if (position.CloseTime.HasValue)
            strings.AppendLine("CloseDate:" + position.CloseTime?.ToLocalTime());
        strings.AppendLine("BreakEvenPrice:" + position.BreakEvenPrice.ToString());

        strings.AppendLine("Invested:" + position.Invested.ToString(position.Symbol.QuoteData.DisplayFormat));
        strings.AppendLine("Commission:" + position.Commission.ToString(position.Symbol.QuoteData.DisplayFormat));
        strings.AppendLine("Returned:" + position.Returned.ToString(position.Symbol.QuoteData.DisplayFormat));
        strings.AppendLine("Profit:" + position.Profit.ToString(position.Symbol.QuoteData.DisplayFormat));
        strings.AppendLine("Percentage:" + position.Percentage.ToString("N2"));

        // debug
        strings.AppendLine("Quantity:" + position.Quantity.ToString());
        strings.AppendLine("BuyPrice:" + position.BuyPrice.ToString());
        strings.AppendLine("BuyAmount:" + position.BuyAmount.ToString());
        strings.AppendLine("SellPrice:" + position.SellPrice.ToString());

        strings.AppendLine("");
        strings.AppendLine("-------------------");
        strings.AppendLine("Parts");

        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            // TODO - informatie van de Part
            strings.AppendLine("  Part dump:");
            strings.AppendLine("");
            strings.AppendLine("  Part Id:" + part.Id.ToString());
            strings.AppendLine("  Name:" + part.Name);
            strings.AppendLine("  Status:" + part.Status.ToString());
            strings.AppendLine("  OpenDate:" + part.CreateTime.ToLocalTime());
            strings.AppendLine("  CloseDate:" + part.CloseTime?.ToLocalTime());
            strings.AppendLine("  BreakEvenPrice:" + part.BreakEvenPrice.ToString());

            strings.AppendLine("  Invested:" + part.Invested.ToString(position.Symbol.QuoteData.DisplayFormat));
            strings.AppendLine("  Commission:" + part.Commission.ToString(position.Symbol.QuoteData.DisplayFormat));
            strings.AppendLine("  Returned:" + part.Returned.ToString(position.Symbol.QuoteData.DisplayFormat));
            strings.AppendLine("  Profit:" + part.Profit.ToString(position.Symbol.QuoteData.DisplayFormat));
            strings.AppendLine("  Percentage:" + part.Percentage.ToString("N2"));

            // debug
            strings.AppendLine("  Quantity:" + part.Quantity.ToString());
            strings.AppendLine("  (signal) BuyPrice:" + part.BuyPrice.ToString()); // van het signaal indien instappen via signaal, kan afwijken
            //strings.AppendLine("  BuyAmount:" + part.BuyAmount.ToString());
            //strings.AppendLine("  SellPrice:" + part.SellPrice.ToString());

            strings.AppendLine("  -------------------");
            strings.AppendLine("  Steps");
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                string s = string.Format("    step#{0} {1} {2} order#{3} {4} ({5}) Price={6} StopPrice={7} StopLimitPrice={8} Quantity={9} QuantityFilled={10} QuoteQuantityFilled={11} close={12} {13}",
                    step.Id, step.Name, step.CreateTime.ToLocalTime(), step.OrderId, step.Side, step.OrderType,
                    step.Price.ToString(position.Symbol.PriceDisplayFormat), step.StopPrice?.ToString(position.Symbol.PriceDisplayFormat), step.StopLimitPrice?.ToString(position.Symbol.PriceDisplayFormat),
                    step.Quantity, step.QuantityFilled, step.QuoteQuantityFilled, step.CloseTime?.ToLocalTime(), step.Status.ToString());

                //if (Trailing > CryptoTrailing.TrailNone)
                //    s += string.Format(" Trailing={0} @={1}", Trailing, TrailActivatePrice?.ToString(format));
                strings.AppendLine(s);
            }
        }

        strings.AppendLine("");
        strings.AppendLine("-------------------");
        strings.AppendLine("Trades");
        foreach (CryptoTrade trade in position.Symbol.TradeList.Values.ToList())
        {
            strings.AppendLine("");
            strings.AppendLine("Side:" + trade.Side);
            strings.AppendLine("Id:" + trade.Id.ToString());
            strings.AppendLine("TradeId:" + trade.TradeId.ToString());
            strings.AppendLine("OrderId:" + trade.OrderId.ToString());
            strings.AppendLine("OpenDate:" + trade.TradeTime.ToLocalTime());

            strings.AppendLine("Price:" + trade.Price.ToString(position.Symbol.PriceDisplayFormat));
            strings.AppendLine("Quantity:" + trade.Quantity.ToString(position.Symbol.QuantityDisplayFormat));
            strings.AppendLine("QuoteQuantity:" + trade.QuoteQuantity.ToString(position.Symbol.QuantityDisplayFormat));

            strings.AppendLine("Commission:" + trade.Commission.ToString(position.Symbol.QuantityDisplayFormat));
            strings.AppendLine("CommissionAsset:" + trade.CommissionAsset);
        }
    }
*/

public class ExcelPositionDump : ExcelBase
{
    CryptoPosition Position;
    Dictionary<long, bool> OrderList = new();

    public void DumpParts()
    {
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet("Parts");

        // Er zijn 2 rijen met headers
        int row = 0;
        //WriteCell(sheet, 00, row, "BUY");
        //WriteCell(sheet, 11, row, "SELL");
        //row++;

        // Headers
        int columns = 0;
        {
            WriteCell(sheet, columns++, row, "Id");
            WriteCell(sheet, columns++, row, "Order");
            WriteCell(sheet, columns++, row, "Side");
            WriteCell(sheet, columns++, row, "Create");
            WriteCell(sheet, columns++, row, "Close");
            WriteCell(sheet, columns++, row, "Type");
            WriteCell(sheet, columns++, row, "Status");
            WriteCell(sheet, columns++, row, "Trailing");
            WriteCell(sheet, columns++, row, "Quantity");
            WriteCell(sheet, columns++, row, "Price");
            WriteCell(sheet, columns++, row, "StopLimit");
            WriteCell(sheet, columns++, row, "Value");
            WriteCell(sheet, columns++, row, "Commission");

            columns++;

            WriteCell(sheet, columns++, row, "Id");
            WriteCell(sheet, columns++, row, "Order");
            WriteCell(sheet, columns++, row, "Side");
            WriteCell(sheet, columns++, row, "Create");
            WriteCell(sheet, columns++, row, "Close");
            WriteCell(sheet, columns++, row, "Type");
            WriteCell(sheet, columns++, row, "Status");
            WriteCell(sheet, columns++, row, "Trailing");
            WriteCell(sheet, columns++, row, "Quantity");
            WriteCell(sheet, columns++, row, "Price");
            WriteCell(sheet, columns++, row, "StopLimit");
            WriteCell(sheet, columns++, row, "Value");
            WriteCell(sheet, columns++, row, "Commission");
        }

        ICell cell;
        foreach (CryptoPositionPart part in Position.Parts.Values.ToList())
        {
            ++row;
            {
                cell = WriteCell(sheet, 0, row, part.Id);
                cell = WriteCell(sheet, 1, row, part.Name);
                cell = WriteCell(sheet, 2, row, part.Side.ToString());
                cell = WriteCell(sheet, 3, row, (DateTime)part.CreateTime.ToLocalTime());
                cell.CellStyle = CellStyleDate;

                if (part.CloseTime.HasValue)
                {
                    cell = WriteCell(sheet, 4, row, (DateTime)part.CloseTime?.ToLocalTime());
                    cell.CellStyle = CellStyleDate;
                }

                cell = WriteCell(sheet, 6, row, part.Status.ToString());

                cell = WriteCell(sheet, 8, row, (double)part.Quantity);
                cell.CellStyle = CellStyleDecimalNormal;

                cell = WriteCell(sheet, 12, row, (double)part.Commission);
                cell.CellStyle = CellStyleDecimalNormal;
            }

            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                ++row;
                // Geannuleerde order of openstaande orders overslagen
                //if (step.Status == CryptoOrderStatus.Expired || step.Status == CryptoOrderStatus.Canceled || !step.CloseTime.HasValue)
                //    continue;

                int column;
                if (step.Side == CryptoOrderSide.Buy)
                {
                    //buyRow++;
                    column = 0;
                    //row = buyRow;
                }
                else
                {
                    //sellRow++;
                    column = 14;
                    //row = sellRow;
                }

                OrderList.TryAdd((long)step.OrderId, false);

                cell = WriteCell(sheet, column++, row, step.Id);
                cell = WriteCell(sheet, column++, row, (long)step.OrderId);
                cell = WriteCell(sheet, column++, row, step.Side.ToString());
                //cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, step.CreateTime.ToLocalTime());
                cell.CellStyle = CellStyleDate;

                if (step.CloseTime.HasValue)
                {
                    cell = WriteCell(sheet, column++, row, (DateTime)step.CloseTime?.ToLocalTime());
                    cell.CellStyle = CellStyleDate;
                }
                else column++;

                cell = WriteCell(sheet, column++, row, step.OrderType.ToString());
                //cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, step.Status.ToString());
                //cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, step.Trailing.ToString());
                //cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)step.Quantity);
                cell.CellStyle = CellStyleDecimalNormal;

                // wat is de werkelijke prijs (stopprice of normale price)?
                // Gekozen om dit ter plekke uit te rekenen (is tevens beter met market orders die over meerdere trades gaan)
                //cell = WriteCell(sheet, column++, row, (double)step.QuoteQuantityFilled / (double)step.Quantity);
                cell = WriteCell(sheet, column++, row, (double)step.Price);
                cell.CellStyle = CellStyleDecimalNormal;

                if (step.StopPrice.HasValue)
                {
                    cell = WriteCell(sheet, column++, row, (double)step.StopPrice);
                    cell.CellStyle = CellStyleDecimalNormal;
                }
                else column++;

                cell = WriteCell(sheet, column++, row, (double)step.QuoteQuantityFilled);
                cell.CellStyle = CellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)step.Commission);
                cell.CellStyle = CellStyleDecimalNormal;
            }
            //row = Math.Max(buyRow, sellRow);
            ++row;
            ++row;
        }

        ++row;
        ++row;
        int x = 13;
        cell = WriteCell(sheet, x++, row, (double)Position.BreakEvenPrice);
        cell.CellStyle = CellStyleDecimalNormal;

        AutoSize(sheet, columns);
    }


    public void DumpBreakEven()
    {
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet("BE");
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

        decimal be;
        decimal totalValue = 0;
        decimal totalQuantity = 0;
        decimal totalCommission = 0;
        foreach (CryptoPositionPart part in Position.Parts.Values.ToList())
        {
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                // Geannuleerde order of openstaande orders overslagen
                if (step.Status == CryptoOrderStatus.Expired || step.Status == CryptoOrderStatus.Canceled || !step.CloseTime.HasValue)
                    continue;

                ++row;
                int column = 0;
                int factor = 1;
                if (step.Side == CryptoOrderSide.Buy)
                    factor = -1;

                var cell = WriteCell(sheet, column++, row, step.Side.ToString());
                //cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, step.CreateTime.ToLocalTime());
                cell.CellStyle = CellStyleDate;

                cell = WriteCell(sheet, column++, row, (DateTime)step.CloseTime?.ToLocalTime());
                cell.CellStyle = CellStyleDate;

                cell = WriteCell(sheet, column++, row, (double)(step.QuoteQuantityFilled / step.Quantity)); // gem. prijs
                cell.CellStyle = CellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)step.Quantity);
                cell.CellStyle = CellStyleDecimalNormal;
                totalQuantity += factor * step.QuantityFilled;

                cell = WriteCell(sheet, column++, row, factor * (double)step.QuoteQuantityFilled);
                cell.CellStyle = CellStyleDecimalNormal;
                totalValue += factor * step.QuoteQuantityFilled;

                cell = WriteCell(sheet, column++, row, (double)step.Commission);
                cell.CellStyle = CellStyleDecimalNormal;

                totalCommission += step.Commission;

                be = 0;
                if (totalQuantity != 0)
                    be = (totalValue - totalCommission) / totalQuantity;
                cell = WriteCell(sheet, column++, row, (double)be);
                cell.CellStyle = CellStyleDecimalNormal;
            }
        }
        AutoSize(sheet, columns);
    }


    public void DumpTrades()
    {
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet("Trades");
        int row = 0;

        int columns = 0;
        WriteCell(sheet, columns++, row, "Id");
        WriteCell(sheet, columns++, row, "TradeId");
        WriteCell(sheet, columns++, row, "OrderId");

        WriteCell(sheet, columns++, row, "Side");
        WriteCell(sheet, columns++, row, "Time");

        WriteCell(sheet, columns++, row, "Price");
        WriteCell(sheet, columns++, row, "Quantity");
        WriteCell(sheet, columns++, row, "QuoteQuantity");
        WriteCell(sheet, columns++, row, "Commission");
        WriteCell(sheet, columns++, row, "C. Asset");

        foreach (CryptoTrade trade in Position.Symbol.TradeList.Values.ToList())
        {
            if (!OrderList.ContainsKey(trade.OrderId))
                continue;

            ++row;
            int column = 0;

            var cell = WriteCell(sheet, column++, row, trade.Id);
            //cell.CellStyle = cellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, trade.TradeId);

            cell = WriteCell(sheet, column++, row, trade.OrderId);

            cell = WriteCell(sheet, column++, row, trade.Side.ToString());

            cell = WriteCell(sheet, column++, row, trade.TradeTime.ToLocalTime());
            cell.CellStyle = CellStyleDate;

            cell = WriteCell(sheet, column++, row, (double)trade.Price);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)trade.Quantity);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)trade.QuoteQuantity);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)trade.Commission);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, trade.CommissionAsset);
            cell.CellStyle = CellStyleDecimalNormal;
        }

        AutoSize(sheet, columns);
    }

    public void ExportToExcell(CryptoPosition position)
    {
        Position = position;
        try
        {
            CreateBook(Position.Symbol.Name);
            CreateFormats();
            DumpParts();
            DumpTrades();
            DumpBreakEven();
            StartExcell("Position", Position.Symbol.Name, Position.Exchange.Name);
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("ERROR postion dump " + error.ToString());
        }
    }
}
