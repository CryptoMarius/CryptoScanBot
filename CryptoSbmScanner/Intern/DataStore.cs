using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;
using System.Text;

namespace CryptoSbmScanner.Intern;

// <summary>
// https://stackoverflow.com/questions/64799591/is-there-a-high-performance-way-to-replace-the-binaryformatter-in-net5
// </summary>


#if !SQLDATABASE
public class DataStore
{
    public static void LoadCandles()
    {
        // De candles uit de database lezen
        // Voor de 1m hebben we de laatste 2 dagen nodig (vanwege de berekening van de barometer)
        // In het algemeen is een minimum van 2 dagen OF 215 candles nodig (indicators)
        GlobalData.AddTextToLogTab("Loading information (please wait!)");

        //int aantaltotaal = 0;
        string basedir = GlobalData.GetBaseDir();
        foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values)
        {
            string dirExchange = basedir + exchange.Name.ToLower() + @"\";

            foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
            {
                string dirSymbol = dirExchange + symbol.Quote.ToLower() + @"\";

                // Verwijder het bestand indien niet relevant of niet actief
                string filename = dirSymbol + symbol.Base.ToLower(); // + ".json.bin";
                if (!symbol.IsBarometerSymbol() && (!symbol.QuoteData.FetchCandles || !symbol.IsSpotTradingAllowed))
                {
                    //if (File.Exists(filename))
                    //    File.Delete(filename);
                    continue;
                }

                foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList)
                {
                    symbolInterval.TrendIndicator = CryptoTrendIndicator.trendSideways;
                    symbolInterval.LastCandleSynchronized = null;
                    symbolInterval.LastStobbOrdSbmDate = null;
                    symbolInterval.TrendInfoDate = null;
                }

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

                                if (version >= 2)
                                {
                                    long lastStobbOrdSbmDate = binaryReader.ReadInt64();
                                    if (lastStobbOrdSbmDate == 0)
                                        symbolInterval.LastStobbOrdSbmDate = null;
                                    else
                                        symbolInterval.LastStobbOrdSbmDate = CandleTools.GetUnixDate(lastStobbOrdSbmDate);
                                }

                                int candleCount = binaryReader.ReadInt32();
                                while (candleCount > 0)
                                {
                                    CryptoCandle candle = new()
                                    {
#if SQLDATABASE
                                        ExchangeId = symbol.Exchange.Id,
                                        SymbolId = symbol.Id,
                                        IntervalId = symbolInterval.Interval.Id,
#endif
                                        //Symbol = symbol,
                                        //Interval = symbolInterval.Interval,
                                        OpenTime = binaryReader.ReadInt64(),
                                        Open = binaryReader.ReadDecimal(),
                                        High = binaryReader.ReadDecimal(),
                                        Low = binaryReader.ReadDecimal(),
                                        Close = binaryReader.ReadDecimal(),
                                        Volume = binaryReader.ReadDecimal()
                                    };

                                    symbolInterval.CandleList.Add(candle.OpenTime, candle);

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
                        GlobalData.Logger.Error(error);
                        GlobalData.AddTextToLogTab(error.ToString() + "\r\n");
                        //throw;
                    }
                    catch (Exception error)
                    {
                        GlobalData.AddTextToLogTab("Problem " + symbol.Name);
                        GlobalData.Logger.Error(error);
                        GlobalData.AddTextToLogTab(error.ToString() + "\r\n");
                        // Een vorig formaat
                        File.Delete(filename);
                        //throw;
                    }

                }
            }
        }
        GlobalData.AddTextToLogTab("Information loaded");
    }


    public static void SaveCandles()
    {
        GlobalData.AddTextToLogTab("Saving information (please wait!)");

        string basedir = GlobalData.GetBaseDir();
        foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values)
        {
            string dirExchange = basedir + exchange.Name.ToLower() + @"\";

            foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
            {
                string dirSymbol = dirExchange + symbol.Quote.ToLower() + @"\";

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
                    Directory.CreateDirectory(dirSymbol);

                    // Een experiment (vanwege de obfuscator)
                    using FileStream writeStream = new(filename, FileMode.Create);

                    using (BinaryWriter binaryWriter = new(writeStream, Encoding.UTF8, false))
                    {
                        int version = 1; // Even teruggezet

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

                            if (version >= 2)
                            {
                                long lastStobbOrdSbmDate = 0;
                                if (symbolInterval.LastStobbOrdSbmDate.HasValue)
                                    lastStobbOrdSbmDate = CandleTools.GetUnixTime((DateTime)symbolInterval.LastStobbOrdSbmDate, 60);
                                binaryWriter.Write((long)lastStobbOrdSbmDate); // int64
                            }
                            binaryWriter.Write(symbolInterval.CandleList.Count);

                            foreach (CryptoCandle candle in symbolInterval.CandleList.Values)
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
                    writeStream.Close();

                }
            }
        }

        GlobalData.AddTextToLogTab("Information saved");
    }


}
#endif
