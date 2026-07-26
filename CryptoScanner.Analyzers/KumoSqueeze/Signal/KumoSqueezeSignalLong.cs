using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.KumoSqueeze.Signal;

public class KumoSqueezeSignalLong : KumoSqueezeSignalBase
{
    public override bool IsSignal()
    {
        ExtraText = "";
        var settings = KumoSqueezePlugin.Settings;

        // 1. BB must be expanding now (current candle wider than the squeeze threshold)
        if (CandleLast.CandleData!.BollingerBandsPercentage <= settings.BBSqueezeMaxPercentage)
        {
            ExtraText = $"BB not expanding {CandleLast.CandleData.BollingerBandsPercentage:N2}%";
            return false;
        }

        // 2. The previous N candles must have been squeezed (narrow BB)
        if (!WasSqueezed(settings.SqueezeMinCandles, settings.BBSqueezeMaxPercentage))
        {
            ExtraText = "No prior squeeze detected";
            return false;
        }

        // 3. Price closes above the upper Bollinger Band
        if (!CandleLast.IsAboveBollingerBands(useLowHigh: false))
        {
            ExtraText = "Close not above upper BB";
            return false;
        }

        // 4. Volume spike: volume > multiplier × SMA(volume, length)
        if (!IsVolumeSpike(settings.VolumeMultiplier, settings.VolumeSmaLength))
        {
            ExtraText = "Volume too low";
            return false;
        }

        // 5. Compute Ichimoku cloud
        var cloud = GetIchimokuCloud(settings.TenkanPeriod, settings.KijunPeriod, settings.SenkouBPeriod);
        if (cloud == null)
        {
            ExtraText = "Insufficient Ichimoku data";
            return false;
        }

        // 6. Price must be above the cloud (close > max(Senkou A, Senkou B))
        decimal cloudTop = Math.Max((decimal)cloud.SenkouSpanA!, (decimal)cloud.SenkouSpanB!);
        if (CandleLast.Candle.Close <= cloudTop)
        {
            ExtraText = $"Close {CandleLast.Candle.Close} not above Kumo top {cloudTop:N8}";
            return false;
        }

        // 7. Cloud is bullish or neutral (Senkou A >= Senkou B)
        if (cloud.SenkouSpanA < cloud.SenkouSpanB)
        {
            ExtraText = "Cloud is bearish (Span A < Span B)";
            return false;
        }

        // 8. Optional: RSI > 50
        if (settings.UseRsiFilter && CandleLast.CandleData.Rsi <= 50)
        {
            ExtraText = $"RSI {CandleLast.CandleData.Rsi:N2} not above 50";
            return false;
        }

        // 9. Optional: Tenkan-sen > Kijun-sen
        if (settings.UseTenkanKijunFilter)
        {
            if (cloud.TenkanSen == null || cloud.KijunSen == null)
            {
                ExtraText = "Tenkan/Kijun not available";
                return false;
            }
            if (cloud.TenkanSen <= cloud.KijunSen)
            {
                ExtraText = $"Tenkan {cloud.TenkanSen:N8} not above Kijun {cloud.KijunSen:N8}";
                return false;
            }
        }

        ExtraText = $"Kumo squeeze breakout LONG (bb={CandleLast.CandleData.BollingerBandsPercentage:N2}%, " +
                    $"rsi={CandleLast.CandleData.Rsi:N2}, cloud.top={cloudTop:N8})";
        return true;
    }


    public override bool AdditionalChecks(MyData data, out string response)
    {
        response = "";
        return true;
    }
}
