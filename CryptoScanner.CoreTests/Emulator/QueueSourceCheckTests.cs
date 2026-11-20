using CryptoScanner.Analyzers.Dbr;
using CryptoScanner.Core.Core;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The source check of a queue entry that repeats an earlier run (13-11-2026). The repeats of Session0
/// differed from their source runs in 1.885 of 3.808 entries, mostly in the MA200: the old flat trading
/// key and the MA200 switch of the old strategy blocks had not been carried over, and the session value of
/// 09-11 (MA200 on, 10 candles) had been frozen in instead. These tests pin the rules the check compares by.
/// </summary>
[TestClass]
public class QueueSourceCheckTests : TestBase
{
    private static List<string> Paths(QueueSourceCheck.Result result) =>
        [.. result.Differences.Select(d => d.Split(' ')[0])];


    [TestMethod]
    public void TradingMa200FrozenInFromTheSessionIsReported()
    {
        string source = """{"Signal":{"AnalyzerSettings":{"stobb":{"EntryConditions":null}}},"Trading":{"EntryConditions":{"CheckPriceAboveMa200":false}}}""";
        string current = """{"Signal":{"AnalyzerSettings":{"stobb":{"EntryConditions":null}}},"Trading":{"EntryConditions":{"CheckPriceAboveMa200":true,"Ma200ConfirmationCandles":10}}}""";

        var result = QueueSourceCheck.Compare(current, source, "stobb");

        CollectionAssert.Contains(Paths(result), "EntryConditions.CheckPriceAboveMa200");
        CollectionAssert.Contains(Paths(result), "EntryConditions.Ma200ConfirmationCandles");
    }


    [TestMethod]
    public void OnlyTheSetThatIsReadCounts()
    {
        // mac reads its own set; the trading MA200 differs but is never read (sbm runs 22-24 and 41-43
        // differed exactly like this and gave the same result to the cent)
        string source = """{"Signal":{"AnalyzerSettings":{"mac":{"EntryConditions":{"CheckPriceAboveMa200":false}}}},"Trading":{"EntryConditions":{"CheckPriceAboveMa200":true,"Ma200ConfirmationCandles":10}}}""";
        string current = """{"Signal":{"AnalyzerSettings":{"mac":{"EntryConditions":{"CheckPriceAboveMa200":false}}}},"Trading":{"EntryConditions":{"CheckPriceAboveMa200":false}}}""";

        var result = QueueSourceCheck.Compare(current, source, "mac");

        Assert.AreEqual(0, result.Differences.Count, string.Join("; ", result.Differences));
    }


    [TestMethod]
    public void TheMa200OfAnOldStrategyBlockIsTheEntryConditionMa200()
    {
        // Session0 run 3202: "Signal.Bre.CheckPriceAboveMa200": true, the trading set off
        string source = """{"Signal":{"Bre":{"BandLength":20,"CheckPriceAboveMa200":true,"EntryConditions":null}},"Trading":{"EntryConditions":{"CheckPriceAboveMa200":false}}}""";
        string withoutMa200 = """{"Signal":{"AnalyzerSettings":{"dbr":{"BandLength":20,"EntryConditions":null}}},"Trading":{"EntryConditions":{"CheckPriceAboveMa200":false}}}""";
        string withMa200 = """{"Signal":{"AnalyzerSettings":{"dbr":{"BandLength":20,"EntryConditions":null}}},"Trading":{"EntryConditions":{"CheckPriceAboveMa200":true}}}""";

        var missing = QueueSourceCheck.Compare(withoutMa200, source, "dbr");
        var matching = QueueSourceCheck.Compare(withMa200, source, "dbr");

        CollectionAssert.AreEqual(new List<string> { "EntryConditions.CheckPriceAboveMa200" }, Paths(missing));
        Assert.AreEqual(0, matching.Differences.Count, string.Join("; ", matching.Differences));
    }


    [TestMethod]
    public void TheFlatTradingConditionsOfTheOldestRunsAreRead()
    {
        // The oldest Session0 runs kept the conditions on Trading itself
        string source = """{"Signal":{},"Trading":{"CheckPriceAboveMa200":false,"StopLossPercentage":3}}""";
        string current = """{"Signal":{},"Trading":{"EntryConditions":{"CheckPriceAboveMa200":true,"Ma200ConfirmationCandles":10},"StopLossPercentage":3}}""";

        var result = QueueSourceCheck.Compare(current, source, "stobb");

        CollectionAssert.AreEqual(new List<string> { "EntryConditions.CheckPriceAboveMa200", "EntryConditions.Ma200ConfirmationCandles" },
            Paths(result));
    }


