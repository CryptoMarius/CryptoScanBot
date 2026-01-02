namespace CryptoScanner.Services;

public interface ITradingViewService
{
    // Events for each symbol
    event EventHandler<decimal>? MarketCapTotalChanged;
    event EventHandler<decimal>? DollarIndexChanged;
    event EventHandler<decimal>? Spx500Changed;
    event EventHandler<decimal>? BitcoinDominanceChanged;
    event EventHandler<decimal>? FearAndGreedIndexChanged;

    // Events for crypto symbols
    event EventHandler<(decimal Price, decimal Volume)>? BtcUsdtChanged;
    event EventHandler<(decimal Price, decimal Volume)>? EthUsdtChanged;
    event EventHandler<(decimal Price, decimal Volume)>? BnbUsdtChanged;
    event EventHandler<(decimal Price, decimal Volume)>? SolUsdtChanged;
    event EventHandler<(decimal Price, decimal Volume)>? XrpUsdtChanged;

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
