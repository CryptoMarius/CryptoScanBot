using Avalonia.Data.Converters;

namespace CryptoScanner.Converters;

public class BarometerColorConverter : ColorConverter, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is decimal valueDecimal)
        {
            if (valueDecimal == 0)
                return GetBrushResource("PriceNeutralBrush");
            else if (valueDecimal > 0)
                return GetBrushResource("PriceUpBrush");
            else
                return GetBrushResource("PriceDownBrush");
        }
        else
        {
            return GetBrushResource("PriceNeutralBrush");
        }
    }
}