using CryptoScanner.Analyzers.MacdCross;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.MacdCrossBand;

/// <summary>
/// The MACD crossover with one extra question: was the price at a band recently? Everything the
/// plain crossover can do is inherited unchanged (it derives from <see cref="MacdCrossSettings"/>),
/// so the two strategies carry the same knobs and can be tuned apart from each other.
/// <para>
/// Three band strategies can be looked back at - Vbs, AtrRb and Dbr - and the ones ticked below are
/// asked in that order. A hit on ANY of them is enough; all of them are reported in the signal
/// text, so the text says which band was touched and how many candles ago. With none of them ticked
/// nothing is looked up and the strategy is the plain crossover again, which is the baseline to
/// compare against.
/// </para>
/// <para>
/// The bands themselves are NOT configured here - each of the three keeps using its own settings
/// tab (length, multiplier, lookback), so this strategy always sees exactly the same bands the
/// original strategy and the chart overlay draw.
/// </para>
/// <para>
/// Meant as an attention filter rather than as a trading rule: a cross that comes right after price
/// stretched to a band is a chart worth opening. The default window is therefore deliberately short.
/// </para>
/// </summary>
[Serializable]
public class MacdCrossBandSettings : MacdCrossSettings
{
    /// <summary>
    /// How many candles back the band break is looked for, the signal candle included. The window
    /// ends AT the signal candle, so the break always lies at or before the cross - which is the
    /// order that makes sense: price stretches to the band first, momentum turns afterwards.
    /// Values under 1 are read as 1.
    /// </summary>
    [SettingCaption("Within candles", SeparatorBefore = true, SubHeader = "Band break lookback",
        Tooltip = "How many candles back the band break is looked for, the signal candle included. "
            + "The break therefore always lies at or before the cross.")]
    public int LookbackWithinCandles { get; set; } = 10;

    /// <summary>The VBS band (volume-weighted VWAP band). Read from the value already on the candle.</summary>
    [SettingCaption("Look back at Vbs", Indented = true,
        Tooltip = "Look for a VBS band break: the volume-weighted VWAP band. Read from the value "
            + "already computed on the candle, so this one is nearly free.")]
    public bool LookbackVbs { get; set; } = true;

    /// <summary>The AtrRb band (an EMA basis with ATR bands, Keltner style).</summary>
    [SettingCaption("Look back at AtrRb", Indented = true,
        Tooltip = "Look for an AtrRb band break: an EMA basis with ATR bands. Costs an EMA and an "
            + "ATR over the recent candles, once per checked signal.")]
    public bool LookbackAtrRb { get; set; } = false;

    /// <summary>The DBR band (Donchian based outer bands), including its stacking rule.</summary>
    [SettingCaption("Look back at Dbr", Indented = true,
        Tooltip = "Look for a DBR band break: Donchian based outer bands, including the rule that "
            + "only the first break of a run counts. Costs a Donchian pass over the recent candles.")]
    public bool LookbackDbr { get; set; } = false;

    /// <summary>
    /// Off (the default) asks for a break on the side of the trade: a long wants the LOWER band
    /// broken, a short the UPPER one - price stretched away and momentum turning back is the
    /// situation the filter is looking for. On accepts a break of either band, which is the wider
    /// "this coin has been at its bands lately" reading.
    /// </summary>
    [SettingCaption("Accept either band",
        Tooltip = "Off asks for the band on the side of the trade: a long wants the lower band "
            + "broken, a short the upper one. On accepts a break of either band.")]
    public bool AcceptEitherBand { get; set; } = false;

    /// <summary>
    /// Whether the wick counts, for the VBS lookback only. Off (the default) mirrors the VBS
    /// strategy itself: a break is a low under the lower band OR a close under it. On asks for the
    /// CLOSE to be beyond the band, which is the stricter reading - a candle that only poked through
    /// with its wick then does not count. AtrRb and Dbr keep their own rule, which is the wick.
    /// </summary>
    [SettingCaption("Vbs: close beyond the band only", Indented = true,
        Tooltip = "For the VBS lookback only. Off counts a wick through the band, the same as the "
            + "VBS strategy does. On asks for the close itself to be beyond the band.")]
    public bool VbsRequireCloseBeyondBand { get; set; } = false;

    public MacdCrossBandSettings() : base()
    {
    }
}
