#if DEBUG
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Experiment;

public class SignalRollingFft : SignalCreateBase
{

    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           )
            return false;

        return true;
    }



    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 0))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }



        // Aanmaken (eenmalig per coin, of gedeeld)
        var fftAnalyzer = new RollingFftAnalyzer(windowSize: 256, topComponents: 3);

        // todo: Can we reduce this amount
        var history = SymbolInterval.CandleList.Values.ToList();
        fftAnalyzer.Analyze(history);
        //var harmonics = fftAnalyzer.Analyze(History);
        //foreach (var h in harmonics)
        //{
        //    // PeriodInCandles bij 1h-candles: 24 = dagcyclus, 168 = weekcyclus
        //    Console.WriteLine($"Cyclus: {h.PeriodInCandles:F1} candles | " +
        //                      $"Amplitude: {h.Amplitude:F4} | " +
        //                      $"Fase: {h.PhaseRadians:F2} rad");
        //}

        // Score voor mean-reversion signaal (-1 = oversold, +1 = overbought tov cyclus)
        double score = fftAnalyzer.ComputeOscillationScore(history);
        //if (score > 0.7)
        //    Console.WriteLine("⚠️ Prijs aan bovenkant harmonische cyclus → mogelijke reversal");
        //else if (score < -0.7)
        //    Console.WriteLine("⚠️ Prijs aan onderkant harmonische cyclus → mogelijke bounce");

        ExtraText = $"FFT={score:N2}";
        if (score > 0.5 || score < -0.5)
            GlobalData.AddTextToLogTab($"{Symbol.Name} {Interval.Name} {SignalSide} score={score:N2}");

        if (SignalSide == CryptoTradeSide.Short)
        {
            if (score > +0.7)
            {
                return true;
            }
        }
        else
        {
            if (score < -0.7)
            {
                return true;
            }
        }

        return false;
    }

}
#endif
