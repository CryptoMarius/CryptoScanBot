using System.Text.Json;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using Mexc.Net.Clients;
using Mexc.Net.Enums;

namespace CryptoScanBot.Core.Exchange.Mexc.Spot;

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

                using var client = new MexcRestClient();
                client.ClientOptions.OutputOriginalData = true;

                // exchangeInfo for symbols...
                var exchangeData = await client.SpotApi.ExchangeData.GetExchangeInfoAsync();
                if (!exchangeData.Success)
                    GlobalData.AddTextToLogTab($"error getting exchangeinfo {exchangeData.Error}");
                if (exchangeData == null)
                    throw new ExchangeException("No exchange data received");

                // Save for debug purposes
                {
                    string filename = $@"{GlobalData.GetBaseDir()}\{exchange.Name}\";
                    Directory.CreateDirectory(filename);
                    filename += "symbols.json";

                    string text = JsonSerializer.Serialize(exchangeData, JsonTools.JsonSerializerIndented);
                    File.WriteAllText(filename, text);
                }


                // tickers for volumes... (need volume because of filtered kline and price tickers)
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                var tickerData = await client.SpotApi.ExchangeData.GetTickersAsync();
                if (!tickerData.Success)
                    GlobalData.AddTextToLogTab($"error getting symbol ticker {tickerData.Error}");
                if (tickerData == null)
                    throw new ExchangeException("No ticker data received");


                // Save for debug purposes
                {
                    string filename = $@"{GlobalData.GetBaseDir()}\{exchange.Name}\";
                    Directory.CreateDirectory(filename);
                    filename += "tickers.json";

                    string text = JsonSerializer.Serialize(tickerData, JsonTools.JsonSerializerIndented);
                    //var accountFile = new FileInfo(filename);
                    File.WriteAllText(filename, text);
                }

                // index volume
                SortedList<string, decimal> volumeTicker = [];
                if (tickerData.Data != null && tickerData.Data != null)
                {
                    foreach (var tickerInfo in tickerData.Data)
                    {
                        if (tickerInfo.QuoteVolume.HasValue)
                        {
                            string symbolName = tickerInfo.Symbol.Replace("-", "");
                            volumeTicker.Add(symbolName, tickerInfo.QuoteVolume.Value);
                        }
                    }
                }



                // Om achteraf de niet aangeboden munten te deactiveren
                SortedList<string, CryptoSymbol> activeSymbols = [];


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = [];
                    try
                    {
                        //BybitSpotSymbol
                        //WebCallResult<BybitResponse<BybitSpotSymbol>> x;
                        foreach (var symbolData in exchangeData.Data.Symbols)
                        {
                            //if (coin != "")
                            {
                                //Het is erg belangrijk om de delisted munten zo snel mogelijk te detecteren.
                                //(ik heb wat slechte ervaringen met de Altrady bot die op paniek pieken handelt)

                                // https://api.bybit.com/v5/market/instruments-info?category=spot
                                /*
                                {
                                "Name": "OGNUSDT",
                                "BaseAssetName": "Origin",
                                "Status": "ENABLED",
                                "BaseAsset": "OGN",
                                "BaseAssetPrecision": 2,
                                "QuoteAsset": "USDT",
                                "QuoteAssetPrecision": 4,
                                "QuoteAssetFeePrecision": 4,
                                "BaseAssetFeePrecision": 2,
                                "OrderTypes": [
                                    0,
                                    1,
                                    2
                                ],
                                "QuoteOrderQuantityMarketAllowed": false,
                                "IsSpotTradingAllowed": false,
                                "IsMarginTradingAllowed": false,
                                "QuoteQuantityPrecision": 5.0000000000000000000000000000,
                                "BaseQuantityPrecision": 0.01,
                                "Permissions": [
                                    "SPOT"
                                ],
                                "MaxQuoteQuantity": 2000000.0000000000000000000000,
                                "MakerFee": 0,
                                "TakerFee": 0,
                                "QuoteQuantityPrecisionMarket": 5.0000000000000000000000000000,
                                "MaxQuoteQuantityMarket": 100000.00000000000000000000000
                                },
                               
                                */

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


                                // min, max en tick (in base amount)
                                symbol.QuantityTickSize = 1;
                                for (int x = symbolData.BaseAssetPrecision; x > 0; x--)
                                    symbol.QuantityTickSize /= 10;

                                //symbol.QuantityMinimum = symbolData.LotSizeFilter?.MinOrderQuantity ?? 0;
                                //symbol.QuantityMaximum = symbolData.LotSizeFilter?.MaxOrderQuantity ?? 0;

                                //symbol.QuoteValueMinimum = symbolData.LotSizeFilter?.MinOrderValue ?? 0;
                                symbol.QuoteValueMaximum = symbolData.MaxQuoteQuantity;


                                // De minimale en maximale prijs voor een order (in base price)
                                // In de definities is wel een minPrice en maxprice aanwezig, maar die is niet gevuld
                                // (dat heeft consequenties voro de werking van de Clamp die wel waarden verwacht)
                                //symbol.PriceMinimum = symbolData.LotSizeFilter.MinOrderValue;
                                //symbol.PriceMaximum = symbolData.LotSizeFilter.MaxOrderValue;

                                symbol.PriceTickSize = 1;
                                for (int x = symbolData.QuoteAssetPrecision; x > 0; x--)
                                    symbol.PriceTickSize /= 10;

                                symbol.IsSpotTradingAllowed = true; // symbolData.IsSpotTradingAllowed; // confusing, there is a Permissions flag as well (read doumentation once..)
                                symbol.IsMarginTradingAllowed = symbolData.IsMarginTradingAllowed;

                                if (symbolData.Status == SymbolStatus.Enabled)
                                    symbol.Status = 1;
                                else
                                    symbol.Status = 0; //Zet de status door (PreTrading, PostTrading of Halt)


                                // volume from the tickers
                                if (volumeTicker.TryGetValue(symbol.Name, out decimal volume))
                                    symbol.Volume = volume;

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

                if (tickerData.Success && tickerData.Success)
                {
                    exchange.LastTimeFetched = DateTime.UtcNow;
                    database.Connection.Update(exchange);
                }

            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
            }

        }
    }
}