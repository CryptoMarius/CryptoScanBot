using System.Text.Encodings.Web;
using System.Text.Json;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Dapper.Contrib.Extensions;

using Kraken.Net.Clients;

namespace CryptoSbmScanner.Exchange.Kraken;

public class FetchSymbols
{

    public static async Task ExecuteAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading symbol information from {Api.ExchangeName}");
                LimitRates.WaitForFairWeight(1);

                using CryptoDatabase database = new();
                database.Open();

                //WebCallResult<BybitSpotResponse> exchangeInfo = null;
                using var client = new KrakenRestClient();
                var exchangeInfo = await client.SpotApi.ExchangeData.GetSymbolsAsync();

                // Zo af en toe komt er geen data of is de Data niet gezet.
                // De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                if (exchangeInfo == null)
                    throw new ExchangeException("Geen exchange data ontvangen (1)");
                if (!exchangeInfo.Success)
                    GlobalData.AddTextToLogTab("error getting exchangeinfo " + exchangeInfo.Error + "\r\n");
                if (exchangeInfo.Data == null)
                    throw new ExchangeException("Geen exchange data ontvangen (2)");


                // Om achteraf de niet aangeboden munten te deactiveren
                SortedList<string, CryptoSymbol> activeSymbols = new();


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = new();
                    try
                    {
                        foreach (var symbolData in exchangeInfo.Data.Values)
                        {
                            //if (coin != "")
                            {
                                string name = symbolData.AlternateName; // symbolData.BaseAsset + symbolData.QuoteAsset;
                                /*
                                    enzovoort..
                                */

                                //Eventueel symbol toevoegen
                                if (!exchange.SymbolListName.TryGetValue(name, out CryptoSymbol symbol))
                                {
                                    symbol = new()
                                    {
                                        Exchange = exchange,
                                        ExchangeId = exchange.Id,
                                        Name = name,
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
                                //symbol.MinNotional = symbolData.MinNotional; // ????

                                //Minimale en maximale amount voor een order (in base amount)
                                //symbol.QuantityMinimum = symbolData.LotSizeFilter.MinOrderQuantity;
                                //symbol.QuantityMaximum = symbolData.LotSizeFilter.MaxOrderQuantity;
                                //symbol.QuantityTickSize = symbolData.LotSizeFilter.QuantityStep;

                                // De minimale en maximale prijs voor een order (in base price)
                                // In de definities is wel een minPrice en maxprice aanwezig, maar die is niet gevuld
                                // (dat heeft consequenties voro de werking van de Clamp die wel waarden verwacht)
                                //symbol.PriceMinimum = symbolData.PriceFilter.MinPrice;
                                //symbol.PriceMaximum = symbolData.PriceFilter.MaxPrice;
                                symbol.PriceTickSize = (decimal)symbolData.TickSize; // ? binanceSymbol.PriceFilter.TickSize;

                                symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                //if (symbolData.Status == )
                                    symbol.Status = 1;
                                //else
                                  //  symbol.Status = 0; //Zet de status door (PreTrading, PostTrading of Halt)

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

                        transaction.Commit();


                        // Bewaren voor debug werkzaamheden
                        {
                            string filename = GlobalData.GetBaseDir();
                            filename += $@"\{Api.ExchangeName}\";
                            Directory.CreateDirectory(filename);
                            filename += "symbols.json";

                            string text = JsonSerializer.Serialize(exchangeInfo, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                            //var accountFile = new FileInfo(filename);
                            File.WriteAllText(filename, text);
                        }



                        // De nieuwe symbols toevoegen aan de lijst
                        // (omdat de symbols pas tijdens de BulkInsert een id krijgen)
                        foreach (CryptoSymbol symbol in cache)
                        {
                            GlobalData.AddSymbol(symbol);
                        }

                    }
                    catch (Exception error)
                    {
                        GlobalData.Logger.Error(error, "");
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
                GlobalData.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
            }

        }
    }
}