//using Avalonia.Data.Converters;
//using Avalonia.Media;

//namespace CryptoScanner.Symbol.Converters;

//public class PriceChangeToColorConverter : IValueConverter
//{
//    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
//    {
//        if (value is decimal pct)
//            return pct > 0 ? Brushes.LimeGreen : pct < 0 ? Brushes.Red : Brushes.Gray;
//        return Brushes.Gray;
//    }

//    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
//}