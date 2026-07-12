using CryptoScanner.Core.Const;

using NLog;

namespace CryptoScanner.Core.Core;

public class ScannerLog
{
    // The global logger class
    public static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    // Matches NLog's FileTarget default layout ("${longdate}|${level:uppercase=true}|${logger}|${message}")
    // plus the simulated-time field (${simtime}, registered in InitializeLogging) inserted right after ${longdate}.
    // ${onexception} appends the full exception (incl. stacktrace) only on calls like Logger.Error(ex, "...") -
    // without it, every Logger.Error(exception, ...) call in the codebase silently drops the stacktrace.
    private const string LogLayout = "${longdate}|sim=${simtime}|${level:uppercase=true}|${logger}|${message}${onexception:${newline}${exception:format=ToString}}";

    static private NLog.Targets.Target CreateTarget(string name, string extra)
    {
        string logName = GlobalData.LogName == "" ? Constants.AppName : GlobalData.LogName;
        string filename = Path.Combine(GlobalData.AppDataFolder, "Log", logName);

        // Inner synchronous file target (the actual writer).
        var fileTarget = new NLog.Targets.FileTarget
        {
            Name = name + "_file",
            KeepFileOpen = true,
            MaxArchiveDays = 7,
            FileName = filename + extra + ".log",
            ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
            ArchiveFileName = filename + " {#}" + extra + ".log",
            ArchiveSuffixFormat = @"_{1:yyyyMMdd}",
            Layout = LogLayout,
        };

        // Wrap in async so Logger.Info() on the run thread never blocks on disk I/O.
        // Queue size 50000 is far above the typical run's log volume; Block prevents line loss.
        return new NLog.Targets.Wrappers.AsyncTargetWrapper(fileTarget, 50000, NLog.Targets.Wrappers.AsyncTargetWrapperOverflowAction.Block)
        {
            Name = name,
        };
    }

    // The shared trace target/rule created by InitializeLogging (DEBUG builds only).
    // StartRunLog detaches it so the global Trace.log does not grow during emulator runs
    // (per-run trace files capture the same data); StopRunLog reattaches it.
    private static NLog.Targets.Target? sharedTraceTarget = null;
    private static NLog.Config.LoggingRule? sharedTraceRule = null;

    // The dynamically attached per-run target/rule (emulator). Held so StopRunLog can detach the
    // exact same instances it added; null when no run log is active.
    // Stored as the outer AsyncTargetWrapper so config.RemoveTarget uses the right name.
    private static NLog.Targets.Target? runFileTarget;
    private static NLog.Config.LoggingRule? runLoggingRule;

    // The per-run trace target/rule. The shared "* at Trace" trace target (see InitializeLogging)
    // grows without bound across every run; this splits the Trace-level detail per run into its own
    // "<base> Run <id> Trace.log" so a single backtest stays readable. Unlike the shared trace target
    // it is NOT gated on DEBUG, so it also works when the emulator runs in Release.
    private static NLog.Targets.Target? runTraceTarget;
    private static NLog.Config.LoggingRule? runTraceRule;

