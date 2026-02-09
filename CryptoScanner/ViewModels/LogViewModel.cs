using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CryptoScanner.ViewModels;

public partial class LogViewModel : BaseGridViewModel<CryptoLog, LogColumnEnum, LogColumnComparer>
{
    private const int MaxLogLines = 5000;
    public static readonly Queue<CryptoLog> LogQueue = new();
    private readonly DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromMilliseconds(2000) };

    public LogViewModel()
    {
        System.Diagnostics.Debug.WriteLine("LogGridViewModel constructor called");

        SortColumn = LogColumnEnum.Date;
        _columns = LogColumns.GetColumns();
        _columnWidths = GetWidths(_columns);
        System.Diagnostics.Debug.WriteLine($"LogGridViewModel: {_columns.Count} columns, {_columnWidths.Count} widths");

        GlobalData.LogToLogTabEvent += new AddTextEvent(AddTextToLogTab);

        LogQueue.EnsureCapacity(5000);

        _updateTimer.Tick += TimerAddLogLinesTick;
        _updateTimer.Start();
    }

    public void Dispose()
    {
        _updateTimer.Stop();
        _updateTimer.Tick -= TimerAddLogLinesTick;
    }

    protected override void RefreshVisibleItems()
    {
        System.Diagnostics.Debug.WriteLine("RefreshVisibleLogs called");
        if (Dispatcher.UIThread.CheckAccess())
        {
            lock (_lock)
            {
                VisibleObjects = new AvaloniaList<CryptoLog>(_allObjects);
            }
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                lock (_lock)
                {
                    VisibleObjects = new AvaloniaList<CryptoLog>(_allObjects);
                }
            });
        }
    }

    private void AddTextToLogTab(string text)
    {
        try
        {
            ScannerLog.Logger.Info(text);
            text = text.Trim();
            if (text != "")
            {
                LogQueue.Enqueue(new CryptoLog() { Date = DateTime.Now, Text = text });
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

        Task.Run(() =>
        {
            try
            {
                List<CryptoLog> list = [];

                lock (LogQueue)
                {
                    while (LogQueue.Count > MaxLogLines)
                        LogQueue.Dequeue();

                    while (LogQueue.Count > 0 && !GlobalData.ApplicationIsClosing)
                        list.Add(LogQueue.Dequeue());
                }

                if (list.Count > 0)
                {
                    lock (_lock)
                    {
                        _allObjects.AddRange(list);

                        while (_allObjects.Count > MaxLogLines)
                            _allObjects.RemoveAt(0);

                        ApplySort(SortColumn);
                    }

                    RefreshVisibleItems();
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "logtick");
            }
        });
    }


}