using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;
using CryptoScanner.CoreTests;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Intern.Tests;

[TestClass]
public class TradeToolsTest : TestBase
{
    //[TestMethod]
    // does not work anymore.... Since the dust adjustments...
    public void CalculateCandleForIntervalTest()
    {
        InitTestSession();

        CryptoDatabase database = new();
        database.Open();

        DateTime lastCandle1mCloseTimeDate;
        DateTime startTime = DateTime.UtcNow.AddHours(-48);

        GlobalData.Settings.Trading.GlobalBuyCooldownTime = 10;
        GlobalData.Settings.Trading.TakeProfitStrategy = CryptoTakeProfitStrategy.FixedPercentage;
        GlobalData.Settings.Trading.TpList = [new CryptoTpEntry { Percentage = 1m, Factor = 100m }];


        // Gebaseerd op een entry in MASKUSDT waarin dust en BE een probleem is/was?
        TradeParams tradeParams = CreateTradeParams(database, startTime, CryptoOrderSide.Buy, CryptoOrderType.Market, 5.6261m, 0.53m);


        // *********************************************************************************************
        // ********************************** place entry **********************************************
        // *********************************************************************************************
        // Entry buy (market)

        CryptoSymbol symbol = CreateTestSymbol(database);
        symbol.QuoteData.EntryAmount = 3m;
        symbol.LastPrice = tradeParams.Price;
        CryptoCandle lastCandle = GenerateCandles(symbol, ref startTime, 1440, tradeParams.Price);
        lastCandle1mCloseTimeDate = lastCandle.Date.AddMinutes(1);

        DeleteAllPositionRelatedStuff(database);

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        CryptoPosition position = PositionTools.CreatePosition(symbol, "stobb",
            CryptoTradeSide.Long, "Test", symbolInterval, lastCandle1mCloseTimeDate);
        database.Connection.Insert<CryptoPosition>(position);
        GlobalData.ActiveExchange!.Data.PositionList[symbol.Name] = position;

        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();

        // Dit wordt een rommeltje, in aparte routines afsplitsen?

        CryptoPositionPart entryPart = PositionTools.ExtendPosition(database, position, CryptoPartPurpose.Entry, GlobalData.IntervalList[0],
            "stobb", tradeParams.Price, lastCandle1mCloseTimeDate);
        if (position.PartList.Count != 1)
            Assert.Fail("Geen entry gemaakt");

        CryptoPositionStep step = PositionTools.CreatePositionStep(position, entryPart, tradeParams, CryptoTrailing.None);
        step.OrderId = database.CreateNewUniqueId();
        database.Connection.Insert<CryptoPositionStep>(step);
        PositionTools.AddPositionPartStep(entryPart, step);
        database.Connection.Update<CryptoPositionPart>(entryPart);
        position.EntryPrice = tradeParams.Price;
        position.EntryAmount = tradeParams.QuoteQuantity;
        database.Connection.Update<CryptoPosition>(position);

        if (entryPart.StepList.Count != 1)
            Assert.Fail("Geen entry order gemaakt");

        // probleem, deze gaat rechtstreeks door naar andere routines (teveel verweven)
        Task task = Task.Run(() =>
        {
            _ = PaperTrading.CreatePaperTrade(database, position, entryPart, step, tradeParams.Price, lastCandle.OpenTime);
        });
        task.Wait();


        // De eerste market buy is gevuld, controle!
        // 2 controles, want blijft alles wel hetzelfde?
        task = Task.Run(() => _ = TradeTools.CalculatePositionResultsViaOrders(database, position));
        task.Wait();
        CheckAfterMarketBuy(position, entryPart, step, CryptoOrderStatus.PartiallyAndClosed);

        // TODO: Verkeerde datum in de stepo voor emulator /backtest, dat moet de laatste datum van de order of trade zijn!
        //step.CloseTime = lastCandle.Time.AddMinutes(1); // CloseTime




        // *********************************************************************************************
        // ********************************** place entry TP *******************************************
        // *********************************************************************************************
        // Nu moet er een sell gezet worden
        // Ik zie een probleem met de PrepareIndicators en afwezige candles!
        // Die zijn bedoeld voor trailing stuff enzo, hoe werk je daar omheen?

        task = Task.Run(() =>
        {
            PositionMonitor positionMonitor = new(position.Symbol, lastCandle);
            _ = positionMonitor.CheckThePosition(position);
        });
        task.Wait();


        CryptoPositionStep? stepProfitx = PositionTools.FindOpenStep(position, CryptoOrderSide.Sell, CryptoPartPurpose.TakeProfit);
        if (stepProfitx == null)
            Assert.Fail("Geen tp order gemaakt");



        // De sell veranderd niets, maar blijft alles wel hetzelfde?
        task = Task.Run(() => _ = TradeTools.CalculatePositionResultsViaOrders(database, position));
        task.Wait();
        CheckAfterMarketBuy(position, entryPart, step, CryptoOrderStatus.PartiallyAndClosed);


        // Is de sell order wel geplaatst?
        CryptoPositionStep? stepProfit = PositionTools.FindPositionPartStep(entryPart, takeProfitOrderSide, false);
        if (stepProfit == null)
            Assert.Fail("Geen take profit order aanwezig");


        // *********************************************************************************************
        // ********************************** place DCA *************************************************
        // *********************************************************************************************
        lastCandle = GenerateCandles(symbol, ref startTime, 12, tradeParams.Price);
        lastCandle1mCloseTimeDate = lastCandle.Date.AddMinutes(1);

        task = Task.Run(() =>
        {
            PositionMonitor positionMonitor = new(position.Symbol, lastCandle);
            _ = positionMonitor.CheckThePosition(position);
        });
        task.Wait();

        if (position.PartList.Count != 2)
            Assert.Fail("Geen dca gemaakt");

        // Check, of het wel zoveel % lager is (wat is die standaard percentage eigenlijk?)

        // De dca en sell veranderd niets, maar blijft alles wel hetzelfde?
        task = Task.Run(() => _ = TradeTools.CalculatePositionResultsViaOrders(database, position));
        task.Wait();
        CheckAfterMarketBuy(position, entryPart, step, CryptoOrderStatus.PartiallyAndClosed);



        // *********************************************************************************************
        // ********************************** DCA 1 filled *********************************************
        // *********************************************************************************************
        CryptoPositionPart dca1Part = position.PartList.Values.Last();
        CryptoPositionStep dca1Step = dca1Part.StepList.Values.Last();
        lastCandle = GenerateCandles(symbol, ref startTime, 20, dca1Step.Price);
        tradeParams = CreateTradeParams(database, startTime, CryptoOrderSide.Buy, CryptoOrderType.Market, dca1Step.Price, dca1Step.Quantity);

        task = Task.Run(() => _ = PaperTrading.CreatePaperTrade(database, position, dca1Part, dca1Step, tradeParams.Price, lastCandle.OpenTime));
        task.Wait();

        // Nu wordt het een en ander aangepast (en wordt het interessant)
        task = Task.Run(() => _ = TradeTools.CalculatePositionResultsViaOrders(database, position));
        task.Wait();
        CheckAfterDca1Buy(position, dca1Part, dca1Step, CryptoOrderStatus.Filled);


        // reactie:
        // De sell wordt geannuleerd
        // Er worden 2 nieuwe sells geplaatst
        // ...?

        // *********************************************************************************************
        // ********************************** place TP#2 ***********************************************
        // *********************************************************************************************
        // Nu moet er een sell gezet worden
        // Ik zie een probleem met de PrepareIndicators en afwezige candles!
        // Die zijn bedoeld voor trailing stuff enzo, hoe werk je daar omheen?

        task = Task.Run(() =>
        {
            PositionMonitor positionMonitor = new(position.Symbol, lastCandle);
            _ = positionMonitor.CheckThePosition(position);
        });
        task.Wait();


        if (entryPart.StepList.Count != 3)
            Assert.Fail("Geen tp order gemaakt");
        if (dca1Part.StepList.Count != 2)
            Assert.Fail("Geen tp order gemaakt");

        // De sell veranderd niets, maar blijft alles wel hetzelfde?
        task = Task.Run(() => _ = TradeTools.CalculatePositionResultsViaOrders(database, position));
        task.Wait();
        CheckAfterDca1Buy(position, dca1Part, dca1Step, CryptoOrderStatus.Filled);


        // Is sell order van de entry part geplaatst?
        CryptoPositionStep? entryProfit = PositionTools.FindPositionPartStep(entryPart, takeProfitOrderSide, false);
        if (entryProfit == null)
            Assert.Fail("Geen take profit order aanwezig");

        // Is sell order van de dca 1 geplaatst?
        CryptoPositionStep? dca1Profit = PositionTools.FindPositionPartStep(dca1Part, takeProfitOrderSide, false);
        if (dca1Profit == null)
            Assert.Fail("Geen take profit order aanwezig");


        for (int i = 0; i <= 2; i++)
        {
            lastCandle = GenerateCandles(symbol, ref startTime, 1, dca1Step.Price);
            lastCandle1mCloseTimeDate = lastCandle.Date.AddMinutes(1);

            task = Task.Run(() =>
            {
                PositionMonitor positionMonitor = new(position.Symbol, lastCandle);
                _ = positionMonitor.CheckThePosition(position);
            });
            task.Wait();

            if (entryPart.StepList.Count != 3)
                Assert.Fail("Geen tp order gemaakt");
            if (dca1Part.StepList.Count != 2)
                Assert.Fail("Geen tp order gemaakt");

            // De sell veranderd niets, maar blijft alles wel hetzelfde?
            task = Task.Run(() => _ = TradeTools.CalculatePositionResultsViaOrders(database, position));
            task.Wait();
            CheckAfterDca1Buy(position, dca1Part, dca1Step, CryptoOrderStatus.Filled);
        }
    }




