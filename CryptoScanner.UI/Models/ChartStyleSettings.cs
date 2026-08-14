using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;

using System.Text.Json;

namespace CryptoScanner.UI.Models;

/// <summary>
/// Colour, thickness and dash pattern of one chart series.
/// </summary>
public class ChartLineStyle
{
    /// <summary>
    /// Colour as "#AARRGGBB", the same notation the rest of the settings use, so the shared
    /// ColorPickerCell can edit it and the transparency is part of the value.
    /// A plain "#RRGGBB" is accepted too and read as fully opaque.
    /// </summary>
    public string Color { get; set; } = "#FF888888";

    /// <summary>The colour as a CSS value the chart can draw with.</summary>
    public string ToCssColor()
    {
        var color = ColorTextHelper.Parse(Color, Core.Model.CoreColor.FromArgb(0xFF, 0x88, 0x88, 0x88));
        string alpha = (color.A / 255.0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        return $"rgba({color.R},{color.G},{color.B},{alpha})";
    }

    /// <summary>Line thickness in pixels (1..4).</summary>
    public int LineWidth { get; set; } = 1;

    /// <summary>0 = solid, 1 = dotted, 2 = dashed (lightweight-charts LineStyle).</summary>
    public int LineStyle { get; set; }

    /// <summary>Drawn as dots per point instead of a connected line (used by PSar).</summary>
    public bool Dots { get; set; }

    public ChartLineStyle Clone() => new()
    {
        Color = Color,
        LineWidth = LineWidth,
        LineStyle = LineStyle,
        Dots = Dots,
    };
}

/// <summary>
/// Per-series chart styling, kept in its own file so a user's personal colours survive a restart
/// and stay separate from the scanner configuration.
/// </summary>
public class ChartStyleSettings
{
    private const string FileName = "chart-styles.json";

    /// <summary>Keyed by the series key the chart uses ("bbUpper", "sma200", ...).</summary>
    public Dictionary<string, ChartLineStyle> Styles { get; set; } = [];

    private static ChartStyleSettings? _current;

    public static ChartStyleSettings Current
    {
        get
        {
            _current ??= Load();
            return _current;
        }
    }

    /// <summary>
    /// One stylable series. <paramref name="OverlayKey"/> ties it to the overlay checkbox on the
    /// chart toolbar, so the little configuration popup there can show exactly its own lines.
    /// <paramref name="Key"/> must match the overlay key produced in Chart.razor / plugin GetSeries.
    /// </summary>
    public sealed record ChartSeriesDefinition(
        string OverlayKey, string Group, string Key, string Label, ChartLineStyle Default);

