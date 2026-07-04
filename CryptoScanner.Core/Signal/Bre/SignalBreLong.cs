using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Signal.Bre;

/// <summary>
/// "bre" algorithm — fires a (long) alert when the Low breaks the macro lower band of the
/// Buddy Reversion Engine construction and all enabled filters (trend/RSI/stoch-RSI) agree,
/// i.e. the exact moment the chart prints its lower-band percentage label.
/// The reported percentage matches the chart label.
///
/// Entry placement (same convention as the atrrb signal):
///   - wick only touches the band  -> entry on the band
///   - body breaks through the band -> entry on the close
/// Stop-loss: the band-width percentage shown in the label, placed below the entry.
/// </summary>
public class SignalBreLong : SignalCreateBase
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
        if (!BreBandsHelper.IsLowerBandBreak(SymbolInterval, CandleLast.Candle.OpenTime, out double bandWidthPct, out double lowerBand, out string reason))
        {
            ExtraText = reason;
            return false;
        }

        var candle = CandleLast.Candle;
        decimal band = (decimal)lowerBand;

        // Wick only touches the band -> entry on the band.
        // Body breaks through the band (body low below the band) -> entry on the close.
        decimal bodyLow = Math.Min(candle.Open, candle.Close);
        _entryPrice = bodyLow < band ? candle.Close : band;

        // Stop-loss: the band-width percentage (from the label) below the entry.
        // Only hand it to the trader when enabled; otherwise leave null so the trader
        // falls back to its default percentage stop-loss.
        if (settings.UseStopLoss)
            _slPercentage = (decimal)bandWidthPct;

        ExtraText = $"hit lower band {bandWidthPct:N2}%";
        return true;
    }
}
