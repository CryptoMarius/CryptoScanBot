using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange;

public abstract class SubscriptionTicker(ExchangeOptions exchangeOptions)
{
    internal ExchangeOptions ExchangeOptions = exchangeOptions;

    public int TickerCount = 0;
    public int TickerCountLast = 0;

    public bool NeedsRestart = false;
    public int ConnectionLostCount = 0;
    public bool ErrorDuringStartup = false;

    public string GroupName = "";
    internal CryptoTickerType TickerType;
    public TickerGroup? TickerGroup;
    internal UpdateSubscription? _subscription;

    // Deze worden niet gebruikt bij de userticker 
    public List<string> Symbols = [];
    public List<CryptoSymbol> SymbolList = [];
    public string SymbolOverview = "";

    public abstract Task<CallResult<UpdateSubscription>?> Subscribe();


    public virtual async Task StartAsync()
    {
        if (_subscription != null)
        {
            ScannerLog.Logger.Trace($"{TickerType} ticker for group {GroupName} already started");
            return;
        }

        NeedsRestart = false;
        ConnectionLostCount = 0;
        ErrorDuringStartup = false;
        ScannerLog.Logger.Trace($"{TickerType} ticker for group {GroupName} starting");

        var subscriptionResult = await Subscribe();
        if (subscriptionResult is not null && subscriptionResult.Success)
        {
            _subscription = subscriptionResult.Data;
            _subscription.Exception += TickerException;
            _subscription.ConnectionLost += TickerConnectionLost;
            _subscription.ConnectionRestored += TickerConnectionRestored;
            ScannerLog.Logger.Trace($"{TickerType} ticker for group {GroupName} started");
        }
        else
        {
            if (_subscription != null)
            {
                _subscription.Exception -= TickerException;
                _subscription.ConnectionLost -= TickerConnectionLost;
                _subscription.ConnectionRestored -= TickerConnectionRestored;
                _subscription = null;
            }

            // todo, nakijken!
            //socketClient.Dispose();
            //socketClient = null;

            ConnectionLostCount++;
            NeedsRestart = true;
            ErrorDuringStartup = true;

            ScannerLog.Logger.Trace($"{TickerType} ticker for group {GroupName} error {subscriptionResult?.Error?.Message} {SymbolOverview}");
            GlobalData.AddTextToLogTab($"{TickerType} ticker for group {GroupName} error {subscriptionResult?.Error?.Message} {SymbolOverview}");
        }
    }


    public virtual async Task StopAsync()
    {
        if (_subscription == null)
        {
            ScannerLog.Logger.Trace($"{TickerType} ticker for group {GroupName} already stopped");
            return;
        }

        ScannerLog.Logger.Trace($"{TickerType} ticker for group {GroupName} stopping");
        _subscription.Exception -= TickerException;
        _subscription.ConnectionLost -= TickerConnectionLost;
        _subscription.ConnectionRestored -= TickerConnectionRestored;

        if (TickerGroup!.SocketClient is not null)
            await TickerGroup.SocketClient.UnsubscribeAsync(_subscription);

        _subscription = null;

        //TickerGroup.SocketClient?.Dispose();
        //TickerGroup.SocketClient = null;
        ScannerLog.Logger.Trace($"{TickerType} ticker for group {GroupName} stopped");
    }


    internal void TickerConnectionLost()
    {
        ConnectionLostCount++;
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} ticker for group {GroupName} connection lost");
        ScannerSession.ConnectionWasLost("");
    }


    internal void TickerConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} ticker for group {GroupName} connection restored");
        ScannerSession.ConnectionWasRestored("");
    }


    internal void TickerException(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} ticker for group {GroupName} connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}