using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using System;
using System.Collections.Generic;
using System.Linq;

namespace BBMA_Strategy
{
    public enum TimeFrame { M5, M15, H1, H4, D1, W1, MN }

    public class BBMA
    {
        private decimal[] ComputeSMA(int period, decimal[] prices)
        {
            decimal[] sma = new decimal[prices.Length];
            for (int i = period - 1; i < prices.Length; i++)
            {
                decimal sum = 0;
                for (int j = 0; j < period; j++)
                {
                    sum += prices[i - j];
                }
                sma[i] = sum / period;
            }
            return sma;
        }

        private decimal[] ComputeStdDev(int period, decimal[] prices)
        {
            decimal[] std = new decimal[prices.Length];
            decimal[] sma = ComputeSMA(period, prices);
            for (int i = period - 1; i < prices.Length; i++)
            {
                decimal sumSq = 0;
                for (int j = 0; j < period; j++)
                {
                    decimal diff = prices[i - j] - sma[i];
                    sumSq += diff * diff;
                }
                std[i] = (decimal)Math.Sqrt((double)(sumSq / period));
            }
            return std;
        }

        private decimal[] ComputeLWMA(int period, decimal[] prices)
        {
            decimal[] lwma = new decimal[prices.Length];
            decimal sumWeights = Enumerable.Range(1, period).Sum();
            int[] weights = Enumerable.Range(1, period).Reverse().ToArray();
            for (int i = period - 1; i < prices.Length; i++)
            {
                decimal sum = 0;
                for (int j = 0; j < period; j++)
                {
                    sum += prices[i - j] * weights[j];
                }
                lwma[i] = sum / sumWeights;
            }
            return lwma;
        }

        private decimal[] ComputeEMA(int period, decimal[] prices)
        {
            decimal[] ema = new decimal[prices.Length];
            decimal multiplier = 2m / (period + 1);
            ema[0] = prices[0];
            for (int i = 1; i < prices.Length; i++)
            {
                ema[i] = (prices[i] * multiplier) + (ema[i - 1] * (1m - multiplier));
            }
            return ema;
        }

