using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using System.Globalization;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot;

public class Symbol() : SymbolBase(), ISymbol
{
    public async Task GetSymbolsAsync()
    {
        if (!GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            return;

        try
        {
            using var client = new BitvavoRestClient();

            using CryptoDatabase database = new();
            database.Open();

            // Tickers for the 24h volume
            GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
            // The 24 hour ticker for every market at once costs 25 weight at Bitvavo, not the 1 of a
            // single candle or market request.
            LimitRate.WaitForFairWeight(25);
            var (tickers, tickersJson) = await client.GetTickersAsync();
            SortedList<string, decimal> volumeTicker = [];
            if (tickers != null)
            {
                SaveExchangeInfo(tickersJson, "tickers.json");
                foreach (var ticker in tickers)
                {
                    if (decimal.TryParse(ticker.VolumeQuote, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal vol))
                        volumeTicker[ticker.Market] = vol;
                }
            }

            // Without the tickers every symbol would end up with a volume of 0, drop below the
            // minimum volume and have its candles and subscriptions released. Stop instead, the
            // next refresh cycle will try again.
            if (volumeTicker.Count == 0)
                throw new ExchangeException("No ticker data received");

            GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
            LimitRate.WaitForFairWeight(1);

            var (markets, marketsJson) = await client.GetMarketsAsync();
            if (markets == null)
                throw new ExchangeException("No market data received from Bitvavo");
            SaveExchangeInfo(marketsJson, "symbols.json");


            // Track active symbols to deactivate delisted ones afterwards
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
                    foreach (var market in markets)
                    {
                        // Market format on Bitvavo: "BTC-EUR" (base-quote with dash)
                        // ParseSymbol(exchangeSymbol, base, quote)
                        SymbolInfo info = ParseSymbol(market.Market, market.Base, market.Quote, ProductOfExchange(exchange));

#pragma warning disable CS8625
                        if (IsSymbolAccepted(exchange, info, null, TradingMode.Spot, out CryptoSymbol? symbol))
#pragma warning restore CS8625
                        {
                            symbol!.QuantityMinimum = decimal.TryParse(market.MinOrderInBaseAsset,
                                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal qMin) ? qMin : 0;
                            symbol.QuantityMaximum = 0;  // No hard maximum on Bitvavo
                            // quantityDecimals states the step directly. Deriving it from
                            // minOrderInBaseAsset gives the same answer on every market today, but only
                            // because Bitvavo formats that amount with exactly that many decimals - it
                            // is a coincidence, not a rule.
                            if (market.QuantityDecimals.HasValue)
                                symbol.QuantityTickSize = GetTickSizeFromDecimals(market.QuantityDecimals.Value);
                            else
                                symbol.QuantityTickSize = GetTickSizeFromString(market.MinOrderInBaseAsset);

                            // Bitvavo refuses an order worth less than this (5 EUR on every market at
                            // the moment). Without it the minimum entry value of a position only knows
                            // about the settings, and the exchange rejects what it lets through.
                            symbol.QuoteValueMinimum = decimal.TryParse(market.MinOrderInQuoteAsset,
                                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal quoteMin) ? quoteMin : 0;

                            // The price step comes from tickSize. The two fallbacks below are dead weight
                            // in practice: pricePrecision is null on every market, and minOrderInQuoteAsset
                            // is "5.00" for all of them - which is how every symbol ended up with a tick
                            // size of 0.01. That rounds the candles of everything under 1 EUR into a coarse
                            // grid and the candles of everything under 0.01 EUR (PEPE, SHIB, BONK) into
                            // zeros, since a candle stores its prices as whole multiples of the tick size.
                            if (decimal.TryParse(market.TickSize, NumberStyles.Any,
                                CultureInfo.InvariantCulture, out decimal priceTickSize) && priceTickSize > 0)
                                symbol.PriceTickSize = priceTickSize;
                            else if (market.PricePrecision > 0)
                                symbol.PriceTickSize = (decimal)Math.Pow(10, -market.PricePrecision.GetValueOrDefault());
                            else
                                symbol.PriceTickSize = GetTickSizeFromString(market.MinOrderInQuoteAsset);

                            // Volume from the tickers (market format on Bitvavo: "BTC-EUR")
                            if (volumeTicker.TryGetValue(market.Market, out decimal volume))
                                symbol.Volume = (double)volume;
                            else
                            {
                                symbol.Volume = 0;
                                withoutVolume++;
                            }

                            symbol.Status = market.Status == "trading" ? 1 : 0;

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

                    // Deactivate symbols no longer listed
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

                    foreach (CryptoSymbol symbol in cache)
                        GlobalData.AddSymbol(symbol);
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

    // Turns a number of decimals into a tick size (3 -> 0.001). Multiplying stays exact in decimal,
    // where the Math.Pow detour would go through a double first.
    // Moved to SymbolBase on 17-08-2026, because Kraken and HyperLiquid state their precision the
    // same way and were writing the number of decimals into the tick size field unconverted.
    private static decimal GetTickSizeFromDecimals(int decimals) => TickSizeFromDecimals(decimals);

    // Derives a tick size from a minimum order string by counting significant decimal places.
    // E.g. "0.001" -> 0.001, "5" -> 1, "" -> 1 (default).
    private static decimal GetTickSizeFromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 1m;

        int dotIndex = value.IndexOf('.');
        if (dotIndex < 0)
            return 1m;

        int decimals = value.Length - dotIndex - 1;

        return decimals > 0 ? (decimal)Math.Pow(10, -decimals) : 1m;
    }
}
