using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.AtrRb.Signal;

/// <summary>
/// "settings" algorithm — fires a (long) alert when price hits the macro lower band of the
/// AtrRb Bands construction, i.e. the exact moment the chart prints its lower-band percentage
/// label. The reported percentage matches the chart label.
/// </summary>
public class AtrRbSignalLong : SignalCreateBase
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

        if (settings.RequireRsiOsOb && !CandleLast.RsiOversold())
        {
            ExtraText = $"RSI not oversold ({CandleLast.CandleData!.Rsi:N2})";
            return false;
        }

        if (settings.RequireStochOsOb && !CandleLast.StochOversold())
        {
            ExtraText = "stoch not oversold";
            return false;
        }

        if (!AtrRbBandsHelper.IsLowerBandBreak(SymbolInterval, CandleLast.Candle.OpenTime, out double pctDeviation, out double lowerBand))
        {
            ExtraText = "no lower band break";
            return false;
        }

        // Multi-timeframe consensus: higher timeframes must also show a band break.
        if (settings.TimeframeConsensusCount > 0)
        {
            int confirmed = 0;
            CryptoIntervalPeriod higherPeriod = Interval.IntervalPeriod;
            for (int i = 0; i < settings.TimeframeConsensusCount; i++)
            {
                if (higherPeriod == CryptoIntervalPeriod.interval1w)
                    break;
                higherPeriod++;

                CryptoSymbolInterval higherSI = Symbol.GetSymbolInterval(higherPeriod);
                if (!AtrRbBandsHelper.IsLowerBandBreak(higherSI, CandleLast.Candle.OpenTime, out _, out _))
                {
                    ExtraText = $"no lower band break on {higherSI.Interval.Name}";
                    return false;
                }
                confirmed++;
            }
            if (confirmed < settings.TimeframeConsensusCount)
            {
                ExtraText = $"not enough higher TFs confirmed ({confirmed}/{settings.TimeframeConsensusCount})";
                return false;
            }
        }

        var candle = CandleLast.Candle;
        decimal band = (decimal)lowerBand;

        // Wick only touches the band -> entry on the band.
        // Body breaks through the band (body low below the band) -> entry on the close.
        decimal bodyLow = Math.Min(candle.Open, candle.Close);
        _entryPrice = bodyLow < band ? candle.Close : band;
        //_entryPrice = Math.Min(candle.Close, band);
        //var _entryPrice2 = Math.Min(candle.Close, band);
        //if (_entryPrice2 != _entryPrice)
        //    _entryPrice = _entryPrice2;


        // Stop-loss: the same percentage (from the label) below the entry.
        // Only hand it to the trader when enabled; otherwise leave null so the trader
        // falls back to its default percentage stop-loss.
        if (settings.UseStopLoss)
            _slPercentage = (decimal)pctDeviation;


        ExtraText = $"hit lower band {pctDeviation:N2}%";
        return true;
    }
}