    /// <summary>
    /// Attaches a dedicated log file for a single emulator run, named after its run id
    /// (e.g. "&lt;base&gt; Run 42.log") in the same Log folder as the default/error/trace targets.
    /// While active every Info-or-above line is ALSO written to this file, on top of the regular
    /// targets, so each run keeps its own isolated, reproducible log. Reapplying the configuration
    /// is how NLog picks up the new target/rule at runtime. Pair with <see cref="StopRunLog"/>.
    /// </summary>
    public static void StartRunLog(int runId)
    {
        if (LogManager.Configuration == null)
            return;

        // Defensive: if a previous run log somehow stayed attached, detach it before adding a new one.
        StopRunLog();

        string logName = GlobalData.LogName == "" ? Constants.AppName : GlobalData.LogName;
        string filename = Path.Combine(GlobalData.AppDataFolder, "Log", $"{logName} Run {runId}.log");

        var innerTarget = new NLog.Targets.FileTarget
        {
            Name = $"run-{runId}_file",
            KeepFileOpen = true,
            // Per-run log files are kept, never deleted: run ids auto-increment and are unique, so each
            // run gets its own file. (After a DB reset the ids restart at 1; NLog then appends to the
            // existing "Run 1.log" rather than deleting it, so earlier content is preserved.)
            FileName = filename,
            Layout = LogLayout,
        };

        // Async wrapper so Logger.Info() on the TickRunner thread never blocks on disk I/O.
        runFileTarget = new NLog.Targets.Wrappers.AsyncTargetWrapper(innerTarget, 50000, NLog.Targets.Wrappers.AsyncTargetWrapperOverflowAction.Block)
        {
            Name = $"run-{runId}",
        };

        var config = LogManager.Configuration;
        config.AddTarget(runFileTarget);
        runLoggingRule = new NLog.Config.LoggingRule("*", LogLevel.Info, runFileTarget);
        config.LoggingRules.Add(runLoggingRule);

        // Split the (otherwise ever-growing) trace log per run as well: a dedicated Trace-level file
        // for this run only, named with the run id exactly like the Info file above. Every
        // Trace-or-above line is written here. Not gated on DEBUG, so it also works in Release.
        string traceFilename = Path.Combine(GlobalData.AppDataFolder, "Log", $"{logName} Run {runId} Trace.log");

        var innerTraceTarget = new NLog.Targets.FileTarget
        {
            Name = $"run-{runId}-trace_file",
            KeepFileOpen = true,
            FileName = traceFilename,
            Layout = LogLayout,
        };

        runTraceTarget = new NLog.Targets.Wrappers.AsyncTargetWrapper(innerTraceTarget, 50000, NLog.Targets.Wrappers.AsyncTargetWrapperOverflowAction.Block)
        {
            Name = $"run-{runId}-trace",
        };

        config.AddTarget(runTraceTarget);
        runTraceRule = new NLog.Config.LoggingRule("*", LogLevel.Trace, runTraceTarget);
        config.LoggingRules.Add(runTraceRule);

        // Detach the shared trace target so the global Trace.log does not grow during emulator runs.
        if (sharedTraceRule != null)
            config.LoggingRules.Remove(sharedTraceRule);
        if (sharedTraceTarget != null)
            config.RemoveTarget(sharedTraceTarget.Name);

        LogManager.Configuration = config;
    }

    /// <summary>
    /// Detaches the per-run log file attached by <see cref="StartRunLog"/> (flushing it as part of
    /// the configuration reapply) and forgets it. No-op when no run log is active.
    /// </summary>
    public static void StopRunLog()
    {
        if (LogManager.Configuration == null || runLoggingRule == null)
            return;

        var config = LogManager.Configuration;
        config.LoggingRules.Remove(runLoggingRule);
        if (runFileTarget != null)
            config.RemoveTarget(runFileTarget.Name);

        if (runTraceRule != null)
            config.LoggingRules.Remove(runTraceRule);
        if (runTraceTarget != null)
            config.RemoveTarget(runTraceTarget.Name);

        // Reattach the shared trace target so the scanner keeps writing to the global Trace.log.
        if (sharedTraceTarget != null && sharedTraceRule != null)
        {
            config.AddTarget(sharedTraceTarget);
            config.LoggingRules.Add(sharedTraceRule);
        }

        LogManager.Configuration = config;

        runFileTarget = null;
        runLoggingRule = null;
        runTraceTarget = null;
        runTraceRule = null;
    }

    public static void InitializeLogging()
    {
        // ${simtime} renders GlobalData.Clock's current time (real wall-clock for the live scanner,
        // the replay's simulated candle time for the emulator) instead of the actual system clock that
        // NLog's built-in ${longdate} always uses. Used alongside ${longdate} in LogLayout below (not
        // a replacement) so log lines keep showing both: how fast the run is really progressing AND
        // which point in the (possibly simulated) timeline a line refers to.
        LogManager.Setup().SetupExtensions(ext =>
            ext.RegisterLayoutRenderer("simtime", _ => GlobalData.Clock.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff")));

        // Create configuration object
        var config = new NLog.Config.LoggingConfiguration();

        var fileTarget = CreateTarget("default", "");
        var rule = new NLog.Config.LoggingRule("*", LogLevel.Info, fileTarget);
        config.LoggingRules.Add(rule);

        fileTarget = CreateTarget("errors", " Error");
        rule = new NLog.Config.LoggingRule("*", LogLevel.Error, fileTarget);
        config.LoggingRules.Add(rule);

#if DEBUG
        // Shared trace target for the live scanner. During emulator runs StartRunLog detaches
        // this (per-run trace files capture the same data); StopRunLog reattaches it.
        sharedTraceTarget = CreateTarget("trace", " Trace");
        sharedTraceRule = new NLog.Config.LoggingRule("*", LogLevel.Trace, sharedTraceTarget);
        config.LoggingRules.Add(sharedTraceRule);

        //fileTarget = CreateTarget("debug", " Debug");
        //rule = new NLog.Config.LoggingRule("*", LogLevel.Debug, fileTarget);
        //config.LoggingRules.Add(rule);
#endif

        LogManager.Configuration = config;
    }
}
