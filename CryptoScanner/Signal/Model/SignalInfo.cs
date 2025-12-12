using CryptoScanner.Core.Model;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CryptoScanner.Signal.Model
{
    public class SignalInfo : INotifyPropertyChanged
    {
        public required CryptoSignal SignalObject { get; set; }

        public int Id { get => SignalObject.Id; set { }}

        public string Date { get => SignalObject.OpenDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + SignalObject.CloseDate.ToLocalTime().ToString("HH:mm"); set { } }
        public string Exchange { get => SignalObject.Exchange.Name; set { } }
        public string Symbol { get => SignalObject.Symbol.Name; set { }}
        public string Side { get => SignalObject.SideText; set { } }
        public string Interval { get => SignalObject.Interval.Name; set { }}
        public string Strategy { get => SignalObject.StrategyText; set { } }
        public decimal SignalPrice { get => SignalObject.SignalPrice; set { } }
        public decimal SignalVolume { get => SignalObject.SignalVolume; set { }}
        public double PriceChange { get => SignalObject.Last24HoursChange; set { } }
        public string? Text { get => SignalObject.EventText; set { } }
        public string TfTrend { get => SignalObject.TrendInterval.ToString(); set { } }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}