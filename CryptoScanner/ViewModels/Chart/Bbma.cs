using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
#if DEBUG
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal.Bbma;
#endif

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

public class Bbma
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval,
        CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesWma5High = new LineSeries
        {
            Title = "wma5high",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkRed,
            Color = OxyColors.DarkRed,
            Tag = group,
        };
        var seriesWma10High = new LineSeries
        {
            Title = "wma10high",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkRed,
            Color = OxyColors.DarkRed,
            Tag = group,
        };

        var seriesWma5Low = new LineSeries
        {
            Title = "wma5low",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkGreen,
            Color = OxyColors.DarkGreen,
            Tag = group,
        };
        var seriesWma10Low = new LineSeries
        {
            Title = "wma10low",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkGreen,
            Color = OxyColors.DarkGreen,
            Tag = group,
        };

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        var candles = symbolInterval.CandleList.Values;
        List<WmaResult> wmaList05Low = (List<WmaResult>)candles.Use(CandlePart.Low).GetWma(05);
        List<WmaResult> wmaList05High = (List<WmaResult>)candles.Use(CandlePart.High).GetWma(05);
        List<WmaResult> wmaList10Low = (List<WmaResult>)candles.Use(CandlePart.Low).GetWma(10);
        List<WmaResult> wmaList10High = (List<WmaResult>)candles.Use(CandlePart.High).GetWma(10);

        List<BollingerBandsResult> bollingerBandsList = (List<BollingerBandsResult>)candles.GetBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length, standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);

        // Filled band between WMA5-High and WMA10-High — dark red background.
        // Filled band between WMA5-Low and WMA10-Low — dark green background.
        // Both inserted at index 0 so they render behind candles and all other series.
        var seriesBandHigh = new AreaSeries
        {
            Title = "wma high band",
            Fill = OxyColor.FromArgb(120, 139, 0, 0),   // semi-transparent dark red
            Color = OxyColors.Transparent,
            StrokeThickness = 0,
            Tag = group,
        };
        var seriesBandLow = new AreaSeries
        {
            Title = "wma low band",
            Fill = OxyColor.FromArgb(120, 0, 100, 0),   // semi-transparent dark green
            Color = OxyColors.Transparent,
            StrokeThickness = 0,
            Tag = group,
        };

        // Scatter signals for extreme conditions (wma crosses outside Bollinger Band).
        // Extreme-A: only wma5 outside the band. Magic extreme: wma10 also outside the band.
        var seriesExtremeAHigh = new ScatterSeries
        {
            Title = "extreme-A high",
            MarkerSize = 4,
            MarkerFill = OxyColors.Red,
            MarkerType = MarkerType.Triangle,
            Tag = group,
        };
        var seriesMagicExtremeHigh = new ScatterSeries
        {
            Title = "magic extreme high",
            MarkerSize = 4,
            MarkerFill = OxyColors.OrangeRed,
            MarkerType = MarkerType.Triangle,
            Tag = group,
        };
        var seriesExtremeALow = new ScatterSeries
        {
            Title = "extreme-A low",
            MarkerSize = 4,
            MarkerFill = OxyColors.Yellow,
            MarkerType = MarkerType.Triangle,
            Tag = group,
        };
        var seriesMagicExtremeLow = new ScatterSeries
        {
            Title = "magic extreme low",
            MarkerSize = 4,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Triangle,
            Tag = group,
        };

        // -----------------------------------------------------------------------
        // OmniView state series
        //
        // "Important" states (large markers, placed just outside the candle body):
        //   - Extreme Buy  : yellow triangle  below   Extreme Sell  : orange-red triangle  above
        //   - TPW Buy      : lime circle      below   TPW Sell      : orange circle        above
        //   - MHV Buy      : cyan diamond     below   MHV Sell      : magenta diamond      above
        //   - Reentry Buy  : white square     below   Reentry Sell  : light-blue square    above
        //
        // "Intermediate" states (small gray dots, 1-bar offset below / above):
        //   CSD / CSAK2 / CSAA / CSM / Cross / GapBbEma50 / RejectedEma50
        // -----------------------------------------------------------------------

        var seriesOmniExtremeBuy = new ScatterSeries
        {
            Title = "omni extreme buy",
            MarkerSize = 5,
            MarkerFill = OxyColors.Yellow,
            MarkerType = MarkerType.Triangle,
            Tag = group,
        };
        var seriesOmniExtremeSell = new ScatterSeries
        {
            Title = "omni extreme sell",
            MarkerSize = 5,
            MarkerFill = OxyColors.OrangeRed,
            MarkerType = MarkerType.Triangle,
            Tag = group,
        };

        var seriesOmniTpwBuy = new ScatterSeries
        {
            Title = "omni tpw buy",
            MarkerSize = 5,
            MarkerFill = OxyColors.LimeGreen,
            MarkerType = MarkerType.Circle,
            Tag = group,
        };
        var seriesOmniTpwSell = new ScatterSeries
        {
            Title = "omni tpw sell",
            MarkerSize = 5,
            MarkerFill = OxyColors.Orange,
            MarkerType = MarkerType.Circle,
            Tag = group,
        };

        var seriesOmniMhvBuy = new ScatterSeries
        {
            Title = "omni mhv buy",
            MarkerSize = 5,
            MarkerFill = OxyColors.Cyan,
            MarkerType = MarkerType.Diamond,
            Tag = group,
        };
        var seriesOmniMhvSell = new ScatterSeries
        {
            Title = "omni mhv sell",
            MarkerSize = 5,
            MarkerFill = OxyColors.Magenta,
            MarkerType = MarkerType.Diamond,
            Tag = group,
        };

        var seriesOmniReentryBuy = new ScatterSeries
        {
            Title = "omni reentry buy",
            MarkerSize = 4,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Square,
            Tag = group,
        };
        var seriesOmniReentrySell = new ScatterSeries
        {
            Title = "omni reentry sell",
            MarkerSize = 4,
            MarkerFill = OxyColor.FromArgb(255, 173, 216, 230),  // light blue
            MarkerType = MarkerType.Square,
            Tag = group,
        };

        // Intermediate states: small gray dots, same for buy and sell (position tells direction)
        var seriesOmniIntermediateBuy = new ScatterSeries
        {
            Title = "omni intermediate buy",
            MarkerSize = 2,
            MarkerFill = OxyColor.FromArgb(200, 160, 160, 160),  // semi-transparent gray
            MarkerType = MarkerType.Circle,
            Tag = group,
        };
        var seriesOmniIntermediateSell = new ScatterSeries
        {
            Title = "omni intermediate sell",
            MarkerSize = 2,
            MarkerFill = OxyColor.FromArgb(200, 160, 160, 160),  // semi-transparent gray
            MarkerType = MarkerType.Circle,
            Tag = group,
        };


        foreach (var (wma5, wma10, bb) in Enumerable.Zip(wmaList05High, wmaList10High, bollingerBandsList))
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(wma5.Date, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (wma5.Wma.HasValue && wma10.Wma.HasValue)
                {
                    seriesBandHigh.Points.Add(new DataPoint(openTime.Minutes, wma5.Wma.Value));
                    seriesBandHigh.Points2.Add(new DataPoint(openTime.Minutes, wma10.Wma.Value));
                }
                if (wma5.Wma.HasValue)
                {
                    seriesWma5High.Points.Add(new DataPoint(openTime.Minutes, wma5.Wma.Value));
                    // Extreme-A: wma5 high above BB upper band
                    //if (bb.UpperBand.HasValue && wma5.Wma.Value > bb.UpperBand.Value)
                    //    seriesExtremeAHigh.Points.Add(new ScatterPoint(openTime.Minutes, 1.005 * wma5.Wma.Value));
                }
                if (wma10.Wma.HasValue)
                {
                    seriesWma10High.Points.Add(new DataPoint(openTime.Minutes, wma10.Wma.Value));
                    // Magic extreme: wma10 high also above BB upper band
                    //if (bb.UpperBand.HasValue && wma10.Wma.Value > bb.UpperBand.Value)
                    //    seriesMagicExtremeHigh.Points.Add(new ScatterPoint(openTime.Minutes, 1.005 * wma10.Wma.Value));
                }
            }
        }

        foreach (var (wma5, wma10, bb) in Enumerable.Zip(wmaList05Low, wmaList10Low, bollingerBandsList))
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(wma5.Date, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (wma5.Wma.HasValue && wma10.Wma.HasValue)
                {
                    seriesBandLow.Points.Add(new DataPoint(openTime.Minutes, wma5.Wma.Value));
                    seriesBandLow.Points2.Add(new DataPoint(openTime.Minutes, wma10.Wma.Value));
                }
                if (wma5.Wma.HasValue)
                {
                    seriesWma5Low.Points.Add(new DataPoint(openTime.Minutes, wma5.Wma.Value));
                    // Extreme-A: wma5 low below BB lower band
                    //if (bb.LowerBand.HasValue && wma5.Wma.Value < bb.LowerBand.Value)
                    //    seriesExtremeALow.Points.Add(new ScatterPoint(openTime.Minutes, 0.995 * wma5.Wma.Value));
                }
                if (wma10.Wma.HasValue)
                {
                    seriesWma10Low.Points.Add(new DataPoint(openTime.Minutes, wma10.Wma.Value));
                    // Magic extreme: wma10 low also below BB lower band
                    //if (bb.LowerBand.HasValue && wma10.Wma.Value < bb.LowerBand.Value)
                    //    seriesMagicExtremeLow.Points.Add(new ScatterPoint(openTime.Minutes, 0.995 * wma10.Wma.Value));
                }
            }
        }


