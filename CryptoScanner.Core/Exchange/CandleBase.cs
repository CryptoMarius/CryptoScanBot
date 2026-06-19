using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Model;

using System.Text.Json;

namespace CryptoScanner.Core.Exchange;

public class CandleBase(ExchangeBase api)
{
    private static readonly SemaphoreSlim GetCandlesSemaphore = new(1);

    private ExchangeBase Api { get; set; } = api;

    internal static void SaveCandleInfo(object exchangeInfo, string name)
    {
        // Save for debug
        try
        {
            string folderName = Path.Combine(GlobalData.AppDataFolder, ExchangeBase.ExchangeOptions.ExchangeName);
            Directory.CreateDirectory(folderName);
            string filename = Path.Combine(folderName, name);

            string text = JsonSerializer.Serialize(exchangeInfo, JsonTools.JsonSerializerIndented);
            File.WriteAllText(filename, text);
        }
        catch
        {
            // ignore
        }

    }

    public async Task GetCandlesForAllIntervalsAsync(CryptoSymbol symbol)
    {
        if (!symbol.QuoteData.FetchCandles || symbol.Status == 0 || symbol.IsBarometerSymbol())
            return;

        using IDisposable client = Api.GetClient();
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            await Api.Candle.GetCandlesForIntervalAsync(client, symbol, interval);
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
                        if (symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
                            continue;

                        // The not so interesting coins (saves a lot of memory)
                        if (!symbol.EnoughVolume() && !symbol.IsTrading())
                            continue;

                        //if (symbol.Name.Equals("BTCUSDT") || symbol.Name.Equals("ETHUSDT") || symbol.Name.Equals("ADABTC") || symbol.Name.Equals("LEVERBTC"))
                        queue.Enqueue(symbol);
                    }

                    int symbolTotal = queue.Count;
                    int symbolsDone = 0;

                    // En dan door x tasks de queue leeg laten trekken
                    List<Task> taskList = [];
                    while (taskList.Count < 5)
                    {
                        Task task = Task.Run(async () =>
                        {
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

                                    // Er is niet geswitched van exchange (omdat het ophalen zo lang duurt)
                                    if (symbol.ExchangeId == GlobalData.ActiveExchange!.Id)
                                    {
                                        int done = Interlocked.Increment(ref symbolsDone);
                                        GlobalData.CandleProgressText = $"{done} / {symbolTotal}  ({symbol.Name})";

                                        // Haal de candles op en zorg dat deze overlapt met de candles van de socket stream(s)
                                        // De datum en tijd tot na het activeren van beide streams (overlap)
                                        CandleTools.DetermineFetchStartDate(symbol);
                                        await GetCandlesForAllIntervalsAsync(symbol);
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
                    GlobalData.CandleProgressText = "";

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


    public async Task GetCandlesForIntervalAsync(IDisposable client, CryptoSymbol symbol, CryptoInterval interval)
    {
        if (symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData!.FetchCandles)
            return;

        CandleTime currentTime = CandleTime.AlignFromDateTime(DateTimeOffset.UtcNow.UtcDateTime, 1);
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        bool intervalSupported = symbol.Exchange.IsIntervalSupported(interval.IntervalPeriod);
        if (intervalSupported)
        {
            // Fetch the candles (we have coins starting and stopping, be aware for endless loops)
            while (symbolInterval.LastCandleSynchronized < currentTime)
            {
                if (symbolInterval.LastCandleSynchronized + interval.Duration > currentTime)
                    break;

                // LastCandleSynchronized alway's has a value (minimum period start or last synched)
                CandleTime fetchFrom = symbolInterval.LastCandleSynchronized.Value;
                var (_, _, fetchedUpTo) = await Api.Candle.GetCandlesForInterval(client, symbol, interval, fetchFrom);
                symbolInterval.LastCandleSynchronized = fetchedUpTo;

                //await symbol.Data.CandleLock.WaitAsync();
                //try
                //{
                //    CandleTools.UpdateCandleFetched(symbol, interval);
                //}
                //finally
                //{
                //    symbol.Data.CandleLock.Release();
                //}

                if (symbolInterval.LastCandleSynchronized == fetchFrom) // not moving forward
                    break;
                currentTime = CandleTime.AlignFromDateTime(DateTimeOffset.UtcNow.UtcDateTime, 1);
            }
        }


        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            // once
            CandleTools.UpdateCandleFetched(symbol, interval);

            // Add missing candles (the only place we know it can be done safely)
            CandleTools.BulkAddMissingCandles(symbol, interval);

            // Bulk calculate the higher interval candles
            if (interval.IntervalPeriod < Enum.GetValues(typeof(CryptoIntervalPeriod)).Cast<CryptoIntervalPeriod>().Last())
            {
                CryptoInterval targetInterval = GlobalData.IntervalListPeriod[interval.IntervalPeriod + 1];
                CryptoInterval sourceInterval = targetInterval.ConstructFrom!;
                CandleTools.BulkCalculateCandles(symbol, sourceInterval, targetInterval, currentTime);
            }

            //// Adjust the administration for the not supported interval's
            //if (!intervalSupported)
            //{
            //    CandleTools.UpdateCandleFetched(symbol, interval);
            //}
            // twice
            CandleTools.UpdateCandleFetched(symbol, interval);
        }
        finally
        {
            symbol.Data.CandleLock.Release();
        }
    }


    public async Task<bool> FetchFrom(CryptoSymbol symbol, CryptoInterval interval, CandleTime unixLoop, CandleTime unixMax)
    {
        // Fetch the candles (we have coins starting and stopping, be aware for endless loops)
        // Kind of the same as the CandleBase.GetCandlesForIntervalAsync, but also different because
        // of the symbolInterval.LastCandleSynchronized and calculation of higher interval candles

        //if (GlobalData.Settings.General.DebugZoneCandles && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
        //    GlobalData.AddTextToLogTab($"Fetch historical data FetchFrom({symbol.Name}, {interval!.Name}, " +
        //        $"{CandleTools.GetUnixDate(unixLoop).ToLocalTime()}, {CandleTools.GetUnixDate(unixMax).ToLocalTime()})");

        int totalFetched = 0;
        if (unixLoop < unixMax)
        {
            var api = symbol.Exchange.GetApiInstance();
            using IDisposable client = api.GetClient();
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

            while (unixLoop < unixMax)
            {
                if (unixLoop + interval.Duration > unixMax)
                    break;

                CandleTime minTime = unixLoop;
                CandleTime maxTime = unixLoop + (ExchangeBase.ExchangeOptions.CandleLimit - 1) * interval.Duration;

                CandleTime lastDate = minTime;
                int countBefore = symbolInterval.CandleList.Count;
                var result = await symbol.Exchange.GetApiInstance().Candle.GetCandlesForInterval(client, symbol, interval, minTime);
                unixLoop = result.fetchedUpTo;

                int added = symbolInterval.CandleList.Count - countBefore;
                totalFetched += added;

                bool debug = GlobalData.Settings.General.DebugZoneCandles && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == "");
                if (debug)
                    ScannerLog.Logger.Info($"Core.Exchange.FetchFrom({symbol.Name}, {interval!.Name}, " +
                        $"{minTime.ToDateTime()} .. {maxTime.ToDateTime()} limit={ExchangeBase.ExchangeOptions.CandleLimit} added={added}");


                //string text3 = $"{text} retrieved={added} total={candleList.Count}";
                //ScannerLog.Logger.Info(text3);
                //GlobalData.AddTextToLogTab(text3);CandleTime

                while (symbolInterval.CandleList!.ContainsKey(unixLoop))
                    unixLoop += interval.Duration;

                if (unixLoop == minTime) // not moving forward
                    break;
            }
        }


        //if (GlobalData.Settings.General.DebugZoneCandles && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
        //    GlobalData.AddTextToLogTab($"Fetch historical data FetchFrom({symbol.Name}, {interval!.Name}, " +
        //        $"{CandleTools.GetUnixDate(unixLoop).ToLocalTime()}, {CandleTools.GetUnixDate(unixMax).ToLocalTime()}) fetched {totalFetched}");

        return totalFetched > 0;
    }


    internal static bool CheckFutureCandleReceived(DateTime openTime, CryptoSymbol symbol, CryptoInterval interval,
        decimal closePrice)
    {
        CandleTime candleTime = CandleTime.AlignFromDateTime(openTime, interval.Duration);
        CandleTime currentTime = CandleTime.AlignFromDateTime(DateTimeOffset.UtcNow.UtcDateTime, interval.Duration);
        if (candleTime + interval.Duration > currentTime)
        {
            ScannerLog.Logger.Debug($"Debug: future candle {symbol.Name} {interval.Name} {openTime.ToLocalTime()} > {candleTime.ToLocalTime()}");
            return true;
        }

        if (closePrice <= 0)
        {
            ScannerLog.Logger.Debug($"Debug: candle with close price 0 {symbol.Name} {interval.Name} {openTime.ToLocalTime()} > {candleTime.ToLocalTime()}");
            return true;
        }
        return false;
    }
}
