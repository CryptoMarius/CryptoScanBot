using Alpaca.Markets;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

// Both the SDK and the scanner have an IAsset, and they mean something completely different
using AlpacaAsset = Alpaca.Markets.IAsset;

namespace CryptoScanner.Core.Exchange.Alpaca.Spot;

public class Symbol() : SymbolBase(), ISymbol
{
    /// <summary>
    /// A stock the scanner is considering, with everything needed to rank it against the others.
    /// </summary>
    private class Candidate
    {
        public required AlpacaAsset Asset { get; init; }
        public decimal? LastPrice { get; set; }
        public double QuoteVolume { get; set; }
    }


    public async Task GetSymbolsAsync()
    {
        if (!GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            return;

        // Alpaca needs a key for its market data as well, not just for trading. Say so once instead of
        // letting the SDK throw somewhere further down with a message that explains nothing.
        if (GlobalData.TradingApi.Key == "" || GlobalData.TradingApi.Secret == "")
        {
            GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} needs an API key and secret " +
                $"(register a free account at alpaca.markets and enter the paper trading key)");
            return;
        }

        try
        {
            SecretKey secretKey = new(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);

            // ListAssetsAsync is on IAlpacaTradingClient, the market data is on IAlpacaDataClient
            using IAlpacaTradingClient tradingClient = Environments.Paper.GetAlpacaTradingClient(secretKey);
            using IAlpacaDataClient dataClient = Environments.Paper.GetAlpacaDataClient(secretKey);

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
            var assets = await tradingClient.ListAssetsAsync(assetsRequest, ExchangeBase.CancellationToken);

            if (assets == null)
                throw new ExchangeException("No asset data received");
            SaveExchangeInfo(assets, "symbols.json");

            Dictionary<string, AlpacaAsset> tradable = [];
            foreach (var asset in assets)
            {
                if (asset.IsTradable)
                    tradable[asset.Symbol] = asset;
            }
            GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} {tradable.Count} tradable assets");


