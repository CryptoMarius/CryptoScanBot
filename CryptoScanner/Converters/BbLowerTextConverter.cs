using Avalonia.Data.Converters;

using CryptoScanner.ViewModels;

namespace CryptoScanner.Converters;

public class BbLowerTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // Display the price using the symbol's price format
        if (value is SignalViewModel signalInfo)
        {
            var price = signalInfo.BbLower;
            return price?.ToString(signalInfo.Object.Symbol.PriceDisplayFormat);
        }
        else if (value is LiveDataViewModel liveDataInfo)
        {
            var price = liveDataInfo.BbLower;
            return price?.ToString(liveDataInfo.Object.Symbol.PriceDisplayFormat);
        }
        else if (value is PositionViewModel positionInfo)
        {
            var price = positionInfo.BbLower;
            return price?.ToString(positionInfo.Object.Symbol.PriceDisplayFormat);
        }

        return "?";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // ConvertBack not needed for OneWay binding
        throw new NotImplementedException();
    }
}