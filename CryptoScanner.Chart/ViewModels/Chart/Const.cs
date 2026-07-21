using CryptoScanner.Core.Enums;

using OxyPlot;

namespace CryptoScanner.Chart.ViewModels.Chart;

public class Const
{
    public const int OxyFontSize = 14;
    public const string OxyFontName = "Arial";

    // Color combinations, and the boolean is there for the closed zones (same colors after complaints)
    public static readonly Dictionary<(CryptoZoneKind, CryptoTradeSide, bool), (OxyColor boxColor, OxyColor textColor)> ColorList = new()
    {
        { (CryptoZoneKind.DominantLevel, CryptoTradeSide.Long, false), (OxyColors.DarkGreen, OxyColors.White) },
        { (CryptoZoneKind.DominantLevel, CryptoTradeSide.Short, false), (OxyColors.DarkRed, OxyColors.White) },
        { (CryptoZoneKind.DominantLevel, CryptoTradeSide.Long, true), (OxyColors.DarkGreen, OxyColors.White) },
        { (CryptoZoneKind.DominantLevel, CryptoTradeSide.Short, true), (OxyColors.DarkRed, OxyColors.White) },

        { (CryptoZoneKind.FairValueGap, CryptoTradeSide.Long, false), (OxyColors.DarkGray, OxyColors.White) },
        { (CryptoZoneKind.FairValueGap, CryptoTradeSide.Short, false), (OxyColors.DarkGray, OxyColors.White) },
        { (CryptoZoneKind.FairValueGap, CryptoTradeSide.Long, true), (OxyColors.DarkGray, OxyColors.White) },
        { (CryptoZoneKind.FairValueGap, CryptoTradeSide.Short, true), (OxyColors.DarkGray, OxyColors.White) },

        // SMC Order Blocks — distinct from DLZ/FVG so they're visually identifiable on the chart.
        // Long  = bullish OB (demand): the last bearish candle before an up-impulse → soft teal.
        // Short = bearish OB (supply): the last bullish candle before a down-impulse → soft purple.
        { (CryptoZoneKind.OrderBlock, CryptoTradeSide.Long, false), (OxyColors.SteelBlue, OxyColors.White) },
        { (CryptoZoneKind.OrderBlock, CryptoTradeSide.Short, false), (OxyColors.MediumPurple, OxyColors.White) },
        { (CryptoZoneKind.OrderBlock, CryptoTradeSide.Long, true), (OxyColors.SteelBlue, OxyColors.White) },
        { (CryptoZoneKind.OrderBlock, CryptoTradeSide.Short, true), (OxyColors.MediumPurple, OxyColors.White) },
    };

}
