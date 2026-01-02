using Avalonia.Threading;

using CryptoScanner.Core.Core;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;
using System.Text;

namespace CryptoScanner.ViewModels;

public partial class LogViewModel : ObservableObject
{
    private const int MaxLogLines = 5000;
    private DispatcherTimer? _updateTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    /// <summary>
    /// Queued text for the Log tab
    /// LogViewModel pulls the text via a timer.
    /// </summary>
    public static readonly Queue<string> LogQueue = new();

    [ObservableProperty]
    private ObservableCollection<string> _logLines = [];

    [ObservableProperty]
    private bool _autoScroll = true; // does not work properly

    public LogViewModel()
    {
        System.Diagnostics.Debug.WriteLine("LogViewModel constructor called");
        LogQueue.EnsureCapacity(2500);
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
            LogQueue.Enqueue(text);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "adding " + text);
        }
    }

    private void TimerAddLogLinesTick(object? sender, EventArgs? e)
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
                    StringBuilder stringBuilder = new();

                    while (LogQueue.Count > 0 && !GlobalData.ApplicationIsClosing)
                    {
                        string text = LogQueue.Dequeue();
                        stringBuilder.AppendLine(text);
                        //LogLines.Add(logEntry);
                    }

                    string allText = stringBuilder.ToString().Trim();
                    if (allText != "")
                    {
                        AddLogLine(allText);
                    }
                }
                finally
                {
                    Monitor.Exit(LogQueue);
                }
            }
        }
    }

    /// <summary>
    /// Adds a single log line with timestamp
    /// </summary>
    public void AddLogLine(string message)
    {
        //var timestamp = DateTime.Now.ToString("HH:mm:ss");
        //var logEntry = $"[{timestamp}] {message}";
        
        LogLines.Add(message);
        
        // Keep only last MaxLogLines entries
        if (LogLines.Count > MaxLogLines)
            LogLines.RemoveAt(0);
        
        // Trigger scroll if auto-scroll is enabled
        if (AutoScroll)
            RequestScrollToEnd?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Adds multiple log lines at once (more efficient)
    /// </summary>
    public void AddLogLines(params string[] messages)
    {
        foreach (var message in messages)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] {message}";
            LogLines.Add(logEntry);
        }
        
        // Trim excess lines
        while (LogLines.Count > MaxLogLines)
            LogLines.RemoveAt(0);
        
        //if (AutoScroll)
        //    RequestScrollToEnd?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event to request scroll to end (handled by View)
    /// </summary>
    public event EventHandler? RequestScrollToEnd;

    [RelayCommand]
    private void ClearLog()
    {
        LogLines.Clear();
        AddLogLine("Log cleared");
    }

}
