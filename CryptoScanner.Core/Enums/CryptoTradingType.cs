namespace CryptoScanner.Core.Enums;

/// <summary>
/// Which product of an exchange a market covers. One exchange can offer several of these at the
/// same time, which is why this sits next to <see cref="CryptoExchangeType"/> rather than inside
/// it: Okx is one exchange with three markets here.
///
/// The numbers are stored in the Exchange table, so they may never be renumbered. Spot and
/// Perpetual carry the values that Spot and "Futures" had before 27-08-2026. Value 2 was XPerp,
/// a market of its own for the X-Perps of Okx; those now live in the Perpetual market beside the
/// swaps, told apart by their product. The number stays unused rather than being handed to
/// something else, so an old row can never read as a market it never was.
///
/// A value only earns a place here once there is an implementation behind it. The products the
/// scanner deliberately leaves alone - inverse contracts, contracts with an expiry date, options -
/// would be dead branches in the three switches of ExchangeProvider until one gets built.
/// </summary>
public enum CryptoTradingType
{
    /// <summary>
    /// Buying the coin itself. No expiry, no leverage, nothing to pay for holding a position.
    /// </summary>
    Spot = 0,

    /// <summary>
    /// A linear perpetual: a contract without an expiry date, with the stablecoin of the pair as
    /// both margin and payout (BTCUSDT, BTCUSDC). This is what the scanner used to call "Futures",
    /// which was wrong - a future expires and these never do.
    /// </summary>
    Perpetual = 1,

}
