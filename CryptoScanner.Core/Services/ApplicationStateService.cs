using Avalonia;
using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Services;

using System.ComponentModel;
using System.Text.Json;

namespace CryptoScanner.Core.Services;

public class GridColumn
{
    public string SortMemberPath { get; set; } = string.Empty;
    public double Width { get; set; }
    public int DisplayIndex { get; set; }
    public bool IsVisible { get; set; } = true;
}

public class GridState
{
    public string? SortColumn { get; set; }
    public ListSortDirection? SortDirection { get; set; }
    public List<GridColumn> Columns { get; set; } = [];
}

public class WindowState
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string State { get; set; } = string.Empty;  // "Normal", "Minimized", "Maximized", "FullScreen", empty= not initialized
}

public class BarometerState
{
    public string Quote { get; set; } = "";
    public string Interval { get; set; } = "1H";
}


public class ApplicationState : IApplicationState
{
    // Last selected Barometer settings
    public BarometerState BarometerState { get; set; } = new();

    // Splitter position MainWindow (Size left panel)
    public double MainWindowSplitterPosition { get; set; } = 300;

    // Window state, Size, Object, Monitor etc.
    public Dictionary<string, WindowState> WindowStates { get; set; } = [];

    public Dictionary<string, GridState> GridStates { get; set; } = [];
}

public class ApplicationStateService
{
    private ApplicationState _states;
    private readonly string _filePath;
    private readonly Lock _lock = new();
    private readonly IPlatformService? _platformService;
    private readonly IJsonSerializerService? _jsonService;

    public ApplicationStateService()
    {
        _platformService = GlobalData.GetService<IPlatformService>()
            ?? throw new InvalidOperationException("IPlatformService not registered");
        // Get services from DI container
        _jsonService = GlobalData.GetService<IJsonSerializerService>()
            ?? throw new InvalidOperationException("IJsonSerializerService not registered");

        // Ensure directory exists
        string directory = _platformService!.GetDataDirectory();
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        _filePath = Path.Combine(directory, "CryptoScanBot-user.json");

        // Load states on initialization
        _states = LoadFromFile();
    }

    public string BarometerQuote { get { return _states.BarometerState.Quote; } set { _states.BarometerState.Quote = value; } }
    public string BarometerInterval { get { return _states.BarometerState.Interval; } set { _states.BarometerState.Interval = value; } }

    public void SaveGridState(string gridName, DataGrid dataGrid, string? sortColumn, ListSortDirection? sortDirection)
    {
        ArgumentNullException.ThrowIfNull(dataGrid);

        lock (_lock)
        {
            //System.Diagnostics.Debug.WriteLine($"SaveGridState({gridName}, {sortColumn} {sortDirection})");
            var gridState = GetGridStateProperty(_states, gridName);
            if (gridState != null)
            {
                // Save sort column and direction
                gridState.SortColumn = sortColumn;
                gridState.SortDirection = sortDirection;

                // Save column settings
                gridState.Columns = dataGrid.Columns.Select(col => new GridColumn
                {
                    SortMemberPath = col.SortMemberPath ?? string.Empty,
                    Width = col.Width.IsAbsolute ? col.Width.Value : -1,  // -1 = Auto
                    DisplayIndex = col.DisplayIndex,
                    IsVisible = col.IsVisible
                }).ToList();
            }

            // Persist to disk
            FlushToDisk();
        }
    }


    public void RestoreGridState(string gridName, DataGrid dataGrid, out string sortColumn, out ListSortDirection sortDirection)
    {
        ArgumentNullException.ThrowIfNull(dataGrid);

        lock (_lock)
        {
            sortColumn = string.Empty;
            sortDirection = ListSortDirection.Ascending;

            var gridState = GetGridStateProperty(_states, gridName);
            if (gridState == null)
                return;

            // Return sort column and direction
            if (gridState.SortDirection != null && !string.IsNullOrEmpty(gridState.SortColumn))
            {
                sortColumn = gridState.SortColumn;
                sortDirection = gridState.SortDirection.Value;
            }
            //System.Diagnostics.Debug.WriteLine($"RestoreGridState({gridName}, {sortColumn} {sortDirection})");

            // Restore column settings
            foreach (var colSetting in gridState.Columns)
            {
                var column = dataGrid.Columns.FirstOrDefault(c => c.SortMemberPath == colSetting.SortMemberPath);

                if (column != null)
                {
                    // Restore width
                    if (colSetting.Width > 0)
                    {
                        column.Width = new DataGridLength(colSetting.Width);
                    }

                    // Restore display order
                    column.DisplayIndex = colSetting.DisplayIndex;

                    // Restore visibility
                    column.IsVisible = colSetting.IsVisible;
                }
            }
        }
    }

