using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using Skender.Stock.Indicators;

// Translation of a MQ4/MQ5 script found by Grok concerning BBMA (which looks nice)
// TODO: lost the link with the source?
// Strange calculation..?


namespace BbmaStrategyGrok3
{
    public enum TradeType
    {
        //Swing, // we dont have monthly candles
        MediumSwing,
        IntradaySwing,
        Intraday,
        Scalping
    }

    public class OHLC
    {
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
    }

    public class BBMAOmaAllyEA
    {
        // Input parameters
        public int Lookback { get; set; } = 6;
        public TradeType SelectedTradeType { get; set; } = TradeType.MediumSwing;

        // Results
        public string EntrySignal { get; private set; } = string.Empty;
        public string ExitSignal { get; private set; } = string.Empty;


        // MTF timeframes per trade type
        private readonly Dictionary<TradeType, CryptoIntervalPeriod[]> _timeframes = new()
        {
            //{ TradeType.Swing, new[] { CryptoIntervalPeriod.Interval1Month, CryptoIntervalPeriod.interval1w, CryptoIntervalPeriod.interval1d, CryptoIntervalPeriod.interval4h } },
            { TradeType.MediumSwing, new[] { CryptoIntervalPeriod.interval1w, CryptoIntervalPeriod.interval1d, CryptoIntervalPeriod.interval4h, CryptoIntervalPeriod.interval1h } },
            { TradeType.IntradaySwing, new[] { CryptoIntervalPeriod.interval1d, CryptoIntervalPeriod.interval4h, CryptoIntervalPeriod.interval1h, CryptoIntervalPeriod.interval15m } },
            { TradeType.Intraday, new[] { CryptoIntervalPeriod.interval4h, CryptoIntervalPeriod.interval1h, CryptoIntervalPeriod.interval15m, CryptoIntervalPeriod.interval5m } },
            { TradeType.Scalping, new[] { CryptoIntervalPeriod.interval1h, CryptoIntervalPeriod.interval15m, CryptoIntervalPeriod.interval5m, CryptoIntervalPeriod.interval1m } }
        };

        class IntervalData : 
              Dictionary<CryptoIntervalPeriod, (IEnumerable<BollingerBandsResult> bb,
                List<EmaResult> ema,
                List<WmaResult> ma5High, List<WmaResult> ma5Low,
                List<WmaResult> ma10High, List<WmaResult> ma10Low,
                List<AtrResult> atr)>
        {
        }

