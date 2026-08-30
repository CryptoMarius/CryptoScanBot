using CryptoExchange.Net.Objects;
using CryptoExchange.Net.RateLimiting;
using CryptoExchange.Net.RateLimiting.Guards;
using CryptoExchange.Net.RateLimiting.Interfaces;

using Microsoft.Extensions.Logging.Abstractions;

using System.Reflection;

namespace CryptoScanner.Core.Core;

/// <summary>
/// Tightens the rate limiter that a CryptoExchange.Net package already carries, so this scanner asks
/// for less than the exchange allows.
/// <para>
/// Every package of that family ships a gate holding the exchange's documented budget - HyperLiquid's
/// is one guard of 1200 weight per minute. Guards are conditions that all have to pass, so ADDING a
/// stricter one lowers the ceiling without touching what the package knows: measured on 28-08-2026
/// against the live API with an extra guard of 100 weight, five requests went straight through and
/// the sixth was held for 57 seconds until the window had room again.
/// </para>
/// <para>
/// Why lower it at all: an exchange counts per IP ADDRESS while the package counts per PROCESS.
/// Nineteen scanners share one address on this machine, so a market that is scanned twice (a Spot and
/// a Perpetual one) asks for twice what either process believes it is asking. What the caller passes
/// in is therefore a SHARE of the documented budget, not the whole of it.
/// </para>
/// <para>
/// The gate itself is internal to the package, so it is reached by reflection - the one brittle step
/// in this, and the reason a failure is written to the error log rather than swallowed. What breaks
/// when a later package renames that property is not the scanner but the tightening: it then runs on
/// whatever the package allows, which for HyperLiquid is the address budget in full.
/// </para>
/// </summary>
public static class LibraryRateLimit
{
    /// <summary>
    /// The gates whose ceiling this process has taken over, by gate name, with the weight per minute
    /// that was applied. Kept for two reasons: a repeat of the same value is a no-op instead of a
    /// second guard, and <see cref="SpendAsync"/> needs to find the gate back. The gate name alone is
    /// the key: it is all <see cref="SpendAsync"/> has, and the packages name their gates after
    /// themselves ("HyperLiquidRest"), so it identifies the package as well.
    /// </summary>
    private static readonly Dictionary<string, (IRateLimitGate Gate, int WeightPerMinute)> Lowered = [];

    /// <summary>Only there because ProcessAsync wants an id per item; nothing reads it back.</summary>
    private static int itemId;


