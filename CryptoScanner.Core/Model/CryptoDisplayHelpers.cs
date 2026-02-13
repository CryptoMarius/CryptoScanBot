using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Core.Model;

public static class CryptoDisplayHelpers
{
    // Shared brushes for performance
    // Lazy initialization brushes
    private static IBrush? _brushGreen;
    private static IBrush? _brushRed;
    private static IBrush? _brushNeutral;

    private static IBrush BrushGreen
    {
        get
        {
            if (_brushGreen == null)
            {
                if (Application.Current?.TryGetResource("PriceUpBrush", null, out var brush) == true && brush is IBrush b)
                    _brushGreen = b;
                else
                    _brushGreen = Brushes.Green; // Fallback
            }
            return _brushGreen;
        }
    }

    private static IBrush BrushRed
    {
        get
        {
            if (_brushRed == null)
            {
                if (Application.Current?.TryGetResource("PriceDownBrush", null, out var brush) == true && brush is IBrush b)
                    _brushRed = b;
                else
                    _brushRed = Brushes.Red; // Fallback
            }
            return _brushRed;
        }
    }

    private static IBrush BrushNeutral // foreground
    {
        //Avalonia theme resources:
        //-SystemControlForegroundBaseHighBrush - Primary text (hoogste contrast)
        //-SystemControlForegroundBaseMediumBrush - Secondary text
        //-SystemControlForegroundBaseLowBrush - Disabled text
        //-TextControlForeground - Text in controls
        get
        {
            if (_brushNeutral == null)
            {
                //// Try theme-aware foreground first (wrong color)
                //if (Application.Current?.TryFindResource("SystemControlForegroundBaseHighBrush",
                //    ThemeVariant.Default, out var brush) == true && brush is ISolidColorBrush b)
                //{
                //    _brushNeutral = b;
                //}
                //// Fallback to actual white for dark themes
                //else 

                // if everything fails and is unreadable..

                if (Application.Current?.ActualThemeVariant == ThemeVariant.Dark)
                {
                    _brushNeutral = Brushes.White;
                }
                else
                {
                    _brushNeutral = Brushes.Black;
                }
            }
            return _brushNeutral;
        }
    }



    internal static IBrush GetBrushColorViaSign(double value)
    {
        if (value < 0)
            return BrushRed;
        if (value > 0)
            return BrushGreen;
        return BrushNeutral;
    }

    internal static IBrush GetBrushColorViaSign(double? value)
    {
        if (value == null)
            return BrushNeutral;
        if (value < 0)
            return BrushRed;
        if (value > 0)
            return BrushGreen;
        return BrushNeutral;
    }

    internal static IBrush GetBrushColorViaSign(decimal? value)
    {
        if (value == null)
            return BrushNeutral;
        if (value < 0)
            return BrushRed;
        if (value > 0)
            return BrushGreen;
        return BrushNeutral;
    }


    internal static IBrush GetBrushColorSide(CryptoTradeSide value)
    {
        if (value == CryptoTradeSide.Short)
            return BrushRed;
        else
            return BrushGreen;
    }


    // TODO: 3 equal methods, but name is a thing..
    internal static IBrush GetBrushColorPSar(CryptoTradeSide side, double? psar, double? sma20)
    {
        if (side == CryptoTradeSide.Long)
        {
            if (psar <= sma20)
                return BrushGreen;
            else if (psar > sma20)
                return BrushRed;
        }
        else if (side == CryptoTradeSide.Short)
        {
            if (psar >= sma20)
                return BrushGreen;
            else if (psar < sma20)
                return BrushRed;
        }
        return BrushNeutral;
    }

    internal static IBrush GetBrushColorSma50(CryptoTradeSide side, double? sma50, double? sma200)
    {
        if (side == CryptoTradeSide.Long)
        {
            if (sma50 <= sma200)
                return BrushGreen;
            else if (sma50 > sma200)
                return BrushRed;
        }
        else if (side == CryptoTradeSide.Short)
        {
            if (sma50 >= sma200)
                return BrushGreen;
            else if (sma50 < sma200)
                return BrushRed;
        }
        return BrushNeutral;
    }

