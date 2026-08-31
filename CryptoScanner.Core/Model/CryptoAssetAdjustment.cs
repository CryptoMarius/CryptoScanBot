using CryptoScanner.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

/// <summary>
/// Money that went into or out of the account without a trade behind it: a balance the user corrected
/// by hand, a coin deleted from the paper-assets screen, the start capital being handed out.
///
/// <para>
/// Without this ledger the capital line of <see cref="CryptoAssetSnapshot"/> cannot be read. Book in
/// 5.000 and the line jumps 5.000 - which looks exactly like a very good day but is nothing of the
/// sort. Subtract what is recorded here and what is left is the part that was actually traded.
/// </para>
/// <para>
/// A separate table, and not a column on the snapshot, for two reasons: a snapshot is a balance while
/// this is an event, and the event has to survive the coin it belongs to. Deleting a coin removes its
/// balance from <see cref="CryptoAsset"/> altogether (see <see cref="Trader.PaperAssets.UpdateAsset"/>),
/// so a correction stored next to that balance would disappear along with the very thing it explains.
/// </para>
/// </summary>
[Table("AssetAdjustment")]
public class CryptoAssetAdjustment
{
    [Key]
    public int Id { get; set; }

    // FK to EmulatorRun (null on live adjustments; populated by the emulator).
    public int? EmulatorRunId { get; set; }

    // When it happened, on GlobalData.Clock - emulator time during a replay.
    public DateTime EventTime { get; set; }

    // The coin (BTC, ETH, USDT and so on)
    public string Name { get; set; } = "";

    public CryptoAssetAdjustmentReason Reason { get; set; }

    // What the balance was and what it became, so the row also reads as a record of the change
    public decimal OldTotal { get; set; }
    public decimal NewTotal { get; set; }

    // NewTotal - OldTotal, so negative when money left the account
    public decimal Quantity { get; set; }

    // The coin Price and Value are expressed in, and the price at THIS moment: money that goes in is
    // worth what it was worth when it went in, not what it would be worth today.
    public string ReferenceCoin { get; set; } = "";
    public decimal Price { get; set; }

    // Quantity * Price, signed
    public decimal Value { get; set; }
}
