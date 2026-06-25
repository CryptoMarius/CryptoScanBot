using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.AtrRb;

/// <summary>
/// "settings" algorithm — fires a (short) alert when price hits the macro upper band of the
/// AtrRb Bands construction, i.e. the exact moment the chart prints its upper-band percentage
/// label. The reported percentage matches the chart label.
///
/// Entry placement:
///   - wick only touches the band  -> entry on the band
///   - body breaks through the band -> entry on the close
/// Stop-loss: the same percentage shown in the label, placed above the entry.
/// </summary>
public class SignalAtrRbShort : SignalCreateBase
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

        if (!AtrRbBandsHelper.IsUpperBandBreak(SymbolInterval, CandleLast.Candle.OpenTime, out double pctDeviation, out double upperBand))
        {
            ExtraText = "no upper band break";
            return false;
        }

        var candle = CandleLast.Candle;
        decimal band = (decimal)upperBand;

        // Wick only touches the band -> entry on the band.
        // Body breaks through the band (body high above the band) -> entry on the close.
        decimal bodyHigh = Math.Max(candle.Open, candle.Close);
        _entryPrice = bodyHigh > band ? candle.Close : band;
        //_entryPrice = Math.Max(candle.Close, band);
        //var _entryPrice2 = Math.Max(candle.Close, band);
        //if (_entryPrice2 != _entryPrice)
        //    _entryPrice = _entryPrice2;

        // Stop-loss: the same percentage (from the label) above the entry.
        // Only hand it to the trader when enabled; otherwise leave null so the trader
        // falls back to its default percentage stop-loss.
        if (settings.UseStopLoss)
            _slPercentage = (decimal)pctDeviation;

        ExtraText = $"hit upper band{pctDeviation:N2}%";
        return true;
    }
}
