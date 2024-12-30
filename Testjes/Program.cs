using CryptoScanBot;
using CryptoScanBot.Core.Core;

using System.Reflection;

namespace Testjes2
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            // Vroeger dan alle andere..
            ApplicationConfiguration.Initialize();
            InitializeApplicationVariables();
            GlobalData.AppName = "CryptoScanBot";
            GlobalData.LogName = "CryptoShowTrend";
            ScannerLog.InitializeLogging();

            // Add the event handler for handling UI thread exceptions to the event.
            Application.ThreadException += new ThreadExceptionEventHandler(OnThreadException);

            // Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Add the event handler for handling non-UI thread exceptions to the event. 
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);

            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new TestForm());
        }

        public static void InitializeApplicationVariables()
        {
            GlobalData.AppName = Assembly.GetExecutingAssembly().GetName().Name;
            GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var assembly = Assembly.GetExecutingAssembly().GetName();
            string appVersion = assembly.Version.ToString();
            while (appVersion.EndsWith(".0.0"))
                appVersion = appVersion[0..^2];

            GlobalData.AppVersion = appVersion;
        }


        static void UnhandledException(object? sender, UnhandledExceptionEventArgs eventArgs)
        {
            //MessageBox.Show("UnhandledException!!!!");
            Exception e = (Exception)eventArgs.ExceptionObject;
            if (eventArgs.IsTerminating)
                ScannerLog.Logger.Error(e, "UnhandledException (terminating)");
            else
                ScannerLog.Logger.Error(e, "UnhandledException (not terminating)");
        }

        static void OnThreadException(object? sender, ThreadExceptionEventArgs eventArgs)
        {
            ScannerLog.Logger.Info("");
            ScannerLog.Logger.Info("Error " + eventArgs.Exception.Message);
            ScannerLog.Logger.Error("");
            ScannerLog.Logger.Error(eventArgs.Exception, "Global Thread Exception");
        }

    }
}