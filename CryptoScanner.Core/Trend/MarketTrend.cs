using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trend;

/// <summary>
/// The market trend: the average trend percentage over the coins of one quote coin, for the primary
/// and the secondary zigzag settings.
/// <para>
/// The market counterpart of <see cref="SymbolTrend"/>, which answers the same question for a single
/// coin. A different thing from the barometer it is stored next to, and that is the point: the
/// barometer averages a price CHANGE over an interval, so it is a return, while this averages a
/// TREND, which is structural. A market can be structurally up while the last hour is red, and only
/// those two numbers together say so.
/// </para>
/// <para>
/// Not to be confused with SettingsTextual.SymbolTrend, which tests the trend of ONE coin - that one
/// is really a coin trend. This is the market.
/// </para>
/// <para>
/// It lives here and not in the caller because both the live scanner and the emulator measure it:
/// BarometerTools per timer tick and BarometerReplay per replayed minute. A second copy of the rule
/// would drift at the first change, and the failure would be silent - a live graph and a replay
/// quietly measuring two different markets.
/// </para>
/// </summary>
public static class MarketTrend
{
    /// <summary>
    /// The smallest number of coins a measurement may rest on. An average over three coins is the
    /// trend of three coins with the word "market" written on it, so below this nothing is returned
    /// at all and the caller keeps its previous value.
    /// </summary>
    public const int MinimumSymbols = 5;


    /// <summary>
    /// Measure both trends over the coins that take part. The participation rule is the one the
    /// barometer itself uses (see CryptoBarometerPrice): the quote coin must be fetched, barometer
    /// symbols do not measure themselves, and a coin below the volume threshold is no part of the
    /// market this figure describes.
    /// <para>
    /// The cost is bounded by candle closes, not by how often this is called:
    /// <see cref="SymbolTrend.CalculateSymbolTrendAsync"/> keeps the answer on the symbol and only
    /// recomputes the intervals whose candle has advanced. It blocks on that task on purpose - every
    /// other caller of it in the scanner does the same, and the work is a cache check on all but the
    /// minutes where an interval actually closed.
    /// </para>
    /// <para>
    /// Both trends are measured over the coins that produced a value for THAT trend, counted
    /// separately: a coin without enough history for one zigzag setting must not silently lower the
    /// average of the other.
    /// </para>
    /// </summary>
    /// <returns>The two averages, or null for a trend that fewer than <paramref name="minimumSymbols"/> coins carried.</returns>
    public static (decimal? Primary, decimal? Secondary) Measure(IReadOnlyList<CryptoSymbol> symbolList, int minimumSymbols)
    {
        decimal sumPrimary = 0, sumSecondary = 0;
        int countPrimary = 0, countSecondary = 0;

        // Indexed and not foreach: the symbol list of a quote coin grows while the scanner runs (a
        // new listing is added to it), and an enumerator would throw on that. Same reason as the
        // loop in CryptoBarometerPrice.
        for (int i = 0; i < symbolList.Count; i++)
        {
            CryptoSymbol symbol = symbolList[i];

            if (symbol.QuoteData == null || !symbol.QuoteData.FetchCandles || symbol.IsBarometerSymbol() || !symbol.EnoughVolume())
                continue;

            CryptoTrendData primary = SymbolTrend.CalculateSymbolTrendAsync(symbol, GlobalData.Settings.Trend.Primary).Result;
            if (primary.Percentage.HasValue)
            {
                sumPrimary += (decimal)primary.Percentage.Value;
                countPrimary++;
            }

            CryptoTrendData secondary = SymbolTrend.CalculateSymbolTrendAsync(symbol, GlobalData.Settings.Trend.Secondary).Result;
            if (secondary.Percentage.HasValue)
            {
                sumSecondary += (decimal)secondary.Percentage.Value;
                countSecondary++;
            }
        }

        // Rounded like every other barometer figure, so a value that travels through a candle and
        // back reads the same to the cent as the one that was measured.
        decimal? averagePrimary = countPrimary >= minimumSymbols ? decimal.Round(sumPrimary / countPrimary, 8) : null;
        decimal? averageSecondary = countSecondary >= minimumSymbols ? decimal.Round(sumSecondary / countSecondary, 8) : null;
        return (averagePrimary, averageSecondary);
    }
}
