using Avalonia;
using Avalonia.Media;

using System.Globalization;

namespace CryptoScanner.Converters
{
    public class ColorConverter 
    {
        internal static IBrush? GetBrushResource(string key)
        {
            if (Application.Current != null &&
                Application.Current.TryGetResource(key,
                    Application.Current.ActualThemeVariant, out var resource))
            {
                return resource as IBrush;
            }
            return null;
        }


        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // ConvertBack not needed for OneWay binding
            throw new NotImplementedException();
        }
    }
}