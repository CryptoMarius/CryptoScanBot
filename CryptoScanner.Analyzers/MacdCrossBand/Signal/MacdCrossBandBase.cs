using CryptoScanner.Analyzers.AtrRb.Signal;
using CryptoScanner.Analyzers.Dbr.Signal;
using CryptoScanner.Analyzers.MacdCross;
using CryptoScanner.Analyzers.MacdCross.Signal;
using CryptoScanner.Analyzers.Vbs;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.MacdCrossBand.Signal;

/// <summary>
/// The MACD crossover with a band break behind it. Everything about the cross, the filters and the
/// exit is inherited from <see cref="MacdCrossBase"/>; the only thing added here is the lookback,
/// which runs last because it is the most expensive test of the lot.
/// <para>
/// The break is recomputed from the candles rather than looked up in the signal list. That way the
/// answer is the same in the scanner, in a rescan and in the emulator, and it does not depend on
/// the band strategy being enabled or on its signal having survived in the list.
/// </para>
/// <para>
/// What is looked for is the BAND BREAK of Vbs / AtrRb / Dbr, not their complete signal: the RSI,
/// stochastic and Bollinger-width filters those strategies carry are not replayed. Those describe
/// the moment they would enter; the question here is only whether the price has been at the band.
/// </para>
/// </summary>
public class MacdCrossBandBase : MacdCrossBase
{
    protected override MacdCrossSettings Settings => MacdCrossBandPlugin.Settings;


    /// <summary>
    /// Was there a band break in the last N candles? Asked of every band strategy that is ticked,
    /// cheapest first: VBS reads a value that is already on the candle, AtrRb and Dbr each cost one
    /// pass over the recent candles. A hit on any of them is enough, but all of them are walked so
    /// the signal text can name every band that was touched and how many candles ago - which is the
    /// whole point of the strategy: it says where on the chart to look.
    /// </summary>
    protected override bool ExtraFiltersOkay(List<MyData> candles, out string text)
    {
        text = "";
        MacdCrossBandSettings settings = MacdCrossBandPlugin.Settings;
        int within = Math.Max(1, settings.LookbackWithinCandles);

        if (!settings.LookbackVbs && !settings.LookbackAtrRb && !settings.LookbackDbr)
            return true;

        bool isLong = SignalSide == CryptoTradeSide.Long;
        List<string> hits = [];

        // Only the VBS lookback can say "warming up": its values live on the candle, and a window
        // without a single band value means nothing was measured rather than that the price stayed
        // inside. AtrRb and Dbr compute their own bands from the candle list and simply say no.
        bool vbsBandsSeen = true;
        if (settings.LookbackVbs)
        {
            if (!CollectCandles(within, candles))
                return false;
            if (VbsFound(settings, candles, within, out string vbsText, out vbsBandsSeen))
                hits.Add(vbsText);
        }

        if (settings.LookbackAtrRb)
        {
            if (Find(AtrRbBandsHelper.TryFindRecentBreak, isLong, within, settings.AcceptEitherBand,
                    out int candlesAgo, out bool foundLower))
            {
                hits.Add(Phrase("atrrb", foundLower ? "lower" : "upper", candlesAgo));
            }
        }

        if (settings.LookbackDbr)
        {
            if (Find(DbrBandsHelper.TryFindRecentBreak, isLong, within, settings.AcceptEitherBand,
                    out int candlesAgo, out bool foundLower))
            {
                hits.Add(Phrase("dbr", foundLower ? "lower" : "upper", candlesAgo));
            }
        }

        if (hits.Count == 0)
        {
            ExtraText = $"no band break in the last {within} candle(s)";
            if (!vbsBandsSeen)
                ExtraText += " (vbs bands not available yet)";
            return false;
        }

        text = string.Concat(hits);
        return true;
    }


    /// <summary>The signature of the AtrRb and Dbr lookups, so both can be asked the same way.</summary>
    private delegate bool RecentBreakLookup(Core.Model.CryptoSymbolInterval symbolInterval,
        Core.Model.CandleTime openTime, bool isLong, int withinCandles, out int candlesAgo);


    /// <summary>
    /// Asks a lookup for the band on the side of the trade, and - when either band is accepted - for
    /// the other one as well, keeping whichever break is the more recent of the two.
    /// </summary>
    private bool Find(RecentBreakLookup lookup, bool isLong, int within, bool eitherBand,
        out int candlesAgo, out bool foundLower)
    {
        candlesAgo = 0;
        foundLower = isLong;

        bool found = lookup(SymbolInterval, CandleLast.Candle.OpenTime, isLong, within, out candlesAgo);
        if (!eitherBand)
            return found;

        if (lookup(SymbolInterval, CandleLast.Candle.OpenTime, !isLong, within, out int otherAgo)
            && (!found || otherAgo < candlesAgo))
        {
            candlesAgo = otherAgo;
            foundLower = !isLong;
            return true;
        }
        return found;
    }


    /// <summary>
    /// The VBS lookback. Its band values are computed by the VBS indicator extension and sit on the
    /// candle already, so this walks the candles the checks above collected instead of recomputing
    /// anything. <paramref name="bandsSeen"/> tells a "no break" apart from a "nothing measured".
    /// The caller has already filled <paramref name="candles"/> to <paramref name="within"/>.
    /// </summary>
    private bool VbsFound(MacdCrossBandSettings settings, List<MyData> candles, int within,
        out string text, out bool bandsSeen)
    {
        text = "";
        bandsSeen = false;

        bool wantLower = SignalSide == CryptoTradeSide.Long || settings.AcceptEitherBand;
        bool wantUpper = SignalSide == CryptoTradeSide.Short || settings.AcceptEitherBand;

        for (int i = 0; i < within; i++)
        {
            VbsCandleData? vbs = candles[i].CandleData?.GetPluginData<VbsCandleData>();
            if (vbs == null)
                continue;

            if (wantLower && vbs.Lower != null)
            {
                bandsSeen = true;
                double band = vbs.Lower.Value;
                bool broke = (double)candles[i].Candle.Close < band
                    || (!settings.VbsRequireCloseBeyondBand && (double)candles[i].Candle.Low < band);
                if (broke)
                {
                    text = Phrase("vbs", "lower", i);
                    return true;
                }
            }

            if (wantUpper && vbs.Upper != null)
            {
                bandsSeen = true;
                double band = vbs.Upper.Value;
                bool broke = (double)candles[i].Candle.Close > band
                    || (!settings.VbsRequireCloseBeyondBand && (double)candles[i].Candle.High > band);
                if (broke)
                {
                    text = Phrase("vbs", "upper", i);
                    return true;
                }
            }
        }
        return false;
    }


    private static string Phrase(string strategy, string band, int candlesAgo)
        => candlesAgo == 0
        ? $", {strategy} {band} band on this candle"
        : $", {strategy} {band} band {candlesAgo} candle(s) ago";
}


public class MacdCrossBandLong : MacdCrossBandBase
{
}


public class MacdCrossBandShort : MacdCrossBandBase
{
}
