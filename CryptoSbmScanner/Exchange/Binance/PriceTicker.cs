using CryptoSbmScanner.Intern;

namespace CryptoSbmScanner.Exchange.Binance;

internal class PriceTicker: PriceTickerBase
{
    private List<PriceTickerStream> TickerList { get; set; } = new();

    public override async Task Start()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} starting price ticker");
        PriceTickerStream ticker = new();
        TickerList.Add(ticker);
        await ticker.ExecuteAsync();
    }
    
    
    public override async Task Stop()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} stopping price ticker");
        List<Task> taskList = new();
        foreach (var ticker in TickerList)
        {
            Task task = Task.Run(async () => { await ticker.StopAsync(); });
            taskList.Add(task);
        }
        if (taskList.Any())
            await Task.WhenAll(taskList);
        TickerList.Clear();
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