        public async Task Analyze(string symbolName, Dictionary<TimeFrame, List<CryptoCandle>> multiTimeFrameCandles)
        {
            int maxSignalAge = 60; // Max leeftijd voor signalen (bijv. 12 H1-candles)

            // Definieer TF-orde voor MTF-validatie (hoog naar laag: D1 > H4 > H1 > M15, etc.)
            var timeFrames = new[] { TimeFrame.D1, TimeFrame.H4, TimeFrame.H1, TimeFrame.M15 };
            var signals = new Dictionary<TimeFrame, Dictionary<string, (bool Active, int Age)>>(); // Per TF: signal-type -> (active, candles sinds signaal)

            foreach (var tf in timeFrames)
            {
                if (!multiTimeFrameCandles.ContainsKey(tf)) 
                    continue;
                var candles = multiTimeFrameCandles[tf];
                if (candles.Count < 50)
                {
                    GlobalData.AddTextToLogTab($"{symbolName} Insufficient data for {tf}. Need at least 50 candles.");
                    return;
                }

                decimal[] highs = candles.Select(c => c.High).ToArray();
                decimal[] lwma5_high = ComputeLWMA(5, highs);
                decimal[] lwma10_high = ComputeLWMA(10, highs);

                decimal[] lows = candles.Select(c => c.Low).ToArray();
                decimal[] lwma5_low = ComputeLWMA(5, lows);
                decimal[] lwma10_low = ComputeLWMA(10, lows);

                decimal[] closes = candles.Select(c => c.Close).ToArray();
                decimal[] ema50 = ComputeEMA(50, closes);

                decimal[] midBB = ComputeSMA(20, closes); // Mid BB for trend check
                decimal[] upperBB = new decimal[closes.Length];
                decimal[] lowerBB = new decimal[closes.Length];
                for (int i = 19; i < closes.Length; i++)
                {
                    upperBB[i] = midBB[i] + (2m * ComputeStdDev(20, closes)[i]);
                    lowerBB[i] = midBB[i] - (2m * ComputeStdDev(20, closes)[i]);
                }

                //GlobalData.AddTextToLogTab($"\n--- Signals for {tf} ---");
                signals[tf] = [];

                for (int i = 50; i < candles.Count; i++)
                {
                    var candle = candles[i];

                    // Verhoog leeftijd van bestaande signalen
                    foreach (var signal in signals[tf].ToList())
                    {
                        signals[tf][signal.Key] = (signal.Value.Active, signal.Value.Age + 1);
                        if (signal.Value.Age > maxSignalAge) 
                            signals[tf][signal.Key] = (false, signal.Value.Age); // Deactiveer oude signalen
                    }

                    // Trend check using EMA50
                    bool isUptrend = ema50[i] < midBB[i]; // EMA50 below mid BB = Uptrend
                    bool isDowntrend = ema50[i] > midBB[i]; // EMA50 above mid BB = Downtrend

                    // Extreme Type A: LWMA 5 high/low closes above/below BB
                    bool extremeTypeA_Buy = lwma5_low[i] < lowerBB[i];
                    bool extremeTypeA_Sell = lwma5_high[i] > upperBB[i];

                    // Extreme Type B: Bullish/bearish candle rejects BB
                    bool extremeTypeB_Buy = candle.Low <= lowerBB[i] && candle.Close > lowerBB[i] && candle.Close > candle.Open;
                    bool extremeTypeB_Sell = candle.High >= upperBB[i] && candle.Close < upperBB[i] && candle.Close < candle.Open;

                    // Magic Extreme: LWMA 5 + LWMA 10 outside BB
                    bool magicExtremeBuy = extremeTypeA_Buy && lwma10_low[i] < lowerBB[i] && candle.Close > candle.Open;
                    bool magicExtremeSell = extremeTypeA_Sell && lwma10_high[i] > upperBB[i] && candle.Close < candle.Open;

                    // Advance Extreme: Price rejects EMA 50 (wick rejection)
                    bool advanceExtremeBuy = candle.Low <= ema50[i] && candle.Close > ema50[i] && candle.Close > candle.Open;
                    bool advanceExtremeSell = candle.High >= ema50[i] && candle.Close < ema50[i] && candle.Close < candle.Open;

                    // CSD (CSAK): LWMA5/WMA10 crossover (use lows for buy, highs for sell)
                    bool csdBull = i > 0 && lwma5_low[i] > lwma10_low[i] && lwma5_low[i - 1] <= lwma10_low[i - 1];
                    bool csdBear = i > 0 && lwma5_high[i] < lwma10_high[i] && lwma5_high[i - 1] >= lwma10_high[i - 1];

                    // Early CSD: CSD zonder volledige MLV (hoog risico)
                    bool earlyCsdBull = csdBull && (!signals[tf].ContainsKey("MLV") || !signals[tf]["MLV"].Active);
                    bool earlyCsdBear = csdBear && (!signals[tf].ContainsKey("MLV") || !signals[tf]["MLV"].Active);

                    // CSM: Strong candle after CSD
                    decimal bodySize = Math.Abs(candle.Close - candle.Open);
                    bool strongCandle = bodySize > 0.01m * candle.Close;
                    bool csmBull = csdBull && strongCandle && candle.Close > candle.Open;
                    bool csmBear = csdBear && strongCandle && candle.Close < candle.Open;

                    // Early CSM: CSM zonder volledige CSD (hoog risico)
                    bool earlyCsmBull = csmBull && (!signals[tf].ContainsKey("CSDBull") || !signals[tf]["CSDBull"].Active);
                    bool earlyCsmBear = csmBear && (!signals[tf].ContainsKey("CSDBear") || !signals[tf]["CSDBear"].Active);

                    // Re-entry Zones (na CSD/CSM)
                    bool reentryBuyZone = (csdBull || csmBull || earlyCsdBull || earlyCsmBull) && candle.Close >= lwma5_low[i] && candle.Close <= lwma10_low[i];
                    bool reentrySellZone = (csdBear || csmBear || earlyCsdBear || earlyCsmBear) && candle.Close <= lwma5_high[i] && candle.Close >= lwma10_high[i];

                    // Reset-condities
                    if (csdBull || earlyCsdBull)
                    {
                        // CSD reset Extreme/Reentry Buy-signalen
                        signals[tf]["ExtremeTypeA_Buy"] = (false, 0);
                        signals[tf]["ExtremeTypeB_Buy"] = (false, 0);
                        signals[tf]["MagicExtremeBuy"] = (false, 0);
                        signals[tf]["AdvanceExtremeBuy"] = (false, 0);
                        signals[tf]["ReentryBuy"] = (false, 0);
                    }
                    if (csdBear || earlyCsdBear)
                    {
                        // CSD reset Extreme/Reentry Sell-signalen
                        signals[tf]["ExtremeTypeA_Sell"] = (false, 0);
                        signals[tf]["ExtremeTypeB_Sell"] = (false, 0);
                        signals[tf]["MagicExtremeSell"] = (false, 0);
                        signals[tf]["AdvanceExtremeSell"] = (false, 0);
                        signals[tf]["ReentrySell"] = (false, 0);
                    }

                    // MLV: Consolidation inside BB after Extreme
                    bool mlvPotential = (extremeTypeA_Buy || extremeTypeA_Sell || extremeTypeB_Buy || extremeTypeB_Sell || advanceExtremeBuy || 
                        advanceExtremeSell || magicExtremeBuy || magicExtremeSell) && candle.High < upperBB[i] && candle.Low > lowerBB[i];
                    if (mlvPotential)
                    {
                        // MLV reset Extreme-signalen
                        signals[tf]["ExtremeTypeA_Buy"] = (false, 0);
                        signals[tf]["ExtremeTypeA_Sell"] = (false, 0);
                        signals[tf]["ExtremeTypeB_Buy"] = (false, 0);
                        signals[tf]["ExtremeTypeB_Sell"] = (false, 0);
                        signals[tf]["MagicExtremeBuy"] = (false, 0);
                        signals[tf]["MagicExtremeSell"] = (false, 0);
                        signals[tf]["AdvanceExtremeBuy"] = (false, 0);
                        signals[tf]["AdvanceExtremeSell"] = (false, 0);
                        signals[tf]["MLV"] = (true, 0);
                    }

                    // TPW: After Extreme, TP at LWMA_low (for buy) or LWMA_high (for sell)
                    bool recentExtremeSell = i > 0 && (extremeTypeA_Sell || extremeTypeB_Sell || magicExtremeSell || advanceExtremeSell) && candle.Close > lwma5_high[i] && candles[i - 1].Close <= lwma5_high[i - 1];
                    if (recentExtremeSell)
                    {
                        decimal tpLevel = Math.Min(lwma5_high[i], lwma10_high[i]);
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} TPW (Take Profit Wajib) Suggestion after Extreme Sell - TP Level: {tpLevel:F4}");
                        // TPW reset Extreme Sell
                        signals[tf]["ExtremeTypeA_Sell"] = (false, 0);
                        signals[tf]["ExtremeTypeB_Sell"] = (false, 0);
                        signals[tf]["MagicExtremeSell"] = (false, 0);
                        signals[tf]["AdvanceExtremeSell"] = (false, 0);
                    }
                    bool recentExtremeBuy = i > 0 && (extremeTypeA_Buy || extremeTypeB_Buy || magicExtremeBuy || advanceExtremeBuy) && candle.Close < lwma5_low[i] && candles[i - 1].Close >= lwma5_low[i - 1];
                    if (recentExtremeBuy)
                    {
                        decimal tpLevel = Math.Max(lwma5_low[i], lwma10_low[i]);
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} TPW (Take Profit Wajib) Suggestion after Extreme Buy - TP Level: {tpLevel:F4}");
                        // TPW reset Extreme Buy
                        signals[tf]["ExtremeTypeA_Buy"] = (false, 0);
                        signals[tf]["ExtremeTypeB_Buy"] = (false, 0);
                        signals[tf]["MagicExtremeBuy"] = (false, 0);
                        signals[tf]["AdvanceExtremeBuy"] = (false, 0);
                    }

                    // Track nieuwe signalen (alleen als trend matcht)
                    if (extremeTypeA_Buy && isUptrend) signals[tf]["ExtremeTypeA_Buy"] = (true, 0);
                    if (extremeTypeA_Sell && isDowntrend) signals[tf]["ExtremeTypeA_Sell"] = (true, 0);
                    if (extremeTypeB_Buy && isUptrend) signals[tf]["ExtremeTypeB_Buy"] = (true, 0);
                    if (extremeTypeB_Sell && isDowntrend) signals[tf]["ExtremeTypeB_Sell"] = (true, 0);
                    if (magicExtremeBuy && isUptrend) signals[tf]["MagicExtremeBuy"] = (true, 0);
                    if (magicExtremeSell && isDowntrend) signals[tf]["MagicExtremeSell"] = (true, 0);
                    if (advanceExtremeBuy && isUptrend) signals[tf]["AdvanceExtremeBuy"] = (true, 0);
                    if (advanceExtremeSell && isDowntrend) signals[tf]["AdvanceExtremeSell"] = (true, 0);
                    if (csdBull) signals[tf]["CSDBull"] = (true, 0);
                    if (csdBear) signals[tf]["CSDBear"] = (true, 0);
                    if (earlyCsdBull) signals[tf]["EarlyCSDBull"] = (true, 0);
                    if (earlyCsdBear) signals[tf]["EarlyCSDBear"] = (true, 0);
                    if (csmBull) signals[tf]["CSMBull"] = (true, 0);
                    if (csmBear) signals[tf]["CSMBear"] = (true, 0);
                    if (earlyCsmBull) signals[tf]["EarlyCSMBull"] = (true, 0);
                    if (earlyCsmBear) signals[tf]["EarlyCSMBear"] = (true, 0);
                    if (reentryBuyZone && isUptrend) signals[tf]["ReentryBuy"] = (true, 0);
                    if (reentrySellZone && isDowntrend) signals[tf]["ReentrySell"] = (true, 0);

                    // Output actieve signalen
                    if ((extremeTypeA_Buy || extremeTypeB_Buy || magicExtremeBuy) && isUptrend && (!signals[tf].ContainsKey("ExtremeBuy") || signals[tf]["ExtremeBuy"].Age <= maxSignalAge))
                    {
                        string type = magicExtremeBuy ? "Magic Extreme Buy" : (extremeTypeA_Buy ? "Extreme Type A Buy" : "Extreme Type B Buy");
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} {type}  Price: {candle.Close:F4}, LWMA5_low: {lwma5_low[i]:F4}");
                    }
                    if ((extremeTypeA_Sell || extremeTypeB_Sell || magicExtremeSell) && isDowntrend && (!signals[tf].ContainsKey("ExtremeSell") || signals[tf]["ExtremeSell"].Age <= maxSignalAge))
                    {
                        string type = magicExtremeSell ? "Magic Extreme Sell" : (extremeTypeA_Sell ? "Extreme Type A Sell" : "Extreme Type B Sell");
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} {type}  Price: {candle.Close:F4}, LWMA5_high: {lwma5_high[i]:F4}");
                    }
                    if (advanceExtremeBuy && isUptrend && (!signals[tf].ContainsKey("AdvanceExtremeBuy") || signals[tf]["AdvanceExtremeBuy"].Age <= maxSignalAge))
                    {
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} Advance Extreme Buy (EMA50 Wick Rejection)  Price: {candle.Close:F4}, EMA50: {ema50[i]:F4}");
                    }
                    if (advanceExtremeSell && isDowntrend && (!signals[tf].ContainsKey("AdvanceExtremeSell") || signals[tf]["AdvanceExtremeSell"].Age <= maxSignalAge))
                    {
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} Advance Extreme Sell (EMA50 Wick Rejection)  Price: {candle.Close:F4}, EMA50: {ema50[i]:F4}");
                    }
                    if (recentExtremeSell)
                    {
                        decimal tpLevel = Math.Min(lwma5_high[i], lwma10_high[i]);
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} TPW (Take Profit Wajib) Suggestion after Extreme Sell  TP Level: {tpLevel:F4}");
                    }
                    if (recentExtremeBuy)
                    {
                        decimal tpLevel = Math.Max(lwma5_low[i], lwma10_low[i]);
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} TPW (Take Profit Wajib) Suggestion after Extreme Buy  TP Level: {tpLevel:F4}");
                    }
                    if (mlvPotential)
                    {
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} MLV (Market Loss Volume)  Price consolidating inside BB");
                    }
                    if (csdBull || earlyCsdBull)
                    {
                        string type = earlyCsdBull ? "Early CSD (CSAK) Bull (High Risk)" : "CSD (CSAK) Bull (Direction Change Up)";
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} {type} at {candles[i].Date:yyyy-MM-dd HH:mm}");
                    }
                    if (csdBear || earlyCsdBear)
                    {
                        string type = earlyCsdBear ? "Early CSD (CSAK) Bear (High Risk)" : "CSD (CSAK) Bear (Direction Change Down)";
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} {type} at {candles[i].Date:yyyy-MM-dd HH:mm}");
                    }
                    if (csmBull || earlyCsmBull)
                    {
                        string type = earlyCsmBull ? "Early CSM Bull (High Risk)" : "CSM Bull (Momentum Up)";
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} {type} - Strong bullish candle");
                    }
                    if (csmBear || earlyCsmBear)
                    {
                        string type = earlyCsmBear ? "Early CSM Bear (High Risk)" : "CSM Bear (Momentum Down)";
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} {type} - Strong bearish candle");
                    }
                    if (reentryBuyZone && isUptrend && (!signals[tf].ContainsKey("ReentryBuy") || signals[tf]["ReentryBuy"].Age <= maxSignalAge))
                    {
                        decimal zoneLow = Math.Min(lwma5_low[i], lwma10_low[i]);
                        decimal zoneHigh = Math.Max(lwma5_low[i], lwma10_low[i]); // Corrigeer: zoneHigh moet bovenste limiet zijn, maar hier is het hetzelfde als low (fix later)
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} Reentry Buy Zone (after CSD/CSM) - Zone: {zoneLow:F4} to {zoneHigh:F4}");
                    }
                    if (reentrySellZone && isDowntrend && (!signals[tf].ContainsKey("ReentrySell") || signals[tf]["ReentrySell"].Age <= maxSignalAge))
                    {
                        decimal zoneLow = Math.Min(lwma5_high[i], lwma10_high[i]);
                        decimal zoneHigh = Math.Max(lwma5_high[i], lwma10_high[i]); // Corrigeer: zoneHigh moet bovenste limiet zijn
                        GlobalData.AddTextToLogTab($"{symbolName} {candle.Date:yyyy-MM-dd HH:mm} Reentry Sell Zone (after CSD/CSM) - Zone: {zoneLow:F4} to {zoneHigh:F4}");
                    }
                }
            }

            // MTF Validatie met Codes (REM, REE, EEM, etc.)
            //GlobalData.AddTextToLogTab($"{symbolName}--- Multi-Timeframe (Multi-Code) Validation ---");
            //GlobalData.AddTextToLogTab($"{symbolName}Criteria: Codes like REM (Reentry on high TF + Extreme on middle + MLV on low). Only recent signals (age <= {maxSignalAge}).");
            // Efficiënt checken met list van codes
            var mtfCodes = new List<(string Code, 
                string HighSignalBuy, string MiddleSignalBuy, string LowSignalBuy, 
                string HighSignalSell, string MiddleSignalSell, string LowSignalSell)> 
            {
                ("REM", "ReentryBuy", "ExtremeBuy", "MLV", "ReentrySell", "ExtremeSell", "MLV"),
                ("REE", "ReentryBuy", "ExtremeBuy", "ExtremeBuy", "ReentrySell", "ExtremeSell", "ExtremeSell"),
                ("EEM", "ExtremeBuy", "ExtremeBuy", "MLV", "ExtremeSell", "ExtremeSell", "MLV"),
                ("ERM", "ExtremeBuy", "ReentryBuy", "MLV", "ExtremeSell", "ReentrySell", "MLV"),
                ("RMM", "ReentryBuy", "MLV", "MLV", "ReentrySell", "MLV", "MLV"),
                ("EEE", "ExtremeBuy", "ExtremeBuy", "ExtremeBuy", "ExtremeSell", "ExtremeSell", "ExtremeSell"),
                ("ERE", "ExtremeBuy", "ReentryBuy", "ExtremeBuy", "ExtremeSell", "ReentrySell", "ExtremeSell"),
                ("MRE", "MLV", "ReentryBuy", "ExtremeBuy", "MLV", "ReentrySell", "ExtremeSell"),
                ("REM", "ReentryBuy", "ExtremeBuy", "MLV", "ReentrySell", "ExtremeSell", "MLV") // Herhaling voor emphasis
            };

            for (int idx = 2; idx < timeFrames.Length; idx++) // Voor 3 TF's: high = D1, middle = H4, low = H1 or M15
            {
                var highTF = timeFrames[idx - 2];
                var middleTF = timeFrames[idx - 1];
                var lowTF = timeFrames[idx];
                if (!signals.ContainsKey(highTF) || !signals.ContainsKey(middleTF) || !signals.ContainsKey(lowTF)) continue;

                foreach (var code in mtfCodes)
                {
                    bool buyMatch = signals[highTF].ContainsKey(code.HighSignalBuy) && signals[highTF][code.HighSignalBuy].Active && signals[highTF][code.HighSignalBuy].Age <= maxSignalAge &&
                                    signals[middleTF].ContainsKey(code.MiddleSignalBuy) && signals[middleTF][code.MiddleSignalBuy].Active && signals[middleTF][code.MiddleSignalBuy].Age <= maxSignalAge &&
                                    signals[lowTF].ContainsKey(code.LowSignalBuy) && signals[lowTF][code.LowSignalBuy].Active && signals[lowTF][code.LowSignalBuy].Age <= maxSignalAge;
                    if (buyMatch)
                    {
                        GlobalData.AddTextToLogTab($"{symbolName} MTF Code {code.Code} Buy Confirmed on {lowTF} (High: {highTF} {code.HighSignalBuy}, Middle: {middleTF} {code.MiddleSignalBuy}, Low: {lowTF} {code.LowSignalBuy}) - Strong Reversal Setup!");
                    }

                    bool sellMatch = signals[highTF].ContainsKey(code.HighSignalSell) && signals[highTF][code.HighSignalSell].Active && signals[highTF][code.HighSignalSell].Age <= maxSignalAge &&
                                     signals[middleTF].ContainsKey(code.MiddleSignalSell) && signals[middleTF][code.MiddleSignalSell].Active && signals[middleTF][code.MiddleSignalSell].Age <= maxSignalAge &&
                                     signals[lowTF].ContainsKey(code.LowSignalSell) && signals[lowTF][code.LowSignalSell].Active && signals[lowTF][code.LowSignalSell].Age <= maxSignalAge;
                    if (sellMatch)
                    {
                        GlobalData.AddTextToLogTab($"{symbolName} MTF Code {code.Code} Sell Confirmed on {lowTF} (High: {highTF} {code.HighSignalSell}, Middle: {middleTF} {code.MiddleSignalSell}, Low: {lowTF} {code.LowSignalSell}) - Strong Reversal Setup!");
                    }
                }
            }
        }
    }

}