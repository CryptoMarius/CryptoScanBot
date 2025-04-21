using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using OxyPlot;

namespace CryptoScanBot.ZoneVisualisation.Chart;

public class Const
{
    public const int OxyFontSize = 14;
    public const string OxyFontName = "Arial";

    // Color combinations, and the boolean is there for the closed zones (same colors after complaints)
    public static readonly Dictionary<(CryptoZoneKind, CryptoTradeSide, bool), (OxyColor boxColor, OxyColor textColor)> ColorList = new()
    {
        { (CryptoZoneKind.DominantLevel, CryptoTradeSide.Long, false), (OxyColors.Green, OxyColors.White) },
        { (CryptoZoneKind.DominantLevel, CryptoTradeSide.Short, false), (OxyColors.OrangeRed, OxyColors.White) },
        { (CryptoZoneKind.DominantLevel, CryptoTradeSide.Long, true), (OxyColors.Green, OxyColors.White) }, // Red
        { (CryptoZoneKind.DominantLevel, CryptoTradeSide.Short, true), (OxyColors.OrangeRed, OxyColors.White) }, // Yellow

        { (CryptoZoneKind.FairValueGap, CryptoTradeSide.Long, false), (OxyColors.LightGray, OxyColors.White) },
        { (CryptoZoneKind.FairValueGap, CryptoTradeSide.Short, false), (OxyColors.LightGray, OxyColors.White) },
        { (CryptoZoneKind.FairValueGap, CryptoTradeSide.Long, true), (OxyColors.DarkGray, OxyColors.White) }, // Red
        { (CryptoZoneKind.FairValueGap, CryptoTradeSide.Short, true), (OxyColors.DarkGray, OxyColors.White) }, // Red
    };

}
