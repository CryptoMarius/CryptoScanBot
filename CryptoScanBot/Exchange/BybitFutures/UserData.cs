using CryptoScanBot.Intern;

namespace CryptoScanBot.Exchange.BybitFutures;

#if TRADEBOT
internal class UserData : UserDataBase
{
    //private List<PriceTickerStream> TickerList { get; set; } = new();
    static private UserDataStream TaskStreamUserData { get; set; }

    public override async Task Start()
    {
        // Is al gestart?
        if (TaskStreamUserData != null)
            return;

        GlobalData.AddTextToLogTab($"{Api.ExchangeName} starting userdata stream");
        TaskStreamUserData = new UserDataStream();
        await Task.Run(async () => { await TaskStreamUserData.ExecuteAsync(); });
        ScannerLog.Logger.Trace($"{Api.ExchangeName} userdata stream started");
    }


    public override async Task Stop()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} stopping userdata stream");
        if (TaskStreamUserData != null)
            await TaskStreamUserData?.StopAsync();
        TaskStreamUserData = null;
        ScannerLog.Logger.Trace($"{Api.ExchangeName} userdata stream stopped");
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