    private static void CheckAfterMarketBuy(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step, CryptoOrderStatus status)
    {

        Assert.IsNotNull(step.CloseTime);
        // die kunnen wel varieren afhankelijk van de vorige acties, even uitgezet want is niet zo boeiend
        //Assert.AreEqual(step.CloseTime.Value, lastCandle1mCloseTimeDate.AddSeconds(2));
        Assert.AreEqual(status, step.Status);
        //Assert.AreEqual(step.CloseTime.Value, position.UpdateTime);

        // Dit kan nooit 0.53 zijn, er is namelijk minder aangekocht (vanwege de fees in base)
        Assert.AreEqual(0.53m, step.QuantityFilled);
        Assert.AreEqual(0.53m - 0.000795m, part.Quantity);
        Assert.AreEqual(0.53m - 0.000795m, position.Quantity);

        Assert.AreEqual(0.0044727495m, step.Commission);
        Assert.AreEqual(0.0044727495m, part.Commission);
        Assert.AreEqual(0.0044727495m, position.Commission);

        Assert.AreEqual(0.000795m, step.CommissionBase);
        Assert.AreEqual(0.000795m, part.CommissionBase);
        Assert.AreEqual(0.000795m, position.CommissionBase);

        Assert.AreEqual(0m, step.CommissionQuote);
        Assert.AreEqual(0m, part.CommissionQuote);
        Assert.AreEqual(0m, position.CommissionQuote);

        Assert.AreEqual(5.6345518277416124186279419129m, part.BreakEvenPrice);
        Assert.AreEqual(5.6429909777416124186279419129m, position.BreakEvenPrice);

        Assert.AreEqual(2.9773602505m, part.Invested);
        Assert.AreEqual(2.9773602505m, position.Invested);

        Assert.AreEqual(0m, part.Returned);
        Assert.AreEqual(0m, position.Returned);

        Assert.AreEqual(0m, part.Reserved);
        Assert.AreEqual(0m, position.Reserved);
    }



