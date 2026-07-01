using Avalonia.Collections;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;

namespace CryptoScanner.ViewModels;

public partial class LogGridViewModel : ObservableObject
{
    private const int MaxLogLines = 5000;
    private readonly DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromMilliseconds(2000) };

    /// <summary>
    /// Queued text for the Log tab
    /// LogViewModel pulls the text via a timer.
    /// </summary>
    public static readonly Queue<LogViewModel> LogQueue = new();

    /// <summary>
    /// Collection of lines to display in the grid
    /// </summary>
    [ObservableProperty]
    private AvaloniaList<LogViewModel> _logLines = [];

    public LogViewModel? SelectedLogLine { get; set; }


    public LogGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("LogGridViewModel constructor called");
        LogQueue.EnsureCapacity(25000);
        GlobalData.LogToLogTabEvent += new AddTextEvent(AddTextToLogTab);

        _updateTimer.Tick += TimerAddLogLinesTick;
        _updateTimer.Start();
    }

    public void Dispose()
    {
        _updateTimer.Stop();
        _updateTimer.Tick -= TimerAddLogLinesTick;
    }

    private void AddTextToLogTab(string text)
    {
        // The queue can be overwhelmed (and there is a max array size)
        try
        {
            // Use a queue because adding lines cost a lot of time (notification/refresh)
            ScannerLog.Logger.Info(text);
            text = text.Trim();

            if (text != "")
            {
                // Clock.UtcNow returns the emulator's current candle close-time in emulator mode,
                // wall-clock otherwise — single source so log timestamps follow the active clock.
                text = GlobalData.Clock.UtcNow.ToLocalTime() + " " + text;
                LogQueue.Enqueue(new LogViewModel() { Date = DateTime.Now, Text = text, });
            }

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "adding " + text);
        }
    }

    private void TimerAddLogLinesTick(object? sender, EventArgs? e)
    {
        if (GlobalData.ApplicationIsClosing || LogQueue.Count == 0)
            return;

        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    // Save current selection
                    var selected = SelectedLogLine;

                    // Way to much for us to follow..
                    while (LogQueue.Count > MaxLogLines)
                        LogQueue.Clear();

                    // Add items one at a time — NOT via AddRange. AvaloniaList.AddRange fires a
                    // single CollectionChanged event with Action = Reset, which causes the DataGrid
                    // to invalidate everything: selection, scroll position and keyboard focus are
                    // lost. Individual Add() calls fire fine-grained Add events that the view
                    // handles in-place, preserving the user's reading position.
                    while (LogQueue.Count > 0 && !GlobalData.ApplicationIsClosing)
                        LogLines.Add(LogQueue.Dequeue());

                    // Prune oldest entries one at a time for the same reason. Clear() also fires
                    // Reset — we cannot use it here. RemoveAt(0) fires individual Remove events
                    // and avoids the index-desync that RemoveRange used to cause.
                    // NOTE: Avalonia's DataGridCollectionView (wrapped around LogLines because the
                    // DataGrid has CanUserSortColumns=True) can throw ArgumentOutOfRangeException
                    // from its internal selection/index tracking while processing the Remove event.
                    // The item itself IS removed from LogLines before the event fires, so the data
                    // stays consistent — only the view's currency tracking gets briefly confused.
                    // We swallow that specific exception per-item so the prune loop keeps going
                    // and the log doesn't get flooded with identical stack traces every tick.
                    while (LogLines.Count > MaxLogLines && !GlobalData.ApplicationIsClosing)
                    {
                        try
                        {
                            LogLines.RemoveAt(0);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Known Avalonia DataGridCollectionView quirk — ignore, item is gone.
                        }
                    }


                    //// Restore selection
                    //if (selected != null)
                    //{
                    //    if (LogLines.Contains(selected))
                    //        SelectedLogLine = selected;
                    //    else
                    //        SelectedLogLine = LogLines.LastOrDefault();
                    //}

                    //// Auto-scroll to lastline to last line
                    //if (SelectedLogLine == null || SelectedLogLine == LogLines.LastOrDefault())
                    //    SelectedLogLine = LogLines.LastOrDefault();
                }
                catch (Exception error)
                {
                    ScannerLog.Logger.Error(error, "logtick");
                }
            });
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "logtick");
        }
    }

    public void Clear()
    {
        LogLines.Clear();
    }
}
