using CryptoScanBot.Core.Model;
using Skender.Stock.Indicators;

namespace CryptoScanBot.Core.Intern;


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
                result.UpperBand = windowAverage + standardDeviations * standardDeviation;
                result.LowerBand = windowAverage - standardDeviations * standardDeviation;
                if (result.UpperBand == result.LowerBand)
                    result.Percentage = 0;
                else
                    result.Percentage = result.UpperBand / result.LowerBand - 1;
            }
        }

        return results;
    }


    /// <summary>
    /// Dave Skender Parabolic Sar
    /// </summary>
    static List<ParabolicSarResult> CalcParabolicSarDaveSkender(List<CryptoCandle> history,
        decimal accelerationStep = 0.02m, decimal maxAccelerationFactor = 0.2m)
    {
        // https://handwiki.org/wiki/Parabolic_SAR

        // https://en.wikipedia.org/wiki/Parabolic_SAR
        // Sar(n+1) =  Sar(n) + alfa*(EP - Sar(n))
        // De alfa is initieel 0.02 (The α value represents the acceleration factor)
        // acceleration is initially 0.2


        //EP(the extreme point) is a record kept during each trend that represents the highest value reached by the price
        //during the current uptrend – or lowest value during a downtrend. During each period, if a new maximum(or minimum)
        //is observed, the EP is updated with that value.

        //The α value represents the acceleration factor. Usually, this is set initially to a value of 0.02, but can be
        //chosen by the trader.This factor is increased by 0.02 each time a new EP is recorded, which means that every time
        //a new EP is observed, it will make the acceleration factor go up. The rate will then quicken to a point where the
        //SAR converges towards the price.To prevent it from getting too large, a maximum value for the acceleration factor
        //is normally set to 0.20.The traders can set these numbers depending on their trading style and the instruments
        //being traded.Generally, it is preferable in stocks trading to set the acceleration factor to 0.01, so that it is
        //not too sensitive to local decreases.For commodity or currency trading, the preferred value is 0.02.

        //The SAR is calculated in this manner for each new period.However, two special cases will modify the SAR value:

        //If the next period's SAR value is inside (or beyond) the current period or the previous period's price range, the
        //SAR must be set to the closest price bound.For example, if in an upward trend, the new SAR value is calculated and
        //if it results to be more than today's or yesterday's lowest price, it must be set equal to that lower boundary.
        //If the next period's SAR value is inside (or beyond) the next period's price range, a new trend direction is then
        //signaled. The SAR must then switch sides.
        //Upon a trend switch, the first SAR value for this new trend is set to the last EP recorded on the prior trend, EP is
        //then reset accordingly to this period's maximum, and the acceleration factor is reset to its initial value of 0.02.


        decimal initialFactor = accelerationStep;

        // check parameter arguments
        if (accelerationStep <= 0)
            throw new ArgumentOutOfRangeException(nameof(accelerationStep), accelerationStep,
                "Acceleration Step must be greater than 0 for Parabolic SAR.");

        if (maxAccelerationFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxAccelerationFactor), maxAccelerationFactor,
                "Max Acceleration Factor must be greater than 0 for Parabolic SAR.");

        if (accelerationStep > maxAccelerationFactor)
        {
            string message = string.Format(
                //EnglishCulture,
                "Acceleration Step must be smaller than provided Max Acceleration Factor ({0}) for Parabolic SAR.",
                maxAccelerationFactor);

            throw new ArgumentOutOfRangeException(nameof(accelerationStep), accelerationStep, message);
        }

        if (initialFactor <= 0 || initialFactor >= maxAccelerationFactor)
            throw new ArgumentOutOfRangeException(nameof(initialFactor), initialFactor,
                "Initial Step must be greater than 0 and less than Max Acceleration Factor for Parabolic SAR.");



        // initialize
        int length = history.Count;
        List<ParabolicSarResult> results = new(length);

        CryptoCandle candle;
        if (length == 0)
            return results;
        else
            candle = history[0];

        decimal accelerationFactor = initialFactor; // 0.02
        decimal extremePoint = candle.High;
        decimal priorSar = candle.Low;
        bool isRising = true;  // initial guess

        // roll through quotes
        for (int i = 0; i < length; i++)
        {
            candle = history[i];

            ParabolicSarResult r = new(candle.Date);
            results.Add(r);

            // skip first one
            if (i == 0)
                continue;

            // was rising
            if (isRising)
            {
                decimal sar = priorSar + accelerationFactor * (extremePoint - priorSar);

                // SAR cannot be higher than last two lows
                if (i >= 2)
                {
                    decimal minLastTwo = Math.Min(history[i - 1].Low, history[i - 2].Low);
                    sar = Math.Min(sar, minLastTwo);
                }

                // turn down
                if (candle.Low < sar)
                {
                    r.IsReversal = true;
                    r.Sar = (double)extremePoint;

                    isRising = false;
                    accelerationFactor = initialFactor;
                    extremePoint = candle.Low;
                }

                // continue rising
                else
                {
                    r.IsReversal = false;
                    r.Sar = (double)sar;

                    // new high extreme point
                    if (candle.High > extremePoint)
                    {
                        extremePoint = candle.High;
                        accelerationFactor = Math.Min(accelerationFactor + accelerationStep, maxAccelerationFactor);
                    }
                }
            }

            // was falling
            else

            {
                decimal sar = priorSar - accelerationFactor * (priorSar - extremePoint);

                // SAR cannot be lower than last two highs
                if (i >= 2)
                {
                    decimal maxLastTwo = Math.Max(history[i - 1].High, history[i - 2].High);
                    sar = Math.Max(sar, maxLastTwo);
                }

                // turn up
                if (candle.High > sar)
                {
                    r.IsReversal = true;
                    r.Sar = (double)extremePoint;

                    isRising = true;
                    accelerationFactor = initialFactor;
                    extremePoint = candle.High;
                }

                // continue falling
                else
                {
                    r.IsReversal = false;
                    r.Sar = (double)sar;

                    // new low extreme point
                    if (candle.Low < extremePoint)
                    {
                        extremePoint = candle.Low;
                        accelerationFactor = Math.Min(accelerationFactor + accelerationStep, maxAccelerationFactor);
                    }
                }
            }

            priorSar = (decimal)r.Sar;
        }


        //// remove first trendline since it is an invalid guess
        //ParabolicSarResult? firstReversal = results
        //    .Where(x => x.IsReversal == true)
        //    .OrderBy(x => x.Date)
        //    .FirstOrDefault();

        //int cutIndex = (firstReversal != null) ? results.IndexOf(firstReversal) : length - 1;

        //for (int d = 0; d <= cutIndex; d++)
        //{
        //    ParabolicSarResult r = results[d];
        //    r.Sar = null;
        //    r.IsReversal = null;
        //}

        return results;
    }


    /// <summary>
    /// ParabolicSar
    /// https://github.com/jasonlam604/StockTechnicals/blob/master/src/com/jasonlam604/stocktechnicals/indicators/ParabolicSar.java
    /// Deze doet het ietsjes anders dan die van Dave Skender, interessant!
    /// </summary>
    public static List<ParabolicSarResult> CalcParabolicSarJasonLam(this List<CryptoCandle> history, decimal acceleration = 0.02m, decimal accelerationMax = 0.2m)
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

        bool isRising = history[1].High >= history[0].High || history[0].Low <= history[1].Low; // ? true : false;
        decimal priorSar = isRising ? history[0].Low : history[0].High;
        decimal extremePoint = isRising ? history[0].High : history[0].Low;
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
                sar = i > 0 ? Math.Min(Math.Min(history[i].Low, history[i - 1].Low), sar) : Math.Min(history[i].Low, sar);

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
                sar = i > 0 ? Math.Max(Math.Max(history[i].High, history[i - 1].High), sar) : Math.Max(history[i].High, sar);

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

    public static List<ParabolicSarResult> CalcParabolicSarTulip(this List<CryptoCandle> history, decimal acceleration = 0.02m, decimal accelerationMax = 0.2m)
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

        bool isRising = history[0].High + history[0].Low <= history[1].High + history[1].Low;
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
                if (i >= 2 && sar > history[i - 2].Low)
                    sar = history[i - 2].Low;

                if (sar > history[i - 1].Low)
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
                if (i >= 2 && sar < history[i - 2].High)
                    sar = history[i - 2].High;

                if (sar < history[i - 1].High)
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



            if (isRising && history[i].Low < sar || !isRising && history[i].High > sar)
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

