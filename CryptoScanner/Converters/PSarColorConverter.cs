using Avalonia.Data.Converters;

using CryptoScanner.Core.Enums;
using CryptoScanner.ViewModels;

using System.Globalization;

namespace CryptoScanner.Converters
{
    public class PSarColorConverter : ColorConverter, IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SignalViewModel signal)
            {
                if (signal.Side == CryptoTradeSide.Long)
                {
                    if (signal.PSar <= signal.Sma20)
                        return GetBrushResource("PriceUpBrush");
                    else if (signal.PSar > signal.Sma20)
                        return GetBrushResource("PriceDownBrush");
                }
                else if (signal.Side == CryptoTradeSide.Short)
                {
                    if (signal.PSar >= signal.Sma20)
                        return GetBrushResource("PriceUpBrush");
                    else if (signal.PSar < signal.Sma20)
                        return GetBrushResource("PriceDownBrush");
                }
            }
            return GetBrushResource("PriceNeutralBrush");
        }
    }
}