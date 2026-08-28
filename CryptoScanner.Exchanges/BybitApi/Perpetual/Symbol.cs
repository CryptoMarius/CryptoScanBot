using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Exchange.BybitApi.Perpetual;

public class Symbol() : SymbolBase(), ISymbol
{

    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new BybitRestClient(options => { options.OutputOriginalData = true; });
                var api = client.V5Api;
                using CryptoDatabase database = new();
                database.Open();


                // Tickers for the 24h volume
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var tickerInfo = await api.ExchangeData.GetLinearInverseTickersAsync(Category.Linear) ??
                    throw new ExchangeException("No ticker data received");
                if (!tickerInfo.Success)
                    GlobalData.AddErrorToLogTab($"error getting symbol ticker {tickerInfo.Error}");
                if (tickerInfo == null)
                    throw new ExchangeException("No ticker data received");
                SaveExchangeInfo(tickerInfo.OriginalData, "tickers.json");

                // Create dictionary for the volume
                SortedList<string, decimal> volumeTicker = [];
                if (tickerInfo.Data != null && tickerInfo.Data != null)
                {
                    foreach (var tickerData in tickerInfo.Data.List)
                        volumeTicker.TryAdd(tickerData.Symbol, tickerData.Turnover24h);
                }


                // Without the tickers every symbol would end up with a volume of 0, drop below the
                // minimum volume and have its candles and subscriptions released. Stop instead, the
                // next refresh cycle will try again.
                if (volumeTicker.Count == 0)
                    throw new ExchangeException("No ticker data received");

                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");

                int page = 1;
                string? pageCursor = null;
                List<BybitLinearInverseSymbol> symbols = [];
                while (true)
                {
                    LimitRate.WaitForFairWeight(1);
                    var symbolInfo = await api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, cursor: pageCursor) ??
                        throw new ExchangeException("No symbol data received");
                    if (!symbolInfo.Success)
                        GlobalData.AddErrorToLogTab("error getting exchangeinfo " + symbolInfo.Error);
                    if (symbolInfo.Data == null)
                        throw new ExchangeException("no exchange data received (2)");
                    SaveExchangeInfo(symbolInfo.OriginalData, $"symbols{page++}.json");

                    symbols.AddRange(symbolInfo.Data.List);

