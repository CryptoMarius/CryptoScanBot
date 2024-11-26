using Microsoft.VisualStudio.TestTools.UnitTesting;
using CryptoScanBotTests;
using Dapper.Contrib.Extensions;
using CryptoScanBot.Core.Trader;
using Dapper;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Intern;

namespace CryptoScanBot.Intern.Tests;

[TestClass]
public class TradeToolsTest : TestBase
{
    [TestMethod]
    // Een ingewikkelde ketentest 
    public void CalculateCandleForIntervalTest()
    {
        InitTestSession();

        CryptoDatabase database = new();
        database.Open();

        DateTime lastCandle1mCloseTimeDate;
        DateTime startTime = DateTime.UtcNow.AddHours(-48);

        GlobalData.Settings.Trading.GlobalBuyCooldownTime = 10;
        GlobalData.Settings.Trading.TakeProfitStrategy = CryptoTakeProfitStrategy.FixedPercentage;
        GlobalData.Settings.Trading.ProfitPercentage = 1m;


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

        CryptoPosition position = PositionTools.CreatePosition(GlobalData.ActiveAccount!, symbol, CryptoSignalStrategy.Stobb, 
            CryptoTradeSide.Long, symbol.IntervalPeriodList[0], lastCandle1mCloseTimeDate);
        database.Connection.Insert<CryptoPosition>(position);
        position.Account.Data.PositionList.Add(symbol.Name, position);

        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();

        // Dit wordt een rommeltje, in aparte routines afsplitsen?

        CryptoPositionPart entryPart = PositionTools.ExtendPosition(database, position, CryptoPartPurpose.Entry, GlobalData.IntervalList[0], 
            CryptoSignalStrategy.Stobb, CryptoEntryOrDcaStrategy.AfterNextSignal, tradeParams.Price, lastCandle1mCloseTimeDate);
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
            _ = PaperTrading.CreatePaperTradeObject(database, position, entryPart, step, tradeParams.Price, lastCandle1mCloseTimeDate);
        });
        task.Wait();


        // De eerste market buy is gevuld, controle!
        // 2 controles, want blijft alles wel hetzelfde?
        task = Task.Run(() => _ = TradeTools.CalculatePositionResultsViaOrders(database, position) );
        task.Wait();
        CheckAfterMarketBuy(position, entryPart, step, CryptoOrderStatus.PartiallyAndClosed);

        // TODO: Verkeerde datum in de stepo voor emulator /backtest, dat moet de laatste datum van de order of trade zijn!
        //step.CloseTime = lastCandle.Date.AddMinutes(1); // CloseTime




        // *********************************************************************************************
        // ********************************** place entry TP *******************************************
        // *********************************************************************************************
        // Nu moet er een sell gezet worden
        // Ik zie een probleem met de Prepare en afwezige candles!
        // Die zijn bedoeld voor trailing stuff enzo, hoe werk je daar omheen?

        task = Task.Run(() =>
        {
            PositionMonitor positionMonitor = new(position.Account, position.Symbol, lastCandle);
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
            PositionMonitor positionMonitor = new(position.Account, position.Symbol, lastCandle);
            _ = positionMonitor.CheckThePosition(position);
        });
        task.Wait();

        if (position.PartList.Count != 2)
            Assert.Fail("Geen dca gemaakt");

        // Check, of het wel zoveel % lager is (wat is die standaard percentage eigenlijk?)

        // De dca en sell veranderd niets, maar blijft alles wel hetzelfde?
        task = Task.Run(() => _ = TradeTools.CalculatePositionResultsViaOrders(database, position) );
        task.Wait();
        CheckAfterMarketBuy(position, entryPart, step, CryptoOrderStatus.PartiallyAndClosed);



        // *********************************************************************************************
        // ********************************** DCA 1 filled *********************************************
        // *********************************************************************************************
        CryptoPositionPart dca1Part = position.PartList.Values.Last();
        CryptoPositionStep dca1Step = dca1Part.StepList.Values.Last();
        lastCandle = GenerateCandles(symbol, ref startTime, 20, dca1Step.Price);
        lastCandle1mCloseTimeDate = lastCandle.Date.AddMinutes(1);
        tradeParams = CreateTradeParams(database, startTime, CryptoOrderSide.Buy, CryptoOrderType.Market, dca1Step.Price, dca1Step.Quantity);

        task = Task.Run(() => _ = PaperTrading.CreatePaperTradeObject(database, position, dca1Part, dca1Step, tradeParams.Price, lastCandle1mCloseTimeDate));
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
        // Ik zie een probleem met de Prepare en afwezige candles!
        // Die zijn bedoeld voor trailing stuff enzo, hoe werk je daar omheen?

        task = Task.Run(() =>
        {
            PositionMonitor positionMonitor = new(position.Account, position.Symbol, lastCandle);
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
                PositionMonitor positionMonitor = new(position.Account, position.Symbol, lastCandle);
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
        Assert.AreEqual(step.Status, status);
        //Assert.AreEqual(step.CloseTime.Value, position.UpdateTime);

        // Dit kan nooit 0.53 zijn, er is namelijk minder aangekocht (vanwege de fees in base)
        Assert.AreEqual(step.QuantityFilled, 0.53m);
        Assert.AreEqual(part.Quantity, 0.53m - 0.000795m);
        Assert.AreEqual(position.Quantity, 0.53m - 0.000795m);

        Assert.AreEqual(step.Commission, 0.0044727495m);
        Assert.AreEqual(part.Commission, 0.0044727495m);
        Assert.AreEqual(position.Commission, 0.0044727495m);

        Assert.AreEqual(step.CommissionBase, 0.000795m);
        Assert.AreEqual(part.CommissionBase, 0.000795m);
        Assert.AreEqual(position.CommissionBase, 0.000795m);

        Assert.AreEqual(step.CommissionQuote, 0m);
        Assert.AreEqual(part.CommissionQuote, 0m);
        Assert.AreEqual(position.CommissionQuote, 0m);

        Assert.AreEqual(part.BreakEvenPrice, 5.6345518277416124186279419129m);
        Assert.AreEqual(position.BreakEvenPrice, 5.6429909777416124186279419129m);

        Assert.AreEqual(part.Invested, 2.9773602505m);
        Assert.AreEqual(position.Invested, 2.9773602505m);

        Assert.AreEqual(part.Returned, 0m);
        Assert.AreEqual(position.Returned, 0m);

        Assert.AreEqual(part.Reserved, 0m);
        Assert.AreEqual(position.Reserved, 0m);
    }



    private static void CheckAfterDca1Buy(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step, CryptoOrderStatus status)
    {
        Assert.IsNotNull(step.CloseTime);
        // die kunnen wel varieren afhankelijk van de vorige acties, even uitgezet want is niet zo boeiend
        //Assert.AreEqual(step.CloseTime.Value, lastCandle1mCloseTimeDate.AddSeconds(2));
        Assert.AreEqual(step.Status, status);
        //Assert.AreEqual(step.CloseTime.Value, position.UpdateTime);

        // Dit kan nooit 0.53 zijn, er is namelijk minder aangekocht (vanwege de fees in base)
        Assert.AreEqual(step.QuantityFilled, 1.07m);
        Assert.AreEqual(part.Quantity, 1.07m - 0.00107m); // + 0.53m - 0.00053m
        Assert.AreEqual(position.Quantity, 1.07m - 0.00107m + 0.53m - 0.00053m);

        Assert.AreEqual(step.Commission, 0.005929619m);
        Assert.AreEqual(part.Commission, 0.005929619m);
        Assert.AreEqual(position.Commission, 0.011869212m);

        Assert.AreEqual(step.CommissionBase, 0.00107m);
        Assert.AreEqual(part.CommissionBase, 0.00107m);
        Assert.AreEqual(position.CommissionBase, 0.00160m);

        Assert.AreEqual(step.CommissionQuote, 0m);
        Assert.AreEqual(part.CommissionQuote, 0m);
        Assert.AreEqual(position.CommissionQuote, 0m);

        Assert.AreEqual(part.BreakEvenPrice, 5.5472417m);
        Assert.AreEqual(position.BreakEvenPrice, 5.5770757575m);

        Assert.AreEqual(part.Invested, 5.929619m);
        Assert.AreEqual(position.Invested, 8.911452m);

        Assert.AreEqual(part.Returned, 0m);
        Assert.AreEqual(position.Returned, 0m);

        Assert.AreEqual(part.Reserved, 0m);
        Assert.AreEqual(position.Reserved, 0m);
    }
}
