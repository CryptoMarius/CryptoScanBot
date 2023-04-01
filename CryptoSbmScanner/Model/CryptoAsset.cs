namespace CryptoSbmScanner.Model;


public class CryptoAsset
{
    public string Quote { get; set; } //De basismunt (BTC, ETH, USDT, BUSD enzovoort)
    public decimal Total { get; set; }
    public decimal Free { get; set; }
    public decimal Locked { get; set; }
}
