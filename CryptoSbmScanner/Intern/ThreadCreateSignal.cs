using CryptoExchange.Net.CommonObjects;
using CryptoSbmScanner.Model;
using System.Collections.Concurrent;

namespace CryptoSbmScanner.Intern;

public class ThreadCreateSignal
{
    public int analyseCount = 0; //Tellertje die in de taakbalk c.q. applicatie titel komt (indicatie meldingen)
    private readonly AnalyseAlgoritmAlarmEvent SignalEvent = null;
    private readonly BlockingCollection<CryptoCandle> Queue = new();
    private readonly CancellationTokenSource cancellationToken = new();

    public ThreadCreateSignal(AnalyseAlgoritmAlarmEvent AnalyseAlgoritmAlarmEvent)
    {
        SignalEvent = AnalyseAlgoritmAlarmEvent;
    }

    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab(string.Format("Stop create signals"));
    }

    public void AddToQueue(CryptoCandle candle)
    {
        Queue.Add(candle);
    }

    private void CleanSymbolData(CryptoSymbol symbol)
    {
        // Analyseer de pair in de opgegeven intervallen (zou in aparte threads kunnen om dit te versnellen, maar dit gaat best goed momenteel)
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            // We nemen aardig wat geheugen in beslag door alles in het geheugen te berekenen, probeer in 
            // ieder geval de CandleData te clearen! Vanaf x candles terug tot de eerste de beste die null is.

            Monitor.Enter(symbol.CandleList);
            try
            {
                // Remove old indicator data
                SortedList<long, CryptoCandle> candles = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;
                for (int i = candles.Count - 62; i > 0; i--)
                {
                    CryptoCandle c = candles.Values[i];
                    if (c.CandleData != null)
                        c.CandleData = null;
                    else break;
                }


                // Remove old candles
                long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, interval, DateTime.UtcNow);
                while (candles.Values.Any())
                {
                    CryptoCandle c = candles.Values.First();
                    if (c == null)
                        break;
                    if (c.OpenTime < startFetchUnix)
                        candles.Remove(c.OpenTime);
                    else break;
                }
            }
            finally
            {
                Monitor.Exit(symbol.CandleList);
            }
        }
    }


    private void AnalyseSymbolData(CryptoCandle candle)
    {
        // De 1m candle is final
        // Neem de tijd van de volgende 1m candle
        long nextCandleOpenTime = candle.OpenTime + 60; 

        // Analyse the symbol in de opgegeven intervallen (zou in aparte threads kunnen om dit te versnellen, maar dit gaat best goed momenteel)
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (GlobalData.Settings.Signal.AnalyseInterval[(int)interval.IntervalPeriod])
            {
                // (0 % 180 = 0, 60 % 180 = 60, 120 % 180 = 120, 180 % 180 = 0)
                if (nextCandleOpenTime % interval.Duration == 0)
                {
                    // We geven als tijd het begin van de "laatste" candle (van dat interval)
                    SignalCreate createSignal = new(candle.Symbol, interval, false, SignalEvent);
                    createSignal.AnalyseSymbol(nextCandleOpenTime - interval.Duration);

                    //Teller voor op het beeldscherm zodat je ziet dat deze thread iets doet en actief blijft.
                    analyseCount++;
                }
            }
        }

    }

    private readonly SemaphoreSlim Semaphore = new(3);

    private void ProcessRequest(CryptoCandle candle)
    {
        Semaphore.Wait();
        try
        {
            CryptoSymbol symbol = candle.Symbol;

            try
            {
                // Heeft de gebruiker het vinkje voor calculatie aan staat?                     
                if (symbol.QuoteData.CreateSignals && GlobalData.Settings.Signal.SignalsActive && symbol.IsSpotTradingAllowed)
                    AnalyseSymbolData(candle);

                // Alway's: if any available positions or signals
                //if (symbol.SignalList.Count > 0 || symbol.PositionList.Count > 0)
                //    GlobalData.TaskMonitorSignal.AddToQueue(symbol);

                CleanSymbolData(symbol);
            }
            catch (Exception error)
            {
                // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab("\r\n" + "\r\n" + symbol.Name + " error Analyse thread\r\n" + error.ToString());
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <summary>
    /// Thread main handler
    /// </summary>
    public void Execute()
    {
        foreach (CryptoCandle candle in Queue.GetConsumingEnumerable(cancellationToken.Token))
        {
            new Thread(() => ProcessRequest(candle)).Start();
        }
    }
}
