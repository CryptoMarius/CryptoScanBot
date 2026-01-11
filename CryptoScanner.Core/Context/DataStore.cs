using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using System.IO.Compression;
using System.Text;

namespace CryptoScanner.Core.Context;

// <summary>
// https://stackoverflow.com/questions/64799591/is-there-a-high-performance-way-to-replace-the-binaryformatter-in-net5
// </summary>


public class DataStore
{
    // Prevent multiple sessions
    private static readonly SemaphoreSlim Semaphore = new(1);

    private static void ReadCandlesFromStream(BinaryReader binaryReader, CryptoSymbol symbol)
    {
        int version = binaryReader.ReadInt32();
        string text = binaryReader.ReadString();
        if (version >= 1 && version <=2 && symbol.Name.Equals(text))
        {
            foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
            {
                // The weekly interval was introduced in version 2 of the storage
                if (version == 1 && symbolInterval.IntervalPeriod == CryptoIntervalPeriod.interval1w)
                    continue;

                CryptoIntervalPeriod intervalPeriod = (CryptoIntervalPeriod)binaryReader.ReadInt32();
                if (intervalPeriod != symbolInterval.IntervalPeriod)
                    throw new Exception($"file {symbol.Name} is corrupted (interval {intervalPeriod} does not match)");
                symbolInterval.LastCandleSynchronized = binaryReader.ReadInt64();
                if (symbolInterval.LastCandleSynchronized == 0)
                    symbolInterval.LastCandleSynchronized = null;

                // max candle date
                // For some reason we can have corrupted candles in the system.
                // This killed the scanner because it had a loop until maxLong!
                long futureCandles = CandleTools.GetUnixTime(DateTime.UtcNow.AddDays(1), 60);

                // min candle date
                long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, symbolInterval.Interval, DateTime.UtcNow);

                // Load interval from stream
                int candleCount = binaryReader.ReadInt32();
                while (candleCount > 0)
                {
                    CryptoCandle candle = new()
                    {
                        OpenTime = binaryReader.ReadInt64(),
                        Open = binaryReader.ReadDecimal(),
                        High = binaryReader.ReadDecimal(),
                        Low = binaryReader.ReadDecimal(),
                        Close = binaryReader.ReadDecimal(),
                        Volume = binaryReader.ReadDecimal(),
                    };

                    // We had some data corruption and 1 candle in the year 2150...
                    // It is not a nice solution, but skip those candles (really weird)
                    if (candle.OpenTime >= startFetchUnix)
                    {
                        if (candle.OpenTime < futureCandles)
                        {
                            symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
                            if (symbolInterval.LastCandle == null || candle.OpenTime >= symbolInterval.LastCandle.OpenTime)
                                symbolInterval.LastCandle = candle;
                        }
                        else
                            GlobalData.AddTextToLogTab($"{symbol.Name} skipped corrupted candle {candle.OpenTime}");
                    }

                    candleCount--;
                }
            }
        }
    }

    private static void LoadCandleForSymbol(string exchangeStoragePath, CryptoSymbol symbol)
    {
        symbol.LastPrice = null;
        string oldFileName = Path.Combine(exchangeStoragePath, symbol.Quote.ToLower(), symbol.Base.ToLower());
        string newFileName = Path.ChangeExtension(oldFileName, ".compressed");

        // reset the previous collected trend data (once a day is preferred)
        CryptoSymbolData accountSymbolData = symbol.Data;
        accountSymbolData.ResetTrendData();

        string fileName = string.Empty;
        {
            try
            {
                // an old uncompressed file
                if (File.Exists(oldFileName))
                {
                    fileName = oldFileName;
                    using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.None, 2 * 1024 * 1024);
                    using BinaryReader binaryReader = new(fileStream, Encoding.UTF8, false);
                    ReadCandlesFromStream(binaryReader, symbol);
                }
                // a new compressed file (preferred)
                else if (File.Exists(newFileName))
                {
                    fileName = newFileName;
                    using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.None, 2 * 1024 * 1024);
                    using GZipStream zipStream = new(fileStream, CompressionMode.Decompress);
                    using BinaryReader binaryReader = new(zipStream, Encoding.UTF8, false);
                    ReadCandlesFromStream(binaryReader, symbol);
                }
            }
            catch (Exception error)
            {
                GlobalData.AddTextToLogTab("Problem " + symbol.Name);
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
                File.Delete(fileName);
            }

        }
    }

    public static void LoadCandles()
    {
        // De candles uit de database lezen
        // Voor de 1m hebben we de laatste 2 dagen nodig (vanwege de berekening van de barometer)
        // In het algemeen is een minimum van 2 dagen OF 215 candles nodig (indicators)
        GlobalData.AddTextToLogTab("Loading candle information (please wait!)");

        var exchange = GlobalData.ActiveExchange;
        if (exchange != null)
        {
            string folderName = Path.Combine(GlobalData.AppDataFolder, exchange.Name.ToLower());

            foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
            {
                // ignore inactive
                if (symbol.QuoteData.FetchCandles && symbol.Status == 1)
                {
                    // Dont load candles for symbols below the minimal volume treshold
                    if (!symbol.IsBarometerSymbol() && !symbol.EnoughVolume())
                    {
                        ScannerLog.Logger.Trace($"Cleared candles for {symbol.Name}");
                        symbol.ClearCandles();
                        continue;
                    }

                    LoadCandleForSymbol(folderName, symbol);
                }
            }
        }
        //GlobalData.AddTextToLogTab("Information loaded");
    }


    public static async Task SaveCandlesAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            GlobalData.AddTextToLogTab("Saving candle information (please wait!)");

            foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values.ToList())
            {
                string folderName = Path.Combine(GlobalData.AppDataFolder, exchange.Name.ToLower());

                for (int i = 0; i < exchange.SymbolListName.Count; i++)
                {
                    CryptoSymbol symbol = exchange.SymbolListName.Values[i];
                    string quoteFolder = Path.Combine(folderName, symbol.Quote.ToLower());
                    try
                    {
                        // Delete any uncompressed file
                        string oldfileName = Path.Combine(quoteFolder, symbol.Base.ToLower());
                        if (File.Exists(oldfileName))
                            File.Delete(oldfileName);
                        

                        string fileName = Path.ChangeExtension(oldfileName, ".compressed");

                        // Dont save candles for symbols below the minimal volume treshold
                        if (!symbol.IsBarometerSymbol() && !symbol.EnoughVolume())
                        {
                            symbol.ClearCandles();
                        }

                        long count = 0;
                        foreach (CryptoSymbolInterval cryptoSymbolInterval in symbol.Data.SymbolIntervalList)
                            count += cryptoSymbolInterval.CandleList.Count;

                        // Delete the file if there is no data
                        if (!symbol.QuoteData.FetchCandles || symbol.Status == 0 || count == 0)
                        {
                            if (File.Exists(fileName))
                                File.Delete(fileName);
                            continue;
                        }

                        if (count > 0)
                        {
                            Directory.CreateDirectory(quoteFolder);
                            ScannerLog.Logger.Trace($"Saving candle information for {symbol.Name} candle count={count}");
                            

                            await symbol.Data.CandleLock.WaitAsync();
                            try
                            {
                                using FileStream writeStream = new(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 2 * 1024 * 1024);
                                using GZipStream zipStream = new(writeStream, CompressionLevel.Optimal);
                                using BinaryWriter binaryWriter = new(zipStream, Encoding.UTF8, false);

                                int version = 2; // Version 2 adds the weekly interval
                                binaryWriter.Write(version);
                                binaryWriter.Write(symbol.Name);

                                foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
                                {
                                    binaryWriter.Write((int)symbolInterval.Interval.IntervalPeriod);
                                    if (symbolInterval.LastCandleSynchronized.HasValue)
                                        binaryWriter.Write((long)symbolInterval.LastCandleSynchronized);
                                    else
                                        binaryWriter.Write((long)0);

                                    binaryWriter.Write(symbolInterval.CandleList.Count);

                                    foreach (var pair in symbolInterval.CandleList)
                                    {
                                        CryptoCandle? candle = pair.Value;
                                        if (candle != null)
                                        {
                                            binaryWriter.Write(candle.OpenTime);
                                            binaryWriter.Write(candle.Open);
                                            binaryWriter.Write(candle.High);
                                            binaryWriter.Write(candle.Low);
                                            binaryWriter.Write(candle.Close);
                                            binaryWriter.Write(candle.Volume);
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                symbol.Data.CandleLock.Release();
                            }
                        }
                    }
                    catch (Exception error)
                    {
                        ScannerLog.Logger.Error(error, "");
                        GlobalData.AddTextToLogTab($"Problem {symbol.Name}");
                        GlobalData.AddTextToLogTab(error.ToString());
                    }
                }
            }

            ScannerLog.Logger.Trace("Candle information saved");
        }
        finally
        {
            // Enabled analysing
            GlobalData.SetCandleTimerEnable(true);

            Semaphore.Release();
        }
    }


}

