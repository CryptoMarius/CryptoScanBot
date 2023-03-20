using CryptoSbmScanner.Model;
using Skender.Stock.Indicators;

namespace CryptoSbmScanner;


public class CryptoBbResult
{
    public DateTime Date { get; set; }
    public decimal? UpperBand { get; set; }
    public decimal? MiddleBand { get; set; }
    public decimal? LowerBand { get; set; }
    public decimal? Percentage { get; set; }
}

public class CryptoSmaResult
{
    public DateTime Date { get; set; }
    public decimal? Sma { get; set; }
}


public static class Indicators
{

    // STANDARD DEVIATION
    public static double StdDev(this decimal[] values)
    {
        // ref: https://stackoverflow.com/questions/2253874/standard-deviation-in-linq
        // and then modified to an iterative model without LINQ, for performance improvement

        if (values is null)
            throw new ArgumentNullException(nameof(values), "StdDev values cannot be null.");

        double deviation = 0;
        int n = values.Length;
        if (n > 1)
        {
            decimal sum = 0;
            for (int i = 0; i < n; i++)
                sum += values[i];

            decimal avg = sum / n;

            decimal sumSq = 0;
            for (int i = 0; i < n; i++)
            {
                decimal d = values[i];
                sumSq += (d - avg) * (d - avg);
            }

            deviation = Math.Sqrt((double)sumSq / n);
        }

        return deviation;
    }


    //public static IEnumerable<CryptoSmaResult> CalcSmaAnalysis(List<CryptoCandle> history, int lookbackPeriods)
    //{
    //    //// initialize
    //    List<CryptoSmaResult> results = new List<CryptoSmaResult>(history.Count);
    //    //    .CalcSma(lookbackPeriods)
    //    //    .Select(x => new SmaAnalysis(x.Date) { Sma = x.Sma })
    //    //    .ToList();

    //    // roll through quotes
    //    for (int i = lookbackPeriods - 1; i < history.Count; i++)
    //    {
    //        SmaAnalysis r = history[i];
    //        decimal sma = (r.Sma == null) ? double.NaN : (double)r.Sma;
    //        decimal sumMad = 0;
    //        decimal sumMse = 0;
    //        decimal sumMape = 0;

    //        for (int p = i + 1 - lookbackPeriods; p <= i; p++)
    //        {
    //            decimal value = history[p].Close;
    //            sumMad += Math.Abs(value - sma);
    //            sumMse += (value - sma) * (value - sma);
    //            sumMape += (value == 0) ? double.NaN : Math.Abs(value - sma) / value;
    //        }

    //        // ?
    //        // mean absolute deviation
    //        //r.Mad = (sumMad / lookbackPeriods).NaN2Null();

    //        // mean squared error
    //        //r.Mse = (sumMse / lookbackPeriods).NaN2Null();

    //        // mean absolute percent error
    //        //r.Mape = (sumMape / lookbackPeriods).NaN2Null();
    //    }

    //    return results;
    //}


    public static List<CryptoBbResult> GetBollingerBands(List<CryptoCandle> history, int lookbackPeriods, decimal standardDeviations)
    {
        // check parameter arguments
        if (lookbackPeriods <= 1)
            throw new ArgumentOutOfRangeException(nameof(lookbackPeriods), lookbackPeriods,
                "Lookback periods must be greater than 1 for Bollinger Bands.");

        if (standardDeviations <= 0)
            throw new ArgumentOutOfRangeException(nameof(standardDeviations), standardDeviations,
                "Standard Deviations must be greater than 0 for Bollinger Bands.");


        // initialize
        List<CryptoBbResult> results = new(history.Count);

        // roll through quotes
        for (int i = 0; i < history.Count; i++)
        {
            CryptoBbResult result = new()
            {
                Date = history[i].Date // overkill, maar voor debug prima
            };
            results.Add(result);

            if (i + 1 >= lookbackPeriods)
            {
                decimal[] window = new decimal[lookbackPeriods];
                decimal sum = 0;
                int n = 0;

                for (int p = i + 1 - lookbackPeriods; p <= i; p++)
                {
                    decimal value = history[p].Close;
                    window[n] = value;
                    sum += value;
                    n++;
                }

                decimal? windowAverage = sum / lookbackPeriods;
                decimal? standardDeviation = (decimal)window.StdDev(); //.NaN2Null();

                result.MiddleBand = windowAverage;
                result.UpperBand = windowAverage + (standardDeviations * standardDeviation);
                result.LowerBand = windowAverage - (standardDeviations * standardDeviation);
                if (result.UpperBand == result.LowerBand)
                    result.Percentage = 0;
                else
                    result.Percentage = (result.UpperBand / result.LowerBand) - 1;
            }
        }

        return results;
    }   


