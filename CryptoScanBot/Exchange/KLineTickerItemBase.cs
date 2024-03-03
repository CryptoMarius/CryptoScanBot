using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange;

public abstract class KLineTickerItemBase
{
    public int TickerCount = 0;
    public int TickerCountLast = 0;
    public int ConnectionLostCount = 0;
    public CryptoQuoteData QuoteData;
    public List<string> Symbols = new();
    public string ApiExchangeName;

    public KLineTickerItemBase(string apiExchangeName, CryptoQuoteData quoteData)
    {
        ApiExchangeName = apiExchangeName;
        QuoteData = quoteData;
    }

    public abstract Task StartAsync();
    public abstract Task StopAsync();


    private protected void Process1mCandle(CryptoSymbol symbol, DateTime openTime, decimal open, decimal high, decimal low, decimal close, decimal volume)
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
}