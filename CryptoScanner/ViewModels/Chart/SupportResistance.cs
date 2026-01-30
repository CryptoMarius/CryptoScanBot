using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using OxyPlot;
using OxyPlot.Annotations;

namespace CryptoScanner.ViewModels.Chart;

public class SupportResistanceLevel
{
    public decimal Price { get; set; }
    public long FirstTime { get; set; }
    public string Type { get; set; } // "Support" of "Resistance"
    public int HitCount { get; set; } = 1;
}

public class SupportResistance
{
    public static List<SupportResistanceLevel> Detect(List<ZigZagResult> candles,
        decimal mergeTolerancePercent = 0.5m, // marge in procent
        int minHits = 1)
    {
        var rawLevels = new List<SupportResistanceLevel>();

        for (int i = 0; i < candles.Count; i++)
        {
            var current = candles[i];
            bool isSwingLow = current.PointType == 'L';
            bool isSwingHigh = current.PointType == 'H';

            if (isSwingHigh)
                AddOrMergeLevel(rawLevels, current.Value, current.Candle.OpenTime, "Resistance", mergeTolerancePercent);

            if (isSwingLow)
                AddOrMergeLevel(rawLevels, current.Value, current.Candle.OpenTime, "Support", mergeTolerancePercent);
        }

        // Filter op minimale hits
        return rawLevels.Where(l => l.HitCount >= minHits).ToList();
    }


    private static void AddOrMergeLevel(List<SupportResistanceLevel> levels, decimal price,
        long time, string type, decimal tolerancePercent)
    {
        var toleranceValue = price * (tolerancePercent / 100m);
        var existing = levels.FirstOrDefault(l =>
            l.Type == type && Math.Abs(l.Price - price) <= toleranceValue);

        if (existing != null)
        {
            // Merge → gemiddelde prijs + hitcount ophogen
            existing.Price = (existing.Price * existing.HitCount + price) / (existing.HitCount + 1);
            existing.HitCount++;
        }
        else
        {
            levels.Add(new SupportResistanceLevel
            {
                Price = price,
                FirstTime = time,
                Type = type,
                HitCount = 1
            });
        }
    }


    internal static void Draw(PlotModel chart, CryptoInterval interval, List<ZigZagResult> candles)
    {
        if (candles.Count == 0)
            return;

        //var list = Detect(candles);

        //long first = candles.First().Candle.OpenTime;
        //long last = candles.Last().Candle.OpenTime;

        foreach (var bb in candles)
        {

            //if (bb.openTime >= minDate && openTime <= maxDate)
            {
                double value = (double)bb.Value;
                //seriesSma.Points.Add(new DataPoint(bb.FirstTime, value));
                //OxyColor color = bb.Type == "Support" ? OxyColors.Green : OxyColors.Red;
                OxyColor color = bb.PointType == 'L' ? OxyColors.Green : OxyColors.Red;

                LineAnnotation annotation = new()
                {
                    Type = LineAnnotationType.Horizontal,
                    Y = value,
                    MinimumX = bb.Candle.OpenTime,
                    MaximumX = bb.Candle.OpenTime + 25 * interval.Duration,
                    Color = color,
                    //Text = "Horizontal" 
                };

                chart.Annotations.Add(annotation);
            }
        }

    }

}

