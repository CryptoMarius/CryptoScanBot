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

    [ObservableProperty]
    private bool _autoScroll = false; // does not work properly

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
            // Via queue want afzonderlijk regels toevoegen kost relatief veel tijd
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
        try
        {
            if (GlobalData.ApplicationIsClosing)
                return;

            // Speed up adding text
            if (LogQueue.Count > 0)
            {
                if (Monitor.TryEnter(LogQueue))
                {
                    try
                    {
                        List<LogViewModel> lines = [];
                        while (LogQueue.Count > 0 && !GlobalData.ApplicationIsClosing)
                        {
                            var x = LogQueue.Dequeue();
                            lines.Add(x);
                        }
                        LogLines.AddRange(lines);
                    }
                    finally
                    {
                        Monitor.Exit(LogQueue);
                    }
                }
            }

            // Keep only last MaxLogLines entries
            if (LogLines.Count > MaxLogLines)
                LogLines.RemoveAt(0);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "logtick");
        }
    }

}
