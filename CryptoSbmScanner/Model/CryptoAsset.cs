using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;

[Table("Asset")]
public class CryptoAsset
{
    [Key]
    public int Id { get; set; }

    public int TradeAccountId { get; set; }

    // De basismunt (BTC, ETH, USDT, BUSD enzovoort)
    public string Name { get; set; }

    public decimal Total { get; set; }
    public decimal Free { get; set; }
    public decimal Locked { get; set; }
}
