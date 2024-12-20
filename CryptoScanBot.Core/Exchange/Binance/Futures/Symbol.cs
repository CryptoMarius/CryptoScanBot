using Binance.Net.Clients;
using Binance.Net.Enums;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using System.Text.Json;

namespace CryptoScanBot.Core.Exchange.Binance.Futures;

public class Symbol() : SymbolBase(), ISymbol
{
    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);

                using CryptoDatabase database = new();
                database.Open();

                using var client = new BinanceRestClient();
                var exchangeInfo = await client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync() ?? throw new ExchangeException("Geen exchange data ontvangen (1)");
                if (!exchangeInfo.Success)
                    GlobalData.AddTextToLogTab("error getting exchangeinfo " + exchangeInfo.Error);
                if (exchangeInfo.Data == null)
                    throw new ExchangeException("Geen exchange data ontvangen (2)");


                // Om achteraf de niet gedeactiveerde munten te melden en te deactiveren
                List<string> reportSymbols = [];
                SortedList<string, CryptoSymbol> activeSymbols = [];


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = [];
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

                            if (symbolData.Name != symbolData.BaseAsset + symbolData.QuoteAsset)
                            {
                                //GlobalData.AddTextToLogTab($"Ignoring symbol {symbolData.Name} {symbolData.BaseAsset} {symbolData.QuoteAsset} weird name?");
                                continue;
                            }

                            //Eventueel symbol toevoegen
                            if (!exchange.SymbolListName.TryGetValue(symbolData.Name, out CryptoSymbol? symbol))
                            {
                                var quoteData = GlobalData.AddQuoteData(symbolData.QuoteAsset);

                                symbol = new()
                                {
                                    Exchange = exchange,
                                    ExchangeId = exchange.Id,
                                    Name = symbolData.Name,
                                    Base = symbolData.BaseAsset,
                                    Quote = symbolData.QuoteAsset,
                                    QuoteData = quoteData,
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
                            symbol.QuantityMinimum = symbolData.LotSizeFilter?.MinQuantity ?? 0;
                            symbol.QuantityMaximum = symbolData.LotSizeFilter?.MaxQuantity ?? 0;
                            symbol.QuantityTickSize = symbolData.LotSizeFilter?.StepSize ?? 0;

                            //Minimale en maximale prijs voor een order (in base price)
                            symbol.PriceMinimum = symbolData.PriceFilter?.MinPrice ?? 0;
                            symbol.PriceMaximum = symbolData.PriceFilter?.MaxPrice ?? 0;
                            symbol.PriceTickSize = symbolData.PriceFilter?.TickSize ?? 0;

                            symbol.IsSpotTradingAllowed = true; // symbolData.IsSpotTradingAllowed;
                            symbol.IsMarginTradingAllowed = true; // symbolData.IsMarginTradingAllowed;

                            if (symbolData.Status == SymbolStatus.Trading | symbolData.Status == SymbolStatus.EndOfDay)
                                symbol.Status = 1;
                            else
                                symbol.Status = 0; //Zet de status door (PreTrading, PostTrading of Halt)

                            if (symbol.Id == 0)
                            {
                                database.Connection.Insert(symbol, transaction);
                                cache.Add(symbol);
                            }
                            else
                                database.Connection.Update(symbol, transaction);

                            activeSymbols.Add(symbol.Name, symbol);

                        }

                        // Deactiveer de munten die niet meer voorkomen
                        foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
                        {
                            if (symbol.Status == 1 && !symbol.IsBarometerSymbol() && !activeSymbols.ContainsKey(symbol.Name))
                            {
                                if (symbol.Status != 0)
                                {
                                    symbol.Status = 0;
                                    database.Connection.Update(symbol, transaction);

                                    reportSymbols.Add(symbol.Name);
                                }
                            }
                        }
                        if (reportSymbols.Count != 0)
                        {
                            var symbols = string.Join(',', [.. reportSymbols]);
                            GlobalData.AddTextToLogTab($"{reportSymbols.Count} symbols deactivated {symbols}");
                        }


                        transaction.Commit();


                        // Bewaren voor debug werkzaamheden
                        {
                            string filename = GlobalData.GetBaseDir();
                            filename += $@"\{ExchangeBase.ExchangeOptions.ExchangeName}\";
                            Directory.CreateDirectory(filename);
                            filename += "symbols.json";

                            string text = JsonSerializer.Serialize(exchangeInfo, JsonTools.JsonSerializerIndented);
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
                        ScannerLog.Logger.Error(error, "");
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
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
            }

        }
    }
}