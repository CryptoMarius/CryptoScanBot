using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Core;

public class IntervalTools
{
    public static CandleTime StartOfIntervalCandle(CandleTime sourceStart, uint sourceDuration)
    {
        //if (sourceDuration % 60 != 0)
        //    throw new ArgumentException("Duration must be minute-aligned.");
        //uint durationMinutes = (uint)(sourceDuration);
        //uint remainder = sourceStart.Minutes % durationMinutes;
        //return new CandleTime(sourceStart.Minutes - remainder);
        //return sourceStart.AlignToInterval(durationMinutes);

        //CandleTime diff = sourceStart % sourceDuration;
        //CandleTime targetStart = sourceStart - diff;
        //return targetStart;

        //return sourceStart - sourceStart.Minutes % sourceDuration;
        return sourceStart.AlignToIntervalMinutes(sourceDuration);
    }


    // TODO: Delete method, replace with 3
    public static CandleTime StartOfIntervalCandle2(CandleTime sourceStart, uint sourceDuration, uint targetDuration)
    {
        // SourceDate should be the candle.OpenTime and sourceDuration the duration of the candle.
        // It is the same result as the StartOfIntervalCandle() but corrected if the higher candle can't be calculated
        if (targetDuration == sourceDuration)
            return sourceStart;

        // This works for lower time frame to higher timeframe and wont work the other way
        if (targetDuration < sourceDuration)
            throw new Exception("Target interval should be higher than source interval");

        CandleTime targetStart = sourceStart.AlignToIntervalMinutes(targetDuration);

        // The target candle cannot be final/complete if is above the end of the start candle
        // (it would be a next candle or an in progress candle)
        CandleTime sourceDateEnd = sourceStart + sourceDuration;
        CandleTime targetDateEnd = targetStart + targetDuration;
        if (targetDateEnd > sourceDateEnd)
            targetStart -= targetDuration;

//#if DEBUG
//        DateTime sourceStartDate = sourceStart.ToDateTime();
//        DateTime sourceEndDate = (sourceStart + sourceDuration).ToDateTime();

//        DateTime targetStartDate = targetStart.ToDateTime();
//        DateTime targetEndDate = (targetStart + targetDuration).ToDateTime();
//#endif
        return targetStart;
    }


    public static (bool targetComplete, CandleTime targetStart) StartOfIntervalCandle3(
        CandleTime sourceStart, uint sourceDuration, uint targetDuration)
    {
        // SourceDate should be the candle.OpenTime and sourceDuration the duration of the candle.
        // It is the same result as the StartOfIntervalCandle() but corrected if the higher candle can't be calculated
        // Same as the 2 but with extended results to avoid unneccesary calculations

        //if (targetDuration == sourceDuration)
        //    return (false, sourceStart);

        // This works for lower time frame to higher timeframe and wont work the other way
        if (targetDuration <= sourceDuration)
            throw new Exception("Target interval should be higher than source interval");

        // Calculate the start and end of source
        sourceStart = sourceStart.AlignToIntervalMinutes(sourceDuration);
        CandleTime sourceDateEnd = sourceStart + sourceDuration;

        // Calculate the start and end of target
        CandleTime targetStart = sourceStart.AlignToIntervalMinutes(targetDuration);
        CandleTime targetDateEnd = targetStart + targetDuration;

        // Test if the target candle is final/complete (but not an in progress candle)
        bool targetComplete = targetDateEnd == sourceDateEnd;
        return (targetComplete, targetStart);
    }

}
