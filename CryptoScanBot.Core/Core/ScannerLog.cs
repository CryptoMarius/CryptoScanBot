using NLog;

namespace CryptoScanBot.Core.Core;

public class ScannerLog
{
    // The nlogger stuff
    public static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    static private NLog.Targets.FileTarget CreateTarget(string name, string extra)
    {
        string logName = GlobalData.LogName == "" ? GlobalData.AppName : GlobalData.LogName;
        string filename = GlobalData.GetBaseDir() + @"\Log\" + logName;

        return new NLog.Targets.FileTarget
        {
            Name = name,
            KeepFileOpen = true,
            MaxArchiveDays = 7,
            FileName = filename + extra + ".log",
            ArchiveDateFormat = "yyyy-MM-dd",
            ArchiveFileName = filename + " {#}" + extra + ".log",
            ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
            ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date
        };
        //NLog.Targets.FileTarget fileTarget = 
        //return fileTarget;


        //// Create targets and add them to the configuration 
        //var fileTarget = new NLog.Targets.FileTarget();
        //config.AddTarget("file", fileTarget);
        //fileTarget.Name = "default";
        //fileTarget.KeepFileOpen = true;
        //fileTarget.MaxArchiveDays = 10;
        //fileTarget.FileName = logPrefix + ".log";
        //fileTarget.ArchiveDateFormat = "yyyy-MM-dd";
        //fileTarget.ArchiveFileName = logPrefix + "{#}.log";
        //fileTarget.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day;
        //fileTarget.ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date;
        //var rule = new NLog.Config.LoggingRule("*", LogLevel.Info, fileTarget);
        //config.LoggingRules.Add(rule);
    }

    public static void InitializeLogging()
    {
        // nlog is lastig te beinvloeden, daarom maar via code
        // serilog is niet veel anders, prima logging, maar beinvloeding van bestandsnamen is gelimiteerd (splitsen errors is een probleem)

        /*
        <targets>
            <target name="default" xsi:type="File" 
                fileName="${specialfolder:folder=ApplicationData}/CryptoScanner/CryptoScanner.log" 
                archiveFileName="${specialfolder:folder=ApplicationData}/CryptoScanner/CryptoScanner.{#}.log" 
                archiveEvery="Day" archiveNumbering="Rolling" 
                maxArchiveFiles="7" />
            <target name="errors" xsi:type="File" 
                fileName="${specialfolder:folder=ApplicationData}/CryptoScanner/CryptoScanner-errors.log" 
                archiveFileName="${specialfolder:folder=ApplicationData}/CryptoScanner/CryptoScanner-errors.{#}.log" 
                archiveEvery="Day" 
                archiveNumbering="Rolling" 
                maxArchiveFiles="7" />
        </targets>

		<logger name="*" writeTo="default" />
		<logger name="*" minlevel="Error" writeTo="errors" />


        <?xml version="1.0" encoding="utf-8" ?>
        <nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
                 xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" >
          <extensions>
            <add assembly="My.Awesome.LoggingExentions"/>
          </extensions>
            <targets>
                <target name="file1" xsi:type="File"
                          fileName="${basedir}/Logs/${date:format=yyyy-MM-dd}.log"
                          layout="${longdate} 
                          ${level:uppercase=true:padding=5} 
                          ${session} 
        ${storeid} ${msisdn} - ${logger:shortName=true} - ${message} 
        ${exception:format=tostring}"
                          keepFileOpen="true"
                        />
            </targets>
          <rules>
              <logger name="*" minlevel="Trace" writeTo="file1" />
          </rules>
        </nlog>
        */


        // ik ben het wel even zat met nlog en die filenames

        //string logPrefix = GlobalData.GetBaseDir() + @"\Log\" + GlobalData.AppName + " ";

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

        //Logger.Info("");
        //Logger.Info("");
        //Logger.Info("****************************************************");
    }
}
