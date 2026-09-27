using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Barometer;

public static class BarometerHelper
{
    /// <summary>
    /// Below this many coins a barometer describes no market: on BitMart Perpetual the USDC barometer
    /// is one coin, so it is the price change of that one coin with a market name on it. Such a
    /// barometer is treated as neutral - it neither blocks nor counts - in every check below (open
    /// point 40). The same limit as the emulator (BarometerReplay.MinimumSymbols) and the report.
    /// </summary>
    public const int MinimumSymbols = 5;

    /// <summary>True when the measurement rests on fewer than <see cref="MinimumSymbols"/> coins.</summary>
    public static bool TooFewSymbols(CryptoBarometerData barometerData)
        => barometerData.PriceSymbolCount.HasValue && barometerData.PriceSymbolCount.Value < MinimumSymbols;

    public static bool CheckValidBarometer(Model.CryptoExchange activeExchange, string quoteName, CryptoIntervalPeriod intervalPeriod, (decimal minValue, decimal maxValue) values, out string reaction)
    {
        if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval? interval))
        {
            reaction = $"Interval {intervalPeriod} does not exist"; // impossible but voila
            return false;
        }

        // We gaan ervan uit dat alles in 1x wordt berekend
        CryptoBarometerData? barometerData = activeExchange.Data.GetBarometer(quoteName, intervalPeriod);
        if (!barometerData.PriceBarometer.HasValue)
        {
            // The barometer is a market-breadth measure over the FULL symbol pool of a quote. The
            // emulator replays only a handful of symbols, so it is never calculated — and computing
            // it from that subset would be meaningless (a few coins do not represent "the market").
            // Treat the missing barometer as neutral (pass) in emulator mode so it does not block
            // every signal/position; the live scanner still requires a real barometer value.
            if (GlobalData.IsEmulatorMode)
            {
                reaction = "";
                return true;
            }

            reaction = $"Barometer {interval.Name} not calculated";
            return false;
        }

        // Too few coins to be a market: neutral (open point 40)
        if (TooFewSymbols(barometerData))
        {
            reaction = "";
            return true;
        }

        if (!barometerData.PriceBarometer.IsBetween(values.minValue, values.maxValue))
        {
            string minValueStr = values.minValue.ToString0("N2");
            if (values.minValue == decimal.MinValue)
                minValueStr = "-maxint";
            string maxValueStr = values.maxValue.ToString0("N2");
            if (values.maxValue == decimal.MaxValue)
                maxValueStr = "+maxint";
            reaction = $"Barometer {interval.Name} {barometerData.PriceBarometer?.ToString0("N2")} not between {minValueStr} and {maxValueStr}";
            return false;
        }


        reaction = "";
        return true;
    }


    public static bool ValidBarometerConditions(Model.CryptoExchange activeExchange, string quoteName, Dictionary<CryptoIntervalPeriod, (decimal minValue, decimal maxValue)> barometer, out string reaction)
    {
        foreach (KeyValuePair<CryptoIntervalPeriod, (decimal, decimal)> item in barometer)
        {
            if (!CheckValidBarometer(activeExchange, quoteName, item.Key, item.Value, out reaction))
                return false;
        }

        reaction = "";
        return true;
    }


    /// <summary>
    /// The market breadth condition (open point 11, phase 3): the percentage of the coins of the quote
    /// that rose over the last hour must lie between minimum and maximum. Missing breadth is handled
    /// as a missing barometer: neutral in the emulator, a refusal in the live scanner.
    /// </summary>
    public static bool CheckBreadth(Model.CryptoExchange activeExchange, string quoteName, decimal minimum, decimal maximum, out string reaction)
    {
        CryptoBarometerData? barometerData = activeExchange.Data.GetBarometer(quoteName, CryptoIntervalPeriod.interval1h);
        if (!barometerData.PricePercentageRising.HasValue)
        {
            if (GlobalData.IsEmulatorMode)
            {
                reaction = "";
                return true;
            }

            reaction = "Market breadth 1h not calculated";
            return false;
        }

        // Too few coins to be a market: neutral (open point 40)
        if (TooFewSymbols(barometerData))
        {
            reaction = "";
            return true;
        }

        decimal breadth = barometerData.PricePercentageRising.Value;
        if (breadth < minimum || breadth > maximum)
        {
            reaction = $"Market breadth 1h {breadth.ToString0("N1")}% not between {minimum.ToString0("N1")} and {maximum.ToString0("N1")}";
            return false;
        }

        reaction = "";
        return true;
    }


    /// Check how many higher-timeframe barometers align with the signal direction.
    /// Only active barometer intervals (those enabled via the Active checkbox) with a higher
    /// duration than the signal interval are considered.
    /// Returns true if the consensus count meets the minimum, or if the check is not applicable.
    public static bool CheckConsensusBarometer(Model.CryptoExchange activeExchange, string quoteName,
        CryptoIntervalPeriod signalIntervalPeriod, Dictionary<CryptoIntervalPeriod, (decimal minValue, decimal maxValue)> activeBarometerIntervals,
        int minConsensus, CryptoTradeSide side, out string reaction)
    {
        reaction = "";
        if (minConsensus <= 0 || activeBarometerIntervals.Count == 0)
            return true;

        // Determine the signal interval duration for comparison
        if (!GlobalData.IntervalListPeriod.TryGetValue(signalIntervalPeriod, out CryptoInterval? signalInterval))
            return true; // Unknown signal interval - skip check

        // Only include active barometers with a higher duration than the signal interval
        // (and, since 27-09-2026, only those that rest on enough coins to be a market - open point 40)
        List<CryptoIntervalPeriod> higherIntervals = activeBarometerIntervals.Keys
            .Where(p => GlobalData.IntervalListPeriod.TryGetValue(p, out CryptoInterval? bInterval) &&
                        bInterval!.Duration > signalInterval.Duration &&
                        !TooFewSymbols(activeExchange.Data.GetBarometer(quoteName, p)))
            .ToList();

        // If fewer higher intervals are available than required, the check cannot be satisfied - skip it
        if (higherIntervals.Count == 0 || minConsensus > higherIntervals.Count)
            return true;

        int count = 0;
        foreach (CryptoIntervalPeriod period in higherIntervals)
        {
            CryptoBarometerData? barometerData = activeExchange.Data.GetBarometer(quoteName, period);
            if (barometerData?.PriceBarometer.HasValue == true)
            {
                if (side == CryptoTradeSide.Long && barometerData.PriceBarometer.Value > 0)
                    count++;
                else if (side == CryptoTradeSide.Short && barometerData.PriceBarometer.Value < 0)
                    count++;
            }
        }

        if (count >= minConsensus)
            return true;

        reaction = $"Barometer consensus {count}/{higherIntervals.Count} < {minConsensus}";
        return false;
    }

}
