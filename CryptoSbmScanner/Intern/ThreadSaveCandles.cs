using CryptoSbmScanner.Context;
using CryptoSbmScanner.Model;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Intern;

// De ThreadSaveCandles is een soort van Lazy write
// Reden: MSSQL en timeout's: Microsoft.Data.SqlClient.SqlException (0x80131904):
// Execution Timeout Expired. The timeout period elapsed prior to completion of the operation or the server is not responding.
// Een alternatief zou MySql zijn, die heeft een fijne (en best snelle) bulkinsert
// NB: Ik heb 2 applicaties, eentje die de candles in de database zet (emulator) en de CryptoSbmScanner (die geen candles in een db zet)

public class ThreadSaveCandles
{
#if DATABASE
    public int QueueCount = 0;
    private List<CryptoCandle> Queue = new List<CryptoCandle>();
    private CancellationTokenSource cancellationToken = new CancellationTokenSource();

    public void AddToQueue(CryptoCandle data)
    {
        Monitor.Enter(Queue);
        try
        {
            Queue.Add(data);
            QueueCount = Queue.Count;
        }
        finally
        {
            Monitor.Exit(Queue);
        }
    }

    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab(string.Format("Stopping saving candles"));
    }

    public void SaveQueuedCandles(List<CryptoCandle> list)
    {
        using (CryptoDatabase databaseThread = new())
        {
            databaseThread.Close();
            databaseThread.Open();


            using (var transaction = databaseThread.BeginTransaction())
            {
                // Lijstjes om info te cachen
                List<CryptoCandle> candleCache = new List<CryptoCandle>();
                SortedList<int?, CryptoSymbol> symbolList = new SortedList<int?, CryptoSymbol>();
                SortedList<string, CryptoCandleFetched> candleFetchList = new SortedList<string, CryptoCandleFetched>();


                foreach (CryptoCandle candle in list)
                {
                    if (GlobalData.ExchangeListId.TryGetValue(candle.ExchangeId, out Model.CryptoExchange exchange))
                    {
                        if (exchange.SymbolListId.TryGetValue(candle.SymbolId, out CryptoSymbol symbol))
                        {
                            if (GlobalData.IntervalListId.TryGetValue(candle.IntervalId, out CryptoInterval interval))
                            {
                                CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);

                                if (candle.Id > 0)
                                    databaseThread.Connection.Update(candle, transaction);
                                else candleCache.Add(candle);

                                CryptoCandleFetched candleFetched = symbolPeriod.CandleFetched;
                                if (candleFetched.IsChanged)
                                {
                                    string id = symbol.Name + "/" + interval.Name;
                                    if (!candleFetchList.ContainsKey(id))
                                        candleFetchList.Add(id, candleFetched);

                                    // Af en toe ook de symbol bewaren
                                    if (!symbolList.ContainsKey(symbol.Id))
                                        symbolList.Add(symbol.Id, symbol);
                                }
                            }

                        }
                    }

                    //CryptoSymbol symbol = candle.Symbol;
                    ///Model.CryptoExchange exchange = candle.Exchange;
                    //CryptoInterval interval = candle.Interval;

                    //CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
                    //CryptoCandleFetched candleFetched = symbolPeriod.CandleFetched;
                    //SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;

                    //if (candle.Id > 0)
                    //    databaseThread.Connection.Update(candle, transaction);
                    //else candleCache.Add(candle);

                    //GlobalData.AddTextToLogTab(string.Format("ThreadSave - Updated candle {0} {1} {2}", candle.Symbol.Name, candle.IntervalObject.Name, CandleTools.GetUnixDate(candle.OpenTime).ToLocalTime()));

                    //if (candleFetched.IsChanged)
                    //{
                    //    string id = symbol.Name + "/" + interval.Name;
                    //    if (!candleFetchList.ContainsKey(id))
                    //        candleFetchList.Add(id, candleFetched);
                    //}

                    //// Af en toe ook de symbol bewaren
                    //if (!symbolList.ContainsKey(symbol.Id))
                    //    symbolList.Add(symbol.Id, symbol);
                }

                // De data bewaren
                if (candleCache.Any())
                    databaseThread.BulkInsertCandles(candleCache, transaction);

                // Pas op: Er is een grote waarschijnlijkheid dat de CandleFetched een datum van candles
                // bevat die nog niet bewaard is (immers het aanbieden van candles gaat continue door)
                // Dit zit eigenlijk niet goed in elkaar, en ik heb zo snel geen betere manier!
                foreach (CryptoCandleFetched candleFetched in candleFetchList.Values)
                {
                    //if (candleFetched.Symbol.Name.Equals("BTCUSDT"))
                    //GlobalData.AddTextToLogTab(string.Format("ThreadSave - Updated candlefetch {0} {1} {2}", candleFetched.Symbol.Name , candleFetched.Interval.Name, CandleTools.GetUnixDate(candleFetched.Date).ToLocalTime()));
                    candleFetched.IsChanged = false;
                    if (candleFetched.Id > 0)
                        databaseThread.Connection.Update(candleFetched, transaction);
                    else databaseThread.Connection.Insert(candleFetched, transaction);
                }

                // Dus iedere candles wordt ook de symbols bewaard (kan even duren als de queue heel vol zit?)
                foreach (CryptoSymbol symbol in symbolList.Values)
                    databaseThread.Connection.Update(symbol, transaction);

                transaction.Commit();
            }
        }
    }

    public List<CryptoCandle> GetSomeFromQueue()
    {
        List<CryptoCandle> list = null;

        // Lock en copy alle candles die in de queue zitten (op dit moment)
        Monitor.Enter(Queue);
        try
        {
            // Niet teveel laden (erg trage inserts met 100k!)
            list = Queue.GetRange(0, Math.Min(QueueCount, 1000));
            Queue.RemoveRange(0, list.Count);
            QueueCount = Queue.Count;
        }
        finally
        {
            Monitor.Exit(Queue);
        }
        return list;
    }


    /// <summary>
    /// 1 Thread voor het bewaren van de candles (geen lock errors meer en grotere bulkinsert)
    /// </summary>
    public void Execute()
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                List<CryptoCandle> list = GetSomeFromQueue();
                if (list.Any())
                {
                    SaveQueuedCandles(list);
                    //GlobalData.AddTextToLogTab(string.Format("Saved={0} candles, queue={1} candles", list.Count, Queue.Count));
                }
                else Thread.Sleep(20000);

                // De queue zo efficient mogelijk maken (zoveel mogelijk batches van 1000 candles)
                //if (Queue.Count < 1000)
                //    Thread.Sleep(10000);
            }

        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab(error.ToString());
            //throw; vanaf nu worden er geen candles meer bewaard (oeps)
        }

    }
#endif
}
