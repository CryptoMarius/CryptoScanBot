namespace CryptoSbmScanner.Exchange.BybitSpot;

internal class KLineTicker : KLineTickerBase
{
    public KLineTicker() : base(Api.ExchangeName, 10, typeof(KLineTickerItem))
    {
    }

}
