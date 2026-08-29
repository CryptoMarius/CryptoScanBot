using CryptoScanner.Core.Telegram;

namespace CryptoScanner.CoreTests.Telegram;

/// <summary>
/// The Start/Stop button of the settings screen reads <see cref="ThreadTelegramBot.IsRunning"/> and
/// the answer of Start to decide what it shows. A refused token therefore has to come back as a
/// plain false with the bot left alone, and not as an exception or as a bot that claims to be
/// running while its loop has already ended.
///
/// These tests deliberately stay away from the network: they use tokens that never reach Telegram.
/// </summary>
[TestClass]
public class ThreadTelegramBotTests
{
    [TestCleanup]
    public void Cleanup()
    {
        ThreadTelegramBot.Token = "";
        ThreadTelegramBot.ChatId = "";
    }


    [TestMethod]
    public async Task EmptyTokenDoesNotStartTheBot()
    {
        bool started = await ThreadTelegramBot.Start("", "");

        Assert.IsFalse(started, "there is nothing to start without a token");
        Assert.IsFalse(ThreadTelegramBot.IsRunning, "the button would otherwise offer to stop a bot that is not there");
    }


    [TestMethod]
    public async Task TokenThatIsNotShapedLikeATokenDoesNotStartTheBot()
    {
        // A token looks like 1234567890:AAF-abc.. - this one has no colon at all, so it never has to
        // leave the machine to be refused
        bool started = await ThreadTelegramBot.Start("plain-nonsense", "710219603");

        Assert.IsFalse(started, "a token of the wrong shape should be refused before anything is sent");
        Assert.IsFalse(ThreadTelegramBot.IsRunning);
    }


    [TestMethod]
    public async Task StoppingAStoppedBotIsHarmless()
    {
        await ThreadTelegramBot.StopAsync();
        await ThreadTelegramBot.StopAsync();

        Assert.IsFalse(ThreadTelegramBot.IsRunning);
    }


    [TestMethod]
    public async Task AFailedStartStillRemembersWhatWasTried()
    {
        // ScannerSession compares the settings against these two to decide whether the bot has to be
        // restarted. Without the token being kept, a refused token would be retried on every pass.
        await ThreadTelegramBot.Start("plain-nonsense", "710219603");

        Assert.AreEqual("plain-nonsense", ThreadTelegramBot.Token);
        Assert.AreEqual("710219603", ThreadTelegramBot.ChatId);
    }
}