        public void Compute(CryptoSymbol symbol)
        {
            // Get current TF data (assume lowest TF is current)
            CryptoIntervalPeriod[] tfs = _timeframes[SelectedTradeType];

            // Compute the indicators for all the TFs
            var indicators = new IntervalData();
            foreach (var tf in tfs)
            {
                var tfData = symbol.GetSymbolInterval(tf).CandleList;
                var candles = ToCandles(tfData);

                var bb = candles.GetBollingerBands(20, 2.0).ToList();
                var ema50 = candles.GetEma(50).ToList();
                var atr = candles.GetAtr(14).ToList();

                var highcandles = candles.Select(q => new Quote { Date = q.Date, Close = q.High }).ToList();
                var lowcandles = candles.Select(q => new Quote { Date = q.Date, Close = q.Low }).ToList();
                var ma5High = highcandles.GetWma(5).ToList();
                var ma5Low = lowcandles.GetWma(5).ToList();
                var ma10High = highcandles.GetWma(10).ToList();
                var ma10Low = lowcandles.GetWma(10).ToList();

                indicators[tf] = (bb, ema50, ma5High, ma5Low, ma10High, ma10Low, atr);
            }

            // to lowwest timeframe (the last tfs)
            CryptoIntervalPeriod currentTf = tfs.Last();
            var currentData = symbol.GetSymbolInterval(currentTf).CandleList;
            var currentcandles = ToCandles(currentData); // again?
            int idx = currentcandles.Count - 1;
            if (idx < 1) 
                return; // Need at least 2 candles for patterns

            // 1. Extreme (Early market ending signal)
            bool extremeBuy = IsExtremeBuy(indicators, tfs, idx, currentcandles);
            bool extremeSell = IsExtremeSell(indicators, tfs, idx, currentcandles);

            // 2. Mandatory TP condition
            bool tpConditionBuy = IsTpConditionBuy(indicators, tfs, idx);
            bool tpConditionSell = IsTpConditionSell(indicators, tfs, idx);

            // 3. MHV (Initial long-distance market movement setup)
            bool mhvBuy = IsMhvBuy(indicators, tfs, idx, currentcandles, extremeBuy);
            bool mhvSell = IsMhvSell(indicators, tfs, idx, currentcandles, extremeSell);

            // 4. CS Direction (Early new market movement signal, not a setup)
            bool csDirBuy = IsCsDirBuy(indicators, tfs, idx, currentcandles);
            bool csDirSell = IsCsDirSell(indicators, tfs, idx, currentcandles);

            // 5. Re-Entry
            bool reBuy = IsReBuy(indicators, tfs, idx, currentcandles);
            bool reSell = IsReSell(indicators, tfs, idx, currentcandles);

            // 6. Momentum (Market strength initiation signal)
            bool momentumBuy = IsMomentumBuy(indicators, tfs, idx, currentcandles);
            bool momentumSell = IsMomentumSell(indicators, tfs, idx, currentcandles);

            // 7. Re-Entry Momentum
            bool reMomentumBuy = IsReMomentumBuy(indicators, tfs, idx, currentcandles);
            bool reMomentumSell = IsReMomentumSell(indicators, tfs, idx, currentcandles);

            // 8. Extreme (repeated confirmation)
            bool repeatedExtremeBuy = IsRepeatedExtremeBuy(indicators, tfs, idx, currentcandles);
            bool repeatedExtremeSell = IsRepeatedExtremeSell(indicators, tfs, idx, currentcandles);

            // Determine entry/exit based on sequence
            if (extremeBuy && tpConditionBuy && mhvBuy && csDirBuy && reBuy && momentumBuy && reMomentumBuy && repeatedExtremeBuy)
            {
                EntrySignal = "Buy Entry";
                Console.WriteLine("Entry: Buy - All conditions met");
            }
            else if (extremeSell && tpConditionSell && mhvSell && csDirSell && reSell && momentumSell && reMomentumSell && repeatedExtremeSell)
            {
                EntrySignal = "Sell Entry";
                Console.WriteLine("Entry: Sell - All conditions met");
            }
            else
            {
                EntrySignal = "No Entry";
                Console.WriteLine("No entry - Conditions not fully met");
            }

            // Exit logic
            if (tpConditionBuy || tpConditionSell)
            {
                ExitSignal = "Take Profit";
                Console.WriteLine("Exit: Take Profit");
            }
            else if ((extremeBuy && !csDirBuy) || (extremeSell && !csDirSell))
            {
                ExitSignal = "Reversal Exit";
                Console.WriteLine("Exit: Reversal");
            }
        }

        // That is a lot of candles!! Reduce to ~500 or even less?
        private List<Quote> ToCandles(CryptoCandleList dict)
        {
            return dict?.OrderBy(x => x.Key).Select(x => new Quote
            {
                Date = DateTimeOffset.FromUnixTimeMilliseconds(x.Key).UtcDateTime,
                Open = x.Value.Open,
                High = x.Value.High,
                Low = x.Value.Low,
                Close = x.Value.Close
            }).ToList() ?? [];
        }
        
        /// <summary>
        /// Helper methods for conditions (implement based on description)
        /// </summary>
        private bool IsExtremeBuy(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx)
        {
            // MA outside BB, reversal candle, retest candle
            // Check across MTF
            return tfs.All(tf =>
            {
                var (bb, _, ma5High, ma5Low, ma10High, ma10Low, _) = indicators[tf];
                var bbRes = bb.ElementAtOrDefault(idx);
                var ma5L = ma5Low.ElementAtOrDefault(idx).Wma ?? 0;
                var ma10L = ma10Low.ElementAtOrDefault(idx).Wma ?? 0;
                // Add reversal and retest logic (use candle patterns)
                return ma5L < bbRes.LowerBand && IsReversalCandle(tf, idx) && IsRetestCandle(tf, idx);
            });
        }

