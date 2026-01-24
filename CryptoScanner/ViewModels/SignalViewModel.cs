using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.ViewModels;

public partial class SignalViewModel : ObservableObject
{
    public required CryptoSignal Object { get; set; }

    public int Id => Object.Id;

    public string Date => Object.OpenDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + Object.CloseDate.ToLocalTime().ToString("HH:mm");
    public string Exchange => Object.Exchange.Name;
    public string Symbol => Object.Symbol.Name;
    public CryptoTradeSide Side => Object.Side;
    public string Interval => Object.Interval.Name;
    public string Strategy => Object.StrategyText;
    public decimal SignalPrice => Object.SignalPrice;
    public decimal SignalVolume => Object.SignalVolume;
    public double PriceChange => Object.Last24HoursChange;
    public string? EventText => Object.EventText;

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

    //public double PriceMinPerc => Object.PriceMinPerc;
    [ObservableProperty]
    private double _priceMinPerc;
    //public double PriceMaxPerc => Object.PriceMaxPerc;
    [ObservableProperty]
    private double _priceMaxPerc;
    //public CryptoSignalStatus SignalStatus => Object.SignalStatus;
    [ObservableProperty]
    private CryptoSignalStatus _signalStatus;


    //partial void OnPriceMaxPercChanged(double value)
    //{
    //    Object.PriceMaxPerc = value;
    //    // Hier kun je extra logica toevoegen als je wilt (bijv. kleur wijzigen)
    //}
    //// Deze methode wordt automatisch aangeroepen als PriceDiff wijzigt
    //partial void OnPriceMinPercChanged(double value)
    //{
    //    Object.PriceMinPerc = value;
    //    // Hier kun je extra logica toevoegen als je wilt (bijv. kleur wijzigen)
    //}


    public bool UpdateSignalStatistics()
    {
        if (UpdateSignalStatisticsInternal())
        {
            // Update viewmodel to update prices..
            PriceMinPerc = Object.PriceMinPerc;
            PriceMaxPerc = Object.PriceMaxPerc;
            SignalStatus = Object.SignalStatus;
            return true;
        }
        return false;
    }

    internal bool UpdateSignalStatisticsInternal()
    {
        var signal = Object;
        if (!signal.BackTest) //  && signal.Strategy != CryptoSignalStrategy.Jump
        {
            try
            {
                CryptoSymbolInterval symbolInterval = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                CryptoCandle? candle = symbolInterval.CandleList.Values.LastOrDefault(); // todo, not working for emulator & dates!
                if (candle != null)
                {
                    var result = false;

                    if (candle.Low < signal.PriceMin || signal.PriceMin == 0)
                    {
                        signal.PriceMin = candle.Low;
                        signal.PriceMinPerc = (double)(100 * (signal.PriceMin / signal.SignalPrice - 1));
                        result = true;
                    }
                    else if (candle.High > signal.PriceMax || signal.PriceMax == 0)
                    {
                        signal.PriceMax = candle.High;
                        signal.PriceMaxPerc = (double)(100 * (signal.PriceMax / signal.SignalPrice - 1));
                        result = true;
                    }

#if DEBUG
                    if (signal.SignalStatus == CryptoSignalStatus.Run)
                    {
                        decimal stopLossPerc = GlobalData.Settings.Trading.StopLossPercentage / 100;
                        if (stopLossPerc != 0.0m)
                        {
                            if (signal.Side == CryptoTradeSide.Long)
                            {
                                decimal stopLossPrice = signal.SignalPrice - stopLossPerc * signal.SignalPrice;
                                if (signal.PriceMin <= stopLossPrice)
                                {
                                    signal.SignalStatus = CryptoSignalStatus.Lost;
                                    result = true;
                                }
                            }
                            else if (signal.Side == CryptoTradeSide.Short)
                            {
                                decimal stopLossPrice = signal.SignalPrice + stopLossPerc * signal.SignalPrice;
                                if (signal.PriceMax >= stopLossPrice)
                                {
                                    signal.SignalStatus = CryptoSignalStatus.Lost;
                                    result = true;
                                }
                            }
                        }
                        // still running? ;-)
                        if (signal.SignalStatus == CryptoSignalStatus.Run)
                        {
                            decimal takeProfitPercentage = GlobalData.Settings.Trading.ProfitPercentage / 100;
                            if (takeProfitPercentage != 0.0m)
                            {
                                if (signal.Side == CryptoTradeSide.Long)
                                {
                                    decimal takeProfitPrice = signal.SignalPrice + takeProfitPercentage * signal.SignalPrice;
                                    if (signal.PriceMax > takeProfitPrice)
                                    {
                                        signal.SignalStatus = CryptoSignalStatus.Win;
                                        result = true;
                                    }
                                }
                                else if (signal.Side == CryptoTradeSide.Short)
                                {
                                    decimal takeProfitPrice = signal.SignalPrice - takeProfitPercentage * signal.SignalPrice;
                                    if (signal.PriceMin < takeProfitPrice)
                                    {
                                        signal.SignalStatus = CryptoSignalStatus.Win;
                                        result = true;
                                    }
                                }
                            }
                        }
                    }
#endif
                    return result;
                }
            }
            catch
            {
                // ignore errors
            }
        }
        return false;
    }

}