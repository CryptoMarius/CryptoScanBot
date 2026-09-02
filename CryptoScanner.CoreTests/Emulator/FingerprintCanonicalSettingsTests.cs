using CryptoScanner.Analyzers.CandlePattern;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;
using System.Text.Json.Nodes;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// Tests for the part of <see cref="EmulatorRunFingerprint"/> that decides whether two runs count
/// as the same measurement. The case they exist for: a build adds or removes a setting, and every
/// checksum from before it becomes worthless - a restarted queue then replays hours of runs whose
/// numbers are already in the database.
/// </summary>
[TestClass]
public class FingerprintCanonicalSettingsTests
{
    private const string Config = """{"Label":"x","FromDate":"2026-01-01","ToDate":"2026-07-30"}""";


    /// <summary>The settings snapshot with one branch replaced, so a test can vary one value.</summary>
    private static string Settings(Action<JsonObject> change)
    {
        var settings = JsonNode.Parse(JsonSerializer.Serialize(
            new SettingsBasic(), JsonTools.JsonSerializerIndented))!.AsObject();
        change(settings);
        return settings.ToJsonString();
    }


    [TestMethod]
    public void AddedSettingAtItsDefault_KeepsTheChecksum()
    {
        // The old build simply did not have the key; the new one writes it at its default. Both are
        // the same measurement, so both have to hash the same.
        string oldBuild = Settings(s => ((JsonObject)s["Trading"]!).Remove("StopLossPercentage"));
        string newBuild = Settings(_ => { });

        Assert.AreEqual(EmulatorRunFingerprint.Compute(Config, oldBuild),
                        EmulatorRunFingerprint.Compute(Config, newBuild));
    }


    [TestMethod]
    public void RemovedSettingThatSatAtItsDefault_KeepsTheChecksum()
    {
        // Same thing the other way round: the key left the code, the older snapshot still has it.
        string withKey = Settings(_ => { });
        string withoutKey = Settings(s => ((JsonObject)s["Trading"]!).Remove("EntryRemoveTime"));

        Assert.AreEqual(EmulatorRunFingerprint.Compute(Config, withKey),
                        EmulatorRunFingerprint.Compute(Config, withoutKey));
    }


    [TestMethod]
    public void ChangedSetting_ChangesTheChecksum()
    {
        // The whole point of the check: a run that was measured differently must not be skipped.
        string reference = Settings(_ => { });
        string changed = Settings(s => ((JsonObject)s["Trading"]!)["StopLossPercentage"] = 6m);

        Assert.AreNotEqual(EmulatorRunFingerprint.Compute(Config, reference),
                           EmulatorRunFingerprint.Compute(Config, changed));
    }


    [TestMethod]
    public void AnUnknownSetting_IsIgnored()
    {
        // A setting that no longer exists in the code is dropped on the way in, whatever value it
        // held. Accepted deliberately: that run WAS measured differently, but so is every run from
        // before any code change that did not touch a setting, and the check never guarded those.
        string oldBuild = Settings(s => ((JsonObject)s["Trading"]!)["SomethingThatLeftTheCode"] = 6m);
        string newBuild = Settings(_ => { });

        Assert.AreEqual(EmulatorRunFingerprint.Compute(Config, oldBuild),
                        EmulatorRunFingerprint.Compute(Config, newBuild));
    }


    [TestMethod]
    public void MovingAPropertyWithinItsClass_KeepsTheChecksum()
    {
        // Hashing the raw text would see two different documents here; after the round trip both
        // come out in the order the current class writes.
        string reference = Settings(_ => { });
        string reordered = Settings(s =>
        {
            var trading = (JsonObject)s["Trading"]!;
            JsonNode? value = trading["StopLossPercentage"]!.DeepClone();
            trading.Remove("StopLossPercentage");
            trading.Insert(0, "StopLossPercentage", value);
        });

        Assert.AreEqual(EmulatorRunFingerprint.Compute(Config, reference),
                        EmulatorRunFingerprint.Compute(Config, reordered));
    }


    [TestMethod]
    public void DifferentConfiguration_ChangesTheChecksum()
    {
        string settings = Settings(_ => { });
        const string other = """{"Label":"x","FromDate":"2026-02-01","ToDate":"2026-07-30"}""";

        Assert.AreNotEqual(EmulatorRunFingerprint.Compute(Config, settings),
                           EmulatorRunFingerprint.Compute(other, settings));
    }


    [TestMethod]
    public void TheLabelIsNotPartOfIt()
    {
        string settings = Settings(_ => { });
        const string renamed = """{"Label":"a different name","FromDate":"2026-01-01","ToDate":"2026-07-30"}""";

        Assert.AreEqual(EmulatorRunFingerprint.Compute(Config, settings),
                        EmulatorRunFingerprint.Compute(renamed, settings));
    }


    [TestMethod]
    public void SettingAddedToAnAnalyzer_KeepsTheChecksum()
    {
        // The case the first attempt missed. SettingsSignal keeps the analyzer blocks as raw
        // JsonElement on purpose, so a plain round trip through SettingsBasic hands them back
        // verbatim and a property added to a plugin still changed the checksum - which is what
        // replayed the anchor on 02-09-2026.
        TestBase.RegisterPlugin(new CandlePatternPlugin());

        var withProperty = JsonNode.Parse(JsonSerializer.Serialize(
            new CandlePatternStrategySettings(), JsonTools.JsonSerializerIndented))!.AsObject();
        var withoutProperty = withProperty.DeepClone().AsObject();
        withoutProperty.Remove(nameof(CandlePatternStrategySettings.RequireZone));

        string newBuild = Settings(s => ((JsonObject)s["Signal"]!["AnalyzerSettings"]!)["candlepattern"] = withProperty);
        string oldBuild = Settings(s => ((JsonObject)s["Signal"]!["AnalyzerSettings"]!)["candlepattern"] = withoutProperty);

        Assert.AreEqual(EmulatorRunFingerprint.Compute(Config, oldBuild),
                        EmulatorRunFingerprint.Compute(Config, newBuild));
    }


    [TestMethod]
    public void ChangedAnalyzerSetting_ChangesTheChecksum()
    {
        TestBase.RegisterPlugin(new CandlePatternPlugin());

        var reference = JsonNode.Parse(JsonSerializer.Serialize(
            new CandlePatternStrategySettings(), JsonTools.JsonSerializerIndented))!.AsObject();
        var changed = reference.DeepClone().AsObject();
        changed[nameof(CandlePatternStrategySettings.RequireZone)] = new JsonArray("fvg");

        string a = Settings(s => ((JsonObject)s["Signal"]!["AnalyzerSettings"]!)["candlepattern"] = reference);
        string b = Settings(s => ((JsonObject)s["Signal"]!["AnalyzerSettings"]!)["candlepattern"] = changed);

        Assert.AreNotEqual(EmulatorRunFingerprint.Compute(Config, a),
                           EmulatorRunFingerprint.Compute(Config, b));
    }


    [TestMethod]
    public void MalformedSettings_DoNotThrow()
    {
        // A blob that cannot be parsed makes the run look unique rather than blowing up: measuring
        // one run twice is cheap, skipping one that was never measured is not.
        Assert.AreNotEqual(EmulatorRunFingerprint.Compute(Config, "{not json"),
                           EmulatorRunFingerprint.Compute(Config, Settings(_ => { })));
    }
}
