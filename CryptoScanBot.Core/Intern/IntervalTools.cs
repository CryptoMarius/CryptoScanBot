using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Intern;

public class IntervalTools
{
    public static long StartOfIntervalCandle(CryptoInterval interval, long sourceDate) //, bool fixFinalCandle
    {
        long diff = sourceDate % interval!.Duration;
        long lastCandleIntervalOpenTime = sourceDate - diff;
        // The candle cannot be final if it has a remainder, go 1 back
        // This is true for candles in progress, NOT for historic candles.
        // (for example the emulator), let the user decide what todo
        // (09:14 -> 09:05 because candle 09:10..09:14:59 cannot be finished)
        //if (fixFinalCandle && diff != 0)
        //    lastCandleIntervalOpenTime -= interval.Duration;
        return lastCandleIntervalOpenTime;
    }


    public static long StartOfIntervalCandle2(long sourceDateStart, int sourceDuration, int targetDuration)
    {
        // sourceDate should be the candle.OpenTime and sourceDuration the duration of the candle.
        // This works for lower time frame to higher timeframe and wont work the other way

        if (targetDuration == sourceDuration)
            return sourceDateStart;

        long sourceDateEnd = sourceDateStart + sourceDuration;
        long diff = sourceDateEnd % targetDuration;
        long targetDateStart = sourceDateEnd - diff;

        // The target candle cannot be final if is above the end of the start candle (it would be an in progress candle)
        long targetDateEnd = targetDateStart + targetDuration;
        if (targetDateEnd >= sourceDateEnd)
            targetDateStart -= targetDuration;

        return targetDateStart;

    }
}
