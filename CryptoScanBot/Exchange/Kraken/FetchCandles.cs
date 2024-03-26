using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using Kraken.Net.Clients;
using Kraken.Net.Enums;

namespace CryptoScanBot.Exchange.Kraken;

/// <summary>
/// Fetch the candles from Binance
/// </summary>
public class FetchCandles
{
    // Prevent multiple sessions
    private static readonly SemaphoreSlim Semaphore = new(1);

    private static KlineInterval GetExchangeInterval(CryptoInterval interval)
    {
        var binanceInterval = interval.IntervalPeriod switch
        {
            CryptoIntervalPeriod.interval1m => KlineInterval.OneMinute,
            CryptoIntervalPeriod.interval5m => KlineInterval.FiveMinutes,
            CryptoIntervalPeriod.interval15m => KlineInterval.FifteenMinutes,
            CryptoIntervalPeriod.interval30m => KlineInterval.ThirtyMinutes,
            CryptoIntervalPeriod.interval1h => KlineInterval.OneHour,
            CryptoIntervalPeriod.interval4h => KlineInterval.FourHour,
            CryptoIntervalPeriod.interval1d => KlineInterval.OneDay,
            //case IntervalPeriod.interval1w:
            //    binanceInterval = KlineInterval.OneWeek;
            //    break;
            _ => KlineInterval.FifteenDays,// Ten teken dat het niet ondersteund wordt
        };
        return binanceInterval;
    }

