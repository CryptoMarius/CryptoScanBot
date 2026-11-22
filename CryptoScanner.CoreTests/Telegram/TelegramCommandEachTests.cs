using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Telegram.Commands;

// CryptoExchange is a package namespace as well, so the model type gets an alias here
using TestExchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Telegram;

/// <summary>
/// One or more tests per Telegram command: what the command is meant to do when it is typed
/// correctly, and what it answers when it is not. Every test runs on fresh settings and a small
/// exchange of its own (BTC and SOL quoted in USDC), with saving the settings blocked, so nothing
/// reaches the settings files or the scanner. Commands that change a setting are also checked to
/// tell both scanners (StatusesHaveChangedMessage), because that is how the screens follow.
/// </summary>
[TestClass]
[DoNotParallelize]
public class TelegramCommandEachTests
{
    /// <summary>
    /// Fresh settings, a test exchange, saving blocked and a counter on the message the screens
    /// listen to. Everything is put back on Dispose.
    /// </summary>
    private sealed class Isolated : IDisposable
    {
        private readonly SettingsBasic savedSettings = GlobalData.Settings;
        private readonly TestExchange? savedExchange = GlobalData.ActiveExchange;
        private readonly Func<string?>? savedBlocked = GlobalData.SaveConfigurationBlocked;
        private readonly IServiceProvider savedServices = GlobalData.Services;

        public TestExchange Exchange { get; }
        public int ScreenMessages { get; private set; }

        public Isolated()
        {
            GlobalData.Settings = new SettingsBasic();
            GlobalData.SaveConfigurationBlocked = () => "unit test";

            // An id no real exchange has, so the profits query finds no closed positions
            Exchange = new() { Id = 987654, Name = "TelegramTest", TradingType = CryptoTradingType.Perpetual };
            GlobalData.ActiveExchange = Exchange;
            AddSymbol(Exchange, 1, "BTC", 95000m);
            AddSymbol(Exchange, 2, "SOL", 140m);

            WeakReferenceMessenger.Default.Register<Isolated, StatusesHaveChangedMessage>(this, (recipient, _) => recipient.ScreenMessages++);
        }

        public void Dispose()
        {
            WeakReferenceMessenger.Default.Unregister<StatusesHaveChangedMessage>(this);
            GlobalData.Settings = savedSettings;
            GlobalData.ActiveExchange = savedExchange;
            GlobalData.SaveConfigurationBlocked = savedBlocked;
            GlobalData.Services = savedServices;
        }
    }


    private static CryptoSymbol AddSymbol(TestExchange exchange, int id, string baseAsset, decimal price)
    {
        CryptoSymbol symbol = new()
        {
            Id = id,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Name = baseAsset + "USDC" + CryptoProduct.Separator + CryptoProduct.Perpetual,
            Base = baseAsset,
            Quote = "USDC",
            Product = CryptoProduct.Perpetual,
            ExchangeName = "instrument-" + id,
            QuoteData = GlobalData.AddQuoteData("USDC"),
            Status = 1,
            LastPrice = price,
        };
        exchange.SymbolListId.Add(symbol.Id, symbol);
        exchange.SymbolListName.Add(symbol.Name, symbol);
        exchange.SymbolListExchangeName.Add(symbol.ExchangeName, symbol);
        exchange.Products.Add(CryptoProduct.Perpetual);
        return symbol;
    }


    private static Task<TelegramCommandReply> Type(string text) => TelegramCommand.ExecuteAsync(text, 1, ownChat: true);


    // ---------------------------------------------------------------------------------------------
    // status
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task StatusShowsWhatIsSwitchedOnAndOff()
    {
        using Isolated isolated = new();
        GlobalData.Settings.Trading.Active = true;
        GlobalData.Settings.Trading.SlotsMaximalLong = 3;
        GlobalData.Settings.Trading.SlotsMaximalShort = 2;
        GlobalData.Settings.Signal.Active = false;
        GlobalData.Settings.Signal.SoundsActive = true;
        GlobalData.Settings.General.SoundTradeNotification = false;

        TelegramCommandReply reply = await Type("status");

        Assert.AreEqual("Trader is active (slots long=3, slots short=2)\n"
            + "Signals are not active\n"
            + "Signal sound is active\n"
            + "Trade sound is not active", Normalize(reply.Text));
    }


