using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.ViewModels;

public partial class PositionViewModel : ObservableObject
{
    public required CryptoPosition Object { get; set; }

    public int Id => Object.Id;
    public string? AltradyPositionId => Object.AltradyPositionId;
    public string CreateTime => Object.CreateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public string UpdateTime => Object.UpdateTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm")!;
    public string CloseTime => Object.CloseTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm")!;
    public string Duration => Object.DurationText();
    public string Exchange => Object.Exchange.Name;
    public string Symbol => Object.Symbol.Name;
    public string Interval => Object.Interval.Name;
    public string Strategy => Object.StrategyText;
    public CryptoTradeSide Side => Object.Side;
    public CryptoPositionStatus? Status => Object.Status;

    public decimal Invested => Object.Invested;
    public decimal Returned => Object.Returned;
    public decimal Commission => Object.Commission;
    public decimal TotalProfit => Object.Profit;
    public decimal CurrentProfit => Object.CurrentProfit();

    public decimal Quantity => Object.Quantity;
    public decimal Open => Object.Invested - Object.Returned - Object.Commission;
    public decimal BreakEvenPrice => Object.BreakEvenPrice;
    public decimal BreakEvenPercent => Object.CurrentBreakEvenPercentage();

    public string Parts => Object.PartCountText();
    public decimal? EntryPrice => Object.EntryPrice;
    public decimal? ProfitPrice => Object.ProfitPrice;
    public decimal CurrentProfitPercentage => Object.CurrentProfitPercentage();
    public decimal TotalPercentage => Object.Percentage;
    public decimal FundingRate => Object.Symbol.FundingRate;
    public decimal QuantityTick => Object.Symbol.QuantityTickSize;
    public decimal RemainingDust => Object.RemainingDust;
    public decimal? RemainingDustValue => Object.RemainingDust * Object.Symbol.LastPrice;

    //public decimal Reserved => Object.Reserved;
    //public decimal CurrentProfit => Object.CurrentProfit;
    //public decimal? EntryAmount => Object.EntryAmount;

    public DateTime SignalDate => Object.SignalEventTime;
    public decimal SignalPrice => Object.SignalPrice;
    public decimal SignalVolume => Object.SignalVolume;
    public double PriceChange => Object.Last24HoursChange;

    public float TrendPercentagePrimary => Object.TrendPercentagePrimary;
    public float TrendPercentageSecondary => Object.TrendPercentageSecondary;

    public double Last24HoursChange => Object.Last24HoursChange;
    public double LastXDaysEffective => Object.LastXDaysEffective;

    public double AvgBB => Object.AvgBB;
    public double? BB => Object.BollingerBandsPercentage;
    public double? BbLower => Object.BollingerBandsLowerBand;
    public double? BbUpper => Object.BollingerBandsUpperBand;

    public double? Rsi => Object.Rsi;
    public int LuxIndicator5m => Object.LuxIndicator5m;

    public double? MacdValue => Object.MacdValue;
    public double? MacdSignal => Object.MacdSignal;
    public double? MacdHistogram => Object.MacdHistogram;
    public double? StochOscillator => Object.StochOscillator;
    public double? StochSignal => Object.StochSignal;
    public double? Sma200 => Object.Sma200;
    public double? Sma50 => Object.Sma50;
    public double? Sma20 => Object.Sma20;
    public double? PSar => Object.PSar;

    public CryptoTrendIndicator TrendInterval => Object.TrendInterval;
    public CryptoTrendIndicator? Trend15m => Object.Trend15m;
    public CryptoTrendIndicator? Trend30m => Object.Trend30m;
    public CryptoTrendIndicator? Trend1h => Object.Trend1h;
    public CryptoTrendIndicator? Trend4h => Object.Trend4h;
    public CryptoTrendIndicator? Trend1d => Object.Trend1d;

    public decimal? Barometer15m => Object.Barometer15m;
    public decimal? Barometer30m => Object.Barometer30m;
    public decimal? Barometer1h => Object.Barometer1h;
    public decimal? Barometer4h => Object.Barometer4h;
    public decimal? Barometer1d => Object.Barometer1d;

    public decimal MinimumEntry => Object.MinEntry;
    public decimal PriceMin => Object.PriceMin;
    public decimal PriceMax => Object.PriceMax;
    public double PriceMinPerc => Object.PriceMinPerc;
    public double PriceMaxPerc => Object.PriceMaxPerc;

    public void NotifyColumnChanged(string column)
    {
        OnPropertyChanged(column); 
    }
}
