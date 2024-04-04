using CryptoExchange.Net.Clients;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange;

public class PriceTickerItemBase(string apiName, CryptoQuoteData quoteData)
{
    internal string ApiExchangeName = apiName;
    internal CryptoQuoteData QuoteData = quoteData;

    public int TickerCount = 0;
    public int TickerCountLast = 0;

    public int ConnectionLostCount = 0;
    public bool ErrorDuringStartup = false;

    internal string GroupName = "";
    internal List<string> Symbols = [];

    internal BaseSocketClient socketClient;
    internal UpdateSubscription _subscription;

    public virtual Task StartAsync()
    {
        return Task.CompletedTask;
    }


    public virtual async Task StopAsync()
    {
        if (_subscription == null)
        {
            ScannerLog.Logger.Trace($"price ticker for group {GroupName} already stopped");
            return;
        }

        ScannerLog.Logger.Trace($"price ticker for group {GroupName} stopping");
        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient?.UnsubscribeAsync(_subscription);
        _subscription = null;

        socketClient?.Dispose();
        socketClient = null;
        ScannerLog.Logger.Trace($"price ticker for group {GroupName} stopped");
    }


    internal void ConnectionLost()
    {
        ConnectionLostCount++;
        GlobalData.AddTextToLogTab($"{ApiExchangeName} price ticker connection lost for group {GroupName}.");
    }

    internal void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{ApiExchangeName} price ticker connection restored for group {GroupName}.");
    }

    internal void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{ApiExchangeName} price ticker connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}
