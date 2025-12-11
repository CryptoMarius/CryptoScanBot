using CryptoScanner.Core.Model;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CryptoScanner.Symbol.Model
{
    public class SymbolInfo : INotifyPropertyChanged
    {
        public required CryptoSymbol SymbolObject { get; set; }

        public int Id { get => SymbolObject.Id; set { } }
        public string Symbol { get => SymbolObject.Name; set { } }
        public decimal Volume { get => SymbolObject.Volume; set { } }

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