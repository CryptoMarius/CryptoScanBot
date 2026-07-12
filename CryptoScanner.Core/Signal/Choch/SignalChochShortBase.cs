using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Choch;

/// <summary>
/// Short signal on a Change of Character: the ZigZag-derived structure has reversed from
/// Bullish to Bearish (a Lower Low was made in a previous uptrend).
/// </summary>
public abstract class SignalChochShortBase : SignalCreateBase
{
    /// <summary>Selects which trend slot (Primary vs Secondary) this signal reads from.</summary>
    protected abstract TrendType TrendType { get; }

    /// <summary>
    /// When true, IsSignal waits for an opposite ZigZag pivot AFTER the CHoCH event and
    /// for the current candle to break back through that pivot in the CHoCH direction.
    /// Effect: the signal fires LATER than the direct variant.
    /// </summary>
    protected virtual bool RequirePullback => false;

    private SettingsZigZag TrendSettings => TrendType == TrendType.Primary
        ? GlobalData.Settings.Trend.Primary
        : GlobalData.Settings.Trend.Secondary;

    private CryptoTrendData GetBosTrend() => TrendType == TrendType.Primary
        ? SymbolInterval.TrendBosPrimary
        : SymbolInterval.TrendBosSecondary;

    private decimal? _overrideSignalPrice;
    public override decimal? OverrideSignalPrice => _overrideSignalPrice;


    public override bool IsSignal()
    {
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval10m)
            return false;

        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, TrendSettings).Result;

        CryptoTrendData data = GetBosTrend();

        // Pullback+BOS variant accepts both CHoCH and a subsequent BOS (the CHoCH initiated
        // the reversal, the BOS confirms it). Direct and plain-pullback variants require CHoCH.
        bool requireBos = RequirePullback && GlobalData.Settings.Signal.Choch.RequireBosConfirmation;
        if (requireBos)
        {
            if ((data.LastStructureEvent != CryptoStructureEvent.ChoCh &&
                 data.LastStructureEvent != CryptoStructureEvent.Bos) ||
                data.LastStructureEventTime == null)
            {
                ExtraText = "no CHoCH/BOS";
                return false;
            }
        }
        else
        {
            if (data.LastStructureEvent != CryptoStructureEvent.ChoCh ||
                data.LastStructureEventTime == null)
            {
                ExtraText = "no CHoCH";
                return false;
            }
        }

        if (data.Trend != CryptoTrendIndicator.Bearish)
        {
            ExtraText = $"CHoCH but trend={data.Trend}";
            return false;
        }

        // Warm-start guard — see SignalChochLongBase for rationale.
        if (!data.LastFiredStructureEventTimes.ContainsKey(SignalStrategy))
        {
            data.LastFiredStructureEventTimes[SignalStrategy] = data.LastStructureEventTime.Value;
            ExtraText = "warm start: existing CHoCH adopted silently";
            return false;
        }

        // Fire only once per structure event per strategy — see SignalChochLongBase for rationale.
        if (data.LastFiredStructureEventTimes.TryGetValue(SignalStrategy, out var firedAt) &&
            firedAt == data.LastStructureEventTime.Value)
        {
            ExtraText = "CHoCH already fired";
            return false;
        }

        if (RequirePullback)
        {
            // Pullback variant — wait for a NEW ZigZag High to form AFTER the CHoCH event
            // and for the current candle to close back below that pullback pivot.
            if (data.LastPivotType != 'H' ||
                data.LastPivotTime == null ||
                data.LastPivotTime <= data.LastStructureEventTime.Value)
            {
                ExtraText = "waiting for pullback pivot (ZigZag High after CHoCH)";
                return false;
            }

            // Optional BOS confirmation: after the CHoCH a Break of Structure must confirm
            // the new bearish trend before we accept the pullback entry.
            if (requireBos && data.LastStructureEvent != CryptoStructureEvent.Bos)
            {
                ExtraText = "waiting for BOS confirmation after CHoCH";
                return false;
            }

            if (CandleLast.Candle.Close >= data.LastPivotValue)
            {
                ExtraText = $"price {CandleLast.Candle.Close:N8} not below pullback high {data.LastPivotValue:N8}";
                return false;
            }

            if (CandleLast.Candle.Close >= CandleLast.Candle.Open)
            {
                ExtraText = "no bearish candle";
                return false;
            }

            data.LastFiredStructureEventTimes[SignalStrategy] = data.LastStructureEventTime.Value;
            // Pullback variant: signal price = current candle close (the breakthrough),
            // NOT the original CHoCH swing price.
            _overrideSignalPrice = null;
            ExtraText = $"CHoCH pullback → Bearish ({TrendType})";
            return true;
        }

        // Direct variant — fire immediately on the CHoCH event
        data.LastFiredStructureEventTimes[SignalStrategy] = data.LastStructureEventTime.Value;
        _overrideSignalPrice = data.LastStructureEventPrice;
        ExtraText = $"CHoCH → Bearish ({TrendType})";
        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        // Setup invalidated when the BOS trend has flipped back to Bullish.
        if (GetBosTrend().Trend == CryptoTrendIndicator.Bullish)
        {
            ExtraText = $"BOS trend ({TrendType}) reverted to bullish";
            return true;
        }

        return base.GiveUp(signal);
    }
}
