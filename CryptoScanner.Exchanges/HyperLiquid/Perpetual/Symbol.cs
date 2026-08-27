using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using HyperLiquid.Net.Clients;

using Microsoft.Data.Sqlite;

using System.Text;

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
                LimitRate.WaitForFairWeight(LimitRate.InfoRequestWeight);
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
                            SymbolInfo info = ParseSymbol(tickerData.Symbol, tickerData.Symbol, "USDC");
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
                            SymbolInfo info = ParseSymbol(symbolData.Name, symbolData.Name, "USDC");
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
                                symbol!.QuantityTickSize = TickSizeFromDecimals(symbolData.QuantityDecimals);

                                //symbol.QuantityMinimum = symbolInfo.LotSizeFilter?.MinOrderQuantity ?? 0;
                                //symbol.QuantityMaximum = symbolInfo.LotSizeFilter?.MaxOrderQuantity ?? 0;

                                //symbol.QuoteValueMinimum = symbolInfo.LotSizeFilter?.MinOrderValue ?? 0;
                                //symbol.QuoteValueMaximum = symbolInfo.LotSizeFilter?.MaxOrderValue ?? 0;


                                // The minimum and maximum price for an order (in base price)
                                // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                // (which has consequences for the Clamp, which does expect values)
                                //symbol.PriceMinimum = symbolInfo.LotSizeFilter.MinOrderValue;
                                //symbol.PriceMaximum = symbolInfo.LotSizeFilter.MaxOrderValue;

                                symbol.PriceTickSize = PriceTickFromMarkPrice(tickerData.MarkPrice);

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
    /// Derives a price tick from a mark price: 4581.4 becomes 0000.1, so the tick carries as many
    /// decimals as the price itself. HyperLiquid publishes no tick size of its own, and the number
    /// of decimals it does publish (szDecimals) is about the QUANTITY, not the price.
    /// </summary>
    private static decimal PriceTickFromMarkPrice(decimal markPrice)
    {
        string x = markPrice.ToString();
        StringBuilder sb = new(x);
        for (int j = 0; j < x.Length; j++)
        {
            if (sb[j] != '.')
                sb[j] = '0';
        }
        if (sb[x.Length - 1] != '.')
            sb[x.Length - 1] = '1';
        x = sb.ToString();
        return Convert.ToDecimal(x);
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
    /// The scanner name carries the deployer, "xyz:GOLD" becoming XYZGOLDUSDC, and that is not
    /// decoration: HyENA runs a BTC of its own ("hyna:BTC"), which would take the scanner name of
    /// the BTC in HyperLiquid's own market and overwrite its instrument name. SubMarket holds the
    /// bare deployer name for the symbol list to show.
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

        LimitRate.WaitForFairWeight(LimitRate.InfoRequestWeight);
        var dexList = await client.FuturesApi.ExchangeData.GetPerpDexesAsync(ExchangeBase.CancellationToken);
        if (!dexList.Success || dexList.Data == null)
        {
            GlobalData.AddErrorToLogTab($"error getting the deployed markets {dexList.Error}");
            return withoutVolume;
        }

        foreach (var dex in dexList.Data)
        {
            // The first entry of the list is HyperLiquid's own market and has no name. That one was
            // already handled by the caller, over the package call that does carry ticker data.
            if (dex == null || string.IsNullOrEmpty(dex.Name))
                continue;

            LimitRate.WaitForFairWeight(LimitRate.InfoRequestWeight);
            var markets = await PerpDexClient.GetMarketsAsync(dex.Name);
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

                SymbolInfo info = ParseSymbol(market.Name, parts[0].ToUpper() + parts[1].ToUpper(), "USDC");
                if (!IsSymbolAccepted(exchange, info, client.FuturesApi, TradingMode.PerpetualLinear, out CryptoSymbol? symbol))
                    continue;

                symbol!.SubMarket = dex.Name;

                // QuantityDecimals is szDecimals, a NUMBER of decimals and not a tick size
                symbol.QuantityTickSize = TickSizeFromDecimals(market.QuantityDecimals);
                if (market.MarkPrice > 0)
                    symbol.PriceTickSize = PriceTickFromMarkPrice(market.MarkPrice);

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
