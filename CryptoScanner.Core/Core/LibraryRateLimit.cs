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
    /// The gates already lowered in this process, by gate name. Kept for two reasons: a second call
    /// is a no-op instead of a second guard, and <see cref="SpendAsync"/> needs to find the gate back.
    /// The gate name alone is the key: it is all <see cref="SpendAsync"/> has, and the packages name
    /// their gates after themselves ("HyperLiquidRest"), so it identifies the package as well.
    /// </summary>
    private static readonly Dictionary<string, IRateLimitGate> Lowered = [];

    /// <summary>Only there because ProcessAsync wants an id per item; nothing reads it back.</summary>
    private static int itemId;


    /// <summary>
    /// Add a guard to one of a package's rate limit gates. Safe to call more than once - the exchange
    /// defaults are applied again on every exchange switch.
    /// </summary>
    /// <param name="rateLimiters">The package's rate limiter object, for example HyperLiquidExchange.RateLimiter.</param>
    /// <param name="gateName">Name of the gate property on it, for example "HyperLiquidRest".</param>
    /// <param name="weightPerMinute">What this process may spend per minute, in the exchange's own weight units.</param>
    /// <param name="exchangeName">Only for the log line.</param>
    /// <returns>False when the gate could not be reached, which means no ceiling was added.</returns>
    public static bool Lower(object rateLimiters, string gateName, int weightPerMinute, string exchangeName)
    {
        lock (Lowered)
        {
            if (Lowered.ContainsKey(gateName))
                return true;

            try
            {
                PropertyInfo property = rateLimiters.GetType().GetProperty(gateName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new Exception($"the package has no property named {gateName}");

                if (property.GetValue(rateLimiters) is not IRateLimitGate gate)
                    throw new Exception($"{gateName} did not hold an IRateLimitGate");

                gate.AddGuard(new RateLimitGuard(RateLimitGuard.PerHost, Array.Empty<IGuardFilter>(),
                    weightPerMinute, TimeSpan.FromMinutes(1), RateLimitWindowType.Sliding));

                Lowered.Add(gateName, gate);
                GlobalData.AddTextToLogTab($"{exchangeName} rate limit lowered to {weightPerMinute} weight per minute");
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
            Lowered.TryGetValue(gateName, out gate);
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
