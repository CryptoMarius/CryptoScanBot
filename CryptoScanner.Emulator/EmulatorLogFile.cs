using CryptoScanner.Core.Core;

namespace CryptoScanner.Emulator;

/// <summary>
/// Mirrors every <c>GlobalData.AddTextToLogTab(...)</c> line to a plain-text file next to the
/// NLog files, so the in-app Log tab's content survives a restart and can be grepped/shared.
///
/// NLog (<see cref="ScannerLog"/>) only captures <c>ScannerLog.Logger.*</c> calls; the human
/// progress messages the emulator and Core push through <c>AddTextToLogTab</c> never reach those
/// files. This class closes that gap by subscribing to the same <see cref="GlobalData.LogToLogTabEvent"/>
/// the Log tab uses and appending each line with a real wall-clock timestamp.
///
/// Writes are guarded by a lock (the event fires from the replay thread, REST fetches, the UI, …)
/// and flushed per line so a crash mid-run still leaves a complete log. The volume is low once the
/// per-tick "missing candles" noise is gone, so per-line flush is not a bottleneck.
/// </summary>
public static class EmulatorLogFile
{
    private static readonly object Gate = new();
    private static StreamWriter? _writer;


    /// <summary>
    /// Opens (or creates) the log file under <c>{AppDataFolder}/Log</c> and starts mirroring.
    /// Must be called after <c>GlobalData.AppDataFolder</c> is set. Safe to call once; a second
    /// call is ignored so we never double-subscribe.
    /// </summary>
    public static void Start()
    {
        lock (Gate)
        {
            if (_writer != null)
                return;

            try
            {
                string logFolder = Path.Combine(GlobalData.AppDataFolder, "Log");
                Directory.CreateDirectory(logFolder);
                string path = Path.Combine(logFolder, "emulator-logtab.log");

                _writer = new StreamWriter(path, append: true) { AutoFlush = true };
                _writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  ===== emulator log session started =====");
            }
            catch
            {
                // A missing/locked file must never take the app down — the in-app Log tab still works.
                _writer = null;
                return;
            }
        }

        GlobalData.LogToLogTabEvent += OnLog;
    }


    private static void OnLog(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        lock (Gate)
        {
            if (_writer == null)
                return;
            try
            {
                _writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {text.Trim()}");
            }
            catch
            {
                // ignore — never let logging crash the run
            }
        }
    }
}
