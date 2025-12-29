using CryptoScanner.Core.Model;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CryptoScanner.LiveData.Model
{
    public class LiveDataInfo : INotifyPropertyChanged
    {
        public required CryptoLiveData LiveDataObject { get; set; }

        //public int Id { get => LiveDataObject.Id; set { }}

        public string Date { get {
                var closeData = LiveDataObject.Candle.Date.AddMilliseconds(LiveDataObject.Interval.Duration);
                return LiveDataObject.Candle.Date.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + closeData.ToLocalTime().ToString("HH:mm"); }
             }
        public string Exchange { get => LiveDataObject.Symbol.Exchange.Name; set { } }
        public string Symbol { get => LiveDataObject.Symbol.Name; set { }}
        public string Interval { get => LiveDataObject.Interval.Name; set { }}
        public decimal Price { get => LiveDataObject.Candle.Close; set { } }
        public decimal Volume { get => LiveDataObject.Symbol.Volume; set { }}

        public double? BB { get => LiveDataObject.Candle.CandleData!.BollingerBandsPercentage; set { } }
        public double? BbLower { get => LiveDataObject.Candle.CandleData!.BollingerBandsLowerBand; set { } }
        public double? BbUpper { get => LiveDataObject.Candle.CandleData!.BollingerBandsUpperBand; set { } }

        public double? Rsi { get => LiveDataObject.Candle.CandleData!.Rsi; set { } }
        public int LuxIndicator5m { get => LiveDataObject.Candle.CandleData!.Lux5mValue; set { } }

        public double? MacdValue { get => LiveDataObject.Candle.CandleData!.MacdValue; set { } }
        public double? MacdLiveData { get => LiveDataObject.Candle.CandleData!.MacdSignal; set { } }
        public double? MacdHistogram { get => LiveDataObject.Candle.CandleData!.MacdHistogram; set { } }
        public double? StochOscillator { get => LiveDataObject.Candle.CandleData!.StochOscillator; set { } }
        public double? StochLiveData { get => LiveDataObject.Candle.CandleData!.StochSignal; set { } }
        public double? Sma200 { get => LiveDataObject.Candle.CandleData!.Sma200; set { } }
        public double? Sma50 { get => LiveDataObject.Candle.CandleData!.Sma50; set { } }
        public double? Sma20 { get => LiveDataObject.Candle.CandleData!.Sma20; set { } }
        public double? PSar { get => LiveDataObject.Candle.CandleData!.PSar; set { } }


        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}