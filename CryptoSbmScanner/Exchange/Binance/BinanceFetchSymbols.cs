using System.Text.Encodings.Web;
using System.Text.Json;

using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Objects;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Exchange.Binance;

public class BinanceFetchSymbols
{
    private static void SaveInformation(BinanceExchangeInfo binanceExchangeInfo)
    {
        //Laad de gecachte (langere historie, minder overhad)
        string filename = GlobalData.GetBaseDir();
        filename += @"\Binance\";
        Directory.CreateDirectory(filename);
        filename += "Binance.Exchangeinfo.json";

        //using (FileStream writeStream = new FileStream(filename, FileMode.Create))
        //{
        //    BinaryFormatter formatter = new BinaryFormatter();
        //    formatter.Serialize(writeStream, GlobalData.Settings);
        //    writeStream.Close();
        //}

        string text = JsonSerializer.Serialize(binanceExchangeInfo, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
        //var accountFile = new FileInfo(filename);
        File.WriteAllText(filename, text);
    }

    public static async Task ExecuteAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab("Reading symbol information from Binance");
                BinanceWeights.WaitForFairBinanceWeight(1);

                WebCallResult<BinanceExchangeInfo> exchangeInfo = null;
                using (var client = new BinanceClient())
                {
                    exchangeInfo = await client.SpotApi.ExchangeData.GetExchangeInfoAsync();

                    if (!exchangeInfo.Success)
                    {
                        GlobalData.AddTextToLogTab("error getting exchangeinfo " + exchangeInfo.Error + "\r\n");
                    }

                    //Zo af en toe komt er geen data of is de Data niet gezet.
                    //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                    if (exchangeInfo == null || exchangeInfo.Data == null)
                        throw new ExchangeException("Geen exchange data ontvangen");
                }
                exchange.ExchangeInfoLastTime = DateTime.UtcNow;



                using (CryptoDatabase database = new())
                {
                    database.Close();
                    database.Open();
                    using (var transaction = database.BeginTransaction())
                    {
                        List<CryptoSymbol> cache = new();
                        try
                        {
                            foreach (var binanceSymbol in exchangeInfo.Data.Symbols)
                            {
                                //string coin = MatchingSymbol(item.Name);
                                //if (coin != "")
                                {
                                    ////
                                    //// Summary:
                                    ////     Status of a symbol
                                    //    public enum SymbolStatus
                                    //    {
                                    //        //
                                    //        // Summary:
                                    //        //     Not trading yet
                                    //        PreTrading = 0,
                                    //        //
                                    //        // Summary:
                                    //        //     Trading
                                    //        Trading = 1,
                                    //        //
                                    //        // Summary:
                                    //        //     No longer trading
                                    //        PostTrading = 2,
                                    //        //
                                    //        // Summary:
                                    //        //     Not trading
                                    //        EndOfDay = 3,
                                    //        //
                                    //        // Summary:
                                    //        //     Halted
                                    //        Halt = 4,
                                    //        AuctionMatch = 5,
                                    //        Break = 6
                                    //    }

                                    //Het is erg belangrijk om de delisted munten zo snel mogelijk te detecteren.
                                    //(ik heb wat slechte ervaringen met de Altrady bot die op paniek pieken handelt)

                                    //Eventueel symbol toevoegen
                                    if (!exchange.SymbolListName.TryGetValue(binanceSymbol.Name, out CryptoSymbol symbol))
                                    {
                                        symbol = new()
                                        {
                                            Exchange = exchange,
                                            ExchangeId = exchange.Id,
                                            Name = binanceSymbol.Name,
                                            Base = binanceSymbol.BaseAsset,
                                            Quote = binanceSymbol.QuoteAsset,
                                            Status = 1,
                                        };
                                    }

                                    //Tijdelijk alles overnemen (vanwege into nieuwe velden)
                                    //De te gebruiken precisie in prijzen
                                    symbol.BaseAssetPrecision = binanceSymbol.BaseAssetPrecision;
                                    symbol.QuoteAssetPrecision = binanceSymbol.QuoteAssetPrecision;
                                    // Tijdelijke fix voor Binance.net (kan waarschijnlijk weer weg)
                                    if (binanceSymbol.MinNotionalFilter != null)
                                        symbol.MinNotional = binanceSymbol.MinNotionalFilter.MinNotional;
                                    else
                                        symbol.MinNotional = 0;

                                    //Minimale en maximale amount voor een order (in base amount)
                                    symbol.QuantityMinimum = binanceSymbol.LotSizeFilter.MinQuantity;
                                    symbol.QuantityMaximum = binanceSymbol.LotSizeFilter.MaxQuantity;
                                    symbol.QuantityTickSize = binanceSymbol.LotSizeFilter.StepSize;

                                    //Minimale en maximale prijs voor een order (in base price)
                                    symbol.PriceMinimum = binanceSymbol.PriceFilter.MinPrice;
                                    symbol.PriceMaximum = binanceSymbol.PriceFilter.MaxPrice;
                                    symbol.PriceTickSize = binanceSymbol.PriceFilter.TickSize;

                                    symbol.IsSpotTradingAllowed = binanceSymbol.IsSpotTradingAllowed;
                                    symbol.IsMarginTradingAllowed = binanceSymbol.IsMarginTradingAllowed;

                                    if (binanceSymbol.Status == SymbolStatus.Trading | binanceSymbol.Status == SymbolStatus.EndOfDay)
                                        symbol.Status = 1;
                                    else
                                        symbol.Status = 0; //Zet de status door (PreTrading, PostTrading of Halt)

                                    if (symbol.Id == 0)
                                    {
#if !SQLDATABASE
                                        database.Connection.Insert(symbol, transaction);
#endif
                                        cache.Add(symbol);
                                    }
                                    else
                                        database.Connection.Update(symbol, transaction);
                                }
                            }
#if SQLDATABASE
                            database.BulkInsertSymbol(cache, transaction);
#endif
                            transaction.Commit();

                            SaveInformation(exchangeInfo.Data);



                            // De nieuwe symbols toevoegen aan de lijst
                            // (omdat de symbols pas tijdens de BulkInsert een id krijgen)
                            foreach (CryptoSymbol symbol in cache)
                            {
                                GlobalData.AddSymbol(symbol);
                            }



                            // De index lijsten opbouwen (een gedeelte van de ~2100 munten)
                            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
                            {
                                // Lock (zie onder andere de BarometerTools)
                                Monitor.Enter(quoteData.SymbolList);
                                try
                                {
                                    quoteData.SymbolList.Clear();
                                    foreach (var symbol in exchange.SymbolListName.Values)
                                    {
                                        if (symbol.Quote.Equals(quoteData.Name) && symbol.Status == 1 && !symbol.IsBarometerSymbol())
                                        {
                                            quoteData.SymbolList.Add(symbol);
                                        }
                                    }
                                }
                                finally
                                {
                                    Monitor.Exit(quoteData.SymbolList);
                                }
                            }

                            // De (nieuwe)muntparen toevoegen aan de userinterface
                            GlobalData.SymbolsHaveChanged("");

                        }
                        catch (Exception error)
                        {
                            GlobalData.Logger.Error(error);
                            GlobalData.AddTextToLogTab(error.ToString());
                            transaction.Rollback();
                            throw;
                        }
                    }
                    database.Close();
                }
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab(error.ToString());
            }

        }
    }
}