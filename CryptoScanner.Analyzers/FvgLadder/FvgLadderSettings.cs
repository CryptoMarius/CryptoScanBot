using CryptoScanner.Core.Settings.Strategy;

using System.Text.Json.Serialization;

namespace CryptoScanner.Analyzers.FvgLadder;

/// <summary>Which reading of the strategy runs.</summary>
public enum FvgLadderPreset
{
    /// <summary>
    /// "BTC FVG DCA - Regels ter afstemming": 1h and 4h gaps, an entry with DCA's on doubling sizes,
    /// take profit on the break-even price, a new parallel position only once the lowest one has
    /// bought all of its DCA's.
    /// </summary>
    Ladder = 0,

    /// <summary>
    /// Every gap is a position of its own with a fixed stake: no DCA, 1h gaps only, take profit on the
    /// entry, at least the step away from every open position.
    /// </summary>
    Grid = 1,
}


/// <summary>
/// The settings of the FVG &amp; DCA strategy (FvgLadderPlugin). The defaults are the rules as agreed in
/// "BTC FVG DCA - Regels ter afstemming"; the Grid preset uses the coins, the minimum gap,
/// the step, the order zones, the take profit and the order distances of this class and ignores the
/// DCA settings.
/// <para>
/// What lives elsewhere, on purpose: the gaps themselves and when they close come from the scanner
/// (Signal.ZonesFvg: the three-candle shape, MaxTouches, the intervals 1h and 4h have to be in its
/// list), the stake from Trading (EntryAmount/percentage), the trailing take profit from
/// Trading.TakeProfitTrailPercentage. There is no stop loss: the strategy is registered without one.
/// </para>
/// </summary>
[Serializable]
public class FvgLadderSettings : SettingsSignalStrategyBase
{
    /// <summary>It reads the fvg zones of the scanner.</summary>
    [JsonIgnore]
    public override IEnumerable<string> RequiredZoneKinds => ["fvg"];

    private const string GroupRules = "Rules";
    private const string GroupOrders = "Orders";
    private const string GroupDca = "DCA (Ladder only)";


    /// <summary>
    /// The coins the strategy trades, by their base asset ("BTC", "ETH"), separated by a comma or a
    /// space. Matched on the base asset, so "BTC" covers BTCUSDT, BTC-EUR and BTCUSDT.PERP alike.
    /// Empty means every coin the strategy is switched on for, like an empty white list. Each coin is
    /// judged on its own chart: its own gaps, its own day EMA200, its own positions.
    /// </summary>
    [SettingCaption("Coins", Group = GroupRules,
        Tooltip = "The base assets the strategy trades, separated by a comma, e.g. BTC, ETH. "
            + "Empty means every coin the strategy is switched on for.")]
    public string Coins { get; set; } = "BTC";

    [SettingCaption("Preset", Group = GroupRules,
        Tooltip = "Ladder: 1h + 4h gaps, DCA's, take profit on the break-even price, parallel positions after the DCA's. "
            + "Grid: every 1h gap a position of its own, no DCA, take profit on the entry.")]
    public FvgLadderPreset Preset { get; set; } = FvgLadderPreset.Ladder;

    /// <summary>Rule 2.3: the minimum width of a gap, (top - bottom) / bottom. The scanner default is 0.25.</summary>
    [SettingCaption("Minimum gap %", Group = GroupRules,
        Tooltip = "The minimum width of a gap, (top - bottom) / bottom. The scanner zones have to be calculated "
            + "with a minimum at or below this (Signal.ZonesFvg).")]
    public decimal MinimumGapPercentage { get; set; } = 0.4m;

    /// <summary>
    /// Rules 4.2 / 6.2: a DCA lies at least this far under the buy before it, and a new parallel
    /// position this far under the last buy of the lowest one. The Grid preset keeps
    /// every position this far from every other one.
    /// </summary>
    [SettingCaption("Step %", Group = GroupRules,
        Tooltip = "A DCA lies at least this far under the previous buy, a new parallel position this far under "
            + "the last buy of the lowest open position. Grid: the distance to every open position.")]
    public decimal StepPercentage { get; set; } = 1.5m;

    /// <summary>Rule 5.1: the take profit above the break-even price (Grid: above the entry).</summary>
    [SettingCaption("Take profit %", Group = GroupRules,
        Tooltip = "Above the break-even price of the whole position including fees. Grid: above the entry.")]
    public decimal TakeProfitPercentage { get; set; } = 1.0m;


    /// <summary>Rule 3.2: only the gaps this close under the price can carry an order.</summary>
    [SettingCaption("Order zones", Group = GroupOrders,
        Tooltip = "Only the nearest this many gaps under the price can carry the entry order.")]
    public int OrderZones { get; set; } = 4;

    /// <summary>
    /// The buy limit is placed once the price is at most this far above the gap top. An order on
    /// every gap would only hold money back; it only has to be there when the price comes near.
    /// </summary>
    [SettingCaption("Place order within %", Group = GroupOrders,
        Tooltip = "The buy limit on the gap top is placed once the price is at most this far above it.")]
    public decimal PlaceOrderDistancePercentage { get; set; } = 1.0m;

    /// <summary>
    /// And taken off again once the price is further than this above the gap top. Larger than the
    /// distance above, so a price that hovers around that distance does not place and cancel the
    /// order on every candle.
    /// </summary>
    [SettingCaption("Withdraw order beyond %", Group = GroupOrders,
        Tooltip = "The waiting buy limit is cancelled once the price is further than this above the gap top. "
            + "Keep it above 'Place order within %'.")]
    public decimal WithdrawOrderDistancePercentage { get; set; } = 2.0m;


    /// <summary>Rule 4.1: how many DCA's a position gets.</summary>
    [SettingCaption("Maximum DCA's", Group = GroupDca)]
    public int MaxDca { get; set; } = 2;

    /// <summary>Rule 4.3: a gap top this far under the previous buy (and at least the step) takes the DCA.</summary>
    [SettingCaption("DCA on a gap up to %", Group = GroupDca,
        Tooltip = "When a gap top lies between the step and this far under the previous buy, the DCA goes on that top.")]
    public decimal DcaFvgMaxPercentage { get; set; } = 3.0m;

    /// <summary>Rule 4.4: each DCA is this many times the buy before it (2: 100 / 200 / 400).</summary>
    [SettingCaption("DCA size factor", Group = GroupDca,
        Tooltip = "Each DCA is this many times the previous buy: 2 gives 100 / 200 / 400.")]
    public decimal DcaFactor { get; set; } = 2.0m;

    /// <summary>Whether this coin is one of <see cref="Coins"/> (always, when the list is empty).</summary>
    public bool TradesCoin(string baseAsset)
    {
        string[] coins = Coins.Split([',', ';', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return coins.Length == 0 || coins.Any(c => c.Equals(baseAsset, StringComparison.OrdinalIgnoreCase));
    }

    // No interval list of its own: it is registered as a zone strategy, which always runs on the
    // minute candle (SignalExecute.Prepare). The gaps come from the 1h and 4h fvg zones.
}
