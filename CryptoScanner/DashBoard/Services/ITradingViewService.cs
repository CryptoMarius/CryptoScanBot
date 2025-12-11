namespace CryptoScanner.DashBoard.Services;

public interface ITradingViewService
{
    // Events voor elk symbool
    event EventHandler<decimal>? MarketCapTotalChanged;
    event EventHandler<decimal>? DollarIndexChanged;
    event EventHandler<decimal>? Spx500Changed;
    event EventHandler<decimal>? BitcoinDominanceChanged;
    event EventHandler<decimal>? FearAndGreedIndexChanged;

    // Current values
    decimal? MarketCapTotal { get; }
    decimal? DollarIndex { get; }
    decimal? Spx500 { get; }
    decimal? BitcoinDominance { get; }
    decimal? FearAndGreedIndex { get; }

    void Start();
    void Stop();
}