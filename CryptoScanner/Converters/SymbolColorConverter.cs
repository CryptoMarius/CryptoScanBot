using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

using CryptoScanner.ViewModels;

using System.Globalization;

namespace CryptoScanner.Converters
{
    // Background symbol based on the quote
    public class SymbolColorConverter : ColorConverter, IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            //Avalonia 11 used the folloowing resource‑keys for DataGrid‑selection:
            //Selectie‑achtergrond DataGridRowSelectedBackground
            //Selectie‑foreground DataGridRowSelectedForeground
            //Hover‑kleur DataGridRowPointerOverBackground

            var isSelected = values[1] as bool? ?? false;
            if (isSelected)
                return Application.Current?.FindResource("DataGridRowSelectedBackground");

            var isHover = values[2] as bool? ?? false;
            if (isHover) 
                return Application.Current?.FindResource("DataGridRowPointerOverBackground");

            var value = values[0];
            if (value is SignalViewModel signal)
            {
                return new SolidColorBrush(signal.Object.Symbol.QuoteData.DisplayColor);
            }
            else if (value is SymbolViewModel symbol)
            {
                return new SolidColorBrush(symbol.Object.QuoteData.DisplayColor);
            }
            else if (value is LiveDataViewModel liveData)
            {
                return new SolidColorBrush(liveData.Object.Symbol.QuoteData.DisplayColor);
            }
            else if (value is PositionViewModel position)
            {
                return new SolidColorBrush(position.Object.Symbol.QuoteData.DisplayColor);
            }

            return Brushes.Transparent;
        }

        
    }
}