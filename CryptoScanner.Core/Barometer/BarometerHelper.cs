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
        BarometerData? barometerData = activeExchange.Data.GetBarometer(quoteName, intervalPeriod);
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

}
