using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Helpers;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CryptoScanner.ViewModels;

/// <summary>
/// The colours and lines of one item of the Indicators menu, behind its gear. The Avalonia
/// counterpart of ChartStyleDialog in the Photino chart: both edit the same file
/// (ChartStyleSettings, CryptoScanBot-chart-styles.json) and both apply a change at once, so the
/// effect shows on the chart while picking. Until 07-11-2026 these settings sat on a tab of the
/// configuration window instead.
/// </summary>
public partial class ChartStyleDialogViewModel : ObservableObject
{
    public string Title { get; }

    public ObservableCollection<ChartStyleRowViewModel> Rows { get; } = [];

    /// <summary>Raised after every change, once the file has been written.</summary>
    public event Action? Changed;

    public ChartStyleDialogViewModel(string title, IEnumerable<ChartStyleSettings.ChartSeriesDefinition> definitions)
    {
        Title = title;

        ChartStyleSettings stored = ChartStyleSettings.Current;
        foreach (var definition in definitions)
        {
            ChartLineStyle style = stored.Styles.TryGetValue(definition.Key, out var s)
                ? s.Clone()
                : definition.Default.Clone();
            var row = new ChartStyleRowViewModel(definition, style);
            row.PropertyChanged += OnRowChanged;
            Rows.Add(row);
        }
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        Save();
        Changed?.Invoke();
    }

    /// <summary>
    /// Write the rows of this dialog into the stored styles and leave the other series as they are.
    /// Only what differs from the default is kept, so a later change to the built-in palette still
    /// reaches series the user never touched (the same rule as the Photino chart).
    /// </summary>
    private void Save()
    {
        var target = new ChartStyleSettings();
        foreach (var (key, style) in ChartStyleSettings.Current.Styles)
            target.Styles[key] = style.Clone();

        foreach (ChartStyleRowViewModel row in Rows)
        {
            ChartLineStyle style = row.ToStyle();
            ChartLineStyle defaultStyle = row.Definition.Default;
            if (style.Color != defaultStyle.Color
                || style.LineWidth != defaultStyle.LineWidth
                || style.LineStyle != defaultStyle.LineStyle
                || style.Dots != defaultStyle.Dots
                || style.Visible != defaultStyle.Visible)
            {
                target.Styles[row.Definition.Key] = style;
            }
            else
                target.Styles.Remove(row.Definition.Key);
        }

        target.Save();
    }

    [RelayCommand]
    private void ResetAll()
    {
        foreach (ChartStyleRowViewModel row in Rows)
            row.ResetCommand.Execute(null);
    }
}

/// <summary>One chart series in the editor. Moved here from the former Chart styles tab of the configuration window.</summary>
public partial class ChartStyleRowViewModel : ObservableObject
{
    public ChartStyleSettings.ChartSeriesDefinition Definition { get; }

    public string Label => Definition.Label;

    /// <summary>Only a series whose definition allows it gets the visibility checkbox.</summary>
    public bool CanHide => Definition.CanHide;

    /// <summary>A series that only takes a colour has no line width or line style to pick.</summary>
    public bool Dots { get; private set; }

    public bool IsLineEditable => !Dots;

    [ObservableProperty]
    private bool _visible;

    [ObservableProperty]
    private Color _color;

    /// <summary>Line width 1..4; decimal because NumericUpDown binds to decimal.</summary>
    [ObservableProperty]
    private decimal? _lineWidth;

    /// <summary>
    /// The line width as index of the combobox (0 = width 1). Since 07-11-2026 a combobox instead of
    /// the NumericUpDown: the Fluent spinner buttons take 68 pixels and left no room for the number.
    /// </summary>
    public int LineWidthIndex
    {
        get => Math.Clamp((int)(LineWidth ?? 1), 1, 4) - 1;
        set => LineWidth = Math.Clamp(value, 0, 3) + 1;
    }

    partial void OnLineWidthChanged(decimal? value) => OnPropertyChanged(nameof(LineWidthIndex));

    /// <summary>0 = solid, 1 = dotted, 2 = dashed, as index of the combobox.</summary>
    [ObservableProperty]
    private int _lineStyle;

    // The colour text as it was loaded. The defaults are written in several notations ("#RRGGBB",
    // lower case), so an untouched colour keeps its own text; otherwise every series would differ
    // from its default on save and be written to the file.
    private string _loadedColorText = "";
    private Color _loadedColor;

    public ChartStyleRowViewModel(ChartStyleSettings.ChartSeriesDefinition definition, ChartLineStyle style)
    {
        Definition = definition;
        Apply(style);
    }

    private void Apply(ChartLineStyle style)
    {
        CoreColor color = ColorTextHelper.Parse(style.Color, CoreColor.FromArgb(0xFF, 0x88, 0x88, 0x88));
        _loadedColorText = style.Color;
        _loadedColor = Color.FromArgb(color.A, color.R, color.G, color.B);
        Color = _loadedColor;
        LineWidth = Math.Clamp(style.LineWidth, 1, 4);
        LineStyle = Math.Clamp(style.LineStyle, 0, 2);
        Visible = style.Visible;
        Dots = style.Dots;
        OnPropertyChanged(nameof(Dots));
        OnPropertyChanged(nameof(IsLineEditable));
    }

    /// <summary>The edited values as a style, colour in the "#AARRGGBB" notation of the settings.</summary>
    public ChartLineStyle ToStyle() => new()
    {
        Color = Color == _loadedColor ? _loadedColorText : $"#{Color.A:X2}{Color.R:X2}{Color.G:X2}{Color.B:X2}",
        LineWidth = Math.Clamp((int)(LineWidth ?? 1), 1, 4),
        LineStyle = Math.Clamp(LineStyle, 0, 2),
        Dots = Dots,
        Visible = Visible,
    };

    /// <summary>Back to the built-in look of this series.</summary>
    [RelayCommand]
    private void Reset()
    {
        Apply(ChartStyleSettings.DefaultFor(Definition.Key).Clone());
    }
}
