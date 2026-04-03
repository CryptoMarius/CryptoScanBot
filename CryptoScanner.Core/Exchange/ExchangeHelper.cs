using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Exchange;

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
                    return new BybitEu.Futures.Api();
            //case CryptoExchangeType.Kraken:
            //    if (exchange.TradingType == CryptoTradingType.Spot)
            //        return new Kraken.Spot.Api();
            //    else
            //        throw new Exception("Kraken Futures not supported");
            case CryptoExchangeType.Kucoin:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Kucoin.Spot.Api();
                else
                    return new Kucoin.Futures.Api();
            case CryptoExchangeType.Mexc:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Mexc.Spot.Api();
                else
                    throw new Exception("Mexc Futures not supported");
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
            case CryptoExchangeType.HyperLiquid:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new HyperLiquid.Spot.Api();
                else
                    return new HyperLiquid.Futures.Api();
            case CryptoExchangeType.BitMart:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new BitMart.Spot.Api();
                else
                    return new BitMart.Futures.Api();
            case CryptoExchangeType.Alpaca:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Alpaca.Spot.Api();
                else
                    throw new Exception("Alpaca Futures not supported");
            case CryptoExchangeType.Bitvavo:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Bitvavo.Spot.Api();
                else
                    throw new Exception("Bitvavo Futures not supported");
            default:
                throw new Exception("Exchange not supported");
        }
    }

    public static bool IsIntervalSupported(this Model.CryptoExchange exchange, CryptoIntervalPeriod intervalPeriod)
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
                    return BybitEu.Futures.Interval.GetExchangeInterval(intervalPeriod) != null;
            //case CryptoExchangeType.Kraken:
            //    if (exchange.TradingType == CryptoTradingType.Spot)
            //        return Kraken.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
            //    else
            //        return false;
            case CryptoExchangeType.Kucoin:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Kucoin.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return Kucoin.Futures.Interval.GetExchangeInterval(intervalPeriod) != null;
            case CryptoExchangeType.Mexc:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return Mexc.Spot.Interval.GetExchangeInterval(intervalPeriod) != null;
                else
                    return false;
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
}



