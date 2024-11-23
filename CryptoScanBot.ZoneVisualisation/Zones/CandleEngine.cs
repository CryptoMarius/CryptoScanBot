using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CryptoScanBot.ZoneVisualisation.Zones;

public class CandleEngine
{
    public static DateTime StartupTime { get; set; }
    public static SortedList<CryptoIntervalPeriod, bool> LoadedCandlesInMemory { get; set; } = [];

    public static readonly JsonSerializerOptions JsonSerializerIndented = new()
    { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true, IncludeFields = true };


    public static void LoadCandleDataFromDisk(CryptoSymbol symbol, CryptoInterval interval, CryptoCandleList candleList)
    {
        if (!LoadedCandlesInMemory.TryGetValue(interval.IntervalPeriod, out bool _))
        {
            // load candles (kind of quick and dirty)
            string baseFolder = GlobalData.GetBaseDir() + @"Pivots\";
            string filename = baseFolder + $"{symbol.Name}-{interval.Name}.json";
            if (File.Exists(filename))
            {
                string text = File.ReadAllText(filename);
                var list = JsonSerializer.Deserialize<CryptoCandleList>(text, JsonSerializerIndented);
                if (list != null)
                {
                    foreach (var c in list.Values)
                        candleList.TryAdd(c.OpenTime, c);
                }
                //GlobalData.AddTextToLogTab($"{symbol.Name} {symbolInterval.Interval!.Name} Loading file {filename} {added} candles added");
            }
            LoadedCandlesInMemory.Add(interval.IntervalPeriod, false); // in memory, nothing changed
        }

    }


