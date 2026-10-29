using CryptoScanner.Core.Core;

using CryptoScanner.Emulator;
using CryptoScanner.Emulator.Engine;
using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// What the run-config window does to the fields it does NOT edit.
/// <para>
/// The window edits the symbols, the period, the label, the base interval, the start capital and the
/// paper balances. The file holds four more: the algorithm selection of the sweep dialog, the
/// barometer switch, the window of the duplicate check and the sort of the results grid, each written
/// by a different place. The window used to build a fresh configuration and save that, so pressing OK
/// silently put all four back on their default - the same class of mistake as the queue dropping
/// UseAssetManagement (point 57).
/// </para>
/// </summary>
[TestClass]
[DoNotParallelize]
public class RunConfigWindowKeepsFieldsTests : TestBase
{
    private string? _saved;

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        _saved = File.Exists(RunConfigFile.FilePath) ? File.ReadAllText(RunConfigFile.FilePath) : null;
    }

    [TestCleanup]
    public void Restore()
    {
        if (_saved != null)
            File.WriteAllText(RunConfigFile.FilePath, _saved);
        else if (File.Exists(RunConfigFile.FilePath))
            File.Delete(RunConfigFile.FilePath);
    }


    /// <summary>The state a queue, a sweep dialog and the results grid left behind on disk.</summary>
    private static void ArrangeStoredConfig()
    {
        RunConfigFile.Save(new EmulatorRunConfig
        {
            ExchangeName = GlobalData.ActiveExchange?.Name ?? "",
            Symbols = ["TESTUSDT"],
            FromDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ToDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            Label = "eerdere-run",
            SelectedAlgorithms = ["dbr", "vbs"],
            CalculateBarometer = false,
            DuplicateCheckDays = 60,
            SortColumn = "Profit",
            SortDescending = true,
        });
    }


    [TestMethod]
    public void PressingOk_KeepsTheFieldsTheWindowDoesNotEdit()
    {
        ArrangeStoredConfig();

        RunConfigViewModel viewModel = new();
        Assert.IsTrue(viewModel.TryBuild(out EmulatorRunConfig config), viewModel.ValidationMessage);

        CollectionAssert.AreEqual(new List<string> { "dbr", "vbs" }, config.SelectedAlgorithms,
            "de keuze uit het sweep-dialoog hoort te blijven staan");
        Assert.IsFalse(config.CalculateBarometer, "de barometer-schakelaar hoort te blijven staan");
        Assert.AreEqual(60, config.DuplicateCheckDays, "het venster van de duplicaatcontrole hoort te blijven staan");
        Assert.AreEqual("Profit", config.SortColumn, "de sortering van het resultatenrooster hoort te blijven staan");
        Assert.IsTrue(config.SortDescending);
    }


    /// <summary>
    /// And the fields it does edit still arrive, otherwise keeping the rest would be a step back.
    /// </summary>
    [TestMethod]
    public void PressingOk_StillWritesWhatTheWindowEdits()
    {
        ArrangeStoredConfig();

        RunConfigViewModel viewModel = new()
        {
            Label = "nieuwe-run",
            SelectedBaseInterval = "5m",
        };
        Assert.IsTrue(viewModel.TryBuild(out EmulatorRunConfig config), viewModel.ValidationMessage);

        Assert.AreEqual("nieuwe-run", config.Label);
        Assert.AreEqual("5m", config.BaseInterval);
        // The start capital and asset management are settings since 26-10-2026, not fields of the run
        Assert.IsNull(config.StartCapital);
        Assert.IsNull(config.UseAssetManagement);
        CollectionAssert.Contains(config.Symbols, "TESTUSDT", "het symbool uit de opgeslagen keuze blijft aangevinkt");
    }


    /// <summary>
    /// A file from before 26-10-2026 with the retired StartCapital and UseAssetManagement. Loading it
    /// moves those to the settings and saves the file again - and that save ran while the file was
    /// still open for reading, so Load fell back on the one-coin placeholder and the run-config window
    /// saved that over the 178 coins of HyperLiquid Session1.
    /// </summary>
    [TestMethod]
    public void LoadingAFileWithTheRetiredFields_KeepsItsCoins()
    {
        if (!GlobalData.Settings.QuoteCoins.ContainsKey("USDT"))
            GlobalData.Settings.QuoteCoins.Add("USDT", new CryptoScanner.Core.Model.CryptoQuoteData { Name = "USDT" });
        GlobalData.Settings.QuoteCoins["USDT"].FetchCandles = true;
        var savedDefaults = GlobalData.Settings.Trading.PaperAssetDefaults;
        GlobalData.Settings.Trading.PaperAssetDefaults = [];
        Action savedSave = RunConfigFile.SaveSettings;
        RunConfigFile.SaveSettings = () => { };
        try
        {
            File.WriteAllText(RunConfigFile.FilePath, """
                {
                  "ExchangeName": "Binance Perpetual",
                  "Symbols": ["AAAUSDT.PERP", "BBBUSDT.PERP", "CCCUSDT.PERP"],
                  "FromDate": "2026-01-01",
                  "ToDate": "2026-10-01",
                  "Label": "",
                  "BaseInterval": "1m",
                  "StartCapital": 100000,
                  "UseAssetManagement": true
                }
                """);

            EmulatorRunConfig config = RunConfigFile.Load();

            Assert.AreEqual(3, config.Symbols.Count, "the coins of the file, not the placeholder");
            Assert.IsNull(config.StartCapital);
            Assert.IsTrue(GlobalData.Settings.Trading.PaperAssetDefaults.Any(d => d.Name == "USDT" && d.Total == 100000m),
                "the start capital moved to the settings");

            string written = File.ReadAllText(RunConfigFile.FilePath);
            StringAssert.Contains(written, "CCCUSDT.PERP");
            Assert.IsFalse(written.Contains("StartCapital"), "the retired field is gone from the file");
        }
        finally
        {
            GlobalData.Settings.Trading.PaperAssetDefaults = savedDefaults;
            RunConfigFile.SaveSettings = savedSave;
        }
    }
}
