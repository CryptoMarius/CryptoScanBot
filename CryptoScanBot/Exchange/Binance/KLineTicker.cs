namespace CryptoScanBot.Exchange.Binance;

internal class KLineTicker() : KLineTickerBase(Api.ExchangeName, 200, typeof(KLineTickerItem))
{
}
