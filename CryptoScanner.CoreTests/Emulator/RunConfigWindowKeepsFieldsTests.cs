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
            StartCapital = 25_000m,
            UseAssetManagement = false,
        };
        Assert.IsTrue(viewModel.TryBuild(out EmulatorRunConfig config), viewModel.ValidationMessage);

        Assert.AreEqual("nieuwe-run", config.Label);
        Assert.AreEqual("5m", config.BaseInterval);
        Assert.AreEqual(25_000m, config.StartCapital);
        Assert.IsFalse(config.UseAssetManagement);
        CollectionAssert.Contains(config.Symbols, "TESTUSDT", "het symbool uit de opgeslagen keuze blijft aangevinkt");
    }
}
