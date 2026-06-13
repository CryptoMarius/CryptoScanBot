using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.AtrRb;

/// <summary>
/// "settings" algorithm — fires a (long) alert when price hits the macro lower band of the
/// AtrRb Bands construction, i.e. the exact moment the chart prints its lower-band percentage
/// label. The reported percentage matches the chart label.
///
/// Entry placement:
///   - wick only touches the band  -> entry on the band
///   - body breaks through the band -> entry on the close
/// Stop-loss: the same percentage shown in the label, placed below the entry.
/// </summary>
public class SignalAtrRbLong : SignalCreateBase
{
    private decimal? _entryPrice;
    private decimal? _slPercentage;

    public override decimal? OverrideSignalPrice => _entryPrice;
    public override decimal? OverrideSlPercentage => _slPercentage;

    public override bool IsSignal()
    {
        ExtraText = "";
        _entryPrice = null;
        _slPercentage = null;

        var settings = GlobalData.Settings.Signal.AtrRb;
        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width out of range {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        if (!AtrRbBandsHelper.IsLowerBandBreak(SymbolInterval, CandleLast.Candle.OpenTime, out double pctDeviation, out double lowerBand))
        {
            ExtraText = "no lower band break";
            return false;
        }

        var candle = CandleLast.Candle;
        decimal band = (decimal)lowerBand;

        // Entry placement (band is this break candle's lower band):
        //   - only the wick (low) pierced the band, close still above it -> entry on the band
        //   - the close itself broke below the band                      -> entry on the close
        _entryPrice = candle.Close < band ? candle.Close : band;

        // Stop-loss: the percentage shown in the label (factor * ATR%), handed to the trader as the
        // SL distance from the entry. Only when enabled; otherwise leave null so the trader falls back
        // to its default percentage stop-loss.
        if (settings.UseStopLoss)
            _slPercentage = (decimal)pctDeviation;

        ExtraText = $"hit lower band {pctDeviation:N2}%";
        return true;
    }
}
