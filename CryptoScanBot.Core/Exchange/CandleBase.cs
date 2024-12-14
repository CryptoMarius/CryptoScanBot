using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange;

public class CandleBase(ExchangeBase api)
{
    private static readonly SemaphoreSlim GetCandlesSemaphore = new(1);

    private ExchangeBase Api { get; set; } = api;

    public async Task GetCandlesForAllIntervalsAsync(CryptoSymbol symbol, long fetchEndUnix)
    {
        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
            return;

        using IDisposable client = Api.GetClient();
        for (int i = 0; i < GlobalData.IntervalList.Count; i++)
        {
            CryptoInterval interval = GlobalData.IntervalList[i];
            await Api.Candle.GetCandlesForIntervalAsync(client, symbol, interval, fetchEndUnix);
        }


        // Remove the candles we needed because of the not supported intervals & bulk calculation
        await CandleTools.CleanCandleDataAsync(symbol, null);
    }


    public virtual async Task GetCandlesForAllSymbolsAndIntervalsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab($"Fetching {exchange.Name} information");
            try
            {
                await GetCandlesSemaphore.WaitAsync();
                try
                {
                    GlobalData.SetCandleTimerEnable(false);
                    //GlobalData.AddTextToLogTab("");
                    //GlobalData.AddTextToLogTab("Ophalen " + exchange.Name);

                    // Bij het opstarten is deze (vanuit de LoadData) reeds uitgevoerd
                    if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Initializing)
                        await Api.Symbol.GetSymbolsAsync();

                    // TODO: Niet alle symbols zijn actief
                    GlobalData.AddTextToLogTab($"{exchange.Name} symbols={exchange.SymbolListName.Values.Count}");


                    Queue<CryptoSymbol> queue = new();
                    foreach (var symbol in exchange.SymbolListName.Values)
                    {
                        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
                            continue;

                        //if (symbol.Name.Equals("BTCUSDT") || symbol.Name.Equals("ETHUSDT") || symbol.Name.Equals("ADABTC") || symbol.Name.Equals("LEVERBTC"))
                        queue.Enqueue(symbol);
                    }


                    // Haal de candles op en zorg dat deze overlapt met de candles van de socket stream(s)
                    // De datum en tijd tot na het activeren van beide streams (overlap)
                    DateTime fetchEndUnixDate = DateTimeOffset.UtcNow.UtcDateTime;
                    long fetchEndUnix = CandleTools.GetUnixTime(fetchEndUnixDate, 60);


                    // En dan door x tasks de queue leeg laten trekken
                    List<Task> taskList = [];
                    while (taskList.Count < 5)
                    {
                        Task task = Task.Run(async () =>
                        {
                            //await GetCandlesForAllSymbolsFetchCandlesAsync(fetchEndUnix, queue); }
                            try
                            {
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

                                    // Er is niet geswitvhed van exchange (omdat het ophalen zo lang duurt)
                                    if (symbol.ExchangeId == GlobalData.Settings.General.ExchangeId)
                                    {
                                        CandleTools.DetermineFetchStartDate(symbol, fetchEndUnix);
                                        await GetCandlesForAllIntervalsAsync(symbol, fetchEndUnix);
                                    }
                                }
                            }
                            catch (Exception error)
                            {
                                ScannerLog.Logger.Error(error, "");
                                GlobalData.AddTextToLogTab("error getting candles " + error.ToString()); // symbol.Text + " " + 
                            }
                        });
                        taskList.Add(task);
                    }
                    await Task.WhenAll(taskList).ConfigureAwait(false);

