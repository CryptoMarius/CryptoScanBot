using System.Text.Encodings.Web;
using System.Text.Json;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Dapper.Contrib.Extensions;

using Kucoin.Net.Clients;
using Kucoin.Net.Objects.Models.Spot;

namespace CryptoSbmScanner.Exchange.Kucoin;

public class FetchSymbols
{

    public static async Task ExecuteAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading symbol information from {Api.ExchangeName}");
                KucoinWeights.WaitForFairWeight(1);

                using CryptoDatabase database = new();
                database.Open();

                using var client = new KucoinRestClient();

                /*
                "Data": [
                {
                    "Symbol": "BTC-USDT",
                    "Name": "BTC-USDT",
                    "Market": "USDS",
                    "BaseAsset": "BTC",
                    "QuoteAsset": "USDT",
                    "BaseMinQuantity": 0.00001,
                    "QuoteMinQuantity": 0.01,
                    "BaseMaxQuantity": 10000000000,
                    "QuoteMaxQuantity": 99999999,
                    "BaseIncrement": 0.00000001,
                    "QuoteIncrement": 0.000001,
                    "PriceIncrement": 0.1,
                    "PriceLimitRate": 0.1,
                    "FeeAsset": "USDT",
                    "IsMarginEnabled": true,
                    "EnableTrading": true,
                    "MinFunds": 0.1
                },
                ...
                ]
                */

                var exchangeInfo = await client.SpotApi.ExchangeData.GetSymbolsAsync();
                // Zo af en toe komt er geen data of is de Data niet gezet.
                // De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                if (exchangeInfo == null)
                    throw new ExchangeException("Geen exchange data ontvangen (1)");
                if (!exchangeInfo.Success)
                    GlobalData.AddTextToLogTab("error getting exchangeinfo " + exchangeInfo.Error + "\r\n");
                if (exchangeInfo.Data == null)
                    throw new ExchangeException("Geen exchange data ontvangen (2)");

