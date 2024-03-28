using CryptoScanBot.Intern;

namespace CryptoScanBot.Exchange.BybitSpot;

#if TRADEBOT
internal class UserData : UserDataBase
{
    //private List<PriceTickerStream> TickerList { get; set; } = new();
    static private UserDataStream TaskStreamUserData { get; set; }

    public override async Task StartAsync()
    {
        // Is al gestart?
        if (TaskStreamUserData != null)
            return;

        GlobalData.AddTextToLogTab($"{Api.ExchangeName} user ticker starting");
        TaskStreamUserData = new UserDataStream();
        await Task.Run(async () => { await TaskStreamUserData.ExecuteAsync(); });
        ScannerLog.Logger.Trace($"{Api.ExchangeName} user ticker started");
    }


    public override async Task StopAsync()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} user ticker stopping");
        if (TaskStreamUserData != null)
            await TaskStreamUserData?.StopAsync();
        TaskStreamUserData = null;
        ScannerLog.Logger.Trace($"{Api.ExchangeName} user ticker stopped");
    }

    //public override void Reset()
    //{
    //    // empty
    //}


    //public override int Count()
    //{
    //    // empty
    //    return 0;
    //}

}
#endif
