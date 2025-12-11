using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

using System;
using System.Globalization;

using CryptoScanner.Symbol.Model;

namespace CryptoScanner.Symbol.Converters
{
    public class SymbolVolumeColorConverter : IValueConverter
    {
        private static IBrush? GetBrushResource(string key)
        {
            if (Application.Current != null &&
                Application.Current.TryGetResource(key,
                    Application.Current.ActualThemeVariant, out var resource))
            {
                return resource as IBrush;
            }
            return null;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Cast naar SymbolInfo - nu met directe property access!
            if (value is not SymbolInfo symbolInfo)
                return GetBrushResource("NormalVolumeBrush");

            // Direct toegang tot alle properties
            var volume = symbolInfo.Volume;
            //var name = symbolInfo.Symbol;
            //var distance = symbolInfo.Distance;
            //var id = symbolInfo.Id;

            // Voorbeeld met meerdere condities:
            // if (volume < 5000000m && distance > 10)
            //     return GetBrushResource("WarningBrush");
            // else if (name.Contains("BTC") && volume < 20000000m)
            //     return GetBrushResource("LowVolumeBrush");

            // Huidige logica: volumes onder 10 miljoen zijn rood
            return volume < 10000000m
                ? GetBrushResource("LowVolumeBrush")
                : GetBrushResource("NormalVolumeBrush");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}