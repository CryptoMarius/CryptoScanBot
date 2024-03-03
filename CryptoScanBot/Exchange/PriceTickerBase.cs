namespace CryptoScanBot.Exchange;

public abstract class PriceTickerBase
{
    public abstract Task Start();
    public abstract Task Stop();
    public abstract void Reset();
    public abstract int Count();
}
