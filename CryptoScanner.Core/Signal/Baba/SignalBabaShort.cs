using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Baba;

/// <summary>
/// Mean Reversion Bands — short signal. Fires when price breaks the UPPER band (wick or close) while
/// RSI is overbought (confluence). Optionally suppressed while the coin is in an UP-slide (don't short a
/// melt-up). Entry on the band, or on the close when the close itself broke through; stop-loss =
/// StopLossAtrFactor * ATR%.
/// </summary>
public class SignalBabaShort : SignalBabaBase
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

        //// Cooldown gate (cheapest): no new signal within CooldownBars candles of the last Baba signal.
        //if (InCooldown())
        //{
        //    ExtraText = "cooldown active";
        //    return false;
        //}

        // Cheap RSI confluence first (precomputed lookup): a sell needs overbought. The OB/OS levels come
        // from the general RSI settings (Indicators tab), so all strategies share the same thresholds.
        if (settings.UseRsiFilter)
        {
            double? rsi = CandleLast.CandleData?.Rsi;
            if (!rsi.HasValue || rsi.Value < GlobalData.Settings.General.SettingsRsi.Overbought)
            {
                ExtraText = $"rsi not overbought ({rsi:N0})";
                return false;
            }
        }

        //// The (rarer, more expensive) upper-band break.
        if (!CandleLast.CandleData!.BabaUpper.HasValue)
            return false;
        double upperBand = CandleLast.CandleData.BabaUpper.Value;
        if ((double)CandleLast.Candle.High <= upperBand && (double)CandleLast.Candle.Close <= upperBand)
        {
            ExtraText = "no upper band break";
            return false;
        }

        //Stoploss percentage
        if (CandleLast.CandleData.BabaAtrSl is not double atr)
            return false;
        double pctDeviation = GlobalData.Settings.Signal.Baba.StopLossAtrFactor * (atr / (double)CandleLast.Candle.Close * 100);


        // Symmetric slide filter: don't go short into an ongoing efficient UP-slide (melt-up).
        if (settings.UseSlideFilter)
        {
            BabaBandsHelper.ComputeSlide(SymbolInterval, CandleLast.Candle.OpenTime, out _, out bool slidingUp);
            if (slidingUp)
            {
                ExtraText = "suppressed: up-slide active";
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
        decimal band = (decimal)upperBand;

        // Entry = the most extreme (HIGHEST) of the wick (High), the Close and the band.
        //_entryPrice = Math.Max(candle.High, Math.Max(candle.Close, band));
        _entryPrice = Math.Max(candle.Close, band);

        if (settings.UseStopLoss)
            _slPercentage = (decimal)pctDeviation;

        //MarkSignalFired();
        ExtraText = $"hit upper band {pctDeviation:N2}%{(zoneInfo != "" ? " @ " + zoneInfo : "")}";
        return true;
    }
}
