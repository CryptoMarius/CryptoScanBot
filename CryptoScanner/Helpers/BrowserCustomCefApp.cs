//using Xilium.CefGlue;

//namespace CryptoScanner.Helpers;

//public class BrowserCustomCefApp : CefApp
//{
//    protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
//    {
//        // Optional: Disable GPU for compatibility
//        // commandLine.AppendSwitch("disable-gpu");
//        commandLine.AppendSwitch("disable-gpu");
//        commandLine.AppendSwitch("disable-gpu-compositing");
//        base.OnBeforeCommandLineProcessing(processType, commandLine);
//    }


//    public static void InitializeCefRuntime()
//    {
//        //return;
//        System.Diagnostics.Debug.WriteLine($"InitializeCefRuntime");

//        //var platformService = App.Services.GetRequiredService<IPlatformService>();
//        //string dataPath = platformService.GetDataDirectory();

//        // Load CefGlue runtime
//        //CefRuntime.Load();

//        var settings = new CefSettings
//        {
//            //CachePath = Path.Combine(dataPath, "Browser"),
//            //LogFile = Path.Combine(dataPath, "Browser", "cef.log"),
//            LogSeverity = CefLogSeverity.Warning,
//            WindowlessRenderingEnabled = true,
//        };

//        var mainArgs = new CefMainArgs([]);
//        CefRuntime.Initialize(mainArgs, settings, new BrowserCustomCefApp(), IntPtr.Zero);
//    }
//}