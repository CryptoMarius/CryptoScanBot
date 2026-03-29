using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Signal.Experiment;

public class SignalBbmaBase : SignalCreateBase
{
    internal enum BbmaTfState
    {
        None,
        Extreme,
        MagicExtreme,
        Mlv,
        Reentry
    }

    internal static string TfStateCode(BbmaTfState state) => state switch
    {
        BbmaTfState.MagicExtreme => "EE",
        BbmaTfState.Extreme => "E",
        BbmaTfState.Mlv => "M",
        BbmaTfState.Reentry => "R",
        _ => "-"
    };


    /// <summary>
    /// Returns the fixed BBMA higher timeframe pair for the signal interval.
    /// These pairs are fixed (not consecutive steps) and define the 3-TF BBMA system.
    /// </summary>
    internal bool GetIntervals(out CryptoIntervalPeriod interval2, out CryptoIntervalPeriod interval3)
    {
        // For BBMA codes
        switch (Interval.IntervalPeriod)
        {
            case CryptoIntervalPeriod.interval1m:
                interval2 = CryptoIntervalPeriod.interval5m;
                interval3 = CryptoIntervalPeriod.interval15m;
                break;
            case CryptoIntervalPeriod.interval2m:
                interval2 = CryptoIntervalPeriod.interval10m;
                interval3 = CryptoIntervalPeriod.interval30m;
                break;
            case CryptoIntervalPeriod.interval3m:
                interval2 = CryptoIntervalPeriod.interval15m;
                interval3 = CryptoIntervalPeriod.interval1h;
                break;
            case CryptoIntervalPeriod.interval5m:
                interval2 = CryptoIntervalPeriod.interval15m;
                interval3 = CryptoIntervalPeriod.interval1h;
                break;
            case CryptoIntervalPeriod.interval10m:
                interval2 = CryptoIntervalPeriod.interval30m;
                interval3 = CryptoIntervalPeriod.interval2h;
                break;
            case CryptoIntervalPeriod.interval15m:
                interval2 = CryptoIntervalPeriod.interval1h;
                interval3 = CryptoIntervalPeriod.interval4h;
                break;
            case CryptoIntervalPeriod.interval30m:
                interval2 = CryptoIntervalPeriod.interval2h;
                interval3 = CryptoIntervalPeriod.interval8h;
                break;
            case CryptoIntervalPeriod.interval1h:
                interval2 = CryptoIntervalPeriod.interval4h;
                interval3 = CryptoIntervalPeriod.interval1d;
                break;
            case CryptoIntervalPeriod.interval2h:
                interval2 = CryptoIntervalPeriod.interval6h;
                interval3 = CryptoIntervalPeriod.interval1d;
                break;
            case CryptoIntervalPeriod.interval3h:
                interval2 = CryptoIntervalPeriod.interval8h;
                interval3 = CryptoIntervalPeriod.interval1d;
                break;
            case CryptoIntervalPeriod.interval4h:
                interval2 = CryptoIntervalPeriod.interval1d;
                interval3 = CryptoIntervalPeriod.interval1w;
                break;
            case CryptoIntervalPeriod.interval6h:
                interval2 = CryptoIntervalPeriod.interval1d;
                interval3 = CryptoIntervalPeriod.interval1w;
                break;
            case CryptoIntervalPeriod.interval8h:
                interval2 = CryptoIntervalPeriod.interval1d;
                interval3 = CryptoIntervalPeriod.interval1w;
                break;
            case CryptoIntervalPeriod.interval12h:
                interval2 = CryptoIntervalPeriod.interval1d;
                interval3 = CryptoIntervalPeriod.interval1w;
                break;
            default:
                ExtraText = $"not accepted interval {Interval.Name}";
                //GlobalData.AddTextToLogTab($"{Symbol.Name} {Interval.IntervalPeriod} {CryptoTradeSide.Long} failed PrepareHigherInterval (1)");
                interval2 = Interval.IntervalPeriod;
                interval3 = Interval.IntervalPeriod;
                return false;
        }
        return true;
    }
}