    /// <summary>
    /// ParabolicSar
    /// https://github.com/jasonlam604/StockTechnicals/blob/master/src/com/jasonlam604/stocktechnicals/indicators/ParabolicSar.java
    /// Deze doet het ietsjes anders dan die van Dave Skender, interessant!
    /// </summary>
    public static List<ParabolicSarResult> CalcParabolicSarJasonLam(List<CryptoCandle> history, decimal acceleration = 0.02m, decimal accelerationMax = 0.2m)
    {
        //* parabolicSars contains psar values
        //decimal[] parabolicSars;
        // The trends represents the trends where above 0 is positive and below zero is negative
        //bool[] trends;
        // The trendFlip when true indicates the trend change since last time
        //bool[] trendFlip;

        // initialize
        int length = history.Count - 1;
        //trends = new bool[length];
        //trendFlip = new bool[length];
        //parabolicSars = new decimal[length];
        List<ParabolicSarResult> results = new(length);

        bool isRising = (history[1].High >= history[0].High || history[0].Low <= history[1].Low); // ? true : false;
        decimal priorSar = (isRising) ? history[0].Low : history[0].High;
        decimal extremePoint = (isRising) ? history[0].High : history[0].Low;
        decimal accelerationFactor = 0;

        // Init first Parabolic Sar and Trend values
        //parabolicSars[1] = priorSar; // SAR Results
        //trends[1] = isRising; // Trend Directions

        ParabolicSarResult r = new(history[0].Date)
        {
            Sar = (double)priorSar,
            IsReversal = null
        };
        results.Add(r);

        // mhhh, dat is niet correct lijkt me
        r = new ParabolicSarResult(history[0].Date)
        {
            Sar = (double)priorSar,
            IsReversal = null
        };
        results.Add(r);


        for (int i = 1; i < length; i++)
        {
            CryptoCandle candle = history[i];

            decimal sar;

            // Up Trend if trend is bigger then 0 else it's a down trend
            if (isRising)
            {
                // Higher highs, accelerate
                if (candle.High > extremePoint)
                {
                    extremePoint = candle.High;
                    accelerationFactor = Math.Min(accelerationMax, accelerationFactor + acceleration);
                }

                // Next Parabolic SAR based on today's close/price value
                sar = priorSar + accelerationFactor * (extremePoint - priorSar);

                // Rule: Parabolic SAR can not be above prior period's low or the current low.
                sar = (i > 0) ? Math.Min(Math.Min(history[i].Low, history[i - 1].Low), sar) : Math.Min(history[i].Low, sar);

                // Rule: If Parabolic SAR crosses tomorrow's price range, the trend switches.
                if (sar > history[i + 1].Low)
                {
                    isRising = false;
                    sar = extremePoint;
                    extremePoint = history[i + 1].Low;
                    accelerationFactor = acceleration;
                }

            }
            else
            {
                // Making lower lows: accelerate
                if (candle.Low < extremePoint)
                {
                    extremePoint = candle.Low;
                    accelerationFactor = Math.Min(accelerationMax, accelerationFactor + acceleration);
                }

                // Next Parabolic SAR based on today's close/price value
                sar = priorSar + accelerationFactor * (extremePoint - priorSar);

                // Rule: Parabolic SAR can not be below prior period's high or the current high.
                sar = (i > 0) ? Math.Max(Math.Max(history[i].High, history[i - 1].High), sar) : Math.Max(history[i].High, sar);

                // Rule: If Parabolic SAR crosses tomorrow's price range, the trend switches.
                if (sar < history[i + 1].High)
                {
                    isRising = true;
                    sar = extremePoint;
                    extremePoint = history[i + 1].High;
                    accelerationFactor = acceleration;
                }
            }

            //parabolicSars[i + 1] = sar;
            //trends[i + 1] = isRising;

            r = new ParabolicSarResult(candle.Date)
            {
                Sar = (double)sar,
                IsReversal = false
            };
            results.Add(r);

            //if (trends[i] != trends[i + 1])
            //{
            //    trendFlip[i + 1] = true;
            //}
            //else
            //{
            //    trendFlip[i + 1] = false;
            //}

            priorSar = sar;
        }

        return results;
    }


