using CryptoScanner.Core.Core;

using System.Reflection;

namespace CryptoScanner.CoreTests;

/// <summary>
/// Assembly-level initialization that runs once before any test in this project.
/// Sets the required application paths so that CryptoDatabase and GlobalData work
/// correctly without depending on a production application startup or ApplicationParams.
///
/// Using [AssemblyInitialize] ensures the paths are always set — regardless of
/// whether a test calls InitTestSession() or not. It is the MSTest-standard way
/// to express "this must happen before anything else in the assembly".
/// </summary>
[TestClass]
public class TestAssemblySetup
{
    [AssemblyInitialize]
    public static void AssemblyInit(TestContext _)
    {
        // AppVersion
        var assembly = Assembly.GetExecutingAssembly().GetName();
        GlobalData.AppVersion = assembly.Version!.ToString();

        // AppPath: directory of the test binary (used for sound files etc.)
        GlobalData.AppPath = AppContext.BaseDirectory;
        //GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

        // AppDataFolder: isolated subfolder so tests never touch the production database.
        // GetBaseDir() guards on IsNullOrEmpty, so setting this here prevents it from
        // calling ApplicationParams.InitApplicationOptions() and redirecting to AppData.
        GlobalData.AppDataFolder = Path.Combine(GlobalData.AppPath, "TestData");
        Directory.CreateDirectory(GlobalData.AppDataFolder);

        // Before anything opens that database: wait for our turn. Several sessions work in this
        // repository at the same time and every one of them runs the suite, and two runs on the
        // one test database delete each other's rows halfway through an arrange. See TestRunLock.
        double waited = TestRunLock.Acquire(GlobalData.AppDataFolder);
        if (waited > 0)
            Console.WriteLine($"Waited {waited:N0} seconds for the other test run to finish.");

        // Wire up log output to the test console (same as TestBase.AddTextToLogTab)
        GlobalData.LogToLogTabEvent += text => Console.WriteLine(text.Trim());

        ScannerLog.InitializeLogging(false);
    }


    /// <summary>
    /// Hands the test database back to whoever is waiting for it. Runs once, after the last test
    /// of the assembly. Not the only thing that releases the lock - the operating system closes
    /// the handle when the process ends however it ends - but it is the one that frees it while
    /// the test host is still shutting down.
    /// </summary>
    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        TestRunLock.Release();
    }
}
