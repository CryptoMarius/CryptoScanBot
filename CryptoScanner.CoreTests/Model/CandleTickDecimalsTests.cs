using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Model;

/// <summary>
/// A candle keeps its four prices as an int number of ticks (price / tickSize, see
/// CryptoCandle._openTicks), and the decimal-to-int conversion in the setters throws an
/// OverflowException when the result does not fit - always, checked context or not.
///
/// SymbolBase.LimitDecimalsToCandleRange already caps the tick size of the SYMBOL, but it can only
/// measure against the price of the moment. A history holds prints that the price of the moment
/// says nothing about, and those are the ones that overflow. CryptoCandle.FitTickDecimals is the
/// second line: it coarsens the tick size for THAT ONE CANDLE, which is free because TickDecimals
/// is a per-candle field and is persisted per candle.
///
/// The two cases below are the ones that actually broke, on HyperLiquid Spot in the night of
/// 19 to 20-08-2026. Both stopped their whole 1w series from synchronising - 175 exceptions and
/// 350 error lines in one night - because the exception aborts the interval loop for the symbol.
/// </summary>
[TestClass]
public class CandleTickDecimalsTests
{
    /// <summary>
    /// UBTC/USDC trades around 70,071, which justifies a tick size of 0.001 (three decimals). Its
    /// weekly history holds a print of 100,002,060 - fourteen hundred times the current price. At
    /// three decimals that is 1.0e11 ticks, fifty times what an int holds.
    /// </summary>
    [TestMethod]
    public void FitTickDecimals_WeeklyOutlierOnUbtc_LowersDecimalsUntilItFits()
    {
        const decimal price = 100_002_060m;

        byte decimals = CryptoCandle.FitTickDecimals(3, price, price, price, price);

        Assert.IsTrue(decimals < 3, "the three decimals of the symbol cannot hold this price");
        AssertStorable(decimals, price);
    }

    /// <summary>
    /// JEFF/USDC has a tick size of 0.00000001 (eight decimals). Its weekly high of 41.515 needs
    /// 4.15e9 ticks, just under twice what an int holds - one decimal less is enough.
    /// </summary>
    [TestMethod]
    public void FitTickDecimals_WeeklyHighOnJeff_LosesExactlyOneDecimal()
    {
        const decimal price = 41.515m;

        byte decimals = CryptoCandle.FitTickDecimals(8, price, price, price, price);

        Assert.AreEqual(7, decimals, "one decimal less is enough here");
        AssertStorable(decimals, price);
    }

    /// <summary>
    /// The ordinary case has to stay untouched - coarsening a tick size that fits perfectly well
    /// would quietly cost precision on every candle of every exchange.
    /// </summary>
    [TestMethod]
    public void FitTickDecimals_PriceThatFits_KeepsTheDecimalsOfTheSymbol()
    {
        // XAUT0/USDC: 5,608.60 at five decimals is 5.6e8 ticks, a quarter of the int range.
        Assert.AreEqual(5, CryptoCandle.FitTickDecimals(5, 5608.60m, 5608.60m, 5608.60m, 5608.60m));

        // UETH/USDC: 4,976.30 at four decimals is 5.0e7 ticks.
        Assert.AreEqual(4, CryptoCandle.FitTickDecimals(4, 4976.30m, 4976.30m, 4976.30m, 4976.30m));
    }

    /// <summary>
    /// The barometer writes a median into Open and an average into Close and both go below zero
    /// regularly (see BarometerTools), so the largest ABSOLUTE value has to decide. Taking the high
    /// alone would walk straight past the value that overflows.
    /// </summary>
    [TestMethod]
    public void FitTickDecimals_NegativeValueIsTheLargest_StillFitted()
    {
        byte decimals = CryptoCandle.FitTickDecimals(8, open: -41.515m, high: 1m, low: -41.515m, close: 1m);

        Assert.AreEqual(7, decimals, "the negative open is the value that does not fit");
    }

    /// <summary>
    /// Storing the price at these decimals must not throw, and must still give the price back.
    /// </summary>
    private static void AssertStorable(byte tickDecimals, decimal price)
    {
        CryptoCandle candle = new()
        {
            TickDecimals = tickDecimals,
            Open = price,
            High = price,
            Low = price,
            Close = price,
        };

        // What comes back is the price rounded to the decimals that did fit, nothing more.
        decimal tolerance = 1m;
        for (int i = 0; i < tickDecimals; i++)
            tolerance *= 0.1m;

        Assert.AreEqual((double)price, (double)candle.Close, (double)tolerance,
            "the stored price has to survive the coarser tick size");
    }
}
