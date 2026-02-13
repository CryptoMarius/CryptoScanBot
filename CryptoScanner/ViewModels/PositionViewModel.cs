using Avalonia.Media;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings.Strategy;

using System.Reflection;

namespace CryptoScanner.ViewModels;

public partial class PositionViewModel : BaseConvertersViewModel
{

    public required CryptoPosition Object { get; set; }

    //public int Id => Object.Id;
    private string? _IdText;
    public string Id
    {
        get
        {
            _IdText ??= Object.Id.ToString();
            return _IdText!;
        }
    }

    public string? AltradyPositionId => Object.AltradyPositionId;

    private string? _CreateTimeText;
    public string CreateTime
    {
        get
        {
            _CreateTimeText ??= Object.CreateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return _CreateTimeText!;
        }
    }

    //public string UpdateTime => Object.UpdateTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm")!;
    private string? _UpdateTimeText;
    public string UpdateTime
    {
        get
        {
            _UpdateTimeText ??= Object.UpdateTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm")!;
            return _UpdateTimeText!;
        }
    }

    //public string CloseTime => Object.CloseTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm")!;
    private string? _CloseTimeText;
    public string CloseTime
    {
        get
        {
            _CloseTimeText ??= Object.CloseTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm")!;
            return _CloseTimeText!;
        }
        set
        {
            _CloseTimeText = null;
            OnPropertyChanged(nameof(CloseTime));
        }
    }

    //public string Duration => Object.DurationText();
    private string? _DurationText;
    public string Duration
    {
        get
        {
            _DurationText ??= Object.DurationText();
            return _DurationText!;
        }
        set
        {
            _DurationText = null;
            OnPropertyChanged(nameof(Duration));
        }
    }

    //public string Exchange => Object.Exchange.Name;
    private string? _ExchangeText;
    public string Exchange
    {
        get
        {
            _ExchangeText ??= Object.Exchange.Name;
            return _ExchangeText!;
        }
    }

    //public string Symbol => Object.Symbol.Name;
    private string? _SymbolText;
    public string Symbol
    {
        get
        {
            _SymbolText ??= Object.Symbol.Name;
            return _SymbolText!;
        }
    }
    private IBrush? _SymbolBackground;
    public IBrush SymbolBackground
    {
        get
        {
            _SymbolBackground ??= new SolidColorBrush(Object.Symbol.QuoteData.DisplayColor);
            return _SymbolBackground!;
        }
    }

    //public CryptoTradeSide Side => Object.Side;
    private string? _SideText;
    public string Side
    {
        get
        {
            _SideText ??= Object.SideText;
            return _SideText!;
        }
    }
    private IBrush? _SideForeground;
    public IBrush SideForeground
    {
        get
        {
            _SideForeground ??= GetBrushColorSide(Object.Side);
            return _SideForeground!;
        }
    }


    //public string Interval => Object.Interval.Name;
    private string? _IntervalText;
    public string Interval
    {
        get
        {
            _IntervalText ??= Object.Interval.Name;
            return _IntervalText!;
        }
    }

    //public string Strategy => Object.StrategyText;
    private string? _StrategyText;
    public string Strategy
    {
        get
        {
            _StrategyText ??= Object.StrategyText;
            return _StrategyText!;
        }
    }
    private IBrush? _StrategyBackground;
    public IBrush StrategyBackground
    {
        get
        {
            if (_StrategyBackground == null)
            {
                if (GlobalData.StrategiesSettings.TryGetValue(Object.Strategy, out (SettingsSignalStrategyBase strategySettings, DateTime _) x))
                {
                    if (Object.Side == CryptoTradeSide.Long)
                        _StrategyBackground = new SolidColorBrush(x.strategySettings.ColorLong);
                    else
                        _StrategyBackground = new SolidColorBrush(x.strategySettings.ColorShort);
                }
            }
            return _StrategyBackground!;
        }
    }

