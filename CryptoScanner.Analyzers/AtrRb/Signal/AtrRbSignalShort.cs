using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.AtrRb.Signal;

/// <summary>
/// "settings" algorithm — fires a (short) alert when price hits the macro upper band of the
/// AtrRb Bands construction, i.e. the exact moment the chart prints its upper-band percentage
/// label. The reported percentage matches the chart label.
/// </summary>
public class AtrRbSignalShort : SignalCreateBase
{
    private decimal? _entryPrice;
    private decimal? _slPercentage;

    public override decimal? OverrideSignalPrice => _entryPrice;
    public override decimal? OverrideSlPercentage => _slPercentage;

    public override bool IsSignal()
    {
        ExtraText = "";
        _entryPrice = null;
        _slPercentage = null;

        var settings = AtrRbPlugin.Settings;
        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width out of range {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        if (settings.RequireRsiOsOb && !CandleLast.RsiOverbought())
        {
            ExtraText = $"RSI not overbought ({CandleLast.CandleData!.Rsi:N2})";
            return false;
        }

        if (settings.RequireStochOsOb && !CandleLast.StochOverbought())
        {
            ExtraText = "stoch not overbought";
            return false;
        }

        if (!AtrRbBandsHelper.IsUpperBandBreak(SymbolInterval, CandleLast.Candle.OpenTime, out double pctDeviation, out double upperBand))
        {
            ExtraText = "no upper band break";
            return false;
        }

        // Band break confirmation: higher timeframes must also show a band break.
        int confirmationCount = settings.BandBreakConfirmationCount;
        if (confirmationCount > 0)
        {
            int confirmed = 0;
            CryptoIntervalPeriod higherPeriod = Interval.IntervalPeriod;
            for (int i = 0; i < confirmationCount; i++)
            {
                if (higherPeriod == CryptoIntervalPeriod.interval1w)
                    break;
                higherPeriod++;

                CryptoSymbolInterval higherSI = Symbol.GetSymbolInterval(higherPeriod);
                if (!AtrRbBandsHelper.IsUpperBandBreak(higherSI, CandleLast.Candle.OpenTime, out _, out _))
                {
                    ExtraText = $"no upper band break on {higherSI.Interval.Name}";
                    return false;
                }
                confirmed++;
            }
            if (confirmed < confirmationCount)
            {
                ExtraText = $"not enough higher timeframes confirmed ({confirmed}/{confirmationCount})";
                return false;
            }
        }

        var candle = CandleLast.Candle;
        decimal band = (decimal)upperBand;

        // Wick only touches the band -> entry on the band.
        // Body breaks through the band (body high above the band) -> entry on the close.
        decimal bodyHigh = Math.Max(candle.Open, candle.Close);
        _entryPrice = bodyHigh > band ? candle.Close : band;
        //_entryPrice = Math.Max(candle.Close, band);
        //var _entryPrice2 = Math.Max(candle.Close, band);
        //if (_entryPrice2 != _entryPrice)
        //    _entryPrice = _entryPrice2;

        // Stop-loss: the same percentage (from the label) above the entry.
        // Only hand it to the trader when enabled; otherwise leave null so the trader
        // falls back to its default percentage stop-loss.
        if (settings.UseStopLoss)
            _slPercentage = (decimal)pctDeviation;

        ExtraText = $"hit upper band {pctDeviation:N2}%";
        return true;
    }
}
