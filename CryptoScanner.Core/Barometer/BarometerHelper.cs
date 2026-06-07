using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Barometer;

public static class BarometerHelper
{
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
        List<CryptoIntervalPeriod> higherIntervals = activeBarometerIntervals.Keys
            .Where(p => GlobalData.IntervalListPeriod.TryGetValue(p, out CryptoInterval? bInterval) &&
                        bInterval!.Duration > signalInterval.Duration)
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