    /// <summary>
    /// Clears all saved grid states from memory and disk
    /// </summary>
    public void ClearAllStates()
    {
        lock (_lock)
        {
            _states = new ApplicationState();

            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
    }

    /// <summary>
    /// Clears the saved state for a specific grid
    /// </summary>
    /// <param name="gridName">Name of the grid to clear</param>
    public void ClearGridState(string gridName)
    {
        lock (_lock)
        {
            var gridState = GetGridStateProperty(_states, gridName);

            if (gridState != null)
            {
                gridState.SortColumn = null;
                gridState.SortDirection = null;
                gridState.Columns.Clear();

                FlushToDisk();
            }
        }
    }

    /// <summary>
    /// Reloads all states from disk, discarding in-memory changes
    /// </summary>
    public void ReloadFromDisk()
    {
        lock (_lock)
        {
            _states = LoadFromFile();
        }
    }

    /// <summary>
    /// Writes the current in-memory state to disk
    /// Should be called on application exit or when you want to ensure changes are persisted
    /// </summary>
    public void FlushToDisk()
    {
        try
        {
            // Ensure directory exists
            string directory = _platformService!.GetDataDirectory();
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Serialize and save
            var json = JsonSerializer.Serialize(_states, _jsonService!.IndentedOptions);

            File.WriteAllText(_filePath, json);
        }
        catch (Exception exc)
        {
            // Log exc but don't throw - grid state is not critical
            System.Diagnostics.Debug.WriteLine($"Failed to save application state: {exc.Message}");
        }
    }

    private ApplicationState LoadFromFile()
    {
        if (!File.Exists(_filePath))
            return new ApplicationState();

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<ApplicationState>(json) ?? new ApplicationState();
        }
        catch (Exception ex)
        {
            // Log exc but don't throw - return empty state instead
            System.Diagnostics.Debug.WriteLine($"Failed to load application state: {ex.Message}");
            return new ApplicationState();
        }
    }

    private static GridState? GetGridStateProperty(ApplicationState states, string gridName)
    {
        if (states.GridStates.TryGetValue(gridName, out var state))
            return state;

        state = new GridState();
        states.GridStates.Add(gridName, state);
        return state;
    }


    public void SaveSplitterPosition(string splitterName, double position)
    {
        lock (_lock)
        {
            switch (splitterName)
            {
                case "MainWindow":
                    _states.MainWindowSplitterPosition = position;
                    break;
            }

            FlushToDisk();
        }
    }

    public double GetSplitterPosition(string splitterName, double defaultValue = 300)
    {
        lock (_lock)
        {
            return splitterName switch
            {
                "MainWindow" => _states.MainWindowSplitterPosition > 0 ? _states.MainWindowSplitterPosition: defaultValue,
                _ => defaultValue
            };
        }
    }


    public void SaveWindowState(string windowName, Window window)
    {
        lock (_lock)
        {
            var state = GetWindowStateProperty(_states, windowName);
            if (state != null)
            {
                state.X = window.Position.X;
                state.Y = window.Position.Y;
                state.Width = window.Width;
                state.Height = window.Height;
                state.State = window.WindowState.ToString();

                FlushToDisk();
            }
        }
    }

    public void RestoreWindowState(string windowName, Window window)
    {
        lock (_lock)
        {
            var state = GetWindowStateProperty(_states, windowName);
            if (state == null) 
                return;

            // Restore window state, position and size (if state is filled)
            if (Enum.TryParse<Avalonia.Controls.WindowState>(state.State, out var windowState))
            {
                // Is saved position on ANY available screen?
                if (IsPositionOnScreen(window, state.X, state.Y))
                {
                    window.Position = new PixelPoint((int)state.X, (int)state.Y);
                }
                else
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                window.Width = state.Width;
                window.Height = state.Height;
                window.WindowState = windowState;
            }
        }
    }

    private WindowState? GetWindowStateProperty(ApplicationState states, string windowName)
    {
        if (_states.WindowStates.TryGetValue(windowName, out var state))
            return state;

        state = new();
        _states.WindowStates.Add(windowName, state);
        return state;
    }

    private static bool IsPositionOnScreen(Window window, double x, double y)
    {
        try
        {
            var point = new PixelPoint((int)x, (int)y);
            var screens = window.Screens.All;

            // Check if point is within ANY screen's working area
            foreach (var screen in screens)
            {
                if (screen.WorkingArea.Contains(point))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            // Op sommige Linux window managers kan dit falen
            return false;
        }
    }
}