    //public CryptoPositionStatus Status => Object.Status;
    private string? _StatusText;
    public string Status
    {
        get
        {
            _StatusText ??= Object.Status.ToString();
            return _StatusText!;
        }
        set
        {
            _StatusText = null;
            OnPropertyChanged(nameof(Status));
        }
    }
    private IBrush? _StatusForeground;
    public IBrush StatusForeground
    {
        get
        {
            _StatusForeground ??= GetPositionStatusColor(Object.Status);
            return _StatusForeground!;
        }
    }

    //public decimal Invested => Object.Invested;
    private string? _InvestedText;
    public string Invested
    {
        get
        {
            if (_InvestedText == null)
            {
                if (Object.Status == CryptoPositionStatus.Timeout || Object.Status == CryptoPositionStatus.Waiting)
                    _InvestedText = "-";
                else
                    _InvestedText = Object.Invested.ToString0(Object.Symbol.QuoteData.DisplayFormat);
            }
            return _InvestedText!;
        }
        set
        {
            _InvestedText = null;
            OnPropertyChanged(nameof(Invested));
        }
    }

    //public decimal Returned => Object.Returned;
    private string? _ReturnedText;
    public string Returned
    {
        get
        {
            if (_ReturnedText == null)
            {
                if (Object.Status == CryptoPositionStatus.Timeout || Object.Status == CryptoPositionStatus.Waiting)
                    _ReturnedText = "-";
                else
                    _ReturnedText = Object.Returned.ToString0(Object.Symbol.QuoteData.DisplayFormat);
            }
            return _ReturnedText!;
        }
        set
        {
            _ReturnedText = null;
            OnPropertyChanged(nameof(Returned));
        }
    }

    //public decimal Commission => Object.Commission;
    private string? _CommissionText;
    public string Commission
    {
        get
        {
            if (_CommissionText == null)
            {
                if (Object.Status == CryptoPositionStatus.Timeout || Object.Status == CryptoPositionStatus.Waiting)
                    _CommissionText = "-";
                else
                    _CommissionText = Object.Commission.ToString0(Object.Symbol.QuoteData.DisplayFormat);
            }
            return _CommissionText!;
        }
        set
        {
            _CommissionText = null;
            OnPropertyChanged(nameof(Commission));
        }
    }


    //public decimal TotalProfit => Object.Profit;
    private string? _TotalProfitText;
    public string TotalProfit
    {
        get
        {
            if (_TotalProfitText == null)
            {
                if (Object.Status == CryptoPositionStatus.Timeout || Object.Status == CryptoPositionStatus.Waiting)
                    _TotalProfitText = "-";
                else
                    _TotalProfitText = Object.Profit.ToString0(Object.Symbol.QuoteData.DisplayFormat);
            }
            return _TotalProfitText!;
        }
    }


    //public decimal CurrentProfit => Object.CurrentProfit();
    private string? _CurrentProfit;
    public string CurrentProfit
    {
        get
        {
            if (_CurrentProfit == null)
            {
                if (Object.Status == CryptoPositionStatus.Timeout || Object.Status == CryptoPositionStatus.Waiting)
                    _CurrentProfit = "-";
                else
                    _CurrentProfit = Object.CurrentProfit().ToString0(Object.Symbol.QuoteData.DisplayFormat);
            }
            return _CurrentProfit!;
        }
        set
        {
            _CurrentProfit = null;
            OnPropertyChanged(nameof(CurrentProfit));
            _CurrentProfitForeground = null;
            OnPropertyChanged(nameof(CurrentProfitForeground));
        }
    }
    private IBrush? _CurrentProfitForeground;
    public IBrush CurrentProfitForeground
    {
        get
        {
            _CurrentProfitForeground ??= GetBrushColorViaSign(Object.CurrentProfit());
            return _CurrentProfitForeground!;
        }
    }


    //public decimal Quantity => Object.Quantity;
    private string? _QuantityText;
    public string Quantity
    {
        get
        {
            if (_QuantityText == null)
            {
                if (Object.Status == CryptoPositionStatus.Timeout)
                    _QuantityText = "-";
                else
                    _QuantityText = Object.Quantity.ToString0(); //Object.Symbol.QuantityDisplayFormat
            }
            return _QuantityText!;
        }
    }

