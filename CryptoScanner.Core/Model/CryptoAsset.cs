using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

[Table("Asset")]
public class CryptoAsset
{
    [Key]
    public int Id { get; set; }

    // De basismunt (BTC, ETH, USDT enzovoort)
    public string Name { get; set; } = "";

    public decimal Total { get; set; }
    public decimal Free { get; set; }
    public decimal Locked { get; set; }
}
