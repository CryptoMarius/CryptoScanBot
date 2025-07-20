using CryptoScanBot.Core.Model;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanBot.ZoneVisualisation.Chart;

public class NadarayaWatsonEnvelope
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, long minDate, long maxDate)
    {
        var seriesHigh = new LineSeries { Title = "n high", MarkerSize = 2, MarkerFill = OxyColors.White };
        var seriesLow = new LineSeries { Title = "n low", MarkerSize = 2, MarkerFill = OxyColors.White };
        var seriesBuy = new ScatterSeries { Title = "n buy", MarkerSize = 2, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Square, };
        var seriesSell = new ScatterSeries { Title = "n sell", MarkerSize = 2, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Square, };

        // configuration:
        double h = 8f;
        double mult = 3.0f;

        // Iterate the last 500 candles
        int maxlen = 500;
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;
        int n = symbolInterval.CandleList.Count;
        int max = Math.Min(maxlen, n - 1);
        //In Pine Script, wanneer je src[x] gebruikt en src = input.source(close) is:
        // dan verwijst x = 0 altijd naar de huidige(laatste beschikbare) candle in de chart context(dus de meest recente die op dat moment verwerkt wordt).
        // en x = 1 verwijst naar de vorige candle.
        long offsett = symbolInterval.CandleList.Values.Last().OpenTime; // - max * interval.Duration;

        List<decimal> nwe = [];
        decimal sae = 0;

        // Compute and set NWE points 
        for (int i = 0; i < max; i++)
        {
            // Compute weighted mean 
            decimal sum = 0;
            decimal sumw = 0;
            for (int j = 0; j < max; j++)
            {
                // Gaussian window
                decimal w = (decimal)Math.Exp(-(Math.Pow(i - j, 2)) / (h * h * 2));
                if (symbolInterval.CandleList.TryGetValue(offsett - j * interval.Duration, out CryptoCandle? candlej))
                    sum += candlej.Close * w;
                sumw += w;
            }
            decimal y2 = sum / sumw;
            nwe.Add(y2);

            if (symbolInterval.CandleList.TryGetValue(offsett - i * interval.Duration, out CryptoCandle? candlei))
                sae += Math.Abs(candlei.Close - y2);
        }
        sae = sae / max * (decimal)mult;


        for (int i = 0; i < max; i++)
        {
            if (symbolInterval.CandleList.TryGetValue(offsett - (i + 0) * interval.Duration, out CryptoCandle? candleLast) &&
                symbolInterval.CandleList.TryGetValue(offsett - (i + 1) * interval.Duration, out CryptoCandle? candlePrev))
            {
                decimal nwevalue = nwe[i];
                decimal upperband = nwevalue + sae;
                decimal lowerband = nwevalue - sae;

                seriesLow.Points.Add(new DataPoint(candleLast.OpenTime, (double)lowerband));
                seriesHigh.Points.Add(new DataPoint(candleLast.OpenTime, (double)upperband));

                // buy alert
                if (candlePrev!.Close > lowerband && candleLast.Close <= lowerband)
                {
                    nwevalue = candleLast.Low * 0.995m;
                    seriesBuy.Points.Add(new ScatterPoint(candleLast.OpenTime, (double)nwevalue));
                }

                // sell alert
                if (candlePrev!.Close < upperband && candleLast.Close >= upperband)
                {
                    nwevalue = candleLast.High * 1.005m;
                    seriesSell.Points.Add(new ScatterPoint(candleLast.OpenTime, (double)nwevalue));
                }
            }
        }

        chart.Series.Add(seriesLow);
        chart.Series.Add(seriesHigh);
        chart.Series.Add(seriesBuy);
        chart.Series.Add(seriesSell);
    }
}
