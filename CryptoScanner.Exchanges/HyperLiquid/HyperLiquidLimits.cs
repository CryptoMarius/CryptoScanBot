using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;

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
///
/// <para>
/// HyperLiquid.md next to this file carries the same limits with their source url, the measured cost
/// of four consecutive starts on 30-08-2026, and the two leads that could still make a start faster.
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

    /// <summary>What the DOCUMENTATION says one address may spend per minute, over all endpoints.</summary>
    public const int AddressWeightPerMinute = 1200;

    /// <summary>
    /// What the address was MEASURED to be allowed per minute on 30-08-2026, which is not the same
    /// thing and is three times as much. Two burst sizes five times apart in weight - 177 requests of
    /// 21 weight, 36 requests of 104 - both settled on this figure within 0.7%, which confirms the
    /// documented weight model exactly while contradicting the documented ceiling. The measurement is
    /// Tools/HyperLiquidRateTest and the write-up is HyperLiquid.md next to this file.
    /// <para>
    /// Here only to be read next to the number above. Nothing computes from it: what the scanner
    /// actually spends is the setting, see <see cref="WeightPerMinute"/>.
    /// </para>
    /// </summary>
    public const int MeasuredAddressWeightPerMinute = 3730;

    /// <summary>
    /// How many candles in one answer cost one extra weight on top of <see cref="InfoRequestWeight"/>.
    /// </summary>
    public const int CandlesPerExtraWeight = 60;

    /// <summary>
    /// What THIS market may spend per minute, in the exchange's own weight units. Comes from
    /// <see cref="SettingsGeneral.HyperLiquidWeightPerMinute"/>, because the right value depends on
    /// something the code cannot see: how many scanners on this machine are talking to HyperLiquid.
    ///
    /// <para>
    /// IT IS A SHARE OF ONE ADDRESS, NOT A BUDGET PER MARKET. HyperLiquid counts per IP address, so
    /// HyperLiquid Spot and HyperLiquid Perpetual running side by side each spend from the same pool
    /// and neither can see the other. One scanner may have the lot; two have to be set to half each.
    /// The setting is the only place where that division is stated.
    /// </para>
    /// <para>
    /// The default is 3000 against a measured ceiling of about 3730
    /// (<see cref="MeasuredAddressWeightPerMinute"/>) and a documented one of 1200. The distance to
    /// the measured figure is deliberate. Our minute and the exchange's minute do not start on the
    /// same second, so a client-side window sitting exactly on the limit tips over it now and then,
    /// and a refusal is not free - it costs the five seconds of the first retry in CandleBase.
    /// </para>
    /// <para>
    /// It stood at a fixed 1150 until 30-08-2026, and the history of that number is worth keeping
    /// because it explains what to distrust here. 450 was chosen as "75% of the documented budget
    /// divided over two markets", against a weight model that did not yet know candleSnapshot carries
    /// a surcharge per 60 candles; correcting the model moved it to 1150, and the start of 117
    /// symbols still took three minutes. Only measuring the exchange instead of reading it showed the
    /// ceiling itself was the wrong number. The measurement is one address on one afternoon, so this
    /// setting can be wrong in the same way - the log line to watch is "delay needed because of rate
    /// limits", which appears when the exchange itself refused and never for a client-side wait.
    /// </para>
    /// <para>
    /// Where the remaining minutes of a start have to come from, once this dial is right: asking
    /// FEWER TIMES. 88% of the weight of a start is the flat 20 per request and only 12% is candles.
    /// Two ways, both in Core: derive 3m, 2h and 4h from candles that are already being fetched (12
    /// requests per fresh symbol become 9), and stop dragging the 1h interval to 3000 candles per
    /// symbol, which happens only because 6h is built from 3h which is built from 1h and HyperLiquid
    /// has neither.
    /// </para>
    /// </summary>
    public static int WeightPerMinute =>
        GlobalData.Settings?.General.HyperLiquidWeightPerMinute ?? DefaultWeightPerMinute;

    /// <summary>
    /// What <see cref="WeightPerMinute"/> falls back to before the settings have been read. The same
    /// constant the setting itself defaults to, so the two cannot drift apart.
    /// </summary>
    public const int DefaultWeightPerMinute = SettingsGeneral.HyperLiquidWeightPerMinuteDefault;


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
