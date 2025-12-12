namespace CryptoScanner.DashBoard.Services;

public interface ITradingViewService
{
    // Events for each symbol
    event EventHandler<decimal>? MarketCapTotalChanged;
    event EventHandler<decimal>? DollarIndexChanged;
    event EventHandler<decimal>? Spx500Changed;
    event EventHandler<decimal>? BitcoinDominanceChanged;
    event EventHandler<decimal>? FearAndGreedIndexChanged;

    // Current values
    decimal? MarketCapTotalValue { get; }
    decimal? DollarIndexValue { get; }
    decimal? Spx500Value { get; }
    decimal? BitcoinDominanceValue { get; }
    decimal? FearAndGreedIndexValue { get; }

    void Start();
    void Stop();
}