using Avalonia.Data.Converters;
using Avalonia.Media;

using System.Globalization;

namespace CryptoScanner.Converters;

public class BarometerColorConverter : IValueConverter
{
    internal static IBrush? GetBrushResource(string key) => App.GetBrushResource(key);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConvertBack not needed for OneWay binding
        throw new NotImplementedException();
    }

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