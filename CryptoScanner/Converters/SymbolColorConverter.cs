using Avalonia.Data.Converters;
using Avalonia.Media;

using CryptoScanner.Signal.Model;

using System.Globalization;

namespace CryptoScanner.Converters
{
    // Background symbol based on the quote
    public class SymbolColorConverter : ColorConverter, IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SignalInfo signal)
            {
                //if (signal.SignalObject.Symbol.QuoteData.DisplayColor != Color.tra)
                        return signal.SignalObject.Symbol.QuoteData.DisplayColor;
            }
            return GetBrushResource("SystemControlBackgroundChromeMediumBrush");
        }
    }
}