    //public decimal Open => Object.Invested - Object.Returned - Object.Commission;
    private string? _OpenText;
    public string Open
    {
        get
        {
            if (_OpenText == null)
            {
                if (Object.Status == CryptoPositionStatus.Timeout || Object.Status == CryptoPositionStatus.Waiting)
                    _OpenText = "-";
                else
                    _OpenText = (Object.Invested - Object.Returned - Object.Commission).ToString0(Object.Symbol.QuoteData.DisplayFormat);
            }
            return _OpenText!;
        }
        set
        {
            _OpenText = null;
            OnPropertyChanged(nameof(Open));
        }
    }

    //public decimal BreakEvenPrice => Object.BreakEvenPrice;
    private string? _BreakEvenPriceText;
    public string BreakEvenPrice
    {
        get
        {
            _BreakEvenPriceText ??= Object.BreakEvenPrice.ToString0(Object.Symbol.PriceDisplayFormat);
            return _BreakEvenPriceText!;
        }
    }

    //public decimal BreakEvenPercent => Object.CurrentBreakEvenPercentage();
    private string? _BreakEvenPercentText;
    public string BreakEvenPercent
    {
        get
        {
            if (_BreakEvenPercentText == null)
            {
                if (Object.Status == CryptoPositionStatus.Timeout || Object.Status == CryptoPositionStatus.Waiting)
                    _BreakEvenPercentText = "-";
                else
                    _BreakEvenPercentText = Object.CurrentBreakEvenPercentage().ToString0("N2");
            }
            return _BreakEvenPercentText!;
        }
        set
        {
            _BreakEvenPercentText = null;
            OnPropertyChanged(nameof(BreakEvenPercent));
            _BreakEvenPercentForeground = null;
            OnPropertyChanged(nameof(BreakEvenPercentForeground));
        }
    }
    private IBrush? _BreakEvenPercentForeground;
    public IBrush BreakEvenPercentForeground
    {
        get
        {
            _BreakEvenPercentForeground ??= GetBrushColorViaSign(Object.CurrentBreakEvenPercentage());
            return _BreakEvenPercentForeground!;
        }
    }

    //public string Parts => Object.PartCountText();
    private string? _PartsText;
    public string Parts
    {
        get
        {
            _PartsText ??= Object.PartCountText();
            return _PartsText!;
        }
        set
        {
            _PartsText = null;
            OnPropertyChanged(nameof(Parts));
        }
    }

    //public decimal? EntryPrice => Object.EntryPrice;
    private string? _EntryPriceText;
    public string EntryPrice
    {
        get
        {
            _EntryPriceText ??= Object.EntryPrice.ToString0(Object.Symbol.PriceDisplayFormat);
            return _EntryPriceText!;
        }
    }

    //public decimal? ProfitPrice => Object.ProfitPrice;
    private string? _ProfitPriceText;
    public string ProfitPrice
    {
        get
        {
            _ProfitPriceText ??= Object.Percentage.ToString0();
            return _ProfitPriceText!;
        }
    }

    //public decimal CurrentProfitPercentage => Object.CurrentProfitPercentage();
    private string? _CurrentProfitPercentage;
    public string CurrentProfitPercentage
    {
        get
        {
            if (_CurrentProfitPercentage == null)
            {
                if (Object.Status == CryptoPositionStatus.Timeout || Object.Status == CryptoPositionStatus.Waiting)
                    _CurrentProfitPercentage = "-";
                else
                    _CurrentProfitPercentage = Object.CurrentProfitPercentage().ToString0(Object.Symbol.QuoteData.DisplayFormat);
            }
            return _CurrentProfitPercentage!;
        }
        set
        {
            _CurrentProfitPercentage = null;
            OnPropertyChanged(nameof(CurrentProfitPercentage));
            _CurrentProfitPercentageForeground = null;
            OnPropertyChanged(nameof(CurrentProfitPercentageForeground));
        }
    }
    private IBrush? _CurrentProfitPercentageForeground;
    public IBrush CurrentProfitPercentageForeground
    {
        get
        {
            _CurrentProfitPercentageForeground ??= GetBrushColorViaSign(Object.CurrentProfitPercentage());
            return _CurrentProfitPercentageForeground!;
        }
    }

