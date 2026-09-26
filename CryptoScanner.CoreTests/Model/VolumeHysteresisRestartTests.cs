using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Model;

/// <summary>
/// The volume decision has a hysteresis band (enter above 0,9 of the boundary, leave below 0,75),
/// but the outcome is not persisted: after a restart of the session every symbol starts with no
/// decision and the first one was taken with the enter factor. A coin inside the band therefore
/// fell off at every restart and only came back after crossing 0,9 again (GRASSUSDT.PERP,
/// 25-09-2026, open point 91). Since 26-09-2026 a symbol whose newest 1m candle is recent counts as
/// "was being followed" and is judged with the leave factor instead.
/// </summary>
[TestClass]
public class VolumeHysteresisRestartTests
{
    [TestInitialize]
    public void Init() => TestBase.InitTestSession();


    private static readonly DateTime Now = new(2026, 9, 25, 12, 32, 0, DateTimeKind.Utc);

    private static CryptoSymbol CreateSymbolInsideTheBand()
    {
        var quoteData = GlobalData.AddQuoteData("USDT");
        quoteData.MinimalVolume = 1_000_000;
        return new CryptoSymbol
        {
            Status = 1,
            Exchange = GlobalData.ActiveExchange!,
            Base = "GRASS",
            Quote = "USDT",
            Name = "GRASSUSDT",
            ExchangeName = "GRASSUSDT",
            QuoteData = quoteData,
            PriceTickSize = 0.0001m,
            // 0,8 of the boundary: below the enter factor (0,9) and above the leave factor (0,75)
            Volume = 800_000,
        };
    }


    private static void RunWithClock(Action test)
    {
        IClock previousClock = GlobalData.Clock;
        bool previousEmulatorMode = GlobalData.IsEmulatorMode;
        GlobalData.Clock = new EmulatorClock { UtcNow = Now };
        GlobalData.IsEmulatorMode = false;
        try
        {
            test();
        }
        finally
        {
            GlobalData.Clock = previousClock;
            GlobalData.IsEmulatorMode = previousEmulatorMode;
        }
    }


    [TestMethod]
    public void ASymbolWithoutCandlesIsJudgedWithTheEnterFactor()
    {
        RunWithClock(() =>
        {
            CryptoSymbol symbol = CreateSymbolInsideTheBand();
            symbol.UpdateEnoughVolume();
            Assert.IsFalse(symbol.EnoughVolume(), "a cold start knows nothing about this coin, so 0,8 is below the enter factor");
        });
    }


    [TestMethod]
    public void ASymbolThatWasFollowedJustBeforeTheRestartStaysOnBoard()
    {
        RunWithClock(() =>
        {
            CryptoSymbol symbol = CreateSymbolInsideTheBand();
            // The newest 1m candle is half an hour old: it was being followed when the session stopped
            CandleTools.CreateCandle(symbol, GlobalData.IntervalList[0], Now.AddMinutes(-30), 1m, 1m, 1m, 1m, 100);

            symbol.UpdateEnoughVolume();
            Assert.IsTrue(symbol.EnoughVolume(), "inside the band and followed before, so judged with the leave factor");
        });
    }


    [TestMethod]
    public void ASymbolWhoseCandlesAreOldIsJudgedWithTheEnterFactor()
    {
        RunWithClock(() =>
        {
            CryptoSymbol symbol = CreateSymbolInsideTheBand();
            // Five hours without a candle: not followed any more, whatever the reason
            CandleTools.CreateCandle(symbol, GlobalData.IntervalList[0], Now.AddHours(-5), 1m, 1m, 1m, 1m, 100);

            symbol.UpdateEnoughVolume();
            Assert.IsFalse(symbol.EnoughVolume(), "old candles are no proof of a subscription");
        });
    }


    [TestMethod]
    public void TheEmulatorKeepsTheOldBehaviour()
    {
        RunWithClock(() =>
        {
            GlobalData.IsEmulatorMode = true;
            CryptoSymbol symbol = CreateSymbolInsideTheBand();
            CandleTools.CreateCandle(symbol, GlobalData.IntervalList[0], Now.AddMinutes(-30), 1m, 1m, 1m, 1m, 100);

            symbol.UpdateEnoughVolume();
            Assert.IsFalse(symbol.EnoughVolume(), "emulator runs have to stay comparable: candles in memory are history there");
        });
    }
}
