using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper;

namespace CryptoScanner.CoreTests.Context;

/// <summary>
/// The exchange stamp in Meta.ExchangeName of a candles.db.
/// <para>
/// The stamp is written once, when the file is created, and no schema migration touches it
/// afterwards. So when every derivatives market was renamed from "&lt;exchange&gt; Futures" to
/// "&lt;exchange&gt; Perpetual" on 27-08-2026, every store that already existed kept the old name:
/// database version 87 renamed the FILES and left what is inside them alone. Seven of the nine
/// perpetual markets were still in that state on 29-08-2026, each of them reading as "copied from
/// another exchange" on a candle history that was correct to the minute.
/// </para>
/// <para>
/// So the stamp has to follow that one rename - and nothing else. A stamp that differs in any other
/// way is what the stamp exists to catch, and accepting those too would leave a guard that guards
/// nothing.
/// </para>
/// </summary>
[TestClass]
[DoNotParallelize]
public class CandleDatabaseExchangeNameTests : TestBase
{
    private const string MarketName = "Bybit Perpetual";
    private const string NameBeforeTheRename = "Bybit Futures";


    /// <summary>
    /// A market of its own, so the file this writes to cannot collide with the store the other
    /// tests share. Its schema is brought up to date once here; every test below then starts from a
    /// file that is already at the current version, which is the state a stale stamp survives in.
    /// </summary>
    private static CryptoScanner.Core.Model.CryptoExchange Setup()
    {
        InitTestSession();
        if (!GlobalData.ExchangeListName.TryGetValue(MarketName, out CryptoScanner.Core.Model.CryptoExchange? exchange))
            throw new Exception($"Exchange '{MarketName}' bestaat niet");

        CandleDatabase.InitializeSchema(exchange);
        return exchange;
    }

    private static void ArrangeStamp(CryptoScanner.Core.Model.CryptoExchange exchange, string? stamp)
    {
        using var candleDb = new CandleDatabase(exchange);
        candleDb.Open();
        if (stamp == null)
            candleDb.Connection.Execute("DELETE FROM Meta WHERE Key = 'ExchangeName'");
        else
            candleDb.Connection.Execute(
                "INSERT OR REPLACE INTO Meta (Key, Value) VALUES ('ExchangeName', @Name)", new { Name = stamp });
    }

    private static string? ReadStamp(CryptoScanner.Core.Model.CryptoExchange exchange)
    {
        using var candleDb = new CandleDatabase(exchange);
        candleDb.Open();
        return candleDb.Connection.QueryFirstOrDefault<string>(
            "SELECT Value FROM Meta WHERE Key = 'ExchangeName'");
    }


    [TestMethod]
    public void TheNameFromBeforeTheRename_IsBroughtInLine()
    {
        CryptoScanner.Core.Model.CryptoExchange exchange = Setup();
        ArrangeStamp(exchange, NameBeforeTheRename);

        CandleDatabase.InitializeSchema(exchange);

        Assert.AreEqual(MarketName, ReadStamp(exchange),
            "the stamp of a market that was only renamed has to follow that rename");
    }


    /// <summary>
    /// The same exchange, the other market. This is the case the stamp is there for: two markets of
    /// one brand whose files sit next to each other, so a copy into the wrong folder is a slip of a
    /// moment. Accepting it would silently read spot candles as perpetual ones.
    /// </summary>
    [TestMethod]
    public void TheOtherMarketOfTheSameExchange_IsNotAccepted()
    {
        CryptoScanner.Core.Model.CryptoExchange exchange = Setup();
        ArrangeStamp(exchange, "Bybit Spot");

        CandleDatabase.InitializeSchema(exchange);

        Assert.AreEqual("Bybit Spot", ReadStamp(exchange),
            "a file from another market keeps its own stamp, so the mismatch stays visible");
    }


    [TestMethod]
    public void AnotherExchangeEntirely_IsNotAccepted()
    {
        CryptoScanner.Core.Model.CryptoExchange exchange = Setup();
        ArrangeStamp(exchange, "Binance Perpetual");

        CandleDatabase.InitializeSchema(exchange);

        Assert.AreEqual("Binance Perpetual", ReadStamp(exchange),
            "the market type matching is not enough - the exchange has to match too");
    }


    /// <summary>
    /// The pre-rename name of ANOTHER exchange. Both halves are wrong at once, which is the case a
    /// check on the suffix alone would wave through.
    /// </summary>
    [TestMethod]
    public void ThePreRenameNameOfAnotherExchange_IsNotAccepted()
    {
        CryptoScanner.Core.Model.CryptoExchange exchange = Setup();
        ArrangeStamp(exchange, "Binance Futures");

        CandleDatabase.InitializeSchema(exchange);

        Assert.AreEqual("Binance Futures", ReadStamp(exchange),
            "'<other exchange> Futures' is not what this market used to be called");
    }


    /// <summary>
    /// A file from before the Meta table carried a name at all. There is nothing to contradict, so
    /// it is stamped rather than reported.
    /// </summary>
    [TestMethod]
    public void AFileWithoutAStamp_IsStamped()
    {
        CryptoScanner.Core.Model.CryptoExchange exchange = Setup();
        ArrangeStamp(exchange, null);

        CandleDatabase.InitializeSchema(exchange);

        Assert.AreEqual(MarketName, ReadStamp(exchange),
            "a missing stamp is filled in with the market the file belongs to");
    }


    /// <summary>
    /// Runs on every save pass, so doing nothing when the stamp is already right is the normal case
    /// rather than an edge one.
    /// </summary>
    [TestMethod]
    public void AStampThatIsAlreadyRight_StaysAsItIs()
    {
        CryptoScanner.Core.Model.CryptoExchange exchange = Setup();
        ArrangeStamp(exchange, MarketName);

        CandleDatabase.InitializeSchema(exchange);
        CandleDatabase.InitializeSchema(exchange);

        Assert.AreEqual(MarketName, ReadStamp(exchange));
    }
}