    public static void SaveCandleDataToDisk(CryptoSymbol symbol, StringBuilder log)
    {
        foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList)
        {
            if (LoadedCandlesInMemory.TryGetValue(symbolInterval.IntervalPeriod, out bool changed) && changed)
            {
                // Save candles (kind of quick and dirty)
                string baseFolder = GlobalData.GetBaseDir() + @"Pivots\";
                string filename = baseFolder + $"{symbol.Name}-{symbolInterval.Interval.Name}.json";
                Directory.CreateDirectory(baseFolder);
                string text = JsonSerializer.Serialize(symbolInterval.CandleList, JsonSerializerIndented);
                File.WriteAllText(filename, text);

                log.AppendLine($"saving {filename}");
                ScannerLog.Logger.Info($"Saving {filename}");

                //using (FileStream writeStream = new FileStream(filename, FileMode.Create))
                //{
                //    BinaryFormatter formatter = new BinaryFormatter();
                //    formatter.Serialize(writeStream, SymbolInterval.CandleList);
                //    writeStream.Close();
                //}
                LoadedCandlesInMemory[symbolInterval.IntervalPeriod] = false; // in memory, nothing changed
            }
        }
    }


    public static async Task CleanLoadedCandlesAsync(CryptoSymbol symbol)
    {
        await symbol.CandleLock.WaitAsync();
        try
        {
            foreach (var symbolInterval in symbol.IntervalPeriodList)
            {
                // Remove old candles
                if (symbolInterval.CandleList.Count > 0)
                {
                    // TODO: Need end date instead of DateTime.UtcNow (works in SignalGrid, but not here)
                    long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, symbolInterval.Interval, DateTime.UtcNow);

                    // investigate the first, does it need removal?
                    CryptoCandle c = symbolInterval.CandleList.Values.First();
                    if (c.OpenTime < startFetchUnix)
                    {
                        // It takes forever to delete 100.000 of candles!!
                        //DateTime startFetchUnixDate = CandleTools.GetUnixDate(startFetchUnix);
                        //while (symbolInterval.CandleList.Values.Any())
                        //{
                        //    CryptoCandle c = symbolInterval.CandleList.Values[0];
                        //    if (c.OpenTime < startFetchUnix)
                        //    {
                        //        symbolInterval.CandleList.Remove(c.OpenTime);
                        //        //GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} candle {c.DateLocal} removed");

                        //    }
                        //    else break;

                        //    startFetchUnix += symbolInterval.Interval.Duration;
                        //}

                        // There are a *huge* amount of candles, just copy them to a new list
                        // This copies worst case 500 for the higher intervals, a bit more for the 1m
                        // TODO: Use TakeLast() does not work with sortedlist
                        CryptoCandleList newList = [];
                        int index = symbolInterval.CandleList.Count - 1;
                        while (index > 0)
                        {
                            c = symbolInterval.CandleList.Values[index];
                            if (c.OpenTime < startFetchUnix)
                                break;
                            newList.Add(c.OpenTime, c);
                            index--;
                        }
                        symbolInterval.CandleList = newList;
                    }
                }
            }
        }
        finally
        {
            symbol.CandleLock.Release();
        }
    }


    private static (long unixMin, long unixMax) CalculateDates(CryptoInterval interval, long fetchFrom, int fetchCount)
    {
        long unixMinTime = IntervalTools.StartOfIntervalCandle(fetchFrom, interval.Duration);
        long unixNowTime = CandleTools.GetUnixTime(StartupTime, 0); // todo, emulator date?
        unixNowTime = IntervalTools.StartOfIntervalCandle(unixNowTime, interval.Duration);
        long unixMaxTime = unixMinTime + fetchCount * interval.Duration;
        if (unixMaxTime > unixNowTime)
            return (unixMinTime, unixNowTime);
        else
            return (unixMinTime, unixMaxTime);
    }


    private static (long unixStartTime, bool dataAllLocal) IsDataLocal(long unixMinTime, long unixMaxTime, CryptoInterval interval, CryptoCandleList candleList)
    {
        while (candleList!.ContainsKey(unixMinTime))
        {
            if (unixMinTime >= unixMaxTime)
            {
                //string text2 = $"available={available}, need={okayCount}";
                //log.AppendLine($"Fetch historical data {symbol.Name}, {symbolInterval.Interval!.Name} candles available, no refresh needed {text2}");
                //ScannerLog.Logger.Info($"Fetch historical data {symbol.Name}, {symbolInterval.Interval!.Name} candles available, no refresh needed {text2}");
                //GlobalData.AddTextToLogTab($"Fetch historical data {symbol.Name}, {symbolInterval.Interval!.Name} candles available, no refresh needed {text2}");
                return (unixMinTime, true);
            }
            unixMinTime += interval.Duration;
        }
        return (unixMinTime, false);
    }


    public static async Task<bool> FetchFrom(CryptoSymbol symbol, CryptoInterval interval, CryptoCandleList candleList, StringBuilder log, long fetchFrom, int fetchCount = -1)
    {
        (long unixMin, long unixMax) = CalculateDates(interval, fetchFrom, fetchCount);
        (long unixLoop, bool dataAllLocal) = IsDataLocal(unixMin, unixMax, interval, candleList);
        if (dataAllLocal)
            return false;

        if (!symbol.Exchange.IsIntervalSupported(interval.IntervalPeriod))
            throw new Exception("Not supported interval");


        //string text3 = $"need={fetchCount}"; 
        //GlobalData.AddTextToLogTab($"Fetch historical data {symbol.Name} {symbolInterval.Interval!.Name} need more data {text3} {minTime.ToLocalTime()} .. {maxTime.ToLocalTime()}");

        int maxFetchCount = 1000;
        using var client = new BybitRestClient();
        while (unixLoop <= unixMax)
        {
            DateTime minTime = CandleTools.GetUnixDate(unixLoop);
            DateTime maxTime = minTime.AddSeconds((maxFetchCount - 1) * interval.Duration);
            string text = $"Fetch historical data {symbol.Name} {interval!.Name} from {minTime.ToLocalTime()} up to {maxTime.ToLocalTime()}";

            KlineInterval? intervalEnumX = Core.Exchange.BybitApi.Spot.Interval.GetExchangeInterval(interval.IntervalPeriod);
            if (intervalEnumX == null)
                throw new Exception("Not supported interval");
            KlineInterval intervalEnum = (KlineInterval)intervalEnumX;

            var result = await client.V5Api.ExchangeData.GetKlinesAsync(Category.Spot, symbol.Name, intervalEnum, startTime: minTime, endTime: maxTime, limit: maxFetchCount);
            if (!result.Success)
            {
                string text2 = $"{text} Error fetching data: {result.Error!.Message}";
                log.AppendLine(text2);
                ScannerLog.Logger.Info(text2);
                GlobalData.AddTextToLogTab(text2);
                break;
            }
            if (result.Data == null)
            {
                string text2 = $"{text} Failed to fetch historical data.";
                log.AppendLine(text2);
                ScannerLog.Logger.Info(text2);
                GlobalData.AddTextToLogTab(text2);
                break;
            }

            int added = 0;
            foreach (var data in result.Data.List)
            {
                long time = CandleTools.GetUnixTime(data.StartTime, 60);
                if (!candleList.ContainsKey(time))
                {
                    candleList.TryAdd(time, new CryptoCandle
                    {
                        OpenTime = time,
                        Open = data.OpenPrice,
                        High = data.HighPrice,
                        Low = data.LowPrice,
                        Close = data.ClosePrice,
                        Volume = data.Volume
                    });
                    added++;
                }
            }
            string text3 = $"{text} retrieved={result.Data.List.Count()} added={added} total={candleList.Count}"; // {period.RequestUrl}
            log.AppendLine(text3);
            ScannerLog.Logger.Info(text3);
            GlobalData.AddTextToLogTab(text3);

            //if (added > 0) // mark interval dirty so it will be saved
            //    LoadedCandlesInMemory[interval.IntervalPeriod] = true;

            // goto next period (we *assume* we got all the data)
            unixLoop += maxFetchCount * interval.Duration;
            while (candleList!.ContainsKey(unixLoop))
                unixLoop += interval.Duration;

            //if (fetchCount > 0 && added >= fetchCount) added is max 1000
            //    break;
            //if (fetchCount > 0 && (unixLoop - unixMin) / interval.Duration > fetchCount)
            //    break;

            //Thread.Sleep(1000);
        }
        return true;
    }

}
