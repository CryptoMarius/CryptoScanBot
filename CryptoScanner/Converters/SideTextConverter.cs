using Avalonia.Data.Converters;

using CryptoScanner.Core.Enums;
using CryptoScanner.Signal.Model;

namespace CryptoScanner.Converters;

public class SideTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // Display the price using the symbol's price format
        if (value is CryptoTradeSide side)
        {
            return side == CryptoTradeSide.Long ? "long" : "short";
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // ConvertBack not needed for OneWay binding
        throw new NotImplementedException();
    }
}