using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Baba.Signal;

/// <summary>
/// Mean Reversion Bands — short signal. Fires when price breaks the UPPER band (wick or close) while
/// RSI is overbought (confluence). Optionally suppressed while the coin is in an UP-slide (don't short a
/// melt-up). Entry on the band, or on the close when the close itself broke through; stop-loss =
/// SLStdevFactor * vwStdev above the upper band.
///
/// When TimeframeConsensusCount > 0, higher timeframes must also confirm the band break
/// (multi-timeframe consensus). Additional filters (RSI, Stoch, Lux5m, trend, zones) are
/// applied only on the lowest (primary) timeframe.
/// </summary>
public class BabaSignalShort : BabaSignalBaba
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

        var settings = BabaPlugin.Settings;

        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width out of range {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        //// Cooldown gate (cheapest): no new signal within CooldownBars candles of the last Baba signal.
        //if (InCooldown())
        //{
        //    ExtraText = "cooldown active";
        //    return false;
        //}

        if (settings.UseRsiFilter && !CandleLast.RsiOverbought())
        {
            ExtraText = $"rsi not overbought ({CandleLast.CandleData?.Rsi:N0})";
            return false;
        }

        if (settings.RequireStochOsOb && !CandleLast.StochOverbought())
        {
            ExtraText = "stoch not overbought";
            return false;
        }

        //// The (rarer, more expensive) upper-band break.
        if (!CandleLast.CandleData!.BabaUpper.HasValue)
            return false;
        double upperBand = CandleLast.CandleData.BabaUpper.Value;
        if ((double)CandleLast.Candle.High <= upperBand && (double)CandleLast.Candle.Close <= upperBand)
        {
            ExtraText = "no upper band break";
            return false;
        }

        // Stop-loss: SLStdevFactor * vwStdev above the upper band.
        // SL price = upperBand + SLStdevFactor * vwStdev; SL% = that distance as % of the band.
        if (CandleLast.CandleData.BabaVwStdev is not double vwStdev)
            return false;
        double slPrice = upperBand + settings.SLStdevFactor * vwStdev;
        double pctDeviation = upperBand > 0 ? (slPrice - upperBand) / upperBand * 100.0 : 0;

        // Old ATR-based SL: factor * ATR(Length)% — replaced by vwStdev approach above.
        //if (CandleLast.CandleData.BabaAtrSl is not double atr)
        //    return false;
        //double pctDeviation = BabaPlugin.Settings.StopLossAtrFactor * (atr / (double)CandleLast.Candle.Close * 100);

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

                var result = IndicatorEngine.CalculateIndicatorsForInterval(Symbol, Interval, CandleLast.Candle.OpenTime, higherPeriod);
                if (!result.success || result.candle?.CandleData?.BabaUpper == null)
                {
                    ExtraText = $"no baba data on {higherPeriod}";
                    return false;
                }
                double htfUpper = result.candle.CandleData.BabaUpper.Value;
                double htfHigh = (double)result.candle.Candle.High;
                double htfClose = (double)result.candle.Candle.Close;
                if (htfHigh <= htfUpper && htfClose <= htfUpper)
                {
                    ExtraText = $"no upper band break on {result.higherInterval.Interval.Name}";
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

        // Symmetric slide filter: don't go short into an ongoing efficient UP-slide (melt-up).
        if (settings.UseSlideFilter)
        {
            BabaBandsHelper.ComputeSlide(SymbolInterval, CandleLast.Candle.OpenTime, out _, out bool slidingUp);
            if (slidingUp)
            {
                ExtraText = "suppressed: up-slide active";
                return false;
            }
        }

        var candle = CandleLast.Candle;
        decimal band = (decimal)upperBand;

        // Entry = the most extreme of the Close and the band.
        _entryPrice = Math.Max(candle.Close, band);

        if (settings.UseStopLoss)
            _slPercentage = (decimal)pctDeviation;

        //MarkSignalFired();
        ExtraText = $"hit upper band {pctDeviation:N2}% {_entryPrice}";
        return true;
    }
}
