namespace CryptoScanBot.Exchange;

public abstract class PriceTickerBase()
{
    public abstract Task StartAsync();
    public abstract Task StopAsync();
    public abstract void Reset();
    public abstract int Count();
}
