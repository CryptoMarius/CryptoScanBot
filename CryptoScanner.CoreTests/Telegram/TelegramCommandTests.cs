using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Telegram.Commands;

// CryptoExchange is a package namespace as well, so the model type gets an alias here
using TestExchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Telegram;

/// <summary>
/// The Telegram commands: how a typed message is taken apart, that a typo never switches the trader,
/// that the help and the menu follow the list of commands, and that long answers are cut into
/// pieces Telegram accepts. None of these tests reaches Telegram or saves the settings.
/// </summary>
[TestClass]
public class TelegramCommandTests
{
    [TestMethod]
    [DataRow("status", "status")]
    [DataRow("STATUS", "status")]
    [DataRow("/status", "status")]
    [DataRow("/status@MyScannerBot", "status")]
    [DataRow("  trend   ethusdt ", "trend")]
    [DataRow(".", ".")]
    public void TheCommandIsFoundWhicheverWayItIsTyped(string text, string expected)
    {
        (string command, string[] _) = TelegramCommand.Parse(text);

        Assert.AreEqual(expected, command);
    }


    [TestMethod]
    public void ArgumentsKeepTheirCaseAndLoseTheExtraSpaces()
    {
        (string command, string[] arguments) = TelegramCommand.Parse("/slots  Long   5");

        Assert.AreEqual("slots", command);
        CollectionAssert.AreEqual(new[] { "Long", "5" }, arguments);
    }


    [TestMethod]
    public void AnEmptyMessageIsNoCommand()
    {
        Assert.AreEqual("", TelegramCommand.Parse("").command);
        Assert.AreEqual("", TelegramCommand.Parse(null).command);
        Assert.AreEqual("", TelegramCommand.Parse("   ").command);
    }


    [TestMethod]
    public void EveryCommandHasAUniqueName()
    {
        var duplicates = TelegramCommand.Commands.GroupBy(c => c.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        Assert.AreEqual(0, duplicates.Count, "twice: " + string.Join(", ", duplicates));
    }


    [TestMethod]
    public async Task TheHelpListsEveryCommand()
    {
        TelegramCommandReply reply = await TelegramCommand.ExecuteAsync("help", 1, ownChat: true);

        Assert.IsTrue(reply.Html, "the help has bold headings");
        foreach (TelegramCommand command in TelegramCommand.Commands)
        {
            // A valid name can be tapped ("/status"); the dot is shown as code
            string shown = command.Name == "." ? "<code>.</code> " : "/" + command.Name + " ";
            Assert.IsTrue(reply.Text.Contains(shown), $"{command.Name} is missing from the help");
        }
    }


    [TestMethod]
    public async Task TheHelpPutsEveryGroupUnderABoldHeadingInOrder()
    {
        TelegramCommandReply reply = await TelegramCommand.ExecuteAsync("help", 1, ownChat: true);

        string[] headings = reply.Text.Replace("\r\n", "\n").Split('\n').Where(l => l.StartsWith("<b>")).ToArray();
        Assert.AreEqual(4, headings.Length);
        StringAssert.Contains(headings[0], "Overview");
        StringAssert.Contains(headings[1], "Trader and signals");
        StringAssert.Contains(headings[2], "Market");
        StringAssert.Contains(headings[3], "Scanner");
        StringAssert.Contains(reply.Text, "/slots <i>[long|short count]</i> – ");
    }


    [TestMethod]
    public void TheMenuOnlyHoldsNamesTelegramAccepts()
    {
        var menu = TelegramCommand.MenuCommands();

        Assert.IsFalse(menu.Any(m => m.command == "."), "Telegram refuses a dot as a command name");
        Assert.IsTrue(menu.Any(m => m.command == "status"));
        foreach ((string command, string description) in menu)
        {
            Assert.IsTrue(command.Length is >= 1 and <= 32);
            Assert.IsTrue(command.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '_'), command);
            Assert.IsTrue(description.Length is >= 1 and <= 256, command);
        }
    }


    [TestMethod]
    public async Task AnUnknownCommandSaysSo()
    {
        TelegramCommandReply reply = await TelegramCommand.ExecuteAsync("statsu", 1, ownChat: true);

        StringAssert.Contains(reply.Text, "Unknown command 'statsu'");
    }


    [TestMethod]
    public async Task AForeignChatOnlyGetsItsChatId()
    {
        TelegramCommandReply status = await TelegramCommand.ExecuteAsync("status", 42, ownChat: false);
        TelegramCommandReply unknown = await TelegramCommand.ExecuteAsync("whatever", 42, ownChat: false);
        TelegramCommandReply chatId = await TelegramCommand.ExecuteAsync("/chatid", 42, ownChat: false);

        Assert.AreEqual("", status.Text, "a foreign chat must not see the status");
        Assert.AreEqual("", unknown.Text, "a foreign chat gets no answer at all");
        Assert.AreEqual("ChatId: 42", chatId.Text);
    }


    [TestMethod]
    public async Task ATypoBehindStartOrStopLeavesTheTraderAlone()
    {
        bool tradingBefore = GlobalData.Settings.Trading.Active;
        bool signalsBefore = GlobalData.Settings.Signal.Active;

        TelegramCommandReply start = await TelegramCommand.ExecuteAsync("start tradign", 1, ownChat: true);
        TelegramCommandReply stop = await TelegramCommand.ExecuteAsync("stop singals", 1, ownChat: true);

        StringAssert.Contains(start.Text, "Unknown argument 'tradign'");
        StringAssert.Contains(stop.Text, "Unknown argument 'singals'");
        Assert.AreEqual(tradingBefore, GlobalData.Settings.Trading.Active);
        Assert.AreEqual(signalsBefore, GlobalData.Settings.Signal.Active);
    }


