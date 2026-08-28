using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using Mexc.Net.Clients;
using Mexc.Net.Enums;

namespace CryptoScanner.Core.Exchange.Mexc.Perpetual;

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
                var api = client.FuturesApi;
                using CryptoDatabase database = new();
                database.Open();


                // Tickers for the 24 hour volume (the contract list carries no volume of its own)
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var tickerInfo = await api.ExchangeData.GetTickersAsync() ?? throw new ExchangeException("No ticker data received");
                if (!tickerInfo.Success)
                    GlobalData.AddErrorToLogTab($"error getting symbol ticker {tickerInfo.Error}");
                SaveExchangeInfo(tickerInfo.OriginalData, "tickers.json");

                // index volume
                SortedList<string, decimal> volumeTicker = [];
                if (tickerInfo.Data != null)
                {
                    foreach (var tickerData in tickerInfo.Data)
                    {
                        // QuoteVolume24h ("amount24") is the 24 hour turnover in the quote currency, which
                        // is what the volume boundary and the rest of the scanner work with. Volume24h is
                        // the same period counted in CONTRACTS.
                        // Assigned instead of added, so a duplicate name cannot abort the whole update.
                        volumeTicker[tickerData.Symbol] = tickerData.QuoteVolume24h;
                    }
                }


                // Without the tickers every symbol would end up with a volume of 0, drop below the
                // minimum volume and have its candles and subscriptions released. Stop instead, the
                // next refresh cycle will try again.
                if (volumeTicker.Count == 0)
                    throw new ExchangeException("No ticker data received");

                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var symbolInfo = await api.ExchangeData.GetSymbolsAsync() ?? throw new ExchangeException("No symbol data received");
                if (!symbolInfo.Success)
                    GlobalData.AddErrorToLogTab($"error getting exchangeinfo {symbolInfo.Error}");
                if (symbolInfo == null || symbolInfo.Data == null)
                    throw new ExchangeException("No exchange data received");
                SaveExchangeInfo(symbolInfo.OriginalData, "symbols.json");



                // Track which symbols are still active, to deactivate the ones we no longer follow
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
                        foreach (var symbolData in symbolInfo.Data)
                        {
                            // Filter BEFORE IsSymbolAccepted, same reason as in Kucoin Perpetual: that method
                            // adopts the instrument name of whatever is passed in, and the continue below
                            // would skip the database update that has to persist it.
                            // The inverse contracts (10 of the 1109 on 14-08-2026, all quoted in USD and
                            // settled in the base coin) report their turnover in the base currency, which
                            // the volume boundary cannot compare with the rest.
                            if (symbolData.SettleAsset != symbolData.QuoteAsset)
                            {
#if DEBUG
                                GlobalData.AddTextToLogTab($"{symbolData.Symbol} settle != quote {symbolData.SettleAsset}");
#endif
                                rejectedSymbols.Add(symbolData.BaseAsset.ToUpper() + symbolData.QuoteAsset.ToUpper());
                                continue;
                            }

                            SymbolInfo info = ParseSymbol(symbolData.Symbol, symbolData.BaseAsset, symbolData.QuoteAsset, ProductOfExchange(exchange));
                            if (IsSymbolAccepted(exchange, info, api, TradingMode.PerpetualLinear, out CryptoSymbol? symbol))
                            {
                                // The quantity of a futures order is expressed in contracts, so the step is
                                // the volume unit times the base amount of one contract (as Kucoin does).
                                // ContractSize is 0.0001 for BTC_USDT, so one contract is 0.0001 BTC.
                                symbol!.QuantityTickSize = symbolData.VolumeUnit * symbolData.ContractSize;
                                symbol.QuantityMinimum = symbolData.MinQuantity * symbolData.ContractSize;
                                // MaxQuantity is filled (400000 contracts for BTC_USDT), but a maximum has
                                // consequences for the Clamp routine, which is why the other exchanges leave
                                // it alone as well.
                                //symbol.QuantityMaximum = symbolData.MaxQuantity * symbolData.ContractSize;

                                // PriceUnit is the price step. It is the same value as 10 to the power minus
                                // PriceScale on all 986 USDT contracts (checked 14-08-2026), and every price
                                // in the fetched candles is a multiple of it - so candles keep their detail.
                                symbol.PriceTickSize = symbolData.PriceUnit;

                                // volume from the tickers
                                if (volumeTicker.TryGetValue(symbol.ExchangeName, out decimal volume))
                                    symbol.Volume = (double)volume;
                                else
                                {
                                    symbol.Volume = 0;
                                    withoutVolume++;
                                }

                                if (symbolData.ContractStatus == ContractStatus.Enabled)
                                    symbol.Status = 1;
                                else
                                    symbol.Status = 0; //Pass the status on (Delivering, Completed, Offline or Paused)

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

                if (tickerInfo.Success && symbolInfo.Success)
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