        private bool IsReversalCandle(CryptoIntervalPeriod tf, int idx)
        {
            //if (isBuy)
            //{
            //    return IsReversalCandleBuy(candles, idx);
            //}
            //else
            //{
            //    return IsReversalCandleSell(candles, idx);
            //}
            return false;
        }

        private bool IsRetestCandle(CryptoIntervalPeriod tf, int idx)
        //List<Quote> candles, int idx, double level, bool isBuy)
        {
            //if (isBuy)
            //{
            //    return IsRetestCandleBuy(candles, idx, level);
            //}
            //else
            //{
            //    return IsRetestCandleSell(candles, idx, level);
            //}
            return false;
        }

        //// Helper to get MA based on type
        //private IEnumerable<WmaResult> GetMa(List<Quote> candles, int period)
        //{
        //    switch (type)
        //    {
        //        case MovingAvgType.SimpleMovingAverage:
        //            return candles.GetSma(period).Cast<WmaResult>();
        //        case MovingAvgType.ExponentialMovingAverage:
        //            return candles.GetEma(period).Cast<WmaResult>();
        //        case MovingAvgType.WeightedMovingAverage:
        //            return candles.GetWma(period).Cast<WmaResult>();
        //        default:
        //            return candles.GetSma(period).Cast<WmaResult>();
        //    }
        //}

        // Condition implementations
        private bool IsExtremeBuy(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles)
        {
            return tfs.All(tf =>
            {
                var (_, _, _, ma5Low, _, ma10Low, _) = indicators[tf];
                var bbRes = indicators[tf].bb.ElementAtOrDefault(idx);
                double ma5L = ma5Low.ElementAtOrDefault(idx).Wma!.Value;
                double ma10L = GetMaValue(ma10Low.ElementAtOrDefault(idx));
                bool maOutsideBb = ma5L < bbRes.LowerBand.GetValueOrDefault() || ma10L < bbRes.LowerBand.GetValueOrDefault();
                bool reversal = IsReversalCandleBuy(candles, idx);
                bool retest = IsRetestCandleBuy(candles, idx, bbRes.LowerBand.GetValueOrDefault());
                return maOutsideBb && reversal && retest;
            });
        }

        private bool IsExtremeSell(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles)
        {
            return tfs.All(tf =>
            {
                var (_, _, ma5High, _, ma10High, _, _) = indicators[tf];
                var bbRes = indicators[tf].bb.ElementAtOrDefault(idx);
                double ma5H = GetMaValue(ma5High.ElementAtOrDefault(idx));
                double ma10H = GetMaValue(ma10High.ElementAtOrDefault(idx));
                bool maOutsideBb = ma5H > bbRes.UpperBand.GetValueOrDefault() || ma10H > bbRes.UpperBand.GetValueOrDefault();
                bool reversal = IsReversalCandleSell(candles, idx);
                bool retest = IsRetestCandleSell(candles, idx, bbRes.UpperBand.GetValueOrDefault());
                return maOutsideBb && reversal && retest;
            });
        }

        private bool IsTpConditionBuy(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx)
        {
            return tfs.All(tf =>
            {
                var (_, _, _, ma5Low, _, ma10Low, _) = indicators[tf];
                var bbRes = indicators[tf].bb.ElementAtOrDefault(idx);
                double ma5L = GetMaValue(ma5Low.ElementAtOrDefault(idx));
                double ma10L = GetMaValue(ma10Low.ElementAtOrDefault(idx));
                double midBb = bbRes.Sma.GetValueOrDefault();
                return (ma5L >= midBb || ma10L >= midBb); // Opposing for buy (MAs above mid in uptrend?)
            });
        }


