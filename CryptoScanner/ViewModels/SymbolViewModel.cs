using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Model;

namespace CryptoScanner.ViewModels
{
    public class SymbolViewModel : ObservableObject
    {
        public required CryptoSymbol Object { get; set; }

        public int Id { get => Object.Id; set { } }
        public string Symbol { get => Object.Name; set { } }
        public decimal Volume { get => Object.Volume; set { } }

        private decimal? _distance = 100.0m;
        public decimal? Distance
        {
            get => _distance;
            set { 
                _distance = value; 
                OnPropertyChanged();
            }
        }

    }
}