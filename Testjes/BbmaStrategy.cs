namespace CryptoScanBot.TestStuff
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    public class Candle
    {
        public DateTime Date { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
    }

    public class BbmaStrategy
    {

        private double[] ComputeSMA(int period, double[] prices)
        {
            double[] sma = new double[prices.Length];
            for (int i = period - 1; i < prices.Length; i++)
            {
                double sum = 0;
                for (int j = 0; j < period; j++)
                {
                    sum += prices[i - j];
                }
                sma[i] = sum / period;
            }
            return sma;
        }

        private double[] ComputeStdDev(int period, double[] prices)
        {
            double[] std = new double[prices.Length];
            double[] sma = ComputeSMA(period, prices);
            for (int i = period - 1; i < prices.Length; i++)
            {
                double sumSq = 0;
                for (int j = 0; j < period; j++)
                {
                    double diff = prices[i - j] - sma[i];
                    sumSq += diff * diff;
                }
                std[i] = Math.Sqrt(sumSq / period);
            }
            return std;
        }

        private static double[] ComputeWMA(int period, double[] prices)
        {
            double[] wma = new double[prices.Length];
            double sumWeights = Enumerable.Range(1, period).Sum();
            int[] weights = Enumerable.Range(1, period).Reverse().ToArray();
            for (int i = period - 1; i < prices.Length; i++)
            {
                double sum = 0;
                for (int j = 0; j < period; j++)
                {
                    sum += prices[i - j] * weights[j];
                }
                wma[i] = sum / sumWeights;
            }
            return wma;
        }

        private double[] ComputeEMA(int period, double[] prices)
        {
            double[] ema = new double[prices.Length];
            double multiplier = 2.0 / (period + 1);
            ema[0] = prices[0]; // Initial EMA is the first price
            for (int i = 1; i < prices.Length; i++)
            {
                ema[i] = (prices[i] * multiplier) + (ema[i - 1] * (1 - multiplier));
            }
            return ema;
        }

        public void Analyze(List<Candle> candles)
        {
            if (candles.Count < 50)
            {
                Console.WriteLine("Insufficient data. Need at least 50 candles.");
                return;
            }

            double[] closes = candles.Select(c => c.Close).ToArray();
            double[] highs = candles.Select(c => c.High).ToArray();
            double[] lows = candles.Select(c => c.Low).ToArray();
            double[] wma5_high = ComputeWMA(5, highs);
            double[] wma10_high = ComputeWMA(10, highs);
            double[] wma5_low = ComputeWMA(5, lows);
            double[] wma10_low = ComputeWMA(10, lows);
            double[] ema50 = ComputeEMA(50, closes);
            double[] sma20 = ComputeSMA(20, closes);
            double[] std20 = ComputeStdDev(20, closes);
            double[] upperBB = new double[closes.Length];
            double[] lowerBB = new double[closes.Length];
            for (int i = 19; i < closes.Length; i++)
            {
                upperBB[i] = sma20[i] + (2 * std20[i]);
                lowerBB[i] = sma20[i] - (2 * std20[i]);
            }

            Console.WriteLine("BBMA Oma Ally Strategy Analysis - Signals Detected:");
            Console.WriteLine("==========================================");

            for (int i = 50; i < candles.Count; i++) // Start after EMA50 period
            {
                // Basic Extreme Signals: Based on WMA5 outside BB
                bool basicExtremeBuy = wma5_low[i] < lowerBB[i] && candles[i].Close > candles[i].Open;
                bool basicExtremeSell = wma5_high[i] > upperBB[i] && candles[i].Close < candles[i].Open;

                // Advance Extreme Signals: Wick rejection of EMA50
                bool advanceExtremeBuy = candles[i].Low <= ema50[i] && candles[i].Close > ema50[i] && candles[i].Close > candles[i].Open;
                bool advanceExtremeSell = candles[i].High >= ema50[i] && candles[i].Close < ema50[i] && candles[i].Close < candles[i].Open;

                // Magic Extreme: WMA5 and WMA10 outside BB
                bool magicExtremeBuy = basicExtremeBuy && wma5_low[i] < lowerBB[i] && wma10_low[i] < lowerBB[i];
                bool magicExtremeSell = basicExtremeSell && wma5_high[i] > upperBB[i] && wma10_high[i] > upperBB[i];

                if (basicExtremeBuy || magicExtremeBuy)
                {
                    string type = magicExtremeBuy ? "Magic Basic Extreme Buy" : "Basic Extreme Buy";
                    Console.WriteLine($"{type} at {candles[i].Date:yyyy-MM-dd HH:mm} - Price: {candles[i].Close:F4}, WMA5_low: {wma5_low[i]:F4}");
                }
                if (basicExtremeSell || magicExtremeSell)
                {
                    string type = magicExtremeSell ? "Magic Basic Extreme Sell" : "Basic Extreme Sell";
                    Console.WriteLine($"{type} at {candles[i].Date:yyyy-MM-dd HH:mm} - Price: {candles[i].Close:F4}, WMA5_high: {wma5_high[i]:F4}");
                }

                if (advanceExtremeBuy)
                {
                    Console.WriteLine($"Advance Extreme Buy (EMA50 Wick Rejection) at {candles[i].Date:yyyy-MM-dd HH:mm} - Price: {candles[i].Close:F4}, EMA50: {ema50[i]:F4}");
                }
                if (advanceExtremeSell)
                {
                    Console.WriteLine($"Advance Extreme Sell (EMA50 Wick Rejection) at {candles[i].Date:yyyy-MM-dd HH:mm} - Price: {candles[i].Close:F4}, EMA50: {ema50[i]:F4}");
                }

                // TPW: After Extreme (basic, magic, or advance), TP at WMA_low (for buy) or WMA_high (for sell)
                bool recentExtremeSell = i > 0 && (basicExtremeSell || magicExtremeSell || advanceExtremeSell) && candles[i].Close > wma5_high[i] && candles[i - 1].Close <= wma5_high[i - 1];
                if (recentExtremeSell)
                {
                    double tpLevel = Math.Min(wma5_high[i], wma10_high[i]);
                    Console.WriteLine($"TPW (Take Profit) Suggestion after Extreme Sell at {candles[i].Date:yyyy-MM-dd HH:mm} - TP Level: {tpLevel:F4}");
                }
                bool recentExtremeBuy = i > 0 && (basicExtremeBuy || magicExtremeBuy || advanceExtremeBuy) && candles[i].Close < wma5_low[i] && candles[i - 1].Close >= wma5_low[i - 1];
                if (recentExtremeBuy)
                {
                    double tpLevel = Math.Max(wma5_low[i], wma10_low[i]);
                    Console.WriteLine($"TPW (Take Profit) Suggestion after Extreme Buy at {candles[i].Date:yyyy-MM-dd HH:mm} - TP Level: {tpLevel:F4}");
                }

                // MHV: Consolidation inside BB after Extreme
                bool mhvPotential = (basicExtremeBuy || basicExtremeSell || advanceExtremeBuy || advanceExtremeSell) && candles[i].High < upperBB[i] && candles[i].Low > lowerBB[i];
                if (mhvPotential)
                {
                    Console.WriteLine($"MHV (Potential Loss of Volume) at {candles[i].Date:yyyy-MM-dd HH:mm} - Price consolidating inside BB");
                }

                // CSD: WMA5/WMA10 crossover (use lows for buy, highs for sell)
                bool csdBull = i > 0 && wma5_low[i] > wma10_low[i] && wma5_low[i - 1] <= wma10_low[i - 1];
                bool csdBear = i > 0 && wma5_high[i] < wma10_high[i] && wma5_high[i - 1] >= wma10_high[i - 1];
                if (csdBull)
                {
                    Console.WriteLine($"CSD Bull (Direction Change Up) at {candles[i].Date:yyyy-MM-dd HH:mm}");
                }
                if (csdBear)
                {
                    Console.WriteLine($"CSD Bear (Direction Change Down) at {candles[i].Date:yyyy-MM-dd HH:mm}");
                }

                // CSM: Strong candle after CSD
                double bodySize = Math.Abs(candles[i].Close - candles[i].Open);
                bool strongCandle = bodySize > 0.01 * candles[i].Close;
                bool csmBull = csdBull && strongCandle && candles[i].Close > candles[i].Open;
                bool csmBear = csdBear && strongCandle && candles[i].Close < candles[i].Open;
                if (csmBull)
                {
                    Console.WriteLine($"CSM Bull (Momentum Up) at {candles[i].Date:yyyy-MM-dd HH:mm} - Strong bullish candle");
                }
                if (csmBear)
                {
                    Console.WriteLine($"CSM Bear (Momentum Down) at {candles[i].Date:yyyy-MM-dd HH:mm} - Strong bearish candle");
                }

                // Reentry: Pullback to WMA band (between wma5_low/wma10_low and wma5_high/wma10_high)
                bool reentryBuyZone = csdBull && i > 0 && candles[i].Low <= Math.Max(wma5_low[i], wma10_low[i]) && candles[i].Close >= Math.Min(wma5_low[i], wma10_low[i]);
                bool reentrySellZone = csdBear && i > 0 && candles[i].High >= Math.Min(wma5_high[i], wma10_high[i]) && candles[i].Close <= Math.Max(wma5_high[i], wma10_high[i]);
                if (reentryBuyZone)
                {
                    double zoneLow = Math.Min(wma5_low[i], wma10_low[i]);
                    double zoneHigh = Math.Max(wma5_high[i], wma10_high[i]);
                    Console.WriteLine($"Reentry Buy Zone at {candles[i].Date:yyyy-MM-dd HH:mm} - Zone: {zoneLow:F4} to {zoneHigh:F4}");
                }
                if (reentrySellZone)
                {
                    double zoneLow = Math.Min(wma5_low[i], wma10_low[i]);
                    double zoneHigh = Math.Max(wma5_high[i], wma10_high[i]);
                    Console.WriteLine($"Reentry Sell Zone at {candles[i].Date:yyyy-MM-dd HH:mm} - Zone: {zoneLow:F4} to {zoneHigh:F4}");
                }
            }
        }
    }

    public class BbmaProgram
    {
        static void Execute()
        {
            List<Candle> sampleCandles = new List<Candle>();
            Random rand = new Random(42);
            double price = 1.1000;
            DateTime startDate = new DateTime(2025, 1, 1);
            for (int i = 0; i < 100; i++)
            {
                double change = (rand.NextDouble() - 0.5) * 0.002;
                price += change;
                double open = price;
                double high = open + Math.Abs(rand.NextDouble() * 0.001);
                double low = open - Math.Abs(rand.NextDouble() * 0.001);
                double close = low + rand.NextDouble() * (high - low);
                sampleCandles.Add(new Candle
                {
                    Date = startDate.AddHours(i),
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = rand.NextDouble() * 10000
                });
            }

            BbmaStrategy bbma = new();
            bbma.Analyze(sampleCandles);

            Console.WriteLine("\nNote: This implementation is based on Oma Ally's BBMA strategy, including Basic Extreme (WMA5 outside BB), Advance Extreme (EMA50 rejection), and other stages.");
            Console.WriteLine("WMA5 and WMA10 form a band: upper based on candle highs, lower based on candle lows.");
            Console.WriteLine("For multi-timeframe (multi-code), extend by analyzing higher TF candles and aligning signals (e.g., Extreme on H1 valid if CSD on H4 aligns).");
            Console.ReadKey();
        }
    }

}