            // Which of those do we follow? (the free plan cannot carry all of them)
            List<Candidate> wanted = await DetermineSymbolsAsync(dataClient, tradable, exchange);
            if (wanted.Count == 0)
            {
                GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} no symbols selected, " +
                    $"leaving the current selection alone");
                return;
            }


            // Track which symbols are still active, to deactivate the ones we no longer follow
            SortedList<string, CryptoSymbol> activeSymbols = [];

            using (var transaction = database.BeginTransaction())
            {
                List<CryptoSymbol> cache = [];
                try
                {
                    foreach (Candidate candidate in wanted)
                    {
                        AlpacaAsset asset = candidate.Asset;

                        // Use the ticker as both the exchange symbol and as base, USD as quote.
                        // This mirrors how HyperLiquid handles single-asset instruments.
                        SymbolInfo info = ParseSymbol(asset.Symbol, asset.Symbol, "USD", ProductOfExchange(exchange));
                        // api parameter not applicable for Alpaca (no CryptoExchange.Net client)
#pragma warning disable CS8625
                        if (IsSymbolAccepted(exchange, info, null, global::CryptoExchange.Net.SharedApis.TradingMode.Spot, out CryptoSymbol? symbol))
#pragma warning restore CS8625
                        {
                            // The asset states its own steps. The fallbacks are the values that apply to
                            // the overwhelming majority: a cent for the price, and a millionth of a share
                            // for the quantity because Alpaca supports fractional shares.
                            symbol!.PriceTickSize = PositiveOrDefault(asset.PriceIncrement, 0.01m);
                            symbol.QuantityTickSize = PositiveOrDefault(asset.MinTradeIncrement, asset.Fractionable ? 0.000001m : 1m);
                            symbol.QuantityMinimum = PositiveOrDefault(asset.MinOrderSize, 0m);
                            symbol.QuantityMaximum = 0;            // No hard maximum

                            // Both are known before the symbol is written, so they survive a restart. The
                            // volume decides whether candles are fetched and whether a subscription is
                            // made (CandleBase.UpdateVolumeDecisions runs right after this method), and
                            // that decision cannot wait for a background task filling it in afterwards.
                            symbol.LastPrice = candidate.LastPrice;
                            symbol.Volume = candidate.QuoteVolume;

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

                    // Deactivate the symbols we no longer follow (delisted, or pushed out of the
                    // selection by a stock that is more active today)
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

            GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} following {activeSymbols.Count} symbols: " +
                $"{string.Join(',', activeSymbols.Keys)}");

            exchange.LastTimeFetched = DateTime.UtcNow;
            database.Connection.Update(exchange);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }


    /// <summary>
    /// The stocks the scanner follows, at most <see cref="Api.MaxSymbols"/> of them.
    ///
    /// Alpaca offers roughly 11.000 tradable US equities and the free plan carries 30 of them on its
    /// single data stream, so the selection has to be made here. The screener endpoint answers the
    /// question "what is being traded today" in one request, but it ranks on the NUMBER of shares,
    /// which puts a two dollar stock above a large cap trading ten times as much money. So we ask for
    /// a wider list and rank it ourselves on the amount of money that changed hands - the same measure
    /// the other exchanges use for their 24 hour volume.
    ///
    /// Returns an empty list when Alpaca says nothing at all, so a hiccup over there does not empty
    /// out the whole exchange in the database. The same goes for a volume that only half arrived:
    /// this list is a ranking, and a ranking on half the measurements is not a smaller answer but a
    /// different one.
    /// </summary>
    private static async Task<List<Candidate>> DetermineSymbolsAsync(IAlpacaDataClient dataClient,
        Dictionary<string, AlpacaAsset> tradable, Model.CryptoExchange exchange)
    {
        Dictionary<string, Candidate> candidates = [];

        void AddCandidate(string name)
        {
            if (tradable.TryGetValue(name, out AlpacaAsset? asset))
                candidates.TryAdd(name, new Candidate { Asset = asset });
        }

        try
        {
            LimitRate.WaitForFairWeight(1);
            var actives = await dataClient.ListMostActiveStocksByVolumeAsync(4 * Api.MaxSymbols, ExchangeBase.CancellationToken);
            foreach (var active in actives)
                AddCandidate(active.Symbol);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddErrorToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} error reading the most active stocks {error.Message}");
        }

        // Whatever the screener says, the instrument the pause rules watch has to be in the list
        AddCandidate(PauseSymbolTicker());

        // Nothing? Then stay with the choice of the previous cycle instead of switching everything off
        if (candidates.Count == 0)
        {
            foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
            {
                if (symbol.Status == 1 && !symbol.IsBarometerSymbol())
                    AddCandidate(symbol.ExchangeName);
            }
            if (candidates.Count > 0)
                GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} keeping the previous selection of {candidates.Count} symbols");
        }

        if (candidates.Count == 0)
            return [];

        // A batch that did not arrive leaves its candidates on a volume of 0, and the ranking below
        // then reads that as "hardly traded" instead of "not measured". With 100 symbols per batch
        // and four times MaxSymbols candidates that is up to a hundred instruments sinking to the
        // bottom at once, after which the cut on MaxSymbols hands back a selection built on the
        // batches that did arrive - and everything it dropped is deactivated by the caller. Rather
        // hand back nothing, which the caller already reads as "leave the current selection alone".
        // The caller says what happens next, so this only states why
        if (!await FetchSnapshotsAsync(dataClient, candidates))
        {
            GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} not every volume snapshot arrived, " +
                $"so the ranking cannot be trusted");
            return [];
        }

        // The most money traded wins. A candidate without a snapshot has a volume of 0 and ends up at
        // the bottom, which is where an instrument we know nothing about belongs.
        List<Candidate> result = [.. candidates.Values.OrderByDescending(x => x.QuoteVolume)];
        if (result.Count > Api.MaxSymbols)
            result.RemoveRange(Api.MaxSymbols, result.Count - Api.MaxSymbols);
        return result;
    }


    /// <summary>
    /// The ticker of the instrument the pause rules watch. The option states the scanner name
    /// ("SPYUSD"), the exchange knows it as the plain ticker.
    /// </summary>
    private static string PauseSymbolTicker()
    {
        string name = ExchangeBase.ExchangeOptions.PauseSymbol;
        string quote = ExchangeBase.ExchangeOptions.DefaultQuote ?? "";
        if (quote != "" && name.EndsWith(quote, StringComparison.OrdinalIgnoreCase))
            return name[..^quote.Length];
        return name;
    }


    /// <summary>
    /// Fill in the last price and the 24 hour volume (in the quote currency, so in dollars) of every
    /// candidate. Uses IAlpacaDataClient.ListSnapshotsAsync, which takes 100 symbols per request.
    /// <para>
    /// Answers whether every batch arrived. A batch that failed is not the same as a batch of
    /// instruments without volume, and the caller ranks on exactly that volume - see the call site
    /// for what an unnoticed failure does to the selection.
    /// </para>
    /// </summary>
    private static async Task<bool> FetchSnapshotsAsync(IAlpacaDataClient dataClient, Dictionary<string, Candidate> candidates)
    {
        List<string> names = [.. candidates.Keys];
        const int batchSize = 100;
        int fetched = 0;
        bool complete = true;

        for (int i = 0; i < names.Count; i += batchSize)
        {
            var batch = names.GetRange(i, Math.Min(batchSize, names.Count - i));
            LimitRate.WaitForFairWeight(1);
            try
            {
                var request = new LatestMarketDataListRequest(batch) { Feed = Api.DataFeed };
                var snapshots = await dataClient.ListSnapshotsAsync(request, ExchangeBase.CancellationToken);
                foreach (var (name, snapshot) in snapshots)
                {
                    if (!candidates.TryGetValue(name, out Candidate? candidate))
                        continue;

                    // Use the latest trade price; fall back to daily bar close when the
                    // market is closed and no recent trade is available.
                    IBar? bar = snapshot.CurrentDailyBar ?? snapshot.PreviousDailyBar;
                    candidate.LastPrice = snapshot.Trade?.Price ?? bar?.Close;

                    if (bar != null)
                    {
                        // The bar states a number of shares, while the scanner works in the quote
                        // currency everywhere else. The VWAP is the average price those shares changed
                        // hands at; the last price is the fallback for a bar without one.
                        decimal price = bar.Vwap > 0 ? bar.Vwap : candidate.LastPrice ?? 0;
                        if (price > 0)
                            candidate.QuoteVolume = (double)(bar.Volume * price);
                    }
                    fetched++;
                }
            }
            catch (Exception error)
            {
                complete = false;
                GlobalData.AddErrorToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} snapshot batch {i / batchSize + 1} error: {error.Message}");
            }
        }

        GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} volume snapshots received for {fetched} symbols");
        return complete;
    }


    /// <summary>
    /// The value the asset states, or the given fallback when it states nothing usable. A step of zero
    /// would round every price or quantity to nothing at all.
    /// </summary>
    private static decimal PositiveOrDefault(decimal? value, decimal fallback)
    {
        return value.HasValue && value.Value > 0 ? value.Value : fallback;
    }
}
