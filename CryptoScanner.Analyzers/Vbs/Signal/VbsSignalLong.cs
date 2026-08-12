using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Vbs.Signal;

/// <summary>
/// Mean Reversion Bands — long signal. Fires when price breaks the LOWER band (wick or close) while
/// RSI is oversold (confluence). Optionally suppressed while the coin is in a DOWN-slide (don't catch a
/// falling knife). Entry on the band, or on the close when the close itself broke through; stop-loss =
/// Entry - ACS% (Average Candle Size, precomputed on CandleData.VbsAcs).
///
/// When TimeframeConsensusCount > 0, higher timeframes must also confirm the band break
/// (multi-timeframe consensus). Additional filters (RSI, Stoch, Lux5m, trend, zones) are
/// applied only on the lowest (primary) timeframe.
/// </summary>
public class VbsSignalLong : VbsSignalVbs
{
    private decimal? _entryPrice;
    private decimal? _slPercentage;
    private decimal? _tpPercentage;

    public override decimal? OverrideSignalPrice => _entryPrice;
    public override decimal? OverrideSlPercentage => _slPercentage;
    public override decimal? OverrideProfitPercentage => _tpPercentage;

    public override bool IsSignal()
    {
        ExtraText = "";
        _entryPrice = null;
        _slPercentage = null;
        _tpPercentage = null;

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


        // The (rarer, more expensive) band break. The VBS values live in the plugin's own
        // per-candle object; a null slot simply means the indicator has not warmed up yet.
        var vbs = CandleLast.CandleData!.GetPluginData<VbsCandleData>();
        if (vbs?.Lower == null || !CandleLast.CandleData.BollingerBandsLowerBand.HasValue)
            return false;
        if (vbs.Upper == null || !CandleLast.CandleData.BollingerBandsUpperBand.HasValue)
            return false;

        double lowerBand = vbs.Lower.Value;
        if ((double)CandleLast.Candle.Low >= lowerBand && (double)CandleLast.Candle.Close >= lowerBand)
        {
            ExtraText = "no lower band break";
            return false;
        }

        if (CandleLast.CandleData.BollingerBandsLowerBand.Value <= vbs.Lower.Value)
        {
            ExtraText = "bb.lower <= vbs.band";
            return false;
        }
        if (CandleLast.CandleData.BollingerBandsUpperBand.Value >= vbs.Upper.Value)
        {
            ExtraText = "bb.upper >= vbs.bands";
            return false;
        }


        // Stop-loss = Entry - ACS% (long). ACS (Average Candle Size) is precomputed on CandleData;
        // the SL distance % equals the average candle size % (reverse-engineered from TradingBuddy).
        double pctDeviation = vbs.Acs ?? 0;

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
                // The candle in its own local, and checked on its own: the compiler cannot see
                // that a non-null band value implies a non-null candle, so reading through
                // result.candle below was flagged as a possible null dereference.
                var htfCandle = result.candle;
                var htfVbs = htfCandle?.CandleData?.GetPluginData<VbsCandleData>();
                if (!result.success || htfCandle == null || htfVbs?.Lower == null)
                {
                    ExtraText = $"no vbs data on {higherPeriod}";
                    return false;
                }
                double htfLower = htfVbs.Lower.Value;
                double htfLow = (double)htfCandle.Candle.Low;
                double htfClose = (double)htfCandle.Candle.Close;
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

        // Take-profit = RiskRewardRatio * SL-distance (RiskRewardRatio * ACS%), handed to the trader as a
        // single TP via OverrideProfitPercentage.
        if (settings.UseTakeProfit && pctDeviation > 0)
            _tpPercentage = (decimal)(settings.RiskRewardRatio * pctDeviation);

        //MarkSignalFired();
        ExtraText = $"hit lower band {pctDeviation:N2}% {_entryPrice}";
        return true;
    }
}
