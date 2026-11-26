using CryptoScanner.Core.Core;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Settings;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Settings;

/// <summary>
/// The trend zigzag settings are fixed values (see <see cref="SettingsTrend"/>). A settings file of a
/// field user (20-11-2026) still carried Primary with TrendType Secondary from the time the screens
/// were visible; the primary trend then stayed empty and every signal showed "-" in its trend columns.
/// </summary>
[TestClass]
public class SettingsTrendFixedTests
{
    // The "Trend" block exactly as it was in that settings file
    private const string FileWithOldTrendBlock = """
        {
          "Trend": {
            "Primary": { "UseHighLow": false, "TrendType": 1 },
            "Secondary": { "UseHighLow": false, "TrendType": 1 }
          }
        }
        """;

    [TestMethod]
    public void OldTrendBlockInTheFileIsIgnored()
    {
        SettingsBasic settings = JsonSerializer.Deserialize<SettingsBasic>(FileWithOldTrendBlock, JsonTools.DeSerializerOptions)!;

        Assert.AreEqual(TrendType.Primary, settings.Trend.Primary.TrendType);
        Assert.IsTrue(settings.Trend.Primary.UseHighLow);
        Assert.AreEqual(TrendType.Secondary, settings.Trend.Secondary.TrendType);
        Assert.IsTrue(settings.Trend.Secondary.UseHighLow);
    }

    [TestMethod]
    public void TrendIsNotWrittenToTheFile()
    {
        string json = JsonSerializer.Serialize(new SettingsBasic(), JsonTools.JsonSerializerIndented);

        using JsonDocument document = JsonDocument.Parse(json);
        Assert.IsFalse(document.RootElement.TryGetProperty("Trend", out _));
    }
}
