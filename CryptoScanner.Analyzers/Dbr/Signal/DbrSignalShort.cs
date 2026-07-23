using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Dbr.Signal;

/// <summary>
/// "dbr" algorithm — fires a (short) alert when the High breaks the macro upper band of the
/// Donchian Breakout Reversion construction and all enabled filters (trend/RSI/stoch-RSI) agree,
/// i.e. the exact moment the chart prints its upper-band percentage label.
/// The reported percentage matches the chart label.
///
/// Entry placement (same convention as the atrrb signal):
///   - wick only touches the band  -> entry on the band
///   - body breaks through the band -> entry on the close
/// Stop-loss: the band-width percentage shown in the label, placed above the entry.
///
/// When TimeframeConsensusCount > 0, higher timeframes must also confirm the band break
/// (multi-timeframe consensus). Additional filters (RSI, Stoch, Lux5m, trend, zones) are
/// applied only on the lowest (primary) timeframe.
/// </summary>
public class DbrSignalShort : SignalCreateBase
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

        var settings = DbrPlugin.Settings;

        if (settings.UseRsiFilter && !CandleLast.RsiOverbought())
        {
            ExtraText = $"rsi not overbought ({CandleLast.CandleData?.Rsi:N2})";
            return false;
        }

        if (settings.RequireStochOsOb && !CandleLast.StochOverbought())
        {
            ExtraText = "stoch not overbought";
            return false;
        }

        if (!DbrBandsHelper.IsUpperBandBreak(SymbolInterval, CandleLast.Candle.OpenTime, out double bandWidthPct, out double upperBand, out string reason))
        {
            ExtraText = reason;
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

                CryptoSymbolInterval higherSI = Symbol.GetSymbolInterval(higherPeriod);
                if (!DbrBandsHelper.IsUpperBandBreak(higherSI, CandleLast.Candle.OpenTime, out _, out _, out string htfReason))
                {
                    ExtraText = $"no upper band break on {higherSI.Interval.Name}: {htfReason}";
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
        decimal band = (decimal)upperBand;

        // Wick only touches the band -> entry on the band.
        // Body breaks through the band (body high above the band) -> entry on the close.
        decimal bodyHigh = Math.Max(candle.Open, candle.Close);
        _entryPrice = bodyHigh > band ? candle.Close : band;

        // Stop-loss: the band-width percentage (from the label) above the entry.
        // Only hand it to the trader when enabled; otherwise leave null so the trader
        // falls back to its default percentage stop-loss.
        if (settings.UseStopLoss)
            _slPercentage = (decimal)bandWidthPct;

        ExtraText = $"hit upper band {bandWidthPct:N2}%";
        return true;
    }
}
