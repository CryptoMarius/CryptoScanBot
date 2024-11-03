using CryptoScanBot.Core.Model;

using Bybit.Net.Clients;
using Bybit.Net.Enums;

using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;

namespace CryptoScanBot.Core.Experimental;

public static class CryptoCandles
{
    public static DateTime StartupTime { get; set; } = DateTime.UtcNow;

    public static string LoadedCandlesFrom { get; set; } = "";
    public static SortedList<CryptoIntervalPeriod, bool> LoadedCandlesInMemory { get; set;} = [];

    public static readonly JsonSerializerOptions JsonSerializerIndented = new()
    { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true, IncludeFields = true };


    //https://www.linkedin.com/pulse/fibonacci-retracement-using-c-net-kamal-chanchal/
    public static double[] Retracement(double high, double low)
    {
        double[] retracements = new double[3];
        double difference = high - low;
        retracements[0] = high - difference * 0.236;
        retracements[1] = high - difference * 0.382;
        retracements[2] = high - difference * 0.618;
        return retracements;
    }


    // https://stackoverflow.com/questions/4564472/how-can-i-return-a-fibonacci-series-should-i-use-ilist
    public static IEnumerable<int> Fibonacci(int x)
    {
        int prev = -1;
        int next = 1;
        for (int i = 0; i < x; i++)
        {
            int sum = prev + next;
            prev = next;
            next = sum;
            yield return sum;
        }
    }

    // https://www.naukri.com/code360/library/fibonacci-series-in-c-sharp
    //public class FibonacciExample
    //{
    //    public static void Main(string[] args)
    //    {
    //        int x = 0, y = 1, z, i, num = 10;

    //        Console.Write(x + " " + y + " ");
    //        for (i = 2; i < num; ++i)
    //        {
    //            z = x + y;
    //            Console.Write(z + " ");
    //            x = y;
    //            y = z;
    //        }
    //    }
    //}

