using CryptoScanner.Core.Enums;
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
        public CryptoTradeSide Side { get => SignalObject.Side; set { } }
        public string Interval { get => SignalObject.Interval.Name; set { }}
        public string Strategy { get => SignalObject.StrategyText; set { } }
        public decimal SignalPrice { get => SignalObject.SignalPrice; set { } }
        public decimal SignalVolume { get => SignalObject.SignalVolume; set { }}
        public double PriceChange { get => SignalObject.Last24HoursChange; set { } }
        public string? Text { get => SignalObject.EventText; set { } }

        public float TrendPercentagePrimary { get => SignalObject.TrendPercentagePrimary; set { } }
        public float TrendPercentageSecondary { get => SignalObject.TrendPercentageSecondary; set { } }

        public double Last24HoursChange { get => SignalObject.Last24HoursChange; set { } }
        public double LastXDaysEffective { get => SignalObject.LastXDaysEffective; set { } }

        public double AvgBB { get => SignalObject.AvgBB; set { } }
        public double? BB { get => SignalObject.BollingerBandsPercentage; set { } }
        public double? BbLower { get => SignalObject.BollingerBandsLowerBand; set { } }
        public double? BbUpper { get => SignalObject.BollingerBandsUpperBand; set { } }

        public double? Rsi { get => SignalObject.Rsi; set { } }
        public int LuxIndicator5m { get => SignalObject.LuxIndicator5m; set { } }

        public double? MacdValue { get => SignalObject.MacdValue; set { } }
        public double? MacdSignal { get => SignalObject.MacdSignal; set { } }
        public double? MacdHistogram { get => SignalObject.MacdHistogram; set { } }
        public double? StochOscillator { get => SignalObject.StochOscillator; set { } }
        public double? StochSignal { get => SignalObject.StochSignal; set { } }
        public double? Sma200 { get => SignalObject.Sma200; set { } }
        public double? Sma50 { get => SignalObject.Sma50; set { } }
        public double? Sma20 { get => SignalObject.Sma20; set { } }
        public double? PSar { get => SignalObject.PSar; set { } }

        public CryptoTrendIndicator TfTrend { get => SignalObject.TrendInterval; set { } }
        public CryptoTrendIndicator? Trend15m { get => SignalObject.Trend15m; set { } }
        public CryptoTrendIndicator? Trend30m { get => SignalObject.Trend30m; set { } }
        public CryptoTrendIndicator? Trend1h { get => SignalObject.Trend1h; set { } }
        public CryptoTrendIndicator? Trend4h { get => SignalObject.Trend4h; set { } }
        public CryptoTrendIndicator? Trend1d { get => SignalObject.Trend1d; set { } }

        public decimal? Barometer15m { get => SignalObject.Barometer15m; set { } }
        public decimal? Barometer30m { get => SignalObject.Barometer30m; set { } }
        public decimal? Barometer1h { get => SignalObject.Barometer1h; set { } }
        public decimal? Barometer4h { get => SignalObject.Barometer4h; set { } }
        public decimal? Barometer1d { get => SignalObject.Barometer1d; set { } }

        public decimal MinEntry { get => SignalObject.MinEntry; set { } }
        public double PriceMinPerc { get => SignalObject.PriceMinPerc; set { } }
        public double PriceMaxPerc{ get => SignalObject.PriceMaxPerc; set { } }
        public CryptoSignalStatus SignalStatus{ get => SignalObject.SignalStatus; set { } }


        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}