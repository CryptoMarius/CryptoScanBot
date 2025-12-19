using Avalonia.Media;

using System.Globalization;

namespace CryptoScanner.Converters
{
    public class ColorConverter 
    {
        internal static IBrush? GetBrushResource(string key) => App.GetBrushResource(key);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // ConvertBack not needed for OneWay binding
            throw new NotImplementedException();
        }
    }
}