    [TestMethod]
    public void AValueWhoseSwitchIsOffOnBothSidesDoesNotCount()
    {
        string source = """{"Trading":{"EntryConditions":{"CheckPriceAboveMa200":false,"Ma200ConfirmationCandles":0,"CheckTrendSecondaryDirection":false,"TrendSecondaryDirectionCount":2}}}""";
        string current = """{"Trading":{"EntryConditions":{"CheckPriceAboveMa200":false,"Ma200ConfirmationCandles":10,"CheckTrendSecondaryDirection":false,"TrendSecondaryDirectionCount":3}}}""";

        var result = QueueSourceCheck.Compare(current, source, "stobb");

        Assert.AreEqual(0, result.Differences.Count, string.Join("; ", result.Differences));
    }


    [TestMethod]
    public void AnInactiveFilterDoesNotCountEither()
    {
        string source = """{"Signal":{"Long":{"MarketRsi":{"Active":false,"Minimum":45,"Maximum":55}}}}""";
        string current = """{"Signal":{"Long":{"MarketRsi":{"Active":false,"Minimum":30,"Maximum":70}}}}""";

        Assert.AreEqual(0, QueueSourceCheck.Compare(current, source, "stobb").Differences.Count);
    }


    [TestMethod]
    public void TheOrderOfANameListDoesNotMatterButItsContentDoes()
    {
        string source = """{"Trading":{"Long":{"Interval":["1h","5m","15m","30m"]}}}""";
        string sameSet = """{"Trading":{"Long":{"Interval":["5m","15m","30m","1h"]}}}""";
        string allIntervals = """{"Trading":{"Long":{"Interval":["1d","1h","5m","15m","30m"]}}}""";

        Assert.AreEqual(0, QueueSourceCheck.Compare(sameSet, source, "dbr").Differences.Count);
        CollectionAssert.AreEqual(new List<string> { "Trading.Long.Interval" }, Paths(QueueSourceCheck.Compare(allIntervals, source, "dbr")));
    }


    [TestMethod]
    public void AnAcceptedPathIsNoLongerADifference()
    {
        string source = """{"Trading":{"DcaList":[{"Factor":200,"Percentage":2},{"Factor":1600,"Percentage":4}]}}""";
        string current = """{"Trading":{"DcaList":[{"Factor":200,"Percentage":2},{"Factor":400,"Percentage":4}]}}""";

        var refused = QueueSourceCheck.Compare(current, source, "dbr");
        var accepted = QueueSourceCheck.Compare(current, source, "dbr", accepted: ["Trading.DcaList"]);

        CollectionAssert.AreEqual(new List<string> { "Trading.DcaList[1].Factor" }, Paths(refused));
        Assert.AreEqual(0, accepted.Differences.Count);
        Assert.AreEqual(1, accepted.Accepted.Count);
    }


    [TestMethod]
    public void AOneSidedSwitchThatIsOnIsADifferenceOtherValuesAreOnlyCounted()
    {
        // Session0 blocks carry settings this build no longer has, like the DIDO of Bre
        string source = """{"Signal":{"Bre":{"BandLength":20,"DidoLength":20,"UseOldFilter":true}}}""";
        string current = """{"Signal":{"AnalyzerSettings":{"dbr":{"BandLength":20}}}}""";

        var result = QueueSourceCheck.Compare(current, source, "dbr", ["bre"]);

        CollectionAssert.AreEqual(new List<string> { "Plugin.UseOldFilter" }, Paths(result));
        CollectionAssert.Contains(result.NotComparable, "Plugin.DidoLength");
    }


    [TestMethod]
    public void TheFrameOfTheSessionAndRemovedFixedSettingsDoNotCount()
    {
        string source = """{"Trading":{"PaperAssetDefaults":[],"UseAssetManagement":false,"DcaOrderType":1,"Long":{"Strategy":["dbr","vbs","mac"]}},"Signal":{"Bbma":{"UseX":true}}}""";
        string current = """{"Trading":{"PaperAssetDefaults":[{"Name":"USDT","Total":100000}],"UseAssetManagement":true,"Long":{"Strategy":["dbr"]}},"Signal":{}}""";

        var result = QueueSourceCheck.Compare(current, source, "dbr", ["bre"]);

        // The Bbma block belongs to another strategy and was never read for dbr
        Assert.AreEqual(0, result.Differences.Count, string.Join("; ", result.Differences));
    }


