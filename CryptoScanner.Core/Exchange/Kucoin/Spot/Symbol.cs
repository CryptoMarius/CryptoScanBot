using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using Kucoin.Net.Clients;

namespace CryptoScanner.Core.Exchange.Kucoin.Spot;

public class Symbol() : SymbolBase(), ISymbol
{
    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new KucoinRestClient(options => { options.OutputOriginalData = true; });
                var api = client.SpotApi;
                using CryptoDatabase database = new();
                database.Open();



                // Tickers for the 24h volume
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                KucoinWeights.WaitForFairWeight(1);
                var tickerInfo = await api.ExchangeData.GetTickersAsync() ?? throw new ExchangeException("No ticker data received");
                if (!tickerInfo.Success)
                    GlobalData.AddErrorToLogTab($"error getting symbol ticker {tickerInfo.Error}");
                if (tickerInfo == null)
                    throw new ExchangeException("No ticker data received");
                SaveExchangeInfo(tickerInfo.OriginalData, "tickers.json");

                // Create dictionary for the volume
                SortedList<string, decimal> volumeTicker = [];
                if (tickerInfo.Data != null && tickerInfo.Data.Data != null)
                {
                    foreach (var tickerData in tickerInfo.Data.Data)
                    {
                        if (tickerData.QuoteVolume.HasValue)
                        {
                            // Assigned instead of added: removing the dash can in theory make two different
                            // pairs collide, and an exception here would abort the whole symbol update.
                            string symbolName = tickerData.Symbol.Replace("-", "");
                            volumeTicker[symbolName] = tickerData.QuoteVolume.Value;
                        }
                    }
                }




                // Without the tickers every symbol would end up with a volume of 0, drop below the
                // minimum volume and have its candles and subscriptions released. Stop instead, the
                // next refresh cycle will try again.
                if (volumeTicker.Count == 0)
                    throw new ExchangeException("No ticker data received");

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
                    // Scanner names of the instruments we do not take over. Nothing is filtered out here
                    // (unlike the futures side), so this only fills up when IsSymbolAccepted refuses one.
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
                            foreach (var symbolData in symbolInfo.Data)
                            {
                                SymbolInfo info = ParseSymbol(symbolData.Symbol, symbolData.BaseAsset, symbolData.QuoteAsset);
                                if (IsSymbolAccepted(exchange, info, api, TradingMode.Spot, out CryptoSymbol? symbol))
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
                                    symbol!.QuantityMinimum = symbolData.BaseMinQuantity;
                                    symbol.QuantityMaximum = symbolData.BaseMaxQuantity; //baseMinSize
                                                                                         // Dit klopt niet, deze heeft wederom effect op de Clamp routine!
                                    symbol.QuantityTickSize = symbolData.BaseIncrement;

                                    // The minimum and maximum price for an order (in base price)
                                    // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                    // (which has consequences for the Clamp, which does expect values)
                                    //symbol.PriceMinimum = niet aanwezig! binanceSymbol.PriceFilter.min;
                                    //symbol.PriceMaximum = niet aanwezig! binanceSymbol.LotSizeFilter.MaxOrderValue;

                                    symbol.PriceTickSize = symbolData.PriceIncrement;

                                    //symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                    //symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                    // volume from the tickers
                                    if (volumeTicker.TryGetValue(symbol.Name, out decimal volume))
                                        symbol.Volume = (double)volume;
                                    else
                                    {
                                        symbol.Volume = 0;
                                        withoutVolume++;
                                    }

                                    if (symbolData.EnableTrading)
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
                                else
                                    rejectedSymbols.Add(info.ScannerName);
                            }

                            // Which scanner names cover more than one instrument
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


                            if (withoutVolume > 0)
                                GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} " +
                                    $"{withoutVolume} symbols without a 24 hour volume (of {activeSymbols.Count})");

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