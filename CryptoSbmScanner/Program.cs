using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;

namespace CryptoSbmScanner
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        static void Main()
        {
            // Add the event handler for handling UI thread exceptions to the event.
            Application.ThreadException += new ThreadExceptionEventHandler(OnThreadException);

            // Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Add the event handler for handling non-UI thread exceptions to the event. 
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }


        static void UnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
        {
            //MessageBox.Show("UnhandledException!!!!");
            Exception e = (Exception)eventArgs.ExceptionObject;
            if (eventArgs.IsTerminating)
                GlobalData.Logger.Error(e, "UnhandledException (terminating)");
            else
                GlobalData.Logger.Error(e, "UnhandledException (not terminating)");
            //Application.Exit();
        }

        static void OnThreadException(object sender, ThreadExceptionEventArgs t)
        {
            GlobalData.Logger.Error(t.Exception, "Global Thread Exception");
            //MessageBox.Show("UIThreadException!!!!","UIThreadException!!!!", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Stop);
            //Application.Exit();
        }
    }
}
