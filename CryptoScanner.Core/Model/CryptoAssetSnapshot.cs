using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

/// <summary>
/// The balance of one coin, frozen on one calendar day.
/// <para>
/// <see cref="CryptoAsset"/> only holds the balance of right now - it is overwritten on every fill
/// and the row is even deleted once the balance reaches zero - so the history it would take to draw
/// a growth line does not exist anywhere else. This table adds the date dimension: one row per coin
/// per day, written by <see cref="Trader.AssetSnapshotTools"/>.
/// </para>
/// <para>
/// The rows of one day add up to the capital of that day: sum(<see cref="Value"/>) over a
/// <see cref="SnapshotDate"/> is the total in <see cref="ReferenceCoin"/>.
/// </para>
/// </summary>
[Table("AssetSnapshot")]
public class CryptoAssetSnapshot
{
    [Key]
    public int Id { get; set; }

    // FK to EmulatorRun (null on live snapshots; populated by the emulator), so the snapshots of a
    // run stay separated from the live ones and from those of other runs - same as Position/Signal/Zone.
    public int? EmulatorRunId { get; set; }

    // The moment the balances were read, taken from GlobalData.Clock so a replay is stamped with
    // emulator time. There is at most one snapshot per calendar day (per run), see AssetSnapshotTools.
    public DateTime SnapshotDate { get; set; }

    // The coin (BTC, ETH, USDT and so on)
    public string Name { get; set; } = "";

    public decimal Total { get; set; }
    public decimal Free { get; set; }
    public decimal Locked { get; set; }

    /// <summary>
    /// Base quantity still owed on open short positions.
    /// <para>
    /// A short is booked entirely in quote (see <see cref="Trader.PaperAssets.Change"/>): the sale
    /// proceeds are added to the quote balance at the entry and the buyback is paid from it again at
    /// the exit, and the base coins that are owed in between are never administered. Without this
    /// column the capital would therefore look too high for exactly as long as a short is open.
    /// </para>
    /// </summary>
    public decimal ShortQuantity { get; set; }

    // The coin Price and Value are expressed in, normally USDT. Stored per row so old snapshots stay
    // readable when the reference ever changes.
    public string ReferenceCoin { get; set; } = "";

    // Price of one coin of Name in ReferenceCoin: 1 for the reference coin itself, and 0 when there
    // is no pair to read a price from (the row is kept, it just does not count towards the total).
    public decimal Price { get; set; }

    // (Total - ShortQuantity) * Price, so what this coin is worth on this day.
    public decimal Value { get; set; }
}
