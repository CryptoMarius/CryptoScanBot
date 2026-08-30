using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.CoreTests.Telegram;

/// <summary>
/// Every message on its way to Telegram passes <see cref="SettingsTelegram.IsAllowed"/>, so this is
/// the one place where a wrong answer silently costs you notifications (or floods the chat).
/// </summary>
[TestClass]
public class SettingsTelegramTests
{
    [TestMethod]
    public void ASettingsFileWithoutTheNewSwitchesKeepsSendingWhatItSentBefore()
    {
        // The trader and the system messages had no checkbox and were sent unconditionally, so the
        // defaults have to keep an existing installation behaving the way it did. Signals are the
        // exception: they already had a switch and it is off by default.
        SettingsTelegram settings = new();

        Assert.IsTrue(settings.IsAllowed(CryptoTelegramCategory.OrderPlaced));
        Assert.IsTrue(settings.IsAllowed(CryptoTelegramCategory.OrderFilled));
        Assert.IsTrue(settings.IsAllowed(CryptoTelegramCategory.System));
        Assert.IsFalse(settings.IsAllowed(CryptoTelegramCategory.Signal));
    }


    [TestMethod]
    public void TheMasterSwitchOverrulesEveryCategory()
    {
        SettingsTelegram settings = new()
        {
            Enabled = false,
            SendSignalsToTelegram = true,
            SendOrdersToTelegram = true,
            SendFilledOrdersToTelegram = true,
            SendSystemMessagesToTelegram = true,
        };

        Assert.IsFalse(settings.IsAllowed(CryptoTelegramCategory.Signal));
        Assert.IsFalse(settings.IsAllowed(CryptoTelegramCategory.OrderPlaced));
        Assert.IsFalse(settings.IsAllowed(CryptoTelegramCategory.OrderFilled));
        Assert.IsFalse(settings.IsAllowed(CryptoTelegramCategory.System));
    }


    [TestMethod]
    public void TheTestButtonIgnoresEverySwitch()
    {
        // Pressing Test after starting the bot by hand has to say something, otherwise there is no
        // way to tell a wrong token from a switched off category
        SettingsTelegram settings = new()
        {
            Enabled = false,
            SendSignalsToTelegram = false,
            SendOrdersToTelegram = false,
            SendFilledOrdersToTelegram = false,
            SendSystemMessagesToTelegram = false,
        };

        Assert.IsTrue(settings.IsAllowed(CryptoTelegramCategory.Test));
    }


    [TestMethod]
    public void SilencingTheTraderLeavesTheSignalsAlone()
    {
        SettingsTelegram settings = new()
        {
            SendSignalsToTelegram = true,
            SendOrdersToTelegram = false,
            SendFilledOrdersToTelegram = false,
        };

        Assert.IsTrue(settings.IsAllowed(CryptoTelegramCategory.Signal));
        Assert.IsFalse(settings.IsAllowed(CryptoTelegramCategory.OrderPlaced));
        Assert.IsFalse(settings.IsAllowed(CryptoTelegramCategory.OrderFilled));
        Assert.IsTrue(settings.IsAllowed(CryptoTelegramCategory.System), "the system messages have their own switch");
    }


    [TestMethod]
    public void PlacedAndFilledOrdersAreSwitchedSeparately()
    {
        SettingsTelegram settings = new()
        {
            SendOrdersToTelegram = false,
            SendFilledOrdersToTelegram = true,
        };

        Assert.IsFalse(settings.IsAllowed(CryptoTelegramCategory.OrderPlaced), "every attempt would otherwise still be reported");
        Assert.IsTrue(settings.IsAllowed(CryptoTelegramCategory.OrderFilled), "the confirmations are the point of leaving this one on");
    }
}
