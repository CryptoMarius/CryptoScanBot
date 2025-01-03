﻿using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using Kraken.Net.Clients;
using Kraken.Net.Enums;

using System.Text.Json;

namespace CryptoScanBot.Core.Exchange.Kraken.Spot;

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

                using var client = new KrakenRestClient();
                var exchangeInfo = await client.SpotApi.ExchangeData.GetSymbolsAsync() ?? throw new ExchangeException("Geen exchange data ontvangen (1)");
                if (!exchangeInfo.Success)
                    GlobalData.AddTextToLogTab("error getting exchangeinfo " + exchangeInfo.Error);
                if (exchangeInfo.Data == null)
                    throw new ExchangeException("Geen exchange data ontvangen (2)");


                // Om achteraf de niet aangeboden munten te deactiveren
                SortedList<string, CryptoSymbol> activeSymbols = [];


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = [];
                    try
                    {
                        foreach (var symbolData in exchangeInfo.Data.Values)
                        {
                            //if (coin != "")
                            {
                                //"AlternateName": "AAVEEUR",
                                //"WebsocketName": "AAVE/EUR",
                                //"BaseAsset": "AAVE",
                                //"QuoteAsset": "ZEUR", ????????


                                string name = symbolData.WebsocketName; // AlternateName; // symbolData.BaseAsset + symbolData.QuoteAsset;
                                string[] nameParts = name.Split('/');
                                name = nameParts[0] + nameParts[1];


                                /*
                                    enzovoort..
                                */
                                //if (symbolData.Name != symbolData.BaseAsset + symbolData.QuoteAsset)
                                //{
                                //    GlobalData.AddTextToLogTab($"Ignoring symbol {symbolData.Name} {symbolData.BaseAsset} {symbolData.QuoteAsset} weird name?");
                                //    continue;
                                //}
                                //Eventueel symbol toevoegen
                                if (!exchange.SymbolListName.TryGetValue(name, out CryptoSymbol? symbol))
                                {
                                    var quoteData = GlobalData.AddQuoteData(symbolData.QuoteAsset);

                                    symbol = new()
                                    {
                                        Exchange = exchange,
                                        ExchangeId = exchange.Id,
                                        Name = name,
                                        Base = nameParts[0],  // symbolData.BaseAsset,
                                        Quote = nameParts[1], //symbolData.QuoteAsset,
                                        QuoteData = quoteData,
                                        Status = 1,
                                    };
                                }

                                //TODO: ?????????????????????????????????????????????

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
                                // (dat heeft consequenties voor de werking van de Clamp die wel waarden verwacht)
                                //symbol.PriceMinimum = symbolData.PriceFilter.MinPrice;
                                //symbol.PriceMaximum = symbolData.PriceFilter.MaxPrice;

                                if (symbolData.MinValue.HasValue)
                                    symbol.QuoteValueMinimum = (decimal)symbolData.MinValue;

                                symbol.PriceTickSize = symbolData.TickSize ?? 0; // ? binanceSymbol.PriceFilter.TickSize;

                                symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                if (symbolData.Status == SymbolStatus.Online)
                                    symbol.Status = 1;
                                else
                                    symbol.Status = 0;

                                if (symbol.Id == 0)
                                {
                                    database.Connection.Insert(symbol, transaction);
                                    cache.Add(symbol);
                                }
                                else
                                    database.Connection.Update(symbol, transaction);
                                activeSymbols.Add(symbol.Name, symbol);
                            }
                        }

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
