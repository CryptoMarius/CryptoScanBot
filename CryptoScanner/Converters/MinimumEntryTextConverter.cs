using Avalonia.Data.Converters;

using CryptoScanner.Core.Core;
using CryptoScanner.ViewModels;

namespace CryptoScanner.Converters;

public class MinimumEntryTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // Display the quantity using the symbol's quantity format
        if (value is SignalViewModel signalInfo)
        {
            decimal minEntry = signalInfo.MinEntry;
            if (minEntry == 0)
                return "";
            return minEntry.ToString0(signalInfo.Object.Symbol.QuantityDisplayFormat);
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // ConvertBack not needed for OneWay binding
        throw new NotImplementedException();
    }
}