using CryptoScanner.Core.Settings.Strategy;

using System.Text.Json.Serialization;

namespace CryptoScanner.Analyzers.FvgLadder;

/// <summary>
/// The settings of the FVG ladder strategy (FvgLadderPlugin), with the rules of
/// "BTC FVG DCA - Regels ter afstemming" as the defaults.
/// <para>
/// What lives elsewhere, on purpose: the gaps themselves and when they close come from the scanner
/// (Signal.ZonesFvg: shape, colours, MaxTouches; 1h and 4h have to be in its interval list), the
/// stake from Trading (EntryAmount/percentage), the trailing take profit from
/// Trading.TakeProfitTrailPercentage. There is no stop loss: the strategy is registered without one.
/// The interval list of the base class is the interval the Kalman trend is measured on.
/// </para>
/// </summary>
[Serializable]
public class FvgLadderSettings : SettingsSignalStrategyBase
{
    /// <summary>It reads the fvg zones of the scanner.</summary>
    [JsonIgnore]
    public override IEnumerable<string> RequiredZoneKinds => ["fvg"];

    private const string GroupGeneral = "General";
    private const string GroupTrend = "Trend";
    private const string GroupEntry = "Entry";
    private const string GroupDca = "DCA";
    private const string GroupTakeProfit = "Take profit";


    /// <summary>
    /// The coins the strategy trades, by their base asset ("BTC", "ETH"), separated by a comma or a
    /// space. Matched on the base asset, so "BTC" covers BTCUSDT, BTC-EUR and BTCUSDT.PERP alike.
    /// Empty means every coin the strategy is switched on for, like an empty white list. Each coin is
    /// judged on its own chart: its own gaps, its own trend, its own positions.
    /// </summary>
    [SettingCaption("Coins", Group = GroupGeneral,
        Tooltip = "The coins the strategy trades (base asset), separated by a comma, e.g. BTC, ETH. "
            + "Empty means every coin the strategy is switched on for.")]
    public string Coins { get; set; } = "BTC";

    // MaxPositionsPerCoin sat here until 24-10-2026; it is the general Trading.MaxPositionsPerCoin now
    // (1..n, no "0 = no limit"). Old settings files keep the key until they are next saved - it is
    // skipped on load.


    /// <summary>
    /// Off: a new position only while the last closed day candle closed above the EMA200 of the day
    /// closes. On: only while the Kalman filter on the strategy's own interval (Intervals, normally
    /// 1h) points up. The trend is only asked for a new position, never for a DCA.
    /// </summary>
    [SettingCaption("Use Kalman filter", Group = GroupTrend,
        Tooltip = "Off: a new position only while the day close is above the EMA200 (1d). "
            + "On: only while the Kalman filter on the strategy's interval (normally 1h) points up.")]
    public bool UseKalmanFilter { get; set; } = false;


    /// <summary>Rule 2.3: the minimum width of a gap, (top - bottom) / bottom; narrower gaps are ignored.</summary>
    [SettingCaption("Minimum gap %", Group = GroupEntry,
        Tooltip = "Gaps narrower than this, (top - bottom) / bottom, are ignored. The scanner zones have to be "
            + "calculated with a minimum at or below this (FVG configuration).")]
    public decimal MinimumGapPercentage { get; set; } = 0.4m;

    /// <summary>Rule 3.2: only the nearest this many gaps under the price take part, the rest is ignored.</summary>
    [SettingCaption("Gaps in view", Group = GroupEntry,
        Tooltip = "Only the nearest this many gaps below the price can get the entry order, the rest is ignored.")]
    public int GapsInView { get; set; } = 4;

    /// <summary>
    /// The buy limit on the gap top goes on the book once the price is at most this far above it. It
    /// stays until it fills, the trend goes, the gap is no longer valid or a higher gap becomes the
    /// target.
    /// </summary>
    [SettingCaption("Place order within %", Group = GroupEntry,
        Tooltip = "The buy limit on the gap top is placed once the price is at most this far above it. It stays until "
            + "it fills, the trend goes, the gap closes or a higher gap becomes the target.")]
    public decimal PlaceOrderWithinPercentage { get; set; } = 5.0m;


    /// <summary>Whether a position buys more when the price falls further (rule 4).</summary>
    [SettingCaption("Use DCA", Group = GroupDca,
        Tooltip = "Buy more when the price falls further. Off: every position is one buy.")]
    public bool UseDca { get; set; } = true;

    /// <summary>Rule 4.1: how many DCA's a position gets.</summary>
    [SettingCaption("Number of DCA's", Group = GroupDca)]
    public int MaxDca { get; set; } = 2;

    /// <summary>
    /// Rules 4.2 / 6.2: a DCA lies at least this far under the buy before it, and a new position at
    /// least this far under the last buy of the lowest open position. Without DCA it is only the
    /// second.
    /// </summary>
    [SettingCaption("DCA step %", Group = GroupDca,
        Tooltip = "A DCA lies at least this far under the previous buy. A new position lies at least this far "
            + "under the last buy of the lowest open position (also without DCA).")]
    public decimal DcaStepPercentage { get; set; } = 1.5m;

    /// <summary>Rule 4.3: a gap top this far under the previous buy (and at least the step) takes the DCA.</summary>
    [SettingCaption("DCA on a gap up to %", Group = GroupDca,
        Tooltip = "When a gap top lies between the step and this far under the previous buy, the DCA goes on that top.")]
    public decimal DcaFvgMaxPercentage { get; set; } = 3.0m;

    /// <summary>Rule 4.4: each DCA is this many times the buy before it (2: 100 / 200 / 400).</summary>
    [SettingCaption("DCA size factor", Group = GroupDca,
        Tooltip = "Each DCA is this many times the previous buy: 2 gives 100 / 200 / 400.")]
    public decimal DcaFactor { get; set; } = 2.0m;


    /// <summary>Rule 5.1: the take profit above the break-even price of the whole position, fees included.</summary>
    [SettingCaption("Take profit %", Group = GroupTakeProfit,
        Tooltip = "Above the break-even price of the whole position, fees included. Moves with every DCA.")]
    public decimal TakeProfitPercentage { get; set; } = 1.0m;


    /// <summary>Whether this coin is one of <see cref="Coins"/> (always, when the list is empty).</summary>
    public bool TradesCoin(string baseAsset)
    {
        string[] coins = Coins.Split([',', ';', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return coins.Length == 0 || coins.Any(c => c.Equals(baseAsset, StringComparison.OrdinalIgnoreCase));
    }


    public FvgLadderSettings() : base()
    {
        // The interval of the Kalman trend; the strategy itself always runs on the minute candle
        // (zone strategy) and reads its gaps from the 1h and 4h fvg zones
        IntervalList.Add("1h");
    }
}
