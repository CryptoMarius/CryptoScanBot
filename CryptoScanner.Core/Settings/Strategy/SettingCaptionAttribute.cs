namespace CryptoScanner.Core.Settings.Strategy;

/// <summary>
/// The caption a plugin setting carries on screen, plus the explanation behind it.
/// <para>
/// The Avalonia configuration window renders a plugin supplied control per analyzer, with the
/// captions written out in its axaml. The Blazor hosts cannot render that control and fall back to
/// the property name, which put "BBMin Percentage" and "Require Rsi Os Ob" on screen instead of
/// "BB width min %" and "Require RSI oversold/overbought". Putting the caption on the property
/// keeps it in one place for every host, right next to the value it describes.
/// </para>
/// <para>
/// Without this attribute the property name is split on capitals, so annotating a settings class is
/// optional — an unannotated plugin keeps working exactly as before.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingCaptionAttribute(string caption) : Attribute
{
    /// <summary>Caption as the Avalonia view shows it, without the trailing colon.</summary>
    public string Caption { get; } = caption;

    /// <summary>The ToolTip.Tip of the matching Avalonia control, shown as a hover text.</summary>
    public string? Tooltip { get; init; }

    /// <summary>Draw a divider above this setting, where the Avalonia view has a Separator.</summary>
    public bool SeparatorBefore { get; init; }

    /// <summary>
    /// Bold sub-heading above this setting, for the blocks a single groupbox is divided into
    /// ("Bollinger Squeeze", "Ichimoku Cloud", "Optional Filters" in the KumoSqueeze view). Drawn
    /// under <see cref="SeparatorBefore"/> when both are set, in that order.
    /// </summary>
    public string? SubHeader { get; init; }

    /// <summary>
    /// Indent this setting, for a value that belongs to the switch above it — the Avalonia views
    /// give those a Margin="20,0,0,0".
    /// </summary>
    public bool Indented { get; init; }

    /// <summary>
    /// Name of a bool property in the same settings class that has to be on for this setting to be
    /// shown, mirroring an IsVisible binding in the Avalonia view.
    /// </summary>
    public string? VisibleWhen { get; init; }

    /// <summary>
    /// Header of the groupbox this setting belongs to. A strategy tab that splits its settings over
    /// several groupboxes (DLZ has five) names them here; everything without a group ends up in the
    /// single "Settings" box. The groups appear in the order their first setting is declared, so
    /// the declaration order in the settings class is the order on screen.
    /// </summary>
    public string? Group { get; init; }

    /// <summary>Text after the field, e.g. the "(1h candles)" behind a candle count.</summary>
    public string? Unit { get; init; }

    /// <summary>
    /// Name of another property in the same settings class this setting shares its row with. The
    /// control is drawn straight after that one, without a caption of its own — a min/max pair
    /// behind one "Filter on BB%:" label, or the lookback box behind the long caption of the
    /// checkbox that governs it. When the owner is a checkbox, the control follows its enabled
    /// state, the way the Avalonia views bind IsEnabled to it.
    /// </summary>
    public string? SameRowAs { get; init; }

    /// <summary>
    /// Extra white space above this setting, for the deliberate gap the Avalonia views leave
    /// between two blocks (Spacing="20" between the SBM versions).
    /// </summary>
    public bool SpaceBefore { get; init; }

    /// <summary>
    /// Name of a bool property that has to be on for this setting to be editable — it stays on
    /// screen but greys out, mirroring an IsEnabled binding in the Avalonia view.
    /// </summary>
    public string? EnabledWhen { get; init; }

    /// <summary>
    /// Not shown at all, for a setting the Avalonia view leaves out. The value keeps loading and
    /// saving, the same as an <c>IsVisible="false"</c> control over there.
    /// </summary>
    public bool Hidden { get; init; }
}