    private static async Task<long> GetCandlesForInterval(KrakenRestClient client, CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval)
    {
        KlineInterval exchangeInterval = GetExchangeInterval(interval);
        if (exchangeInterval >= KlineInterval.FifteenDays)
            return 0;

        LimitRates.WaitForFairWeight(1); // *5x ivm API weight waarschuwingen
        string prefix = $"{Api.ExchangeName} {symbol.Name} {interval.Name}";

        // The maximum is 1000 candles
        // En (verrassing) de volgorde van de candles is van nieuw naar oud! 
        DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
        var result = await client.SpotApi.ExchangeData.GetKlinesAsync(symbol.Base + '/' + symbol.Quote, exchangeInterval, dateStart);
        if (!result.Success)
        {
            // Might do something better than this
            GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
            return 0;
        }


        // Might have problems with no internet etc.
        if (result == null || result.Data == null || !result.Data.Data.Any())
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized)} no candles received");
            return 0;
        }

        // Remember
        long startFetchDate = (long)symbolInterval.LastCandleSynchronized;

        Monitor.Enter(symbol.CandleList);
        try
        {
            long last = long.MinValue;
            // Combine candles, calculating other interval's
            foreach (var kline in result.Data.Data)
            {
                // Quoted = volume * price (expressed in usdt/eth/btc etc), base is coins
                CryptoCandle candle = CandleTools.HandleFinalCandleData(symbol, interval, kline.OpenTime,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, false);

                //GlobalData.AddTextToLogTab("Debug: Fetched candle " + symbol.Name + " " + interval.Name + " " + candle.DateLocal);

                // Pas op: Candle volgorde is niet gegarandeerd (zeker bybit niet), onthoud de jongste candle 
                // Voor de volgende GetCandlesForInterval() sessie
                //symbolInterval.IsChanged = true; // zie tevens setter (maar ach)
                //symbolInterval.LastCandleSynchronized = candle.OpenTime;

                // Onthoud de laatste aangeleverde candle, t/m die datum is alles binnen gehaald
                if (candle.OpenTime > last)
                    last = candle.OpenTime;
#if SQLDATABASE
                GlobalData.TaskSaveCandles.AddToQueue(candle);
#endif
            }

            // Voor de volgende GetCandlesForInterval() sessie
            if (last > long.MinValue)
            {
                symbolInterval.IsChanged = true; // zie tevens setter (maar ach)
                symbolInterval.LastCandleSynchronized = last;
                // Alternatief (maar als er gaten in de candles zijn geeft dit problemen, endless loops)
                //CandleTools.UpdateCandleFetched(symbol, interval);
            }

            //SaveInformation(symbol, result.Data.List);
        }
        finally
        {
            Monitor.Exit(symbol.CandleList);
        }


        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;
        string s = symbol.Exchange.Name + " " + symbol.Name + " " + interval.Name + " ophalen vanaf " + CandleTools.GetUnixDate(startFetchDate).ToLocalTime() + " UTC tot " + CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized).ToLocalTime() + " UTC";
        GlobalData.AddTextToLogTab(s + " opgehaald: " + result.Data.Data.Count() + " totaal: " + candles.Count.ToString());
        return result.Data.Data.Count();
    }


    private static async Task FetchCandlesInternal(KrakenRestClient client, CryptoSymbol symbol, long fetchEndUnix)
    {
        DateTime[] fetchFrom = new DateTime[Enum.GetNames(typeof(CryptoIntervalPeriod)).Length];

        DateTime utcNow = DateTime.UtcNow;
        foreach (CryptoInterval interval in GlobalData.IntervalList)
            fetchFrom[(int)interval.IntervalPeriod] = utcNow;

        // Determine the (maximum) startdate (without knowing what we already have)
        // If the exchange does not have this interval than make the lower timeframe
        // a bit bigger so we can calculate the candles ourselves
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, interval, utcNow);

            CryptoInterval loopInterval = interval;
            while (true)
            {
                DateTime startFetchUnixDate = CandleTools.GetUnixDate(startFetchUnix);
                if (fetchFrom[(int)loopInterval.IntervalPeriod] > startFetchUnixDate)
                    fetchFrom[(int)loopInterval.IntervalPeriod] = startFetchUnixDate;

                // Is this timeframe supported?
                if (GetExchangeInterval(loopInterval) != KlineInterval.FifteenDays)
                    break;
                else
                    loopInterval = loopInterval.ConstructFrom;
            }
        }

        // Debug
        //foreach (CryptoInterval interval in GlobalData.IntervalList)
        //  GlobalData.AddTextToLogTab("Fetching " + symbol.Name + " " + interval.Name + " " + fetchFrom[(int)interval.IntervalPeriod].ToLocalTime());


        // Correct the start date with what we already have
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            DateTime startFetchUnixDate = fetchFrom[(int)interval.IntervalPeriod];
            long startFetchUnix = CandleTools.GetUnixTime(startFetchUnixDate, 60);

            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            if (symbolInterval.LastCandleSynchronized == null || startFetchUnix > symbolInterval.LastCandleSynchronized)
                symbolInterval.LastCandleSynchronized = startFetchUnix;
        }


        for (int i = 0; i < GlobalData.IntervalList.Count; i++)
        {
            CryptoInterval interval = GlobalData.IntervalList[i];
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

            // Fetch the candles
            while (symbolInterval.LastCandleSynchronized < fetchEndUnix)
            {
                long lastDate = (long)symbolInterval.LastCandleSynchronized;
                //DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
                //GlobalData.AddTextToLogTab("Debug: Fetching " + symbol.Name + " " + interval.Name + " " + dateStart.ToLocalTime());


                if (symbolInterval.LastCandleSynchronized + interval.Duration > fetchEndUnix)
                    break;

                // Nothing more? (we have coins stopping, beaware for endless loops)
                long candleCount = await GetCandlesForInterval(client, symbol, interval, symbolInterval);
                if (symbolInterval.LastCandleSynchronized == lastDate || candleCount == 0)
                    break;
            }

            Monitor.Enter(symbol.CandleList);
            try
            {
                // Fill missing candles (at only place we know it can be done safely)
                if (symbolInterval.CandleList.Any())
                {
                    CryptoCandle stickOld = symbolInterval.CandleList.Values.First();
                    //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Debug missing candle " + CandleTools.GetUnixDate(stickOld.OpenTime).ToLocalTime());
                    long unixTime = stickOld.OpenTime;
                    while (unixTime < (long)symbolInterval.LastCandleSynchronized)
                    {
                        if (!symbolInterval.CandleList.TryGetValue(unixTime, out CryptoCandle candle))
                        {
                            candle = new()
                            {
#if SQLDATABASE
                                ExchangeId = symbol.Exchange.Id,
                                SymbolId = symbol.Id,
                                IntervalId = interval.Id,
#endif
                                //Symbol = symbol,
                                //Interval = interval,
                                OpenTime = unixTime,
                                Open = stickOld.Close,
                                High = stickOld.Close,
                                Low = stickOld.Close,
                                Close = stickOld.Close,
                                Volume = 0
                            };
                            symbolInterval.CandleList.Add(candle.OpenTime, candle);
                            //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Added missing candle " + CandleTools.GetUnixDate(candle.OpenTime).ToLocalTime());
                        }
                        stickOld = candle;
                        unixTime += interval.Duration;
                    }
                }

                // Calculate higher interval candles
                for (int j = i + 1; j < GlobalData.IntervalList.Count; j++)
                {
                    CryptoInterval intervalHigherTimeFrame = GlobalData.IntervalList[j];
                    CryptoInterval intervalLowerTimeFrame = intervalHigherTimeFrame.ConstructFrom;
                    
                    CryptoSymbolInterval periodLowerTimeFrame = symbol.GetSymbolInterval(intervalLowerTimeFrame.IntervalPeriod);
                    SortedList<long, CryptoCandle> candlesLowerTimeFrame = periodLowerTimeFrame.CandleList;

                    if (candlesLowerTimeFrame.Values.Any())
                    {
                        long candleHigherTimeFrameStart = candlesLowerTimeFrame.Values.First().OpenTime;
                        candleHigherTimeFrameStart -= candleHigherTimeFrameStart % intervalHigherTimeFrame.Duration;
                        DateTime candleHigherTimeFrameStartDate = CandleTools.GetUnixDate(candleHigherTimeFrameStart);

                        long candleHigherTimeFrameEinde = candlesLowerTimeFrame.Values.Last().OpenTime;
                        candleHigherTimeFrameEinde -= candleHigherTimeFrameEinde % intervalHigherTimeFrame.Duration;
                        DateTime candleHigherTimeFrameEindeDate = CandleTools.GetUnixDate(candleHigherTimeFrameEinde);

                        // Bulk calculation
                        while (candleHigherTimeFrameStart <= candleHigherTimeFrameEinde)
                        {
                            // Die laatste parameter is de closetime van een candle
                            candleHigherTimeFrameStart += intervalHigherTimeFrame.Duration;
                            CandleTools.CalculateCandleForInterval(intervalHigherTimeFrame, intervalLowerTimeFrame, symbol, candleHigherTimeFrameStart);
                        }
                        
                        CandleTools.UpdateCandleFetched(symbol, intervalHigherTimeFrame);
                    }
                }

            }
            finally
            {
                Monitor.Exit(symbol.CandleList);
            }
        }
    }

    public static async Task FetchCandlesAsync(long fetchEndUnix, Queue<CryptoSymbol> queue)
    {
        try
        {
            // Reuse the socket in this thread, because:
            // "An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full"
            using KrakenRestClient client = new();

            while (true)
            {
                CryptoSymbol symbol;

                Monitor.Enter(queue);
                try
                {
                    if (queue.Count > 0)
                        symbol = queue.Dequeue();
                    else
                        break;
                }
                finally
                {
                    Monitor.Exit(queue);
                }

                // Er is niet geswicthed van exchange (omdat het ophalen zo lang duurt)
                if (symbol.ExchangeId == GlobalData.Settings.General.ExchangeId)
                    await FetchCandlesInternal(client, symbol, fetchEndUnix);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("error getting candles " + error.ToString()); // symbol.Text + " " + 
        }
    }


    public static async Task ExecuteAsync()
    {
        //GlobalData.AddTextToLogTab("Fetching historical candles");

        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab($"Fetching {exchange.Name} information");
            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    GlobalData.SetCandleTimerEnable(false);
                    //GlobalData.AddTextToLogTab("");
                    //GlobalData.AddTextToLogTab("Ophalen " + exchange.Name);

                    // Bij het opstarten is deze (vanuit de LoadData) reeds uitgevoerd
                    if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Initializing)
                        await Task.Run(FetchSymbols.ExecuteAsync);

                    // TODO: Niet alle symbols zijn actief
                    GlobalData.AddTextToLogTab($"Aantal symbols={exchange.SymbolListName.Values.Count}");


                    Queue<CryptoSymbol> queue = new();
                    foreach (var symbol in exchange.SymbolListName.Values)
                    {
                        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol())
                            continue;

                        if (symbol.QuoteData.FetchCandles)
                        {
                            //if (symbol.Name.Equals("BTCUSDT") || symbol.Name.Equals("ETHUSDT") || symbol.Name.Equals("ADABTC") || symbol.Name.Equals("LEVERBTC"))
                            queue.Enqueue(symbol);
                        }
                    }


                    // Haal de candles op en zorg dat deze overlapt met de candles van de socket stream(s)
                    // De datum en tijd tot na het activeren van beide streams (overlap)
                    DateTime fetchEndUnixDate = DateTimeOffset.UtcNow.UtcDateTime;
                    long fetchEndUnix = CandleTools.GetUnixTime(fetchEndUnixDate, 60);


                    // En dan door x tasks de queue leeg laten trekken
                    List<Task> taskList = [];
                    while (taskList.Count < 5)
                    {
                        Task task = Task.Run(async () => { await FetchCandlesAsync(fetchEndUnix, queue); });
                        taskList.Add(task);
                    }
                    await Task.WhenAll(taskList).ConfigureAwait(false);

                    GlobalData.AddTextToLogTab("Candles ophalen klaar", true);
                }
                finally
                {
                    // Enabled analysing
                    GlobalData.SetCandleTimerEnable(true);

                    Semaphore.Release();
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab("error get prices " + error.ToString() + "\r\n");
            }
        }
    }


}
