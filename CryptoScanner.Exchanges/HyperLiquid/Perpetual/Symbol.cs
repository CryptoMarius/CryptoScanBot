using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using HyperLiquid.Net.Clients;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.Core.Exchange.HyperLiquid.Perpetual;

public class Symbol() : SymbolBase(), ISymbol
{

    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new HyperLiquidRestClient(options => { options.OutputOriginalData = true; });
                var api = client.FuturesApi;
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
                if (tickerInfo.Data != null && tickerInfo.Data != null)
                {
                    foreach (var tickerData in tickerInfo.Data.Tickers)
                    {
                        if (tickerData.Symbol != null)
                        {
                            SymbolInfo info = ParseSymbol(tickerData.Symbol, tickerData.Symbol, "USDC", ProductOfExchange(exchange));
                            volumeTicker.TryAdd(info.ExchangeName, tickerData.NotionalVolume); // QuoteVolume?
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
                        for (int i = 0; i < symbolInfo.Data.ExchangeInfo.Symbols.Count(); i++)
                        {
                            var tickerData = symbolInfo.Data.Tickers[i];
                            var symbolData = symbolInfo.Data.ExchangeInfo.Symbols[i];

                            // TODO: 
                            SymbolInfo info = ParseSymbol(symbolData.Name, symbolData.Name, "USDC", ProductOfExchange(exchange));
                            if (IsSymbolAccepted(exchange, info, api, TradingMode.PerpetualLinear, out CryptoSymbol? symbol))
                            {

                                //Temporarily copy everything (because of the new fields)
                                //The precision to use for prices
                                //symbol.BaseAssetPrecision = symbolData.QuantityDecimals;
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
                                // size (see SymbolBase.TickSizeFromDecimals). Written straight into the
                                // field it left 97 of the 233 instruments on a tick size of zero - the
                                // ones with szDecimals 0 - and gave the rest a tick of 1, 2 or 3 base
                                // units. The price tick below is derived from the mark price and was
                                // never affected, so only order sizing suffered from this.
                                // The size grid, the minimum quantity and the order value limits in one
                                // place, shared with the spot market - see HyperLiquidOrderLimits, which is
                                // also where the reasoning behind each of them sits. maxLeverage is the one
                                // number of the three that the meta does carry per symbol, and it decides the
                                // maximum order value.
                                HyperLiquidOrderLimits.ApplyLimits(symbol!, symbolData.QuantityDecimals, symbolData.MaxLeverage);

                                //symbol.QuantityMinimum = symbolInfo.LotSizeFilter?.MinOrderQuantity ?? 0;
                                //symbol.QuantityMaximum = symbolInfo.LotSizeFilter?.MaxOrderQuantity ?? 0;

                                //symbol.QuoteValueMinimum = symbolInfo.LotSizeFilter?.MinOrderValue ?? 0;
                                //symbol.QuoteValueMaximum = symbolInfo.LotSizeFilter?.MaxOrderValue ?? 0;


                                // The minimum and maximum price for an order (in base price)
                                // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                // (which has consequences for the Clamp, which does expect values)
                                //symbol.PriceMinimum = symbolInfo.LotSizeFilter.MinOrderValue;
                                //symbol.PriceMaximum = symbolInfo.LotSizeFilter.MaxOrderValue;

                                symbol.PriceTickSize = PriceTickFromMarkPrice(tickerData.MarkPrice, symbolData.QuantityDecimals);
                                // An existing symbol keeps the decimals it was loaded with unless we
                                // derive them again here (see CryptoSymbol.DeriveDecimalsFromTickSizes).
                                symbol.DeriveDecimalsFromTickSizes();

                                //symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                //symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                // volume from the tickers
                                if (volumeTicker.TryGetValue(symbol.ExchangeName, out decimal volume))
                                    symbol.Volume = (double)volume;
                                else
                                {
                                    symbol.Volume = 0;
                                    withoutVolume++;
                                }

                                if (!symbolData.IsDelisted)
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

                        // The markets that outside parties deployed on HyperLiquid, next to the one
                        // HyperLiquid runs itself. Added here rather than in a market of their own:
                        // it is the same address, the same account and the same USDC as margin, and
                        // GetKlinesAsync and the kline subscription both take "xyz:GOLD" unchanged.
                        withoutVolume += await AddDeployedMarketsAsync(exchange, client, database, transaction, cache, activeSymbols);

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


    /// <summary>
    /// How many decimals a PERPETUAL price may carry at most, minus szDecimals. HyperLiquid states
    /// 8 for a spot price - see the Spot/Symbol.cs of this exchange, which applies the same rule.
    /// </summary>
    private const int MaxPriceDecimals = 6;


    /// <summary>
    /// The price tick of a perpetual market. HyperLiquid publishes no tick size of its own, and the
    /// number of decimals it does publish (szDecimals) is about the QUANTITY, not the price, so the
    /// tick has to come from the two rules HyperLiquid states for an ORDER price instead: at most
    /// five significant figures, and at most 6 - szDecimals decimals. Whichever of the two is the
    /// tighter decides.
    /// <para>
    /// Counting the decimals the mark price happened to be written with - what this did until
    /// 30-08-2026 - answers a different question, and the answer moves with the price: HyperLiquid
    /// drops trailing zeros, so 0.8549 gave a tick of 0.0001 where 0.85497 gave 0.00001. With the
    /// symbol refresh running every hour that is not a rare event. On 30-08-2026 at 14:05 DOT and
    /// WIF changed tick over nothing else, after which every take profit and stop loss order of an
    /// open position was cancelled and replaced one tick away and reported over Telegram as a
    /// changed break-even price - while the break-even price had not moved at all. In that same
    /// answer the mark price and the oracle price, two prices of the same instant, disagreed about
    /// the tick for 92 of the 232 markets. It also landed on ticks the exchange does not accept:
    /// BTC ended up at 0.1 while HyperLiquid rejects 78270.1 (six significant figures and not an
    /// integer), and AVAX at 0.001 where 0.0001 is allowed.
    /// </para>
    /// <para>
    /// That old version also went through markPrice.ToString() and looked for a '.', which only
    /// exists on a machine whose decimal separator IS a point. A tester on a Dutch Windows (decimal
    /// comma) got "0,41234", every character became a zero, and the tick came out as 1 for every
    /// single market: prices under 0.50 rounded to a candle close of 0, the flush skipped them, and
    /// those coins were restarted every ten minutes as 'inactive' from 29-08 to 03-09-2026. The
    /// arithmetic below has no such dependency.
    /// </para>
    /// <para>
    /// A price is still needed, but only for its ORDER OF MAGNITUDE - the digit position the first
    /// significant figure sits in - and that is stable where the exact spelling is not. Five
    /// significant figures also keep price/tick under 100.000, so the int a candle stores its price
    /// in cannot overflow here and SymbolBase.LimitDecimalsToCandleRange has nothing left to cap.
    /// </para>
    /// </summary>
    /// <param name="markPrice">A current price of the market; only its magnitude is used</param>
    /// <param name="quantityDecimals">szDecimals of the market</param>
    internal static decimal PriceTickFromMarkPrice(decimal markPrice, int quantityDecimals)
    {
        int maxDecimals = MaxPriceDecimals - quantityDecimals;

        // Without a price there is no magnitude to measure, so only the decimals rule is left.
        if (markPrice <= 0)
            return TickSizeFromDecimals(Math.Max(0, maxDecimals));

        // Power of ten of the first significant digit: 78270 -> 4, 7.392 -> 0, 0.85497 -> -1.
        int exponent = 0;
        decimal value = markPrice;
        while (value >= 10m)
        {
            value /= 10m;
            exponent++;
        }
        while (value < 1m)
        {
            value *= 10m;
            exponent--;
        }

        // Five significant figures: the fifth one sits four positions behind the first.
        int decimals = 4 - exponent;
        if (decimals > maxDecimals)
            decimals = maxDecimals;
        if (decimals < 0)
            decimals = 0;
        return TickSizeFromDecimals(decimals);
    }


    /// <summary>
    /// Adds the markets that outside parties deployed on HyperLiquid. Answers with the number of
    /// markets that had no volume, which the caller adds to its own count.
    /// <para>
    /// HyperLiquid lets an outside party run its own perpetual market on its infrastructure, and
    /// names every market after the party that deployed it: the gold of the party calling itself XYZ
    /// is "xyz:GOLD". There were ten of those on 27-08-2026, of which XYZ is by far the largest with
    /// 101 traded markets against 176 in HyperLiquid's own market.
    /// </para>
    /// <para>
    /// The deployer is the product of these symbols, so "xyz:GOLD" becomes GOLDUSDC.XYZ. That is not
    /// decoration: HyENA runs a BTC of its own ("hyna:BTC"), and without the product both it and the
    /// BTC of HyperLiquid's own market would be called BTCUSDC.
    /// </para>
    /// </summary>
    private async Task<int> AddDeployedMarketsAsync(
        Model.CryptoExchange exchange,
        HyperLiquidRestClient client,
        CryptoDatabase database,
        SqliteTransaction transaction,
        List<CryptoSymbol> cache,
        SortedList<string, CryptoSymbol> activeSymbols)
    {
        int withoutVolume = 0;

        var dexList = await client.FuturesApi.ExchangeData.GetPerpDexesAsync(ExchangeBase.CancellationToken);
        if (!dexList.Success || dexList.Data == null)
        {
            // Abort the whole refresh instead of returning. Returning leaves activeSymbols without a
            // single symbol of a deployed market, after which the caller's deactivation loop
            // switches every one of them off over a hiccup at HyperLiquid. That is not theory: on
            // 28-08-2026 at 19:40 this call was cut short by the rate limiter and 47 symbols across
            // HYNA, IO, MKTS and PARA went to status 0 in one go, while the markets themselves were
            // perfectly alive. The transaction is rolled back by the caller, so this cycle writes
            // nothing at all and the next one tries again.
            throw new ExchangeException($"error getting the deployed markets {dexList.Error}");
        }

        foreach (var dex in dexList.Data)
        {
            // The first entry of the list is HyperLiquid's own market and has no name. That one was
            // already handled by the caller, over the package call that does carry ticker data.
            if (dex == null || string.IsNullOrEmpty(dex.Name))
                continue;

            var (success, markets, rawJson) = await PerpDexClient.GetMarketsAsync(dex.Name);

            // One file per deployed market, next to the symbols.json of HyperLiquid's own market.
            // Without it these instruments are nowhere on disk and a name like HYNA1000PEPEUSDC
            // cannot be traced back to the "hyna:1000PEPE" it came from.
            SaveExchangeInfo(rawJson, $"symbols.{dex.Name}.json");

            // Same reasoning as above, now for one deployed market: an answer that did not arrive
            // says nothing about which instruments this market has, so it may not end up as an
            // empty list that deactivates all of them.
            if (!success)
                throw new ExchangeException($"error getting the instruments of the '{dex.Name}' market");

            // An answer that DID arrive and holds nothing is an ordinary answer - four of the ten
            // deployed markets have no instruments at all today - and anything this market had
            // before is deactivated by the caller, exactly like a delisted market.
            if (markets.Count == 0)
                continue;

            foreach (PerpDexMarket market in markets)
            {
                // A delisted market is not stored. It disappears from activeSymbols and the caller
                // deactivates it below, the same route a delisted market of the own market takes.
                if (market.IsDelisted)
                    continue;

                // "xyz:GOLD" splits into the deployer and the market. Everything HyperLiquid offers
                // here settles in USDC, exactly like its own market.
                string[] parts = market.Name.Split(':', 2);
                if (parts.Length != 2 || parts[1].Length == 0)
                    continue;

                // The deployer is the product, so "hyna:BTC" becomes BTCUSDC.HYNA. The base stays the
                // coin itself: putting the deployer in front of it made {BASE} in the links read
                // HYNABTC, which neither TradingView nor Altrady knows.
                SymbolInfo info = ParseSymbol(market.Name, parts[1].ToUpper(), "USDC", parts[0]);
                if (!IsSymbolAccepted(exchange, info, client.FuturesApi, TradingMode.PerpetualLinear, out CryptoSymbol? symbol))
                    continue;

                // The same limits as HyperLiquid's own market - these markets run on the same
                // infrastructure, the same account and the same USDC as margin, and their meta carries
                // szDecimals and maxLeverage and no quantity limits either.
                HyperLiquidOrderLimits.ApplyLimits(symbol, market.QuantityDecimals, market.MaxLeverage);
                if (market.MarkPrice > 0)
                    symbol.PriceTickSize = PriceTickFromMarkPrice(market.MarkPrice, market.QuantityDecimals);
                // See the same call for HyperLiquid's own market above.
                symbol.DeriveDecimalsFromTickSizes();

                if (market.DayVolume > 0)
                    symbol.Volume = (double)market.DayVolume;
                else
                {
                    symbol.Volume = 0;
                    withoutVolume++;
                }

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

        return withoutVolume;
    }
}
