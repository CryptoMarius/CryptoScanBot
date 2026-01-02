using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using Mexc.Net.Clients;
using Mexc.Net.Enums;

namespace CryptoScanner.Core.Exchange.Mexc.Spot;

public class Symbol() : SymbolBase(), ISymbol
{
    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new MexcRestClient(options => { options.OutputOriginalData = true; });
                client.ClientOptions.OutputOriginalData = true;
                var api = client.SpotApi;
                using CryptoDatabase database = new();
                database.Open();


                // tickers for volumes... (need volume because of filtered kline and price tickers)
                // https://api.bybit.com/v5/market/instruments-info?category=spot
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var tickerInfo = await api.ExchangeData.GetTickersAsync() ?? throw new ExchangeException("No ticker data received");
                if (!tickerInfo.Success)
                    GlobalData.AddTextToLogTab($"error getting symbol ticker {tickerInfo.Error}");
                if (tickerInfo == null)
                    throw new ExchangeException("No ticker data received");
                SaveExchangeInfo(tickerInfo.OriginalData, "tickers.json");

                // index volume
                SortedList<string, decimal> volumeTicker = [];
                if (tickerInfo.Data != null && tickerInfo.Data != null)
                {
                    foreach (var tickerData in tickerInfo.Data)
                    {
                        if (tickerData.QuoteVolume.HasValue)
                        {
                            string symbolName = tickerData.Symbol.Replace("-", "");
                            volumeTicker.Add(symbolName, tickerData.QuoteVolume.Value);
                        }
                    }
                }


                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var symbolInfo = await api.ExchangeData.GetExchangeInfoAsync() ?? throw new ExchangeException("No symbol data received");
                if (!symbolInfo.Success)
                    GlobalData.AddTextToLogTab($"error getting exchangeinfo {symbolInfo.Error}");
                if (symbolInfo == null)
                    throw new ExchangeException("No exchange data received");
                SaveExchangeInfo(symbolInfo.OriginalData, "symbols.json");



                // Om achteraf de niet aangeboden munten te deactiveren
                SortedList<string, CryptoSymbol> activeSymbols = [];
                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = [];
                    try
                    {
                        foreach (var symbolData in symbolInfo.Data.Symbols)
                        {
                            SymbolInfo info = ParseSymbol(symbolData.Name, symbolData.BaseAsset, symbolData.QuoteAsset);
                            if (IsSymbolAccepted(exchange, info, api, TradingMode.Spot, out CryptoSymbol? symbol))
                            {
                                //Het is erg belangrijk om de delisted munten zo snel mogelijk te detecteren.
                                //(ik heb wat slechte ervaringen met de AltradyStandard bot die op paniek pieken handelt)

                                // https://api.bybit.com/v5/market/instruments-info?category=spot
                                /*
                                {
                                "ExchangeSymbol": "OGNUSDT",
                                "BaseAssetName": "Origin",
                                "Status": "ENABLED",
                                "Base": "OGN",
                                "BaseAssetPrecision": 2,
                                "Quote": "USDT",
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

                                // min, max en tick (in base amount)
                                symbol!.QuantityTickSize = 1;
                                for (int x = symbolData.BaseAssetPrecision; x > 0; x--)
                                    symbol.QuantityTickSize /= 10;

                                //symbol.QuantityMinimum = symbolInfo.LotSizeFilter?.MinOrderQuantity ?? 0;
                                //symbol.QuantityMaximum = symbolInfo.LotSizeFilter?.MaxOrderQuantity ?? 0;

                                //symbol.QuoteValueMinimum = symbolInfo.LotSizeFilter?.MinOrderValue ?? 0;
                                symbol.QuoteValueMaximum = symbolData.MaxQuoteQuantity;


                                // De minimale en maximale prijs voor een order (in base price)
                                // In de definities is wel een minPrice en maxprice aanwezig, maar die is niet gevuld
                                // (dat heeft consequenties voro de werking van de Clamp die wel waarden verwacht)
                                //symbol.PriceMinimum = symbolInfo.LotSizeFilter.MinOrderValue;
                                //symbol.PriceMaximum = symbolInfo.LotSizeFilter.MaxOrderValue;

                                symbol.PriceTickSize = 1;
                                for (int x = symbolData.QuoteAssetPrecision; x > 0; x--)
                                    symbol.PriceTickSize /= 10;

                                // confusing, there is a Permissions flag as well
                                //symbol.IsSpotTradingAllowed = symbolData.IsSpotTradingAllowed;
                                //symbol.IsMarginTradingAllowed = symbolData.IsMarginTradingAllowed;

                                // volume from the tickers
                                if (volumeTicker.TryGetValue(symbol.Name, out decimal volume))
                                    symbol.Volume = volume;
                                else
                                    symbol.Volume = 0;

                                if (symbolData.IsSpotTradingAllowed && symbolData.Status == SymbolStatus.Enabled)
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

                if (tickerInfo.Success && tickerInfo.Success)
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