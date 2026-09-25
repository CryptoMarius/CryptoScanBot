namespace CryptoScanner.Analyzers.Mac;

/// <summary>
/// The MAC values for one candle, computed once by <see cref="Indicators.MacIndicatorExtension"/>
/// and shared by the long and the short signal - so a candle with both sides active pays for the
/// four moving averages and the pivot scan once, not twice.
/// <para>
/// The four lengths belong together and are fixed on purpose; see MacSettings.
/// </para>
/// <para>
/// Lives in the plugin, not in CryptoData: the engine attaches it through
/// <c>CryptoData.SetPluginData</c> without knowing what MAC is.
/// </para>
/// </summary>
public sealed class MacCandleData
{
    /// <summary>The fast line, EMA(20) by default - the one a pullback bounces off.</summary>
    public double? EmaFast { get; set; }

    /// <summary>The second line, EMA(40) by default. Its crossing with the fast one is the entry.</summary>
    public double? EmaSecond { get; set; }

    /// <summary>The third line, SMA(50) by default.</summary>
    public double? SmaMedium { get; set; }

    /// <summary>
    /// The slow line, SMA(150) by default: the far edge of the cloud, and the one whose slope says
    /// whether there is a trend at all.
    /// </summary>
    public double? SmaSlow { get; set; }

    /// <summary>
    /// How much the slow line has moved over the last N candles, as a percentage of its own value.
    /// Positive is rising.
    /// <para>
    /// Its DIRECTION says how much weight the other signals deserve: a trade in the direction of a
    /// slow line that is going nowhere is a trade in a range. Null while there is not enough
    /// history for the comparison.
    /// </para>
    /// </summary>
    public double? SlowSlopePercentage { get; set; }

    /// <summary>
    /// The price of the last CONFIRMED pivot high, the resistance a long breaks through. Null until
    /// one has been seen. Confirmed means the pivot already has its right-hand candles, so the
    /// price sits at least PivotRightCandles candles back and can never be the candle in hand.
    /// </summary>
    public double? PivotHigh { get; set; }

    /// <summary>How many candles ago that pivot high sits, so a strategy can ignore a stale level.</summary>
    public int PivotHighAge { get; set; }

    /// <summary>The price of the last confirmed pivot low, the support a short breaks through.</summary>
    public double? PivotLow { get; set; }

    /// <summary>How many candles ago that pivot low sits.</summary>
    public int PivotLowAge { get; set; }

    /// <summary>
    /// The resistance taken where the RSI turned instead of where the price did, filled only when
    /// MacSettings.UseRsiLevels is on. The price is the HIGH of the candle the RSI turned on.
    /// </summary>
    public double? RsiLevelHigh { get; set; }

    /// <summary>
    /// How many candles ago that RSI resistance sits. Zero on the candle that sets it, which is
    /// allowed: a close never exceeds its own high, so that candle can never break it.
    /// </summary>
    public int RsiLevelHighAge { get; set; }

    /// <summary>The support taken where the RSI turned: the LOW of that candle.</summary>
    public double? RsiLevelLow { get; set; }

    /// <summary>How many candles ago that RSI support sits. Zero on the candle that sets it.</summary>
    public int RsiLevelLowAge { get; set; }

    /// <summary>
    /// Which new high this candle is within the current run beyond the resistance, counted from
    /// one, or zero when the close is not beyond the level or does not better the run's own high.
    /// <para>
    /// A run is the unbroken stretch of candles whose close stands beyond the level. Measured
    /// against the reference indicator over four coins, its break marker lands on a candle that
    /// makes a new high of such a run in 87 of 98 cases - 35% of those candles carry one against
    /// 4% of all the others - and never later than the eighth, almost always the first three.
    /// </para>
    /// </summary>
    public int BreakoutRank { get; set; }

    /// <summary>
    /// Whether the run this candle belongs to was worth marking at all, judged on its FIRST candle
    /// and then held for the whole run. Of 251 runs the reference marks 54, so this is where the
    /// selection happens; the rank above only says where inside a run the marks go.
    /// </summary>
    public bool BreakoutRunAllowed { get; set; }

    /// <summary>The same count on the other side: new lows within a run beyond the support.</summary>
    public int BreakdownRank { get; set; }

    /// <summary>The same judgement on the other side.</summary>
    public bool BreakdownRunAllowed { get; set; }

    /// <summary>The highest of the four lines at this candle, or null while they are warming up.</summary>
    public double? CloudTop
    {
        get
        {
            if (EmaFast == null || EmaSecond == null || SmaMedium == null || SmaSlow == null)
                return null;
            return Math.Max(Math.Max(EmaFast.Value, EmaSecond.Value), Math.Max(SmaMedium.Value, SmaSlow.Value));
        }
    }

    /// <summary>The lowest of the four lines at this candle, or null while they are warming up.</summary>
    public double? CloudBottom
    {
        get
        {
            if (EmaFast == null || EmaSecond == null || SmaMedium == null || SmaSlow == null)
                return null;
            return Math.Min(Math.Min(EmaFast.Value, EmaSecond.Value), Math.Min(SmaMedium.Value, SmaSlow.Value));
        }
    }
}
