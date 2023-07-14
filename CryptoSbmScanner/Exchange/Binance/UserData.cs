using CryptoSbmScanner.Intern;

namespace CryptoSbmScanner.Exchange.Binance;

#if TRADEBOT
internal class UserData : UserDataBase
{
    //private List<PriceTickerStream> TickerList { get; set; } = new();
    static private UserDataStream TaskBinanceStreamUserData { get; set; }

    public override async Task Start()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} starting userdata stream");
        TaskBinanceStreamUserData = new UserDataStream();
        await Task.Run(async () => { await TaskBinanceStreamUserData.ExecuteAsync(); });
    }


    public override async Task Stop()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} stopping userdata stream");
        if (TaskBinanceStreamUserData != null)
            await TaskBinanceStreamUserData?.StopAsync();
        TaskBinanceStreamUserData = null;
    }


    public override void Reset()
    {
        // empty
    }


    public override int Count()
    {
        // empty
        return 0;
    }

}
#endif
