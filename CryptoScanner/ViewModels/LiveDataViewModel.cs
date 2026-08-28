using Avalonia.Media;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;
using CryptoScanner.Services;

namespace CryptoScanner.ViewModels;

public class LiveDataViewModel : BaseConvertersViewModel
{
    public required CryptoLiveData Object { get; set; }

    //public int Id { get => Object.Id; set { }}

    public string Date
    {
        get
        {
            var closeData = Object.Candle.Date.AddSeconds(Object.Interval.Duration);
            return Object.Candle.Date.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + closeData.ToLocalTime().ToString("HH:mm");
        }
    }

    //public string Exchange => Object.Exchange.Name;
    private string? _ExchangeText;
    public string Exchange
    {
        get
        {
            _ExchangeText ??= Object.Symbol.Exchange.Name;
            return _ExchangeText!;
        }
    }

    //public string Symbol => Object.Symbol.Name;
    private string? _SymbolText;
    public string Symbol
    {
        get
        {
            _SymbolText ??= Object.Symbol.PairName;
            return _SymbolText!;
        }
    }

    /// <summary>
    /// The market label shown as a coloured badge behind the name (see CryptoSymbol.MarketLabel).
    /// The name itself stays bare: what the badge carries used to be glued behind the name as text.
    /// </summary>
    private string? _MarketLabelText;
    public string MarketLabel
    {
        get
        {
            _MarketLabelText ??= Object.Symbol.MarketLabel;
            return _MarketLabelText!;
        }
    }
    private IBrush? _MarketLabelBackground;
    public IBrush MarketLabelBackground
    {
        get
        {
            _MarketLabelBackground ??= GetBrushColorMarketLabel(MarketLabel);
            return _MarketLabelBackground!;
        }
    }

    private IBrush? _SymbolBackground;
    public IBrush SymbolBackground
    {
        get
        {
            _SymbolBackground ??= new SolidColorBrush(Object.Symbol.QuoteData.DisplayColor.ToAvaloniaColor());
            return _SymbolBackground!;
        }
    }