    //public decimal TotalPercentage => Object.Percentage;
    private string? _TotalPercentageText;
    public string TotalPercentage
    {
        get
        {
            _TotalPercentageText ??= Object.Percentage.ToString0();
            return _TotalPercentageText!;
        }
    }

    //public decimal FundingRate => Object.Symbol.FundingRate;
    private string? _FundingRateText;
    public string FundingRate
    {
        get
        {
            _FundingRateText ??= Object.Symbol.FundingRate.ToString0();
            return _FundingRateText!;
        }
    }

    //public decimal QuantityTick => Object.Symbol.QuantityTickSize;
    private string? _QuantityTickText;
    public string QuantityTick
    {
        get
        {
            _QuantityTickText ??= Object.Symbol.QuantityTickSize.ToString0();
            return _QuantityTickText!;
        }
    }

    //public decimal RemainingDust => Object.RemainingDust;
    private string? _RemainingDustText;
    public string RemainingDust
    {
        get
        {
            if (_RemainingDustText == null)
            {
                if (Object.Status == CryptoPositionStatus.Timeout || Object.Status == CryptoPositionStatus.Waiting)
                    _RemainingDustText = "-";
                else
                    _RemainingDustText = Object.RemainingDust.ToString0(); //Object.Symbol.QuantityDisplayFormat
            }
            return _RemainingDustText!;
        }
    }

    //public decimal? RemainingDustValue => Object.RemainingDust * Object.Symbol.LastPrice;
    private string? _RemainingDustValueText;
    public string RemainingDustValue
    {
        get
        {
            if (_RemainingDustValueText == null)
            {
                if (Object.Symbol.LastPrice == null)
                    _RemainingDustValueText ??= (Object.RemainingDust * Object.ProfitPrice).ToString0(Object.Symbol.QuoteData.DisplayFormat);
                else
                    _RemainingDustValueText ??= (Object.RemainingDust * Object.Symbol.LastPrice).ToString0(Object.Symbol.QuoteData.DisplayFormat);
            }
            return _RemainingDustValueText!;
        }
    }
    //public decimal Reserved => Object.Reserved;
    //public decimal CurrentProfit => Object.CurrentProfit;
    //public decimal? EntryAmount => Object.EntryAmount;

    //public DateTime SignalDate => Object.SignalEventTime;
    private string? _SignalDateText;
    public string SignalDate
    {
        get
        {
            _SignalDateText ??= Object.SignalEventTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return _SignalDateText!;
        }
    }

    //public decimal SignalPrice => Object.SignalPrice;
    private string? _SignalPriceText;
    public string SignalPrice
    {
        get
        {
            _SignalPriceText ??= Object.SignalPrice.ToString0(Object.Symbol.PriceDisplayFormat);
            return _SignalPriceText!;
        }
    }

    //public decimal SignalVolume => Object.SignalVolume;
    private string? _SignalVolumeText;
    public string SignalVolume
    {
        get
        {
            _SignalVolumeText ??= Object.SignalVolume.ToString0("N0");
            return _SignalVolumeText!;
        }
    }
    //public float TrendPercentagePrimary => Object.TrendPercentagePrimary;
    private string? _TrendPercentagePrimaryText;
    public string TrendPercentagePrimary
    {
        get
        {
            _TrendPercentagePrimaryText ??= Object.TrendPercentagePrimary.ToString("N2");
            return _TrendPercentagePrimaryText!;
        }
    }

    private IBrush? _TrendPercentagePrimaryForeground;
    public IBrush TrendPercentagePrimaryForeground
    {
        get
        {
            _TrendPercentagePrimaryForeground ??= GetBrushColorViaSign(Object.TrendPercentagePrimary);
            return _TrendPercentagePrimaryForeground!;
        }
    }

    //public float TrendPercentageSecondary => Object.TrendPercentageSecondary;
    private string? _TrendPercentageSecondaryText;
    public string TrendPercentageSecondary
    {
        get
        {
            _TrendPercentageSecondaryText ??= Object.TrendPercentageSecondary.ToString("N2");
            return _TrendPercentageSecondaryText!;
        }
    }

