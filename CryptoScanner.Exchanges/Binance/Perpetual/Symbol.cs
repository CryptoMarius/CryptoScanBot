using Binance.Net.Clients;
using Binance.Net.Enums;

using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Exchange.Binance.Perpetual;

public class Symbol() : SymbolBase(), ISymbol
{
    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new BinanceRestClient(options => { options.OutputOriginalData = true; });
                var api = client.UsdFuturesApi;
                using CryptoDatabase database = new();
                database.Open();


                // Tickers for the 24h volume
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var tickerInfo = await api.ExchangeData.GetTickersAsync() ?? throw new ExchangeException("No ticker data received");
                if (!tickerInfo.Success)
                    GlobalData.AddErrorToLogTab($"error getting symbol ticker {tickerInfo.Error}");
                if (tickerInfo == null)
                    throw new ExchangeException("No ticker data received");
                SaveExchangeInfo(tickerInfo.OriginalData, "tickers.json");

                // Create dictionary for the volume
                SortedList<string, decimal> volumeTicker = [];
                if (tickerInfo.Data != null && tickerInfo.Data != null)
                {
                    foreach (var tickerData in tickerInfo.Data)
                        volumeTicker.TryAdd(tickerData.Symbol, tickerData.QuoteVolume);
                }

                // Without the tickers every symbol would end up with a volume of 0, drop below the
                // minimum volume and have its candles and subscriptions released. Stop instead, the
                // next refresh cycle will try again.
                if (volumeTicker.Count == 0)
                    throw new ExchangeException("No ticker data received");


                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var symbolInfo = await api.ExchangeData.GetExchangeInfoAsync() ?? throw new ExchangeException("No symbol data received");
                if (!symbolInfo.Success)
                    GlobalData.AddErrorToLogTab("error getting exchangeinfo " + symbolInfo.Error);
                if (symbolInfo.Data == null)
                    throw new ExchangeException("Geen exchange data ontvangen (2)");
                SaveExchangeInfo(symbolInfo.OriginalData, "symbols.json");



                // Track which symbols are still active, to report and deactivate the ones we no longer follow
                List<string> reportSymbols = [];
                SortedList<string, CryptoSymbol> activeSymbols = [];
                // Scanner names of the instruments we skip below. Intersected with the accepted names
                // after the loop, that gives the symbols whose name covers more than one instrument.
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
                        foreach (var symbolData in symbolInfo.Data.Symbols)
                        {
                            // These two filters must run BEFORE IsSymbolAccepted. A delivery contract such as
                            // BTCUSDT_261225 carries the same base and quote as its perpetual, so ParseSymbol
                            // gives it the same ScannerName ("BTCUSDT") and IsSymbolAccepted hands back the
                            // EXISTING perpetual symbol — and overwrites its ExchangeName with the delivery
                            // contract's name. The continue below then skips the database update, so the stored
                            // row keeps saying BTCUSDT while every candle fetch for the rest of the session asks
                            // Binance for BTCUSDT_261225 (which trades ~1.7% above the perpetual and is far less
                            // liquid). Only BTCUSDT and ETHUSDT have such a sibling, which is exactly why their
                            // candles were wrong while the database looked perfectly normal.
                            //
                            // PerpetualTradFi passes as well. Binance answers two questions in this one
                            // field: whether the contract expires, and what it tracks. TRADIFI_PERPETUAL
                            // says "never expires, and it follows a share, a commodity or an index" -
                            // XAUUSDT is gold, AAPLUSDT is Apple. Mechanically they are the same product
                            // as the crypto perpetuals: no expiry, a funding rate, USDT as both margin and
                            // payout, and candles that keep coming outside the hours of the market
                            // underneath (thinner, but never absent). Every other exchange that carries
                            // them - Okx, Bybit, Kucoin, BloFin, Mexc - hands them over as an ordinary
                            // perpetual, which is why the scanner has had them there all along and only
                            // Binance stood at zero. 175 of them on 27-08-2026, and not one of their
                            // scanner names collides with a crypto perpetual or with another TradFi one.
                            if (symbolData.ContractType != ContractType.Perpetual &&
                                symbolData.ContractType != ContractType.PerpetualTradFi)
                            {
#if DEBUG
                                //GlobalData.AddTextToLogTab($"{info.ExchangeName} contracttype != {ContractType.Perpetual}");
#endif
                                rejectedSymbols.Add(symbolData.BaseAsset.ToUpper() + symbolData.QuoteAsset.ToUpper());
                                continue;
                            }

                            if (symbolData.UnderlyingSubType.Contains("Chinese"))
                            {
#if DEBUG
                                //GlobalData.AddTextToLogTab($"{info.ExchangeName} UnderlyingSubType != {symbolData.UnderlyingSubType}");
#endif
                                rejectedSymbols.Add(symbolData.BaseAsset.ToUpper() + symbolData.QuoteAsset.ToUpper());
                                continue;
                            }

                            SymbolInfo info = ParseSymbol(symbolData.Name, symbolData.BaseAsset, symbolData.QuoteAsset, ProductOfExchange(exchange));
                            if (IsSymbolAccepted(exchange, info, api, TradingMode.PerpetualLinear, out CryptoSymbol? symbol))
                            {
                                //Temporarily copy everything (because of the new fields)
                                //The precision to use for prices
                                //symbol.BaseAssetPrecision = binanceSymbol.BaseAssetPrecision;
                                //symbol.QuoteAssetPrecision = binanceSymbol.QuoteAssetPrecision;
                                // Tijdelijke fix voor Binance.net (kan waarschijnlijk weer weg)
                                //if (binanceSymbol.MinNotionalFilter != null)
                                //    symbol.MinNotional = binanceSymbol.MinNotionalFilter.MinNotional;
                                //else
                                //    symbol.MinNotional = 0;

                                //Minimum and maximum amount for an order (in base amount)
                                symbol!.QuantityMinimum = symbolData.LotSizeFilter?.MinQuantity ?? 0;
                                symbol.QuantityMaximum = symbolData.LotSizeFilter?.MaxQuantity ?? 0;
                                symbol.QuantityTickSize = symbolData.LotSizeFilter?.StepSize ?? 0;

                                //Minimum and maximum price for an order (in base price)
                                symbol.PriceMinimum = symbolData.PriceFilter?.MinPrice ?? 0;
                                symbol.PriceMaximum = symbolData.PriceFilter?.MaxPrice ?? 0;
                                symbol.PriceTickSize = symbolData.PriceFilter?.TickSize ?? 0;

                                //symbol.IsSpotTradingAllowed = true; // symbolData.IsSpotTradingAllowed;
                                //symbol.IsMarginTradingAllowed = true; // symbolData.IsMarginTradingAllowed;

                                // volume from the tickers
                                if (volumeTicker.TryGetValue(symbol.Name, out decimal volume))
                                    symbol.Volume = (double)volume;
                                else
                                {
                                    symbol.Volume = 0;
                                    withoutVolume++;
                                }

                                if (symbolData.Status == SymbolStatus.Trading | symbolData.Status == SymbolStatus.EndOfDay)
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

                        // Which scanner names cover more than one instrument (BTCUSDT and ETHUSDT here)
                        RegisterAmbiguousSymbolNames(exchange, rejectedSymbols, activeSymbols.Keys);

                        // Deactivate the symbols who have disappeared
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