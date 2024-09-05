using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using System.Text;

namespace CryptoScanBot.Core.Context;

// <summary>
// https://stackoverflow.com/questions/64799591/is-there-a-high-performance-way-to-replace-the-binaryformatter-in-net5
// </summary>


public class DataStore
{
    // Prevent multiple sessions
    private static readonly SemaphoreSlim Semaphore = new(1);

    public static void LoadCandleForSymbol(string exchangeStoragePath, CryptoSymbol symbol)
    {
        symbol.AskPrice = null;
        symbol.BidPrice = null;
        symbol.LastPrice = null;
        string dirSymbol = exchangeStoragePath + symbol.Quote.ToLower() + @"\";

        // Verwijder het bestand indien niet relevant of niet actief
        string filename = dirSymbol + symbol.Base.ToLower(); // + ".json.bin";

        // reset the precious collected trend data (once a day is preferred)
        AccountSymbolData accountSymbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
        accountSymbolData.Reset();

        // Laad in 1x alle intervallen 
        if (File.Exists(filename))
        {
            try
            {
                // Een experiment (vanwege de obfuscator)
                using FileStream readStream = new(filename, FileMode.Open);

                using (BinaryReader binaryReader = new(readStream, Encoding.UTF8, false))
                {
                    // Iets met een version
                    int version = binaryReader.ReadInt32();
                    string text = binaryReader.ReadString();

                    while (readStream.Position != readStream.Length)
                    {
                        CryptoIntervalPeriod intervalPeriod = (CryptoIntervalPeriod)binaryReader.ReadInt32();
                        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(intervalPeriod);
                        symbolInterval.LastCandleSynchronized = binaryReader.ReadInt64();
                        if (symbolInterval.LastCandleSynchronized == 0)
                            symbolInterval.LastCandleSynchronized = null;

                        long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, symbolInterval.Interval, DateTime.UtcNow);

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
                                Volume = binaryReader.ReadDecimal()
                            };

                            if (candle.OpenTime >= startFetchUnix)
                                symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);

                            candleCount--;
                        }
                    }
                }
                readStream.Close();
            }
            catch (InvalidCastException error)
            {
                // Een vorig formaat
                File.Delete(filename);
                ScannerLog.Logger.Error(error, symbol.Name);
                GlobalData.AddTextToLogTab(error.ToString());
                //throw;
            }
            catch (Exception error)
            {
                GlobalData.AddTextToLogTab("Problem " + symbol.Name);
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
                // Een vorig formaat
                File.Delete(filename);
                //throw;
            }

        }
    }

    public static void LoadCandles()
    {
        // De candles uit de database lezen
        // Voor de 1m hebben we de laatste 2 dagen nodig (vanwege de berekening van de barometer)
        // In het algemeen is een minimum van 2 dagen OF 215 candles nodig (indicators)
        GlobalData.AddTextToLogTab("Loading candle information (please wait!)");

        //int aantaltotaal = 0;
        string baseStoragePath = GlobalData.GetBaseDir();
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange? exchange))
        {
            string exchangeStoragePath = baseStoragePath + exchange.Name.ToLower() + @"\";

            foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
            {
                if (!symbol.IsBarometerSymbol() && (!symbol.QuoteData!.FetchCandles && symbol.IsSpotTradingAllowed))
                    LoadCandleForSymbol(exchangeStoragePath, symbol);
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

            string baseStoragePath = GlobalData.GetBaseDir();
            foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values.ToList())
            {
                string exchangeStoragePath = baseStoragePath + exchange.Name.ToLower() + @"\";

                for (int i = 0; i < exchange.SymbolListName.Count; i++)
                {
                    CryptoSymbol symbol = exchange.SymbolListName.Values[i];
                    string dirSymbol = exchangeStoragePath + symbol.Quote.ToLower() + @"\";
                    try
                    {

                        // Verwijder het bestand indien niet relevant of niet actief
                        string filename = dirSymbol + symbol.Base.ToLower(); // + ".json.bin";
                        if (!symbol.IsBarometerSymbol() && (!symbol.QuoteData.FetchCandles || !symbol.IsSpotTradingAllowed))
                        {
                            //if (File.Exists(filename))
                            //    File.Delete(filename);
                            continue;
                        }

                        long count = 0;
                        foreach (CryptoSymbolInterval cryptoSymbolInterval in symbol.IntervalPeriodList)
                            count += cryptoSymbolInterval.CandleList.Count;

                        if (count > 0)
                        {
                            using (var memoryStream = new MemoryStream(2 * 1024 * 1024))
                            {
                                using (BinaryWriter binaryWriter = new(memoryStream, Encoding.UTF8, false))
                                {
                                    int version = 1;

                                    // Iets met een version
                                    binaryWriter.Write(version);
                                    binaryWriter.Write(symbol.Name);

                                    foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList)
                                    {
                                        binaryWriter.Write((int)symbolInterval.Interval.IntervalPeriod);
                                        if (symbolInterval.LastCandleSynchronized.HasValue)
                                            binaryWriter.Write((long)symbolInterval.LastCandleSynchronized); // int64
                                        else
                                            binaryWriter.Write((long)0); // int64

                                        binaryWriter.Write(symbolInterval.CandleList.Count);

                                        for (int j = 0; j < symbolInterval.CandleList.Count; j++)
                                        {
                                            CryptoCandle candle = symbolInterval.CandleList.Values[j];
                                            binaryWriter.Write(candle.OpenTime);
                                            binaryWriter.Write(candle.Open);
                                            binaryWriter.Write(candle.High);
                                            binaryWriter.Write(candle.Low);
                                            binaryWriter.Write(candle.Close);
                                            binaryWriter.Write(candle.Volume);
                                        }
                                    }
                                    Directory.CreateDirectory(dirSymbol);
                                    using (FileStream writeStream = new(filename, FileMode.Create))
                                    {
                                        memoryStream.Position = 0;
                                        memoryStream.CopyTo(writeStream);
                                        writeStream.Close();
                                    }
                                    binaryWriter.Close();
                                }
                                memoryStream.Close();
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

