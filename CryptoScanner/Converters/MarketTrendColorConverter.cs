using Avalonia.Data.Converters;

namespace CryptoScanner.Converters;

public class MarketTrendColorConverter : ColorConverter, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is float marketTrend)
        {
            if (marketTrend == 0)
            {
                return GetBrushResource("PriceNeutralBrush");
            }
            else
            {
                return GetBrushResource(marketTrend < 0 ? "PriceUpBrush" : "PriceDownBrush");
            }
        }
        else
        {
            return GetBrushResource("PriceNeutralBrush");
        }
    }
}