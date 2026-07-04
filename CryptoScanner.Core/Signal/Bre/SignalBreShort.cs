using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Signal.Bre;

/// <summary>
/// "bre" algorithm — fires a (short) alert when the High breaks the macro upper band of the
/// Buddy Reversion Engine construction and all enabled filters (trend/RSI/stoch-RSI) agree,
/// i.e. the exact moment the chart prints its upper-band percentage label.
/// The reported percentage matches the chart label.
///
/// Entry placement (same convention as the atrrb signal):
///   - wick only touches the band  -> entry on the band
///   - body breaks through the band -> entry on the close
/// Stop-loss: the band-width percentage shown in the label, placed above the entry.
/// </summary>
public class SignalBreShort : SignalCreateBase
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

        var settings = GlobalData.Settings.Signal.Bre;
        if (!BreBandsHelper.IsUpperBandBreak(SymbolInterval, CandleLast.Candle.OpenTime, out double bandWidthPct, out double upperBand, out string reason))
        {
            ExtraText = reason;
            return false;
        }

        var candle = CandleLast.Candle;
        decimal band = (decimal)upperBand;

        // Wick only touches the band -> entry on the band.
        // Body breaks through the band (body high above the band) -> entry on the close.
        decimal bodyHigh = Math.Max(candle.Open, candle.Close);
        _entryPrice = bodyHigh > band ? candle.Close : band;

        // Stop-loss: the band-width percentage (from the label) above the entry.
        // Only hand it to the trader when enabled; otherwise leave null so the trader
        // falls back to its default percentage stop-loss.
        if (settings.UseStopLoss)
            _slPercentage = (decimal)bandWidthPct;

        ExtraText = $"hit upper band {bandWidthPct:N2}%";
        return true;
    }
}