    private IBrush? _TrendPercentageSecondaryForeground;
    public IBrush TrendPercentageSecondaryForeground
    {
        get
        {
            _TrendPercentageSecondaryForeground ??= GetBrushColorViaSign(Object.TrendPercentageSecondary);
            return _TrendPercentageSecondaryForeground!;
        }
    }

    //public double Last24HoursChange => Object.Last24HoursChange;
    private string? _Last24HoursChangeText;
    public string Last24HoursChange
    {
        get
        {
            _Last24HoursChangeText ??= Object.Last24HoursChange.ToString("N2");
            return _Last24HoursChangeText!;
        }
    }

    private IBrush? _Last24HoursChangeForeground;
    public IBrush Last24HoursChangeForeground
    {
        get
        {
            _Last24HoursChangeForeground ??= GetBrushColorViaSign(Object.Last24HoursChange);
            return _Last24HoursChangeForeground!;
        }
    }
    //public double LastXDaysEffective => Object.LastXDaysEffective;
    private string? _LastXDaysEffectiveText;
    public string LastXDaysEffective
    {
        get
        {
            _LastXDaysEffectiveText ??= Object.LastXDaysEffective.ToString("N2");
            return _LastXDaysEffectiveText!;
        }
    }
    //public double AvgBB => Object.AvgBB;
    private string? _AvgBBText;
    public string AvgBB
    {
        get
        {
            _AvgBBText ??= Object.AvgBB.ToString("N2");
            return _AvgBBText!;
        }
    }

    //public double? BB => Object.BollingerBandsPercentage;
    private string? _BbText;
    public string Bb
    {
        get
        {
            _BbText ??= Object.BollingerBandsPercentage?.ToString("N2");
            return _BbText!;
        }
    }
    //public double? BbLower => Object.BollingerBandsLowerBand;
    private string? _BbLowerText;
    public string BbLower
    {
        get
        {
            if (_BbLowerText == null)
            {
                _BbLowerText = Object.BollingerBandsLowerBand.ToString0(Object.Symbol.PriceDisplayFormat);
            }
            return _BbLowerText!;
        }
    }

    //public double? BbUpper => Object.BollingerBandsUpperBand;
    private string? _BbUpperText;
    public string BbUpper
    {
        get
        {
            if (_BbUpperText == null)
            {
                _BbUpperText = Object.BollingerBandsUpperBand.ToString0(Object.Symbol.PriceDisplayFormat);
            }
            return _BbUpperText!;
        }
    }

    //public double? Rsi => Object.Rsi;
    private string? _RsiText;
    public string Rsi
    {
        get
        {
            _RsiText ??= Object.Rsi?.ToString("N2");
            return _RsiText!;
        }
    }

    private IBrush? _rsiForeground;
    public IBrush RsiForeground
    {
        get
        {
            _rsiForeground ??= GetBrushColorRsi(Object.Rsi);
            return _rsiForeground!;
        }
    }

    //public int LuxIndicator5m => Object.LuxIndicator5m;
    private string? _LuxIndicator5mText;
    public string LuxIndicator5m
    {
        get
        {
            _LuxIndicator5mText ??= Object.LuxIndicator5m.ToString("N0");
            return _LuxIndicator5mText!;
        }
    }

    private IBrush? _LuxIndicator5mForeground;
    public IBrush LuxIndicator5mForeground
    {
        get
        {
            _LuxIndicator5mForeground ??= GetBrushColorViaSign((double)Object.LuxIndicator5m);
            return _LuxIndicator5mForeground!;
        }
    }


    //public double? MacdValue => Object.MacdValue;
    private string? _MacdValueText;
    public string MacdValue
    {
        get
        {
            _MacdValueText ??= Object.MacdValue?.ToString("N5");
            return _MacdValueText!;
        }
    }
    private IBrush? _MacdValueForeground;
    public IBrush MacdValueForeground
    {
        get
        {
            _MacdValueForeground ??= GetBrushColorViaSign(Object.MacdValue);
            return _MacdValueForeground!;
        }
    }