    /// <summary>
    /// Take over the ceiling of one of a package's rate limit gates, so this scanner asks for exactly
    /// <paramref name="weightPerMinute"/> and not for whatever the package believes the exchange
    /// allows. Safe to call more than once - the exchange defaults are applied again on every
    /// exchange switch - and a call with a different value replaces the previous one.
    ///
    /// <para>
    /// It REPLACES the package's guards instead of adding one next to them, and that is the whole
    /// point. Guards are conditions that all have to pass, so an added guard can only ever make the
    /// ceiling stricter: with HyperLiquid's own guard of 1200 still in place, a setting of 3000 would
    /// silently keep running at 1200. That was harmless while every value we ever passed was below
    /// the documented budget, and stopped being harmless when the budget turned out to be measured
    /// too low - see CryptoScanner.Exchanges/HyperLiquid/HyperLiquid.md.
    /// </para>
    /// <para>
    /// The guard list is a ConcurrentBag on the gate, reached by reflection like the gate itself, and
    /// it has no Remove - so the whole bag is cleared and one guard of ours is put back. Two things
    /// follow from that. Anything else the package had on this gate is gone as well, which is why
    /// this is only called from ExchangeDefaults, when no request is in flight and no retry-after
    /// guard can be pending. And when the bag cannot be reached the call falls back to ADDING a
    /// guard, which still holds for any value below what the package allows and is reported as such,
    /// because a ceiling that quietly means something other than what it says is the thing this whole
    /// class exists to prevent.
    /// </para>
    /// </summary>
    /// <param name="rateLimiters">The package's rate limiter object, for example HyperLiquidExchange.RateLimiter.</param>
    /// <param name="gateName">Name of the gate property on it, for example "HyperLiquidRest".</param>
    /// <param name="weightPerMinute">What this process may spend per minute, in the exchange's own weight units.</param>
    /// <param name="exchangeName">Only for the log line.</param>
    /// <returns>False when the gate could not be reached, which means no ceiling was set at all.</returns>
    public static bool Lower(object rateLimiters, string gateName, int weightPerMinute, string exchangeName)
    {
        lock (Lowered)
        {
            if (Lowered.TryGetValue(gateName, out var applied) && applied.WeightPerMinute == weightPerMinute)
                return true;

            try
            {
                PropertyInfo property = rateLimiters.GetType().GetProperty(gateName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new Exception($"the package has no property named {gateName}");

                if (property.GetValue(rateLimiters) is not IRateLimitGate gate)
                    throw new Exception($"{gateName} did not hold an IRateLimitGate");

                bool replaced = TryClearGuards(gate, out int removed);

                gate.AddGuard(new RateLimitGuard(RateLimitGuard.PerHost, Array.Empty<IGuardFilter>(),
                    weightPerMinute, TimeSpan.FromMinutes(1), RateLimitWindowType.Sliding));

                Lowered[gateName] = (gate, weightPerMinute);
                if (replaced)
                    GlobalData.AddTextToLogTab($"{exchangeName} rate limit set to {weightPerMinute} weight per minute " +
                        $"(replaced {removed} guard(s) of the package)");
                else
                    GlobalData.AddErrorToLogTab($"{exchangeName} could not replace the guards of the package, so " +
                        $"{weightPerMinute} weight per minute only holds as far as the package itself allows");
                return true;
            }
            catch (Exception error)
            {
                // Loud on purpose. Silence here means the scanner spends the exchange's full budget
                // while every process on this address believes it is being modest.
                ScannerLog.Logger.Error(error, $"LibraryRateLimit.Lower({exchangeName}, {gateName})");
                GlobalData.AddErrorToLogTab($"{exchangeName} could NOT lower the rate limit of the package " +
                    $"({error.Message}) - this process now spends whatever the package allows");
                return false;
            }
        }
    }


    /// <summary>
    /// Empty the guard list of a gate, so the guard added right after it is the only condition left.
    /// Reflection, because the list is private and a ConcurrentBag has no Remove; a failure is not
    /// thrown but reported, so the caller can say what the ceiling really means.
    /// </summary>
    /// <param name="removed">How many guards were dropped, for the log line.</param>
    /// <returns>False when the field could not be reached or holds nothing that can be cleared.</returns>
    private static bool TryClearGuards(IRateLimitGate gate, out int removed)
    {
        removed = 0;
        try
        {
            FieldInfo? field = gate.GetType().GetField("_guards",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(gate) is not System.Collections.ICollection guards)
                return false;

            removed = guards.Count;

            // ConcurrentBag<T> does have a Clear, but the field is typed as that concrete bag and
            // this class must not depend on which collection the package happened to pick.
            MethodInfo? clear = guards.GetType().GetMethod("Clear", Type.EmptyTypes);
            if (clear == null)
                return false;

            clear.Invoke(guards, null);
            return true;
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "LibraryRateLimit.TryClearGuards");
            return false;
        }
    }


    /// <summary>
    /// Book the weight of a request this scanner sends ITSELF into the same budget the package uses,
    /// and wait when that budget is full. For the handful of endpoints a package does not cover and
    /// that we therefore call over an HttpClient of our own - without this they ride on top of the
    /// ceiling instead of inside it.
    /// <para>
    /// Measured on 28-08-2026 against the live API: with a guard of 120 weight, three requests through
    /// the package plus three bookings through here filled the budget together, and the fourth booking
    /// was held for 59 seconds. One budget, whoever asks.
    /// </para>
    /// <para>
    /// Does nothing when the gate was never reached (see <see cref="Lower"/>), because there is then no
    /// ceiling to book against anyway - the error was already written at that point.
    /// </para>
    /// </summary>
    /// <param name="gateName">The same gate name that was passed to <see cref="Lower"/>.</param>
    /// <param name="baseAddress">Scheme and host of the request, "https://api.hyperliquid.xyz".</param>
    /// <param name="path">Path of the request, "/info". Together with the address this is what the
    /// per-host guard keys on, so it has to be the address the package uses.</param>
    /// <param name="weight">What the exchange charges for this request, in its own units.</param>
    public static async Task SpendAsync(string gateName, string baseAddress, string path, int weight,
        CancellationToken cancellationToken)
    {
        IRateLimitGate? gate;
        lock (Lowered)
        {
            gate = Lowered.TryGetValue(gateName, out var applied) ? applied.Gate : null;
        }

        if (gate == null)
            return;

        try
        {
            RequestDefinition definition = new(baseAddress, path, HttpMethod.Post);
            await gate.ProcessAsync(NullLogger.Instance, Interlocked.Increment(ref itemId),
                RateLimitItemType.Request, definition, null, weight, RateLimitingBehaviour.Wait,
                null, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The session was stopped; the caller deals with that
            throw;
        }
        catch (Exception error)
        {
            // Booking must never break the request itself
            ScannerLog.Logger.Error(error, $"LibraryRateLimit.SpendAsync({gateName})");
        }
    }
}
