using BitMart.Net.Clients;
using BitMart.Net.Enums;

using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Exchange.BitMart.Futures;

public class Symbol() : SymbolBase(), ISymbol
{

    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new BitMartRestClient(options => { options.OutputOriginalData = true; });
                var api = client.UsdFuturesApi;
                using CryptoDatabase database = new();
                database.Open();


                //// tickers for volumes... (need volume because of filtered kline and price tickers)
                //GlobalData.AddTextToLogTab($"Reading symbol and ticker information from {ExchangeOptions.ExchangeSymbol}");
                ////LimitRate.WaitForFairWeight(1);
                //var tickerInfo = await api.ExchangeData.GetTickersAsync();
                //if (!tickerInfo.Success)
                //    GlobalData.AddTextToLogTab($"error getting symbol ticker info {tickerInfo.Error}");
                //if (tickerInfo == null)
                //    throw new ExchangeException("No ticker data received");
                //SaveExchangeInfo(tickerInfo.OriginalData, "tickers.json");

                //// index volume
                //SortedList<string, decimal> volumeTicker = [];
                //if (tickerInfo.Data != null && tickerInfo.Data != null)
                //{
                //    foreach (var tickerData in tickerInfo.Data.Tickers)
                //    {
                //        if (tickerData.ScannerSymbol != null)
                //        {
                //            string symbolName = tickerData.ScannerSymbol.Replace("/", "");
                //            volumeTicker.Add(symbolName, tickerData.NotionalVolume); // QuoteVolume?
                //        }
                //    }
                //}



                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                //LimitRate.WaitForFairWeight(1);
                var symbolInfo = await api.ExchangeData.GetContractsAsync() ?? throw new ExchangeException("No exchange data retrieved (1)");
                if (!symbolInfo.Success)
                    GlobalData.AddTextToLogTab("error getting symbol information " + symbolInfo.Error);
                if (symbolInfo.Data == null)
                    throw new ExchangeException("No exchange data retrieved (2)");
                SaveExchangeInfo(symbolInfo.OriginalData, "symbols.json");


                // Track which symbols are still active, to deactivate the ones we no longer follow
                SortedList<string, CryptoSymbol> activeSymbols = [];
                // Scanner names of the instruments we skip below. Intersected with the accepted names
                // after the loop, that gives the symbols whose name covers more than one instrument.
                List<string> rejectedSymbols = [];


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = [];
                    try
                    {
                        foreach (var symbolData in symbolInfo.Data)
                        {
                            // Filter BEFORE IsSymbolAccepted, same reason as in Mexc Futures: that method
                            // adopts the instrument name of whatever is passed in, and the continue below
                            // would skip the database update that has to persist it.
                            // The contract list has no settle asset, but the USD quoted contracts (21 of
                            // the 1215 on 14-08-2026) are the coin margined ones and report their turnover
                            // in the base currency, which the volume boundary cannot compare with the rest:
                            // BTCUSD states 1848 against the 4 billion of BTCUSDT.
                            if (symbolData.QuoteAsset.Equals("USD", StringComparison.OrdinalIgnoreCase))
                            {
#if DEBUG
                                GlobalData.AddTextToLogTab($"{symbolData.Symbol} inverse contract, quoted in {symbolData.QuoteAsset}");
#endif
                                rejectedSymbols.Add(symbolData.BaseAsset.ToUpper() + symbolData.QuoteAsset.ToUpper());
                                continue;
                            }

                            SymbolInfo info = ParseSymbol(symbolData.Symbol, symbolData.BaseAsset, symbolData.QuoteAsset);
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
                                //if (symbolData.Base.PriceDecimals)
                                //    symbol.QuantityTickSize = symbolData.LotSize.Value;
                                // The quantity of a futures order is expressed in contracts, so the step
                                // is the quantity precision times the base amount of one contract (as
                                // Kucoin and Mexc do). ContractQuantity is 0.001 for BTCUSDT, so one
                                // contract is 0.001 BTC and the bare QuantityPrecision (1 on every
                                // contract) was a factor 1000 out.
                                // Candle and SubscriptionKLineTicker read this field as well, to convert
                                // the contract volume of a kline into a quote volume.
                                symbol!.QuantityTickSize = symbolData.QuantityPrecision * symbolData.ContractQuantity;
                                symbol.QuantityMinimum = symbolData.MinQuantity * symbolData.ContractQuantity;
                                // MaxQuantity is filled, but a maximum has consequences for the Clamp
                                // routine, which is why the other exchanges leave it alone as well.
                                //symbol.QuantityMaximum = symbolInfo.LotSizeFilter?.MaxOrderQuantity ?? 0;

                                //symbol.QuoteValueMinimum = symbolInfo.LotSizeFilter?.MinOrderValue ?? 0;
                                //symbol.QuoteValueMaximum = symbolInfo.LotSizeFilter?.MaxOrderValue ?? 0;


                                // The minimum and maximum price for an order (in base price)
                                // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                // (which has consequences for the Clamp, which does expect values)
                                //symbol.PriceMinimum = symbolInfo.LotSizeFilter.MinOrderValue;
                                //symbol.PriceMaximum = symbolInfo.LotSizeFilter.MaxOrderValue;

                                // PricePrecision ("price_precision") IS the price step, despite its name:
                                // 0.1 for BTCUSDT, 0.01 for ETHUSDT, 0.00001 for DOGEUSDT. Without it the
                                // tick size stayed 0, and that breaks two things at once - the candle
                                // store rounds every price to a whole number (see CryptoCandle, which
                                // keeps prices as an int number of ticks, so DOGEUSDT became 0 and was
                                // thrown away as an empty candle) and Clamp divides by the step.
                                symbol.PriceTickSize = symbolData.PricePrecision;

                                //symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                //symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                // Turnover24h ("turnover_24h") is the 24 hour volume in the QUOTE currency,
                                // which is what the volume boundary and the rest of the scanner work with.
                                // Volume24h counts CONTRACTS over that same period: BTCUSDT reports 63 million
                                // against a turnover of 4 billion USDT, so the two are a factor contract size
                                // and price apart and cannot be compared between coins at all.
                                //// volume from the tickers
                                //if (volumeTicker.TryGetValue(symbol.ExchangeSymbol, out decimal volume))
                                //    symbol.Volume = volume;
                                //else
                                symbol.Volume = (double)symbolData.Turnover24h;

                                if (symbolData.Status == ContractStatus.Trading)
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

                        // Which scanner names cover more than one instrument (an inverse contract rejected
                        // above carries the same base and quote as a linear one would)
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
                            GlobalData.AddTextToLogTab($"{deactivated} coins deactivated");

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