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
                if (GlobalData.BackTest)
                    text = GlobalData.BackTestDateTime.ToLocalTime() + " " + text;
                else
                    text = DateTime.Now.ToLocalTime() + " " + text;
            }
            LogQueue.Enqueue(new LogViewModel() { Date = DateTime.Now, Text = text, });

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

                    // Add items one by one
                    List<LogViewModel> list = [];
                    while (LogQueue.Count > 0 && !GlobalData.ApplicationIsClosing)
                        list.Add(LogQueue.Dequeue());
                    LogLines.AddRange(list);

                    // Keep only last MaxLogLines entries (single RemoveRange to avoid DataGridCollectionView index desync)
                    int excess = LogLines.Count - MaxLogLines;
                    if (excess > 0)
                        LogLines.RemoveRange(0, excess);


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
