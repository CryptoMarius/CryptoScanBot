using Xilium.CefGlue;

namespace CryptoScanner.Browser.Helpers;

public class CustomCefApp : CefApp
{
    protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
    {
        // Optional: Disable GPU for compatibility
        // commandLine.AppendSwitch("disable-gpu");

        base.OnBeforeCommandLineProcessing(processType, commandLine);
    }
}