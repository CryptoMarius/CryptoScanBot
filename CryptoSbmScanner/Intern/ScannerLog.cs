using NLog;

namespace CryptoSbmScanner.Intern;

public class ScannerLog
{
    // The nlogger stuff
    static public Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    static public void InitializeLogging()
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

        //// Create configuration object 
        var config = new NLog.Config.LoggingConfiguration();

        // Create targets and add them to the configuration 
        var fileTarget = new NLog.Targets.FileTarget();
        config.AddTarget("file", fileTarget);
        fileTarget.Name = "default";
        fileTarget.KeepFileOpen = true;
        fileTarget.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day; // None?
        fileTarget.FileName = GlobalData.GetBaseDir() + "CryptoScanner ${date:format=yyyy-MM-dd}.log";
        fileTarget.MaxArchiveDays = 14;
        //fileTarget.ArchiveDateFormat = "yyyy-MM-dd";
        //fileTarget.EnableArchiveFileCompression = false;
        //fileTarget.ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date;
        //fileTarget.MaxArchiveDays = 10;
        //fileTarget.ArchiveFileName = fileTarget.FileName; //"${logDirectory}/Log.{#}.log";
        //fileTarget.Layout = "Exception Type: ${exception:format=Type}${newline}Target Site:  ${event-context:TargetSite }${newline}Message: ${message}";
        var rule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, fileTarget);
        config.LoggingRules.Add(rule);


        fileTarget = new NLog.Targets.FileTarget();
        config.AddTarget("file", fileTarget);
        fileTarget.Name = "errors";
        fileTarget.KeepFileOpen = true;
        fileTarget.MaxArchiveDays = 14;
        fileTarget.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day; // None?
        fileTarget.FileName = GlobalData.GetBaseDir() + "CryptoScanner ${date:format=yyyy-MM-dd}-Errors.log";
        rule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Error, fileTarget);
        config.LoggingRules.Add(rule);


        //fileTarget = new NLog.Targets.FileTarget();
        //config.AddTarget("file", fileTarget);
        //fileTarget.Name = "trace";
        //fileTarget.KeepFileOpen = true;
        //fileTarget.MaxArchiveDays = 14;
        //fileTarget.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day; // None?
        //fileTarget.FileName = GetBaseDir() + "CryptoScanner ${date:format=yyyy-MM-dd}-Trace.log";
        //rule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, fileTarget);
        //config.LoggingRules.Add(rule);



        NLog.LogManager.Configuration = config;

        Logger.Info("");
        Logger.Info("");
        Logger.Info("****************************************************");
    }

}
