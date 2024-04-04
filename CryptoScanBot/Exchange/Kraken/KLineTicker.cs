namespace CryptoScanBot.Exchange.Kraken;

internal class KLineTicker() : KLineTickerBase(Api.ExchangeName, 10, typeof(KLineTickerItem))
{
}