                    pageCursor = symbolInfo.Data.NextPageCursor;
                    if (symbolInfo.Data.List.Length == 0 || string.IsNullOrEmpty(symbolInfo.Data.NextPageCursor))
                        break;
                }


                // Track which symbols are still active, to deactivate the ones we no longer follow
                SortedList<string, CryptoSymbol> activeSymbols = [];
                // Scanner names of the instruments we skip below, see RegisterAmbiguousSymbolNames
                List<string> rejectedSymbols = [];
                // Symbols the tickers had no volume for. A handful is normal (a pair that has not
                // traded at all), a large number means the two calls are not on the same naming
                // again and everything silently falls below the volume boundary
                int withoutVolume = 0;

                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = [];
                    try
                    {
                        //BybitSpotSymbol
                        //HttpResult<BybitResponse<BybitSpotSymbol>> x;
                        foreach (var symbolData in symbols)
                        {
                            // Filter BEFORE IsSymbolAccepted, same reason as in Binance Perpetual: a dated
                            // contract (BTCUSDT-25DEC26) shares base and quote with its perpetual, so it gets
                            // the same ScannerName and IsSymbolAccepted overwrites the perpetual's
                            // ExchangeName with the dated one. Bybit currently has eight such collisions
                            // (BTC, ETH, DOGE, HYPE, MNT and three more).
                            if (symbolData.ContractType != ContractTypeV5.LinearPerpetual)
                            {
#if DEBUG
                                //GlobalData.AddTextToLogTab($"{info.ExchangeName} contracttype != {ContractTypeV5.LinearPerpetual}");
#endif
                                rejectedSymbols.Add(symbolData.BaseAsset.ToUpper() + symbolData.QuoteAsset.ToUpper());
                                continue;
                            }

                            SymbolInfo info = ParseSymbol(symbolData.Name, symbolData.BaseAsset, symbolData.QuoteAsset, ProductOfExchange(exchange));
                            if (IsSymbolAccepted(exchange, info, api, TradingMode.PerpetualLinear, out CryptoSymbol? symbol))
                            {
                                //Het is erg belangrijk om de delisted munten zo snel mogelijk te detecteren.
                                //(ik heb wat slechte ervaringen met de Altrady bot die op paniek pieken handelt)

                                // https://api.bybit.com/v5/market/instruments-info?category=spot
                                /*
                                  "Data": {
                                    "List": [
                                      {
                                        "ExchangeSymbol": "BTCUSDT",
                                        "Base": "BTC",
                                        "Quote": "USDT",
                                        "Status": 1,
                                        "MarginTading": 0,
                                        "Innovation": false,
                                        "LotSizeFilter": {
                                          "BasePrecision": 0.000001,
                                          "QuotePrecision": 0.00000001,
                                          "MinOrderQuantity": 0.000048,
                                          "MaxOrderQuantity": 71.73956243,
                                          "MinOrderValue": 1,
                                          "MaxOrderValue": 2000000
                                        },
                                        "PriceFilter": {
                                          "TickSize": 0.01
                                        }
                                      },
                                enzovoort..
                                */



                                symbol!.FundingInterval = symbolData.FundingInterval;

                                //Temporarily copy everything (because of the new fields)
                                //The precision to use for prices
                                //symbol.BaseAssetPrecision = binanceSymbol.LotSizeFilter.BasePrecision.ToString().Length - 2;
                                //if (symbol.BaseAssetPrecision <= 0)
                                //    symbol.BaseAssetPrecision = 8;
                                //symbol.QuoteAssetPrecision = binanceSymbol.LotSizeFilter.QuotePrecision.ToString().Length - 2;
                                //if (symbol.QuoteAssetPrecision <= 0)
                                //    symbol.QuoteAssetPrecision = 8;
                                //symbol.MinNotional = binanceSymbol.MinNotional; // ????

                                //Minimum and maximum amount for an order (in base amount)
                                symbol.QuantityMinimum = symbolData.LotSizeFilter?.MinOrderQuantity ?? 0;
                                symbol.QuantityMaximum = symbolData.LotSizeFilter?.MaxOrderQuantity ?? 0;
                                symbol.QuantityTickSize = symbolData.LotSizeFilter?.QuantityStep ?? 0;

                                // The minimum and maximum price for an order (in base price)
                                // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                // (which has consequences for the Clamp, which does expect values)
                                symbol.PriceMinimum = symbolData.PriceFilter?.MinPrice ?? 0;
                                symbol.PriceMaximum = symbolData.PriceFilter?.MaxPrice ?? 0;
                                symbol.PriceTickSize = symbolData.PriceFilter?.TickSize ?? 0;

                                //symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                //symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                // volume from the tickers, looked up on the PAIR: the ticker list is keyed on the
                                // exchange's own spelling and knows nothing of the product behind the dot. On the
                                // scanner name it matched nothing at all and every symbol silently ended up at zero.
                                if (volumeTicker.TryGetValue(CryptoProduct.PairOf(symbol.Name), out decimal volume))
                                    symbol.Volume = (double)volume;
                                else
                                {
                                    symbol.Volume = 0;
                                    withoutVolume++;
                                }

                                if (symbolData.Status == SymbolStatus.Trading)
                                    symbol.Status = 1;
                                else
                                    symbol.Status = 0; //Pass the status on (PreTrading, PostTrading or Halt)

                                if (symbol.Id == 0)
                                {
                                    database.Connection.Insert(symbol, transaction);
                                    cache.Add(symbol);
                                }
                                else
                                    database.Connection.Update(symbol, transaction);
                                activeSymbols[symbol.Name] = symbol;
                            }
                        }

                        // Which scanner names cover more than one instrument (BTC, ETH, DOGE, HYPE, MNT, ...)
                        RegisterAmbiguousSymbolNames(exchange, rejectedSymbols, activeSymbols.Keys);

                        // Deactivate the symbols who have disappeared
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

                        if (withoutVolume > 0)
                            GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} " +
                                $"{withoutVolume} symbols without a 24 hour volume (of {activeSymbols.Count})");

                        transaction.Commit();




                        // Add the new symbols to the list
                        // (because the symbols only get an id during the BulkInsert)
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