using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Trader;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner;

public class CryptoDataGridPositionsOpen<T>(DataGridView grid, List<T> list) : CryptoDataGrid<T>(grid, list) where T : CryptoPosition
{
    public override void InitializeCommands(ContextMenuStrip menuStrip)
    {
        AddStandardSymbolCommands(menuStrip, false);

        menuStrip.Items.Add(new ToolStripSeparator());

        ToolStripMenuItem menuCommand;
        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Positie herberekenen";
        menuCommand.Click += CommandPositionsOpenRecalculateExecute;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Positie verwijderen uit database";
        menuCommand.Click += CommandPositionsOpenDeleteFromDatabase;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Extra DCA toevoegen aan de positie";
        menuCommand.Click += CommandPositionsOpenCreateAdditionalDca;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Openstaande DCA van positie annuleren";
        menuCommand.Click += CommandPositionsOpenRemoveAdditionalDca;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Positie profit nemen (indien mogelijk)";
        menuCommand.Click += CommandPositionsOpenLastPartTakeProfit;
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
        CreateColumn("Update", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
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

        CreateColumn("BreakEven", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Quantity", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Open", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Net NPL", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        //CreateColumn("Net NPL%", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("BE perc", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);

        CreateColumn("Parts", typeof(int), string.Empty, DataGridViewContentAlignment.MiddleRight);
        CreateColumn("EntryPrice", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("ProfitPrice", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("FundingRate", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);

    }


    private int Compare(CryptoPosition a, CryptoPosition b)
    {
        int compareResult = SortColumn switch
        {
            00 => ObjectCompare.Compare(a.Id, b.Id),
            01 => ObjectCompare.Compare(a.CreateTime, b.CreateTime),
            02 => ObjectCompare.Compare(a.UpdateTime, b.UpdateTime),
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

            14 => ObjectCompare.Compare(a.BreakEvenPrice, b.BreakEvenPrice),
            15 => ObjectCompare.Compare(a.Quantity, b.Quantity),
            16 => ObjectCompare.Compare(a.Invested - a.Returned - a.Commission, b.Invested - b.Returned - b.Commission),
            17 => ObjectCompare.Compare(a.CurrentProfit(), b.CurrentProfit()),
            18 => ObjectCompare.Compare(a.CurrentBreakEvenPercentage(), b.CurrentBreakEvenPercentage()),

            19 => ObjectCompare.Compare(a.PartCount, b.PartCount),
            20 => ObjectCompare.Compare(a.EntryPrice, b.EntryPrice),
            21 => ObjectCompare.Compare(a.ProfitPrice, b.ProfitPrice),
            22 => ObjectCompare.Compare(a.Symbol.FundingRate, b.Symbol.FundingRate),
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
                2 => position.UpdateTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
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
                14 => // iff ? true, false shitty line
                      //                if (position.Status == CryptoPositionStatus.Waiting)
                      //          position.EntryPrice?.ToString(position.Symbol.PriceDisplayFormat),
                      //            else
                    position.BreakEvenPrice.ToString(position.Symbol.PriceDisplayFormat),
                15 => position.Quantity.ToString0(position.Symbol.QuantityDisplayFormat),
                16 => (position.Invested - position.Returned).ToString(position.Symbol.QuoteData.DisplayFormat),
                17 => position.CurrentProfit().ToString(position.Symbol.QuoteData.DisplayFormat),
                18 => position.CurrentBreakEvenPercentage().ToString("N2"),
                19 => position.PartCountText(),
                20 => position.EntryPrice?.ToString(position.Symbol.PriceDisplayFormat),
                21 => position.ProfitPrice?.ToString(position.Symbol.PriceDisplayFormat),
                _ => '?',
            };
        }
    }


    public override void CellFormattingEvent(object sender, DataGridViewCellFormattingEventArgs e)
    {
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
            await TradeTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
            TradeTools.CalculatePositionResultsViaTrades(databaseThread, position, saveChangesAnywhay: true);

            Grid.InvalidateRow(rowIndex);
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig positie {position.Id} herberekend");
        }

    }


    private void CommandPositionsOpenDeleteFromDatabase(object sender, EventArgs e)
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
            await TradeTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
            TradeTools.CalculatePositionResultsViaTrades(databaseThread, position);
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
            await TradeTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
            TradeTools.CalculatePositionResultsViaTrades(databaseThread, position);

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
            await TradeTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
            TradeTools.CalculatePositionResultsViaTrades(databaseThread, position);
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

}
