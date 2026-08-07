namespace CryptoScanner.Core.Trend;

// Experiment, Double tops and bottoms
//
// Lived in the Avalonia chart project; moved to Core (it only uses Core types) so the Blazor
// chart page can draw the DTB overlay as well.

public class DoubleTopAndBottom
{
    public static List<(ZigZagResult, ZigZagResult, ZigZagResult)> CalculateDoubleTopBottom(ZigZagIndicator indicator)
    {
        // how? Steven Hart (The Trading Channel):
        // Using a Swing High - Draw a box from the high to the close / low to open
        // if a next high/low connects with that rectangle (not cross it with close)

        // Not sure if enumerating the points will do the job...
        // buy hey, lets just try it for now...
        List<(ZigZagResult, ZigZagResult, ZigZagResult)> l = [];

        // primary trend
        ZigZagResult? previous2 = null;
        ZigZagResult? previous1 = null;
        foreach (var p0 in indicator.ZigZagList)
        {
            if (previous1 != null && previous2 != null && p0.PivotIndex + 2 < indicator.PivotList.Count)
            {
                ZigZagResult p1 = indicator.PivotList[p0.PivotIndex + 1];
                ZigZagResult p2 = indicator.PivotList[p0.PivotIndex + 2];
                //ZigZagResult px = data.Indicator.PivotList[p0.PivotIndex + 3];

                // double top
                if (previous1.PointType == 'L' && p0.PointType == 'H' && p1.PointType == 'L' && p2.PointType == 'H'
                    && previous1.Value < p1.Value) //&& previous2.Value < p0.Value
                {
                    decimal value = Math.Abs(Math.Max(p2.Candle.Open, p2.Candle.Close) - Math.Max(p0.Candle.Open, p0.Candle.Close));
                    decimal perc = 100 * value / Math.Max(p0.Candle.Open, p0.Candle.Close);
                    if (perc < 0.75m)
                    {
                        l.Add((p2, p1, p0));
                    }
                }

                // double bottom
                if (previous1.PointType == 'H' && p0.PointType == 'L' && p1.PointType == 'H' && p2.PointType == 'L'
                    && previous1.Value > p1.Value) //&& previous2.Value > p0.Value
                {
                    decimal value = Math.Abs(Math.Min(p2.Candle.Open, p2.Candle.Close) - Math.Min(p0.Candle.Open, p0.Candle.Close));
                    decimal perc = 100 * value / Math.Min(p0.Candle.Open, p0.Candle.Close);
                    if (perc < 0.75m)
                    {
                        l.Add((p2, p1, p0));
                    }
                }
            }
            previous2 = previous1;
            previous1 = p0;
        }
        return l;
    }


}
