using System.Drawing;
using System.Text.Json.Serialization;

namespace CryptoScanBot.Core.Settings;

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

[Serializable]
public class SettingsUser
{
    public FormDimensions MainForm { get; set; } = new();
    public FormDimensions ChartForm { get; set; } = new();

    // Saved custom colors
    public int[] CustomColors { get; set; } = [];

    // Visible columns in each grid
    public SortedList<string, ColumnSetting> GridColumnsSignal { get; set; } = [];
    public SortedList<string, ColumnSetting> GridColumnsSymbol { get; set; } = [];
    public SortedList<string, ColumnSetting> GridColumnsPositionsOpen { get; set; } = [];
    public SortedList<string, ColumnSetting> GridColumnsPositionsClosed { get; set; } = [];

}

