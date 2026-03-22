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

        // Wire up log output to the test console (same as TestBase.AddTextToLogTab)
        GlobalData.LogToLogTabEvent += text => Console.WriteLine(text.Trim());

        ScannerLog.InitializeLogging();
    }
}
