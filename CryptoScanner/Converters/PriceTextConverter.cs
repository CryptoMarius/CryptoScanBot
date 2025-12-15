using Avalonia.Data.Converters;

using CryptoScanner.Core.Core;
using CryptoScanner.Signal.Model;

namespace CryptoScanner.Converters;

public class PriceTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // Display the price using the symbol's price format
        if (value is SignalInfo signalInfo)
        {
            decimal price = signalInfo.SignalPrice;
            return price.ToString0(signalInfo.SignalObject.Symbol.PriceDisplayFormat);
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // ConvertBack not needed for OneWay binding
        throw new NotImplementedException();
    }
}