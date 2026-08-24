using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.HyperLiquid.Spot;

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
///
/// The requests are SPREAD over the minute (see <see cref="MinimumSpacing"/>) instead of being spent
/// as fast as they are asked for. Counting weight is by itself enough to stay inside the allowance,
/// but it hands out the whole budget in one burst and then blocks until the oldest registration
/// leaves the window - and because those registrations were all made within a second of each other,
/// they also expire within a second of each other. Measured during the startup of 24-08-2026 on
/// HyperLiquid Futures: 25 requests inside 1.2 seconds, then 42.9 seconds in which nothing happened,
/// and that eight times in a row. Of the 429 seconds the catch-up took, roughly 31 were spent
/// sending requests. The longest wait drops from 43 seconds to 2.4, and with it the reason for the
/// log line that used to be written on every waiting round.
///
/// It does cost a little. Bursting reached 27 requests a minute over those 429 seconds, above the 25
/// this class means to spend: a registration is stamped in whole seconds and therefore leaves the
/// window up to a second early, which lets an extra request through per cycle. Spacing holds exactly
/// 25, so a catch-up of 194 requests takes some 35 seconds longer. Only a higher budget makes
/// starting up faster, see ScannersOnThisAddress.
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

    /// <summary>
    /// The shortest distance between two requests: the window divided by what fits in it, so 2.4
    /// seconds. The weight count stays as the backstop underneath it - the spacing knows only about
    /// the requests that pass through this class, the weight window also sees what a previous burst
    /// left behind.
    /// </summary>
    private static readonly TimeSpan MinimumSpacing = TimeSpan.FromSeconds((double)WindowSeconds / RequestsPerMinute);

    public static long CurrentWeight { get; set; }
    static private List<HyperLiquidWeight> List { get; } = new List<HyperLiquidWeight>();

    /// <summary>When the previous request was let through; <see cref="MinimumSpacing"/> is measured from here.</summary>
    private static DateTime lastRequest = DateTime.MinValue;

    /// <summary>
    /// Set while the weight ceiling is holding requests back, so the log gets one line when that
    /// starts and one when it ends. Every waiting thread used to write a line on every round, which
    /// came down to 657 identical lines for 194 requests during the startup of 24-08-2026.
    /// </summary>
    private static bool throttled;

    /// <summary>When <see cref="throttled"/> was set, so the closing line can say how long it lasted.</summary>
    private static DateTime throttledSince;


    /// <summary>
    /// Wait without holding the lock, and take it back afterwards; the caller must already own it.
    /// Release the lock while waiting. Sleeping inside Monitor.Enter(List) queues
    /// every other fetch thread behind this one, after which they each sleep in
    /// turn instead of re-testing the (by then lowered) weight.
    /// </summary>
    private static void SleepWithoutTheLock(TimeSpan duration)
    {
        Monitor.Exit(List);
        try
        {
            Thread.Sleep(duration);
        }
        finally
        {
            Monitor.Enter(List);
        }
    }


    public static void WaitForFairWeight(long newWeight)
    {
        Monitor.Enter(List);
        try
        {
            while (true)
            {
                // Current time.
                DateTime utcNow = DateTime.UtcNow;
                DateTimeOffset dateTimeOffset = utcNow;
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
                    if (!throttled)
                    {
                        throttled = true;
                        throttledSince = utcNow;
                        GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} rate limit reached: " +
                            $"{CurrentWeight / InfoRequestWeight} requests in the last {WindowSeconds} seconds, holding further requests");
                    }
                    SleepWithoutTheLock(TimeSpan.FromMilliseconds(2500));
                }
                else if (utcNow - lastRequest < MinimumSpacing)
                {
                    // Not at the ceiling, only too soon after the previous request. Waiting out the
                    // remainder keeps the pace even instead of emptying the budget in one burst.
                    SleepWithoutTheLock(MinimumSpacing - (utcNow - lastRequest));
                }
                else
                {
                    if (throttled)
                    {
                        throttled = false;
                        GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} rate limit released after " +
                            $"{(utcNow - throttledSince).TotalSeconds:N1} s");
                    }

                    CurrentWeight += newWeight;
                    lastRequest = utcNow;

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
