namespace CryptoSbmScanner.Exchange.Binance;

internal class KLineTicker : KLineTickerBase
{
    public KLineTicker() : base(Api.ExchangeName, 200, typeof(KLineTickerItem))
    {
    }

}
