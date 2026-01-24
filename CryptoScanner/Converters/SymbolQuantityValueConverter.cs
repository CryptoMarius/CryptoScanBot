using Avalonia.Data.Converters;

using System.Globalization;
using CryptoScanner.Core.Core;

namespace CryptoScanner.Converters
{
    public class SymbolQuantityValueConverter : ColorConverter, IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] == null || values[1] == null)
                return "";

            // Display the first parameter formatted using the symbol's quantity format

            // problem: The Symbol.QuantityDisplayFormat is not property set, so just display the whole number
            // (perhaps it is property set, but for dust we need the exact number (seems to be a problemen somewhere)

            if (values[0] is decimal quantity1)
            {
                //var value = values[1];
                //if (value is SignalViewModel signal)
                //{
                //    return quantity1.ToString0();
                //}
                //else if (value is LiveDataViewModel livedata)
                //{
                //    return quantity1.ToString0(livedata.Object.Symbol.QuantityDisplayFormat);
                //}
                //else if (value is PositionViewModel position)
                //{
                //    return quantity1.ToString0(position.Object.Symbol.QuantityDisplayFormat);
                //}
                //else return quantity1.ToString0("N2");
                return quantity1.ToString0();
            }
            else if (values[0] is double quantity2)
            {
                //var value = values[1];
                //if (value is SignalViewModel signal)
                //{
                //    return quantity2.ToString0(signal.Object.Symbol.QuantityDisplayFormat);
                //}
                //else if (value is LiveDataViewModel livedata)
                //{
                //    return quantity2.ToString0(livedata.Object.Symbol.QuantityDisplayFormat);
                //}
                //else if (value is PositionViewModel position)
                //{
                //    return quantity2.ToString0(position.Object.Symbol.QuantityDisplayFormat);
                //}
                //else return quantity2.ToString0("N2");
                return quantity2.ToString0();
            }

            return "?";
        }
    }
}