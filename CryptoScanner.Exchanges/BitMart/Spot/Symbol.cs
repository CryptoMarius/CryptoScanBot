using BitMart.Net.Clients;
using BitMart.Net.Enums;

using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Exchange.BitMart.Spot;

public class Symbol() : SymbolBase(), ISymbol
{
    /// <summary>
    /// Ten to the power minus <paramref name="decimals"/>, as an exact decimal. BitMart states the
    /// price step of a spot pair as a number of digits instead of a step, and the double route
    /// (Math.Pow) would hand a value with a rounding tail to a field the whole candle store rounds
    /// against. Capped at 15 because CryptoCandle keeps its tick decimals in a nibble.
    /// Moved to SymbolBase on 17-08-2026, because Kraken and HyperLiquid state their precision the
    /// same way and were writing the number of decimals into the tick size field unconverted.
    /// </summary>
    private static new decimal TickSizeFromDecimals(int decimals) => SymbolBase.TickSizeFromDecimals(decimals);


    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new BitMartRestClient(options => { options.OutputOriginalData = true; });
                using CryptoDatabase database = new();
                var api = client.SpotApi;
                database.Open();


                // tickers for volumes... (need volume because of filtered kline and price tickers)
                GlobalData.AddTextToLogTab($"Reading symbol and ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                //LimitRate.WaitForFairWeight(1);
                var tickerInfo = await api.ExchangeData.GetTickersAsync();
                if (!tickerInfo.Success)
                    GlobalData.AddErrorToLogTab($"error getting symbol ticker info {tickerInfo.Error}");
                if (tickerInfo == null)
                    throw new ExchangeException("No ticker data received");
                SaveExchangeInfo(tickerInfo.OriginalData, "tickers.json");

                // index volume
                SortedList<string, decimal> volumeTicker = [];
                if (tickerInfo.Data != null && tickerInfo.Data != null)
                {
                    foreach (var tickerData in tickerInfo.Data)
                    {
                        if (tickerData.Symbol != null)
                        {
                            // QuoteVolume24h ("qv_24h") is the 24 hour turnover in the quote currency,
                            // which is what the volume boundary and the rest of the scanner work with.
                            // Volume24h ("v_24h") is the same period counted in the BASE currency, so
                            // it made an expensive coin look like it barely traded (AVAX_USDT reports
                            // 369 against a turnover of 2365 USDT).
                            // Assigned instead of added, so a duplicate name cannot abort the whole update.
                            volumeTicker[tickerData.Symbol] = tickerData.QuoteVolume24h;
                        }
                    }
                }



                // Without the tickers every symbol would end up with a volume of 0, drop below the
                // minimum volume and have its candles and subscriptions released. Stop instead, the
                // next refresh cycle will try again.
                if (volumeTicker.Count == 0)
                    throw new ExchangeException("No ticker data received");

                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                //LimitRate.WaitForFairWeight(1);
                var symbolInfo = await api.ExchangeData.GetSymbolsAsync() ?? throw new ExchangeException("No exchange data retrieved (1)");
                if (!symbolInfo.Success)
                    GlobalData.AddErrorToLogTab("error getting symbol information " + symbolInfo.Error);
                if (symbolInfo.Data == null)
                    throw new ExchangeException("No exchange data retrieved (2)");
                SaveExchangeInfo(symbolInfo.OriginalData, "symbols.json");


                // Track which symbols are still active, to deactivate the ones we no longer follow
                SortedList<string, CryptoSymbol> activeSymbols = [];
                // How many pairs claim to be trading while they are not traded any more
                int notTraded = 0;


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

                                // min, max en tick (in base amount)
                                //if (symbolData.Base.PriceDecimals)
                                //    symbol.QuantityTickSize = symbolData.LotSize.Value;
                                //symbol.QuantityTickSize = symbolData. QuantityDecimals;
                                // QuoteIncrement ("quote_increment") is the step of the order QUANTITY,
                                // despite its name - it is the same value as BaseMinQuantity on every
                                // pair (0.00001 for BTC_USDT, 1 for DOGE_USDT).
                                if (symbolData.QuoteIncrement.HasValue)
                                    symbol!.QuantityTickSize = symbolData.QuoteIncrement.Value;
                                if (symbolData.BaseMinQuantity.HasValue)
                                    symbol!.QuantityMinimum = symbolData.BaseMinQuantity.Value;
                                //symbol.QuantityMaximum = symbolInfo.LotSizeFilter?.MaxOrderQuantity ?? 0;

                                //symbol.QuoteValueMinimum = symbolInfo.LotSizeFilter?.MinOrderValue ?? 0;
                                //symbol.QuoteValueMaximum = symbolInfo.LotSizeFilter?.MaxOrderValue ?? 0;


                                // The minimum and maximum price for an order (in base price)
                                // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                // (which has consequences for the Clamp, which does expect values)
                                //symbol.PriceMinimum = symbolInfo.LotSizeFilter.MinOrderValue;
                                //symbol.PriceMaximum = symbolInfo.LotSizeFilter.MaxOrderValue;

                                // The price step is ten to the power minus PriceMaxPrecision, and that
                                // is also the number of decimals every fetched candle carries (checked
                                // 14-08-2026: 2 for ETH_USDT, 6 for DOGE_USDT, 10 for SHIB_USDT).
                                // This used to be QuoteIncrement, which is the quantity step above, and
                                // that wrecked the candles: it stores prices as an int number of ticks
                                // of PriceDecimals (see CryptoCandle), so DOGE_USDT (step 1, so zero
                                // decimals) rounded every price to 0 and was thrown away as an empty
                                // candle, while BTC_USDT (step 0.00001) needed 6.2 billion ticks and
                                // overflowed the int.
                                // PriceMinPrecision is not usable, it is -1 on BTC_USDT and ETH_USDT.
                                symbol!.PriceTickSize = TickSizeFromDecimals(symbolData.PriceMaxPrecision);

                                //symbol!.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                //symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                // volume from the tickers
                                if (volumeTicker.TryGetValue(symbolData.Symbol, out decimal volume))
                                    symbol!.Volume = (double)volume;
                                else
                                    symbol!.Volume = 0;

                                // The ticker list holds the pairs that are really being traded, the same
                                // way the funding rate list does for the futures. BitMart leaves the trade
                                // status on trading long after the trading has stopped: on 16-08-2026 the
                                // 65 pairs were all trading while only 45 of them had a ticker, and the
                                // klines of the other 20 (PENDLE_USDT, LDO_USDT, ONDO_USDT and so on) show
                                // a volume of zero over the whole day. Membership of the list is used
                                // instead of a volume of zero because a thin pair can round down to almost
                                // nothing while it is still traded (SOL_BTC turns over 0.0019 BTC a day).
                                // The list is only believed when the call actually returned something,
                                // because a failed call would otherwise deactivate the entire exchange.
                                bool delisted = symbolData.PlannedDelistTime.HasValue && symbolData.PlannedDelistTime.Value <= DateTime.UtcNow;
                                bool withoutTicker = volumeTicker.Count > 0 && !volumeTicker.ContainsKey(symbolData.Symbol);
                                if (symbolData.TradeStatus == SymbolStatus.Trading && (delisted || withoutTicker))
                                    notTraded++;

                                if (symbolData.TradeStatus == SymbolStatus.Trading && !delisted && !withoutTicker)
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
                        if (notTraded > 0)
                            GlobalData.AddTextToLogTab($"{notTraded} coins report trading without being traded");

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