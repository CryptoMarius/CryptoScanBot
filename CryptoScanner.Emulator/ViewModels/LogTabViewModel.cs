using Avalonia.Collections;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>
/// Lightweight log viewer for the emulator. Hooks <see cref="GlobalData.LogToLogTabEvent"/>
/// — the same event the live scanner's LogGridViewModel listens to — so any <c>AddTextToLogTab</c>
/// call anywhere in Core surfaces here.
///
/// Like the scanner's LogGridViewModel, every line is ALSO forwarded to
/// <see cref="ScannerLog.Logger"/> at Info level, so it lands in the NLog files configured by
/// <see cref="ScannerLog.InitializeLogging"/> — the default log, and (in DEBUG) the Trace log that
/// also captures explicit <c>ScannerLog.Logger.Trace(...)</c> calls. That gives the emulator the
/// exact same on-disk logging as the scanner: drop a Trace call anywhere and it shows up in the
/// Trace file alongside the human progress lines.
///
/// Timestamp (in-app only): during a run (<see cref="GlobalData.CurrentEmulatorRunId"/> set) lines
/// are stamped with the virtual EmulatorClock — i.e. the replay date the emulator is currently AT —
/// so you can see how far the run has progressed straight from the log. Outside a run the
/// EmulatorClock is uninitialised (reads as a year-0001 time), so we fall back to the real
/// wall-clock there. The NLog files use their own wall-clock layout, same as the scanner.
///
/// Older entries are pruned at <see cref="MaxLines"/> to stop the list from growing without bound
/// during long runs. The deliberate simplification vs the scanner version: no DataGrid sort, no
/// timer flush — for the emulator's volume an immediate dispatcher-post is fast enough.
/// </summary>
public partial class LogTabViewModel : ObservableObject
{
    private const int MaxLines = 5000;

    [ObservableProperty]
    private AvaloniaList<string> _lines = [];


    public LogTabViewModel()
    {
        //GlobalData.LogToLogTabEvent += OnLog;
    }


    private void OnLog(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        // Mirror the scanner's LogGridViewModel: route every log line through NLog so it lands in
        // the on-disk log + Trace files. Wrapped so a logging failure can never break a run.
        try
        {
            ScannerLog.Logger.Info(text.Trim());
        }
        catch
        {
            // ignore — never let logging crash the run
        }

        //DateTime stamp = GlobalData.CurrentEmulatorRunId != null
        //    ? GlobalData.Clock.UtcNow   // virtual replay date — shows how far the run has progressed
        //    : DateTime.Now;             // wall-clock when no run is active (EmulatorClock is unset)
        //string stamped = $"{stamp:yyyy-MM-dd HH:mm:ss}  {text.Trim()}";
        string stamped = text.Trim();

        // Marshal to the UI thread because LogToLogTabEvent fires from any worker (REST
        // fetch, TickRunner, etc.). Without the post Avalonia logs binding errors.
        Dispatcher.UIThread.Post(() =>
        {
            Lines.Add(stamped);
            while (Lines.Count > MaxLines)
                Lines.RemoveAt(0);
        });
    }


    [RelayCommand]
    private void Clear() => Lines.Clear();
}
