using OxyPlot.Axes;

namespace CryptoScanner.Chart.ViewModels.Chart;

/// <summary>
/// A LinearAxis that generates major ticks aligned to local midnight instead of UTC midnight.
/// X-axis values are CandleTime minutes (minutes since 2010-01-04 UTC).
/// Default OxyPlot ticks at n*step land on UTC midnight; this subclass shifts the tick grid
/// by the negative of the local UTC offset so ticks land on local midnight instead.
/// DST transitions shift the alignment by 1 hour twice per year (accepted trade-off).
/// </summary>
public class LocalMidnightLinearAxis : LinearAxis
{
    public override void GetTickValues(
        out IList<double> majorLabelValues,
        out IList<double> majorTickValues,
        out IList<double> minorTickValues)
    {
        // Let the base class compute minor ticks (kept at their default positions)
        base.GetTickValues(out majorLabelValues, out majorTickValues, out minorTickValues);

        double step = this.ActualMajorStep;
        if (step <= 0)
            return;

        // CandleTime values are in minutes since a UTC epoch.
        // Default: ticks at n*step (UTC midnight). Shift by -utcOffset to hit local midnight.
        // Example: UTC+2 (CEST) → shiftMinutes = -120 → ticks at n*step - 120 = 22:00 UTC = 00:00 local.
        double shiftMinutes = -TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes;

        var aligned = new List<double>();
        double min = this.ActualMinimum;
        double max = this.ActualMaximum;

        // First aligned tick at or after ActualMinimum
        double x = Math.Ceiling((min - shiftMinutes) / step) * step + shiftMinutes;
        while (x <= max + step * 1e-3)
        {
            if (x >= min - step * 1e-3)
                aligned.Add(x);
            x += step;
        }

        majorTickValues = aligned;
        majorLabelValues = aligned;
    }
}
