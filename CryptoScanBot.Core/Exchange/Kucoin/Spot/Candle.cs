using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Kucoin.Net.Clients;
using Kucoin.Net.Enums;

namespace CryptoScanBot.Core.Exchange.Kucoin.Spot;

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class Candle
{
#if KUCOINDEBUG
    private static int tickerIndex = 0;
#endif

    // Prevent multiple sessions
    private static readonly SemaphoreSlim Semaphore = new(1);

    private static async Task<long> GetCandlesForInterval(KucoinRestClient client, CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval)
    {
        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval);
        if (exchangeInterval == null)
            return 0;

        //KucoinWeights.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval.Name}";

        // Er is een maximum van circa 1500 volgens de docs
        // bewust 5 candles terug (omdat de API qua klines raar doet, hebben we in ieder geval 1 te pakken)
        DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized - 10 * interval.Duration);
        while (true)
        {
            long dateNow = CandleTools.GetUnixTime(DateTime.UtcNow, 0);


            DateTime dateEnd = dateStart.AddSeconds(1500 * interval.Duration);
            var result = await client.SpotApi.ExchangeData.GetKlinesAsync(symbol.Base + '-' + symbol.Quote, (KlineInterval)exchangeInterval, dateStart, dateEnd);
            //GlobalData.AddTextToLogTab($"Debug: {symbol.Name} {interval.Name} volume={symbol.Volume} start={dateStart} end={dateEnd} url={result.RequestUrl}");
            if (!result.Success)
            {
                // We doen het gewoon over (dat is tenminste het advies)
                // 13-07-2023 14:08:00 AOA-BTC 30m error getting klines 429000: Too Many Requests
                if (result.Error?.Code == 429000)
                {
                    GlobalData.AddTextToLogTab($"{prefix} delay needed for weight: (because of rate limits)");
                    Thread.Sleep(10000);
                    continue;
                }
                // Might do something better than this
                GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
                return 0;
            }


            // Might have problems with no internet etc.
            if (result == null || result.Data == null || !result.Data.Any())
            {
                GlobalData.AddTextToLogTab($"{prefix} fetch from {CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized)} no candles received");
                return 0;
            }

            // Remember
            long? startFetchDate = symbolInterval.LastCandleSynchronized;

            Monitor.Enter(symbol.CandleList);
            try
            {
                long last = long.MinValue;
                // Combine candles, calculating other interval's
                foreach (var kline in result.Data)
                {
                    // Combine candles, calculating other interval's
                    CryptoCandle candle = CandleTools.HandleFinalCandleData(symbol, interval, kline.OpenTime,
                        kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.QuoteVolume, false);

                    // Onthoud de laatste aangeleverde candle, t/m die datum is ten minste alles binnen gehaald
                    if (candle.OpenTime > last)
                        last = candle.OpenTime + interval.Duration; // new (saves 1 candle)
                }

                // For the next session
                if (last > long.MinValue)
                {
                    symbolInterval.LastCandleSynchronized = last;
                    // Alternatief (maar als er gaten in de candles zijn geeft dit problemen, endless loops)
                    //CandleTools.UpdateCandleFetched(symbol, interval);
                }

#if KUCOINDEBUG
                // Debug, wat je al niet moet doen voor een exchange...
                tickerIndex++;
                long unix = CandleTools.GetUnixTime(DateTime.UtcNow, 0);
                string filename = $@"{GlobalData.GetBaseDir()}\Kucoin Spot\Candles-{symbol.Name}-{interval.Name}-{unix}-#{tickerIndex}.json";
                string text = System.Text.Json.JsonSerializer.Serialize(result, GlobalData.JsonSerializerIndented);
                File.WriteAllText(filename, text);
#endif
            }
            finally
            {
                Monitor.Exit(symbol.CandleList);
            }


            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
            SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;
            string s = symbol.Exchange.Name + " " + symbol.Name + " " + interval.Name + " fetch from " + CandleTools.GetUnixDate(startFetchDate).ToLocalTime() + " UTC tot " + CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized).ToLocalTime() + " UTC";
            GlobalData.AddTextToLogTab(s + " received: " + result.Data.Count() + " totaal: " + candles.Count.ToString());
            return result.Data.Count();
        }
    }


    private static async Task FetchCandlesInternal(KucoinRestClient client, CryptoSymbol symbol, long fetchEndUnix)
    {
        for (int i = 0; i < GlobalData.IntervalList.Count; i++)
        {
            CryptoInterval interval = GlobalData.IntervalList[i];
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            bool intervalSupported = Interval.GetExchangeInterval(interval) != null;


            if (intervalSupported)
            {
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
                    CandleTools.UpdateCandleFetched(symbol, interval);
                    if (symbolInterval.LastCandleSynchronized == lastDate || candleCount == 0)
                        break;
                }
            }


            Monitor.Enter(symbol.CandleList);
            try
            {
                // Fill missing candles (the only place we know it can be done safely)
                if (symbolInterval.CandleList.Count != 0)
                {
                    CryptoCandle stickOld = symbolInterval.CandleList.Values.First();
                    //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Debug missing candle " + CandleTools.GetUnixDate(stickOld.OpenTime).ToLocalTime());
                    long unixTime = stickOld.OpenTime;
                    while (unixTime < symbolInterval.LastCandleSynchronized)
                    {
                        if (!symbolInterval.CandleList.TryGetValue(unixTime, out CryptoCandle? candle))
                        {
                            candle = new()
                            {
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
                    CryptoInterval intervalLowerTimeFrame = intervalHigherTimeFrame.ConstructFrom!;

                    CryptoSymbolInterval periodLowerTimeFrame = symbol.GetSymbolInterval(intervalLowerTimeFrame.IntervalPeriod);
                    SortedList<long, CryptoCandle> candlesLowerTimeFrame = periodLowerTimeFrame.CandleList;

                    if (candlesLowerTimeFrame.Values.Any())
                    {
                        long candleHigherTimeFrameStart = candlesLowerTimeFrame.Values.First().OpenTime;
                        candleHigherTimeFrameStart -= candleHigherTimeFrameStart % intervalHigherTimeFrame.Duration;
                        DateTime candleHigherTimeFrameStartDate = CandleTools.GetUnixDate(candleHigherTimeFrameStart);

                        long candleHigherTimeFrameEnd = candlesLowerTimeFrame.Values.Last().OpenTime;
                        candleHigherTimeFrameEnd -= candleHigherTimeFrameEnd % intervalHigherTimeFrame.Duration;
                        DateTime candleHigherTimeFrameEindeDate = CandleTools.GetUnixDate(candleHigherTimeFrameEnd);

                        // Bulk calculation
                        while (candleHigherTimeFrameStart <= candleHigherTimeFrameEnd)
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
            using KucoinRestClient client = new();

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

                if (symbol.ExchangeId == GlobalData.Settings.General.ExchangeId)
                {
                    Interval.DetermineFetchStartDate(symbol, fetchEndUnix);
                    await FetchCandlesInternal(client, symbol, fetchEndUnix);
                }
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("error getting candles " + error.ToString()); // symbol.Text + " " + 
        }
    }


    public static async Task GetCandlesForSymbolAsync(CryptoSymbol symbol, long fetchEndUnix)
    {
        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
            return;

        //GlobalData.AddTextToLogTab($"Fetching historical candles {symbol.Name}");

        using KucoinRestClient client = new();
        await FetchCandlesInternal(client, symbol, fetchEndUnix);

        //GlobalData.AddTextToLogTab("Candles ophalen klaar", true);
    }


    public static async Task GetCandlesForAllSymbolsAsync()
    {
        //GlobalData.AddTextToLogTab("Fetching historical candles");

        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
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
                        await Task.Run(Symbol.ExecuteAsync);

                    // TODO: Niet alle symbols zijn actief
                    GlobalData.AddTextToLogTab($"Aantal symbols={exchange.SymbolListName.Values.Count}");


                    Queue<CryptoSymbol> queue = new();
                    foreach (var symbol in exchange.SymbolListName.Values)
                    {
                        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol())
                            continue;

                        if (symbol.QuoteData!.FetchCandles)
                        {
                            if (symbol.QuoteData.MinimalVolume == 0 || (symbol.QuoteData.MinimalVolume > 0 && symbol.Volume > 0.1m * symbol.QuoteData.MinimalVolume))
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
