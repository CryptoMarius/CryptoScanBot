using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Exchange;

public static class Helper
{
    public static ExchangeBase GetApiInstance(this Model.CryptoExchange exchange)
    {
        switch (exchange.ExchangeType)
        {
            case CryptoExchangeType.Binance:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Binance.Spot.Api();
                else
                    return new Binance.Futures.Api();
            case CryptoExchangeType.Bybit:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new BybitApi.Spot.Api();
                else
                    return new BybitApi.Futures.Api();
            case CryptoExchangeType.Kraken:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Kraken.Spot.Api();
                else
                    throw new Exception("Niet ondersteunde exchange");
            case CryptoExchangeType.Kucoin:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Kucoin.Spot.Api();
                else
                    throw new Exception("Niet ondersteunde exchange");
            case CryptoExchangeType.Mexc:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Mexc.Spot.Api();
                else
                    throw new Exception("Niet ondersteunde exchange");
            default:
                throw new Exception("Niet ondersteunde exchange");
        }
    }

    public static bool IsIntervalSupported(this Model.CryptoExchange exchange, CryptoIntervalPeriod interval)
    {
        switch (exchange.ExchangeType)
        {
            case CryptoExchangeType.Binance:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Binance.Spot.Interval.GetExchangeInterval(interval) != null;
                else
                    return Binance.Futures.Interval.GetExchangeInterval(interval) != null;
            case CryptoExchangeType.Bybit:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return BybitApi.Spot.Interval.GetExchangeInterval(interval) != null;
                else
                    return BybitApi.Futures.Interval.GetExchangeInterval(interval) != null;
            case CryptoExchangeType.Kraken:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Kraken.Spot.Interval.GetExchangeInterval(interval) != null;
                else
                    return false;
            case CryptoExchangeType.Kucoin:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Kucoin.Spot.Interval.GetExchangeInterval(interval) != null;
                else
                    return false;
            case CryptoExchangeType.Mexc:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Mexc.Spot.Interval.GetExchangeInterval(interval) != null;
                else
                    return false;
            default:
                return false;
        }
    }
}



