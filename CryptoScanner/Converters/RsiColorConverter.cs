using Avalonia.Data.Converters;

using CryptoScanner.Core.Core;

using System.Globalization;

namespace CryptoScanner.Converters
{
    public class RsiColorConverter : ColorConverter, IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double rsiValue)
            {
                if (rsiValue < GlobalData.Settings.General.SettingsRsi.Oversold)
                    return GetBrushResource("PriceUpBrush");
                else if (rsiValue > GlobalData.Settings.General.SettingsRsi.Overbought)
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