using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

using System.Reflection;

namespace CryptoScanner.Core.Exchange;

/// <summary>
/// The seam between this assembly and CryptoScanner.Exchanges.
///
/// The core needs a concrete exchange in a handful of places (fetching candles, placing orders,
/// the external url list), but the concrete exchanges need the core (Model, GlobalData, the
/// database). Referencing each other is not possible, so the concrete side registers three
/// delegates here and the core only ever calls those.
///
/// CryptoScanner.Exchanges registers itself through ExchangeProvider.Register(). Applications
/// normally do not have to call that: the first use below loads the assembly by name and calls
/// Register() through reflection. The explicit call is still available (and cheaper), and it
/// gives a compile time guarantee that the project is referenced at all.
/// </summary>
public static class ExchangeRegistry
{
    private const string ProviderTypeName = "CryptoScanner.Core.Exchange.ExchangeProvider, CryptoScanner.Exchanges";

    private static readonly object RegistrationLock = new();
    private static bool loadAttempted;

    private static Func<Model.CryptoExchange, ExchangeBase>? apiFactory;
    private static Func<Model.CryptoExchange, CryptoIntervalPeriod, bool>? intervalSupported;
    private static Action<CryptoExternalUrlList>? initializeUrls;

    /// <summary>
    /// Called by CryptoScanner.Exchanges. Registering twice is harmless (the same delegates are
    /// simply stored again), so an application may call ExchangeProvider.Register() at startup
    /// without having to care whether something already triggered the lazy load.
    /// </summary>
    public static void Register(
        Func<Model.CryptoExchange, ExchangeBase> apiFactory,
        Func<Model.CryptoExchange, CryptoIntervalPeriod, bool> intervalSupported,
        Action<CryptoExternalUrlList> initializeUrls)
    {
        lock (RegistrationLock)
        {
            ExchangeRegistry.apiFactory = apiFactory;
            ExchangeRegistry.intervalSupported = intervalSupported;
            ExchangeRegistry.initializeUrls = initializeUrls;
            loadAttempted = true;
        }
    }

    /// <summary>
    /// True once the exchange implementations have registered themselves.
    /// </summary>
    public static bool IsRegistered => apiFactory != null;

    internal static Func<Model.CryptoExchange, ExchangeBase> ApiFactory
    {
        get
        {
            EnsureRegistered();
            return apiFactory!;
        }
    }

    internal static Func<Model.CryptoExchange, CryptoIntervalPeriod, bool> IntervalSupported
    {
        get
        {
            EnsureRegistered();
            return intervalSupported!;
        }
    }

    internal static Action<CryptoExternalUrlList> InitializeUrls
    {
        get
        {
            EnsureRegistered();
            return initializeUrls!;
        }
    }

    /// <summary>
    /// Loads CryptoScanner.Exchanges and lets it register itself. Only tried once: if the assembly
    /// is not next to the executable, retrying on every call would only slow things down.
    /// </summary>
    private static void EnsureRegistered()
    {
        if (apiFactory != null)
            return;

        lock (RegistrationLock)
        {
            if (apiFactory == null && !loadAttempted)
            {
                loadAttempted = true;
                Type? providerType = Type.GetType(ProviderTypeName, throwOnError: false);
                MethodInfo? register = providerType?.GetMethod("Register", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
                register?.Invoke(null, null);
            }
        }

        if (apiFactory == null)
            throw new InvalidOperationException(
                "No exchange implementations registered. Add a project reference to CryptoScanner.Exchanges " +
                "(and optionally call ExchangeProvider.Register() at startup).");
    }
}
