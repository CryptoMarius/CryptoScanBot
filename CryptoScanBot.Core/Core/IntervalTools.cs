using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Core;

public class IntervalTools
{
    public static long StartOfIntervalCandle(long sourceStart, int sourceDuration)
    {
        long diff = sourceStart % sourceDuration;
        long targetStart = sourceStart - diff;
        return targetStart;
    }


    // TODO: Delete method, replace with 3
    public static long StartOfIntervalCandle2(long sourceStart, int sourceDuration, int targetDuration)
    {
        // SourceDate should be the candle.OpenTime and sourceDuration the duration of the candle.
        // It is the same result as the StartOfIntervalCandle() but corrected if the higher candle can't be calculated

        if (targetDuration == sourceDuration)
            return sourceStart;

        // This works for lower time frame to higher timeframe and wont work the other way
        if (targetDuration < sourceDuration)
            throw new Exception("Target interval should be higher than source interval");

        long diff = sourceStart % targetDuration;
        long targetStart = sourceStart - diff;

        // The target candle cannot be final/complete if is above the end of the start candle
        // (it would be a next candle or an in progress candle)
        long sourceDateEnd = sourceStart + sourceDuration;
        long targetDateEnd = targetStart + targetDuration;
        if (targetDateEnd > sourceDateEnd)
            targetStart -= targetDuration;

#if DEBUG
        DateTime sourceStartDate = CandleTools.GetUnixDate(sourceStart);
        DateTime sourceEndDate = CandleTools.GetUnixDate(sourceStart + sourceDuration);

        DateTime targetStartDate = CandleTools.GetUnixDate(targetStart);
        DateTime targetEndDate = CandleTools.GetUnixDate(targetStart + targetDuration);
#endif
        return targetStart;
    }


    public static (bool targetComplete, long targetStart) StartOfIntervalCandle3(long sourceStart, int sourceDuration, int targetDuration)
    {
        // SourceDate should be the candle.OpenTime and sourceDuration the duration of the candle.
        // It is the same result as the StartOfIntervalCandle() but corrected if the higher candle can't be calculated
        // Same as the 2 but with extended results to avoid unneccesary calculations

        if (targetDuration == sourceDuration)
            return (false, sourceStart);

        // This works for lower time frame to higher timeframe and wont work the other way
        if (targetDuration < sourceDuration)
            throw new Exception("Target interval should be higher than source interval");

        // Calculate source (just in case)
        sourceStart -= sourceStart % sourceDuration;
        
        // Calculate target
        long targetStart = sourceStart - sourceStart % targetDuration;


        // Test if the target candle is final/complete (not a in progress candle)
        long sourceDateEnd = sourceStart + sourceDuration;
        long targetDateEnd = targetStart + targetDuration;
        bool targetComplete = targetDateEnd == sourceDateEnd;

        return (targetComplete, targetStart);
    }

}
