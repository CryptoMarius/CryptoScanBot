using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CryptoScanner.ViewModels
{
    public class SymbolViewModel : ObservableObject
    {
        public required CryptoSymbol Object { get; set; }

        public int Id { get => Object.Id; set { } }
        public string Symbol { get => Object.Name; set { } }
        public decimal Volume { get => Object.Volume; set { } }

        //private decimal?_distance = ZoneTools.ZoneDistance(Object);
        public decimal? Distance
        {
            get => ZoneTools.ZoneDistance(Object);
            set { //_distance = value; OnPropertyChanged();
                  }
        }


        //public event PropertyChangedEventHandler? PropertyChanged;

        //protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}
    }
}