    /// <summary>Every stylable series, in the order the settings screen shows them.</summary>
    public static readonly ChartSeriesDefinition[] Definitions =
    [
        new("bb", "Bollinger Bands", "bbUpper",  "Upper band",  new() { Color = "#FF2196F3" }),
        new("bb", "Bollinger Bands", "bbMiddle", "Middle band", new() { Color = "#FF2196F3", LineStyle = 2 }),
        new("bb", "Bollinger Bands", "bbLower",  "Lower band",  new() { Color = "#FF2196F3" }),

        new("sma200", "Moving averages", "sma200", "SMA 200", new() { Color = "#FFe53935", LineWidth = 2 }),
        new("sma50",  "Moving averages", "sma50",  "SMA 50",  new() { Color = "#FFff9800", LineWidth = 2 }),
        new("sma20",  "Moving averages", "sma20",  "SMA 20",  new() { Color = "#FF4caf50" }),

        new("kc", "Keltner Channel", "keltnerUpper",  "Upper band",  new() { Color = "#FFab47bc" }),
        new("kc", "Keltner Channel", "keltnerMiddle", "Middle band", new() { Color = "#FFab47bc", LineStyle = 2 }),
        new("kc", "Keltner Channel", "keltnerLower",  "Lower band",  new() { Color = "#FFab47bc" }),

        new("psar", "Parabolic SAR", "psar", "PSar dots", new() { Color = "#FFffeb3b", Dots = true }),

        new("nwe", "NWE (not repainting)", "nweUpper",  "Upper band",  new() { Color = "#FF9e9e9e" }),
        new("nwe", "NWE (not repainting)", "nweMiddle", "Middle band", new() { Color = "#FF757575", LineStyle = 2 }),
        new("nwe", "NWE (not repainting)", "nweLower",  "Lower band",  new() { Color = "#FF9e9e9e" }),

        new("nwe.r", "NWE (repainting)", "nweRepaintUpper",  "Upper band",  new() { Color = "#FF8d6e63", LineStyle = 2 }),
        new("nwe.r", "NWE (repainting)", "nweRepaintMiddle", "Middle band", new() { Color = "#FF6d4c41", LineStyle = 2 }),
        new("nwe.r", "NWE (repainting)", "nweRepaintLower",  "Lower band",  new() { Color = "#FF8d6e63", LineStyle = 2 }),

        new("atrrb", "ATR Reversal Bands", "atrRbUpper", "Upper band", new() { Color = "#FF90a4ae" }),
        new("atrrb", "ATR Reversal Bands", "atrRbLower", "Lower band", new() { Color = "#FF90a4ae" }),
        new("atrrb", "ATR Reversal Bands", "atrRbBasis", "Basis",      new() { Color = "#FF42a5f5", LineStyle = 2 }),

        new("vbs", "VBS Bands", "vbsUpper", "Upper band", new() { Color = "#FF26a69a" }),
        new("vbs", "VBS Bands", "vbsLower", "Lower band", new() { Color = "#FF26a69a" }),
        new("vbs", "VBS Bands", "vbsBasis", "Basis",      new() { Color = "#FF9e9e9e", LineStyle = 2 }),

        new("dbr", "DBR Bands", "dbrUpper", "Upper band", new() { Color = "#FFbdbdbd" }),
        new("dbr", "DBR Bands", "dbrLower", "Lower band", new() { Color = "#FFbdbdbd" }),

        new("bbma", "BBMA", "bbmaWma5High",  "WMA 5 high",  new() { Color = "#FFc62828" }),
        new("bbma", "BBMA", "bbmaWma10High", "WMA 10 high", new() { Color = "#FFc62828", LineStyle = 2 }),
        new("bbma", "BBMA", "bbmaWma5Low",   "WMA 5 low",   new() { Color = "#FF2e7d32" }),
        new("bbma", "BBMA", "bbmaWma10Low",  "WMA 10 low",  new() { Color = "#FF2e7d32", LineStyle = 2 }),
        new("bbma", "BBMA", "bbmaEma50",     "EMA 50",      new() { Color = "#FFef6c00", LineWidth = 2 }),

        new("zigzag",    "Trend", "zigzag",    "ZigZag",     new() { Color = "#FFffffff" }),
        new("fibZigzag", "Trend", "fibZigzag", "FIB ZigZag", new() { Color = "#FFffeb3b", LineStyle = 2 }),

        // Sub-panel indicators. Line width and style apply to the lines; the histogram bars
        // (Lux, MACD, volume) only take the colour.
        new("rsi", "RSI", "rsi",           "RSI",        new() { Color = "#FFab47bc" }),
        new("rsi", "RSI", "rsiOversold",   "Oversold",   new() { Color = "#6622c55e", LineStyle = 2 }),
        new("rsi", "RSI", "rsiOverbought", "Overbought", new() { Color = "#66f0616d", LineStyle = 2 }),

        new("stoch", "Stochastic", "stochK",          "%K",         new() { Color = "#FF2196F3" }),
        new("stoch", "Stochastic", "stochD",          "%D",         new() { Color = "#FFff9800" }),
        new("stoch", "Stochastic", "stochOversold",   "Oversold",   new() { Color = "#6622c55e", LineStyle = 2 }),
        new("stoch", "Stochastic", "stochOverbought", "Overbought", new() { Color = "#66f0616d", LineStyle = 2 }),

        new("lux", "Lux", "luxOversold",   "Oversold bars",   new() { Color = "#8c22c55e" }),
        new("lux", "Lux", "luxOverbought", "Overbought bars", new() { Color = "#8cf0616d" }),

        new("macd", "MACD", "macdLine",      "MACD",           new() { Color = "#FF2196F3" }),
        new("macd", "MACD", "macdSignal",    "Signal",         new() { Color = "#FFff9800" }),
        new("macd", "MACD", "macdHistUp",    "Histogram up",   new() { Color = "#9922c55e" }),
        new("macd", "MACD", "macdHistDown",  "Histogram down", new() { Color = "#99f0616d" }),

        new("volume", "Volume", "volumeUp",   "Rising candle",  new() { Color = "#8022c55e" }),
        new("volume", "Volume", "volumeDown", "Falling candle", new() { Color = "#80f0616d" }),
    ];

    /// <summary>
    /// The series belonging to one overlay checkbox. "sbm" draws the same three moving averages
    /// as the individual SMA overlays, so it reuses their definitions.
    /// </summary>
    public static IEnumerable<ChartSeriesDefinition> DefinitionsForOverlay(string overlayKey)
    {
        if (overlayKey == "sbm")
            return Definitions.Where(d => d.Group == "Moving averages");

        return Definitions.Where(d => d.OverlayKey == overlayKey);
    }

    /// <summary>Style for a series key, falling back to its built-in default.</summary>
    public ChartLineStyle Get(string key)
    {
        if (Styles.TryGetValue(key, out var style))
            return style;

        foreach (var definition in Definitions)
        {
            if (definition.Key == key)
                return definition.Default;
        }
        return new ChartLineStyle();
    }

    /// <summary>Built-in look of a series, ignoring anything the user changed.</summary>
    public static ChartLineStyle DefaultFor(string key)
    {
        foreach (var definition in Definitions)
        {
            if (definition.Key == key)
                return definition.Default;
        }
        return new ChartLineStyle();
    }

    /// <summary>Reset one series back to its built-in look.</summary>
    public void ResetToDefault(string key)
    {
        Styles.Remove(key);
    }

    public void ResetAllToDefault()
    {
        Styles.Clear();
    }

    private static string FullPath =>
        Path.Combine(GlobalData.AppDataFolder, $"{Constants.AppName}-{FileName}");

    public static ChartStyleSettings Load()
    {
        try
        {
            string path = FullPath;
            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize<ChartStyleSettings>(File.ReadAllText(path));
                if (loaded != null)
                    return loaded;
            }
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"Chart styles load error: {ex.Message}");
        }
        return new ChartStyleSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(GlobalData.AppDataFolder);
            string text = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FullPath, text);
            _current = this;
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"Chart styles save error: {ex.Message}");
        }
    }
}
