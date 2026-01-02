using Avalonia.Data.Converters;

using CryptoScanner.Core.Core;
using CryptoScanner.Model;
using CryptoScanner.ViewModels;

namespace CryptoScanner.Converters;

public class PriceTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // Display the price using the symbol's price format
        if (value is SignalViewModel signalInfo)
        {
            decimal price = signalInfo.SignalPrice;
            return price.ToString0(signalInfo.Object.Symbol.PriceDisplayFormat);
        }
        else if (value is LiveDataViewModel liveDataInfo)
        {
            decimal price = liveDataInfo.Price;
            return price.ToString0(liveDataInfo.Object.Symbol.PriceDisplayFormat);
        }
        else if (value is decimal valueDecimal)
        {
            return valueDecimal.ToString0("N2");
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // ConvertBack not needed for OneWay binding
        throw new NotImplementedException();
    }
}