using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Intern;

public static class BarometerHelper
{

    public static bool ValidBarometerConditions(CryptoQuoteData quoteData, Dictionary<CryptoIntervalPeriod, (decimal minValue, decimal maxValue)> barometer, out string reaction)
    {
        foreach (KeyValuePair<CryptoIntervalPeriod, (decimal, decimal)> item in barometer)
        {
            if (!SymbolTools.CheckValidBarometer(quoteData, item.Key, item.Value, out reaction))
            {
                return false;
            }
        }

        reaction = "";
        return true;
    }

}
