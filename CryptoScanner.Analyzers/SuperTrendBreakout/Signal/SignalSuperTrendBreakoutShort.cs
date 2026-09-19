using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.SuperTrendBreakout.Signal;

// DEBUG only: everything below reads CryptoData.SuperTrend / SuperTrendUpperBand /
// SuperTrendLowerBand, and those three fields only exist in a Debug build. See
// CryptoData.SuperTrend and SuperTrendBreakoutPlugin.Strategies.
#if DEBUG

public class SignalSuperTrendBreakoutShort : SignalSuperTrendBreakoutBase
{
    public override bool IsSignal()
    {
        ExtraText = "";

        // SuperTrend must have just flipped from bullish to bearish
        if (!IsSuperTrendFlip(bullish: false))
        {
            ExtraText = "No bearish SuperTrend flip";
            return false;
        }

        // Price must be near (or recently was near) a DLZ resistance zone
        if (!WasNearDlzZone(CryptoTradeSide.Short, out string zoneInfo))
        {
            ExtraText = "No DLZ zone nearby";
            return false;
        }

        ExtraText = $"SuperTrend bearish flip near {zoneInfo}";
        return true;
    }


    public override bool AdditionalChecks(MyData data, out string response)
    {
        response = "";
        return true;
    }
}
#endif