    //public double? MacdSignal => Object.MacdSignal;
    private string? _MacdSignalText;
    public string MacdSignal
    {
        get
        {
            _MacdSignalText ??= Object.MacdSignal?.ToString("N5");
            return _MacdSignalText!;
        }
    }
    private IBrush? _MacdSignalForeground;
    public IBrush MacdSignalForeground
    {
        get
        {
            _MacdSignalForeground ??= GetBrushColorViaSign(Object.MacdSignal);
            return _MacdSignalForeground!;
        }
    }

    //public double? MacdHistogram => Object.MacdHistogram;
    private string? _MacdHistogramText;
    public string MacdHistogram
    {
        get
        {
            _MacdHistogramText ??= Object.MacdHistogram?.ToString("N2");
            return _MacdHistogramText!;
        }
    }
    private IBrush? _MacdHistogramForeground;
    public IBrush MacdHistogramForeground
    {
        get
        {
            _MacdHistogramForeground ??= GetBrushColorViaSign(Object.MacdHistogram);
            return _MacdHistogramForeground!;
        }
    }

    //public double? StochOscillator => Object.StochOscillator;
    private string? _StochOscillatorText;
    public string StochOscillator
    {
        get
        {
            _StochOscillatorText ??= Object.StochOscillator?.ToString0("N2");
            return _StochOscillatorText!;
        }
    }
    private IBrush? _StochOscillatorForeground;
    public IBrush StochOscillatorForeground
    {
        get
        {
            _StochOscillatorForeground ??= GetBrushColorStoch(Object.StochOscillator);
            return _StochOscillatorForeground!;
        }
    }


    //public double? StochSignal => Object.StochSignal;
    private string? _StochSignalText;
    public string StochSignal
    {
        get
        {
            _StochSignalText ??= Object.StochSignal?.ToString0("N2");
            return _StochSignalText!;
        }
    }
    private IBrush? _StochSignalForeground;
    public IBrush StochSignalForeground
    {
        get
        {
            _StochSignalForeground ??= GetBrushColorStoch(Object.StochSignal);
            return _StochSignalForeground!;
        }
    }


    //public double? Sma200 => Object.Sma200;
    private string? _Sma200Text;
    public string Sma200
    {
        get
        {
            _Sma200Text ??= Object.Sma200?.ToString0(Object.Symbol.PriceDisplayFormat);
            return _Sma200Text!;
        }
    }

    //public double? Sma50 => Object.Sma50;
    private string? _Sma50Text;
    public string Sma50
    {
        get
        {
            _Sma50Text ??= Object.Sma50?.ToString0(Object.Symbol.PriceDisplayFormat);
            return _Sma50Text!;
        }
    }
    private IBrush? _Sma50Foreground;
    public IBrush Sma50Foreground
    {
        get
        {
            _Sma50Foreground ??= GetBrushColorSma50(Object.Side, Object.Sma50, Object.Sma50);
            return _Sma50Foreground!;
        }
    }

    //public double? Sma20 => Object.Sma20;
    private string? _Sma20Text;
    public string Sma20
    {
        get
        {
            _Sma20Text ??= Object.Sma20?.ToString0(Object.Symbol.PriceDisplayFormat);
            return _Sma20Text!;
        }
    }
    private IBrush? _Sma20Foreground;
    public IBrush Sma20Foreground
    {
        get
        {
            _Sma20Foreground ??= GetBrushColorSma20(Object.Side, Object.Sma20, Object.Sma50);
            return _Sma20Foreground!;
        }
    }

    //public double? PSar => Object.PSar;
    private string? _PSarText;
    public string PSar
    {
        get
        {
            _PSarText ??= Object.PSar?.ToString0(Object.Symbol.PriceDisplayFormat);
            return _PSarText!;
        }
    }
    private IBrush? _PSarForeground;
    public IBrush PSarForeground
    {
        get
        {
            _PSarForeground ??= GetBrushColorPSar(Object.Side, Object.PSar, Object.Sma20);
            return _PSarForeground!;
        }
    }

