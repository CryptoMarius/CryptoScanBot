using Avalonia.Threading;

using CryptoScanner.Core.Core;

using CommunityToolkit.Mvvm.ComponentModel;
using CryptoScanner.Model;

namespace CryptoScanner.ViewModels;

public partial class LogGridViewModel : ObservableObject
{
    private const int MaxLogLines = 5000;
    private DispatcherTimer? _updateTimer = new() { Interval = TimeSpan.FromMilliseconds(2000) };

    /// <summary>
    /// Queued text for the Log tab
    /// LogViewModel pulls the text via a timer.
    /// </summary>
    public static readonly Queue<LogViewModel> LogQueue = new();

    /// <summary>
    /// Collection of lines to display in the grid
    /// </summary>
    [ObservableProperty]
    private ObservableRangeCollection<LogViewModel> _logLines = [];

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
        _updateTimer?.Stop();
        _updateTimer = null;
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

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                // Save current selection
                var selected = SelectedLogLine;

                // Add items one by one
                while (LogQueue.Count > 0 && !GlobalData.ApplicationIsClosing)
                    LogLines.Add(LogQueue.Dequeue());

                // Keep only last MaxLogLines entries
                while (LogLines.Count > MaxLogLines)
                    LogLines.RemoveAt(0);

                // Restore selection
                if (selected != null)
                {
                    if (LogLines.Contains(selected))
                        SelectedLogLine = selected;
                    else
                        SelectedLogLine = LogLines.LastOrDefault();
                }

                // Auto-scroll to lastline to last line
                if (SelectedLogLine == null || SelectedLogLine == LogLines.LastOrDefault())
                    SelectedLogLine = LogLines.LastOrDefault();
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "logtick");
            }
        });
    }

}
