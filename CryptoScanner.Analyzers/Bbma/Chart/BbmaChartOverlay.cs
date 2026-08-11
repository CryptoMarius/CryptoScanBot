using CryptoScanner.Analyzers.Bbma.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.Bbma.Chart;

// Moved from CryptoScanner.Chart (ViewModels/Chart/Bbma.cs) into the Bbma plugin as an
// IChartOverlay, so the chart host no longer references the BBMA signal classes directly
// and the overlay gets its own checkbox in the chart's overlay list.
public class BbmaChartOverlay : IChartOverlay
{
    public string Label => "BBMA";
    public string GroupKey => "bbma";
#pragma warning disable CS0067 // Required by IChartOverlay; raised externally when needed
    public event Action? RequestRedraw;
#pragma warning restore CS0067

    public IReadOnlyList<ChartOverlaySeries> GetSeries(CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles)
    {
        if (candles.Count == 0)
            return [];

        var quotes = candles.AsQuotes();
        var wma05Low = quotes.Use(CandlePart.Low).ToWma(5).ToList();
        var wma05High = quotes.Use(CandlePart.High).ToWma(5).ToList();
        var wma10Low = quotes.Use(CandlePart.Low).ToWma(10).ToList();
        var wma10High = quotes.Use(CandlePart.High).ToWma(10).ToList();
        var ema50 = quotes.ToEma(50).ToList();

        var s5High = new ChartOverlaySeries { Key = "bbmaWma5High", Label = "WMA5 high", Color = "#c62828" };
        var s10High = new ChartOverlaySeries { Key = "bbmaWma10High", Label = "WMA10 high", Color = "#c62828", LineStyle = 2 };
        var s5Low = new ChartOverlaySeries { Key = "bbmaWma5Low", Label = "WMA5 low", Color = "#2e7d32" };
        var s10Low = new ChartOverlaySeries { Key = "bbmaWma10Low", Label = "WMA10 low", Color = "#2e7d32", LineStyle = 2 };
        var sEma = new ChartOverlaySeries { Key = "bbmaEma50", Label = "EMA50", Color = "#ef6c00", LineWidth = 2 };

        for (int i = 0; i < candles.Count; i++)
        {
            long time = CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration).ToUnixSeconds();

            if (i < wma05High.Count && wma05High[i].Wma.HasValue)
                s5High.Points.Add(new ChartOverlayPoint { Time = time, Value = wma05High[i].Wma!.Value });
            if (i < wma10High.Count && wma10High[i].Wma.HasValue)
                s10High.Points.Add(new ChartOverlayPoint { Time = time, Value = wma10High[i].Wma!.Value });
            if (i < wma05Low.Count && wma05Low[i].Wma.HasValue)
                s5Low.Points.Add(new ChartOverlayPoint { Time = time, Value = wma05Low[i].Wma!.Value });
            if (i < wma10Low.Count && wma10Low[i].Wma.HasValue)
                s10Low.Points.Add(new ChartOverlayPoint { Time = time, Value = wma10Low[i].Wma!.Value });
            if (i < ema50.Count && ema50[i].Ema.HasValue)
                sEma.Points.Add(new ChartOverlayPoint { Time = time, Value = ema50[i].Ema!.Value });
        }

