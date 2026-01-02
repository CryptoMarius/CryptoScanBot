using Avalonia.Data.Converters;

using CryptoScanner.Core.Core;
using CryptoScanner.ViewModels;

namespace CryptoScanner.Converters;

public class QuantityTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // Display the quantity using the symbol's quantity format
        if (value is PositionViewModel positionInfo)
        {
            decimal quantity = positionInfo.Quantity;
            return quantity.ToString0(positionInfo.Object.Symbol.QuantityDisplayFormat);
        }
        else if (value is decimal valueDecimal)
        {
            return valueDecimal.ToString0("N2");
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // ConvertBack not needed for OneWay binding
        throw new NotImplementedException();
    }
}