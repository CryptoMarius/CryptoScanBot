using Avalonia.Data.Converters;

using CryptoScanner.Core.Enums;
using CryptoScanner.LiveData.Model;
using CryptoScanner.Signal.Model;

namespace CryptoScanner.Converters;

public class Sma50TextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // Display the price using the symbol's price format
        if (value is SignalInfo signalInfo)
        {
            var price = signalInfo.Sma50;
            return price?.ToString(signalInfo.SignalObject.Symbol.PriceDisplayFormat);
        }
        else if (value is LiveDataInfo liveDataInfo)
        {
            var price = liveDataInfo.Sma50;
            return price?.ToString(liveDataInfo.LiveDataObject.Symbol.PriceDisplayFormat);
        }

        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // ConvertBack not needed for OneWay binding
        throw new NotImplementedException();
    }
}