    //public CryptoTrendIndicator TrendInterval => Object.TrendInterval;
    private string? _TrendIntervalText;
    public string TrendInterval
    {
        get
        {
            _TrendIntervalText ??= Object.TrendInterval == CryptoTrendIndicator.Bullish ? "up" : "down";
            return _TrendIntervalText!;
        }
    }
    private IBrush? _TrendIntervalForeground;
    public IBrush TrendIntervalForeground
    {
        get
        {
            _TrendIntervalForeground ??= GetBrushColorTrend(Object.TrendInterval);
            return _TrendIntervalForeground!;
        }
    }

    //public CryptoTrendIndicator? Trend15m => Object.Trend15m;
    private string? _Trend15mText;
    public string Trend15m
    {
        get
        {
            _Trend15mText ??= Object.Trend15m == CryptoTrendIndicator.Bullish ? "up" : "down";
            return _Trend15mText!;
        }
    }
    private IBrush? _Trend15mForeground;
    public IBrush Trend15mForeground
    {
        get
        {
            _Trend15mForeground ??= GetBrushColorTrend(Object.Trend15m);
            return _Trend15mForeground!;
        }
    }

    //public CryptoTrendIndicator? Trend30m => Object.Trend30m;
    private string? _Trend30mText;
    public string Trend30m
    {
        get
        {
            _Trend30mText ??= Object.Trend30m == CryptoTrendIndicator.Bullish ? "up" : "down";
            return _Trend30mText!;
        }
    }
    private IBrush? _Trend30mForeground;
    public IBrush Trend30mForeground
    {
        get
        {
            _Trend30mForeground ??= GetBrushColorTrend(Object.Trend30m);
            return _Trend30mForeground!;
        }
    }

    //public CryptoTrendIndicator? Trend1h => Object.Trend1h;
    private string? _Trend1hText;
    public string Trend1h
    {
        get
        {
            _Trend1hText ??= Object.Trend1h == CryptoTrendIndicator.Bullish ? "up" : "down";
            return _Trend1hText!;
        }
    }
    private IBrush? _Trend1hForeground;
    public IBrush Trend1hForeground
    {
        get
        {
            _Trend1hForeground ??= GetBrushColorTrend(Object.Trend1h);
            return _Trend1hForeground!;
        }
    }


    //public CryptoTrendIndicator? Trend4h => Object.Trend4h;
    private string? _Trend4hText;
    public string Trend4h
    {
        get
        {
            _Trend4hText ??= Object.Trend4h == CryptoTrendIndicator.Bullish ? "up" : "down";
            return _Trend4hText!;
        }
    }
    private IBrush? _Trend4hForeground;
    public IBrush Trend4hForeground
    {
        get
        {
            _Trend4hForeground ??= GetBrushColorTrend(Object.Trend4h);
            return _Trend4hForeground!;
        }
    }

    //public CryptoTrendIndicator? Trend1d => Object.Trend1d;
    private string? _Trend1dText;
    public string Trend1d
    {
        get
        {
            _Trend1dText ??= Object.Trend1d == CryptoTrendIndicator.Bullish ? "up" : "down";
            return _Trend1dText!;
        }
    }
    private IBrush? _Trend1dForeground;
    public IBrush Trend1dForeground
    {
        get
        {
            _Trend1dForeground ??= GetBrushColorTrend(Object.Trend1d);
            return _Trend1dForeground!;
        }
    }

    //public decimal? Barometer15m => Object.Barometer15m;
    private string? _Barometer15mText;
    public string Barometer15m
    {
        get
        {
            _Barometer15mText ??= Object.Barometer15m?.ToString("N2");
            return _Barometer15mText!;
        }
    }
    private IBrush? _Barometer15mForeground;
    public IBrush Barometer15mForeground
    {
        get
        {
            _Barometer15mForeground ??= GetBrushColorViaSign(Object.Barometer15m);
            return _Barometer15mForeground!;
        }
    }

    //public decimal? Barometer30m => Object.Barometer30m;
    private string? _Barometer30mText;
    public string Barometer30m
    {
        get
        {
            _Barometer30mText ??= Object.Barometer30m?.ToString("N2");
            return _Barometer30mText!;
        }
    }
    private IBrush? _Barometer30mForeground;
    public IBrush Barometer30mForeground
    {
        get
        {
            _Barometer30mForeground ??= GetBrushColorViaSign(Object.Barometer30m);
            return _Barometer30mForeground!;
        }
    }