#if DEBUG
        // Build indicator data ending at maxDate.
        // Add 200 extra bars beyond the view window so SMA200 (and all slower indicators) are
        // fully warmed up for every displayed candle. Without the extra history, SMA200 is null
        // for all displayed bars and the Sma200==null warmup gate skips all state calculations.
        CryptoIndicatorDataList indicatorDataList = [];
        int count = (int)((maxDate.Minutes - minDate.Minutes + 1) / interval.Duration);
        indicatorDataList.PrepareIndicators(symbol, interval, maxDate, count + 200);

        // Build OmniView classifiers for this symbol/interval.
        // GetPrevCandle uses IndicatorData (the base-interval CryptoIndicatorData) to walk back
        // through history. IndicatorDataList is needed for multi-TF calls (not used in GetOmniState
        // itself, but required by the base class initialiser).
        SignalBbmaOmniLong? longClassifier = null;
        SignalBbmaOmniShort? shortClassifier = null;

        if (indicatorDataList.TryGetValue(interval.IntervalPeriod, out CryptoIndicatorData? indicatorData)
            && indicatorData != null)
        {
            // Use the last calculated candle as the classifier seed (always valid).
            MyData seedCandle = new() { Candle = indicatorData.LastCandle, CandleData = indicatorData.LastCandleData };

            longClassifier = new SignalBbmaOmniLong
            {
                Symbol = symbol,
                Interval = interval,
                SymbolInterval = symbolInterval,
                SignalSide = CryptoTradeSide.Long,
                SignalStrategy = CryptoSignalStrategy.BbmaOmni,
                CandleLast = seedCandle,
                IndicatorData = indicatorData,
                IndicatorDataList = indicatorDataList,
            };
            shortClassifier = new SignalBbmaOmniShort
            {
                Symbol = symbol,
                Interval = interval,
                SymbolInterval = symbolInterval,
                SignalSide = CryptoTradeSide.Short,
                SignalStrategy = CryptoSignalStrategy.BbmaOmni,
                CandleLast = seedCandle,
                IndicatorData = indicatorData,
                IndicatorDataList = indicatorDataList,
            };

            // Build the forward-pass TPW caches (matches MQ5 tpwbuy/tpwsell exactly).
            // Both classifiers are available here, so cross-reset delegates are wired.
            longClassifier.BuildTpwCache(indicatorData, d => shortClassifier.IsExtremeSellBar(d));
            shortClassifier.BuildTpwCache(indicatorData, d => longClassifier.IsExtremeBuyBar(d));
        }

        if (longClassifier != null && shortClassifier != null)
        {
            // Iterate ALL candles in chronological order (oldest → newest) so prevData is always
            // the bar immediately before the current one — needed for MHV fractal confirmation.
            // Points are only added for candles inside [minDate, maxDate].
            MyData? prevData = null;

            foreach (var candle in candles)
            {
                CandleTime openTime = candle.OpenTime;

                if (!indicatorDataList.TryGetCandle(interval, candle.OpenTime, out MyData? newData) || newData == null)
                {
                    prevData = null; // chain broken — no indicator data for this bar
                    continue;
                }

                if (newData.CandleData == null || newData.CandleData.Sma200 == null)
                {
                    prevData = null; // indicator warmup not complete; reset the prev/next chain
                    continue;
                }

                try
                {
                    // MHV fires at prevData (cursor) once newData (next) confirms the fractal.
                    // Plot the marker at prevData's position, only when it falls in [minDate, maxDate].
                    if (prevData != null
                        && prevData.Candle.OpenTime >= minDate
                        && prevData.Candle.OpenTime <= maxDate)
                    {
                        CandleTime prevTime = prevData.Candle.OpenTime;
                        double prevLow = (double)prevData.Candle.Low;
                        double prevLB = prevData.CandleData.BollingerBandsLowerBand!.Value;
                        double prevHigh = (double)prevData.Candle.High;
                        double prevUB = prevData.CandleData.BollingerBandsUpperBand!.Value;

                        if (longClassifier.IsMhvBuy(prevData, newData))
                            seriesOmniMhvBuy.Points.Add(new ScatterPoint(
                                prevTime.Minutes, 0.993 * Math.Min(prevLB, prevLow)));

                        if (shortClassifier.IsMhvSell(prevData, newData))
                            seriesOmniMhvSell.Points.Add(new ScatterPoint(
                                prevTime.Minutes, 1.007 * Math.Max(prevUB, prevHigh)));
                    }

                    // Classify all other states for the current bar (only when inside the view window)
                    if (openTime >= minDate && openTime <= maxDate)
                    {
                        double low = (double)newData.Candle.Low;
                        double lowerB = newData.CandleData.BollingerBandsLowerBand!.Value;
                        double minY = Math.Min(lowerB, low);

                        double high = (double)newData.Candle.High;
                        double upperB = newData.CandleData.BollingerBandsUpperBand!.Value;
                        double maxY = Math.Max(upperB, high);

                        // --- Long (buy) states — marker placed below the candle ---
                        var longState = longClassifier.GetOmniState(newData);
                        switch (longState)
                        {
                            case SignalBbmaOmniBase.OmniState.Extreme:
                                seriesOmniExtremeBuy.Points.Add(new ScatterPoint(openTime.Minutes, 0.993 * minY));
                                break;
                            case SignalBbmaOmniBase.OmniState.Tpw:
                                seriesOmniTpwBuy.Points.Add(new ScatterPoint(openTime.Minutes, 0.993 * minY));
                                break;
                            case SignalBbmaOmniBase.OmniState.Reentry:
                                seriesOmniReentryBuy.Points.Add(new ScatterPoint(openTime.Minutes, 0.993 * minY));
                                break;
                            case SignalBbmaOmniBase.OmniState.Csd:
                            case SignalBbmaOmniBase.OmniState.Csak2:
                            case SignalBbmaOmniBase.OmniState.Csaa:
                            case SignalBbmaOmniBase.OmniState.Csm:
                            case SignalBbmaOmniBase.OmniState.Cross:
                            case SignalBbmaOmniBase.OmniState.GapBbEma50:
                            case SignalBbmaOmniBase.OmniState.RejectedEma50:
                                seriesOmniIntermediateBuy.Points.Add(new ScatterPoint(openTime.Minutes, 0.993 * minY));
                                break;
                        }

                        // --- Short (sell) states — marker placed above the candle ---
                        var shortState = shortClassifier.GetOmniState(newData);
                        switch (shortState)
                        {
                            case SignalBbmaOmniBase.OmniState.Extreme:
                                seriesOmniExtremeSell.Points.Add(new ScatterPoint(openTime.Minutes, 1.007 * maxY));
                                break;
                            case SignalBbmaOmniBase.OmniState.Tpw:
                                seriesOmniTpwSell.Points.Add(new ScatterPoint(openTime.Minutes, 1.007 * maxY));
                                break;
                            case SignalBbmaOmniBase.OmniState.Reentry:
                                seriesOmniReentrySell.Points.Add(new ScatterPoint(openTime.Minutes, 1.007 * maxY));
                                break;
                            case SignalBbmaOmniBase.OmniState.Csd:
                            case SignalBbmaOmniBase.OmniState.Csak2:
                            case SignalBbmaOmniBase.OmniState.Csaa:
                            case SignalBbmaOmniBase.OmniState.Csm:
                            case SignalBbmaOmniBase.OmniState.Cross:
                            case SignalBbmaOmniBase.OmniState.GapBbEma50:
                            case SignalBbmaOmniBase.OmniState.RejectedEma50:
                                seriesOmniIntermediateSell.Points.Add(new ScatterPoint(openTime.Minutes, 1.007 * maxY));
                                break;
                        }
                    }
                }
                catch (Exception error)
                {
                    ScannerLog.Logger.Error(error, "");
                    GlobalData.AddTextToLogTab($"error showing chart {error.Message}");
                }

                prevData = newData;
            }
        }
