using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.Analyzers.TradingBuddy.Chart;

public class TradingBuddyOverlay : IChartOverlay
{
    public string Label => "TradingBuddy";
    public string GroupKey => "tb";
    public event Action? RequestRedraw;

    public TradingBuddyOverlay()
    {
        TradingBuddyBands.BandsUpdated += () => RequestRedraw?.Invoke();
    }

    private static readonly OxyColor BandColor = OxyColors.Yellow; //OxyColor.FromArgb(255, 255, 145, 0);   // orange
    private static readonly OxyColor BasisColor = OxyColors.Yellow; //OxyColor.FromArgb(160, 255, 145, 0);  // orange, faint

    public void Draw(object plotModel, CryptoSymbol symbol, CryptoInterval interval,
                     List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        var chart = (PlotModel)plotModel;
        if (candles.Count == 0)
            return;

        TradingBuddyBands.BandSeries? tb = TradingBuddyBands.GetCached(symbol, interval);
        if (tb == null)
            return;

        var upper = new LineSeries { Title = "tb.upper", Color = BandColor, StrokeThickness = 1.5, LineStyle = LineStyle.Dash, YAxisKey = "price", Tag = group };
        var lower = new LineSeries { Title = "tb.lower", Color = BandColor, StrokeThickness = 1.5, LineStyle = LineStyle.Dash, YAxisKey = "price", Tag = group };
        var basis = new LineSeries { Title = "tb.basis", Color = BasisColor, StrokeThickness = 1, LineStyle = LineStyle.Dash, YAxisKey = "price", Tag = group };

        foreach (var candle in candles)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(candle.Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;

            long ms = ((DateTimeOffset)DateTime.SpecifyKind(candle.Date, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
            double x = openTime.Minutes;
            if (tb.Upper.TryGetValue(ms, out double u))
                upper.Points.Add(new DataPoint(x, u));
            if (tb.Lower.TryGetValue(ms, out double l))
                lower.Points.Add(new DataPoint(x, l));
            if (tb.Basis.TryGetValue(ms, out double b))
                basis.Points.Add(new DataPoint(x, b));
        }

        chart.Series.Add(upper);
        chart.Series.Add(lower);
        chart.Series.Add(basis);
    }
}
