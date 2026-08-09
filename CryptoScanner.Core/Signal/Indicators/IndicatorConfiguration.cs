namespace CryptoScanner.Core.Signal.Indicators;

/// <summary>
/// Version stamp for everything an <see cref="IntervalIndicatorHub"/> reads from the settings at
/// construction time: the Bollinger/RSI/Stochastic parameters of the base set, and which plugins
/// have an enabled strategy (which decides whether their indicator extension runs at all).
/// <para>
/// A hub is created once per symbol+interval and then fed incrementally, so without this stamp a
/// settings change would never reach the hubs that already exist — the scanner would keep using the
/// old RSI length, and a strategy enabled after startup would read null indicator values and
/// silently never signal. <see cref="Bump"/> is called whenever settings are applied;
/// IndicatorData rebuilds any hub whose stamp is behind.
/// </para>
/// </summary>
public static class IndicatorConfiguration
{
    private static int _version;

    /// <summary>The current settings generation. A hub built under an older one is stale.</summary>
    public static int Version => Volatile.Read(ref _version);

    /// <summary>Invalidate every existing hub; call after applying settings.</summary>
    public static void Bump() => Interlocked.Increment(ref _version);
}
