using Avalonia.Data.Converters;

using System.Globalization;

namespace CryptoScanner.Converters
{
    public class RsiColorConverter : ColorConverter, IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double rsiValue)
            {
                if (rsiValue < 30)
                    return GetBrushResource("PriceUpBrush");
                else if (rsiValue > 70)
                    return GetBrushResource("PriceDownBrush");
                else
                    return GetBrushResource("PriceNeutralBrush");
            }
            else
            {
                return GetBrushResource("PriceNeutralBrush");
            }

        }
    }
}