        private bool IsTpConditionSell(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx)
        {
            return tfs.All(tf =>
            {
                var (_, _, ma5High, _, ma10High, _, _) = indicators[tf];
                var bbRes = indicators[tf].bb.ElementAtOrDefault(idx);
                double ma5H = GetMaValue(ma5High.ElementAtOrDefault(idx));
                double ma10H = GetMaValue(ma10High.ElementAtOrDefault(idx));
                double midBb = bbRes.Sma.GetValueOrDefault();
                return (ma5H <= midBb || ma10H <= midBb);
            });
        }

        private bool IsMhvBuy(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles, bool extremeBuy)
        {
            if (!extremeBuy) return false;
            return tfs.All(tf =>
            {
                var bbRes = indicators[tf].bb.ElementAtOrDefault(idx);
                bool closeNotSurpass = candles[idx].Close > (decimal)bbRes.LowerBand.GetValueOrDefault();
                bool reversal = IsReversalCandleBuy(candles, idx);
                bool retest = IsRetestCandleBuy(candles, idx, bbRes.LowerBand.GetValueOrDefault());
                return closeNotSurpass && reversal && retest;
            });
        }

        private bool IsMhvSell(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles, bool extremeSell)
        {
            if (!extremeSell) return false;
            return tfs.All(tf =>
            {
                var bbRes = indicators[tf].bb.ElementAtOrDefault(idx);
                bool closeNotSurpass = candles[idx].Close < (decimal)bbRes.UpperBand.GetValueOrDefault();
                bool reversal = IsReversalCandleSell(candles, idx);
                bool retest = IsRetestCandleSell(candles, idx, bbRes.UpperBand.GetValueOrDefault());
                return closeNotSurpass && reversal && retest;
            });
        }

        private bool IsCsDirBuy(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles)
        {
            return tfs.All(tf =>
            {
                var (_, _, _, ma5Low, _, ma10Low, _) = indicators[tf];
                double ma5L = GetMaValue(ma5Low.ElementAtOrDefault(idx));
                double ma10L = GetMaValue(ma10Low.ElementAtOrDefault(idx));
                var bbRes = indicators[tf].bb.ElementAtOrDefault(idx);
                bool breakMa = candles[idx].Close > (decimal)Math.Max(ma5L, ma10L);
                bool strong = candles[idx].Close > (decimal)bbRes.Sma.GetValueOrDefault();
                return breakMa && strong;
            });
        }


        private bool IsCsDirSell(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles)
        {
            return tfs.All(tf =>
            {
                var (_, _, ma5High, _, ma10High, _, _) = indicators[tf];
                double ma5H = GetMaValue(ma5High.ElementAtOrDefault(idx));
                double ma10H = GetMaValue(ma10High.ElementAtOrDefault(idx));
                var bbRes = indicators[tf].bb.ElementAtOrDefault(idx);
                bool breakMa = candles[idx].Close < (decimal)Math.Min(ma5H, ma10H);
                bool strong = candles[idx].Close < (decimal)bbRes.Sma.GetValueOrDefault();
                return breakMa && strong;
            });
        }

        private bool IsReBuy(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles)
        {
            return tfs.All(tf =>
            {
                var (_, _, _, ma5Low, _, ma10Low, _) = indicators[tf];
                double ma5L = GetMaValue(ma5Low.ElementAtOrDefault(idx));
                double ma10L = GetMaValue(ma10Low.ElementAtOrDefault(idx));
                var bbRes = indicators[tf].bb.ElementAtOrDefault(idx);
                bool notSurpassMa = candles[idx].Close >= (decimal)Math.Max(ma5L, ma10L);
                bool withinMid = candles[idx].Close >= (decimal)bbRes.Sma.GetValueOrDefault();
                return notSurpassMa && withinMid;
            });
        }

