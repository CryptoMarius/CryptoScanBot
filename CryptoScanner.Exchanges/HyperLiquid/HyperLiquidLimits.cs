using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.HyperLiquid;

/// <summary>
/// What HyperLiquid allows an IP ADDRESS to spend, and the share of it this process takes. Shared by
/// the Spot and the Perpetual market because the exchange does not separate them: both talk to the
/// same host and both are counted against the same address budget.
///
/// <para>
/// The documented REST limits, checked against the API documentation on 28-08-2026:
/// <list type="bullet">
///   <item>1200 request weight per minute per address, over every endpoint together.</item>
///   <item>An ordinary info request weighs 20.</item>
///   <item>l2Book, allMids, clearinghouseState, orderStatus, spotClearinghouseState and
///         exchangeStatus weigh 2; userRole weighs 60.</item>
///   <item><b>candleSnapshot carries an ADDITIONAL weight per 60 candles in the answer.</b></item>
///   <item>An order request weighs 1 + floor(batch length / 40).</item>
/// </list>
/// </para>
///
/// <para>
/// That fourth line is the one that was missing until 28-08-2026, and it is not a detail. The package
/// books a flat 20 for a candle request, because the number of candles is only known once the answer
/// is in. Measured over the cold start of HyperLiquid Perpetual that afternoon - 296 requests, 218304
/// candles - the package counted 296 x 20 = 5920 weight while the exchange counted 5920 + 218304 / 60
/// = 9558. A ceiling of 450 was therefore really about 700, and two markets on the documented "75% of
/// the budget" were really at 1400 of the 1200 allowed.
/// </para>
///
/// <para>
/// It also moves the ceiling on the request RATE. Not 1200 / 20 = 60 requests per minute, which is
/// what the comments assumed: our average candle request came to 32 weight (20 plus some 740 candles),
/// so the address allows about 37 of them per minute and no more.
/// </para>
///
/// <para>
/// The websocket side, same source and same date, also per address: 10 connections, 30 NEW connections
/// per minute, 1000 subscriptions, 10 unique users over user subscriptions, 2000 messages per minute
/// and 100 simultaneous inflight post messages. The connection count is the tight one - see the
/// SubscriptionsPerBundle and SocketSubscriptionsCombineTarget comments in both Api classes.
/// </para>
/// </summary>
public static class HyperLiquidLimits
{
    /// <summary>Name of the rate limit gate the package carries; also the key in <see cref="LibraryRateLimit"/>.</summary>
    public const string GateName = "HyperLiquidRest";

    /// <summary>Host of every request, the package's and ours. The per-host guard keys on it.</summary>
    public const string BaseAddress = "https://api.hyperliquid.xyz";

    /// <summary>Path of every info request, the package's and ours.</summary>
    public const string InfoPath = "/info";

    /// <summary>What the exchange charges for an ordinary info request, before any per-item surcharge.</summary>
    public const int InfoRequestWeight = 20;

    /// <summary>Everything the exchange allows ONE ADDRESS to spend per minute, over all endpoints.</summary>
    public const int AddressWeightPerMinute = 1200;

    /// <summary>
    /// How many candles in one answer cost one extra weight on top of <see cref="InfoRequestWeight"/>.
    /// </summary>
    public const int CandlesPerExtraWeight = 60;

    /// <summary>
    /// What ONE running HyperLiquid market may spend per minute, in the exchange's own weight units.
    ///
    /// <para>
    /// 1000 of the 1200 the address allows, so 200 stays free for the calls that arrive outside the
    /// candle fetch: the hourly symbol and ticker refresh, the ten deployed-market requests of
    /// PerpDexClient, and the retries of whatever the exchange refuses anyway. Against the measured
    /// 32 weight per candle request that is about 31 requests per minute, where 450 gave 22.
    /// </para>
    /// <para>
    /// The number that matters for the user is the cold start: 181 symbols x 12 intervals x 32 weight
    /// is roughly 69000 weight, which at 1000 per minute is some 70 minutes against the 100 minutes
    /// measured on 28-08-2026. Below that hour lies a floor this dial cannot reach - even the full
    /// 1200 only brings it to 58 minutes. Shortening it further means asking for FEWER candles, not
    /// asking faster: the 1h interval alone fetches 3000 candles per symbol because 6h is built from
    /// 3h which is built from 1h, and neither of those two exists on HyperLiquid.
    /// </para>
    /// <para>
    /// IT IS THE WHOLE BUDGET OF ONE ADDRESS, NOT A SHARE PER MARKET. Running HyperLiquid Spot and
    /// HyperLiquid Perpetual on the same machine at the same time spends 2000 of the 1200 allowed and
    /// the exchange starts refusing; halve this number when that becomes the normal situation. The
    /// earlier value of 450 was a share meant to survive that case, but it was measured against a
    /// weight model that turned out to be wrong, so it protected nothing and only cost speed.
    /// </para>
    /// </summary>
    public const int WeightPerMinute = 1000;


    /// <summary>
    /// Book the weight that the exchange charges ON TOP of the flat <see cref="InfoRequestWeight"/> the
    /// package already booked for a candle request, now that the number of candles is known. Waits when
    /// the budget is full, which is the whole point: without it every candle request is undercounted by
    /// however much history it brought back, and the ceiling means something other than what it says.
    /// <para>
    /// Rounded UP on purpose. The documentation says "an additional weight per 60 items" without giving
    /// the rounding, and booking slightly more than the exchange does costs one weight per request while
    /// booking less would put us over the limit without any way to see it.
    /// </para>
    /// </summary>
    /// <param name="candleCount">Number of candles the exchange returned.</param>
    public static async Task BookCandleWeightAsync(int candleCount)
    {
        if (candleCount <= 0)
            return;

        int extraWeight = (candleCount + CandlesPerExtraWeight - 1) / CandlesPerExtraWeight;

        try
        {
            await LibraryRateLimit.SpendAsync(GateName, BaseAddress, InfoPath, extraWeight,
                ExchangeBase.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The session was stopped (exchange switch, standby, shutdown). The fetch loop tests the
            // same token right after this call, so letting it through here would only turn a normal
            // stop into an error line.
        }
    }
}
