using CryptoScanBot.Core.Core;

using System.Reflection;

namespace CryptoScanBot.Experiment;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Vroeger dan alle andere..
        InitializeApplicationVariables();
        ScannerLog.InitializeLogging();

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }


    public static void InitializeApplicationVariables()
    {
        // Appname && name of database
        GlobalData.AppName = "CryptoScanBot"; // Assembly.GetExecutingAssembly().GetName().Name;

        // Path of the executable
        GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        // Version stuff
        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appVersion = assembly.Version.ToString();
        while (appVersion.EndsWith(".0"))
            appVersion = appVersion[0..^2];
        GlobalData.AppVersion = appVersion;
    }
}