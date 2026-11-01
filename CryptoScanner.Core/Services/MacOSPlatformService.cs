using CryptoScanner.Core.Core;

using System.Runtime.InteropServices;

namespace CryptoScanner.Core.Services;

public class MacOSPlatformService : IPlatformService
{
    private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";
    private const string SystemLibrary = "/usr/lib/libSystem.dylib";

    [DllImport(ObjectiveCLibrary)]
    private static extern nint objc_getClass(string name);

    [DllImport(ObjectiveCLibrary)]
    private static extern nint sel_registerName(string name);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern nint SendMessage(nint receiver, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern nint SendMessage(nint receiver, nint selector, nint argument);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern nint SendMessage(nint receiver, nint selector, nint argument1, nuint argument2);

    [DllImport(SystemLibrary)]
    private static extern void dispatch_async_f(nint queue, nint context, nint work);

    private delegate void DispatchWork(nint context);

    /// <summary>Kept in a field: native code holds only the pointer, so the delegate must not be collected.</summary>
    private static readonly DispatchWork SetDockIconWork = SetDockIconOnMainThread;

    /// <summary>The picture waiting for the main thread; when two are queued, the last one wins.</summary>
    private static byte[]? _pendingDockIcon;

    /// <summary>
    /// The Dock shows the icon of the application bundle and never the icon of a window, so the
    /// picture becomes the application icon (NSApp.applicationIconImage). AppKit may only be touched
    /// from the main thread, which Avalonia and Photino both run the Cocoa event loop on; the work is
    /// queued on the main dispatch queue, which that loop drains. Queued before the loop runs, it
    /// waits until the application has finished launching, so the bundle icon does not overwrite it.
    /// </summary>
    public void ApplyApplicationIcon(byte[] png)
    {
        if (!OperatingSystem.IsMacOS())
            return;

        try
        {
            Volatile.Write(ref _pendingDockIcon, png);
            nint mainQueue = NativeLibrary.GetExport(NativeLibrary.Load(SystemLibrary), "_dispatch_main_q");
            dispatch_async_f(mainQueue, 0, Marshal.GetFunctionPointerForDelegate(SetDockIconWork));
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "ApplyApplicationIcon");
        }
    }

    /// <summary>Runs on the main thread; an exception must not escape into native code.</summary>
    private static void SetDockIconOnMainThread(nint context)
    {
        try
        {
            byte[]? png = Interlocked.Exchange(ref _pendingDockIcon, null);
            if (png == null)
                return;

            // NSData copies the bytes, so they only need to stay pinned for the call itself
            GCHandle pinned = GCHandle.Alloc(png, GCHandleType.Pinned);
            nint data;
            try
            {
                data = SendMessage(SendMessage(objc_getClass("NSData"), sel_registerName("alloc")),
                    sel_registerName("initWithBytes:length:"), pinned.AddrOfPinnedObject(), (nuint)png.Length);
            }
            finally
            {
                pinned.Free();
            }
            if (data == 0)
                return;

            nint image = SendMessage(SendMessage(objc_getClass("NSImage"), sel_registerName("alloc")),
                sel_registerName("initWithData:"), data);
            if (image != 0)
            {
                nint application = SendMessage(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
                SendMessage(application, sel_registerName("setApplicationIconImage:"), image);
                SendMessage(image, sel_registerName("release"));
            }
            SendMessage(data, sel_registerName("release"));
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "SetDockIconOnMainThread");
        }
    }

    public string GetDataDirectory()
    {
        // Normally we store data in the user data folder under the name of the application
        var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // But we can overrule that via the -f parameter and that can be a partial or a full path
        ApplicationParams.InitApplicationOptions();
        var folder = ApplicationParams.Options?.AppDataFolder;
        if (string.IsNullOrEmpty(folder))
        {
            // This is the standard path
            return Path.Combine(baseFolder, Const.Constants.AppName);
        }
        else if (!Path.IsPathFullyQualified(folder))
        {
            // This is the standard path + folder parameter
            return Path.Combine(baseFolder, folder);
        }
        else
        {
            // This is a full path given by the parameter
            return folder;
        }
    }

    /// <summary>
    /// Opens the file with its associated application through the open command of macOS, which is
    /// what Finder does on a double click.
    /// </summary>
    public void OpenFile(string path)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo("open") { UseShellExecute = false };
            startInfo.ArgumentList.Add(path);
            using var process = System.Diagnostics.Process.Start(startInfo);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "OpenFile");
            GlobalData.AddErrorToLogTab($"Cannot open {path}: {error.Message}");
        }
    }

    /// <summary>
    /// macOS has no message-box call we can reach from .NET, so this goes through AppleScript.
    /// Falls back to the console when osascript is not available.
    /// </summary>
    public void ShowMessage(string title, string message)
    {
        try
        {
            // AppleScript string literals only need the quote and the backslash escaped
            string text = message.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string caption = title.Replace("\\", "\\\\").Replace("\"", "\\\"");

            var startInfo = new System.Diagnostics.ProcessStartInfo("osascript") { UseShellExecute = false };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add($"display dialog \"{text}\" with title \"{caption}\" buttons {{\"OK\"}} default button 1 with icon caution");

            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit();
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "ShowMessage");
            Console.WriteLine($"{title}: {message}");
        }
    }
}