using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using Dapper.Contrib.Extensions;
using CryptoScanBot.CoreTests;
using CryptoScanBot.Core.Core;

namespace CryptoScanBot.Core.Trader.Tests;

[TestClass]
public class PaperAssetsTests : TestBase
{
    [TestMethod()]
    public void ChangeTest()
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();

        // arrange
        DateTime startTime = DateTime.UtcNow.AddHours(-48);

        GlobalData.Settings.Trading.GlobalBuyCooldownTime = 10;
        GlobalData.Settings.Trading.TakeProfitStrategy = CryptoTakeProfitStrategy.FixedPercentage;
        GlobalData.Settings.Trading.ProfitPercentage = 1m;

        CryptoSymbol symbol = CreateTestSymbol(database);

        DeleteAllPositionRelatedStuff(database);

        // Quote asset (USDT)
        CryptoAsset assetQuote = new()
        {
            Name = symbol.Quote,
            TradeAccountId = GlobalData.ActiveAccount!.Id,
            Total = 1000,
            Free = 1000,
            Locked = 0,
        };
        GlobalData.ActiveAccount!.Data.AssetList.Add(assetQuote.Name, assetQuote);
        database.Connection.Insert(assetQuote);

        CryptoPosition position = PositionTools.CreatePosition(GlobalData.ActiveAccount!, symbol, CryptoSignalStrategy.Stobb,
            CryptoTradeSide.Long, symbol.IntervalPeriodList[0], startTime);

        // act
        TradeParams tradeParams = CreateTradeParams(database, startTime, CryptoOrderSide.Buy, CryptoOrderType.Market, 5.6261m, 0.53m);
        PaperAssets.Change(position.Account, symbol, CryptoTradeSide.Long, tradeParams.OrderSide,
            CryptoOrderStatus.New, tradeParams.Quantity, tradeParams.QuoteQuantity, "test1.1");
        Assert.AreEqual(assetQuote.Total, 1000m);
        Assert.AreEqual(assetQuote.Locked, 2.981833m);
        Assert.AreEqual(assetQuote.Free, 997.018167m);

        PaperAssets.Change(position.Account, symbol, CryptoTradeSide.Long, tradeParams.OrderSide,
            CryptoOrderStatus.Filled, tradeParams.Quantity, tradeParams.QuoteQuantity, "test1.2");
        Assert.AreEqual(assetQuote.Total, 1000m - 2.981833m);
        Assert.AreEqual(assetQuote.Locked, 0);
        Assert.AreEqual(assetQuote.Free, 997.018167m);
    }

}