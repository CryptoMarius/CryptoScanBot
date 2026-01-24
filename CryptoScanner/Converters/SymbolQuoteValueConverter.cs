using Avalonia.Data.Converters;

using System.Globalization;
using CryptoScanner.Core.Core;
using CryptoScanner.ViewModels;

namespace CryptoScanner.Converters
{
    // Price displayed in base
    public class SymbolQuoteValueConverter : ColorConverter, IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] == null || values[1] == null)
                return "null";

            // Display the first parameter formatted using the symbol's price format
            if (values[0] is decimal price)
            {
                var value = values[1];
                if (value is SignalViewModel signal)
                {
                    return price.ToString0(signal.Object.Symbol.QuoteData.DisplayFormat);
                }
                else if (value is LiveDataViewModel livedata)
                {
                    return price.ToString0(livedata.Object.Symbol.QuoteData.DisplayFormat);
                }
                else if (value is PositionViewModel position)
                {
                    return price.ToString0(position.Object.Symbol.QuoteData.DisplayFormat);
                }
                else return price.ToString0("N2");
            }
            else if (values[0] is double price2)
            {
                var value = values[1];
                if (value is SignalViewModel signal)
                {
                    return price2.ToString0(signal.Object.Symbol.QuoteData.DisplayFormat);
                }
                else if (value is LiveDataViewModel livedata)
                {
                    return price2.ToString0(livedata.Object.Symbol.QuoteData.DisplayFormat);
                }
                else if (value is PositionViewModel position)
                {
                    return price2.ToString0(position.Object.Symbol.QuoteData.DisplayFormat);
                }
                else return price2.ToString0("N2");
            }

            return "?";
        }
    }
}