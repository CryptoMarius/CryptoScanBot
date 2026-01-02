using Avalonia.Data.Converters;
using Avalonia.Media;

using CryptoScanner.ViewModels;

using System.Globalization;

namespace CryptoScanner.Converters
{
    // Background symbol based on the quote
    public class SymbolColorConverter : ColorConverter, IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SignalViewModel signal)
            {
                //if (signal.Object.Symbol.QuoteData.DisplayColor != Color.tra)
                        return signal.Object.Symbol.QuoteData.DisplayColor;
            }
            return GetBrushResource("SystemControlBackgroundChromeMediumBrush");
        }
    }
}