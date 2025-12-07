using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.CoreTests;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader.Tests;

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
            Total = 1000,
            Free = 1000,
            Locked = 0,
        };
        GlobalData.ActiveExchange!.Data.AssetList.Add(assetQuote.Name, assetQuote);
        database.Connection.Insert(assetQuote);

        CryptoPosition position = PositionTools.CreatePosition(symbol, CryptoSignalStrategy.Stobb,
            CryptoTradeSide.Long, symbol.Data.SymbolIntervalList[0], startTime);

        // act
        TradeParams tradeParams = CreateTradeParams(database, startTime, CryptoOrderSide.Buy, CryptoOrderType.Market, 5.6261m, 0.53m);
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Long, tradeParams.OrderSide,
            CryptoOrderStatus.New, tradeParams.Quantity, tradeParams.QuoteQuantity, "test1.1");
        Assert.AreEqual(1000m, assetQuote.Total);
        Assert.AreEqual(2.981833m, assetQuote.Locked);
        Assert.AreEqual(997.018167m, assetQuote.Free);

        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Long, tradeParams.OrderSide,
            CryptoOrderStatus.Filled, tradeParams.Quantity, tradeParams.QuoteQuantity, "test1.2");
        Assert.AreEqual(1000m - 2.981833m, assetQuote.Total);
        Assert.AreEqual(0, assetQuote.Locked);
        Assert.AreEqual(997.018167m, assetQuote.Free);
    }

}