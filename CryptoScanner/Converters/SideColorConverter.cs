using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

using System;
using System.Globalization;

namespace CryptoScanner.Converters;

public class SideColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is true ? Brushes.LimeGreen : Brushes.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}