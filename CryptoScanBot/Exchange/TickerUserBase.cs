using CryptoScanBot.Intern;

namespace CryptoScanBot.Exchange;

public abstract class TickerUserBase(ExchangeOptions exchangeOptions)
{
    internal ExchangeOptions ExchangeOptions = exchangeOptions;

    //private List<PriceTickerStream> TickerList { get; set; } = new();
    public abstract Task StartAsync();
    public abstract Task StopAsync();
    //public abstract void Reset();
    //public abstract int Count();
    //public abstract Task ExecuteAsync();



    //public virtual bool NeedsRestart()
    //{
    //    bool restart = false;
    //    foreach (var ticker in TickerList)
    //    {
    //        // Is deze ooit gestart?
    //        if (ticker.TickerCount != 0)
    //        {
    //            // Is ie blijven staan? Netwerk storing enzovoort?
    //            if (ticker.TickerCount == ticker.TickerCountLast)
    //                restart = true;
    //            else
    //                ticker.TickerCountLast = ticker.TickerCount;
    //        }
    //    }
    //    return restart;
    //}


    internal void ConnectionLost()
    {
        //ConnectionLostCount++;
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} user ticker connection lost.");
    }

    internal void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} user ticker connection restored.");
    }

    internal void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} user ticker connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }
}