    //public decimal? Barometer1h => Object.Barometer1h;
    private string? _Barometer1hText;
    public string Barometer1h
    {
        get
        {
            _Barometer1hText ??= Object.Barometer1h?.ToString("N2");
            return _Barometer1hText!;
        }
    }
    private IBrush? _Barometer1hForeground;
    public IBrush Barometer1hForeground
    {
        get
        {
            _Barometer1hForeground ??= GetBrushColorViaSign(Object.Barometer1h);
            return _Barometer1hForeground!;
        }
    }


    //public decimal? Barometer4h => Object.Barometer4h;
    private string? _Barometer4hText;
    public string Barometer4h
    {
        get
        {
            _Barometer4hText ??= Object.Barometer4h?.ToString("N2");
            return _Barometer4hText!;
        }
    }
    private IBrush? _Barometer4hForeground;
    public IBrush Barometer4hForeground
    {
        get
        {
            _Barometer4hForeground ??= GetBrushColorViaSign(Object.Barometer4h);
            return _Barometer4hForeground!;
        }
    }


    //public decimal? Barometer1d => Object.Barometer1d;
    private string? _Barometer1dText;
    public string Barometer1d
    {
        get
        {
            _Barometer1dText ??= Object.Barometer1d?.ToString("N2");
            return _Barometer1dText!;
        }
    }
    private IBrush? _Barometer1dForeground;
    public IBrush Barometer1dForeground
    {
        get
        {
            _Barometer1dForeground ??= GetBrushColorViaSign(Object.Barometer1d);
            return _Barometer1dForeground!;
        }
    }

    //public decimal MinimumEntry => Object.MinEntry;
    private string? _MinimumEntryText;
    public string MinimumEntry
    {
        get
        {
            _MinimumEntryText ??= Object.MinEntry.ToString("N2");
            return _MinimumEntryText!;
        }
    }

    //public double PriceMinPerc => Object.PriceMinPerc;
    private string? _PriceMinPercText;
    public string PriceMinPerc
    {
        get
        {
            _PriceMinPercText ??= Object.PriceMinPerc.ToString("N2");
            return _PriceMinPercText!;
        }
        set
        {
            _PriceMinPercText = null;
            OnPropertyChanged(nameof(PriceMinPerc));
        }
    }


    //public double PriceMaxPerc => Object.PriceMaxPerc;
    private string? _PriceMaxPercText;
    public string PriceMaxPerc
    {
        get
        {
            _PriceMaxPercText ??= Object.PriceMaxPerc.ToString("N2");
            return _PriceMaxPercText!;
        }
        set
        {
            _PriceMaxPercText = null;
            OnPropertyChanged(nameof(PriceMaxPerc));
        }
    }

    //public CryptoSignalStatus SignalStatus => Object.SignalStatus;
    private string? _SignalStatusText;
    public string SignalStatus
    {
        get
        {
            _SignalStatusText ??= GetSignalStatusText(Object.SignalStatus);
            return _SignalStatusText!;
        }
        set
        {
            _SignalStatusText = null;
            OnPropertyChanged(nameof(SignalStatus));
        }
    }
    private IBrush? _SignalStatusForeground;
    public IBrush SignalStatusForeground
    {
        get
        {
            _SignalStatusForeground ??= GetSignalStatusColor(Object.SignalStatus);
            return _SignalStatusForeground!;
        }
        set
        {
            _SignalStatusForeground = null;
            OnPropertyChanged(nameof(SignalStatusForeground));
        }
    }

    public void NotifyColumnChanged(string column)
    {
        OnPropertyChanged(column);
    }

    public void Refresh()
    {
        // Reset alle private fields
        var fields = GetType()
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(string) || f.FieldType == typeof(IBrush));

        foreach (var field in fields)
        {
            field.SetValue(this, null);
        }

        foreach (var field in fields)
        {
            OnPropertyChanged(field.Name);
        }
    }
}