                    //GlobalData.AddTextToLogTab("Candles ophalen klaar");
                }
                finally
                {
                    // Enabled analysing
                    GlobalData.SetCandleTimerEnable(true);

                    GetCandlesSemaphore.Release();
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab("error get prices " + error.ToString());
            }
        }
    }


    public async Task GetCandlesForIntervalAsync(IDisposable client, CryptoSymbol symbol, CryptoInterval interval, long fetchMax)
    {
        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData!.FetchCandles)
            return;

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        bool intervalSupported = symbol.Exchange.IsIntervalSupported(interval.IntervalPeriod);
        if (intervalSupported)
        {
            // Fetch the candles (we have coins starting and stopping, be aware for endless loops)
            while (symbolInterval.LastCandleSynchronized < fetchMax)
            {
                if (symbolInterval.LastCandleSynchronized + interval.Duration > fetchMax)
                    break;

                long lastTime = (long)symbolInterval.LastCandleSynchronized!;
                symbolInterval.LastCandleSynchronized = await Api.Candle.GetCandlesForInterval(client, symbol, interval, symbolInterval, lastTime, fetchMax);
                CandleTools.UpdateCandleFetched(symbol, interval);
                if (symbolInterval.LastCandleSynchronized == lastTime) // not moving forward
                    break;
            }
        }


        await symbol.CandleLock.WaitAsync();
        try
        {
            // Add missing candles (the only place we know it can be done safely)
            CandleTools.BulkAddMissingCandles(symbol, interval);

            // Bulk calculate the higher interval candles
            if (interval.IntervalPeriod < Enum.GetValues(typeof(CryptoIntervalPeriod)).Cast<CryptoIntervalPeriod>().Last())
            {
                CryptoInterval targetInterval = GlobalData.IntervalListPeriod[interval.IntervalPeriod + 1];
                CryptoInterval sourceInterval = targetInterval.ConstructFrom!;
                CandleTools.BulkCalculateCandles(symbol, sourceInterval, targetInterval, fetchMax);
            }
        }
        finally
        {
            symbol.CandleLock.Release();
        }


        // Adjust the administration for the not supported interval's
        if (!intervalSupported && symbolInterval.CandleList.Count > 0)
        {
            CryptoCandle candle = symbolInterval.CandleList.Values.Last();
            symbolInterval.LastCandleSynchronized = candle.OpenTime + symbolInterval.Interval.Duration;
        }
    }

    public async Task<bool> FetchFrom(CryptoSymbol symbol, CryptoInterval interval, CryptoCandleList candleList, long unixLoop, long unixMax)
    {
        // Fetch the candles (we have coins starting and stopping, be aware for endless loops)
        // Kind of the same as the CandleBase.GetCandlesForIntervalAsync, but also different because 
        // of the symbolInterval.LastCandleSynchronized and calculation of higher interval candles
        int totalFetched = 0;
        var api = symbol.Exchange.GetApiInstance();
        using IDisposable client = api.GetClient();
        while (unixLoop < unixMax)
        {
            if (unixLoop + interval.Duration > unixMax)
                break;

            long minTime = unixLoop;
            DateTime minDate = CandleTools.GetUnixDate(minTime);
            long maxTime = unixLoop + (ExchangeBase.ExchangeOptions.CandleLimit - 1) * interval.Duration;
            DateTime maxDate = CandleTools.GetUnixDate(maxTime);
            //string text = $"Fetch historical klines {symbol.Name} {interval!.Name} from {minDate.ToLocalTime()} up to {maxDate.ToLocalTime()}";

            long lastDate = minTime;
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            int countBefore = symbolInterval.CandleList.Count;
            unixLoop = await symbol.Exchange.GetApiInstance().Candle.GetCandlesForInterval(client, symbol, interval, symbolInterval, minTime, maxTime);

            int added = symbolInterval.CandleList.Count - countBefore;
            totalFetched += added;

            //string text3 = $"{text} retrieved={added} total={candleList.Count}";
            //ScannerLog.Logger.Info(text3);
            //GlobalData.AddTextToLogTab(text3);

            while (candleList!.ContainsKey(unixLoop))
                unixLoop += interval.Duration;

            if (unixLoop == minTime) // not moving forward
                break;
        }
        return totalFetched > 0;
    }

}
