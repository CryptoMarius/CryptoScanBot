using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Vbs.Signal;

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
public class VbsSignalLong : VbsSignalVbs
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

        var settings = VbsPlugin.Settings;

        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width out of range {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        //// Cooldown gate (cheapest): no new signal within CooldownBars candles of the last VBS signal.
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


        // The (rarer, more expensive) band break.
        if (!CandleLast.CandleData!.VbsLower.HasValue || !CandleLast.CandleData.BollingerBandsLowerBand.HasValue)
            return false;
        if (!CandleLast.CandleData!.VbsUpper.HasValue || !CandleLast.CandleData.BollingerBandsUpperBand.HasValue)
            return false;

        double lowerBand = CandleLast.CandleData.VbsLower.Value;
        if ((double)CandleLast.Candle.Low >= lowerBand && (double)CandleLast.Candle.Close >= lowerBand)
        {
            ExtraText = "no lower band break";
            return false;
        }

        if (CandleLast.CandleData.BollingerBandsLowerBand.Value <= CandleLast.CandleData.VbsLower.Value)
        {
            ExtraText = "bb.lower <= vbs.band";
            return false;
        }
        if (CandleLast.CandleData.BollingerBandsUpperBand.Value >= CandleLast.CandleData.VbsUpper.Value)
        {
            ExtraText = "bb.upper >= vbs.bands";
            return false;
        }


        // Stop-loss: SLStdevFactor * vwStdev below the lower band.
        // SL price = lowerBand - SLStdevFactor * vwStdev; SL% = that distance as % of the band.
        if (CandleLast.CandleData.VbsVwStdev is not double vwStdev)
            return false;
        double slPrice = lowerBand - settings.SLStdevFactor * vwStdev;
        double pctDeviation = slPrice > 0 ? (lowerBand - slPrice) / lowerBand * 100.0 : 0;

        // Old ATR-based SL: factor * ATR(Length)% — replaced by vwStdev approach above.
        //if (CandleLast.CandleData.VbsAtrSl is not double atr)
        //    return false;
        //double pctDeviation = VbsPlugin.Settings.StopLossAtrFactor * (atr / (double)CandleLast.Candle.Close * 100);

        if (settings.BandMaxPercentage > 0 && pctDeviation > settings.BandMaxPercentage)
        {
            ExtraText = $"band margin {pctDeviation:N2}% exceeds max {settings.BandMaxPercentage:N2}%";
            return false;
        }

        // Multi-timeframe consensus: higher timeframes must also show a band break.
        int consensusCount = ResolveEntryConditions().TimeframeConsensusCount;
        if (consensusCount > 0)
        {
            int confirmed = 0;
            CryptoIntervalPeriod higherPeriod = Interval.IntervalPeriod;
            for (int i = 0; i < consensusCount; i++)
            {
                if (higherPeriod == CryptoIntervalPeriod.interval1w)
                    break;
                higherPeriod++;

                var result = IndicatorEngine.CalculateIndicatorsForInterval(Symbol, Interval, CandleLast.Candle.OpenTime, higherPeriod);
                if (!result.success || result.candle?.CandleData?.VbsLower == null)
                {
                    ExtraText = $"no vbs data on {higherPeriod}";
                    return false;
                }
                double htfLower = result.candle.CandleData.VbsLower.Value;
                double htfLow = (double)result.candle.Candle.Low;
                double htfClose = (double)result.candle.Candle.Close;
                if (htfLow >= htfLower && htfClose >= htfLower)
                {
                    ExtraText = $"no lower band break on {result.higherInterval.Interval.Name}";
                    return false;
                }
                confirmed++;
            }
            if (confirmed < consensusCount)
            {
                ExtraText = $"not enough higher TFs confirmed ({confirmed}/{consensusCount})";
                return false;
            }
        }

        var candle = CandleLast.Candle;
        decimal band = (decimal)lowerBand;

        // Entry = the most extreme of the Close and the band.
        _entryPrice = Math.Min(candle.Close, band);

        if (settings.UseStopLoss)
            _slPercentage = (decimal)pctDeviation;

        //MarkSignalFired();
        ExtraText = $"hit lower band {pctDeviation:N2}% {_entryPrice}";
        return true;
    }
}
