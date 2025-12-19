using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text;
using CryptoScanner.Core.Core;
using Avalonia.Threading;

namespace CryptoScanner.Log.ViewModels;

public partial class LogViewModel : ObservableObject
{
    private const int MaxLogLines = 5000;
    private DispatcherTimer? _updateTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };


    [ObservableProperty]
    private ObservableCollection<string> _logLines = [];

    [ObservableProperty]
    private bool _autoScroll = true;

    public LogViewModel()
    {
        System.Diagnostics.Debug.WriteLine("LogViewModel constructor called");

        _updateTimer.Tick += TimerAddLogLinesTick;
        _updateTimer.Start();
    }

    public void Dispose()
    {
        _updateTimer?.Stop();
        _updateTimer = null;
    }

    private void TimerAddLogLinesTick(object? sender, EventArgs? e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        // Speed up adding text
        if (App.LogQueue.Count > 0)
        {
            if (Monitor.TryEnter(App.LogQueue))
            {
                try
                {
                    StringBuilder stringBuilder = new();

                    while (App.LogQueue.Count > 0 && !GlobalData.ApplicationIsClosing)
                    {
                        string text = App.LogQueue.Dequeue();
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
                    Monitor.Exit(App.LogQueue);
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
        
        if (AutoScroll)
            RequestScrollToEnd?.Invoke(this, EventArgs.Empty);
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
