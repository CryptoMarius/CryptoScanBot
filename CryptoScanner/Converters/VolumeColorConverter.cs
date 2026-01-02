using Avalonia.Data.Converters;

using System.Globalization;
using CryptoScanner.Model;
using CryptoScanner.Core.Model;
using CryptoScanner.ViewModels;

namespace CryptoScanner.Converters
{
    public class VolumeColorConverter : ColorConverter, IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            decimal volume;
            CryptoSymbol? symbol;

            // This converter is called from a couple of different views
            if (value is SignalViewModel signalInfo)
            {
                volume = signalInfo.SignalVolume;
                symbol = signalInfo.Object.Symbol;
            }
            else if (value is SymbolViewModel symbolInfo)
            {
                symbol = symbolInfo.Object;
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