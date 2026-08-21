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

    public static ExchangeBase GetApiInstance(Model.CryptoExchange exchange)
    {
        switch (exchange.ExchangeType)
        {
            case CryptoExchangeType.Alpaca:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Alpaca.Spot.Api();
                else
                    throw new Exception("Alpaca Futures not supported");
            case CryptoExchangeType.Binance:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Binance.Spot.Api();
                else
                    return new Binance.Futures.Api();
            case CryptoExchangeType.Bitvavo:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Bitvavo.Spot.Api();
                else
                    throw new Exception("Bitvavo Futures not supported");
            case CryptoExchangeType.BitMart:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new BitMart.Spot.Api();
                else
                    return new BitMart.Futures.Api();
            case CryptoExchangeType.BloFin:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    throw new Exception("BloFin Spot not supported");
                else
                    return new BloFin.Futures.Api();
            case CryptoExchangeType.Bybit:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new BybitApi.Spot.Api();
                else
                    return new BybitApi.Futures.Api();
            case CryptoExchangeType.BybitEu:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new BybitEu.Spot.Api();
                else
                    throw new Exception("Bybit EU Futures not supported");
            case CryptoExchangeType.HyperLiquid:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new HyperLiquid.Spot.Api();
                else
                    return new HyperLiquid.Futures.Api();
            case CryptoExchangeType.Kraken:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Kraken.Spot.Api();
                else
                    return new Kraken.Futures.Api();
            case CryptoExchangeType.Kucoin:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Kucoin.Spot.Api();
                else
                    return new Kucoin.Futures.Api();
            case CryptoExchangeType.Mexc:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Mexc.Spot.Api();
                else
                    return new Mexc.Futures.Api();
            case CryptoExchangeType.Okx:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Okx.Spot.Api();
                else
                    return new Okx.Futures.Api();
            case CryptoExchangeType.Coinbase:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Coinbase.Spot.Api();
                else
                    throw new Exception("Coinbase Futures not supported");
            default:
                throw new Exception("Exchange not supported");
        }
    }

    public static bool IsIntervalSupported(Model.CryptoExchange exchange, CryptoIntervalPeriod intervalPeriod)
    {
        switch (exchange.ExchangeType)
        {
            case CryptoExchangeType.Binance:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Binance.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return Binance.Futures.Interval.GetExchangeInterval(intervalPeriod) != null;
            case CryptoExchangeType.BloFin:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    throw new Exception("BloFin Spot not supported");
                else
                    return BloFin.Futures.Interval.GetExchangeInterval(intervalPeriod) != null;
            case CryptoExchangeType.Bybit:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return BybitApi.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return BybitApi.Futures.Interval.GetExchangeInterval(intervalPeriod) != null;
            case CryptoExchangeType.BybitEu:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return BybitEu.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    throw new Exception("Bybit EU Futures not supported");
            case CryptoExchangeType.Kraken:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Kraken.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return Kraken.Futures.Interval.GetExchangeInterval(intervalPeriod) != null;
            case CryptoExchangeType.Kucoin:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Kucoin.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return Kucoin.Futures.Interval.GetExchangeInterval(intervalPeriod) != null;
            case CryptoExchangeType.Mexc:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Mexc.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return Mexc.Futures.Interval.GetExchangeInterval(intervalPeriod) != null;
            case CryptoExchangeType.Okx:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Okx.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return Okx.Futures.Interval.GetExchangeInterval(intervalPeriod) != null;
            case CryptoExchangeType.Coinbase:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Coinbase.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return false;
            case CryptoExchangeType.HyperLiquid:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return HyperLiquid.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return HyperLiquid.Futures.Interval.GetExchangeInterval(intervalPeriod) != null;
            case CryptoExchangeType.BitMart:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return BitMart.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return BitMart.Futures.Interval.GetExchangeInterval(intervalPeriod) != null;
            case CryptoExchangeType.Alpaca:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Alpaca.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return false;
            case CryptoExchangeType.Bitvavo:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Bitvavo.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return false;
            default:
                return false;
        }
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
        list.TryAdd("Binance Futures", Binance.Futures.Api.GetExchangeLinks());

        list.Remove("Bitvavo");
        list.TryAdd("Bitvavo Spot", Bitvavo.Spot.Api.GetExchangeLinks());
        //list.TryAdd("Bitvavo Futures", Bitvavo.Futures.Api.GetExchangeLinks());

        list.Remove("Bybit");
        list.TryAdd("Bybit Spot", BybitApi.Spot.Api.GetExchangeLinks());
        list.TryAdd("Bybit Futures", BybitApi.Futures.Api.GetExchangeLinks());

        list.Remove("Bybit EU");
        list.TryAdd("Bybit EU Spot", BybitEu.Spot.Api.GetExchangeLinks());
        //list.TryAdd("Bybit EU Futures", BybitEu.Futures.Api.GetExchangeLinks());

        list.Remove("BitMart");
        list.TryAdd("BitMart Spot", BitMart.Spot.Api.GetExchangeLinks());
        list.TryAdd("BitMart Futures", BitMart.Futures.Api.GetExchangeLinks());

        list.Remove("BloFin");
        //list.TryAdd("BloFin Spot", BloFin.Spot.Api.GetExchangeLinks());
        list.TryAdd("BloFin Futures", BloFin.Futures.Api.GetExchangeLinks());

        list.Remove("Coinbase");
        list.TryAdd("Coinbase Spot", Coinbase.Spot.Api.GetExchangeLinks());
        // (there is no Coinbase.Futures api, this line used to be a copy of the Bybit EU Futures one)

        list.Remove("HyperLiquid");
        list.TryAdd("HyperLiquid Spot", HyperLiquid.Spot.Api.GetExchangeLinks());
        list.TryAdd("HyperLiquid Futures", HyperLiquid.Futures.Api.GetExchangeLinks());

        list.Remove("Kucoin");
        list.TryAdd("Kucoin Spot", Kucoin.Spot.Api.GetExchangeLinks());
        list.TryAdd("Kucoin Futures", Kucoin.Futures.Api.GetExchangeLinks());

        list.Remove("Kraken");
        list.TryAdd("Kraken Spot", Kraken.Spot.Api.GetExchangeLinks());
        list.TryAdd("Kraken Futures", Kraken.Futures.Api.GetExchangeLinks());

        list.Remove("Mexc");
        list.TryAdd("Mexc Spot", Mexc.Spot.Api.GetExchangeLinks());
        list.TryAdd("Mexc Futures", Mexc.Futures.Api.GetExchangeLinks());

        list.Remove("Okx");
        list.TryAdd("Okx Spot", Okx.Spot.Api.GetExchangeLinks());
        list.TryAdd("Okx Futures", Okx.Futures.Api.GetExchangeLinks());

        list.Remove("Coinbase");
        list.TryAdd("Coinbase Spot", Coinbase.Spot.Api.GetExchangeLinks());
    }
}
