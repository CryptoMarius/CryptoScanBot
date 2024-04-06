using CryptoExchange.Net.Clients;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange;

public class TickerPriceItemBase(ExchangeOptions exchangeOptions)
{
    internal ExchangeOptions ExchangeOptions = exchangeOptions;

    public int TickerCount = 0;
    public int TickerCountLast = 0;

    public int ConnectionLostCount = 0;
    public bool ErrorDuringStartup = false;

    internal string GroupName = "";
    internal List<string> Symbols = [];

    internal BaseSocketClient socketClient;
    internal UpdateSubscription _subscription;


    public virtual Task<CallResult<UpdateSubscription>> Subscribe()
    {
        throw new NotImplementedException();
    }

    public virtual async Task StartAsync()
    {
        if (_subscription != null)
        {
            ScannerLog.Logger.Trace($"price ticker for group {GroupName} already started");
            return;
        }

        ConnectionLostCount = 0;
        ErrorDuringStartup = false;
        ScannerLog.Logger.Trace($"price ticker for group {GroupName} starting");

        var subscriptionResult = await Subscribe();
        if (subscriptionResult.Success)
        {
            _subscription = subscriptionResult.Data;
            _subscription.Exception += Exception;
            _subscription.ConnectionLost += ConnectionLost;
            _subscription.ConnectionRestored += ConnectionRestored;
            ScannerLog.Logger.Trace($"price ticker for group {GroupName} started");
        }
        else
        {
            _subscription.Exception -= Exception;
            _subscription.ConnectionLost -= ConnectionLost;
            _subscription.ConnectionRestored -= ConnectionRestored;
            _subscription = null;

            // todo, nakijken!
            socketClient.Dispose();
            socketClient = null;

            ConnectionLostCount++;
            ErrorDuringStartup = true;
            ScannerLog.Logger.Trace($"price ticker for group {GroupName} error {subscriptionResult.Error.Message} {string.Join(',', Symbols)}");
            GlobalData.AddTextToLogTab($"price ticker for group {GroupName} error {subscriptionResult.Error.Message} {string.Join(',', Symbols)}");
        }
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
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} price ticker connection lost for group {GroupName}.");
    }


    internal void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} price ticker connection restored for group {GroupName}.");
    }


    internal void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} price ticker connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}
