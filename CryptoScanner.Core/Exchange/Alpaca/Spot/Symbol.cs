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
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }
}
