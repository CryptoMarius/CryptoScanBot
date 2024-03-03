namespace CryptoScanBot.Exchange.Kraken;

internal class KLineTicker : KLineTickerBase
{
    public KLineTicker() : base(Api.ExchangeName, 10, typeof(KLineTickerItem))
    {
    }

}
