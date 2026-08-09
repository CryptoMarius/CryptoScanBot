using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Services;
using CryptoScanner.UI.ViewModels;

using System.Collections.Concurrent;
using System.ComponentModel;

namespace CryptoScanner.UI.Services;

public class LogService : IDisposable
{
    private const string GridName = "Log";
    private const int MaxLogLines = 5000;
    private readonly object _lock = new();
    private readonly ApplicationStateService _stateService;
    private List<LogEntryViewModel> _entries = [];
    private readonly ConcurrentQueue<LogEntryViewModel> _pendingEntries = new();

    // Pump lives in the singleton service, not in Log.razor: otherwise the queue keeps growing
    // (and the MaxLogLines cap never applies) while the Log tab is closed.
    private System.Threading.Timer? _pumpTimer;
    private bool _disposed;

    public GridSortState<LogColumnEnum> SortState { get; }

    public event Action? LogsChanged;

    public LogService(ApplicationStateService stateService)
    {
        _stateService = stateService;

        _stateService.RestoreGridSortState(GridName, out var sortColumn, out var sortDirection);
        SortState = !string.IsNullOrEmpty(sortColumn)
            ? new GridSortState<LogColumnEnum>()
            : new GridSortState<LogColumnEnum>(LogColumnEnum.Date, ListSortDirection.Ascending);
        SortState.Restore(sortColumn, sortDirection);
    }

    public IReadOnlyList<LogEntryViewModel> Entries
    {
        get
        {
            lock (_lock)
                return _entries.ToList();
        }
    }

    public void Start()
    {
        GlobalData.LogToLogTabEvent += OnLogMessage;

        _pumpTimer = new System.Threading.Timer(_ =>
        {
            if (_disposed || GlobalData.ApplicationIsClosing)
                return;
            try
            {
                ProcessPendingEntries();
            }
            catch
            {
            }
        }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    public void Sort(LogColumnEnum column)
    {
        SortState.ToggleSort(column);
        ApplySort();
        _stateService.SaveGridSortState(GridName, SortState.SortColumnName, SortState.SortDirection);
        LogsChanged?.Invoke();
    }

    public void ProcessPendingEntries()
    {
        bool changed = false;
        lock (_lock)
        {
            // Way too much for us to follow — drop the backlog instead of choking on it
            if (_pendingEntries.Count > MaxLogLines)
            {
                while (_pendingEntries.TryDequeue(out _)) { }
            }

            while (_pendingEntries.TryDequeue(out var entry))
            {
                _entries.Add(entry);
                changed = true;
            }

            if (_entries.Count > MaxLogLines)
            {
                _entries.RemoveRange(0, _entries.Count - MaxLogLines);
                changed = true;
            }
        }
        if (changed)
        {
            ApplySort();
            LogsChanged?.Invoke();
        }
    }

    public void Clear()
    {
        lock (_lock)
            _entries.Clear();
        LogsChanged?.Invoke();
    }

    private void OnLogMessage(string text)
    {
        try
        {
            // Mirror the Avalonia LogGridViewModel: everything that reaches the log tab is also
            // written to the NLog file, and empty lines are dropped.
            ScannerLog.Logger.Info(text);
            text = text.Trim();
            if (text == "")
                return;

            _pendingEntries.Enqueue(new LogEntryViewModel
            {
                Date = DateTime.Now,
                Text = text,
            });
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "adding " + text);
        }
    }

    private void ApplySort()
    {
        if (SortState.SortColumn is not { } col)
            return;

        lock (_lock)
        {
            var comparer = new LogEntryComparer(col);
            if (SortState.IsAscending)
                _entries.Sort(comparer);
            else
                _entries.Sort((a, b) => comparer.Compare(b, a));
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _pumpTimer?.Dispose();
        _pumpTimer = null;
        GlobalData.LogToLogTabEvent -= OnLogMessage;
        GC.SuppressFinalize(this);
    }
}

internal class LogEntryComparer(LogColumnEnum sortColumn) : IComparer<LogEntryViewModel>
{
    public int Compare(LogEntryViewModel? x, LogEntryViewModel? y)
    {
        if (x == null || y == null)
            return 0;

        int result = sortColumn switch
        {
            LogColumnEnum.Date => x.Date.CompareTo(y.Date),
            LogColumnEnum.Text => string.Compare(x.Text, y.Text, StringComparison.OrdinalIgnoreCase),
            _ => 0,
        };

        if (result == 0)
            result = x.Date.CompareTo(y.Date);

        return result;
    }
}
