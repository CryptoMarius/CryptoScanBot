using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.SuperTrendBreakout.Signal;

public class SignalSuperTrendBreakoutLong : SignalSuperTrendBreakoutBase
{
    public override bool IsSignal()
    {
        ExtraText = "";

        // SuperTrend must have just flipped from bearish to bullish
        if (!IsSuperTrendFlip(bullish: true))
        {
            ExtraText = "No bullish SuperTrend flip";
            return false;
        }

        // Price must be near (or recently was near) a DLZ support zone
        if (!WasNearDlzZone(CryptoTradeSide.Long, out string zoneInfo))
        {
            ExtraText = "No DLZ zone nearby";
            return false;
        }

        ExtraText = $"SuperTrend bullish flip near {zoneInfo}";
        return true;
    }


    public override bool AdditionalChecks(MyData data, out string response)
    {
        response = "";
        return true;
    }
}
