using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.BbSqueeze.Signal;

public class SignalBbSqueezeLong : SignalBbSqueezeBase
{
    public override bool IsSignal()
    {
        ExtraText = "";
        var settings = BbSqueezePlugin.Settings;

        // BB must be expanding now (current candle wider than the squeeze threshold)
        if (CandleLast.CandleData!.BollingerBandsPercentage <= settings.BBSqueezeMaxPercentage)
        {
            ExtraText = $"BB not expanding {CandleLast.CandleData.BollingerBandsPercentage:N2}%";
            return false;
        }

        // The previous N candles must have been squeezed (narrow BB)
        if (!WasSqueezed(settings.SqueezeMinCandles, settings.BBSqueezeMaxPercentage))
        {
            ExtraText = "No prior squeeze detected";
            return false;
        }

        // MACD histogram must be rising (bullish momentum confirmation)
        if (!IsMacdHistogramRising(settings.MacdConfirmCandles))
        {
            ExtraText = "MACD histogram not rising";
            return false;
        }

        // MACD histogram should be positive (above zero line)
        if (CandleLast.CandleData.MacdHistogram <= 0)
        {
            ExtraText = "MACD histogram not positive";
            return false;
        }

        ExtraText = $"BB squeeze breakout (bb={CandleLast.CandleData.BollingerBandsPercentage:N2}%, macd.h={CandleLast.CandleData.MacdHistogram:N8})";
        return true;
    }


    public override bool AdditionalChecks(MyData data, out string response)
    {
        response = "";
        return true;
    }
}
