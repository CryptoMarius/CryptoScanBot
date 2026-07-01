using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Baba;

/// <summary>
/// Mean Reversion Bands — long signal. Fires when price breaks the LOWER band (wick or close) while
/// RSI is oversold (confluence). Optionally suppressed while the coin is in a DOWN-slide (don't catch a
/// falling knife). Entry on the band, or on the close when the close itself broke through; stop-loss =
/// SLStdevFactor * vwStdev below the lower band.
/// </summary>
public class SignalBabaLong : SignalBabaBase
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

        var settings = GlobalData.Settings.Signal.Baba;

        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width out of range {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        //// Cooldown gate (cheapest): no new signal within CooldownBars candles of the last Baba signal.
        //if (InCooldown())
        //{
        //    ExtraText = "cooldown active";
        //    return false;
        //}

        // Cheap RSI confluence first (precomputed lookup): a buy needs oversold. The OB/OS levels come
        // from the general RSI settings (Indicators tab), so all strategies share the same thresholds.
        if (settings.UseRsiFilter)
        {
            double? rsi = CandleLast.CandleData?.Rsi;
            if (!rsi.HasValue || rsi.Value > GlobalData.Settings.General.SettingsRsi.Oversold)
            {
                ExtraText = $"rsi not oversold ({rsi:N0})";
                return false;
            }
        }

        //// The (rarer, more expensive) lower-band break.
        if (!CandleLast.CandleData!.BabaLower.HasValue)
            return false;
        double lowerBand = CandleLast.CandleData.BabaLower.Value;
        if ((double)CandleLast.Candle.Low >= lowerBand && (double)CandleLast.Candle.Close >= lowerBand)
        {
            ExtraText = "no lower band break";
            return false;
        }

        // Stop-loss: SLStdevFactor * vwStdev below the lower band.
        // SL price = lowerBand - SLStdevFactor * vwStdev; SL% = that distance as % of the band.
        if (CandleLast.CandleData.BabaVwStdev is not double vwStdev)
            return false;
        double slPrice = lowerBand - settings.SLStdevFactor * vwStdev;
        double pctDeviation = slPrice > 0 ? (lowerBand - slPrice) / lowerBand * 100.0 : 0;

        // Old ATR-based SL: factor * ATR(Length)% — replaced by vwStdev approach above.
        //if (CandleLast.CandleData.BabaAtrSl is not double atr)
        //    return false;
        //double pctDeviation = GlobalData.Settings.Signal.Baba.StopLossAtrFactor * (atr / (double)CandleLast.Candle.Close * 100);


        // Symmetric slide filter: don't go long into an ongoing efficient DOWN-slide.
        if (settings.UseSlideFilter)
        {
            BabaBandsHelper.ComputeSlide(SymbolInterval, CandleLast.Candle.OpenTime, out bool slidingDown, out _);
            if (slidingDown)
            {
                ExtraText = "suppressed: down-slide active";
                return false;
            }
        }

        // Optional DLZ/FVG/SMC zone confluence (settings checkboxes). Checked only after the rare band
        // break, so the zone lookup runs sparingly.
        if (!CheckEnabledZoneRejections(out string zoneInfo))
        {
            ExtraText = zoneInfo;
            return false;
        }

        var candle = CandleLast.Candle;
        decimal band = (decimal)lowerBand;

        // Entry = the most extreme of the Close and the band.
        _entryPrice = Math.Min(candle.Close, band);

        if (settings.UseStopLoss)
            _slPercentage = (decimal)pctDeviation;

        //MarkSignalFired();
        ExtraText = $"hit lower band {pctDeviation:N2}%{(zoneInfo != "" ? " @ " + zoneInfo : "")} {_entryPrice}";
        return true;
    }
}
