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

                WebCallResult<BinanceExchangeInfo> exchangeInfo = null;
                var client = new BinanceRestClient();
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

                // Om achteraf de niet gedeactiveerde munten te melden en te deactiveren
                List<string> reportSymbols = new();
                SortedList<string, CryptoSymbol> activeSymbols = new();


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = new();
                    try
                    {
                        foreach (var symbolData in exchangeInfo.Data.Symbols)
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
                            if (!exchange.SymbolListName.TryGetValue(symbolData.Name, out CryptoSymbol symbol))
                            {
                                symbol = new()
                                {
                                    Exchange = exchange,
                                    ExchangeId = exchange.Id,
                                    Name = symbolData.Name,
                                    Base = symbolData.BaseAsset,
                                    Quote = symbolData.QuoteAsset,
                                    Status = 1,
                                };
                            }

                            //Tijdelijk alles overnemen (vanwege into nieuwe velden)
                            //De te gebruiken precisie in prijzen
                            //symbol.BaseAssetPrecision = binanceSymbol.BaseAssetPrecision;
                            //symbol.QuoteAssetPrecision = binanceSymbol.QuoteAssetPrecision;
                            // Tijdelijke fix voor Binance.net (kan waarschijnlijk weer weg)
                            //if (binanceSymbol.MinNotionalFilter != null)
                            //    symbol.MinNotional = binanceSymbol.MinNotionalFilter.MinNotional;
                            //else
                            //    symbol.MinNotional = 0;

                            //Minimale en maximale amount voor een order (in base amount)
                            symbol.QuantityMinimum = symbolData.LotSizeFilter.MinQuantity;
                            symbol.QuantityMaximum = symbolData.LotSizeFilter.MaxQuantity;
                            symbol.QuantityTickSize = symbolData.LotSizeFilter.StepSize;

                            //Minimale en maximale prijs voor een order (in base price)
                            symbol.PriceMinimum = symbolData.PriceFilter.MinPrice;
                            symbol.PriceMaximum = symbolData.PriceFilter.MaxPrice;
                            symbol.PriceTickSize = symbolData.PriceFilter.TickSize;

                            symbol.IsSpotTradingAllowed = symbolData.IsSpotTradingAllowed;
                            symbol.IsMarginTradingAllowed = symbolData.IsMarginTradingAllowed;

                            if (symbolData.Status == SymbolStatus.Trading | symbolData.Status == SymbolStatus.EndOfDay)
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
                        foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
                        {
                            if (symbol.Status == 1 && !activeSymbols.ContainsKey(symbol.Name))
                            {
                                if (symbol.Status != 0)
                                {
                                    symbol.Status = 0;
                                    database.Connection.Update(symbol, transaction);

                                    reportSymbols.Add(symbol.Name);
                                }
                            }
                        }
                        if (reportSymbols.Any())
                        {
                            var symbols = string.Join(',', reportSymbols.ToArray());
                            GlobalData.AddTextToLogTab($"{reportSymbols.Count} munten gedeactiveerd {symbols}");
                        }


                        transaction.Commit();


                        // Bewaren voor debug werkzaamheden
                        {
                            string filename = GlobalData.GetBaseDir();
                            filename += $@"\{Api.ExchangeName}\";
                            Directory.CreateDirectory(filename);
                            filename += "symbols.json";

                            string text = JsonSerializer.Serialize(exchangeInfo, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
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