    [TestMethod]
    public void TheSnapshotOfAnAppliedEntryMatchesItselfAndShowsAChange()
    {
        InitTestSession();
        RegisterAndEnablePlugin(new DbrPlugin());

        string Snapshot(EmulatorQueueEntry entry)
        {
            List<SignalGridExpander.Override> overrides = [];
            QueueBaseline.Install();
            try
            {
                overrides = QueueEntryApplier.Apply(entry);
                return QueueSourceCheck.CaptureSettingsJson();
            }
            finally
            {
                SignalGridExpander.Revert(overrides);
                QueueBaseline.Restore();
            }
        }

        EmulatorQueueEntry reference = new()
        {
            Algorithm = "dbr",
            StopLossPercentage = 4m,
            TradingOverrides = new() { ["EntryConditions.CheckPriceAboveMa200"] = JsonDocument.Parse("true").RootElement },
        };
        EmulatorQueueEntry variant = new()
        {
            Algorithm = "dbr",
            StopLossPercentage = 5m,
            TradingOverrides = new() { ["EntryConditions.CheckPriceAboveMa200"] = JsonDocument.Parse("true").RootElement },
        };

        string source = Snapshot(reference);
        var same = QueueSourceCheck.Compare(Snapshot(reference), source, "dbr", CryptoScanner.Core.Contracts.PluginManager.FindByName("dbr")?.FormerStrategyNames);
        var changed = QueueSourceCheck.Compare(Snapshot(variant), source, "dbr");

        Assert.AreEqual(0, same.Differences.Count, string.Join("; ", same.Differences));
        CollectionAssert.AreEquivalent(new List<string> { "Trading.StopLossLimitPercentage", "Trading.StopLossPercentage" }, Paths(changed));
    }


    /// <summary>
    /// Runs the real check over a real queue folder and writes a report. Only when the environment
    /// variable CRYPTOSCANBOT_QUEUE_CHECK points at a session folder (for example
    /// E:\CryptoScanBot\Data\Emulator\Session2); otherwise inconclusive, so a normal test run never
    /// touches the data.
    /// </summary>
    [TestMethod]
    public void RealQueueFolder()
    {
        string? folder = Environment.GetEnvironmentVariable("CRYPTOSCANBOT_QUEUE_CHECK");
        if (string.IsNullOrWhiteSpace(folder))
        {
            Assert.Inconclusive("Set CRYPTOSCANBOT_QUEUE_CHECK to a session folder to run the source check on its queue");
            return;
        }

        InitTestSession();
        CryptoScanner.Analyzers.AnalyzerRegistration.RegisterAll();
        string savedFolder = GlobalData.AppDataFolder;
        GlobalData.AppDataFolder = folder;
        List<string> report = [];
        int checkedEntries = 0, refused = 0;
        try
        {
            string queue = Path.Combine(folder, EmulatorQueueFolder.FolderName);
            foreach (string file in Directory.GetFiles(queue, "*.json").OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                List<EmulatorQueueEntry> entries = EmulatorQueueFile.LoadFrom(file);
                for (int i = 0; i < entries.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(entries[i].SourceRun))
                        continue;
                    checkedEntries++;
                    string? reason;
                    try
                    {
                        reason = QueueSourceCheck.CheckEntry(entries[i]);
                    }
                    catch (Exception ex)
                    {
                        reason = $"check failed: {ex.GetType().Name}: {ex.Message}";
                    }
                    if (reason == null)
                        continue;
                    refused++;
                    report.Add($"{Path.GetFileName(file)}\t{i + 1}\t{entries[i].Algorithm}\t{entries[i].SourceRun}\t{reason}");
                }
            }
        }
        finally
        {
            GlobalData.AppDataFolder = savedFolder;
        }

        string output = Environment.GetEnvironmentVariable("CRYPTOSCANBOT_QUEUE_CHECK_REPORT")
            ?? Path.Combine(Path.GetTempPath(), "queue-source-check.tsv");
        File.WriteAllLines(output, [$"checked\t{checkedEntries}\trefused\t{refused}", .. report]);
        Console.WriteLine($"{checkedEntries} entries checked, {refused} refused, report {output}");
    }
}
