using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using Kraken.Net.Clients;
using Kraken.Net.Enums;

namespace CryptoScanner.Core.Exchange.Kraken.Spot;

public class Symbol() : SymbolBase(), ISymbol
{
    // Symbol names per ticker request. They travel in the url of a GET, so the amount is bound by
    // its length: 250 names is about 2.7 KB and the endpoint still accepts 500 without complaining.
    private const int TickerSymbolsPerRequest = 250;

    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new KrakenRestClient(options => { options.OutputOriginalData = true; });
                var api = client.SpotApi;

                using CryptoDatabase database = new();
                database.Open();


                // The symbols come first because the tickers below are requested per symbol name
                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var symbolInfo = await api.ExchangeData.GetSymbolsAsync(newAssetNameResponse: true) ?? throw new ExchangeException("No symbol data received");
                if (!symbolInfo.Success)
                    GlobalData.AddErrorToLogTab("error getting exchangeinfo " + symbolInfo.Error);
                if (symbolInfo.Data == null)
                    throw new ExchangeException("No exchange data received (2)");
                SaveExchangeInfo(symbolInfo, "symbols.json");


                // Tickers for the 24h volume, asked for per symbol on purpose. An unfiltered
                // GetTickersAsync() answers with Kraken's legacy pair id (XXBTZUSD, XETHZUSD,
                // XDGUSD) while the symbols above are read with the new asset names (BTC, ETH,
                // DOGE), and then 84 of the 1430 pairs find no volume at all: BTC/USD, ETH/USD,
                // USDT/USD, XRP/USD and the rest of the oldest markets on this exchange, all of
                // them dropped by the volume boundary afterwards. Kraken echoes back exactly the
                // names that were asked for, so requesting them per name keeps the answer on the
                // same names as ExchangeName - which is what the lookup below uses.
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");

                // Create dictionary for the volume
                SortedList<string, decimal> volumeTicker = [];
                List<object> tickerResults = [];
                foreach (string[] chunk in symbolInfo.Data.Keys.Chunk(TickerSymbolsPerRequest))
                {
                    LimitRate.WaitForFairWeight(1);
                    var tickerInfo = await api.ExchangeData.GetTickersAsync(chunk);
                    tickerResults.Add(tickerInfo);
                    if (!tickerInfo.Success || tickerInfo.Data == null)
                    {
                        GlobalData.AddErrorToLogTab($"error getting symbol ticker {tickerInfo.Error}");
                        continue;
                    }

                    // The key is the name Kraken echoed back, the safest thing to match on
                    foreach (var (tickerName, tickerData) in tickerInfo.Data)
                        volumeTicker[tickerName] = tickerData.Volume.Value24H * tickerData.VolumeWeightedAveragePrice.Value24H;  // Value24h
                }
                SaveExchangeInfo(tickerResults, "tickers.json");

                if (volumeTicker.Count == 0)
                    throw new ExchangeException("No ticker data received");


                // Track which symbols are still active, to deactivate the ones we no longer follow
                SortedList<string, CryptoSymbol> activeSymbols = [];

                // Symbols the tickers had no volume for. A handful is normal (a pair that has not
                // traded at all), a large number means the two calls are not on the same naming
                // again and everything silently falls below the volume boundary
                int withoutVolume = 0;


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = [];
                    try
                    {
                        foreach (var (key, symbolData) in symbolInfo.Data)
                        {
                            //"AlternateName": "AAVEEUR",
                            //"WebsocketName": "AAVE/EUR",
                            //"Base": "AAVE",
                            //"Quote": "ZEUR",
                            SymbolInfo info = ParseSymbol(key, symbolData.BaseAsset, symbolData.QuoteAsset);

                            // Safety net against two pairs parsing to the same scanner name: a
                            // duplicate must never take the entire fetch down, because the exception
                            // rolls back the whole transaction and then not a single symbol is
                            // stored and the exchange keeps its old symbol list. Kraken Spot keys its
                            // pairs on BASE/QUOTE so today all 1430 names are unique, but that is the
                            // naming of the exchange and not something this loop can lean on. Tested
                            // before IsSymbolAccepted() so the second pair cannot overwrite the
                            // ExchangeName of the first one either.
                            if (activeSymbols.ContainsKey(info.ScannerName))
                            {
                                GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} " +
                                    $"{key} skipped, {info.ScannerName} is already taken");
                                continue;
                            }

                            if (IsSymbolAccepted(exchange, info, api, TradingMode.Spot, out CryptoSymbol? symbol))
                            {
                                //TODO: ?????????????????????????????????????????????

                                //Temporarily copy everything (because of the new fields)
                                //The precision to use for prices
                                //symbol.BaseAssetPrecision = binanceSymbol.LotSizeFilter.BasePrecision.ToString().Length - 2;
                                //if (symbol.BaseAssetPrecision <= 0)
                                //    symbol.BaseAssetPrecision = 8;
                                //symbol.QuoteAssetPrecision = binanceSymbol.LotSizeFilter.QuotePrecision.ToString().Length - 2;
                                //if (symbol.QuoteAssetPrecision <= 0)
                                //    symbol.QuoteAssetPrecision = 8;
                                //symbol.MinNotional = symbolInfo.MinNotional; // ????

                                //Minimum and maximum amount for an order (in base amount)
                                //symbol.QuantityMinimum = symbolInfo.LotSizeFilter.MinOrderQuantity;
                                //symbol.QuantityMaximum = symbolInfo.LotSizeFilter.MaxOrderQuantity;
                                //symbol.QuantityTickSize = symbolInfo.LotSizeFilter.QuantityStep;

                                // Kraken has no quantity step of its own, it states lot_decimals - a
                                // NUMBER of decimals, so it needs the conversion (see
                                // SymbolBase.TickSizeFromDecimals). Without this 1374 of the 1580 pairs
                                // kept a quantity tick size of zero, which makes every amount
                                // calculation wrong the moment the trading is switched on.
                                symbol!.QuantityTickSize = TickSizeFromDecimals(symbolData.LotDecimals);

                                // The minimum and maximum price for an order (in base price)
                                // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                // (which has consequences for the Clamp, which does expect values)
                                //symbol.PriceMinimum = symbolInfo.PriceFilter.MinPrice;
                                //symbol.PriceMaximum = symbolInfo.PriceFilter.MaxPrice;

                                if (symbolData.MinValue.HasValue)
                                    symbol!.QuoteValueMinimum = (decimal)symbolData.MinValue;

                                symbol!.PriceTickSize = symbolData.TickSize ?? 0; // ? binanceSymbol.PriceFilter.TickSize;

                                //symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                //symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                // volume from the tickers (on the exchange name, see the ticker
                                // request above - the scanner name is a different naming altogether)
                                if (volumeTicker.TryGetValue(symbol.ExchangeName, out decimal volume))
                                    symbol.Volume = (double)volume;
                                else
                                {
                                    symbol.Volume = 0;
                                    withoutVolume++;
                                }

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
                                activeSymbols[symbol.Name] = symbol;
                            }
                        }

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
                            GlobalData.AddTextToLogTab($"{deactivated} symbols deactivated");
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
