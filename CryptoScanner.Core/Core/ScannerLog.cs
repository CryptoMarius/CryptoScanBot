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