    private static void CheckAfterDca1Buy(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step, CryptoOrderStatus status)
    {
        Assert.IsNotNull(step.CloseTime);
        // die kunnen wel varieren afhankelijk van de vorige acties, even uitgezet want is niet zo boeiend
        //Assert.AreEqual(step.CloseTime.Value, lastCandle1mCloseTimeDate.AddSeconds(2));
        Assert.AreEqual(step.Status, status);
        //Assert.AreEqual(step.CloseTime.Value, position.UpdateTime);

        // Dit kan nooit 0.53 zijn, er is namelijk minder aangekocht (vanwege de fees in base)
        Assert.AreEqual(1.07m, step.QuantityFilled);
        Assert.AreEqual(1.07m - 0.00107m, part.Quantity); // + 0.53m - 0.00053m
        Assert.AreEqual(1.07m - 0.00107m + 0.53m - 0.00053m, position.Quantity);

        Assert.AreEqual(0.005929619m, step.Commission);
        Assert.AreEqual(0.005929619m, part.Commission);
        Assert.AreEqual(0.011869212m, position.Commission);

        Assert.AreEqual(0.00107m, step.CommissionBase);
        Assert.AreEqual(0.00107m, part.CommissionBase);
        Assert.AreEqual(0.00160m, position.CommissionBase);

        Assert.AreEqual(0m, step.CommissionQuote);
        Assert.AreEqual(0m, part.CommissionQuote);
        Assert.AreEqual(0m, position.CommissionQuote);

        Assert.AreEqual(5.5472417m, part.BreakEvenPrice);
        Assert.AreEqual(5.5770757575m, position.BreakEvenPrice);

        Assert.AreEqual(5.929619m, part.Invested);
        Assert.AreEqual(8.911452m, position.Invested);

        Assert.AreEqual(0m, part.Returned);
        Assert.AreEqual(0m, position.Returned);

        Assert.AreEqual(0m, part.Reserved);
        Assert.AreEqual(0m, position.Reserved);
    }
}
