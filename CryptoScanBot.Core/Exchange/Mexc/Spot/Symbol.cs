﻿using System.Text.Encodings.Web;
using System.Text.Json;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Dapper.Contrib.Extensions;

using Mexc.Net.Clients;
using Mexc.Net.Enums;

namespace CryptoScanBot.Core.Exchange.Mexc.Spot;

public class Symbol
{

    public static async Task ExecuteAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);

                using CryptoDatabase database = new();
                database.Open();

                using var client = new MexcRestClient();
                var exchangeInfo = await client.SpotApi.ExchangeData.GetExchangeInfoAsync() ?? throw new ExchangeException("Geen exchange data ontvangen (1)");
                if (!exchangeInfo.Success)
                    GlobalData.AddTextToLogTab("error getting exchangeinfo " + exchangeInfo.Error, true);
                if (exchangeInfo.Data == null)
                    throw new ExchangeException("Geen exchange data ontvangen (2)");


                // Om achteraf de niet aangeboden munten te deactiveren
                SortedList<string, CryptoSymbol> activeSymbols = [];


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = [];
                    try
                    {
                        //BybitSpotSymbol
                        //WebCallResult<BybitResponse<BybitSpotSymbol>> x;
                        foreach (var symbolData in exchangeInfo.Data.Symbols)
                        {
                            //if (coin != "")
                            {
                                //Het is erg belangrijk om de delisted munten zo snel mogelijk te detecteren.
                                //(ik heb wat slechte ervaringen met de Altrady bot die op paniek pieken handelt)

                                // https://api.bybit.com/v5/market/instruments-info?category=spot
                                /*
                                    {
                                    "Name": "HFTUSDT",
                                    "BaseAsset": "HFT",
                                    "QuoteAsset": "USDT",
                                    "Status": 1,
                                    "MarginTrading": 2,
                                    "Innovation": false,
                                    "LotSizeFilter": {
                                        "BasePrecision": 0.01,
                                        "QuotePrecision": 0.000001,
                                        "MinOrderQuantity": 2.5,
                                        "MaxOrderQuantity": 738825.267824,
                                        "MinOrderValue": 1,
                                        "MaxOrderValue": 200000
                                    },
                                    "PriceFilter": {
                                        "TickSize": 0.0001
                                    },
                                    "PricePercentageFilter": {
                                        "LimitPricePercentageLimit": 0.03,
                                        "MarketPricePercentageLimit": 0.03
                                    }
                                    },
                               
                                enzovoort..
                                */
                                //Eventueel symbol toevoegen
                                if (!exchange.SymbolListName.TryGetValue(symbolData.Name, out CryptoSymbol? symbol))
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


                                // min, max en tick (in base amount)
                                //symbol.QuantityTickSize = symbolData.BaseAssetPrecision / 100;
                                //symbol.QuantityMinimum = symbolData.LotSizeFilter?.MinOrderQuantity ?? 0;
                                //symbol.QuantityMaximum = symbolData.LotSizeFilter?.MaxOrderQuantity ?? 0;

                                //symbol.QuoteValueMinimum = symbolData.LotSizeFilter?.MinOrderValue ?? 0;
                                //symbol.QuoteValueMaximum = symbolData.LotSizeFilter?.MaxOrderValue ?? 0;


                                // De minimale en maximale prijs voor een order (in base price)
                                // In de definities is wel een minPrice en maxprice aanwezig, maar die is niet gevuld
                                // (dat heeft consequenties voro de werking van de Clamp die wel waarden verwacht)
                                //symbol.PriceMinimum = symbolData.LotSizeFilter.MinOrderValue;
                                //symbol.PriceMaximum = symbolData.LotSizeFilter.MaxOrderValue;

                                //symbol.PriceTickSize = symbolData.PriceFilter?.TickSize ?? 0;

                                symbol.IsSpotTradingAllowed = true; // symbolData.IsSpotTradingAllowed;
                                symbol.IsMarginTradingAllowed = false; //symbolData.IsMarginTradingAllowed;

                                if (symbolData.Status == "ENABLED")
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

                            string text = JsonSerializer.Serialize(exchangeInfo, GlobalData.JsonSerializerIndented);
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