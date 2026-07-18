using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Baba.Signal;

/// <summary>
/// Mean Reversion Bands — long signal. Fires when price breaks the LOWER band (wick or close) while
/// RSI is oversold (confluence). Optionally suppressed while the coin is in a DOWN-slide (don't catch a
/// falling knife). Entry on the band, or on the close when the close itself broke through; stop-loss =
/// SLStdevFactor * vwStdev below the lower band.
///
/// When TimeframeConsensusCount > 0, higher timeframes must also confirm the band break
/// (multi-timeframe consensus). Additional filters (RSI, Stoch, Lux5m, trend, zones) are
/// applied only on the lowest (primary) timeframe.
/// </summary>
public class BabaSignalLong : BabaSignalBaba
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

        if (settings.UseRsiFilter && !CandleLast.RsiOversold())
        {
            ExtraText = $"rsi not oversold ({CandleLast.CandleData?.Rsi:N0})";
            return false;
        }

        if (settings.RequireStochOsOb && !CandleLast.StochOversold())
        {
            ExtraText = "stoch not oversold";
            return false;
        }

        if (settings.OnlyIfLux5m)
        {
            int needed = settings.Lux5mPercentage;
            if (CandleLast.CandleData!.Lux5mValue > -needed)
            {
                ExtraText = $"lux 5m not oversold enough ({CandleLast.CandleData!.Lux5mValue}%, need <= -{needed}%)";
                return false;
            }
        }

        //// The (rarer, more expensive) lower-band break.
        if (!CandleLast.CandleData!.BabaLower.HasValue)
            return false;
        double lowerBand = CandleLast.CandleData.BabaLower.Value;
        if ((double)CandleLast.Candle.Low >= lowerBand && (double)CandleLast.Candle.Close >= lowerBand)
        {
            ExtraText = "no lower band break";
            return false;
        }

        // Stop-loss: SLStdevFactor * vwStdev below the lower band.
        // SL price = lowerBand - SLStdevFactor * vwStdev; SL% = that distance as % of the band.
        if (CandleLast.CandleData.BabaVwStdev is not double vwStdev)
            return false;
        double slPrice = lowerBand - settings.SLStdevFactor * vwStdev;
        double pctDeviation = slPrice > 0 ? (lowerBand - slPrice) / lowerBand * 100.0 : 0;

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
                if (!result.success || result.candle?.CandleData?.BabaLower == null)
                {
                    ExtraText = $"no baba data on {higherPeriod}";
                    return false;
                }
                double htfLower = result.candle.CandleData.BabaLower.Value;
                double htfLow = (double)result.candle.Candle.Low;
                double htfClose = (double)result.candle.Candle.Close;
                if (htfLow >= htfLower && htfClose >= htfLower)
                {
                    ExtraText = $"no lower band break on {result.higherInterval.Interval.Name}";
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

        // Symmetric slide filter: don't go long into an ongoing efficient DOWN-slide.
        if (settings.UseSlideFilter)
        {
            BabaBandsHelper.ComputeSlide(SymbolInterval, CandleLast.Candle.OpenTime, out bool slidingDown, out _);
            if (slidingDown)
            {
                ExtraText = "suppressed: down-slide active";
                return false;
            }
        }

        if (settings.CheckTrendPrimaryDirection && !CheckTrendPrimary(settings.TrendPrimaryDirectionCount))
            return false;
        if (settings.CheckTrendSecondaryDirection && !CheckTrendSecondary(settings.TrendSecondaryDirectionCount))
            return false;

        if (!CheckMa200Filter(settings.CheckPriceAboveMa200, settings.Ma200MinDistancePercentage, settings.Ma200ConfirmationCandles))
            return false;

        // Optional DLZ/FVG/SMC zone confluence (settings checkboxes). Checked only after the rare band
        // break, so the zone lookup runs sparingly.
        if (!CheckEnabledZoneRejections(out string zoneInfo))
        {
            ExtraText = zoneInfo;
            return false;
        }

        var candle = CandleLast.Candle;
        decimal band = (decimal)lowerBand;

        // Entry = the most extreme of the Close and the band.
        _entryPrice = Math.Min(candle.Close, band);

        if (settings.UseStopLoss)
            _slPercentage = (decimal)pctDeviation;

        //MarkSignalFired();
        ExtraText = $"hit lower band {pctDeviation:N2}%{(zoneInfo != "" ? " @ " + zoneInfo : "")} {_entryPrice}";
        return true;
    }
}
