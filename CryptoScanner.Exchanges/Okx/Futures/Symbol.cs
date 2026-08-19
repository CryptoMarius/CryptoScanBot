using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using OKX.Net.Clients;
using OKX.Net.Enums;

namespace CryptoScanner.Core.Exchange.Okx.Futures;

public class Symbol() : SymbolBase(), ISymbol
{

    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new OKXRestClient(options => { options.OutputOriginalData = true; });
                var api = client.UnifiedApi;

                using CryptoDatabase database = new();
                database.Open();


                // tickers for volumes... (need volume because of filtered kline and price tickers)
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var tickerInfo = await api.ExchangeData.GetTickersAsync(InstrumentType.Swap) ?? throw new ExchangeException("No ticker data received");
                if (!tickerInfo.Success)
                    GlobalData.AddErrorToLogTab($"error getting symbol ticker info {tickerInfo.Error}");
                if (tickerInfo == null)
                    throw new ExchangeException("No ticker data received");
                SaveExchangeInfo(tickerInfo.OriginalData, "tickers.json");

                // index volume
                // Indexed on the instrument id ("BCH-USDT-SWAP") because a swap symbol name cannot be
                // reconstructed by simply removing the hyphens. For derivatives OKX reports volCcy24h
                // in the base asset, so multiply by the last price to get the volume in the quote asset.
                SortedList<string, decimal> volumeTicker = [];
                if (tickerInfo.Data != null && tickerInfo.Data != null)
                {
                    foreach (var tickerData in tickerInfo.Data)
                    {
                        volumeTicker.TryAdd(tickerData.Symbol, tickerData.QuoteVolume * (tickerData.LastPrice ?? 0));
                    }
                }



                // Without the tickers every symbol would end up with a volume of 0, drop below the
                // minimum volume and have its candles and subscriptions released. Stop instead, the
                // next refresh cycle will try again.
                if (volumeTicker.Count == 0)
                    throw new ExchangeException("No ticker data received");

                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var symbolInfo = await api.ExchangeData.GetSymbolsAsync(InstrumentType.Swap) ?? throw new ExchangeException("No symbol data received");
                if (!symbolInfo.Success)
                    GlobalData.AddErrorToLogTab("error getting symbol information " + symbolInfo.Error);
                if (symbolInfo.Data == null)
                    throw new ExchangeException("No exchange data retrieved (2)");
                SaveExchangeInfo(symbolInfo.OriginalData, "symbols.json");


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
                        foreach (var symbolData in symbolInfo.Data)
                        {
                            //PerpetualLinear, but library adds -SWAP at the end (is that correct?)

                            // Only take instruments that are actually tradable (skip PreTrading, PostTrading or Halt).
                            // Instruments in the "preopen" state have almost none of their fields filled in (base and
                            // quote asset are empty), which would give them an empty scanner name. Symbols that were
                            // live before are deactivated further down because they are missing from activeSymbols.
                            if (symbolData.State != InstrumentState.Live)
                                continue;

                            // A swap does not report a base and quote asset, only the instrument family
                            // ("BCH-USDT") and the instrument id ("BCH-USDT-SWAP"). Derive base and quote
                            // from the family so the scanner name stays identical to the spot one.
                            string[] family = symbolData.InstrumentFamily.Split('-');
                            if (family.Length != 2)
                                continue;

                            // Skip the inverse (coin margined) contracts, those settle in the base asset
                            // instead of the quote asset and their prices/volumes are not comparable.
                            if (!string.Equals(symbolData.SettlementAsset, family[1], StringComparison.OrdinalIgnoreCase))
                                continue;

                            SymbolInfo info = ParseSymbol(symbolData.Symbol, family[0], family[1]);
                            if (IsSymbolAccepted(exchange, info, api, TradingMode.PerpetualLinear, out CryptoSymbol? symbol))
                            {
                                //Temporarily copy everything (because of the new fields)
                                //The precision to use for prices
                                //symbol.BaseAssetPrecision = binanceSymbol.LotSizeFilter.BasePrecision.ToString().Length - 2;
                                //if (symbol.BaseAssetPrecision <= 0)
                                //    symbol.BaseAssetPrecision = 8;
                                //symbol.QuoteAssetPrecision = binanceSymbol.LotSizeFilter.QuotePrecision.ToString().Length - 2;
                                //if (symbol.QuoteAssetPrecision <= 0)
                                //    symbol.QuoteAssetPrecision = 8;
                                //symbol.MinNotional = binanceSymbol.MinNotional; // ????

                                // min, max en tick (in base amount)
                                // A swap order quantity is expressed in contracts, ctVal tells how much of the
                                // base asset one contract represents. Multiply them to get the step in base amount.
                                if (symbolData.LotSize.HasValue && symbolData.ContractValue.HasValue)
                                    symbol!.QuantityTickSize = symbolData.LotSize.Value * symbolData.ContractValue.Value;
                                else if (symbolData.LotSize.HasValue)
                                    symbol!.QuantityTickSize = symbolData.LotSize.Value;
                                //symbol.QuantityMinimum = symbolInfo.LotSizeFilter?.MinOrderQuantity ?? 0;
                                //symbol.QuantityMaximum = symbolInfo.LotSizeFilter?.MaxOrderQuantity ?? 0;

                                //symbol.QuoteValueMinimum = symbolInfo.LotSizeFilter?.MinOrderValue ?? 0;
                                //symbol.QuoteValueMaximum = symbolInfo.LotSizeFilter?.MaxOrderValue ?? 0;


                                // The minimum and maximum price for an order (in base price)
                                // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                // (which has consequences for the Clamp, which does expect values)
                                //symbol.PriceMinimum = symbolInfo.LotSizeFilter.MinOrderValue;
                                //symbol.PriceMaximum = symbolInfo.LotSizeFilter.MaxOrderValue;

                                if (symbolData.TickSize.HasValue)
                                    symbol!.PriceTickSize = symbolData.TickSize.Value;

                                //symbol!.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                //symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                // volume from the tickers (indexed on the instrument id, not the scanner name)
                                if (volumeTicker.TryGetValue(symbol!.ExchangeName, out decimal volume))
                                    symbol.Volume = (double)volume;
                                else
                                {
                                    symbol.Volume = 0;
                                    withoutVolume++;
                                }

                                // Only live instruments reach this point
                                symbol.Status = 1;

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