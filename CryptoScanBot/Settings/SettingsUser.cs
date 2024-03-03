using System.Text.Json.Serialization;

namespace CryptoScanBot.Settings;

[Serializable]
public class ColumnSetting
{
    public int Width { get; set; } = -1;
    public bool Visible { get; set; } = true;
}

[Serializable]
public class SettingsUser
{
    [JsonConverter(typeof(Intern.RectangleConverter))]
    public Rectangle WindowPosition { get; set; } = new Rectangle();
    public FormWindowState WindowState { get; set; } = FormWindowState.Normal;

    // Welke kolommen zijn zichtbaar in de diverse grids
    public SortedList<string, ColumnSetting> GridColumnsSignal { get; set; } = [];
    public SortedList<string, ColumnSetting> GridColumnsSymbol { get; set; } = [];
    public SortedList<string, ColumnSetting> GridColumnsPositionsOpen { get; set; } = [];
    public SortedList<string, ColumnSetting> GridColumnsPositionsClosed { get; set; } = [];


    public SettingsUser()
    {
    }
}

