using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoSbmScanner
{
    public class ThreadCreateSignal
    {
        public int analyseCount = 0; //Tellertje die in de taakbalk c.q. applicatie titel komt (indicatie meldingen)
        private AnalyseAlgoritmAlarmEvent AlarmEvent = null;
        private BlockingCollection<CryptoCandle> Queue = new BlockingCollection<CryptoCandle>();
        private CancellationTokenSource cancellationToken = new CancellationTokenSource();

        public ThreadCreateSignal(AnalyseAlgoritmAlarmEvent AnalyseAlgoritmAlarmEvent)
        {
            AlarmEvent = AnalyseAlgoritmAlarmEvent;
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

        /// <summary>
        /// Thread main handler
        /// </summary>
        public void Execute()
        {
            foreach (CryptoCandle candle in Queue.GetConsumingEnumerable(cancellationToken.Token))
            {
                try
                {
                    // Heeft de gebruiker het vinkje voor calculatie aan staat?                     
                    if (GlobalData.Settings.Signal.SignalsActive)
                    {
                        CryptoQuoteData quoteData;
                        if (candle.Symbol.IsSpotTradingAllowed && (GlobalData.Settings.QuoteCoins.TryGetValue(candle.Symbol.Quote, out quoteData)) && quoteData.CreateSignals)
                        {
                            // De 1m candle is definitief, doe een herberekening van de relevante intervallen
                            long nextCandleOpenTime = candle.OpenTime + 60; // Neem de tijd van de volgende 1m candle
                            DateTime nextCandleOpenTimeDebug = CandleTools.GetUnixDate(nextCandleOpenTime);

                            // Analyseer de pair in de opgegeven intervallen (zou in aparte threads kunnen om dit te versnellen, maar dit gaat best goed momenteel)
                            foreach (CryptoInterval interval in GlobalData.IntervalList)
                            {
                                // We nemen aardig wat geheugen in beslag door alles in het geheugen te berekenen, probeer in 
                                // ieder geval de CandleData te clearen! Vanaf x candles terug tot de eerste de beste die null is.

                                CryptoSymbolInterval symbolPeriod = candle.Symbol.GetSymbolInterval(interval.IntervalPeriod);
                                SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;
                                for (int i = candles.Count - 62; i > 0; i--)
                                {
                                    CryptoCandle c = candles.Values[i];
                                    if (c.CandleData != null)
                                        c.CandleData = null;
                                    else break;
                                }


                                Monitor.Enter(candle.Symbol.CandleList);
                                try
                                {
                                    // Oude candles opruimen die niet relevant zijn voor de berekening (of barometer)
                                    long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(candle.Symbol, interval, DateTime.UtcNow);
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
                                    Monitor.Exit(candle.Symbol.CandleList);
                                }

                                if (GlobalData.Settings.Signal.AnalyseInterval[(int)interval.IntervalPeriod])
                                {
                                    // (0 % 180 = 0, 60 % 180 = 60, 120 % 180 = 120, 180 % 180 = 0)
                                    if (nextCandleOpenTime % interval.Duration == 0)
                                    {
                                        // We geven als tijd het begin van de "laatste" candle (van dat interval)
                                        SignalCreate createSignal = new SignalCreate(candle.Symbol, interval, AlarmEvent);
                                        createSignal.AnalyseSymbol(nextCandleOpenTime - interval.Duration, false);

                                        //Teller voor op het beeldscherm zodat je ziet dat deze thread iets doet en actief blijft.
                                        analyseCount++;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception error)
                {
                    // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                    GlobalData.Logger.Error(error);
                    GlobalData.AddTextToLogTab("\r\n" + "\r\n" + candle.Symbol.Name + " error Analyse thread\r\n" + error.ToString());
                }
            }
        }
    }
}
