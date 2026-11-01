using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

[Table("Asset")]
public class CryptoAsset
{
    [Key]
    public int Id { get; set; }

    // The exchange this balance belongs to (since 31-10-2026). Positions and signals are per
    // exchange as well, so switching exchange no longer brings the balances of the previous one.
    public int ExchangeId { get; set; }

    // De basismunt (BTC, ETH, USDT enzovoort)
    public string Name { get; set; } = "";

    public decimal Total { get; set; }
    public decimal Free { get; set; }
    public decimal Locked { get; set; }

    // What a reset of the paper account hands out for this coin (the Default column of Paper assets).
    // Kept in this row since 31-10-2026, before that it was a list in the settings file that every
    // exchange shared. A row with only a default (Total 0) stays, see PaperAssets.UpdateAsset.
    public decimal DefaultTotal { get; set; }
}
