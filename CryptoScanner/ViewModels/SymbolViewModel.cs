using Avalonia.Media;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;
using CryptoScanner.Services;

namespace CryptoScanner.ViewModels
{
    public class SymbolViewModel : BaseConvertersViewModel
    {
        public required CryptoSymbol Object { get; set; }

        //public int Id { get => Object.Id; set { } }
        private string? _IdText;
        public string Id
        {
            get
            {
                _IdText ??= Object.Id.ToString();
                return _IdText!;
            }
        }

        //public string Symbol { get => Object.Name; set { } }
        private string? _SymbolText;
        public string Symbol
        {
            get
            {
                _SymbolText ??= Object.Name;
                return _SymbolText!;
            }
        }
        private IBrush? _SymbolBackground;
        public IBrush SymbolBackground
        {
            get
            {
                _SymbolBackground ??= new SolidColorBrush(Object.QuoteData.DisplayColor.ToAvaloniaColor());
                return _SymbolBackground!;
            }
        }


        //public decimal Volume { get => Object.Volume; set { } }
        private string? _VolumeText;
        public string Volume
        {
            get
            {
                _VolumeText ??= Object.Volume.ToString("N0");
                return _VolumeText!;
            }
            set
            {
                _VolumeText = null;
                _VolumeForeground = null;
                OnPropertyChanged(nameof(Volume));
                OnPropertyChanged(nameof(VolumeForeground));
            }
        }
        private IBrush? _VolumeForeground;
        public IBrush VolumeForeground
        {
            get
            {
                _VolumeForeground ??= GetVolumeColor(Object, Object.Volume);
                return _VolumeForeground!;
            }
        }

        //private decimal? _distance = 100.0m;
        //public decimal? Distance
        //{
        //    get => _distance;
        //    set { 
        //        _distance = value; 
        //        OnPropertyChanged();
        //    }
        //}
        private string? _DistanceText = "100";
        public string Distance
        {
            get
            {
                _DistanceText ??= ZoneTools.ZoneDistance(Object).ToString0("N2");
                return _DistanceText!;
            }
            set
            {
                _DistanceText = null;
                OnPropertyChanged(nameof(Distance));
            }
        }
    }
}