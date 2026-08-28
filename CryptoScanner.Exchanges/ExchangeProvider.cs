using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Core.Exchange;

/// <summary>
/// Everything the core needs from the concrete exchange implementations, in one place.
/// Registered with <see cref="ExchangeRegistry"/> so the core can stay free of a reference to
/// this assembly. The bodies below are the ones that used to sit in ExchangeHelper.cs and in
/// CryptoExternalUrlList.InitializeUrls().
/// </summary>
public static class ExchangeProvider
{
    /// <summary>
    /// Hands the three entry points to the core. Called automatically on first use (the core
    /// loads this assembly by name), but an application may also call it explicitly at startup.
    /// </summary>
    public static void Register()
    {
        ExchangeRegistry.Register(GetApiInstance, IsIntervalSupported, InitializeUrls);
    }

    /// <summary>
    /// The api that serves one market, chosen on the exchange AND the product. Both halves matter:
    /// Okx appears three times below, once per product it offers.
    /// A combination that is missing here is a market that has no implementation - the exchange list
    /// carries those with IsSupported = false, so the user can see the market exists.
    /// </summary>
    public static ExchangeBase GetApiInstance(Model.CryptoExchange exchange)
    {
        return (exchange.ExchangeType, exchange.TradingType) switch
        {
            (CryptoExchangeType.Alpaca, CryptoTradingType.Spot) => new Alpaca.Spot.Api(),

            (CryptoExchangeType.Binance, CryptoTradingType.Spot) => new Binance.Spot.Api(),
            (CryptoExchangeType.Binance, CryptoTradingType.Perpetual) => new Binance.Perpetual.Api(),

            (CryptoExchangeType.BitMart, CryptoTradingType.Spot) => new BitMart.Spot.Api(),
            (CryptoExchangeType.BitMart, CryptoTradingType.Perpetual) => new BitMart.Perpetual.Api(),

            (CryptoExchangeType.Bitvavo, CryptoTradingType.Spot) => new Bitvavo.Spot.Api(),

            (CryptoExchangeType.BloFin, CryptoTradingType.Perpetual) => new BloFin.Perpetual.Api(),

            (CryptoExchangeType.Bybit, CryptoTradingType.Spot) => new BybitApi.Spot.Api(),
            (CryptoExchangeType.Bybit, CryptoTradingType.Perpetual) => new BybitApi.Perpetual.Api(),

            (CryptoExchangeType.BybitEu, CryptoTradingType.Spot) => new BybitEu.Spot.Api(),

            (CryptoExchangeType.Coinbase, CryptoTradingType.Spot) => new Coinbase.Spot.Api(),

            (CryptoExchangeType.HyperLiquid, CryptoTradingType.Spot) => new HyperLiquid.Spot.Api(),
            (CryptoExchangeType.HyperLiquid, CryptoTradingType.Perpetual) => new HyperLiquid.Perpetual.Api(),

            (CryptoExchangeType.Kraken, CryptoTradingType.Spot) => new Kraken.Spot.Api(),
            (CryptoExchangeType.Kraken, CryptoTradingType.Perpetual) => new Kraken.Perpetual.Api(),

            (CryptoExchangeType.Kucoin, CryptoTradingType.Spot) => new Kucoin.Spot.Api(),
            (CryptoExchangeType.Kucoin, CryptoTradingType.Perpetual) => new Kucoin.Perpetual.Api(),

            (CryptoExchangeType.Mexc, CryptoTradingType.Spot) => new Mexc.Spot.Api(),
            (CryptoExchangeType.Mexc, CryptoTradingType.Perpetual) => new Mexc.Perpetual.Api(),

            (CryptoExchangeType.Okx, CryptoTradingType.Spot) => new Okx.Spot.Api(),
            (CryptoExchangeType.Okx, CryptoTradingType.Perpetual) => new Okx.Perpetual.Api(),

            _ => throw new Exception($"{exchange.Name} is not supported"),
        };
    }

