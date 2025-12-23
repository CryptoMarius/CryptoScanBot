using Avalonia.Data.Converters;

using CryptoScanner.Core.Core;

using System.Globalization;

namespace CryptoScanner.Converters
{
    public class StochColorConverter : ColorConverter, IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double stochValue)
            {
                if (stochValue < GlobalData.Settings.General.SettingsStoch.Oversold)
                    return GetBrushResource("PriceUpBrush");
                else if (stochValue > GlobalData.Settings.General.SettingsStoch.Overbought)
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