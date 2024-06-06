using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Barometer;

public static class BarometerHelper
{

    public static bool CheckValidBarometer(CryptoQuoteData quoteData, CryptoIntervalPeriod intervalPeriod, (decimal minValue, decimal maxValue) values, out string reaction)
    {
        if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval? interval))
        {
            reaction = $"Interval {intervalPeriod} bestaat niet";
            return false;
        }

        // We gaan ervan uit dat alles in 1x wordt berekend
        BarometerData barometerData = quoteData.BarometerList[intervalPeriod];
        if (!barometerData.PriceBarometer.HasValue)
        {
            reaction = $"Barometer {interval.Name} not calculated";
            return false;
        }

        barometerData = quoteData.BarometerList[intervalPeriod];
        if (!barometerData.PriceBarometer.IsBetween(values.minValue, values.maxValue))
        {
            string minValueStr = values.minValue.ToString0("N2");
            if (values.minValue == decimal.MinValue)
                minValueStr = "-maxint";
            string maxValueStr = values.maxValue.ToString0("N2");
            if (values.maxValue == decimal.MaxValue)
                maxValueStr = "+maxint";
            reaction = $"Barometer {interval.Name} {barometerData.PriceBarometer?.ToString0("N2")} niet tussen {minValueStr} en {maxValueStr}";
            return false;
        }


        reaction = "";
        return true;
    }


    public static bool ValidBarometerConditions(CryptoQuoteData quoteData, Dictionary<CryptoIntervalPeriod, (decimal minValue, decimal maxValue)> barometer, out string reaction)
    {
        foreach (KeyValuePair<CryptoIntervalPeriod, (decimal, decimal)> item in barometer)
        {
            if (!CheckValidBarometer(quoteData, item.Key, item.Value, out reaction))
            {
                return false;
            }
        }

        reaction = "";
        return true;
    }

}
