using CryptoScanner.Core.Core;

namespace CryptoScanner.Emulator;

/// <summary>
/// Routes every <see cref="GlobalData.AddTextToLogTab"/> line to the NLog files, independently of
/// the UI. The in-app Log tab (<c>LogTabViewModel</c>) is only responsible for rendering lines for
/// the user — and its subscription is currently disabled to keep long runs responsive. On-disk
/// logging must NOT depend on that UI ViewModel being alive and subscribed: otherwise early
/// bootstrap lines (logged before the MainWindow exists) and entire runs — including the
/// <c>Timing —</c> profiling line and the per-run log file — silently never reach the files.
///
/// This subscribes exactly once, right after <see cref="ScannerLog.InitializeLogging"/>, for the
/// whole application lifetime. NLog applies its own wall-clock layout, so the on-disk format matches
/// the live scanner. The per-run log file attached by <see cref="ScannerLog.StartRunLog"/> picks
/// these lines up automatically (its rule is "* at Info"), so each run gets its own log.
/// </summary>
public static class EmulatorLogBridge
{
    private static bool started;

    /// <summary>
    /// Begins forwarding log-tab lines to NLog. Idempotent: a second call is a no-op, so it can be
    /// invoked defensively without risk of double-logging every line.
    /// </summary>
    public static void Start()
    {
        if (started)
            return;
        started = true;
        GlobalData.LogToLogTabEvent += OnLog;
    }


    private static void OnLog(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        // Wrapped so a logging failure can never break a run — exactly as the scanner's
        // LogGridViewModel does when it mirrors lines into NLog.
        try
        {
            ScannerLog.Logger.Info(text.Trim());
        }
        catch
        {
            // ignore — never let logging crash the run
        }
    }
}
