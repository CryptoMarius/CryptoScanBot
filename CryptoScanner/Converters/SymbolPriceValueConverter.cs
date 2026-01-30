using Avalonia.Data.Converters;

using CryptoScanner.Core.Core;
using CryptoScanner.ViewModels;

using System.Globalization;

namespace CryptoScanner.Converters
{
    // Price displayed in base
    public class SymbolPriceValueConverter : ColorConverter, IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] == null || values[1] == null)
                return "";

            // Display the first parameter formatted using the symbol's price format
            if (values[0] is decimal price)
            {
                var value = values[1];
                if (value is SignalViewModel signal)
                {
                    return price.ToString0(signal.Object.Symbol.PriceDisplayFormat);
                }
                else if (value is LiveDataViewModel livedata)
                {
                    return price.ToString0(livedata.Object.Symbol.PriceDisplayFormat);
                }
                else if (value is PositionViewModel position)
                {
                    return price.ToString0(position.Object.Symbol.PriceDisplayFormat);
                }
                else return price.ToString0("N2");
            }
            else if (values[0] is double price2)
            {
                var value = values[1];
                if (value is SignalViewModel signal)
                {
                    return price2.ToString0(signal.Object.Symbol.PriceDisplayFormat);
                }
                else if (value is LiveDataViewModel livedata)
                {
                    return price2.ToString0(livedata.Object.Symbol.PriceDisplayFormat);
                }
                else if (value is PositionViewModel position)
                {
                    return price2.ToString0(position.Object.Symbol.PriceDisplayFormat);
                }
                else return price2.ToString0("N2");
            }

            return "?";
        }
    }
}