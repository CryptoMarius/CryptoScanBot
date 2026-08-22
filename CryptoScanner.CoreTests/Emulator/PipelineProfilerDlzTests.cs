using CryptoScanner.Core.Core;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The DLZ carve-out in <see cref="PipelineProfiler"/>, added on 2026-08-22 because the emulator
/// runs DLZ inline in SignalPrepare and its cost therefore vanished into the "indicators" bucket -
/// a bucket that was routinely 99% of the pipeline, so DLZ and the indicator hub could not be told
/// apart.
/// <para>
/// The counters themselves are trivial; what these tests actually guard is the wiring around them.
/// A counter that <see cref="PipelineProfiler.Reset"/> forgets keeps the totals of the PREVIOUS run,
/// and the emulator resets once per run (ReplayRunner) - so the second run of a queue would report
/// the first run's DLZ time on its very first chunk and nobody would notice, because a plausible
/// number is exactly what it would print. Reset was in fact incomplete when these counters were
/// added.
/// </para>
/// </summary>
// PipelineProfiler is static and global: Enabled switches accumulation on for the whole process and
// Reset clears every counter, not just the DLZ ones. Running alongside another class would therefore
// let this one wipe or inflate what that one measures. Same reason the other classes that touch
// global state carry this attribute.
[DoNotParallelize]
[TestClass]
public class PipelineProfilerDlzTests
{
    [TestCleanup]
    public void LeaveTheProfilerAsWeFoundIt()
    {
        PipelineProfiler.Reset();
        PipelineProfiler.Enabled = false;
    }

    /// <summary>Nothing accumulates while the profiler is off - the live scanner must not pay for it.</summary>
    [TestMethod]
    public void DisabledProfilerRecordsNothing()
    {
        PipelineProfiler.Reset();
        PipelineProfiler.Enabled = false;

        PipelineProfiler.RecordDlzInline(1000);
        PipelineProfiler.RecordDlzPhases(1, 2, 3, 4, incremental: true);

        Assert.AreEqual(0, PipelineProfiler.DlzInlineTicks);
        Assert.AreEqual(0, PipelineProfiler.DlzInlineCalls);
        Assert.AreEqual(0, PipelineProfiler.DlzJudgeTicks);
        Assert.AreEqual(0, PipelineProfiler.DlzIncrementalRuns);
    }

    /// <summary>The phases and the branch counters land where they belong.</summary>
    [TestMethod]
    public void EnabledProfilerSplitsThePhasesAndTheBranches()
    {
        PipelineProfiler.Reset();
        PipelineProfiler.Enabled = true;

        PipelineProfiler.RecordDlzInline(1000);
        PipelineProfiler.RecordDlzPhases(100, 500, 40, 300, incremental: true);
        PipelineProfiler.RecordDlzInline(2000);
        PipelineProfiler.RecordDlzPhases(200, 900, 60, 700, incremental: false);

        Assert.AreEqual(3000, PipelineProfiler.DlzInlineTicks);
        Assert.AreEqual(2, PipelineProfiler.DlzInlineCalls);
        Assert.AreEqual(300, PipelineProfiler.DlzFeedTicks);
        Assert.AreEqual(1400, PipelineProfiler.DlzJudgeTicks);
        Assert.AreEqual(100, PipelineProfiler.DlzMergeTicks);
        Assert.AreEqual(1000, PipelineProfiler.DlzBrokenTicks);
        Assert.AreEqual(1, PipelineProfiler.DlzIncrementalRuns);
        Assert.AreEqual(1, PipelineProfiler.DlzFullRuns);

        // The four phases are a split OF the inline total, so they cannot exceed it. They may be
        // less: the bookkeeping between the phases belongs to no phase in particular.
        long phases = PipelineProfiler.DlzFeedTicks + PipelineProfiler.DlzJudgeTicks
                    + PipelineProfiler.DlzMergeTicks + PipelineProfiler.DlzBrokenTicks;
        Assert.IsTrue(phases <= PipelineProfiler.DlzInlineTicks,
            $"the phases ({phases}) add up to more than the whole ({PipelineProfiler.DlzInlineTicks})");
    }

    /// <summary>
    /// Reset has to clear every one of them. This is the test that matters: the emulator resets once
    /// per run, so a counter left behind here reports the previous run's numbers.
    /// </summary>
    [TestMethod]
    public void ResetClearsEveryDlzCounter()
    {
        PipelineProfiler.Enabled = true;
        PipelineProfiler.RecordDlzInline(1000);
        PipelineProfiler.RecordDlzPhases(1, 2, 3, 4, incremental: true);
        PipelineProfiler.RecordDlzPhases(1, 2, 3, 4, incremental: false);

        PipelineProfiler.Reset();

        Assert.AreEqual(0, PipelineProfiler.DlzInlineTicks, "DlzInlineTicks");
        Assert.AreEqual(0, PipelineProfiler.DlzInlineCalls, "DlzInlineCalls");
        Assert.AreEqual(0, PipelineProfiler.DlzFeedTicks, "DlzFeedTicks");
        Assert.AreEqual(0, PipelineProfiler.DlzJudgeTicks, "DlzJudgeTicks");
        Assert.AreEqual(0, PipelineProfiler.DlzMergeTicks, "DlzMergeTicks");
        Assert.AreEqual(0, PipelineProfiler.DlzBrokenTicks, "DlzBrokenTicks");
        Assert.AreEqual(0, PipelineProfiler.DlzFullRuns, "DlzFullRuns");
        Assert.AreEqual(0, PipelineProfiler.DlzIncrementalRuns, "DlzIncrementalRuns");
    }
}