        private bool IsReSell(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles)
        {
            return tfs.All(tf =>
            {
                var (_, _, ma5High, _, ma10High, _, _) = indicators[tf];
                double ma5H = GetMaValue(ma5High.ElementAtOrDefault(idx));
                double ma10H = GetMaValue(ma10High.ElementAtOrDefault(idx));
                var bbRes = indicators[tf].bb.ElementAtOrDefault(idx);
                bool notSurpassMa = candles[idx].Close <= (decimal)Math.Min(ma5H, ma10H);
                bool withinMid = candles[idx].Close <= (decimal)bbRes.Sma.GetValueOrDefault();
                return notSurpassMa && withinMid;
            });
        }

        private bool IsMomentumBuy(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles)
        {
            return tfs.All(tf =>
            {
                var bbRes = indicators[tf].bb.ElementAtOrDefault(idx);
                return candles[idx].Close > (decimal)bbRes.UpperBand.GetValueOrDefault();
            });
        }

        private bool IsMomentumSell(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles)
        {
            return tfs.All(tf =>
            {
                var bbRes = indicators[tf].bb.ElementAtOrDefault(idx);
                return candles[idx].Close < (decimal)bbRes.LowerBand.GetValueOrDefault();
            });
        }

        private bool IsReMomentumBuy(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles)
        {
            return tfs.All(tf =>
            {
                var (_, _, _, ma5Low, _, ma10Low, _) = indicators[tf];
                double ma5L = GetMaValue(ma5Low.ElementAtOrDefault(idx));
                double ma10L = GetMaValue(ma10Low.ElementAtOrDefault(idx));
                decimal close = candles[idx].Close;
                return close > (decimal)ma5L && close > (decimal)ma10L; // Near MA for re-entry momentum
            });
        }

        private bool IsReMomentumSell(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles)
        {
            return tfs.All(tf =>
            {
                var (_, _, ma5High, _, ma10High, _, _) = indicators[tf];
                double ma5H = GetMaValue(ma5High.ElementAtOrDefault(idx));
                double ma10H = GetMaValue(ma10High.ElementAtOrDefault(idx));
                decimal close = candles[idx].Close;
                return close < (decimal)ma5H && close < (decimal)ma10H;
            });
        }

        private bool IsRepeatedExtremeBuy(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles)
        {
            // Similar to ExtremeBuy, assume repeated if occurred in lookback
            return IsExtremeBuy(indicators, tfs, idx, candles);
        }

        private bool IsRepeatedExtremeSell(IntervalData indicators, CryptoIntervalPeriod[] tfs, int idx, List<Quote> candles)
        {
            return IsExtremeSell(indicators, tfs, idx, candles);
        }

        private double GetMaValue(WmaResult result)
        {
            //if (result is SmaResult sma) return sma.Sma.GetValueOrDefault();
            //if (result is EmaResult ema) return ema.Ema.GetValueOrDefault();
            if (result is WmaResult wma) return wma.Wma.GetValueOrDefault();
            return 0;
        }

        private bool IsReversalCandleBuy(List<Quote> candles, int idx)
        {
            // Example: Hammer - long lower wick, small body
            decimal open = candles[idx].Open;
            decimal close = candles[idx].Close;
            decimal high = candles[idx].High;
            decimal low = candles[idx].Low;
            decimal body = Math.Abs(close - open);
            decimal lowerWick = Math.Min(open, close) - low;
            return lowerWick > 2 * body && high - Math.Max(open, close) < body;
        }

        private bool IsReversalCandleSell(List<Quote> candles, int idx)
        {
            // Example: Shooting star - long upper wick, small body
            decimal open = candles[idx].Open;
            decimal close = candles[idx].Close;
            decimal high = candles[idx].High;
            decimal low = candles[idx].Low;
            decimal body = Math.Abs(close - open);
            decimal upperWick = high - Math.Max(open, close);
            return upperWick > 2 * body && Math.Min(open, close) - low < body;
        }

        private bool IsRetestCandleBuy(List<Quote> candles, int idx, double level)
        {
            // Candle touches level and bounces
            return candles[idx].Low <= (decimal)level && candles[idx].Close > (decimal)level;
        }

        private bool IsRetestCandleSell(List<Quote> candles, int idx, double level)
        {
            return candles[idx].High >= (decimal)level && candles[idx].Close < (decimal)level;
        }

    }
}