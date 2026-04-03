using System.Globalization;

using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

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
            LimitRate.WaitForFairWeight(1);
            var tickers = await client.GetTickersAsync();
            SortedList<string, decimal> volumeTicker = [];
            if (tickers != null)
            {
                SaveExchangeInfo(tickers, "tickers.json");
                foreach (var ticker in tickers)
                {
                    if (decimal.TryParse(ticker.VolumeQuote, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal vol))
                        volumeTicker[ticker.Market] = vol;
                }
            }

            GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
            LimitRate.WaitForFairWeight(1);

            var markets = await client.GetMarketsAsync();
            if (markets == null)
                throw new ExchangeException("No market data received from Bitvavo");
            SaveExchangeInfo(markets, "symbols.json");


            // Track active symbols to deactivate delisted ones afterwards
            SortedList<string, CryptoSymbol> activeSymbols = [];

            using (var transaction = database.BeginTransaction())
            {
                List<CryptoSymbol> cache = [];
                try
                {
                    foreach (var market in markets)
                    {
                        // Market format on Bitvavo: "BTC-EUR" (base-quote with dash)
                        // ParseSymbol(exchangeSymbol, base, quote)
                        SymbolInfo info = ParseSymbol(market.Market, market.Base, market.Quote);

#pragma warning disable CS8625
                        if (IsSymbolAccepted(exchange, info, null, TradingMode.Spot, out CryptoSymbol? symbol))
#pragma warning restore CS8625
                        {
                            symbol!.QuantityMinimum = decimal.TryParse(market.MinOrderInBaseAsset,
                                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal qMin) ? qMin : 0;
                            symbol.QuantityMaximum = 0;  // No hard maximum on Bitvavo
                            symbol.QuantityTickSize = GetTickSizeFromString(market.MinOrderInBaseAsset);

                            symbol.PriceTickSize = market.PricePrecision > 0
                                ? (decimal)Math.Pow(10, -market.PricePrecision.GetValueOrDefault())
                                : GetTickSizeFromString(market.MinOrderInQuoteAsset);

                            // Volume from the tickers (market format on Bitvavo: "BTC-EUR")
                            symbol.Volume = volumeTicker.TryGetValue(market.Market, out decimal volume)
                                ? (double)volume
                                : 0;

                            symbol.Status = market.Status == "trading" ? 1 : 0;

                            if (symbol.Id == 0)
                            {
                                database.Connection.Insert(symbol, transaction);
                                cache.Add(symbol);
                            }
                            else
                                database.Connection.Update(symbol, transaction);

                            activeSymbols.Add(symbol.Name, symbol);
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