    /// <summary>
    /// Drop the cached values that were derived from the settings, so the row picks up the new ones.
    /// <para>
    /// The volume colour belongs here just as much as the symbol background: it is decided against
    /// QuoteData.MinimalVolume, and that value moves when the user changes the minimum volume of a
    /// quote coin. Only the background was cleared, so after such a change the volume column kept
    /// showing red or green according to the OLD boundary until the row was rebuilt for another
    /// reason - which for this grid never happens, its rows only get appended.
    /// </para>
    /// </summary>
    public void ResetSymbolBackground()
    {
        // Every cached brush of the row, not just these two: the indicator columns are coloured
        // against the RSI and stochastic levels from the settings, and green and red themselves come
        // from the theme.
        ResetCachedBrushes();
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
    //public decimal Price { get => Object.Candle.Close; set { } }
    private string? _PriceText;
    public string Price
    {
        get
        {
            _PriceText ??= Object.Candle.Close.ToString0(Object.Symbol.PriceDisplayFormat);
            return _PriceText!;
        }
    }

    //public decimal Volume { get => Object.Symbol.Volume; set { } }
    private string? _VolumeText;
    public string Volume
    {
        get
        {
            _VolumeText ??= Object.Symbol.Volume.ToString("N0");
            return _VolumeText!;
        }
    }
    private IBrush? _SignalVolumeForeground;
    public IBrush SignalVolumeForeground
    {
        get
        {
            if (_SignalVolumeForeground == null)
            {
                if (Object.Symbol.QuoteData.MinimalVolume <= 0)
                    _SignalVolumeForeground = BrushNeutral;
                else if (Object.Symbol.Volume < Object.Symbol.QuoteData.MinimalVolume)
                    _SignalVolumeForeground = BrushRed;
                else
                    _SignalVolumeForeground = BrushGreen;
            }
            return _SignalVolumeForeground!;
        }
    }

    //public double? BB => Object.BollingerBandsPercentage;
    private string? _BbText;
    public string Bb
    {
        get
        {
            _BbText ??= Object.CandleData?.BollingerBandsPercentage?.ToString("N2");
            return _BbText!;
        }
    }

    //public double? BbLower => Object.BollingerBandsLowerBand;
    private string? _BbLowerText;
    public string BbLower
    {
        get
        {
            _BbLowerText ??= Object.CandleData?.BollingerBandsLowerBand?.ToString0(Object.Symbol.PriceDisplayFormat);
            return _BbLowerText!;
        }
    }

    //public double? BbUpper => Object.BollingerBandsUpperBand;
    private string? _BbUpperText;
    public string BbUpper
    {
        get
        {
            _BbUpperText ??= Object.CandleData?.BollingerBandsUpperBand.ToString0(Object.Symbol.PriceDisplayFormat);
            return _BbUpperText!;
        }
    }

    // Band-range statistics, kept per symbol+interval next to the indicator hub. Not part of
    // CandleData: it describes the last few hundred candles, not this single one.
    private BandRangeTracker? BandRange
        => Object.Symbol.GetSymbolInterval(Object.Interval.IntervalPeriod).BandRange;

    private string? _RangeIndexText;
    public string RangeIndex
    {
        get
        {
            _RangeIndexText ??= BandRange?.Index?.ToString("N2") ?? "";
            return _RangeIndexText!;
        }
    }

    private IBrush? _RangeIndexForeground;
    public IBrush RangeIndexForeground
    {
        get
        {
            _RangeIndexForeground ??= GetBrushColorBandRangeIndex(BandRange?.Index);
            return _RangeIndexForeground!;
        }
    }

    private string? _RangeCountText;
    public string RangeCount
    {
        get
        {
            _RangeCountText ??= BandRange?.MeasurementCount.ToString() ?? "";
            return _RangeCountText!;
        }
    }

    //public double? Rsi => Object.Rsi;
    private string? _RsiText;
    public string Rsi
    {
        get
        {
            _RsiText ??= Object.CandleData?.Rsi.ToString0("N2");
            return _RsiText!;
        }
    }

    private IBrush? _rsiForeground;
    public IBrush RsiForeground
    {
        get
        {
            _rsiForeground ??= GetBrushColorRsi(Object.CandleData?.Rsi);
            return _rsiForeground!;
        }
    }

    //public int LuxIndicator5m => Object.LuxIndicator5m;
    private string? _LuxIndicator5mText;
    public string LuxIndicator5m
    {
        get
        {
            _LuxIndicator5mText ??= Object.CandleData?.Lux5mValue?.ToString("N0");
            return _LuxIndicator5mText!;
        }
    }
    private IBrush? _LuxIndicator5mForeground;
    public IBrush LuxIndicator5mForeground
    {
        get
        {
            _LuxIndicator5mForeground ??= GetBrushColorViaSign((double)Object.CandleData?.Lux5mValue!);
            return _LuxIndicator5mForeground!;
        }
    }

    //public double? MacdValue => Object.MacdValue;
    private string? _MacdValueText;
    public string MacdValue
    {
        get
        {
            _MacdValueText ??= Object.CandleData?.MacdValue?.ToString("N5");
            return _MacdValueText!;
        }
    }
    private IBrush? _MacdValueForeground;
    public IBrush MacdValueForeground
    {
        get
        {
            _MacdValueForeground ??= GetBrushColorViaSign(Object.CandleData?.MacdValue);
            return _MacdValueForeground!;
        }
    }


    //public double? MacdSignal => Object.MacdSignal;
    private string? _MacdSignalText;
    public string MacdSignal
    {
        get
        {
            _MacdSignalText ??= Object.CandleData?.MacdSignal?.ToString("N5");
            return _MacdSignalText!;
        }
    }
    private IBrush? _MacdSignalForeground;
    public IBrush MacdSignalForeground
    {
        get
        {
            _MacdSignalForeground ??= GetBrushColorViaSign(Object.CandleData?.MacdSignal);
            return _MacdSignalForeground!;
        }
    }

    //public double? MacdHistogram => Object.MacdHistogram;
    private string? _MacdHistogramText;
    public string MacdHistogram
    {
        get
        {
            _MacdHistogramText ??= Object.CandleData?.MacdHistogram?.ToString("N2");
            return _MacdHistogramText!;
        }
    }
    private IBrush? _MacdHistogramForeground;
    public IBrush MacdHistogramForeground
    {
        get
        {
            _MacdHistogramForeground ??= GetBrushColorViaSign(Object.CandleData?.MacdHistogram);
            return _MacdHistogramForeground!;
        }
    }

    //public double? StochOscillator => Object.StochOscillator;
    private string? _StochOscillatorText;
    public string StochOscillator
    {
        get
        {
            _StochOscillatorText ??= Object.CandleData?.StochOscillator?.ToString("N2");
            return _StochOscillatorText!;
        }
    }
    private IBrush? _StochOscillatorForeground;
    public IBrush StochOscillatorForeground
    {
        get
        {
            _StochOscillatorForeground ??= GetBrushColorStoch(Object.CandleData?.StochOscillator);
            return _StochOscillatorForeground!;
        }
    }


    //public double? StochSignal => Object.StochSignal;
    private string? _StochSignalText;
    public string StochSignal
    {
        get
        {
            _StochSignalText ??= Object.CandleData?.StochSignal?.ToString("N2");
            return _StochSignalText!;
        }
    }
    private IBrush? _StochSignalForeground;
    public IBrush StochSignalForeground
    {
        get
        {
            _StochSignalForeground ??= GetBrushColorStoch(Object.CandleData?.StochSignal);
            return _StochSignalForeground!;
        }
    }


    //public double? Sma200 => Object.Sma200;
    private string? _Sma200Text;
    public string Sma200
    {
        get
        {
            _Sma200Text ??= Object.CandleData?.Sma200?.ToString0(Object.Symbol.PriceDisplayFormat);
            return _Sma200Text!;
        }
    }

    //public double? Sma50 => Object.Sma50;
    private string? _Sma50Text;
    public string Sma50
    {
        get
        {
            _Sma50Text ??= Object.CandleData?.Sma50?.ToString0(Object.Symbol.PriceDisplayFormat);
            return _Sma50Text!;
        }
    }
    private IBrush? _Sma50Foreground;
    public IBrush Sma50Foreground
    {
        get
        {
            _Sma50Foreground ??= GetBrushColorSma50(Core.Enums.CryptoTradeSide.Long, Object.CandleData?.Sma50, Object.CandleData?.Sma50);
            return _Sma50Foreground!;
        }
    }

    //public double? Sma20 => Object.Sma20;
    private string? _Sma20Text;
    public string Sma20
    {
        get
        {
            _Sma20Text ??= Object.CandleData?.Sma20?.ToString0(Object.Symbol.PriceDisplayFormat);
            return _Sma20Text!;
        }
    }
    private IBrush? _Sma20Foreground;
    public IBrush Sma20Foreground
    {
        get
        {
            _Sma20Foreground ??= GetBrushColorSma20(Core.Enums.CryptoTradeSide.Long, Object.CandleData?.Sma20, Object.CandleData?.Sma50);
            return _Sma20Foreground!;
        }
    }

    //public double? PSar => Object.PSar;
    private string? _PSarText;
    public string PSar
    {
        get
        {
            _PSarText ??= Object.CandleData?.PSar?.ToString0(Object.Symbol.PriceDisplayFormat);
            return _PSarText!;
        }
    }
    private IBrush? _PSarForeground;
    public IBrush PSarForeground
    {
        get
        {
            _PSarForeground ??= GetBrushColorPSar(Core.Enums.CryptoTradeSide.Long, Object.CandleData?.PSar, Object.CandleData?.Sma20);
            return _PSarForeground!;
        }
    }


}