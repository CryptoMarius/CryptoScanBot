using CryptoExchange.Net.Clients;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange;

public abstract class KLineTickerItemBase(string apiName, CryptoQuoteData quoteData)
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


    private protected static void Process1mCandle(CryptoSymbol symbol, DateTime openTime, decimal open, decimal high, decimal low, decimal close, decimal volume)
    {
        Monitor.Enter(symbol.CandleList);
        try
        {
            // Laatste bekende prijs (priceticker vult aan)
            symbol.LastPrice = close;

            // Process the single 1m candle
            CryptoCandle candle = CandleTools.HandleFinalCandleData(symbol, GlobalData.IntervalList[0], openTime, open, high, low, close, volume, false);
            CandleTools.UpdateCandleFetched(symbol, GlobalData.IntervalList[0]);
#if SQLDATABASE
            GlobalData.TaskSaveCandles.AddToQueue(candle);
#endif
#if SHOWTIMING

            GlobalData.Logger.Info($"ticker(1m):" + candle.OhlcText(symbol, GlobalData.IntervalList[0], symbol.PriceDisplayFormat, true, false, true));
#endif


            // Calculate higher timeframes
            //long candle1mOpenTime = candle.OpenTime;
            long candle1mCloseTime = candle.OpenTime + 60;
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                if (interval.ConstructFrom != null && candle1mCloseTime % interval.Duration == 0)
                {
                    CryptoCandle candleNew = CandleTools.CalculateCandleForInterval(interval, interval.ConstructFrom, symbol, candle1mCloseTime);
                    CandleTools.UpdateCandleFetched(symbol, interval);
#if SHOWTIMING

                    GlobalData.Logger.Info($"ticker({interval.Name}):" + candleNew.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, false, true));
#endif

                }
            }

            // Aanbieden voor analyse (dit gebeurd zowel in de ticker als ProcessCandles)
            if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                GlobalData.ThreadMonitorCandle.AddToQueue(symbol, candle);
        }
        finally
        {
            Monitor.Exit(symbol.CandleList);
        }
    }


    public virtual async Task StopAsync()
    {
        if (_subscription == null)
        {
            ScannerLog.Logger.Trace($"kline ticker for group {GroupName} already stopped");
            return;
        }

        ScannerLog.Logger.Trace($"kline ticker for group {GroupName} stopping");
        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient?.UnsubscribeAsync(_subscription);
        _subscription = null;

        socketClient?.Dispose();
        socketClient = null;
        ScannerLog.Logger.Trace($"kline ticker for group {GroupName} stopped");
    }


    internal void ConnectionLost()
    {
        ConnectionLostCount++;
        GlobalData.AddTextToLogTab($"{ApiExchangeName} {QuoteData.Name} kline ticker for group {GroupName} connection lost.");
        ScannerSession.ConnectionWasLost("");
    }

    internal void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{ApiExchangeName} {QuoteData.Name} kline ticker for group {GroupName} connection restored.");
        ScannerSession.ConnectionWasRestored("");
    }

    internal void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{ApiExchangeName} kline ticker for group {GroupName} connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}