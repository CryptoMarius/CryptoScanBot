using Avalonia.Collections;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>
/// Lightweight log viewer for the emulator. Hooks <see cref="GlobalData.LogToLogTabEvent"/>
/// — the same event the live scanner's LogGridViewModel listens to — so any <c>AddTextToLogTab</c>
/// call anywhere in Core surfaces here. Lines are timestamped with the real wall-clock
/// (<see cref="DateTime.Now"/>): during a run <c>GlobalData.Clock</c> is the virtual EmulatorClock,
/// whose UtcNow jumps around the replay window (and reads as a nonsensical year-0001 time before
/// the first tick), which is useless for telling when a line was actually emitted.
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
        GlobalData.LogToLogTabEvent += OnLog;
    }


    private void OnLog(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        string stamped = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {text.Trim()}";

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