    internal static IBrush GetBrushColorSma20(CryptoTradeSide side, double? sma20, double? sma50)
    {
        if (side == CryptoTradeSide.Long)
        {
            if (sma20 <= sma50)
                return BrushGreen;
            else if (sma20 > sma50)
                return BrushRed;
        }
        else if (side == CryptoTradeSide.Short)
        {
            if (sma20 >= sma50)
                return BrushGreen;
            else if (sma20 < sma50)
                return BrushRed;
        }
        return BrushNeutral;
    }

    internal static IBrush GetBrushColorRsi(double? rsi)
    {
        if (rsi < GlobalData.Settings.General.SettingsRsi.Oversold)
            return BrushGreen;
        else if (rsi > GlobalData.Settings.General.SettingsRsi.Overbought)
            return BrushRed;
        else
            return BrushNeutral;
    }

    internal static IBrush GetBrushColorStoch(double? stochValue)
    {
        if (stochValue < GlobalData.Settings.General.SettingsStoch.Oversold)
            return BrushGreen;
        else if (stochValue > GlobalData.Settings.General.SettingsStoch.Overbought)
            return BrushRed;
        else
            return BrushNeutral;
    }

    internal static IBrush GetBrushColorTrend(CryptoTrendIndicator? trend)
    {
        if (trend != null)
        {
            switch (trend)
            {
                case CryptoTrendIndicator.Unknown:
                    return BrushNeutral;
                case CryptoTrendIndicator.Bullish:
                    return BrushGreen;
                case CryptoTrendIndicator.Bearish:
                    return BrushRed;
            }
        }
        return BrushNeutral;
    }

    internal static string GetSignalStatusText(CryptoSignalStatus status)
    {
        switch (status)
        {
            case CryptoSignalStatus.Lost:
                return "lost";
            case CryptoSignalStatus.Win:
                return "win";
            case CryptoSignalStatus.Run:
                return "run";
        }
        return "";
    }

    internal static IBrush GetSignalStatusColor(CryptoSignalStatus status)
    {
        switch (status)
        {
            case CryptoSignalStatus.Lost:
                return BrushRed;
            case CryptoSignalStatus.Win:
                return BrushGreen;
            case CryptoSignalStatus.Run:
                return BrushNeutral;
            default:
                break;
        }
        return BrushNeutral;
    }

    internal static IBrush GetPositionStatusColor(CryptoPositionStatus status)
    {
        switch (status)
        {
            case CryptoPositionStatus.Waiting:
                break;
            case CryptoPositionStatus.Trading:
                break;
            case CryptoPositionStatus.Ready:
                return BrushGreen;
            case CryptoPositionStatus.TakeOver:
                return BrushRed;
            case CryptoPositionStatus.Altrady:
                break;
            case CryptoPositionStatus.Timeout:
                return BrushRed;
        }
        return BrushNeutral;
    }

    internal static string GetLargeVolumeText(decimal number)
    {
        if (number >= 1_000_000_000) // Miljard
            return $"{number / 1_000_000_000:N2} B";

        if (number >= 1_000_000) // Miljoen
            return $"{number / 1_000_000:N2} M";

        if (number >= 1_000) // Duizend
            return $"{number / 1_000:N2} K";

        return $"{number:N2}";
    }

    internal static IBrush GetVolumeColor(CryptoSymbol symbol, decimal volume)
    {
        if (volume <= 0)
            return BrushNeutral;
        else if (volume < symbol.QuoteData.MinimalVolume)
            return BrushRed;
        else
            return BrushGreen;
    }


    internal static IBrush GetStrategyBackground(CryptoTradeSide side, CryptoSignalStrategy strategy)
    {
        if (GlobalData.StrategiesSettings.TryGetValue(strategy, out (SettingsSignalStrategyBase strategySettings, DateTime _) x))
        {
            if (side == CryptoTradeSide.Long)
                return new SolidColorBrush(x.strategySettings.ColorLong);
            else
                return new SolidColorBrush(x.strategySettings.ColorShort);
        }
        return Brushes.Transparent;
    }

}