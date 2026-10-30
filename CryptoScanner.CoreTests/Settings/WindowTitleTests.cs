using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.CoreTests.Settings;

/// <summary>
/// The window title the user writes (Settings.General.WindowTitle). Before 29-10-2026 the title was
/// fixed and only a caption could be added behind it; a settings file from that time has to give the
/// same title as it did.
/// </summary>
[TestClass]
[DoNotParallelize] // changes the exchange in the global settings
public class WindowTitleTests
{
    private string _exchangeName = "";

    [TestInitialize]
    public void Setup()
    {
        _exchangeName = GlobalData.Settings.General.ExchangeName;
        GlobalData.Settings.General.ExchangeName = "Bybit Spot";
    }

    [TestCleanup]
    public void Cleanup()
    {
        GlobalData.Settings.General.ExchangeName = _exchangeName;
    }

    [TestMethod]
    public void AnEmptyTitle_IsTheTitleEveryScannerHadBefore()
    {
        Assert.AreEqual($"CryptoScanBot {GlobalData.AppVersion} Bybit Spot", GlobalData.FormatTitle(""));
        Assert.AreEqual($"CryptoScanBot {GlobalData.AppVersion} Bybit Spot", GlobalData.FormatTitle("   "));
    }

    [TestMethod]
    public void ThePlaceholders_AreFilledIn_WhateverTheCase()
    {
        Assert.AreEqual($"Main scanner Bybit Spot v{GlobalData.AppVersion}",
            GlobalData.FormatTitle("Main scanner {Exchange} v{VERSION}"));
        Assert.AreEqual("Just a name", GlobalData.FormatTitle("Just a name"));
    }

    [TestMethod]
    public void AnOldExtraCaption_GivesTheSameTitleAsBefore()
    {
        SettingsGeneral general = new() { ExtraCaption = "test 2 ", ExchangeName = "Bybit Spot" };

        GlobalData.MoveExtraCaptionIntoWindowTitle(general);

        Assert.AreEqual("", general.ExtraCaption);
        Assert.AreEqual($"CryptoScanBot {GlobalData.AppVersion} Bybit Spot test 2", GlobalData.FormatTitle(general.WindowTitle));
    }

    /// <summary>
    /// A title of its own wins, and the old caption goes anyway: left behind it would come back the
    /// moment the user empties the title to get the default.
    /// </summary>
    [TestMethod]
    public void ATitleOfItsOwn_Wins_AndTheOldCaptionIsCleared()
    {
        SettingsGeneral general = new() { ExtraCaption = "old", WindowTitle = "New {exchange}" };

        GlobalData.MoveExtraCaptionIntoWindowTitle(general);

        Assert.AreEqual("New {exchange}", general.WindowTitle);
        Assert.AreEqual("", general.ExtraCaption);
    }
}
