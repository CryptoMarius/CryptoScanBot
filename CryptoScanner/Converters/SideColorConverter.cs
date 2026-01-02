using Avalonia.Data.Converters;

using CryptoScanner.Core.Enums;
using CryptoScanner.ViewModels;

namespace CryptoScanner.Converters;

public class SideColorConverter : ColorConverter, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is CryptoTradeSide side)
        {
            return GetBrushResource(side == CryptoTradeSide.Long ? "PriceUpBrush" : "PriceDownBrush");
        }
        else if (value is SignalViewModel signalInfo)
        {
            return GetBrushResource(signalInfo.Side == CryptoTradeSide.Long ? "PriceUpBrush" : "PriceDownBrush");
        }
        else
        {
            return GetBrushResource("PriceNeutralBrush");
        }
    }
}
