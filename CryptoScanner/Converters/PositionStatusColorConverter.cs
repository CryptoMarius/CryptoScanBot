using Avalonia.Data.Converters;

using CryptoScanner.Core.Enums;

namespace CryptoScanner.Converters;

public class PositionStatusColorConverter : ColorConverter, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is CryptoPositionStatus status)
        {
            switch (status)
            {
                case CryptoPositionStatus.Waiting:
                    break;
                case CryptoPositionStatus.Trading:
                    break;
                case CryptoPositionStatus.Ready:
                    return GetBrushResource("PriceUpBrush");
                case CryptoPositionStatus.Timeout:
                    return GetBrushResource("PriceDownBrush");
                case CryptoPositionStatus.TakeOver:
                    return GetBrushResource("PriceDownBrush");
                case CryptoPositionStatus.Altrady:
                    break;
            }
        }
        return GetBrushResource("PriceNeutralBrush");
    }
}
