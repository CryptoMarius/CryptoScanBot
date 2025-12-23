using Avalonia.Data.Converters;

using CryptoScanner.Core.Enums;

namespace CryptoScanner.Converters;

public class SignalStatusColorConverter : ColorConverter, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // Display the price using the symbol's price format
        if (value is CryptoSignalStatus status)
        {
            switch (status)
            {
                case CryptoSignalStatus.Lost:
                    return GetBrushResource("PriceDownBrush");
                case CryptoSignalStatus.Win:
                    return GetBrushResource("PriceUpBrush");
                case CryptoSignalStatus.Run:
                    return GetBrushResource("PriceNeutralBrush");
            }
        }
        return GetBrushResource("PriceNeutralBrush");
    }

}