    /// nog eentje (maar geen code):
    ///https://www.cmcmarkets.com/en/trading-guides/parabolic-sar
    ///Rising SAR = Prior SAR + Prior AF (Prior HP – Prior SAR)
    ///Falling SAR = Prior SAR – Prior AF(Prior SAR – Prior LP)
    ///

    // En nog eentje:
    // https://tulipindicators.org/download

    public static List<ParabolicSarResult> CalcParabolicSarTulip(List<CryptoCandle> history, decimal acceleration = 0.02m, decimal accelerationMax = 0.2m)
    {
        int size = history.Count;
        List<ParabolicSarResult> results = new(size);

        // Een dummy toevoegen (de 1e slagen we over)
        ParabolicSarResult r = new(history[0].Date);
        results.Add(r);

        //if (acceleration <= 0) return TI_INVALID_OPTION;
        //if (accelerationMax <= acceleration) return TI_INVALID_OPTION;
        //if (size < 2) return TI_OKAY;


        /* Try to choose if we start as short or long.
         * There is really no right answer here. */
        decimal sar, extremePoint;

        bool isRising = (history[0].High + history[0].Low <= history[1].High + history[1].Low);
        if (isRising)
        {
            extremePoint = history[0].High;
            sar = history[0].Low;
        }
        else
        {
            extremePoint = history[0].Low;
            sar = history[0].High;
        }

        decimal accel = acceleration;



        int i;
        for (i = 1; i < size; ++i)
        {
            sar = (extremePoint - sar) * accel + sar;

            if (isRising)
            {
                if (i >= 2 && (sar > history[i - 2].Low))
                    sar = history[i - 2].Low;

                if ((sar > history[i - 1].Low))
                    sar = history[i - 1].Low;

                if (accel < accelerationMax && history[i].High > extremePoint)
                {
                    accel += acceleration;
                    if (accel > accelerationMax)
                        accel = accelerationMax;
                }

                if (history[i].High > extremePoint)
                    extremePoint = history[i].High;

            }
            else
            {
                if (i >= 2 && (sar < history[i - 2].High))
                    sar = history[i - 2].High;

                if ((sar < history[i - 1].High))
                    sar = history[i - 1].High;

                if (accel < accelerationMax && history[i].Low < extremePoint)
                {
                    accel += acceleration;
                    if (accel > accelerationMax)
                        accel = accelerationMax;
                }

                if (history[i].Low < extremePoint)
                    extremePoint = history[i].Low;
            }



            if ((isRising && history[i].Low < sar) || (!isRising && history[i].High > sar))
            {
                accel = acceleration;
                sar = extremePoint;

                isRising = !isRising;
                if (isRising)
                    extremePoint = history[i].High;
                else
                    extremePoint = history[i].Low;
            }


            r = new ParabolicSarResult(history[i].Date)
            {
                Sar = (double)sar
            };
            results.Add(r);
        }


        return results;
    }

}