    public static async Task GetCandleData(CryptoSymbol symbol, CryptoSymbolInterval symbolInterval, StringBuilder log, long fetchFrom, bool fetchAll, int okayCount)
    {
        //fetchAll = true;
        //if (LoadedCandlesInMemory.TryGetValue(SymbolInterval.IntervalPeriod, out bool _))
        //    return;
        KlineInterval? intervalEnumX = Exchange.BybitApi.Spot.Interval.GetExchangeInterval(symbolInterval.IntervalPeriod);
        if (intervalEnumX == null)
            throw new Exception("Not supported interval");
        KlineInterval intervalEnum = (KlineInterval)intervalEnumX;


        if (!LoadedCandlesInMemory.TryGetValue(symbolInterval.IntervalPeriod, out bool _))
        {
            // load candles (kind of quick and dirty)
            string baseFolder = GlobalData.GetBaseDir() + @"Pivots\";
            string filename = baseFolder + $"{symbol.Name}-{symbolInterval.Interval.Name}.json";
            if (File.Exists(filename))
            {
                int added = 0;
                string text = File.ReadAllText(filename);
                var list = JsonSerializer.Deserialize<SortedList<long, CryptoCandle>>(text, JsonSerializerIndented);
                if (list != null)
                {
                    foreach (var c in list.Values)
                    {
                        if (symbolInterval.CandleList.TryAdd(c.OpenTime, c))
                            added++;
                    }
                }
                //GlobalData.AddTextToLogTab($"{symbol.Name} {symbolInterval.Interval!.Name} Loading file {filename} {added} candles added");
            }
            LoadedCandlesInMemory.Add(symbolInterval.IntervalPeriod, false);
            //using (FileStream readStream = new FileStream(filename, FileMode.Open)) ?obsolete?
            //{
            //    BinaryFormatter formatter = new BinaryFormatter();
            //    SymbolInterval.CandleList = (SortedList<long, CryptoCandle>)formatter.Deserialize(readStream);
            //    readStream.Close();
            //}
        }




        int available = 0;
        long unixStartTime = IntervalTools.StartOfIntervalCandle(fetchFrom, symbolInterval.Interval.Duration);
        //if (unixStartTime == 1730170800 && symbolInterval.Interval.Name == "3m")
        //    unixStartTime = unixStartTime;
        while (symbolInterval.CandleList!.ContainsKey(unixStartTime))
        {
            available++;
            if (available >= okayCount)
            {
                // okay
                //string text2 = $"available={available}, need={okayCount}";
                //log.AppendLine($"Fetch historical data {symbol.Name}, {symbolInterval.Interval!.Name} candles available, no refresh needed {text2}");
                //ScannerLog.Logger.Info($"Fetch historical data {symbol.Name}, {symbolInterval.Interval!.Name} candles available, no refresh needed {text2}");
                //GlobalData.AddTextToLogTab($"Fetch historical data {symbol.Name}, {symbolInterval.Interval!.Name} candles available, no refresh needed {text2}");
                return;
            }

            unixStartTime += symbolInterval.Interval.Duration;
        }

        // calculate the "current" time
        long unixMaxTime = CandleTools.GetUnixTime(StartupTime, symbolInterval.Interval.Duration);
        if (unixStartTime >= unixMaxTime)
            return;

        int maxFetch = 1000;
        DateTime startTime = CandleTools.GetUnixDate(unixStartTime);
        DateTime endTime = startTime.AddSeconds(maxFetch * symbolInterval.Interval.Duration);
        string text3 = $"available={available}, need={okayCount}";
        //GlobalData.AddTextToLogTab($"Fetch historical data {symbol.Name} {symbolInterval.Interval!.Name} need more data {text3} {startTime.ToLocalTime()} .. {endTime.ToLocalTime()}");

        using var client = new BybitRestClient();
        //if (unixStartTime == 1730170800 && symbolInterval.Interval.Name == "3m")
        //    unixStartTime = unixStartTime;
        while (unixStartTime < unixMaxTime)
        {
            startTime = CandleTools.GetUnixDate(unixStartTime);
            endTime = startTime.AddSeconds((maxFetch - 1) * symbolInterval.Interval.Duration);
            string text = $"Fetch historical data {symbol.Name} {symbolInterval.Interval!.Name} from {startTime.ToLocalTime()} up to {endTime.ToLocalTime()}";

            var result = await client.V5Api.ExchangeData.GetKlinesAsync(Category.Spot, symbol.Name, intervalEnum, startTime: startTime, endTime: endTime, limit: maxFetch);
            if (!result.Success)
            {
                log.AppendLine($"{text} Error fetching data: " + result.Error!.Message);
                ScannerLog.Logger.Info($"{text} Error fetching data: " + result.Error!.Message);
                GlobalData.AddTextToLogTab($"{text} Error fetching data: " + result.Error!.Message);
                break;
            }
            if (result.Data == null)
            {
                log.AppendLine("{text} Failed to fetch historical data.");
                ScannerLog.Logger.Info($"{text} Failed to fetch historical data.");
                GlobalData.AddTextToLogTab($"{text} Failed to fetch historical data.");
                break;
            }

            int added = 0;
            foreach (var data in result.Data.List)
            {
                long time = CandleTools.GetUnixTime(data.StartTime, 60);
                if (!symbolInterval.CandleList.ContainsKey(time))
                {
                    symbolInterval.CandleList.TryAdd(time, new CryptoCandle
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
            string text4 = $"{text} retrieved={result.Data.List.Count()} added={added} total={symbolInterval.CandleList.Count}"; // {result.RequestUrl}
            log.AppendLine(text4);
            ScannerLog.Logger.Info(text4);
            GlobalData.AddTextToLogTab(text4);

            //// ergerlijk
            //if (result.Data.List.Count() == 1000 && added == 0)
            //{
            //    var baseFolder = GlobalData.GetBaseDir();
            //    Directory.CreateDirectory(baseFolder);
            //    var filename = baseFolder + $"{unixStartTime}-{symbolInterval.Interval.Name}.json";
            //    string text6 = JsonSerializer.Serialize(result, JsonSerializerIndented);
            //    text6 += "\r\n local time  \r\n";
            //    text6 += "\r\n" + startTime.ToLocalTime() + "\r\n";
            //    text6 += "\r\n" + endTime.ToLocalTime() + "\r\n";
            //    text6 += "\r\n UTC \r\n";
            //    text6 += $"\r\n {startTime} \r\n";
            //    text6 += $"\r\n {endTime} \r\n";
            //    text6 += "\r\n unix \r\n";
            //    text6 += $"\r\n {unixStartTime} \r\n";
            //    File.WriteAllText(filename, text6);
            //}


            // save candles for the next Session (it stay's forever buffered)
            if (added > 0)
                LoadedCandlesInMemory[symbolInterval.IntervalPeriod] = true;

            unixStartTime += (maxFetch - 1) * symbolInterval.Interval.Duration;
            while (symbolInterval.CandleList!.ContainsKey(unixStartTime))
                unixStartTime += symbolInterval.Interval.Duration;

            if (!fetchAll)
                break;

            Thread.Sleep(1000);
        }
    }

    public static void SaveAddedCandleData(CryptoSymbol symbol, StringBuilder log)
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
                LoadedCandlesInMemory[symbolInterval.IntervalPeriod] = false;
            }
        }
    }
}
