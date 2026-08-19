using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using Kucoin.Net.Clients;

namespace CryptoScanner.Core.Exchange.Kucoin.Futures;

public class Symbol() : SymbolBase(), ISymbol
{
    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new KucoinRestClient(options => { options.OutputOriginalData = true; });
                var api = client.FuturesApi;
                using CryptoDatabase database = new();
                database.Open();



                //// Tickers for the 24h volume
                //GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeOptions.ExchangeSymbol}");
                //KucoinWeights.WaitForFairWeight(1);
                //var tickerInfo = await api.ExchangeData.GetTickersAsync() ?? throw new ExchangeException("No ticker data received");
                //if (!tickerInfo.Success)
                //    GlobalData.AddErrorToLogTab($"error getting symbol ticker {tickerInfo.Error}");
                //if (tickerInfo == null)
                //    throw new ExchangeException("No ticker data received");
                //SaveExchangeInfo(tickerInfo.OriginalData, "tickers.json");

                //// Create dictionary for the volume
                //SortedList<string, decimal> volumeTicker = [];
                //if (tickerInfo.Data != null && tickerInfo.Data != null)
                //{
                //    foreach (var tickerData in tickerInfo.Data)
                //    {
                //        if (tickerData.QuoteVolume.HasValue)
                //        {
                //            string symbolName = tickerData.ScannerSymbol.Replace("-", "");
                //            volumeTicker.Add(symbolName, tickerData.QuoteVolume.Value);
                //        }
                //    }
                //}


                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                KucoinWeights.WaitForFairWeight(1);
                var symbolInfo = await api.ExchangeData.GetSymbolsAsync() ?? throw new ExchangeException("No symbol data received");
                if (!symbolInfo.Success)
                    GlobalData.AddErrorToLogTab($"error getting exchangeinfo {symbolInfo.Error}");
                if (symbolInfo == null)
                    throw new ExchangeException("No exchange data received");
                SaveExchangeInfo(symbolInfo.OriginalData, "symbols.json");

                if (symbolInfo.Data != null)
                {

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
                                // Filter BEFORE IsSymbolAccepted, same reason as in Binance and Bybit Futures:
                                // that method adopts the instrument id of whatever is passed in, and the
                                // continue below would skip the database update that has to persist it.
                                if (symbolData.RootSymbol != symbolData.QuoteAsset)
                                {
#if DEBUG
                                    // was info.ExchangeName, which is the same value but is only parsed below now
                                    GlobalData.AddTextToLogTab($"{symbolData.Symbol} rootsymbol != quote {symbolData.RootSymbol}");
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

                                    //Minimum and maximum amount for an order (in base amount)
                                    //symbol.QuantityMinimum = symbolData.Base.MinQuantity;
                                    //symbol.QuantityMaximum = symbolData.MaxQuantity; //baseMinSize
                                    // Dit klopt niet, deze heeft wederom effect op de Clamp routine!

                                    // The quantity of a futures order is expressed in contracts, so the step
                                    // is the lot size times the base amount of one contract (as BloFin does).
                                    // TickSize is the PRICE step and belongs below. SubscriptionKLineTicker
                                    // reads this field as well, to convert the contract volume of a streamed
                                    // kline into a quote volume.
                                    symbol!.QuantityTickSize = symbolData.LotSize * symbolData.Multiplier;

                                    // The minimum and maximum price for an order (in base price)
                                    // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                    // (which has consequences for the Clamp, which does expect values)
                                    //symbol.PriceMinimum = niet aanwezig! binanceSymbol.PriceFilter.min;
                                    //symbol.PriceMaximum = niet aanwezig! binanceSymbol.LotSizeFilter.MaxOrderValue;
                                    symbol.PriceTickSize = symbolData.TickSize;

                                    //symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                    //symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                    // Turnover24H is the 24 hour volume in the QUOTE currency, which is what
                                    // the volume boundary and the rest of the scanner work with. Volume24H is
                                    // the same period in the base currency: XBTUSDCM reports 21.88 against a
                                    // turnover of 1380480 USDC, so it dropped out while SUIUSDCM (1346 against
                                    // 921 USDC) stayed - exactly the wrong way around.
                                    symbol.Volume = (double)symbolData.Turnover24H;
                                    //// volume from the tickers
                                    //if (volumeTicker.TryGetValue(symbol.ExchangeSymbol, out decimal volume))
                                    //    symbol.Volume = volume;
                                    //else
                                    //    symbol.Volume = 0;

                                    if (symbolData.Status == "Open")
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

                            // Which scanner names cover more than one instrument (the inverse contracts
                            // rejected above carry the same base and quote as a linear one)
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
                                GlobalData.AddTextToLogTab($"{deactivated} symbols deactivated");


                            // Add the new symbols to the list
                            // (because the symbols only get an id during the BulkInsert)
                            foreach (CryptoSymbol symbol in cache)
                            {
                                GlobalData.AddSymbol(symbol);
                            }


                            transaction.Commit();
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
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
            }

        }
    }
}