                // Bewaren voor debug werkzaamheden
                {
                    string filename = GlobalData.GetBaseDir();
                    filename += $@"\{exchange.Name}\";
                    Directory.CreateDirectory(filename);
                    filename += "symbols.json";

                    string text = JsonSerializer.Serialize(exchangeInfo, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                    //var accountFile = new FileInfo(filename);
                    File.WriteAllText(filename, text);
                }


                /* ticker
                 
                  {
                    "Symbol": "BTC-USDT",
                    "SymbolName": "BTC-USDT",
                    "BestAskPrice": 29435.3,
                    "BestBidPrice": 29435.2,
                    "ChangePercentage": 0.0018,
                    "ChangePrice": 55.4,
                    "HighPrice": 29472.5,
                    "LowPrice": 29100.5,
                    "Volume": 1143.99893497,
                    "QuoteVolume": 33549185.280333833,
                    "LastPrice": 29435.2,
                    "AveragePrice": 29410.48473908,
                    "TakerFeeRate": 0.001,
                    "MakerFeeRate": 0.001,
                    "TakerCoefficient": 1,
                    "MakerCoefficient": 1
                  },
                */
                // Aanvullend de tickers aanroepen voor het volume...
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {Api.ExchangeName}");
                var tickersInfos = await client.SpotApi.ExchangeData.GetTickersAsync();
                // Zo af en toe komt er geen data of is de Data niet gezet.
                // De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                if (tickersInfos == null)
                    throw new ExchangeException("Geen symbol ticker data ontvangen (1)");
                if (!tickersInfos.Success)
                    GlobalData.AddTextToLogTab("error getting symbol ticker " + tickersInfos.Error + "\r\n");
                if (tickersInfos.Data == null)
                    throw new ExchangeException("Geen symbol ticker data ontvangen (2)");

                // Bewaren voor debug werkzaamheden
                {string filename = GlobalData.GetBaseDir();
                    filename += $@"\{exchange.Name}\";
                    Directory.CreateDirectory(filename);
                    filename += "tickers.json";

                    string text = JsonSerializer.Serialize(tickersInfos, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                    //var accountFile = new FileInfo(filename);
                    File.WriteAllText(filename, text);
                }


                // Om achteraf de niet aangeboden munten te deactiveren
                SortedList<string, CryptoSymbol> activeSymbols = new();


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = new();
                    try
                    {
                        foreach (var symbolData in exchangeInfo.Data)
                        {
                            // https://docs.kucoin.com/#symbols-amp-ticker
                            // https://api.kucoin.com/api/v1/symbols
                            //Eventueel symbol toevoegen
                            string symbolName = symbolData.Name.Replace("-", "");
                            if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
                            {
                                symbol = new()
                                {
                                    Exchange = exchange,
                                    ExchangeId = exchange.Id,
                                    Name = symbolName,
                                    Base = symbolData.BaseAsset,
                                    Quote = symbolData.QuoteAsset,
                                    Status = 1,
                                };
                            }

                            //Tijdelijk alles overnemen (vanwege into nieuwe velden)
                            //De te gebruiken precisie in prijzen
                            //symbol.BaseAssetPrecision = binanceSymbol.LotSizeFilter.BasePrecision.ToString().Length - 2;
                            //if (symbol.BaseAssetPrecision <= 0)
                            //    symbol.BaseAssetPrecision = 8;
                            //symbol.QuoteAssetPrecision = binanceSymbol.LotSizeFilter.QuotePrecision.ToString().Length - 2;
                            //if (symbol.QuoteAssetPrecision <= 0)
                            //    symbol.QuoteAssetPrecision = 8;
                            //symbol.MinNotional = binanceSymbol.MinNotional; // ????

                            //Minimale en maximale amount voor een order (in base amount)
                            symbol.QuantityMinimum = symbolData.BaseMinQuantity;
                            symbol.QuantityMaximum = symbolData.BaseMaxQuantity; //baseMinSize
                                                                                 // Dit klopt niet, deze heeft wederom effect op de Clamp routine!
                            symbol.QuantityTickSize = symbolData.BaseIncrement;

                            // De minimale en maximale prijs voor een order (in base price)
                            // In de definities is wel een minPrice en maxprice aanwezig, maar die is niet gevuld
                            // (dat heeft consequenties voro de werking van de Clamp die wel waarden verwacht)
                            //symbol.PriceMinimum = niet aanwezig! binanceSymbol.PriceFilter.min;
                            //symbol.PriceMaximum = niet aanwezig! binanceSymbol.LotSizeFilter.MaxOrderValue;

                            symbol.PriceTickSize = symbolData.PriceIncrement;

                            symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                            symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                            if (symbolData.EnableTrading)
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
                            activeSymbols.Add(symbol.Name, symbol);
                        }
#if SQLDATABASE
                        database.BulkInsertSymbol(cache, transaction);
#endif

                        // Deactiveer de munten die niet meer voorkomen
                        int deactivated = 0;
                        foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
                        {
                            if (symbol.Status == 1 && !symbol.IsBarometerSymbol() && !activeSymbols.ContainsKey(symbol.Name))
                            {
                                deactivated++;
                                symbol.Status = 0;
                                database.Connection.Update(symbol, transaction);
                            }
                        }
                        if (deactivated > 0)
                            GlobalData.AddTextToLogTab($"{deactivated} munten gedeactiveerd");


                        // De nieuwe symbols toevoegen aan de lijst
                        // (omdat de symbols pas tijdens de BulkInsert een id krijgen)
                        foreach (CryptoSymbol symbol in cache)
                        {
                            GlobalData.AddSymbol(symbol);
                        }



                        // Aanvullend de tickers aanroepen voor het volume...
                        foreach (var tickerInfo in tickersInfos.Data.Data)
                        {
                            string symbolName = tickerInfo.Symbol.Replace("-", "");
                            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
                            {
                                if (tickerInfo.QuoteVolume.HasValue)
                                {
                                    symbol.Volume = (decimal)tickerInfo.QuoteVolume;
                                    database.Connection.Update(symbol, transaction);
                                }
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception error)
                    {
                        GlobalData.Logger.Error(error);
                        GlobalData.AddTextToLogTab(error.ToString());
                        transaction.Rollback();
                        throw;
                    }
                }

                exchange.LastTimeFetched = DateTime.UtcNow;
                database.Connection.Update(exchange);

            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab(error.ToString());
            }

        }
    }
}