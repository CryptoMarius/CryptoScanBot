using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.SuperTrendBreakout.Signal;

public class SignalSuperTrendBreakoutBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.SuperTrend == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    /// <summary>
    /// Detect a SuperTrend direction flip by comparing current and previous candle.
    /// Bullish flip: previous had UpperBand set (bearish), current has LowerBand set (bullish).
    /// Bearish flip: previous had LowerBand set (bullish), current has UpperBand set (bearish).
    /// </summary>
    protected bool IsSuperTrendFlip(bool bullish)
    {
        if (!GetPrevCandle(CandleLast!, out MyData? prev))
            return false;

        if (prev?.CandleData?.SuperTrend == null)
            return false;

        if (bullish)
        {
            // Bullish flip: prev was bearish (UpperBand != null), current is bullish (LowerBand != null)
            return prev.CandleData.SuperTrendUpperBand != null
                && CandleLast!.CandleData!.SuperTrendLowerBand != null;
        }
        else
        {
            // Bearish flip: prev was bullish (LowerBand != null), current is bearish (UpperBand != null)
            return prev.CandleData.SuperTrendLowerBand != null
                && CandleLast!.CandleData!.SuperTrendUpperBand != null;
        }
    }


    /// <summary>
    /// Check if price is near an open DLZ zone OR a recently closed one.
    /// This avoids the problem where zones get closed by other strategies before
    /// the SuperTrend flip happens.
    /// </summary>
    protected bool WasNearDlzZone(CryptoTradeSide side, out string zoneInfo)
    {
        zoneInfo = "";
        var settings = SuperTrendBreakoutPlugin.Settings;
        var dlzSettings = GlobalData.Settings.Signal.ZonesDlz;
        var candle = CandleLast!.Candle;
        var symbolData = Symbol.Data;

        decimal warnPct = dlzSettings.NearZonePercentage;

        foreach (var intervalName in SignalPrepare.EffectiveDlzIntervals)
        {
            if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
                continue;

            var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);
            var zones = symbolIntervalData.DlzZones;

            // Check open zones first
            if (settings.IncludeOpenZones)
            {
                if (CheckZoneProximity(side, candle, side == CryptoTradeSide.Long ? zones.LongOpen : zones.ShortOpen,
                    warnPct, intervalName, "open", out zoneInfo))
                    return true;
            }

            // Check recently closed zones
            if (settings.IncludeClosedZones)
            {
                var closedZones = side == CryptoTradeSide.Long ? zones.LongClosed : zones.ShortClosed;
                CandleTime maxAge = candle.OpenTime - (settings.ClosedZoneMaxAgeCandles * Interval.Duration);

                if (CheckZoneProximity(side, candle, closedZones, warnPct, intervalName, "closed",
                    out zoneInfo, minCloseTime: maxAge))
                    return true;
            }
        }

        return false;
    }


    private static bool CheckZoneProximity(CryptoTradeSide side, CryptoCandle candle,
        OrderedList<CryptoZone> zones, decimal warnPct, string intervalName, string zoneState,
        out string zoneInfo, CandleTime? minCloseTime = null)
    {
        zoneInfo = "";

        for (int i = 0; i < zones.Count; i++)
        {
            var zone = zones[i];

            if (candle.OpenTime < zone.OpenTime)
                continue;

            // For closed zones, only consider recently closed ones
            if (minCloseTime.HasValue && zone.CloseTime.HasValue && zone.CloseTime.Value < minCloseTime.Value)
                continue;

            if (side == CryptoTradeSide.Long)
            {
                decimal toleranceTop = zone.Top * (100 + warnPct) / 100;
                if (candle.Low > toleranceTop)
                    continue;

                if (candle.Close >= zone.Bottom)
                {
                    decimal dist = 100m * (candle.Close - zone.Top) / candle.Close;
                    zoneInfo = $"dlz {intervalName} {zoneState} {zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                    return true;
                }
            }
            else
            {
                decimal toleranceBottom = zone.Bottom * (100 - warnPct) / 100;
                if (candle.High < toleranceBottom)
                    continue;

                if (candle.Close <= zone.Top)
                {
                    decimal dist = 100m * (zone.Bottom - candle.Close) / candle.Close;
                    zoneInfo = $"dlz {intervalName} {zoneState} {zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                    return true;
                }
            }
        }

        return false;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        if (base.GiveUp(signal))
            return true;

        // Give up when the SuperTrend flips back against the signal direction
        if (SignalSide == CryptoTradeSide.Long && CandleLast?.CandleData?.SuperTrendUpperBand != null)
        {
            ExtraText = "SuperTrend flipped bearish";
            return true;
        }
        if (SignalSide == CryptoTradeSide.Short && CandleLast?.CandleData?.SuperTrendLowerBand != null)
        {
            ExtraText = "SuperTrend flipped bullish";
            return true;
        }

        ExtraText = "";
        return false;
    }
}
