using System.Reflection;

using CryptoScanBot.Core.Intern;

namespace ExchangeTest;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        InitializeApplicationVariables();
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }


    public static void InitializeApplicationVariables()
    {
        GlobalData.AppName = Assembly.GetExecutingAssembly().GetName().Name;
        GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appVersion = assembly.Version.ToString();
        while (appVersion.EndsWith(".0"))
            appVersion = appVersion[0..^2];

        GlobalData.AppVersion = appVersion;
    }
}