        return [s5High, s10High, s5Low, s10Low, sEma];
    }

    public void Draw(object plotModel, CryptoSymbol symbol, CryptoInterval interval,
        List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        Draw((PlotModel)plotModel, symbol, interval, candles, minDate, maxDate, group);
    }

    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval,
        List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesWma5High = new LineSeries
        {
            Title = "wma5high",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkRed,
            Color = OxyColors.DarkRed,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesWma10High = new LineSeries
        {
            Title = "wma10high",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkRed,
            Color = OxyColors.DarkRed,
            YAxisKey = "price",
            Tag = group,
        };

        var seriesWma5Low = new LineSeries
        {
            Title = "wma5low",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkGreen,
            Color = OxyColors.DarkGreen,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesWma10Low = new LineSeries
        {
            Title = "wma10low",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkGreen,
            Color = OxyColors.DarkGreen,
            YAxisKey = "price",
            Tag = group,
        };

        if (candles.Count == 0)
            return;

        IReadOnlyList<WmaResult> wmaList05Low = candles.AsQuotes().Use(CandlePart.Low).ToWma(05).ToList();
        IReadOnlyList<WmaResult> wmaList05High = candles.AsQuotes().Use(CandlePart.High).ToWma(05).ToList();
        IReadOnlyList<WmaResult> wmaList10Low = candles.AsQuotes().Use(CandlePart.Low).ToWma(10).ToList();
        IReadOnlyList<WmaResult> wmaList10High = candles.AsQuotes().Use(CandlePart.High).ToWma(10).ToList();

        IReadOnlyList<BollingerBandsResult> bollingerBandsList = candles.AsQuotes().ToBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length, standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation).ToList();

        // Filled band between WMA5-High and WMA10-High — dark red background.
        // Filled band between WMA5-Low and WMA10-Low — dark green background.
        // Both inserted at index 0 so they render behind candles and all other series.
        var seriesBandHigh = new AreaSeries
        {
            Title = "wma high band",
            Fill = OxyColor.FromArgb(120, 139, 0, 0),   // semi-transparent dark red
            Color = OxyColors.Transparent,
            StrokeThickness = 0,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesBandLow = new AreaSeries
        {
            Title = "wma low band",
            Fill = OxyColor.FromArgb(120, 0, 100, 0),   // semi-transparent dark green
            Color = OxyColors.Transparent,
            StrokeThickness = 0,
            YAxisKey = "price",
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
            YAxisKey = "price",
            Tag = group,
        };
        var seriesMagicExtremeHigh = new ScatterSeries
        {
            Title = "magic extreme high",
            MarkerSize = 4,
            MarkerFill = OxyColors.OrangeRed,
            MarkerType = MarkerType.Triangle,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesExtremeALow = new ScatterSeries
        {
            Title = "extreme-A low",
            MarkerSize = 4,
            MarkerFill = OxyColors.Yellow,
            MarkerType = MarkerType.Triangle,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesMagicExtremeLow = new ScatterSeries
        {
            Title = "magic extreme low",
            MarkerSize = 4,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Triangle,
            YAxisKey = "price",
            Tag = group,
        };

        // -----------------------------------------------------------------------
        // OmniView state series
        //
        // Convention: COLOR encodes direction (LimeGreen = buy, Red = sell),
        //             SHAPE encodes which state it is.
        //
        // "Important" states (large markers, placed just outside the candle body):
        //   - Extreme   → Triangle   (buy below, sell above)
        //   - TPW       → Circle
        //   - MHV       → Diamond
        //   - Reentry   → Square
        //
        // "Intermediate" states (small gray dots, 1-bar offset below / above):
        //   CSD / CSAK2 / CSAA / CSM / Cross / GapBbEma50 / RejectedEma50
        //
        // The authoritative reference for both the chart symbol legend AND the
        // OLD-vs-NEW BBMA code translation (RRE / REM / REE / RMEE → RRE / REH /
        // REE / RHE) lives in SignalBbmaOmniBase.cs above the OmniState enum.
        // -----------------------------------------------------------------------

        var seriesOmniExtremeBuy = new ScatterSeries
        {
            Title = "omni extreme buy",
            MarkerSize = 5,
            MarkerFill = OxyColors.LimeGreen,
            MarkerType = MarkerType.Triangle,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesOmniExtremeSell = new ScatterSeries
        {
            Title = "omni extreme sell",
            MarkerSize = 5,
            MarkerFill = OxyColors.Red,
            MarkerType = MarkerType.Triangle,
            YAxisKey = "price",
            Tag = group,
        };

        var seriesOmniTpwBuy = new ScatterSeries
        {
            Title = "omni tpw buy",
            MarkerSize = 5,
            MarkerFill = OxyColors.LimeGreen,
            MarkerType = MarkerType.Circle,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesOmniTpwSell = new ScatterSeries
        {
            Title = "omni tpw sell",
            MarkerSize = 5,
            MarkerFill = OxyColors.Red,
            MarkerType = MarkerType.Circle,
            YAxisKey = "price",
            Tag = group,
        };

        var seriesOmniMhvBuy = new ScatterSeries
        {
            Title = "omni mhv buy",
            MarkerSize = 5,
            MarkerFill = OxyColors.LimeGreen,
            MarkerType = MarkerType.Diamond,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesOmniMhvSell = new ScatterSeries
        {
            Title = "omni mhv sell",
            MarkerSize = 5,
            MarkerFill = OxyColors.Red,
            MarkerType = MarkerType.Diamond,
            YAxisKey = "price",
            Tag = group,
        };

        var seriesOmniReentryBuy = new ScatterSeries
        {
            Title = "omni reentry buy",
            MarkerSize = 5,
            MarkerFill = OxyColors.LimeGreen,
            MarkerType = MarkerType.Square,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesOmniReentrySell = new ScatterSeries
        {
            Title = "omni reentry sell",
            MarkerSize = 5,
            MarkerFill = OxyColors.Red,
            MarkerType = MarkerType.Square,
            YAxisKey = "price",
            Tag = group,
        };

        // Intermediate states: small gray dots, same for buy and sell (position tells direction)
        var seriesOmniIntermediateBuy = new ScatterSeries
        {
            Title = "omni intermediate buy",
            MarkerSize = 2,
            MarkerFill = OxyColor.FromArgb(200, 160, 160, 160),  // semi-transparent gray
            MarkerType = MarkerType.Circle,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesOmniIntermediateSell = new ScatterSeries
        {
            Title = "omni intermediate sell",
            MarkerSize = 2,
            MarkerFill = OxyColor.FromArgb(200, 160, 160, 160),  // semi-transparent gray
            MarkerType = MarkerType.Circle,
            YAxisKey = "price",
            Tag = group,
        };


        foreach (var (wma5, wma10, bb) in Enumerable.Zip(wmaList05High, wmaList10High, bollingerBandsList))
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(wma5.Timestamp, interval.Duration);
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
            CandleTime openTime = CandleTime.AlignFromDateTime(wma5.Timestamp, interval.Duration);
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
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        // Build indicator data ending at maxDate.
        // Add 200 extra bars beyond the view window so SMA200 (and all slower indicators) are
        // fully warmed up for every displayed candle. Without the extra history, SMA200 is null
        // for all displayed bars and the Sma200==null warmup gate skips all state calculations.
        int count = (int)((maxDate.Minutes - minDate.Minutes + 1) / interval.Duration);
        IndicatorEngine.PrepareIndicators(symbol, interval, maxDate, count + 200);

        // Build OmniView classifiers for this symbol/interval. GetPrevCandle / BuildTpwCache read the
        // per-interval indicator data from symbolInterval.Data (filled by PrepareIndicators above).
        SignalBbmaOmniLong? longClassifier = null;
        SignalBbmaOmniShort? shortClassifier = null;

        if (symbolInterval.TryGetCandle(maxDate, out MyData? seedCandle) && seedCandle != null)
        {
            longClassifier = new SignalBbmaOmniLong
            {
                Symbol = symbol,
                Interval = interval,
                SymbolInterval = symbolInterval,
                SignalSide = CryptoTradeSide.Long,
                SignalStrategy = BbmaPlugin.StrategyInternal.ToLower(),
                CandleLast = seedCandle,
            };
            shortClassifier = new SignalBbmaOmniShort
            {
                Symbol = symbol,
                Interval = interval,
                SymbolInterval = symbolInterval,
                SignalSide = CryptoTradeSide.Short,
                SignalStrategy = BbmaPlugin.StrategyInternal.ToLower(),
                CandleLast = seedCandle,
            };

            // Build the forward-pass TPW caches (matches MQ5 tpwbuy/tpwsell exactly).
            // Both classifiers are available here, so cross-reset delegates are wired.
            longClassifier.BuildTpwCache(symbolInterval, d => shortClassifier.IsExtremeSellBar(d));
            shortClassifier.BuildTpwCache(symbolInterval, d => longClassifier.IsExtremeBuyBar(d));
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

                if (!symbolInterval.TryGetCandle(candle.OpenTime, out MyData? newData) || newData == null)
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
            YAxisKey = "price",
            Tag = group,
        };

        IReadOnlyList<EmaResult> emaList = candles.AsQuotes().ToEma(50).ToList();

        foreach (var ema in emaList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(ema.Timestamp, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (ema.Ema.HasValue)
                    seriesEma50.Points.Add(new DataPoint(openTime.Minutes, ema.Ema.Value));
            }
        }
        chart.Series.Add(seriesEma50);
    }

}
