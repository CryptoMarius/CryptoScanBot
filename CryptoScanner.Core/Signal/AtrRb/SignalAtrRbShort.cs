using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.AtrRb;

/// <summary>
/// "atrrb" algorithm — fires a (short) alert when price hits the macro upper band of the
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
    private decimal? _slPrice;

    //public override decimal? OverrideSignalPrice => _entryPrice;
    //public override decimal? OverrideSlPrice => _slPrice;

    public override bool IsSignal()
    {
        ExtraText = "";
        _entryPrice = null;
        _slPrice = null;

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

        // Stop-loss: the same percentage (from the label) above the entry.
        _slPrice = _entryPrice * (1m + (decimal)pctDeviation / 100m);

        ExtraText = $"AtrRb upper band hit {pctDeviation:N2}%";
        return true;
    }
}