    /// <summary>
    /// Whether a market can deliver candles for this interval. False for a market without an
    /// implementation as well - GetApiInstance would throw for it anyway.
    /// </summary>
    public static bool IsIntervalSupported(Model.CryptoExchange exchange, CryptoIntervalPeriod intervalPeriod)
    {
        return (exchange.ExchangeType, exchange.TradingType) switch
        {
            (CryptoExchangeType.Alpaca, CryptoTradingType.Spot) => Alpaca.Spot.Interval.GetExchangeInterval(intervalPeriod) != null,

            (CryptoExchangeType.Binance, CryptoTradingType.Spot) => Binance.Spot.Interval.GetExchangeInterval(intervalPeriod) != null,
            (CryptoExchangeType.Binance, CryptoTradingType.Perpetual) => Binance.Perpetual.Interval.GetExchangeInterval(intervalPeriod) != null,

            (CryptoExchangeType.BitMart, CryptoTradingType.Spot) => BitMart.Spot.Interval.GetExchangeInterval(intervalPeriod) != null,
            (CryptoExchangeType.BitMart, CryptoTradingType.Perpetual) => BitMart.Perpetual.Interval.GetExchangeInterval(intervalPeriod) != null,

            (CryptoExchangeType.Bitvavo, CryptoTradingType.Spot) => Bitvavo.Spot.Interval.GetExchangeInterval(intervalPeriod) != null,

            (CryptoExchangeType.BloFin, CryptoTradingType.Perpetual) => BloFin.Perpetual.Interval.GetExchangeInterval(intervalPeriod) != null,

            (CryptoExchangeType.Bybit, CryptoTradingType.Spot) => BybitApi.Spot.Interval.GetExchangeInterval(intervalPeriod) != null,
            (CryptoExchangeType.Bybit, CryptoTradingType.Perpetual) => BybitApi.Perpetual.Interval.GetExchangeInterval(intervalPeriod) != null,

            (CryptoExchangeType.BybitEu, CryptoTradingType.Spot) => BybitEu.Spot.Interval.GetExchangeInterval(intervalPeriod) != null,

            (CryptoExchangeType.Coinbase, CryptoTradingType.Spot) => Coinbase.Spot.Interval.GetExchangeInterval(intervalPeriod) != null,

            (CryptoExchangeType.HyperLiquid, CryptoTradingType.Spot) => HyperLiquid.Spot.Interval.GetExchangeInterval(intervalPeriod) != null,
            (CryptoExchangeType.HyperLiquid, CryptoTradingType.Perpetual) => HyperLiquid.Perpetual.Interval.GetExchangeInterval(intervalPeriod) != null,

            (CryptoExchangeType.Kraken, CryptoTradingType.Spot) => Kraken.Spot.Interval.GetExchangeInterval(intervalPeriod) != null,
            (CryptoExchangeType.Kraken, CryptoTradingType.Perpetual) => Kraken.Perpetual.Interval.GetExchangeInterval(intervalPeriod) != null,

            (CryptoExchangeType.Kucoin, CryptoTradingType.Spot) => Kucoin.Spot.Interval.GetExchangeInterval(intervalPeriod) != null,
            (CryptoExchangeType.Kucoin, CryptoTradingType.Perpetual) => Kucoin.Perpetual.Interval.GetExchangeInterval(intervalPeriod) != null,

            (CryptoExchangeType.Mexc, CryptoTradingType.Spot) => Mexc.Spot.Interval.GetExchangeInterval(intervalPeriod) != null,
            (CryptoExchangeType.Mexc, CryptoTradingType.Perpetual) => Mexc.Perpetual.Interval.GetExchangeInterval(intervalPeriod) != null,

            (CryptoExchangeType.Okx, CryptoTradingType.Spot) => Okx.Spot.Interval.GetExchangeInterval(intervalPeriod) != null,
            (CryptoExchangeType.Okx, CryptoTradingType.Perpetual) => Okx.Perpetual.Interval.GetExchangeInterval(intervalPeriod) != null,

            _ => false,
        };
    }

