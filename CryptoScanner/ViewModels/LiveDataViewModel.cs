using CryptoScanner.Core.Model;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CryptoScanner.ViewModels;

public class LiveDataViewModel : INotifyPropertyChanged
{
    public required CryptoLiveData Object { get; set; }

    //public int Id { get => Object.Id; set { }}

    public string Date
    {
        get
        {
            var closeData = Object.Candle.Date.AddMilliseconds(Object.Interval.Duration);
            return Object.Candle.Date.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + closeData.ToLocalTime().ToString("HH:mm");
        }
    }
    public string Exchange { get => Object.Symbol.Exchange.Name; set { } }
    public string Symbol { get => Object.Symbol.Name; set { } }
    public string Interval { get => Object.Interval.Name; set { } }
    public decimal Price { get => Object.Candle.Close; set { } }
    public decimal Volume { get => Object.Symbol.Volume; set { } }

    public double? BB { get => Object.Candle.CandleData!.BollingerBandsPercentage; set { } }
    public double? BbLower { get => Object.Candle.CandleData!.BollingerBandsLowerBand; set { } }
    public double? BbUpper { get => Object.Candle.CandleData!.BollingerBandsUpperBand; set { } }

    public double? Rsi { get => Object.Candle.CandleData!.Rsi; set { } }
    public int LuxIndicator5m { get => Object.Candle.CandleData!.Lux5mValue; set { } }

    public double? MacdValue { get => Object.Candle.CandleData!.MacdValue; set { } }
    public double? MacdSignal { get => Object.Candle.CandleData!.MacdSignal; set { } }
    public double? MacdHistogram { get => Object.Candle.CandleData!.MacdHistogram; set { } }
    public double? StochOscillator { get => Object.Candle.CandleData!.StochOscillator; set { } }
    public double? StochSignal { get => Object.Candle.CandleData!.StochSignal; set { } }
    public double? Sma200 { get => Object.Candle.CandleData!.Sma200; set { } }
    public double? Sma50 { get => Object.Candle.CandleData!.Sma50; set { } }
    public double? Sma20 { get => Object.Candle.CandleData!.Sma20; set { } }
    public double? PSar { get => Object.Candle.CandleData!.PSar; set { } }


    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}