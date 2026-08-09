namespace CryptoScanner.Core.Model;

/// <summary>
/// Hands out one array index per plugin data type, once per process.
/// <para>
/// This is what lets a plugin attach its own per-candle values to <see cref="CryptoData"/> without
/// Core knowing the plugin exists. Before this, every plugin added fields to CryptoData directly
/// (VbsBasis, VbsUpper, …) or squeezed them into a string-keyed dictionary — the first grew the
/// shared class with every new plugin, the second lost type safety and allocated a dictionary for
/// every candle whether it was used or not.
/// </para>
/// </summary>
public static class PluginDataSlots
{
    private static int _count;

    /// <summary>Number of slots handed out so far; the size a full slot array needs.</summary>
    public static int Count => Volatile.Read(ref _count);

    internal static int Reserve() => Interlocked.Increment(ref _count) - 1;
}

/// <summary>
/// The slot index for one plugin data type. The static initialiser runs once per closed generic
/// type, so <c>PluginDataSlot&lt;VbsCandleData&gt;.Index</c> is a constant lookup after the first use.
/// </summary>
public static class PluginDataSlot<T> where T : class
{
    public static readonly int Index = PluginDataSlots.Reserve();
}
