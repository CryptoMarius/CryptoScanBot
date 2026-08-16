using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.UI.ViewModels;

/// <summary>
/// CSS class helpers for grid color logic, replaces Avalonia BaseConvertersViewModel brushes.
/// Returns CSS class names: "text-green", "text-red", "text-neutral".
/// </summary>
public static class ColorHelper
{
    public const string Green = "text-green";
    public const string Red = "text-red";
    public const string Neutral = "text-neutral";

    public static string GetVolumeColorClass(CryptoSymbol symbol, double volume)
    {
        if (volume <= 0)
            return Neutral;
        else if (volume < (double)symbol.QuoteData.MinimalVolume)
            return Red;
        else
            return Green;
    }

    public static string GetColorClassViaSign(double value)
    {
        if (value < 0)
            return Red;
        if (value > 0)
            return Green;
        return Neutral;
    }

    public static string GetColorClassViaSign(double? value)
    {
        if (value == null)
            return Neutral;
        if (value < 0)
            return Red;
        if (value > 0)
            return Green;
        return Neutral;
    }

    public static string GetColorClassViaSign(decimal value)
    {
        if (value < 0)
            return Red;
        if (value > 0)
            return Green;
        return Neutral;
    }

    public static string GetColorClassViaSign(decimal? value)
    {
        if (value == null)
            return Neutral;
        if (value < 0)
            return Red;
        if (value > 0)
            return Green;
        return Neutral;
    }

    public static string GetColorClassSide(CryptoTradeSide value)
    {
        if (value == CryptoTradeSide.Short)
            return Red;
        else
            return Green;
    }

    public static string GetColorClassTrend(CryptoTrendIndicator? trend)
    {
        if (trend != null)
        {
            switch (trend)
            {
                case CryptoTrendIndicator.Unknown:
                    return Neutral;
                case CryptoTrendIndicator.Bullish:
                    return Green;
                case CryptoTrendIndicator.Bearish:
                    return Red;
            }
        }
        return Neutral;
    }

    public static string GetColorClassPositionStatus(CryptoPositionStatus status)
    {
        switch (status)
        {
            case CryptoPositionStatus.Ready:
                return Green;
            case CryptoPositionStatus.TakeOver:
            case CryptoPositionStatus.Timeout:
                return Red;
            default:
                return Neutral;
        }
    }

    public static string GetColorClassRsi(double? rsi)
    {
        if (rsi < GlobalData.Settings.General.SettingsRsi.Oversold)
            return Green;
        else if (rsi > GlobalData.Settings.General.SettingsRsi.Overbought)
            return Red;
        else
            return Neutral;
    }

    /// <summary>
    /// Colours the band-range index (see BandRangeTracker). Three buckets, no finer: the difference
    /// between 2.6 and 2.9 is inside the measurement noise. Above 3 was the level where a plain
    /// mean-reversion entry turned profitable in the measurement, under 2 it never did.
    /// </summary>
    public static string GetColorClassBandRangeIndex(double? index)
    {
        if (index == null)
            return "";
        if (index >= 3.0)
            return Green;
        if (index < 2.0)
            return Red;
        return "";
    }

    public static string GetColorClassStoch(double? stochValue)
    {
        if (stochValue < GlobalData.Settings.General.SettingsStoch.Oversold)
            return Green;
        else if (stochValue > GlobalData.Settings.General.SettingsStoch.Overbought)
            return Red;
        else
            return Neutral;
    }

    public static string GetLargeVolumeText(decimal number)
    {
        if (number >= 1_000_000_000)
            return $"{number / 1_000_000_000:N2} B";

        if (number >= 1_000_000)
            return $"{number / 1_000_000:N2} M";

        if (number >= 1_000)
            return $"{number / 1_000:N2} K";

        return $"{number:N2}";
    }

    /// <summary>
    /// Bumped whenever the basecoin colours change. Screens that CACHE a background style compare
    /// against this instead of computing one per row per repaint — the symbol grid kept showing the
    /// old colour because its cache was filled once and never refreshed, while the signal and
    /// live-data grids build the style on every render and did follow along.
    /// </summary>
    public static int QuoteColorVersion { get; private set; }

    public static void InvalidateQuoteColors() => QuoteColorVersion++;

    public static string GetBackgroundStyle(CryptoQuoteData quoteData)
    {
        var c = quoteData.DisplayColor;
        if (c.A == 0)
            return "";
        return $"background-color: rgba({c.R},{c.G},{c.B},{c.A / 255.0:F2})";
    }
}
