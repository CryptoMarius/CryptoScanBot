using System.Text.RegularExpressions;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The chart widget is one big JavaScript object literal. A member that is written twice in it does
/// not fail anywhere: the last one silently wins, and the first one - usually the new one - is
/// simply never called. That cost a long hunt once, with the chart drawing shapes that no longer
/// existed anywhere in the source, so the file is checked for it here.
/// </summary>
[TestClass]
public class ChartWidgetScriptTests
{
    /// <summary>
    /// The repository root, found by walking up from the test assembly until the chart script is
    /// there. Null when the sources are not next to the binaries, which is why the test skips
    /// itself rather than failing in that case.
    /// </summary>
    private static string? FindScript()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName,
                "CryptoScanner.UI", "wwwroot", "js", "chart-widget.js");
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }
        return null;
    }


    [TestMethod]
    public void NoMemberIsDefinedTwice()
    {
        // arrange
        string? script = FindScript();
        if (script == null)
        {
            Assert.Inconclusive("chart-widget.js was not found next to the test assembly");
            return;
        }

        // act - the members of the widget itself sit at exactly one indent; anything deeper belongs
        // to a nested object and may well repeat a name legitimately.
        Dictionary<string, List<int>> seen = [];
        string[] lines = File.ReadAllLines(script);
        for (int i = 0; i < lines.Length; i++)
        {
            Match match = Regex.Match(lines[i], @"^    ([A-Za-z_][A-Za-z0-9_]*):");
            if (!match.Success)
                continue;

            string name = match.Groups[1].Value;
            if (!seen.TryGetValue(name, out List<int>? at))
                seen[name] = at = [];
            at.Add(i + 1);
        }

        // assert
        List<string> duplicates = [.. seen
            .Where(x => x.Value.Count > 1)
            .Select(x => x.Key + " on lines " + string.Join(", ", x.Value))];
        Assert.AreEqual(0, duplicates.Count,
            "chart-widget.js defines these members more than once, and only the last one is used: "
            + string.Join("; ", duplicates));
    }
}
