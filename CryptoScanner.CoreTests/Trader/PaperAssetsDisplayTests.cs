using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.CoreTests;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// How the paper-assets screen shows an amount. Both screens read this from Core, so the Avalonia
/// window and the Photino dialog cannot drift apart on it.
/// </summary>
[TestClass]
public class PaperAssetsDisplayTests : TestBase
{
    [TestInitialize]
    public void Setup()
    {
        InitTestSession();

        // USDT is a quote coin with two decimals - AddQuoteData is what puts it on N2.
        GlobalData.AddQuoteData("USDT");
    }


    /// <summary>
    /// A quote coin is rounded the way the rest of the application shows amounts in that coin, so a
    /// balance does not print every decimal a fill left behind.
    /// </summary>
    [TestMethod]
    public void AQuoteCoinIsRoundedToItsOwnDisplayFormat()
    {
        // Written as the rounded amount formatted by the same culture, so the test says something
        // about the rounding and nothing about the separators of the machine it runs on.
        Assert.AreEqual(9954.13m.ToString("N2"), PaperAssetsEditor.FormatAmount("USDT", 9954.126m));
    }


    /// <summary>
    /// Everything else in the list is a traded quantity, and those keep their decimals: 0,0108 BTC
    /// rounded to two decimals is zero.
    /// </summary>
    [TestMethod]
    public void AQuantityKeepsItsDecimals()
    {
        Assert.IsFalse(GlobalData.Settings.QuoteCoins.ContainsKey("TEST"), "not a quote coin, so not rounded");

        string text = PaperAssetsEditor.FormatAmount("TEST", 0.0108m);
        Assert.AreEqual(0.0108m.ToString0(), text);
        Assert.AreNotEqual(0.01m.ToString("N2"), text, "two decimals would leave nothing of it");
    }


    /// <summary>What the screen shows has to be readable back, separators and all.</summary>
    [TestMethod]
    public void AnAmountIsReadBackTheWayItIsShown()
    {
        string text = PaperAssetsEditor.FormatAmount("USDT", 1234567.89m);

        Assert.IsTrue(PaperAssetsEditor.TryParseAmount(text, out decimal amount));
        Assert.AreEqual(1234567.89m, amount);
    }


    /// <summary>Anything unreadable is refused, so the screen can leave the balance alone.</summary>
    [TestMethod]
    public void UnreadableTextIsRefused()
    {
        Assert.IsFalse(PaperAssetsEditor.TryParseAmount("", out _));
        Assert.IsFalse(PaperAssetsEditor.TryParseAmount("   ", out _));
        Assert.IsFalse(PaperAssetsEditor.TryParseAmount(null, out _));
        Assert.IsFalse(PaperAssetsEditor.TryParseAmount("ten thousand", out _));
    }
}
