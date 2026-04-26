using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Trend;

/// <summary>
/// Box Theory — Long entry.
///
/// The "box" is the high/low range of the previous full 1d candle. A long signal fires
/// the moment the active-interval close breaks above the previous-day high.
///
/// We require the previous active-interval close to still have been at or below the box
/// top, so the signal fires once on the transition (not on every candle that happens to
/// stay above the level for the rest of the day).
/// </summary>
public class SignalBoxLong : SignalCreateBase
{
    // Time limit for the trader to pick up the signal before we give up.
    private const int GiveUpCandles = 10;

    public override bool IsSignal()
    {
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval15m ||
            Interval.IntervalPeriod >= CryptoIntervalPeriod.interval1d)
            return false;

        if (!TryGetPreviousDayBox(out decimal boxTop, out decimal boxBottom, out string error))
        {
            ExtraText = error;
            return false;
        }

        decimal minHeight = GlobalData.Settings.Signal.Box.MinBoxHeightPercent;
        if (minHeight > 0m && boxBottom > 0m)
        {
            decimal heightPercent = (boxTop - boxBottom) / boxBottom * 100m;
            if (heightPercent < minHeight)
            {
                ExtraText = $"box too small ({heightPercent:N2}%)";
                return false;
            }
        }

        decimal lastClose = CandleLast.Candle.Close;
        if (lastClose <= boxTop)
        {
            ExtraText = $"waiting for break above prev-day high {boxTop:N8}";
            return false;
        }

        // Anti-duplicate: previous active candle must still have been inside the box.
        if (!GetPrevCandle(CandleLast, out MyData? prev) || prev == null)
        {
            ExtraText = "no previous candle";
            return false;
        }
        if (prev.Candle.Close > boxTop)
        {
            ExtraText = "prev-day high was already broken in previous candle";
            return false;
        }

        ExtraText = $"Box Long break @ {lastClose:N8} (prev day {boxBottom:N8}-{boxTop:N8})";
        return true;
    }


    /// <summary>
    /// Resolve the previous full 1d candle and return its high/low as the box.
    /// </summary>
    private bool TryGetPreviousDayBox(out decimal boxTop, out decimal boxBottom, out string error)
    {
        boxTop = 0m;
        boxBottom = 0m;
        error = "";

        CryptoInterval interval1d = Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1d).Interval;

        // Make sure the 1d candle list + indicator data is loaded for this symbol.
        if (!IndicatorDataList.PrepareIndicators(Symbol, interval1d, CandleLast.Candle.OpenTime))
        {
            error = "1d data not available";
            return false;
        }

        // Align current time down to the start of today's 1d candle, then step one day back.
        CandleTime todayStart = CandleLast.Candle.OpenTime.AlignToIntervalMinutes(interval1d.Duration);
        CandleTime previousDayOpen = todayStart - interval1d.Duration;

        if (!IndicatorDataList.TryGetCandle(interval1d, previousDayOpen, out MyData? prevDay) || prevDay == null)
        {
            error = "no previous-day 1d candle";
            return false;
        }

        boxTop = prevDay.Candle.High;
        boxBottom = prevDay.Candle.Low;
        return true;
    }


    /// <summary>
    /// Give up when the trader fails to pick up the signal within GiveUpCandles bars
    /// after it fired (for example when no trading slot is free).
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        if (CandleTime.FromDateTime(signal.CloseDate).Minutes + GiveUpCandles * Interval.Duration < CandleLast.Candle.OpenTime.Minutes)
        {
            ExtraText = $"give up after {GiveUpCandles} candles";
            return true;
        }
        return false;
    }
}
