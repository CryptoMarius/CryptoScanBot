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

    // How many candles OF THE SIGNAL'S OWN INTERVAL to watch a signal before acting on it, counted
    // from the signal candle's open. Zero switches the whole rule off, which is the default.
    //
    // Every signal is followed by a small pullback - the coin dips and the oscillators briefly turn
    // the other way - and the trader steps in during that dip without knowing whether the coin is
    // going to turn or simply carry on. Measured on run 401 (2319 positions): the entries that
    // stayed clean and the ones that ended up walking the whole DCA ladder move IDENTICALLY over
    // the first five minutes (median -0.219% against, versus -0.223%). They only start to differ
    // further out, which is why this waits instead of testing an indicator at signal time.
    //
    // This was EntryWaitMinutes until 29-08-2026, and minutes turned out to measure four different
    // things at once. A signal is only re-examined when a candle of its own interval closes, so a
    // wait in minutes is rounded UP to the next candle: on runs 492/493 a setting of 5 and one of
    // 15 produced an identical 15, 30 and 60 minute delay on the 15m, 30m and 1h signals, and
    // differed only on 5m. One number, four meanings, and the two runs were not comparable on
    // anything except their 5m entries. In candles the delay is the same everywhere.
    //
    // Keep this BELOW EntryRemoveTime, or GiveUp removes the signal before the wait is over and
    // nothing is ever entered - both are now counted in candles of the same interval, so that is a
    // straight comparison. Waiting longer also means keeping signals in SignalList longer, and that
    // list is walked for every symbol on every candle.
    public int EntryWaitCandles { get; set; } = 0;

    // How far price may run AGAINST the signal during that wait, as a positive percentage of the
    // signal price. Zero means no limit: the wait then only delays the entry, it does not skip any.
    //
    // Above this the signal is dropped. Measured on run 401 with a 15 minute wait, the result went
    // from +98.06 to +147.43 for longs at 3% and from +413.83 to +463.60 for shorts at 2.5%, at the
    // cost of roughly one entry in ten. The optimum is a broad valley rather than a sharp point and
    // it shifts with the wait, so it is worth re-measuring whenever the wait changes.
    public decimal EntryMaxAdversePercentage { get; set; } = 0m;

    // EntryWaitForPatterns and EntryPatternShape sat here until 02-09-2026: a list of reversal
    // shapes turned the wait above into a SEARCH WINDOW - the entry was taken on the first candle
    // within it that formed one of those shapes. Measured on 37 runs (532-568, 616-618, 690-705)
    // and removed because it lost money on every strategy that makes any: dbr +682.25 -> +327.85,
    // failedbreakout +551.68 -> +193.35, and all five zone strategies worse. The cause was measured
    // too: the entry price goes from 0.38% against the signal to 0.69% against it, so the wait buys
    // away the very moment the strategy fired on. Only vbs improved (-123.53 -> +65.48), and it
    // halved the number of trades doing so.
    //
    // The shapes themselves live on as the CandlePattern STRATEGY, where they decide whether there
    // is a signal at all instead of delaying one (run 706: +449.73 over 2609 trades).

    public int StochExtremeLookback { get; set; } = 20;
    public int StochMinExtremeBars { get; set; } = 0;
    public decimal StochMinExtremeArea { get; set; } = 0m;
    public decimal StochMinExtremeZScore { get; set; } = 0m;

    // The multi-timeframe band-break confirmation used to live here as TimeframeConsensusCount, but
    // only the three band strategies could act on it while every strategy showed the field. It now
    // sits in the settings of those strategies as BandBreakConfirmationCount.
}
