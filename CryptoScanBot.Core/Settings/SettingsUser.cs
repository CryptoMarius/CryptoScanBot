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
public class SettingsUser
{
    [JsonConverter(typeof(Intern.RectangleConverter))]
    public Rectangle WindowPosition { get; set; } = new Rectangle();
    public int WindowState { get; set; } = 0;


    public int[] CustomColors { get; set; } = [];

    // Welke kolommen zijn zichtbaar in de diverse grids
    public SortedList<string, ColumnSetting> GridColumnsSignal { get; set; } = [];
    public SortedList<string, ColumnSetting> GridColumnsSymbol { get; set; } = [];
    public SortedList<string, ColumnSetting> GridColumnsPositionsOpen { get; set; } = [];
    public SortedList<string, ColumnSetting> GridColumnsPositionsClosed { get; set; } = [];


    public SettingsUser()
    {
    }
}

