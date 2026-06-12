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
    private decimal? _slPrice;

    public override decimal? OverrideSignalPrice => _entryPrice;
    public override decimal? OverrideSlPrice => _slPrice;

    public override bool IsSignal()
    {
        ExtraText = "";
        _entryPrice = null;
        _slPrice = null;

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

        // Wick only touches the band -> entry on the band.
        // Body breaks through the band (body low below the band) -> entry on the close.
        decimal bodyLow = Math.Min(candle.Open, candle.Close);
        _entryPrice = bodyLow < band ? candle.Close : band;

        // Stop-loss: the same percentage (from the label) below the entry.
        // Only hand it to the trader when enabled; otherwise leave null so the trader
        // falls back to its default percentage stop-loss.
        if (GlobalData.Settings.Signal.AtrRb.UseStopLoss)
            _slPrice = _entryPrice * (1m - (decimal)pctDeviation / 100m);

        ExtraText = $"hit lower band {pctDeviation:N2}%";
        return true;
    }
}
