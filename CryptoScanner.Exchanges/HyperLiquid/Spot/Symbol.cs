using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using HyperLiquid.Net.Clients;

namespace CryptoScanner.Core.Exchange.HyperLiquid.Spot;

public class Symbol() : SymbolBase(), ISymbol
{

    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new HyperLiquidRestClient(options => { options.OutputOriginalData = true; });
                var api = client.SpotApi;
                using CryptoDatabase database = new();
                database.Open();


                // tickers for volumes... (need volume because of filtered kline and price tickers)
                GlobalData.AddTextToLogTab($"Reading symbol and ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                // Counts against the same budget as the candle requests - it is an ordinary info
                // request, so it weighs 20 as well.
                var tickerInfo = await api.ExchangeData.GetExchangeInfoAndTickersAsync() ?? throw new ExchangeException("No ticker and symbol data received");
                if (!tickerInfo.Success)
                    GlobalData.AddErrorToLogTab($"error getting symbol ticker info {tickerInfo.Error}");
                if (tickerInfo == null)
                    throw new ExchangeException("No ticker data received");
                SaveExchangeInfo(tickerInfo, "tickers.json");

                // index volume
                SortedList<string, decimal> volumeTicker = [];
                // The price of the moment, only used to cap the price tick size below so the candle
                // storage cannot overflow. Not a property of the symbol, so it is not stored anywhere.
                SortedList<string, decimal> priceTicker = [];
                if (tickerInfo.Data != null && tickerInfo.Data != null)
                {
                    foreach (var tickerData in tickerInfo.Data.Tickers)
                    {
                        if (tickerData.Symbol != null)
                        {
                            string symbolName = tickerData.Symbol.Replace("/", "");
                            volumeTicker.TryAdd(symbolName, tickerData.QuoteVolume);
                            priceTicker.TryAdd(symbolName, tickerData.MidPrice ?? tickerData.MarkPrice);
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
                var symbolInfo = tickerInfo;
                //var symbolInfo = await api.ExchangeData.GetExchangeInfoAndTickersAsync() ?? throw new ExchangeException("No exchange data retrieved (1)");
                if (!symbolInfo.Success)
                    GlobalData.AddErrorToLogTab("error getting symbol information " + symbolInfo.Error);
                if (symbolInfo.Data == null)
                    throw new ExchangeException("No exchange data retrieved (2)");
                SaveExchangeInfo(symbolInfo, "symbols.json");


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
                        foreach (var symbolData in symbolInfo.Data.ExchangeInfo.Symbols)
                        {
                            SymbolInfo info = ParseSymbol(symbolData.Name, symbolData.BaseAsset.Name, symbolData.QuoteAsset.Name, ProductOfExchange(exchange));
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
                                // QuantityDecimals is szDecimals, a NUMBER of decimals and not a tick
                                // size, so it has to be converted (see SymbolBase.TickSizeFromDecimals).
                                // The size grid, the minimum quantity and the minimum order value in one
                                // place, shared with the perpetual market - see HyperLiquidOrderLimits, which
                                // is also where the reasoning behind each of them sits. Spot passes no
                                // leverage: HyperLiquid states no maximum order value for it.
                                HyperLiquidOrderLimits.ApplyLimits(symbol!, symbolData.BaseAsset.QuantityDecimals, maxLeverage: null);
                                //symbol.QuantityMinimum = symbolInfo.LotSizeFilter?.MinOrderQuantity ?? 0;
                                //symbol.QuantityMaximum = symbolInfo.LotSizeFilter?.MaxOrderQuantity ?? 0;

                                //symbol.QuoteValueMinimum = symbolInfo.LotSizeFilter?.MinOrderValue ?? 0;
                                //symbol.QuoteValueMaximum = symbolInfo.LotSizeFilter?.MaxOrderValue ?? 0;


                                // The minimum and maximum price for an order (in base price)
                                // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                // (which has consequences for the Clamp, which does expect values)
                                //symbol.PriceMinimum = symbolInfo.LotSizeFilter.MinOrderValue;
                                //symbol.PriceMaximum = symbolInfo.LotSizeFilter.MaxOrderValue;

                                // NOT BaseAsset.PriceDecimals: that field is weiDecimals, the on-chain
                                // precision of the token, which has nothing to do with the price grid of
                                // the market (the name the package gives it is misleading). HyperLiquid
                                // states the rule instead: a spot price carries at most 8 - szDecimals
                                // decimals. Checked against the mid price of all 293 pairs that quote one
                                // (17-08-2026): not a single price needed more decimals than that.
                                //
                                // Assigning the decimals straight into PriceTickSize, the way it used to
                                // happen here, made GlobalData derive ZERO decimals from it, after which
                                // every candle was stored rounded to a whole number: 42 of the 80 symbols
                                // that traded ended up with nothing but zero prices, and 53% of the whole
                                // 1m history was zero. The candle store has to be refetched once.
                                int priceDecimals = 8 - symbolData.BaseAsset.QuantityDecimals;
                                if (priceDecimals < 0)
                                    priceDecimals = 0;

                                // A tick that fine on a four digit price does not fit in the int a candle
                                // stores its price in (TSLA/USDC at 14162 would need 14.2 billion ticks),
                                // so the price of the moment decides how far the precision can go.
                                //
                                // On the PAIR, for the same reason as the volume lookup below: the ticker
                                // list is keyed on the exchange's own spelling, without the product behind
                                // the dot. On the scanner name it matched nothing at all, so referencePrice
                                // stayed zero for every symbol and LimitDecimalsToCandleRange handed the
                                // decimals straight back - the cap was in place but never capped anything.
                                // XAUT0/USDC (gold, 4422.10) kept a tick size of 0.000001, which is 4.42
                                // billion ticks against an int that holds 2.15 billion, and the flat candle
                                // the flush timer synthesizes threw an OverflowException every minute of the
                                // night of 31-08/01-09-2026.
                                priceTicker.TryGetValue(CryptoProduct.PairOf(symbol.Name), out decimal referencePrice);
                                symbol.PriceTickSize = TickSizeFromDecimals(
                                    LimitDecimalsToCandleRange(priceDecimals, referencePrice));

                                //symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                //symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                // volume from the tickers, looked up on the PAIR: the ticker list is keyed on the
                                // exchange's own spelling and knows nothing of the product behind the dot. On the
                                // scanner name it matched nothing at all and every symbol silently ended up at zero.
                                if (volumeTicker.TryGetValue(CryptoProduct.PairOf(symbol.Name), out decimal volume))
                                    symbol.Volume = (double)volume;
                                else
                                {
                                    symbol.Volume = 0;
                                    withoutVolume++;
                                }

                                //if (symbolData.Base.QuantityDecimalsState == InstrumentState.Live)
                                symbol.Status = 1;
                                //else
                                //  symbol.Status = 0; //Pass the status on (PreTrading, PostTrading or Halt)

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