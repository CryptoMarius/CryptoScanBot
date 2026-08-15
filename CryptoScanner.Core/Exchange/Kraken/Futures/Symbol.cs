using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using Kraken.Net.Clients;
using Kraken.Net.Enums;

namespace CryptoScanner.Core.Exchange.Kraken.Futures;

public class Symbol() : SymbolBase(), ISymbol
{
    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new KrakenRestClient(options => { options.OutputOriginalData = true; });
                var api = client.FuturesApi;

                using CryptoDatabase database = new();
                database.Open();


                // Tickers for the 24h volume
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var tickerInfo = await api.ExchangeData.GetTickersAsync() ?? throw new ExchangeException("No ticker data received");
                if (!tickerInfo.Success)
                    GlobalData.AddErrorToLogTab($"error getting symbol ticker {tickerInfo.Error}");
                // Tested on the data, not on the result object: a WebCallResult is never null, so the
                // check this replaces could not fire and a failed call simply carried on with an empty
                // volume list - after which every symbol ends up with volume 0 and drops out of the
                // scanner. Better to leave the symbols as they were until the next round.
                if (tickerInfo.Data == null)
                    throw new ExchangeException("No ticker data received");
                SaveExchangeInfo(tickerInfo, "tickers.json");

                // Create dictionary for the volume
                SortedList<string, decimal> volumeTicker = [];
                foreach (var tickerData in tickerInfo.Data)
                    volumeTicker[tickerData.Symbol] = tickerData.Volume24hQuote;



                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var symbolInfo = await api.ExchangeData.GetSymbolsAsync() ?? throw new ExchangeException("No symbol data received");
                if (!symbolInfo.Success)
                    GlobalData.AddErrorToLogTab("error getting exchangeinfo " + symbolInfo.Error);
                if (symbolInfo.Data == null)
                    throw new ExchangeException("No exchange data received (2)");
                SaveExchangeInfo(symbolInfo, "symbols.json");


                // Track which symbols are still active, to deactivate the ones we no longer follow
                SortedList<string, CryptoSymbol> activeSymbols = [];
                // Scanner names of the instruments we skip below, see RegisterAmbiguousSymbolNames
                List<string> rejectedSymbols = [];


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = [];
                    try
                    {
                        foreach (var symbolData in symbolInfo.Data)
                        {
                            // Only take the tradeable PF_ perpetuals (flexible futures without an
                            // expiry): linear, USD quoted and multi collateral, which is the
                            // PerpetualLinear mode requested below. Everything else is skipped
                            // because it collides on the scanner name: the PI_ inverse contracts
                            // and the dated FF_/FI_ contracts all parse to the same base+quote as
                            // their PF_ counterpart (PF_ETHUSD, PI_ETHUSD and FF_ETHUSD_260925 all
                            // become ETHUSD), after which activeSymbols.Add() further down throws
                            // a duplicate key and rolls back the whole transaction - so nothing at
                            // all gets stored and the exchange keeps its old symbol list.
                            //
                            // This replaces a "Category != DeFi" test filter, which threw away
                            // almost everything: PF_XBTUSD and PF_ETHUSD have category "Layer 1"
                            // and the inverse contracts have an empty category. Measured against
                            // the instrument list: DeFi gave 95 symbols, this gives 274, all PF_
                            // and all with a unique base+quote.
                            if (!symbolData.Tradeable || symbolData.Type != SymbolType.FlexibleFutures
                                || symbolData.LastTradingTime != null)
                            {
                                rejectedSymbols.Add(symbolData.BaseAsset.ToUpper() + symbolData.QuoteAsset.ToUpper());
                                continue;
                            }

                            // TODO? Inspect
                            SymbolInfo info = ParseSymbol(symbolData.Symbol, symbolData.BaseAsset, symbolData.QuoteAsset);

                            // Safety net for the same collision: a duplicate name must never take
                            // the entire fetch down, because the exception rolls back the whole
                            // transaction and then not a single symbol is stored. Tested before
                            // IsSymbolAccepted() so the second contract cannot overwrite the
                            // ExchangeName of the first one either.
                            if (activeSymbols.ContainsKey(info.ScannerName))
                            {
                                GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} " +
                                    $"{symbolData.Symbol} skipped, {info.ScannerName} is already taken");
                                rejectedSymbols.Add(info.ScannerName);
                                continue;
                            }

                            if (IsSymbolAccepted(exchange, info, api, TradingMode.PerpetualLinear, out CryptoSymbol? symbol))
                            {
                                //string name = symbolInfo.WebsocketName; // AlternateName; // symbolInfo.Base + symbolInfo.Quote;
                                //string[] nameParts = name.Split('/');
                                //name = nameParts[0] + nameParts[1];


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

                                // The minimum and maximum price for an order (in base price)
                                // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                // (which has consequences for the Clamp, which does expect values)
                                //symbol.PriceMinimum = symbolInfo.PriceFilter.MinPrice;
                                //symbol.PriceMaximum = symbolInfo.PriceFilter.MaxPrice;

                                //if (symbolData.MinValue.HasValue)
                                //    symbol.QuoteValueMinimum = (decimal)symbolData.MinValue;

                                symbol!.PriceTickSize = symbolData.TickSize ?? 0; // ? binanceSymbol.PriceFilter.TickSize;

                                //symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                //symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                // volume from the tickers
                                if (volumeTicker.TryGetValue(symbol.ExchangeName, out decimal volume))
                                    symbol.Volume = (double)volume;
                                else
                                    symbol.Volume = 0;

                                //if (symbolData.Status == SymbolStatus.Online)
                                symbol.Status = 1;
                                //else
                                //symbol.Status = 0;

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

                        // Which scanner names cover more than one instrument (BTCUSD, ETHUSD, LTCUSD,
                        // SOLUSD and XRPUSD: the PI_ inverse contracts and the dated FF_ contracts
                        // share their base and quote with the PF_ perpetual that was accepted)
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
