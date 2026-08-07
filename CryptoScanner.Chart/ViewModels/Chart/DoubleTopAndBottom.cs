using CryptoScanner.Core.Trend;

namespace CryptoScanner.Chart.ViewModels.Chart;

// Experiment, Double tops and bottoms
//
// The calculation moved to CryptoScanner.Core.Trend.DoubleTopAndBottom so the Blazor chart can
// use it too. This forwarder keeps the existing call sites (Dtb.Draw) unchanged.

public class DoubleTopAndBottom
{
    public static List<(ZigZagResult, ZigZagResult, ZigZagResult)> CalculateDoubleTopBottom(ZigZagIndicator indicator)
    {
        return Core.Trend.DoubleTopAndBottom.CalculateDoubleTopBottom(indicator);
    }
}
