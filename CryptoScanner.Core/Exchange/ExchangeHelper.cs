using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Exchange;

public static class Helper
{
    /// <summary>
    /// The api of an exchange. The switch that used to be here moved to
    /// CryptoScanner.Exchanges (ExchangeProvider), because that is where the concrete
    /// implementations live. See <see cref="ExchangeRegistry"/> for the how and why.
    /// </summary>
    public static ExchangeBase GetApiInstance(this Model.CryptoExchange exchange)
        => ExchangeRegistry.ApiFactory(exchange);

    public static bool IsIntervalSupported(this Model.CryptoExchange exchange, CryptoIntervalPeriod intervalPeriod)
        => ExchangeRegistry.IntervalSupported(exchange, intervalPeriod);
}
