using Avalonia.Data.Converters;

using CryptoScanner.Core.Core;
using CryptoScanner.Signal.Model;

namespace CryptoScanner.Converters;

public class MinimumEntryTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // Display the quantity using the symbol's quantity format
        if (value is SignalInfo signalInfo)
        {
            decimal minEntry = signalInfo.MinEntry;
            if (minEntry == 0)
                return "";
            return minEntry.ToString0(signalInfo.SignalObject.Symbol.QuantityDisplayFormat);
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // ConvertBack not needed for OneWay binding
        throw new NotImplementedException();
    }
}