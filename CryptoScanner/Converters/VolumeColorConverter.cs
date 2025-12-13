using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

using System.Globalization;

using CryptoScanner.Symbol.Model;
using CryptoScanner.Signal.Model;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Converters
{
    public class VolumeColorConverter : IValueConverter
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
            decimal volume;
            CryptoSymbol? symbol;

            // This converter is called from a couple of different views
            if (value is SignalInfo signalInfo)
            {
                volume = signalInfo.SignalVolume;
                symbol = signalInfo.SignalObject.Symbol;
            }
            else if (value is SymbolInfo symbolInfo)
            {
                symbol = symbolInfo.SymbolObject;
                volume = symbol.Volume;
            }
            else
            {
                return GetBrushResource("NormalVolumeBrush");
            }

            if (symbol == null)
                return GetBrushResource("NormalVolumeBrush");

            // Huidige logica: volumes onder 10 miljoen zijn rood
            return volume < symbol.QuoteData.MinimalVolume
                ? GetBrushResource("LowVolumeBrush")
                : GetBrushResource("NormalVolumeBrush");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}