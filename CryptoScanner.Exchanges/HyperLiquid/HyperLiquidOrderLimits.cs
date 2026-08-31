using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.HyperLiquid;

/// <summary>
/// What HyperLiquid allows a single ORDER to be worth. Shared by the Spot and the Perpetual market,
/// because neither of the two publishes a minimum or a maximum QUANTITY per symbol. The meta of a
/// perpetual market carries name, szDecimals, maxLeverage, onlyIsolated, isDelisted, marginTableId
/// and marginMode, and the spot meta carries szDecimals and weiDecimals per token - checked against
/// the stored symbols.json of both markets on 30-08-2026, and the markets deployed by outside
/// parties answer with the same fields. The size grid (szDecimals) is therefore the only per-symbol
/// limit on the QUANTITY the exchange states; everything else it states is a limit on the VALUE.
/// </summary>
public static class HyperLiquidOrderLimits
{
    /// <summary>
    /// The smallest value an order may have, in the quote currency. HyperLiquid answers a smaller
    /// one with "Order must have minimum value of $10." on a perpetual market and "Order must have
    /// minimum value of 10 {quote_token}." on spot. Every spot pair we follow is quoted in a dollar
    /// stablecoin - 309 in USDC, 11 in USDH, 5 in USDT0 and 1 in USDE - so the two come down to the
    /// same amount here.
    /// <para>
    /// On nearly every symbol this is the binding one of the two limits, not the size tick. Over the
    /// 176 active perpetual markets of 30-08-2026 there is not one where a single size tick is worth
    /// more than ten dollar: the most expensive is BTC at 0.00001 x 79156 = 0.79. Of the 326 spot
    /// pairs three do go over it - XAUT0/USDC at 44.62, XAUT0/USDT0 at 43.50 and TSLA/USDC at 15.80,
    /// all three a size tick of 0.01 under a price in the thousands. The minimum entry is the larger
    /// of this amount and one size tick, which is why the size tick is stored as the minimum
    /// quantity as well.
    /// </para>
    /// </summary>
    public const decimal MinimumOrderValue = 10m;


    /// <summary>
    /// The largest value a LIMIT order may have on a perpetual market, in the quote currency.
    /// HyperLiquid states it as ten times the maximum value of a MARKET order, which in turn follows
    /// from the maximum leverage of that instrument - the one number of the three that does sit in
    /// the meta per symbol.
    /// <para>
    /// The limit variant is the one stored, because the trader enters and exits with limit orders.
    /// A market order is allowed one tenth of it, so a market order beyond that is let through here
    /// and refused by the exchange. That is the better way round: the alternative silently trades a
    /// smaller size than asked for, and both amounts are far above anything this scanner stakes -
    /// the tightest of them all is 500.000 dollar.
    /// </para>
    /// <para>
    /// Spot has no leverage and HyperLiquid states no maximum for it, so a spot symbol keeps a
    /// maximum of zero, which Clamp reads as "no maximum".
    /// </para>
    /// </summary>
    /// <param name="maxLeverage">maxLeverage of the instrument, straight from the meta</param>
    public static decimal MaximumLimitOrderValue(int maxLeverage)
    {
        return 10m * MaximumMarketOrderValue(maxLeverage);
    }


    /// <summary>
    /// The largest value a MARKET order may have, in the quote currency. Four brackets, taken from
    /// the contract specifications of HyperLiquid. Measured over the 176 active markets of
    /// 30-08-2026: 142 sit in the lowest bracket, 30 in the third, 2 in the second and 2 in the
    /// highest.
    /// </summary>
    private static decimal MaximumMarketOrderValue(int maxLeverage)
    {
        if (maxLeverage >= 25)
            return 30_000_000m;
        if (maxLeverage >= 20)
            return 5_000_000m;
        if (maxLeverage >= 10)
            return 2_000_000m;
        return 500_000m;
    }


    /// <summary>
    /// Fills the size grid and the order limits of one symbol. Both HyperLiquid markets and the
    /// markets deployed by outside parties come through here, so there is one place that decides
    /// what a symbol may be ordered in and one place a test can reach.
    /// <para>
    /// The minimum QUANTITY is one size tick, because that is the smallest order the grid allows
    /// at all. On nearly every symbol the minimum order VALUE above is the binding one of the two,
    /// but not on all of them - see the measurements there - so both are stored and whichever is
    /// the larger decides the minimum entry.
    /// </para>
    /// <para>
    /// The maximum QUANTITY is deliberately zero: HyperLiquid publishes none, and zero is what
    /// Clamp and CheckOrderSetAgainstSymbolLimits both read as "no maximum". Written every refresh
    /// rather than left alone, so a value an earlier build wrote into the symbol row cannot
    /// survive in the database.
    /// </para>
    /// </summary>
    /// <param name="symbol">The symbol being refreshed.</param>
    /// <param name="quantityDecimals">szDecimals of the instrument - a NUMBER of decimals and not
    /// a tick size, so it is converted here (see SymbolBase.TickSizeFromDecimals).</param>
    /// <param name="maxLeverage">maxLeverage of a PERPETUAL instrument, or null on spot. Spot has
    /// no leverage and HyperLiquid states no maximum order value for it, so the maximum value stays
    /// zero there.</param>
    public static void ApplyLimits(CryptoSymbol symbol, int quantityDecimals, int? maxLeverage)
    {
        symbol.QuantityTickSize = SymbolBase.TickSizeFromDecimals(quantityDecimals);
        symbol.QuantityMinimum = symbol.QuantityTickSize;
        symbol.QuantityMaximum = 0m;

        symbol.QuoteValueMinimum = MinimumOrderValue;
        symbol.QuoteValueMaximum = maxLeverage.HasValue ? MaximumLimitOrderValue(maxLeverage.Value) : 0m;
    }
}
