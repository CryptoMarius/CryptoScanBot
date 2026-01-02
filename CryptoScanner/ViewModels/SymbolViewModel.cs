using CryptoScanner.Core.Model;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CryptoScanner.ViewModels
{
    public class SymbolViewModel : INotifyPropertyChanged
    {
        public required CryptoSymbol Object { get; set; }

        public int Id { get => Object.Id; set { } }
        public string Symbol { get => Object.Name; set { } }
        public decimal Volume { get => Object.Volume; set { } }

        private double _distance;
        public double Distance
        {
            get => _distance;
            set { _distance = value; OnPropertyChanged(); }
        }

        // Voeg andere properties toe uit je repo (bijv. voor exchanges)

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}