using Avalonia.Data.Converters;

namespace CryptoScanner.Converters;

// wrong name, just < 0 = red or > 0 green and 0 = neutral
// ValueSignColorConverter (sounds as the best neutral name)
public class ValueSignColorConverter : ColorConverter, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is float floatValue)
        {
            if (floatValue == 0)
                return GetBrushResource("PriceNeutralBrush");
            else if (floatValue > 0)
                return GetBrushResource("PriceUpBrush");
            else
                return GetBrushResource("PriceDownBrush");
        }
        else if (value is decimal decimalValue)
        {
            if (decimalValue == 0)
                return GetBrushResource("PriceNeutralBrush");
            else if (decimalValue > 0)
                return GetBrushResource("PriceUpBrush");
            else
                return GetBrushResource("PriceDownBrush");
        }
        else if (value is double doubleValue)
        {
            if (doubleValue == 0)
                return GetBrushResource("PriceNeutralBrush");
            else if (doubleValue > 0)
                return GetBrushResource("PriceUpBrush");
            else
                return GetBrushResource("PriceDownBrush");
        }
        else if (value is int intValue)
        {
            if (intValue == 0)
                return GetBrushResource("PriceNeutralBrush");
            else if (intValue > 0)
                return GetBrushResource("PriceUpBrush");
            else
                return GetBrushResource("PriceDownBrush");
        }
        return GetBrushResource("PriceNeutralBrush");
    }
}