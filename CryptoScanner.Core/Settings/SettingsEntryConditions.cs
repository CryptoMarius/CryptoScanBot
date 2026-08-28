namespace CryptoScanner.Core.Settings;

[Serializable]
public class SettingsEntryConditions
{
    public bool CheckIncreasingRsi { get; set; } = false;
    public bool CheckIncreasingMacd { get; set; } = false;
    public bool CheckIncreasingStoch { get; set; } = false;
    public bool CheckFurtherPriceMove { get; set; } = false;

    public bool CheckTrendPrimaryDirection { get; set; } = false;
    public int TrendPrimaryDirectionCount { get; set; } = 2;
    public bool CheckTrendSecondaryDirection { get; set; } = false;
    public int TrendSecondaryDirectionCount { get; set; } = 2;

    public bool CheckPriceAboveMa200 { get; set; } = false;
    public decimal Ma200MinDistancePercentage { get; set; } = 0m;
    public int Ma200ConfirmationCandles { get; set; } = 0;

    public bool WaitForStochRecovery { get; set; } = false;
    public bool WaitForRsiRecovery { get; set; } = false;

    // Minutes to watch a signal before acting on it, counted from the signal candle's open. Zero
    // switches the whole rule off, which is the default.
    //
    // Every signal is followed by a small pullback - the coin dips and the oscillators briefly turn
    // the other way - and the trader steps in during that dip without knowing whether the coin is
    // going to turn or simply carry on. Measured on run 401 (2319 positions): the entries that
    // stayed clean and the ones that ended up walking the whole DCA ladder move IDENTICALLY over
    // the first five minutes (median -0.219% against, versus -0.223%). They only start to differ
    // further out, which is why this waits instead of testing an indicator at signal time.
    //
    // Keep this BELOW EntryRemoveTime * interval duration, or GiveUp removes the signal before the
    // wait is over and nothing is ever entered. With EntryRemoveTime = 5 a 5m signal lives 25
    // minutes, so 15 costs nothing extra - the signal was being kept alive anyway. Waiting longer
    // means keeping signals in SignalList longer, and that list is walked for every symbol on every
    // candle.
    public int EntryWaitMinutes { get; set; } = 0;

    // How far price may run AGAINST the signal during that wait, as a positive percentage of the
    // signal price. Zero means no limit: the wait then only delays the entry, it does not skip any.
    //
    // Above this the signal is dropped. Measured on run 401 with a 15 minute wait, the result went
    // from +98.06 to +147.43 for longs at 3% and from +413.83 to +463.60 for shorts at 2.5%, at the
    // cost of roughly one entry in ten. The optimum is a broad valley rather than a sharp point and
    // it shifts with the wait, so it is worth re-measuring whenever the wait changes.
    public decimal EntryMaxAdversePercentage { get; set; } = 0m;

    public int StochExtremeLookback { get; set; } = 20;
    public int StochMinExtremeBars { get; set; } = 0;
    public decimal StochMinExtremeArea { get; set; } = 0m;
    public decimal StochMinExtremeZScore { get; set; } = 0m;

    // The multi-timeframe band-break confirmation used to live here as TimeframeConsensusCount, but
    // only the three band strategies could act on it while every strategy showed the field. It now
    // sits in the settings of those strategies as BandBreakConfirmationCount.
}