    // ---------------------------------------------------------------------------------------------
    // start, stop, signalstart, signalstop
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("start trading")]
    [DataRow("START Trading")]
    public async Task StartTradingSwitchesTheTraderOnAndTellsTheScreens(string text)
    {
        using Isolated isolated = new();
        GlobalData.Settings.Trading.Active = false;

        TelegramCommandReply reply = await Type(text);

        Assert.AreEqual("Trader started", reply.Text);
        Assert.IsTrue(GlobalData.Settings.Trading.Active);
        Assert.AreEqual(1, isolated.ScreenMessages);
    }


    [TestMethod]
    public async Task StartTradingWhenItIsAlreadyOnChangesNothing()
    {
        using Isolated isolated = new();
        GlobalData.Settings.Trading.Active = true;

        TelegramCommandReply reply = await Type("start trading");

        Assert.AreEqual("Trader is already active", reply.Text);
        Assert.AreEqual(0, isolated.ScreenMessages);
    }


    [TestMethod]
    public async Task StartSignalsAndStartSoundSwitchTheirOwnSettingOnly()
    {
        using Isolated isolated = new();
        GlobalData.Settings.Trading.Active = false;
        GlobalData.Settings.Signal.Active = false;
        GlobalData.Settings.Signal.SoundsActive = false;

        TelegramCommandReply signals = await Type("start signals");
        TelegramCommandReply sound = await Type("start sound");

        Assert.AreEqual("Signals started", signals.Text);
        Assert.AreEqual("Sound started", sound.Text);
        Assert.IsTrue(GlobalData.Settings.Signal.Active);
        Assert.IsTrue(GlobalData.Settings.Signal.SoundsActive);
        Assert.IsFalse(GlobalData.Settings.Trading.Active, "the trader must stay off");
        Assert.AreEqual(2, isolated.ScreenMessages);
    }


    [TestMethod]
    public async Task ABareStopStopsTheTrader()
    {
        using Isolated isolated = new();
        GlobalData.Settings.Trading.Active = true;
        GlobalData.Settings.Signal.Active = true;

        TelegramCommandReply reply = await Type("stop");

        Assert.AreEqual("Trader stopped", reply.Text);
        Assert.IsFalse(GlobalData.Settings.Trading.Active);
        Assert.IsTrue(GlobalData.Settings.Signal.Active, "the signals keep running");
        Assert.AreEqual(1, isolated.ScreenMessages);
    }


    [TestMethod]
    public async Task StopSignalsAndStopSoundSwitchTheirOwnSettingOnly()
    {
        using Isolated isolated = new();
        GlobalData.Settings.Trading.Active = true;
        GlobalData.Settings.Signal.Active = true;
        GlobalData.Settings.Signal.SoundsActive = true;

        TelegramCommandReply signals = await Type("stop signals");
        TelegramCommandReply sound = await Type("stop sound");
        TelegramCommandReply again = await Type("stop sound");

        Assert.AreEqual("Signals stopped", signals.Text);
        Assert.AreEqual("Sound stopped", sound.Text);
        Assert.AreEqual("Sound is already inactive", again.Text);
        Assert.IsFalse(GlobalData.Settings.Signal.Active);
        Assert.IsFalse(GlobalData.Settings.Signal.SoundsActive);
        Assert.IsTrue(GlobalData.Settings.Trading.Active, "the trader must keep running");
        Assert.AreEqual(2, isolated.ScreenMessages);
    }


    [TestMethod]
    public async Task SignalStartAndSignalStopAreTheShortFormOfStartAndStopSignals()
    {
        using Isolated isolated = new();
        GlobalData.Settings.Signal.Active = false;

        TelegramCommandReply start = await Type("signalstart");
        Assert.AreEqual("Signals started", start.Text);
        Assert.IsTrue(GlobalData.Settings.Signal.Active);

        TelegramCommandReply stop = await Type("signalstop");
        Assert.AreEqual("Signals stopped", stop.Text);
        Assert.IsFalse(GlobalData.Settings.Signal.Active);
    }


    // ---------------------------------------------------------------------------------------------
    // slots
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task SlotsChangesTheMaximumOfOneSide()
    {
        using Isolated isolated = new();
        GlobalData.Settings.Trading.SlotsMaximalLong = 1;
        GlobalData.Settings.Trading.SlotsMaximalShort = 1;

        TelegramCommandReply longReply = await Type("slots long 5");
        TelegramCommandReply shortReply = await Type("slots SHORT 0");

        Assert.AreEqual("Slots long = 5", longReply.Text);
        Assert.AreEqual("Slots short = 0", shortReply.Text);
        Assert.AreEqual(5, GlobalData.Settings.Trading.SlotsMaximalLong);
        Assert.AreEqual(0, GlobalData.Settings.Trading.SlotsMaximalShort);
        Assert.AreEqual(2, isolated.ScreenMessages);
    }


    [TestMethod]
    public async Task SlotsWithAnUnknownSideChangesNothing()
    {
        using Isolated isolated = new();
        GlobalData.Settings.Trading.SlotsMaximalLong = 1;

        TelegramCommandReply reply = await Type("slots lang 5");

        StringAssert.StartsWith(reply.Text, "Unknown side 'lang'");
        Assert.AreEqual(1, GlobalData.Settings.Trading.SlotsMaximalLong);
        Assert.AreEqual(0, isolated.ScreenMessages);
    }


    // ---------------------------------------------------------------------------------------------
    // positions, profits, assets
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task PositionsWithoutOpenPositionsSaysSo()
    {
        using Isolated isolated = new();

        TelegramCommandReply reply = await Type("positions");

        Assert.AreEqual("no positions", reply.Text);
    }


    [TestMethod]
    public async Task ProfitsWithoutClosedPositionsShowsZeros()
    {
        TestBase.InitTestSession();
        using Isolated isolated = new();

        TelegramCommandReply reply = await Type("profits");

        Assert.AreEqual($"0 positions, invested {0m:N2}, profits {0m:N2}, {0m:N2}%", reply.Text);
    }


    [TestMethod]
    public async Task AssetsShowsTheAssetList()
    {
        using Isolated isolated = new();
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        isolated.Exchange.Data.AssetList["USDC"] = new CryptoAsset { Name = "USDC", Total = 1000m, Free = 1000m };

        TelegramCommandReply reply = await Type("assets");

        StringAssert.StartsWith(reply.Text, "Assets:");
        StringAssert.Contains(reply.Text, "USDC " + 1000m.ToString0());
        Assert.IsFalse(reply.Text.Contains("Note:"), "paper trading can always read its assets");
    }


    [TestMethod]
    public async Task AssetsSaysWhenTheListCouldNotBeRefreshed()
    {
        using Isolated isolated = new();
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.RealTrading;

        TelegramCommandReply reply = await Type("assets");

        StringAssert.StartsWith(reply.Text, "Note: Reading balances from the exchange is not implemented, the list below may be out of date");
        StringAssert.Contains(reply.Text, "Assets:");
    }


    // ---------------------------------------------------------------------------------------------
    // barometer
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task BarometerShowsEveryCalculatedInterval()
    {
        using Isolated isolated = new();
        isolated.Exchange.Data.GetBarometer("USDC", CryptoIntervalPeriod.interval1h).PriceBarometer = 1.5m;

        // No quote typed and no USDT on this exchange: the first quote (USDC) is taken
        TelegramCommandReply reply = await Type("barometer");

        string[] lines = Normalize(reply.Text).Split('\n');
        Assert.AreEqual("Barometer USDC", lines[0]);
        Assert.AreEqual(1 + 10, lines.Length, "a header and the 10 intervals the barometer is calculated on");
        Assert.AreEqual(1, lines.Count(l => l.EndsWith(" " + 1.5m.ToString("N2"))), "the 1h value");
        Assert.AreEqual(9, lines.Count(l => l.EndsWith(" -")), "the intervals without a value");
    }


    [TestMethod]
    public async Task BarometerOfAnUnknownQuoteSaysSo()
    {
        using Isolated isolated = new();

        TelegramCommandReply reply = await Type("barometer xyz");

        Assert.AreEqual("Unknown quote XYZ", reply.Text);
    }


    // ---------------------------------------------------------------------------------------------
    // trend
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task TrendWithoutASymbolExplainsItself()
    {
        using Isolated isolated = new();

        TelegramCommandReply reply = await Type("trend");

        StringAssert.StartsWith(reply.Text, "Usage: trend symbol");
    }


    [TestMethod]
    public async Task TrendOfAnUnknownSymbolSaysSo()
    {
        using Isolated isolated = new();

        TelegramCommandReply reply = await Type("trend xyzusdc");

        Assert.AreEqual("Unknown symbol XYZUSDC", reply.Text);
    }


    // ---------------------------------------------------------------------------------------------
    // zones
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("zones", "Zones top 10")]
    [DataRow("zones 3", "Zones top 3")]
    [DataRow("zones 999", "Zones top 50")]
    [DataRow("zones 0", "Zones top 1")]
    [DataRow("zones abc", "Zones top 10")]
    public async Task ZonesKeepsTheCountBetweenOneAndFifty(string text, string header)
    {
        using Isolated isolated = new();

        TelegramCommandReply reply = await Type(text);

        Assert.IsTrue(reply.Html);
        Assert.AreEqual(header + "\nnot calculated?", Normalize(reply.Text));
    }


    [TestMethod]
    public async Task ZonesListsTheClosestFirstAlsoWhenTwoDistancesAreEqual()
    {
        TestBase.InitTestSession(); // the intervals, the 1h one is used for the link
        using Isolated isolated = new();
        CryptoSymbol eth = AddSymbol(isolated.Exchange, 3, "ETH", 3000m);

        // The old SortedList on the distance threw on the second symbol with the same distance
        SetZone(isolated.Exchange.SymbolListName["BTCUSDC.PERP"], longZone: 2m, shortZone: null);
        SetZone(isolated.Exchange.SymbolListName["SOLUSDC.PERP"], longZone: null, shortZone: 2m);
        SetZone(eth, longZone: 1m, shortZone: 5m);

        TelegramCommandReply reply = await Type("zones 2");

        string[] lines = Normalize(reply.Text).Split('\n');
        Assert.AreEqual(3, lines.Length, "the header and the two closest");
        Assert.AreEqual("Zones top 2", lines[0]);
        StringAssert.Contains(lines[1], "ETHUSDC.PERP");
        StringAssert.EndsWith(lines[1], $"{1m:N2} \U0001f7e2");
        StringAssert.Contains(lines[2], "BTCUSDC.PERP", "equal distance: sorted on the name");
        StringAssert.EndsWith(lines[2], $"{2m:N2} \U0001f7e2");
    }


    private static void SetZone(CryptoSymbol symbol, decimal? longZone, decimal? shortZone)
    {
        symbol.Data.DlzZoneDistance.BestLongZone = longZone;
        symbol.Data.DlzZoneDistance.BestShortZone = shortZone;
    }


    // ---------------------------------------------------------------------------------------------
    // calculatezones, reset, chatid, overview
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task CalculateZonesStartsTheCalculation()
    {
        using Isolated isolated = new();

        TelegramCommandReply reply = await Type("calculatezones");

        Assert.AreEqual("Started calculations of zones", reply.Text);
    }


    private sealed class FakeScannerSession : IScannerSession
    {
        public int Refreshes { get; private set; }
        public void ScheduleRefresh() => Refreshes++;

        public void AfterStartup() { }
        public void ChooseExchange(string exchangeName) { }
        public Task ApplyConfigurationAsync(bool loadSymbols) => Task.CompletedTask;
        public void ConnectionWasLost(string text) { }
        public void ConnectionWasRestored(string text, TimeSpan downtime) { }
        public void SetTimerDefaults() { }
        public bool Start(int delay) => true;
        public Task StopAsync() => Task.CompletedTask;
    }


    private sealed class FakeServices(object service) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType.IsInstanceOfType(service) ? service : null;
    }


    [TestMethod]
    public async Task ResetSchedulesARestartOfTheScanner()
    {
        using Isolated isolated = new();
        FakeScannerSession session = new();
        GlobalData.Services = new FakeServices(session);

        TelegramCommandReply reply = await Type("reset");

        Assert.AreEqual("Scheduled restart of the scanner", reply.Text);
        Assert.AreEqual(1, session.Refreshes);
    }


    [TestMethod]
    public async Task ResetWithoutAScannerAnswersWithTheError()
    {
        using Isolated isolated = new();
        GlobalData.Services = new FakeServices(new object());

        TelegramCommandReply reply = await Type("reset");

        Assert.AreEqual("Error in reset: IScannerSession not registered in services", reply.Text);
    }


    [TestMethod]
    public async Task ChatIdAnswersTheOwnChatAsWell()
    {
        TelegramCommandReply reply = await TelegramCommand.ExecuteAsync("chatid", 12345, ownChat: true);

        Assert.AreEqual("ChatId: 12345", reply.Text);
    }


    [TestMethod]
    public async Task TheOverviewHoldsBarometerPositionsProfitsAssetsAndValues()
    {
        TestBase.InitTestSession();
        using Isolated isolated = new();
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;

        TelegramCommandReply reply = await Type(".");

        StringAssert.StartsWith(reply.Text, "Barometer USDC");
        StringAssert.Contains(reply.Text, "no positions");
        StringAssert.Contains(reply.Text, "0 positions, invested");
        StringAssert.Contains(reply.Text, "Assets:");
        StringAssert.Contains(reply.Text, "SOLUSDC " + 140m.ToString0());
    }


    private static string Normalize(string text) => text.Replace("\r\n", "\n");
}
