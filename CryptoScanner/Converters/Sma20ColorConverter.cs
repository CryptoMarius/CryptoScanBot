using Avalonia.Data.Converters;

using CryptoScanner.Core.Enums;
using CryptoScanner.ViewModels;

using System.Globalization;

namespace CryptoScanner.Converters
{
    public class Sma20ColorConverter : ColorConverter, IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SignalViewModel signal)
            {
                if (signal.Side == CryptoTradeSide.Long)
                {
                    if (signal.Sma20 > signal.Sma50)
                        return GetBrushResource("PriceUpBrush");
                    else if (signal.Sma20 > signal.Sma50)
                        return GetBrushResource("PriceDownBrush");
                }
                else if (signal.Side == CryptoTradeSide.Short)
                {
                    if (signal.Sma20 < signal.Sma50)
                        return GetBrushResource("PriceUpBrush");
                    else if (signal.Sma20 > signal.Sma50)
                        return GetBrushResource("PriceDownBrush");
                }
            }
            return GetBrushResource("PriceNeutralBrush");
        }
    }
}