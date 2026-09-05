using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Reflection;

namespace CryptoScanner.ViewModels;

public partial class BaseConvertersViewModel : ObservableObject
{
    // Read on every use instead of captured in the constructor. A row that was built before the
    // theme from the settings had been applied used to freeze the brushes it saw at that moment and
    // kept them for the rest of the session, so the grids that load their rows at startup drifted
    // away from the symbol grid and the information dashboard (which reads App.PriceUp/PriceDown per
    // update). The cells still cache the brush per row, but only from their first render - by then
    // the theme is settled.
    internal IBrush BrushGreen => App.PriceUp;
    internal IBrush BrushRed => App.PriceDown;
    internal IBrush BrushNeutral => App.PriceNeutral;


    /// <summary>
    /// Drop every cached brush of this row so the next render works them out again. Used when the
    /// theme changes (green and red come from the theme) and when the settings change (the volume
    /// boundary, the RSI and stochastic levels and the quote/strategy colours all decide a colour).
    /// <para>
    /// The fields are collected by reflection on purpose. Naming them one by one is what went wrong
    /// before: a reset that listed the symbol background but not the volume colour left that column
    /// showing the previous boundary until the row was rebuilt for another reason. A row holds up to
    /// thirty of these, and every new coloured column would have to be added to the list by hand.
    /// </para>
    /// </summary>
    public void ResetCachedBrushes()
    {
        foreach (var field in GetBrushFields(GetType()))
            field.SetValue(this, null);

        // One notification for the entire row: a null property name tells the bindings to re-read
        // everything, which is both cheaper and safer than naming thirty properties.
        OnPropertyChanged((string?)null);
    }


    /// <summary>
    /// The IBrush backing fields of a row type, including the ones its base types declare. Worked
    /// out once per type: reflection is fine for an occasional theme or settings change, but not
    /// per row.
    /// </summary>
    private static FieldInfo[] GetBrushFields(Type type)
    {
        lock (BrushFieldsPerType)
        {
            if (BrushFieldsPerType.TryGetValue(type, out var cached))
                return cached;

            List<FieldInfo> fields = [];
            for (Type? walk = type; walk != null && walk != typeof(ObservableObject); walk = walk.BaseType)
            {
                // DeclaredOnly: private fields of a base type are invisible to a lookup on the
                // derived type, so the hierarchy is walked instead.
                fields.AddRange(walk
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(x => typeof(IBrush).IsAssignableFrom(x.FieldType) && !x.IsInitOnly));
            }

            var result = fields.ToArray();
            BrushFieldsPerType.Add(type, result);
            return result;
        }
    }

    private static readonly Dictionary<Type, FieldInfo[]> BrushFieldsPerType = [];

    /// <summary>
    /// The colour of the market label beside a symbol (see CryptoSymbol.MarketLabel). The hex
    /// values live in CryptoProduct.ColorOf, one table shared with the Photino side, so the two
    /// UIs can never show the same product in different colours.
    /// </summary>
    internal static IBrush GetBrushColorMarketLabel(string label)
    {
        return new SolidColorBrush(Color.Parse(CryptoProduct.ColorOf(label)));
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

    //internal static string GetSignalStatusText(CryptoSignalStatus status)
    //{
    //    switch (status)
    //    {
    //        case CryptoSignalStatus.Lost:
    //            return "lost";
    //        case CryptoSignalStatus.Win:
    //            return "win";
    //        case CryptoSignalStatus.Run:
    //            return "run";
    //    }
    //    return "";
    //}

    //internal IBrush GetSignalStatusColor(CryptoSignalStatus status)
    //{
    //    switch (status)
    //    {
    //        case CryptoSignalStatus.Lost:
    //            return BrushRed;
    //        case CryptoSignalStatus.Win:
    //            return BrushGreen;
    //        case CryptoSignalStatus.Run:
    //            return BrushNeutral;
    //        default:
    //            break;
    //    }
    //    return BrushNeutral;
    //}

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