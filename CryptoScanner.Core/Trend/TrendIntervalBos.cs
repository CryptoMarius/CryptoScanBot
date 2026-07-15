using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Text;

namespace CryptoScanner.Core.Trend;


/// <summary>
/// Trend interpretation using BOS (Break of Structure) and CHoCH (Change of Character).
///
/// Unlike Dow Theory which requires two consecutive confirming swing points (e.g. HH + HL),
/// a single structural break is sufficient here:
///   - In a bearish trend: a Higher High → CHoCH → switches trend to Bullish
///   - In a bullish trend: a Lower Low  → CHoCH → switches trend to Bearish
///   - Same direction:     Higher High in uptrend / Lower Low in downtrend → BOS (continuation)
///
/// This makes BOS/CHoCH faster than Dow Theory, at the cost of potentially more reversals.
///
/// The window/indicator setup used to live here in CalculateAsync; that orchestration
/// is now shared with the Dow interpretation through <see cref="TrendCalculator.CalculateBothAsync"/>.
/// </summary>
public class TrendIntervalBos
{
    /// <summary>
    /// Interpret zigzag swing points using BOS/CHoCH logic.
    /// Returns the resulting trend (Bullish/Bearish/Unknown) and reports the most
    /// recent structural event (its swing-point candle and price) via out parameters.
    /// Callers use that event info so downstream signals can report the actual
    /// break-candle price instead of the close of the latest candle.
    /// </summary>
    public static CryptoTrendIndicator InterpretZigZagPoints(ZigZagIndicator indicator, StringBuilder? log,
        out List<StructureEvent> structureEvents)
    {
        // Skip dummy pivots — these are provisional points added by TryAddDummyPoints when
        // price has ALREADY broken the last swing high/low but the underlying pivot candle
        // is not yet confirmed by the Lance Beggs buffer (it can still shift to a later
        // candle as price continues to extend). Treating them as real pivots makes BOS/CHoCH
        // fire prematurely AND repeatedly with shifting timestamps, which defeats the
        // LastFiredStructureEventTimes guard in SignalChoch.
        // TrendInterval (Dow) gets away with not filtering dummies because its 'count > 1'
        // damping requires two consecutive contra-trend pivots before flipping — BOS lacks
        // that buffer and flips on a single break, so dummies must be excluded here.
        var zigZagList = indicator.ZigZagList.Where(z => !z.Dummy).ToList();
        CryptoTrendIndicator trend = CryptoTrendIndicator.Unknown;
        structureEvents = [];

        if (log != null)
        {
            log.AppendLine("");
            log.AppendLine($"BOS/CHoCH ZigZag points={zigZagList.Count} (dummies excluded):");
        }

        if (zigZagList.Count < 2)
        {
            log?.AppendLine($"Not enough zigzag points, trend={trend}");
            return trend;
        }


        // Two parallel tracking pairs:
        //   protectedHigh / protectedLow  — the STRUCTURAL level: only updated on an actual
        //                                   break (HH > protectedHigh or LL < protectedLow).
        //                                   This is what BOS/CHoCH compares against.
        //   recentHigh / recentLow        — the MOST RECENT pivot of each type: updated on
        //                                   every pivot. Used to reset the protected level
        //                                   of the OPPOSITE type when a CHoCH flips the trend.
        //
        // The old implementation updated lastHigh/lastLow on every pivot, which made the
        // protected level drift down with every LH inside a downtrend (and up with every HL
        // inside an uptrend), eventually triggering a false CHoCH on a minor reactionary move
        // even though price never actually broke the structural high/low.
        decimal protectedHigh;
        decimal protectedLow;
        decimal recentHigh;
        decimal recentLow;
        if (zigZagList[1].Value > zigZagList[0].Value)
        {
            recentLow = protectedLow = zigZagList[0].Value;
            recentHigh = protectedHigh = zigZagList[1].Value;
            trend = CryptoTrendIndicator.Bullish;
        }
        else
        {
            recentLow = protectedLow = zigZagList[1].Value;
            recentHigh = protectedHigh = zigZagList[0].Value;
            trend = CryptoTrendIndicator.Bearish;
        }

        for (int i = 2; i < zigZagList.Count; i++)
        {
            var zigZag = zigZagList[i];
            CryptoStructureEvent structureEvent = CryptoStructureEvent.None;

            if (zigZag.PointType == 'H')
            {
                if (zigZag.Value > protectedHigh)
                {
                    if (trend == CryptoTrendIndicator.Bearish)
                    {
                        // Higher High beyond the protected high in a downtrend
                        //   = Change of Character → reversal to Bullish.
                        // Reset the opposite protected level to the most recent low (= the
                        // bottom of the just-ended downtrend leg) so a future CHoCH-back to
                        // bearish can fire when price breaks THAT low, not the prehistoric one.
                        structureEvent = CryptoStructureEvent.ChoCh;
                        trend = CryptoTrendIndicator.Bullish;
                        protectedLow = recentLow;
                    }
                    else
                    {
                        // Higher High in an uptrend = Break of Structure (continuation)
                        structureEvent = CryptoStructureEvent.Bos;
                    }
                    protectedHigh = zigZag.Value;
                }
                // else: this is a Lower High inside the current trend — no event, and the
                // protected high stays put. Only the recent tracker moves.
                recentHigh = zigZag.Value;
            }
            else // 'L'
            {
                if (zigZag.Value < protectedLow)
                {
                    if (trend == CryptoTrendIndicator.Bullish)
                    {
                        // Lower Low beyond the protected low in an uptrend → CHoCH to Bearish.
                        // Reset opposite protected level to the most recent high.
                        structureEvent = CryptoStructureEvent.ChoCh;
                        trend = CryptoTrendIndicator.Bearish;
                        protectedHigh = recentHigh;
                    }
                    else
                    {
                        // Lower Low in a downtrend = Break of Structure (continuation)
                        structureEvent = CryptoStructureEvent.Bos;
                    }
                    protectedLow = zigZag.Value;
                }
                // else: Higher Low inside the current trend — no event, protected low stays.
                recentLow = zigZag.Value;
            }

            if (structureEvent != CryptoStructureEvent.None)
            {
                structureEvents.Add(new StructureEvent(zigZag.Candle!.OpenTime, structureEvent, zigZag.Value, trend));
            }

            if (log != null)
            {
                if (structureEvent != CryptoStructureEvent.None)
                    log.AppendLine($"date={zigZag.Candle!.Date.ToLocalTime()} {zigZag.PointType} {zigZag.Value:N8} {structureEvent}, trend={trend}");
                else
                    log.AppendLine($"date={zigZag.Candle!.Date.ToLocalTime()} {zigZag.PointType} {zigZag.Value:N8} trend={trend}");
            }
        }

        log?.AppendLine("");
        return trend;
    }
}