    /// <summary>
    /// Defaults for the url's
    /// </summary>
    public static void InitializeUrls(CryptoExternalUrlList list)
    {
        // This can/should be some kind of service..

        // Altrady: Codes on webpage
        // https://support.altrady.com/en/article/valid-values-for-exchange-and-symbol-1xrzfap/
        // TradingView: Codes are in the symbol description (kind of hidden)

        // Registered under the name it has in the database, which is "Alpaca" and not "Alpaca Spot"
        list.TryAdd("Alpaca", Alpaca.Spot.Api.GetExchangeLinks());

        list.Remove("Binance");
        list.TryAdd("Binance Spot", Binance.Spot.Api.GetExchangeLinks());
        list.TryAdd("Binance Perpetual", Binance.Perpetual.Api.GetExchangeLinks());

        list.Remove("Bitvavo");
        list.TryAdd("Bitvavo Spot", Bitvavo.Spot.Api.GetExchangeLinks());
        //list.TryAdd("Bitvavo Perpetual", Bitvavo.Perpetual.Api.GetExchangeLinks());

        list.Remove("Bybit");
        list.TryAdd("Bybit Spot", BybitApi.Spot.Api.GetExchangeLinks());
        list.TryAdd("Bybit Perpetual", BybitApi.Perpetual.Api.GetExchangeLinks());

        list.Remove("Bybit EU");
        list.TryAdd("Bybit EU Spot", BybitEu.Spot.Api.GetExchangeLinks());
        //list.TryAdd("Bybit EU Perpetual", BybitEu.Perpetual.Api.GetExchangeLinks());

        list.Remove("BitMart");
        list.TryAdd("BitMart Spot", BitMart.Spot.Api.GetExchangeLinks());
        list.TryAdd("BitMart Perpetual", BitMart.Perpetual.Api.GetExchangeLinks());

        list.Remove("BloFin");
        //list.TryAdd("BloFin Spot", BloFin.Spot.Api.GetExchangeLinks());
        list.TryAdd("BloFin Perpetual", BloFin.Perpetual.Api.GetExchangeLinks());

        list.Remove("Coinbase");
        list.TryAdd("Coinbase Spot", Coinbase.Spot.Api.GetExchangeLinks());
        // (there is no Coinbase.Perpetual api, this line used to be a copy of the Bybit EU Perpetual one)

        list.Remove("HyperLiquid");
        list.TryAdd("HyperLiquid Spot", HyperLiquid.Spot.Api.GetExchangeLinks());
        list.TryAdd("HyperLiquid Perpetual", HyperLiquid.Perpetual.Api.GetExchangeLinks());

        list.Remove("Kucoin");
        list.TryAdd("Kucoin Spot", Kucoin.Spot.Api.GetExchangeLinks());
        list.TryAdd("Kucoin Perpetual", Kucoin.Perpetual.Api.GetExchangeLinks());

        list.Remove("Kraken");
        list.TryAdd("Kraken Spot", Kraken.Spot.Api.GetExchangeLinks());
        list.TryAdd("Kraken Perpetual", Kraken.Perpetual.Api.GetExchangeLinks());

        list.Remove("Mexc");
        list.TryAdd("Mexc Spot", Mexc.Spot.Api.GetExchangeLinks());
        list.TryAdd("Mexc Perpetual", Mexc.Perpetual.Api.GetExchangeLinks());

        list.Remove("Okx");
        list.TryAdd("Okx Spot", Okx.Spot.Api.GetExchangeLinks());
        list.TryAdd("Okx Perpetual", Okx.Perpetual.Api.GetExchangeLinks());

        list.Remove("Coinbase");
        list.TryAdd("Coinbase Spot", Coinbase.Spot.Api.GetExchangeLinks());
    }
}
