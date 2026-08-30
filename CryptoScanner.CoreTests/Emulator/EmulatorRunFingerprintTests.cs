using CryptoScanner.Emulator.Engine;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The queue skips an entry whose configuration checksum matches a completed run from the same
/// build. What the checksum ignores decides whether that works: the run label is free-form text
/// naming the experiment, so a repeat under a new name has to hash the same, while anything that
/// reaches the replay has to change the hash.
/// <para>
/// Measured on the 540 runs in Session1: 111 of them reproduce an earlier run exactly. Five of
/// those are from the queue era - runs 484 to 488 repeat 479 to 483 - and cost 72 minutes for
/// numbers that were already on the row.
/// </para>
/// </summary>
[TestClass]
public class EmulatorRunFingerprintTests : TestBase
{
    private const string Settings = "{\"Trading\":{\"StopLossPercentage\":4}}";

    private static string Config(string label, string interval = "1m") =>
        "{\"ExchangeName\":\"Binance Perpetual\",\"BaseInterval\":\"" + interval + "\""
        + ",\"Label\":\"" + label + "\",\"SortColumn\":3,\"SortDescending\":true}";


    [TestMethod]
    public void TheSameRunUnderAnotherLabelHashesTheSame()
    {
        // Exactly the case that made run 487 repeat run 482: identical settings, and the label is
        // the only thing a queue entry is guaranteed to differ in.
        string a = EmulatorRunFingerprint.Compute(Config("G 3m: alleen 3m"), Settings);
        string b = EmulatorRunFingerprint.Compute(Config("AB alleen 3m op de winnaar"), Settings);

        Assert.AreEqual(a, b);
    }


    [TestMethod]
    public void TheGridSortOrderDoesNotChangeTheChecksum()
    {
        string a = EmulatorRunFingerprint.Compute(
            "{\"ExchangeName\":\"Binance Perpetual\",\"SortColumn\":1,\"SortDescending\":false}", Settings);
        string b = EmulatorRunFingerprint.Compute(
            "{\"ExchangeName\":\"Binance Perpetual\",\"SortColumn\":9,\"SortDescending\":true}", Settings);

        Assert.AreEqual(a, b);
    }


    [TestMethod]
    public void ADifferentBaseIntervalIsADifferentRun()
    {
        // The base interval decides how often the strategy is evaluated AND how orders fill, so two
        // runs that differ only in this are two measurements and not a repeat.
        string a = EmulatorRunFingerprint.Compute(Config("zelfde naam", "1m"), Settings);
        string b = EmulatorRunFingerprint.Compute(Config("zelfde naam", "15m"), Settings);

        Assert.AreNotEqual(a, b);
    }


    [TestMethod]
    public void ADifferentSettingsSnapshotIsADifferentRun()
    {
        string a = EmulatorRunFingerprint.Compute(Config("zelfde naam"), Settings);
        string b = EmulatorRunFingerprint.Compute(Config("zelfde naam"), "{\"Trading\":{\"StopLossPercentage\":3}}");

        Assert.AreNotEqual(a, b);
    }


    [TestMethod]
    public void AMissingSettingsSnapshotIsNotTheSameAsAnEmptyOne()
    {
        // Rows written before the SettingsJson column existed carry null. Those runs cannot be
        // compared on their settings at all, so they must not collide with a run that has none.
        string a = EmulatorRunFingerprint.Compute(Config("zelfde naam"), null);
        string b = EmulatorRunFingerprint.Compute(Config("zelfde naam"), Settings);

        Assert.AreNotEqual(a, b);
    }


    [TestMethod]
    public void UnparsableConfigurationStillProducesAChecksum()
    {
        // A malformed blob must make the run look unique rather than throw - the alternative is a
        // crash halfway through an overnight queue.
        string a = EmulatorRunFingerprint.Compute("not json at all", Settings);
        string b = EmulatorRunFingerprint.Compute("not json either", Settings);

        Assert.AreNotEqual(a, b);
        Assert.AreEqual(64, a.Length);
    }


    [TestMethod]
    public void ChangingTheDuplicateWindowDoesNotChangeTheChecksum()
    {
        // The window is a setting for this check, not something the replay reads. If it counted,
        // widening it would invalidate every checksum at the moment it is meant to start matching.
        string a = EmulatorRunFingerprint.Compute(
            "{\"ExchangeName\":\"Binance Perpetual\",\"DuplicateCheckDays\":14}", Settings);
        string b = EmulatorRunFingerprint.Compute(
            "{\"ExchangeName\":\"Binance Perpetual\",\"DuplicateCheckDays\":60}", Settings);

        Assert.AreEqual(a, b);
    }


    [TestMethod]
    public void WithoutAWindowTheCheckReachesBackNoFurtherThanTheBuild()
    {
        // A run from before the current build may have been produced by different code, so it does
        // not count as already measured. Runs 483 and 488 have byte-identical settings and produced
        // -11.20 and +437.99 two hours apart, with a repaired signal condition in between.
        DateTime? since = EmulatorRunFingerprint.GetRecentSince(0);

        Assert.IsNotNull(since);
        Assert.IsTrue(since <= DateTime.UtcNow, "the build cannot lie in the future");
        Assert.IsTrue(since > DateTime.UtcNow.AddYears(-5), "a missing build date must not disable the check entirely");
    }


    [TestMethod]
    public void AWindowInDaysWidensThePeriodBeyondTheBuild()
    {
        // The point of the setting: the emulator is rebuilt often, so on the build alone the window
        // is empty and the first queue after every rebuild compares against nothing.
        DateTime? build = EmulatorRunFingerprint.GetRecentSince(0);
        DateTime? wide = EmulatorRunFingerprint.GetRecentSince(3650);

        Assert.IsNotNull(build);
        Assert.IsNotNull(wide);
        Assert.IsTrue(wide < build, "ten years back has to reach further than the build");
    }


    [TestMethod]
    public void ANegativeWindowSwitchesTheCheckOff()
    {
        Assert.IsNull(EmulatorRunFingerprint.GetRecentSince(-1));
    }
}
