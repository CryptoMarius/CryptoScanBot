using System.Globalization;
using System.Text;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// A DCA ladder of one signal or position, written as text so it can be stored in a single column:
/// "percentage:factor" per level, separated by ";", invariant culture, near to far. For example
/// "2.13:200;4.5:400" is a DCA of 200% at 2.13% from the entry and one of 400% at 4.5%.
/// <para>
/// A strategy that places its DCA levels itself (on its own price levels) hands the ladder over
/// with the signal; the position keeps it, so the trader and the Altrady webhook use those levels
/// instead of Settings.Trading.DcaList. Null or empty text means: no ladder of its own.
/// </para>
/// </summary>
public static class DcaLadder
{
    /// <summary>
    /// The text of a ladder WITHOUT levels: the strategy wants no DCA at all, also not the ones of
    /// Settings.Trading.DcaList (see SignalCreateBase.WithoutDca). Null text means something else:
    /// no ladder of its own, so the global one applies.
    /// </summary>
    public const string None = "none";


    /// <summary>The ladder as text, null for no ladder (null or empty list).</summary>
    public static string? Format(IReadOnlyList<CryptoDcaEntry>? levels)
    {
        if (levels == null || levels.Count == 0)
            return null;

        StringBuilder text = new();
        foreach (CryptoDcaEntry level in levels)
        {
            if (text.Length > 0)
                text.Append(';');
            text.Append(level.Percentage.ToString(CultureInfo.InvariantCulture));
            text.Append(':');
            text.Append(level.Factor.ToString(CultureInfo.InvariantCulture));
        }
        return text.ToString();
    }


    /// <summary>
    /// The ladder from its text, null when there is none or the text cannot be read, and an empty
    /// list for <see cref="None"/> (no DCA at all).
    /// </summary>
    public static List<CryptoDcaEntry>? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        if (text == None)
            return [];

        List<CryptoDcaEntry> levels = [];
        foreach (string item in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = item.Split(':');
            if (parts.Length != 2
                || !decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal percentage)
                || !decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal factor))
                return null;
            levels.Add(new CryptoDcaEntry { Percentage = percentage, Factor = factor });
        }
        return levels.Count > 0 ? levels : null;
    }
}
