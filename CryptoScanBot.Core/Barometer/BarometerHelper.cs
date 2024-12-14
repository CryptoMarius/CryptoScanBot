using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Barometer;

public static class BarometerHelper
{

    public static bool CheckValidBarometer(CryptoAccount account, string quoteName, CryptoIntervalPeriod intervalPeriod, (decimal minValue, decimal maxValue) values, out string reaction)
    {
        if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval? interval))
        {
            reaction = $"Interval {intervalPeriod} does not exist"; // impossible but voila
            return false;
        }

        // We gaan ervan uit dat alles in 1x wordt berekend
        BarometerData? barometerData = account.Data.GetBarometer(quoteName, intervalPeriod);
        if (!barometerData.PriceBarometer.HasValue)
        {
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


    public static bool ValidBarometerConditions(CryptoAccount account, string quoteName, Dictionary<CryptoIntervalPeriod, (decimal minValue, decimal maxValue)> barometer, out string reaction)
    {
        foreach (KeyValuePair<CryptoIntervalPeriod, (decimal, decimal)> item in barometer)
        {
            if (!CheckValidBarometer(account, quoteName, item.Key, item.Value, out reaction))
                return false;
        }

        reaction = "";
        return true;
    }

}
