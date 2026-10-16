using Avalonia.Data.Converters;

using CryptoScanner.Core.Barometer;

using System.Globalization;

namespace CryptoScanner.Converters;

/// <summary>
/// Turns a label of the barometer graph dropdown into its explanation, for the tooltip of each item.
/// The text lives in BarometerCandleFields.GetDescription, so the Photino dashboard shows the same.
/// </summary>
public class BarometerGraphValueDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string name ? BarometerCandleFields.GetDescription(name) : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConvertBack not needed for OneWay binding
        throw new NotImplementedException();
    }
}