#endif


        chart.Series.Insert(0, seriesBandLow);
        chart.Series.Insert(0, seriesBandHigh);
        chart.Series.Add(seriesWma5Low);
        chart.Series.Add(seriesWma10Low);
        chart.Series.Add(seriesWma5High);
        chart.Series.Add(seriesWma10High);
        chart.Series.Add(seriesExtremeAHigh);
        chart.Series.Add(seriesMagicExtremeHigh);
        chart.Series.Add(seriesExtremeALow);
        chart.Series.Add(seriesMagicExtremeLow);

        // OmniView states: important (large) first, then intermediate (small dots)
        chart.Series.Add(seriesOmniIntermediateBuy);
        chart.Series.Add(seriesOmniIntermediateSell);
        chart.Series.Add(seriesOmniReentryBuy);
        chart.Series.Add(seriesOmniReentrySell);
        chart.Series.Add(seriesOmniTpwBuy);
        chart.Series.Add(seriesOmniTpwSell);
        chart.Series.Add(seriesOmniMhvBuy);
        chart.Series.Add(seriesOmniMhvSell);
        chart.Series.Add(seriesOmniExtremeBuy);
        chart.Series.Add(seriesOmniExtremeSell);


        var seriesEma50 = new LineSeries
        {
            Title = "ema50",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkOrange,
            Color = OxyColors.DarkOrange,
            Tag = group,
        };

        List<EmaResult> emaList = (List<EmaResult>)candles.Use(CandlePart.Close).GetEma(50);

        foreach (var ema in emaList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(ema.Date, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (ema.Ema.HasValue)
                    seriesEma50.Points.Add(new DataPoint(openTime.Minutes, ema.Ema.Value));
            }
        }
        chart.Series.Add(seriesEma50);
    }

}
