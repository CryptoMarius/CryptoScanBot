using Avalonia.Data.Converters;

using CryptoScanner.Core.Enums;

namespace CryptoScanner.Converters;

public class TrendColorConverter : ColorConverter, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is CryptoTrendIndicator trend)
        {
            return GetBrushResource(trend == CryptoTrendIndicator.Bullish ? "PriceUpBrush" : "PriceDownBrush");
        }
        else
        {
            return GetBrushResource("PriceNeutralBrush");
        }
    }
}
