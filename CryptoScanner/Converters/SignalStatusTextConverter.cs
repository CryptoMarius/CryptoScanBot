using Avalonia.Data.Converters;

using CryptoScanner.Core.Enums;

namespace CryptoScanner.Converters;

public class SignalStatusTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // Display the price using the symbol's price format
        if (value is CryptoSignalStatus status)
        {
            switch (status)
            {
                case CryptoSignalStatus.Lost:
                    return "lost";
                case CryptoSignalStatus.Win:
                    return "win";
                case CryptoSignalStatus.Run:
                    return "run";
            }
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // ConvertBack not needed for OneWay binding
        throw new NotImplementedException();
    }
}