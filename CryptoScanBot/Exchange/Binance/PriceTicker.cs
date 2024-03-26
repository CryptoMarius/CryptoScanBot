using CryptoScanBot.Intern;

namespace CryptoScanBot.Exchange.Binance;

internal class PriceTicker() : PriceTickerBase
{
    private List<PriceTickerItem> TickerList { get; set; } = [];

    public override async Task StartAsync()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} starting price tickers");
        PriceTickerItem ticker = new();
        TickerList.Add(ticker);
        await ticker.ExecuteAsync();
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} price tickers started");
    }


    public override async Task StopAsync()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} stopping price tickers");
        List<Task> taskList = [];
        foreach (var ticker in TickerList)
        {
            Task task = Task.Run(async () => { await ticker.StopAsync(); });
            taskList.Add(task);
        }
        if (taskList.Count != 0)
            await Task.WhenAll(taskList).ConfigureAwait(false);
        TickerList.Clear();
        ScannerLog.Logger.Trace($"{Api.ExchangeName} price tickers stopped");
    }


    public override void Reset()
    {
        foreach (var ticker in TickerList)
            ticker.TickerCount = 0;
    }


    public override int Count()
    {
        int count = 0;
        foreach (var ticker in TickerList)
            count += ticker.TickerCount;
        return count;
    }

}
