using Alpaca.Markets;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Exchange.Alpaca.Spot;

public class Symbol() : SymbolBase(), ISymbol
{
    public async Task GetSymbolsAsync()
    {
        if (!GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            return;

        try
        {
            // ListAssetsAsync is on IAlpacaTradingClient, not IAlpacaDataClient
            using IAlpacaTradingClient tradingClient = Environments.Paper.GetAlpacaTradingClient(
                new SecretKey(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret));

            using CryptoDatabase database = new();
            database.Open();

            GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
            LimitRate.WaitForFairWeight(1);

            // Fetch all active tradable US equity assets
            var assetsRequest = new AssetsRequest
            {
                AssetStatus = AssetStatus.Active,
                AssetClass = AssetClass.UsEquity,
            };
            var assets = await tradingClient.ListAssetsAsync(assetsRequest);

            if (assets == null)
                throw new ExchangeException("No asset data received");
            SaveExchangeInfo(assets, "symbols.json");


            // Track which symbols are still active, to deactivate delisted ones
            SortedList<string, CryptoSymbol> activeSymbols = [];

            using (var transaction = database.BeginTransaction())
            {
                List<CryptoSymbol> cache = [];
                try
                {
                    foreach (var asset in assets)
                    {
                        if (!asset.IsTradable)
                            continue;

                        // Use the ticker as both the exchange symbol and as base, USD as quote.
                        // This mirrors how HyperLiquid handles single-asset instruments.
                        SymbolInfo info = ParseSymbol(asset.Symbol, asset.Symbol, "USD");
                        // api parameter not applicable for Alpaca (no CryptoExchange.Net client)
#pragma warning disable CS8625
                        if (IsSymbolAccepted(exchange, info, null, global::CryptoExchange.Net.SharedApis.TradingMode.Spot, out CryptoSymbol? symbol))
#pragma warning restore CS8625
                        {
                            symbol!.QuantityTickSize = 0.000001m;  // Alpaca supports fractional shares
                            symbol.QuantityMinimum = 0.000001m;
                            symbol.QuantityMaximum = 0;            // No hard maximum
                            symbol.PriceTickSize = 0.01m;          // Cent precision for most stocks

                            symbol.Status = asset.IsTradable ? 1 : 0;

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

                    // Deactivate symbols that are no longer offered
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

            // Run in the background so symbol loading does not block startup.
            // Volume will populate gradually while the scanner is already running.
            _ = Task.Run(async () => await FetchSnapshotsAsync(activeSymbols));
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }


    /// <summary>
    /// Fetch snapshots in batches to populate initial Volume (quote) and LastPrice for each symbol.
    /// Uses IAlpacaDataClient.ListSnapshotsAsync with LatestMarketDataListRequest.
    /// </summary>
    private static async Task FetchSnapshotsAsync(SortedList<string, CryptoSymbol> symbols)
    {
        if (symbols.Count == 0)
            return;

        GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} fetching volume snapshots for {symbols.Count} symbols");

        using IAlpacaDataClient dataClient = Environments.Paper.GetAlpacaDataClient(
            new SecretKey(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret));

        var symbolNames = symbols.Keys.ToList();
        const int batchSize = 100;
        int fetched = 0;

        for (int i = 0; i < symbolNames.Count; i += batchSize)
        {
            var batch = symbolNames.GetRange(i, Math.Min(batchSize, symbolNames.Count - i));
            LimitRate.WaitForFairWeight(1);
            try
            {
                var request = new LatestMarketDataListRequest(batch);
                var snapshots = await dataClient.ListSnapshotsAsync(request, ExchangeBase.CancellationToken);
                foreach (var (name, snapshot) in snapshots)
                {
                    if (!symbols.TryGetValue(name, out CryptoSymbol? symbol))
                        continue;

                    // Use the latest trade price; fall back to daily bar close when the
                    // market is closed and no recent trade is available.
                    IBar? bar = snapshot.CurrentDailyBar ?? snapshot.PreviousDailyBar;
                    symbol.LastPrice = snapshot.Trade?.Price ?? bar?.Close;

                    if (bar != null)
                    {
                        // Prefer last price; fall back to VWAP so volume is never zero
                        // just because the market happens to be closed right now.
                        decimal price = symbol.LastPrice ?? bar.Vwap;
                        if (price > 0)
                            symbol.Volume = (double)(bar.Volume * price);
                    }
                    fetched++;
                }
            }
            catch (Exception ex)
            {
                GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} snapshot batch {i / batchSize + 1} error: {ex.Message}");
            }
        }

        GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} volume snapshots received for {fetched} symbols");
    }
}
