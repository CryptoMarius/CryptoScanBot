using CryptoScanner.Core.Enums;

using System.Drawing;
using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Settings;

[Serializable]
public class ColumnSetting
{
    public int Width { get; set; } = -1;
    public bool Visible { get; set; } = true;
    public bool AlwaysVisible { get; set; } = false;
}

[Serializable]
public class FormDimensions
{
    [JsonConverter(typeof(Json.RectangleConverter))]
    public Rectangle WindowPosition { get; set; } = new Rectangle();
    public int WindowState { get; set; } = 0;

    // mainform only
    public int SplitterDistance { get; set; } = 0;
}

// Columns and sort order per grid
[Serializable]
public class GridSettingsUser
{
    public int? SortColumn = null;
    public GridSortOrder? SortOrder = null;
    public SortedList<string, ColumnSetting> ColumnList { get; set; } = [];
}

[Serializable]
public class SettingsUser
{
    public FormDimensions MainForm { get; set; } = new();
    public FormDimensions ChartForm { get; set; } = new();

    // Saved custom colors
    public int[] CustomColors { get; set; } = [];

    public GridSettingsUser GridSignal { get; set; } = new();
    public SortedList<string, ColumnSetting>? GridColumnsSignal { get; set; } // only for migration
    
    public GridSettingsUser GridSymbol { get; set; } = new();
    public SortedList<string, ColumnSetting>? GridColumnsSymbol { get; set; } // only for migration

    public GridSettingsUser GridPositionsOpen { get; set; } = new();
    public SortedList<string, ColumnSetting>? GridColumnsPositionsOpen { get; set; } // only for migration

    public GridSettingsUser GridPositionsClosed { get; set; } = new();
    public SortedList<string, ColumnSetting>? GridColumnsPositionsClosed { get; set; } // only for migration

    public GridSettingsUser GridLiveData { get; set; } = new();
    public SortedList<string, ColumnSetting>? GridColumnsLiveData { get; set; } // only for migration
}

