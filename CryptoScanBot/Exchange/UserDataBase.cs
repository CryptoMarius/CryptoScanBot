namespace CryptoScanBot.Exchange;

public abstract class UserDataBase
{
    //private List<PriceTickerStream> TickerList { get; set; } = new();

    public abstract Task Start();
    public abstract Task Stop();
    //public abstract void Reset();
    //public abstract int Count();



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

}