    [TestMethod]
    public async Task ABareStartSwitchesNothingOn()
    {
        // Telegram sends "/start" by itself when the chat with the bot is opened
        bool tradingBefore = GlobalData.Settings.Trading.Active;

        TelegramCommandReply reply = await TelegramCommand.ExecuteAsync("/start", 1, ownChat: true);

        StringAssert.StartsWith(reply.Text, "Usage: start");
        Assert.AreEqual(tradingBefore, GlobalData.Settings.Trading.Active);
    }


    [TestMethod]
    [DataRow("slots long ten")]
    [DataRow("slots short -1")]
    [DataRow("slots long")]
    public async Task AWrongSlotCountIsRefusedWithAnAnswer(string text)
    {
        int longBefore = GlobalData.Settings.Trading.SlotsMaximalLong;
        int shortBefore = GlobalData.Settings.Trading.SlotsMaximalShort;

        TelegramCommandReply reply = await TelegramCommand.ExecuteAsync(text, 1, ownChat: true);

        Assert.AreEqual("Not a valid number!", reply.Text);
        Assert.AreEqual(longBefore, GlobalData.Settings.Trading.SlotsMaximalLong);
        Assert.AreEqual(shortBefore, GlobalData.Settings.Trading.SlotsMaximalShort);
    }


    [TestMethod]
    public async Task SlotsWithoutArgumentsShowsTheCurrentValues()
    {
        TelegramCommandReply reply = await TelegramCommand.ExecuteAsync("slots", 1, ownChat: true);

        StringAssert.StartsWith(reply.Text, "Slots long = ");
    }


    [TestMethod]
    public async Task StatusNoLongerStartsWithNotSupported()
    {
        TelegramCommandReply reply = await TelegramCommand.ExecuteAsync("status", 1, ownChat: true);

        Assert.IsFalse(reply.Text.Contains("not supported"));
        StringAssert.Contains(reply.Text, "Signal sound is");
    }


    /// <summary>
    /// HyperLiquid quotes in USDC and names its perpetual BTCUSDC.PERP. The old value command glued
    /// USDT behind the coin and found nothing there.
    /// </summary>
    [TestMethod]
    public async Task ValueFindsACoinOnAnExchangeThatQuotesInUsdcWithAProduct()
    {
        SettingsBasic savedSettings = GlobalData.Settings;
        TestExchange? savedExchange = GlobalData.ActiveExchange;
        try
        {
            GlobalData.Settings = new SettingsBasic();
            TestExchange exchange = new() { Id = 1, Name = "Test", TradingType = CryptoTradingType.Perpetual };
            GlobalData.ActiveExchange = exchange;
            AddSymbol(exchange, 1, "BTC", CryptoProduct.Perpetual, 95000m);
            AddSymbol(exchange, 2, "BTC", "HYNA", 1m);
            AddSymbol(exchange, 3, "SOL", CryptoProduct.Perpetual, 140m);

            TelegramCommandReply one = await TelegramCommand.ExecuteAsync("value btc", 1, ownChat: true);
            TelegramCommandReply pair = await TelegramCommand.ExecuteAsync("value btcusdc", 1, ownChat: true);
            TelegramCommandReply all = await TelegramCommand.ExecuteAsync("value", 1, ownChat: true);

            string btc = "BTCUSDC " + 95000m.ToString0();
            Assert.AreEqual(btc, one.Text);
            Assert.AreEqual(btc, pair.Text);
            StringAssert.Contains(all.Text, btc);
            StringAssert.Contains(all.Text, "SOLUSDC " + 140m.ToString0());
        }
        finally
        {
            GlobalData.Settings = savedSettings;
            GlobalData.ActiveExchange = savedExchange;
        }
    }


    private static void AddSymbol(TestExchange exchange, int id, string baseAsset, string product, decimal price)
    {
        CryptoSymbol symbol = new()
        {
            Id = id,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Name = baseAsset + "USDC" + CryptoProduct.Separator + product,
            Base = baseAsset,
            Quote = "USDC",
            Product = product,
            ExchangeName = "instrument-" + id,
            QuoteData = GlobalData.AddQuoteData("USDC"),
            Status = 1,
            LastPrice = price,
        };
        exchange.SymbolListId.Add(symbol.Id, symbol);
        exchange.SymbolListName.Add(symbol.Name, symbol);
        exchange.SymbolListExchangeName.Add(symbol.ExchangeName, symbol);
        exchange.Products.Add(product);
    }


    [TestMethod]
    public void AShortTextStaysOnePiece()
    {
        CollectionAssert.AreEqual(new[] { "a\nb" }, TelegramCommand.SplitMessage("a\r\nb\r\n"));
        Assert.AreEqual(0, TelegramCommand.SplitMessage("").Count);
    }


    [TestMethod]
    public void ALongTextIsCutOnLineBoundaries()
    {
        // 300 lines of 20 characters (plus newline) is 6300 characters
        string text = string.Join("\n", Enumerable.Range(0, 300).Select(i => $"line {i:D3} 12345678901"));

        List<string> parts = TelegramCommand.SplitMessage(text);

        Assert.AreEqual(2, parts.Count);
        Assert.IsTrue(parts.All(p => p.Length <= TelegramCommand.MaximumMessageLength));
        Assert.AreEqual(text, string.Join("\n", parts), "nothing may be lost or added");
        Assert.IsTrue(parts[1].StartsWith("line "), "the cut falls between two lines");
    }


    [TestMethod]
    public void ASingleLineLongerThanTheMaximumIsCutHard()
    {
        string text = new('x', 10);

        List<string> parts = TelegramCommand.SplitMessage(text, maximumLength: 4);

        CollectionAssert.AreEqual(new[] { "xxxx", "xxxx", "xx" }, parts);
    }
}
