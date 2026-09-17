using CryptoScanner.Core.Helpers;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using OxyPlot;

namespace CryptoScanner.Analyzers.Chart;

/// <summary>
/// The colours a user picked, translated for the OxyPlot chart.
/// <para>
/// The colour screen lives in the Photino window, but what it writes belongs to both charts: since
/// 17-09-2026 <see cref="ChartStyleSettings"/> sits in Core exactly so the Avalonia overlays can read
/// it as well. Before that a colour picked in one window meant nothing in the other, because these
/// overlays drew with their own hard-coded values.
/// </para>
/// <para>
/// Nothing is imposed: without a saved style the overlay keeps the colour, thickness and dash pattern
/// it already used. So a scanner where the user never opened the colour screen draws exactly what it
/// drew before.
/// </para>
/// </summary>
public static class ChartStyleOxy
{
    /// <summary>What the user picked for this series, or null when they never touched it.</summary>
    private static ChartLineStyle? Configured(string key) => ChartStyleSettings.Current.Get(key);

    /// <summary>The colour to draw with: the user's choice, otherwise <paramref name="fallback"/>.</summary>
    public static OxyColor ColorFor(string key, OxyColor fallback)
    {
        ChartLineStyle? style = Configured(key);
        if (style == null)
            return fallback;

        // The settings keep "#AARRGGBB" (a plain "#RRGGBB" reads as fully opaque), the same notation
        // the rest of the settings use, so the alpha the user picked comes along.
        CoreColor color = ColorTextHelper.Parse(style.Color, CoreColor.FromArgb(0xFF, 0x88, 0x88, 0x88));
        return OxyColor.FromArgb(color.A, color.R, color.G, color.B);
    }

    /// <summary>The line thickness: the user's choice, otherwise <paramref name="fallback"/>.</summary>
    public static double ThicknessFor(string key, double fallback)
    {
        ChartLineStyle? style = Configured(key);
        return style != null && style.LineWidth > 0 ? style.LineWidth : fallback;
    }

    /// <summary>
    /// The dash pattern: the user's choice, otherwise <paramref name="fallback"/>. The settings use
    /// the numbering of lightweight-charts (0 solid, 1 dotted, 2 dashed) because that is what the web
    /// chart needs; OxyPlot has its own enum, so the two are matched up here.
    /// </summary>
    public static LineStyle LineStyleFor(string key, LineStyle fallback)
    {
        ChartLineStyle? style = Configured(key);
        if (style == null)
            return fallback;

        return style.LineStyle switch
        {
            1 => LineStyle.Dot,
            2 => LineStyle.Dash,
            _ => LineStyle.Solid,
        };
    }
}
