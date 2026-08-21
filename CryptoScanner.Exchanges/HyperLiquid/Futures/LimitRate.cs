using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.HyperLiquid.Futures;

internal class HyperLiquidWeight
{
    public long Time { get; set; }
    public long Weight { get; set; }

}

/// <summary>
/// Delays the caller when too many requests are sent to the exchange, counted as weight over a
/// moving window of 60 seconds. Same shape as the other exchanges, but the numbers here are the
/// exchange's OWN units.
///
/// HyperLiquid grants 1200 request weight per minute PER IP ADDRESS
/// (https://hyperliquid.gitbook.io/hyperliquid-docs/for-developers/api/rate-limits-and-user-limits).
/// A handful of info requests weigh 2 (l2Book, allMids, clearinghouseState, orderStatus,
/// spotClearinghouseState, exchangeStatus); every other info request weighs 20, and both the
/// candleSnapshot this scanner lives on and the symbol/ticker refresh are in that second group.
/// So the whole machine gets 1200 / 20 = 60 candle requests per minute, and not one per scanner.
///
/// PER IP is what this class exists for. The rate limiter inside the library counts per process and
/// therefore holds EVERY process to 60 requests a minute - measured over the night of 19/20-08-2026,
/// HyperLiquid Spot sat at precisely that ceiling for 150 minutes and HyperLiquid Futures for 58.
/// Two scanners on one machine is one IP address asking twice the allowance, which is exactly why
/// that night produced "Server rate limit exceeded" while both processes believed they were behaving.
///
/// The budget below is a SHARE of the machine's allowance, not the whole of it:
///
///     25 candle requests per minute per scanner (500 of the 1200 weight)
///     two HyperLiquid scanners  = 50 of the 60 the IP address is allowed
///     the remaining 10 covers the symbol/ticker refresh of both, and anything else on this
///     machine that talks to HyperLiquid.
///
/// Raise ScannersOnThisAddress when a third HyperLiquid scanner is added, or lower RequestsPerMinute when
/// another application starts using the same address. Catching up a fresh candle history is the
/// only thing that really feels this cap; the steady state of a night needed 10 requests a minute
/// on Spot and 4 on Futures.
/// </summary>
public static class LimitRate
{
    /// <summary>Weight of one ordinary info request, straight from the documentation.</summary>
    public const long InfoRequestWeight = 20;

    /// <summary>What the exchange allows one IP address per minute.</summary>
    private const long AddressBudgetPerMinute = 1200;

    /// <summary>HyperLiquid scanners expected to run on this machine (Spot and Futures).</summary>
    private const long ScannersOnThisAddress = 2;

    /// <summary>
    /// What this one scanner spends. AddressBudgetPerMinute / InfoRequestWeight / ScannersOnThisAddress
    /// works out at 30; the five below that leave room for the requests this class never sees - the
    /// symbol and ticker refresh of both scanners, and anything else on this machine.
    /// </summary>
    private const long RequestsPerMinute = 25;

    private const long WindowSeconds = 60;

    private static long MaximumWeight => RequestsPerMinute * InfoRequestWeight;

    public static long CurrentWeight { get; set; }
    static private List<HyperLiquidWeight> List { get; } = new List<HyperLiquidWeight>();

    public static void WaitForFairWeight(long newWeight)
    {
        Monitor.Enter(List);
        try
        {
            while (true)
            {
                // Current time.
                DateTimeOffset dateTimeOffset = DateTime.UtcNow;
                long unix = dateTimeOffset.ToUnixTimeSeconds();

                // Drop the registrations that fell out of the window
                long removeBeforeDate = unix - WindowSeconds;

                while (List.Count > 0)
                {
                    HyperLiquidWeight item = List[0];
                    if (item.Time <= removeBeforeDate)
                    {
                        CurrentWeight -= item.Weight;
                        List.RemoveAt(0);
                    }
                    else break;
                }

                if (CurrentWeight > MaximumWeight)
                {
                    GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} delay needed for weight: {CurrentWeight} (rate limits)");
                    // Release the lock while waiting. Sleeping inside Monitor.Enter(List) queues
                    // every other fetch thread behind this one, after which they each sleep in
                    // turn instead of re-testing the (by then lowered) weight.
                    Monitor.Exit(List);
                    try
                    {
                        Thread.Sleep(2500);
                    }
                    finally
                    {
                        Monitor.Enter(List);
                    }
                }
                else
                {
                    CurrentWeight += newWeight;

                    // And add a new registration
                    HyperLiquidWeight item = new();
                    DateTimeOffset dateTimeOffset2 = DateTime.UtcNow;
                    item.Time = dateTimeOffset2.ToUnixTimeSeconds();
                    item.Weight = newWeight;
                    List.Add(item);

                    break;
                }
            }
        }
        finally
        {
            Monitor.Exit(List);
        }
    }
}
