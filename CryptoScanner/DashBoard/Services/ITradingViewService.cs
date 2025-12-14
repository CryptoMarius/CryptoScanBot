namespace CryptoScanner.DashBoard.Services;

public interface ITradingViewService
{
    // Events for each symbol
    event EventHandler<decimal>? MarketCapTotalChanged;
    event EventHandler<decimal>? DollarIndexChanged;
    event EventHandler<decimal>? Spx500Changed;
    event EventHandler<decimal>? BitcoinDominanceChanged;
    event EventHandler<decimal>? FearAndGreedIndexChanged;

    // Events for crypto symbols
    event EventHandler<decimal>? BtcUsdtChanged;
    event EventHandler<decimal>? EthUsdtChanged;
    event EventHandler<decimal>? BnbUsdtChanged;
    event EventHandler<decimal>? SolUsdtChanged;
    event EventHandler<decimal>? XrpUsdtChanged;

    // Current values - market indicators
    decimal? MarketCapTotalValue { get; }
    decimal? DollarIndexValue { get; }
    decimal? Spx500Value { get; }
    decimal? BitcoinDominanceValue { get; }
    decimal? FearAndGreedIndexValue { get; }

    // Current values - crypto symbols
    decimal? BtcUsdtPriceValue { get; }
    decimal? EthUsdtPriceValue { get; }
    decimal? BnbUsdtPriceValue { get; }
    decimal? SolUsdtPriceValue { get; }
    decimal? XrpUsdtPriceValue { get; }

    // Current values - crypto symbols
    decimal? BtcUsdtVolumeValue { get; }
    decimal? EthUsdtVolumeValue { get; }
    decimal? BnbUsdtVolumeValue { get; }
    decimal? SolUsdtVolumeValue { get; }
    decimal? XrpUsdtVolumeValue { get; }

    void Start();
    void Stop();
}
