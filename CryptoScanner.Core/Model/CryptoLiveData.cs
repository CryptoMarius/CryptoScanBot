namespace CryptoScanner.Core.Model;

public class CryptoLiveData
{
    public required CryptoSymbol Symbol { get; set; }
    public required CryptoInterval Interval { get; set; }
    public required CryptoCandle Candle { get; set; }
}
