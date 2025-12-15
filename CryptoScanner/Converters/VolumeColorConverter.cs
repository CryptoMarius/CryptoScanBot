using Avalonia.Data.Converters;

using System.Globalization;

using CryptoScanner.Symbol.Model;
using CryptoScanner.Signal.Model;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Converters
{
    public class VolumeColorConverter : ColorConverter, IValueConverter
    {
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

            return GetBrushResource(volume < symbol.QuoteData.MinimalVolume ? "LowVolumeBrush" : "NormalVolumeBrush");
        }
    }
}