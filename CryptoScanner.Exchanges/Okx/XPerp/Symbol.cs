using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using OKX.Net.Clients;
using OKX.Net.Enums;

namespace CryptoScanner.Core.Exchange.Okx.XPerp;

public class Symbol() : SymbolBase(), ISymbol
{

    /// <summary>
    /// The quote of an X-Perp under the name the coin has that actually moves. Okx states USD as the
    /// settlement asset of every one of them, because these contracts settle in USD VALUE rather than
    /// in one fixed coin. Under Okx Europe that value is paid in USDC or USDG, with USDC as the
    /// default when the account makes no choice of its own, so fees, funding, margin and pnl all run
    /// in USDC. The exchange interface and Altrady both name these markets after that coin
    /// (AAVE/USDC), and the scanner follows them: USD becomes USDC, every other settlement asset
    /// (the inverse contracts, which settle in the base coin) is left alone.
    /// </summary>
    private static string ScannerQuote(string? settlementAsset)
    {
        string asset = (settlementAsset ?? "").ToUpper();
        if (asset == "USD")
            return "USDC";
        return asset;
    }


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
                var tickerInfo = await api.ExchangeData.GetTickersAsync(InstrumentType.Futures) ?? throw new ExchangeException("No ticker data received");
                if (!tickerInfo.Success)
                    GlobalData.AddErrorToLogTab($"error getting symbol ticker info {tickerInfo.Error}");
                if (tickerInfo == null)
                    throw new ExchangeException("No ticker data received");
                SaveExchangeInfo(tickerInfo.OriginalData, "tickers.json");

                // index volume
                // Indexed on the instrument id ("BTC-USD_UM_XPERP-310404") because the scanner name of
                // these contracts cannot be reconstructed from it - the expiry date in the tail is part
                // of the name. For derivatives OKX reports volCcy24h in the contract value asset, so
                // multiply by the last price to get the volume in the settlement asset.
                SortedList<string, decimal> volumeTicker = [];
                if (tickerInfo.Data != null)
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
                var symbolInfo = await api.ExchangeData.GetSymbolsAsync(InstrumentType.Futures) ?? throw new ExchangeException("No symbol data received");
                if (!symbolInfo.Success)
                    GlobalData.AddErrorToLogTab("error getting symbol information " + symbolInfo.Error);
                if (symbolInfo.Data == null)
                    throw new ExchangeException("No exchange data retrieved (2)");
                SaveExchangeInfo(symbolInfo.OriginalData, "symbols.json");


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
                        foreach (var symbolData in symbolInfo.Data)
                        {
                            // InstrumentType.Futures holds three products at once, and only one of them
                            // belongs here (counted on 27-08-2026):
                            //  - 155 X-Perps, ruleType xperp, the USD_UM contracts this market is about;
                            //  -  16 dated USD_UM contracts, which expire and roll to a new name;
                            //  -  16 dated inverse contracts, which settle in the base asset.
                            // This filter must run BEFORE IsSymbolAccepted, the way the Binance and Bybit
                            // markets do it: a dated USD_UM contract carries the same contract value and
                            // settlement asset as its X-Perp (BTC-USD_UM-260828 next to
                            // BTC-USD_UM_XPERP-310404), so both parse to the scanner name BTCUSDC. Four
                            // names are shared that way today (BTCUSDC, ETHUSDC, SOLUSDC, XAUUSDC) and
                            // RegisterAmbiguousSymbolNames below is what records them. The rejected name
                            // goes through ScannerQuote as well, otherwise the two sides no longer match
                            // and the check silently records nothing.
                            if (symbolData.RuleType != SymbolRuleType.Perp)
                            {
                                rejectedSymbols.Add((symbolData.ContractValueAsset ?? "").ToUpper() + ScannerQuote(symbolData.SettlementAsset));
                                continue;
                            }

                            // Only take instruments that are actually tradable (skip PreTrading, PostTrading or Halt).
                            // Symbols that were live before are deactivated further down because they are
                            // missing from activeSymbols.
                            if (symbolData.State != InstrumentState.Live)
                                continue;

                            // These contracts report no base and no quote asset, and their instrument
                            // family ("BTC-USD_UM_XPERP") carries the product name rather than the quote.
                            // The pair is in the two asset fields instead: the contract value asset is
                            // what the contract is on, the settlement asset is what it pays out in. That
                            // settlement asset reads USD for every one of them - Okx settles these in USD
                            // VALUE, to be paid in USDC (or another accepted currency), which is exactly
                            // what makes them the USDC route on this exchange. ScannerQuote turns that
                            // USD into the coin that actually moves, so the scanner name is BTCUSDC.
                            string baseAsset = symbolData.ContractValueAsset ?? "";
                            string quoteAsset = ScannerQuote(symbolData.SettlementAsset);

                            SymbolInfo info = ParseSymbol(symbolData.Symbol, baseAsset, quoteAsset);
                            if (IsSymbolAccepted(exchange, info, api, TradingMode.PerpetualLinear, out CryptoSymbol? symbol))
                            {
                                // min, max en tick (in base amount)
                                // An order quantity is expressed in contracts, ctVal tells how much of the
                                // base asset one contract represents. Multiply them to get the step in base amount.
                                if (symbolData.LotSize.HasValue && symbolData.ContractValue.HasValue)
                                    symbol!.QuantityTickSize = symbolData.LotSize.Value * symbolData.ContractValue.Value;
                                else if (symbolData.LotSize.HasValue)
                                    symbol!.QuantityTickSize = symbolData.LotSize.Value;

                                // The minimum and maximum price for an order (in base price)
                                // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                // (which has consequences for the Clamp, which does expect values)
                                if (symbolData.TickSize.HasValue)
                                    symbol!.PriceTickSize = symbolData.TickSize.Value;

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
