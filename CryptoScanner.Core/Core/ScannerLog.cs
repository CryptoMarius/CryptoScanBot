using CryptoScanner.Core.Const;

using NLog;

namespace CryptoScanner.Core.Core;

public class ScannerLog
{
    // The global logger class
    public static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    static private NLog.Targets.FileTarget CreateTarget(string name, string extra)
    {
        string logName = GlobalData.LogName == "" ? Constants.AppName : GlobalData.LogName;
        string filename = Path.Combine(GlobalData.AppDataFolder, "Log", logName);

        return new NLog.Targets.FileTarget
        {
            Name = name,
            KeepFileOpen = true,
            MaxArchiveDays = 7,
            FileName = filename + extra + ".log",
            ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
            ArchiveFileName = filename + " {#}" + extra + ".log",
            ArchiveSuffixFormat = @"_{1:yyyyMMdd}",
        };

    }

    // The dynamically attached per-run target/rule (emulator). Held so StopRunLog can detach the
    // exact same instances it added; null when no run log is active.
    private static NLog.Targets.FileTarget? runFileTarget;
    private static NLog.Config.LoggingRule? runLoggingRule;

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

        runFileTarget = new NLog.Targets.FileTarget
        {
            Name = $"run-{runId}",
            KeepFileOpen = true,
            // Run ids auto-increment and are never reused, but after a DB reset they restart at 1;
            // start the file fresh so a stale file from an earlier reset can never be appended to.
            DeleteOldFileOnStartup = true,
            FileName = filename,
        };

        var config = LogManager.Configuration;
        config.AddTarget(runFileTarget);
        runLoggingRule = new NLog.Config.LoggingRule("*", LogLevel.Info, runFileTarget);
        config.LoggingRules.Add(runLoggingRule);
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
        LogManager.Configuration = config;

        runFileTarget = null;
        runLoggingRule = null;
    }

    public static void InitializeLogging()
    {
        // Create configuration object
        var config = new NLog.Config.LoggingConfiguration();

        var fileTarget = CreateTarget("default", "");
        var rule = new NLog.Config.LoggingRule("*", LogLevel.Info, fileTarget);
        config.LoggingRules.Add(rule);

        fileTarget = CreateTarget("errors", " Error");
        rule = new NLog.Config.LoggingRule("*", LogLevel.Error, fileTarget);
        config.LoggingRules.Add(rule);

#if DEBUG
        fileTarget = CreateTarget("trace", " Trace");
        rule = new NLog.Config.LoggingRule("*", LogLevel.Trace, fileTarget);
        config.LoggingRules.Add(rule);

        //fileTarget = CreateTarget("debug", " Debug");
        //rule = new NLog.Config.LoggingRule("*", LogLevel.Debug, fileTarget);
        //config.LoggingRules.Add(rule);
#endif

        LogManager.Configuration = config;
    }
}
