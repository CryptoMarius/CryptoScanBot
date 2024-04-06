using CryptoScanBot.Intern;

namespace CryptoScanBot.Exchange;

#if TRADEBOT
public class TickerUser(ExchangeOptions exchangeOptions)
{
    internal ExchangeOptions ExchangeOptions { get; set; } = exchangeOptions;
    static private TickerUserBase UserTickerTask { get; set; }

    public virtual async Task StartAsync()
    {
        // Is al gestart
        if (UserTickerTask != null)
            return;

        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} user ticker starting");
        UserTickerTask = (TickerUserBase)Activator.CreateInstance(ExchangeOptions.UserTickerItemType, []);
        await Task.Run(async () => { await UserTickerTask.StartAsync(); });
        ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} user ticker started");
    }


    public virtual async Task StopAsync()
    {
        // Is al gestopt
        if (UserTickerTask == null)
            return;

        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} user ticker stopping");
        await UserTickerTask?.StopAsync();
        UserTickerTask = null;
        ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} user ticker stopped");
    }

    // ???
    //public override void Reset()
    //{
    //    // empty
    //}


    // ???
    //public override int Count()
    //{
    //    // empty
    //    return 0;
    //}
}
#endif
