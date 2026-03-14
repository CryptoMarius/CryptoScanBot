using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.ViewModels;

public partial class BaseConvertersViewModel : ObservableObject
{
    internal readonly IBrush BrushGreen;
    internal readonly IBrush BrushRed;
    internal readonly IBrush BrushNeutral;

    public BaseConvertersViewModel()
    {
        BrushGreen = App.GetBrushResource("PriceUpBrush");
        BrushRed = App.GetBrushResource("PriceDownBrush");
        BrushNeutral = App.GetBrushResource("PriceNeutralBrush");
    }

    internal IBrush GetBrushColorViaSign(double value)
    {
        if (value < 0)
            return BrushRed;
        if (value > 0)
            return BrushGreen;
        return BrushNeutral;
    }

    internal IBrush GetBrushColorViaSign(double? value)
    {
        if (value == null)
            return BrushNeutral;
        if (value < 0)
            return BrushRed;
        if (value > 0)
            return BrushGreen;
        return BrushNeutral;
    }

    internal IBrush GetBrushColorViaSign(decimal? value)
    {
        if (value == null)
            return BrushNeutral;
        if (value < 0)
            return BrushRed;
        if (value > 0)
            return BrushGreen;
        return BrushNeutral;
    }


    internal IBrush GetBrushColorSide(CryptoTradeSide value)
    {
        if (value == CryptoTradeSide.Short)
            return BrushRed;
        else
            return BrushGreen;
    }


    // TODO: 3 equal methods, but name is a thing..
    internal IBrush GetBrushColorPSar(CryptoTradeSide side, double? psar, double? sma20)
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

    internal IBrush GetBrushColorSma50(CryptoTradeSide side, double? sma50, double? sma200)
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

    internal IBrush GetBrushColorSma20(CryptoTradeSide side, double? sma20, double? sma50)
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

    internal IBrush GetBrushColorRsi(double? rsi)
    {
        if (rsi < GlobalData.Settings.General.SettingsRsi.Oversold)
            return BrushGreen;
        else if (rsi > GlobalData.Settings.General.SettingsRsi.Overbought)
            return BrushRed;
        else
            return BrushNeutral;
    }

    internal IBrush GetBrushColorStoch(double? stochValue)
    {
        if (stochValue < GlobalData.Settings.General.SettingsStoch.Oversold)
            return BrushGreen;
        else if (stochValue > GlobalData.Settings.General.SettingsStoch.Overbought)
            return BrushRed;
        else
            return BrushNeutral;
    }

    internal IBrush GetBrushColorTrend(CryptoTrendIndicator? trend)
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

    internal IBrush GetSignalStatusColor(CryptoSignalStatus status)
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

    internal IBrush GetPositionStatusColor(CryptoPositionStatus status)
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

    internal IBrush GetVolumeColor(CryptoSymbol symbol, double volume)
    {
        if (volume <= 0)
            return BrushNeutral;
        else if (volume < (double)symbol.QuoteData.MinimalVolume)
            return BrushRed;
        else
            return BrushGreen;
    }

}