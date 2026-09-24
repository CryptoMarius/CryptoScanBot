using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using CryptoScanner.Analyzers.Chart;

namespace CryptoScanner.Analyzers.Mac.Chart;

/// <summary>
/// The four MAC lines on the chart, plus the two levels the breakout trigger measures against.
/// <para>
/// The lines are EMA(20), EMA(40), SMA(50) and SMA(150) on the close - ordinary indicators, so the
/// same picture can be put next to any other charting package. Their default colours are declared
/// below and can be changed per series in the chart style screen.
/// </para>
/// </summary>
public class MacChartOverlay : IChartOverlay
{
    public string Label => "Mac Cloud";
    public string GroupKey => "mac";
#pragma warning disable CS0067 // Required by IChartOverlay; raised externally when needed
    public event Action? RequestRedraw;
#pragma warning restore CS0067

    public const string KeyFast = "macFast";
    public const string KeySecond = "macSecond";
    public const string KeyMedium = "macMedium";
    public const string KeySlow = "macSlow";
    public const string KeyResistance = "macResistance";
    public const string KeySupport = "macSupport";
    public const string KeyCloudUp = "macCloudUp";
    public const string KeyCloudDown = "macCloudDown";
    public const string KeyOpenLong = "macOpenLong";
    public const string KeyOpenShort = "macOpenShort";
    public const string KeyBreakout = "macBreakout";
    public const string KeyBreakdown = "macBreakdown";
    public const string KeyCrossUp = "macCrossUp";
    public const string KeyCrossDown = "macCrossDown";
    public const string KeyCloseLong = "macCloseLong";
    public const string KeyCloseShort = "macCloseShort";

    /// <summary>
    /// What this overlay draws and how it looks by default. The colour screen builds its MAC section
    /// from this, so the entries exist exactly as long as the plugin is registered - a strategy
    /// moved under #if DEBUG takes its colours with it instead of leaving them behind on screen.
    /// </summary>
    public IReadOnlyList<ChartOverlayStyleDefinition> StyleDefinitions { get; } =
    [
        // Matched against screenshots of the indicator this strategy is modelled on: a bright
        // turquoise for the two EMAs, a soft pink for the slow line, red and green for the levels,
        // and two muted fills at about a sixth opacity so the lines stay readable through them.
        new() { Key = KeyFast, Label = "Fast EMA", Color = "#FF4FD1C5", LineWidth = 2 },
        new() { Key = KeySecond, Label = "Second EMA", Color = "#804FD1C5" },
        new() { Key = KeyMedium, Label = "Medium SMA", Color = "#55BDBDBD" },
        new() { Key = KeySlow, Label = "Slow SMA", Color = "#FFEC7BA5", LineWidth = 2 },
        new() { Key = KeyResistance, Label = "Resistance", Color = "#FFEF5350", LineStyle = 1 },
        new() { Key = KeySupport, Label = "Support", Color = "#FF66BB6A", LineStyle = 1 },
        new() { Key = KeyCloudUp, Label = "Cloud up", Color = "#4D53A092", IsFill = true },
        new() { Key = KeyCloudDown, Label = "Cloud down", Color = "#4DB1648C", IsFill = true },
        // The two entry markers carry the colours of the Pine script, so the same marker has the
        // same colour on this chart and on TradingView. They used to be a plain green and red,
        // which read well on their own but made the pair impossible to lay side by side. The short
        // is therefore MAGENTA rather than red - that is the reference's own colour for it.
        new() { Key = KeyOpenLong, Label = "Open long marker", Color = "#FF00E676" },
        new() { Key = KeyOpenShort, Label = "Open short marker", Color = "#FFE040FB" },
        new() { Key = KeyBreakout, Label = "Breakout marker", Color = "#FFFFFFFF" },
        new() { Key = KeyBreakdown, Label = "Breakdown marker", Color = "#FFD4E157" },
        // The four colours the reference uses for these, read off its own style screen: a
        // green and a red cross for the line crossing, a blue and an orange diamond for the
        // exit.
        new() { Key = KeyCrossUp, Label = "Cross up marker", Color = "#FF00D96F" },
        new() { Key = KeyCrossDown, Label = "Cross down marker", Color = "#FFFF5252" },
        new() { Key = KeyCloseLong, Label = "Close long marker", Color = "#FF2962FF" },
        new() { Key = KeyCloseShort, Label = "Close short marker", Color = "#FFF59200" },
    ];


    /// <summary>
    /// How firmly each of the three bands is painted, as a fraction of the configured opacity:
    /// fast-to-second, second-to-medium, medium-to-slow.
    /// <para>
    /// Measured off a chart image rather than chosen. Over one background the three bands lift
    /// the pixels by 26, 36 and 47 counts in the same hue, so the outer band is the firm one
    /// and the two inner ones step back in that ratio. It is what makes the cloud read as one shape
    /// with a near edge and a far edge instead of three stripes.
    /// </para>
    /// </summary>
    private static readonly double[] BandDepth = [0.55, 0.78, 1.00];


    /// <summary>
    /// The colour to draw one of the entries above with, as OxyPlot wants it: what the user picked in
    /// the colour screen, and the default declared here when they never touched it. Both windows read
    /// the same settings since 17-09-2026 (see ChartStyleOxy), so a colour changed in one shows up in
    /// the other.
    /// </summary>
    private OxyColor DefaultColor(string key) => DefaultColor(key, 1.0);


    /// <summary>The same colour at a fraction of its opacity; see <see cref="BandDepth"/>.</summary>
    private OxyColor DefaultColor(string key, double alphaFactor)
    {
        OxyColor declared = OxyColors.Gray;
        foreach (var definition in StyleDefinitions)
        {
            if (definition.Key != key)
                continue;
            string hex = definition.Color.TrimStart('#');
            if (hex.Length == 8 && uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out uint argb))
                declared = OxyColor.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
            break;
        }
        OxyColor colour = ChartStyleOxy.ColorFor(key, declared);
        if (alphaFactor >= 1.0)
            return colour;
        return OxyColor.FromArgb((byte)Math.Clamp(colour.A * alphaFactor, 0, 255), colour.R, colour.G, colour.B);
    }

    public void Draw(object plotModel, CryptoSymbol symbol, CryptoInterval interval,
                     List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        var chart = (PlotModel)plotModel;
        if (candles.Count == 0)
            return;

        MacLineValues[] values = MacLinesHelper.Compute(candles);

        // The cloud, filled between each neighbouring pair of lines. Every pair carries its own
        // direction, so at a turn the upper band changes colour before the lower one and the two
        // colours run into each other. Added before the lines so it ends up underneath them.
        var openFills = new AreaSeries?[3];
        var fillRising = new bool[3];
        for (int i = 0; i < candles.Count; i++)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;

            double x = openTime.Minutes;
            MacLineValues v = values[i];
            double?[] edges = [v.EmaFast, v.EmaSecond, v.SmaMedium, v.SmaSlow];

            for (int pair = 0; pair < 3; pair++)
            {
                double? faster = edges[pair];
                double? slower = edges[pair + 1];
                if (faster == null || slower == null)
                {
                    if (openFills[pair] != null && openFills[pair]!.Points.Count > 1)
                        chart.Series.Add(openFills[pair]!);
                    openFills[pair] = null;
                    continue;
                }

                bool rising = faster.Value > slower.Value;
                if (openFills[pair] != null && rising != fillRising[pair])
                {
                    AddFillPoint(openFills[pair], x, faster, slower);
                    if (openFills[pair]!.Points.Count > 1)
                        chart.Series.Add(openFills[pair]!);
                    openFills[pair] = null;
                }
                if (openFills[pair] == null)
                {
                    openFills[pair] = new AreaSeries
                    {
                        Title = rising ? "mac.cloud.up" : "mac.cloud.down",
                        Fill = DefaultColor(rising ? KeyCloudUp : KeyCloudDown, BandDepth[pair]),
                        Color = OxyColors.Transparent,
                        Color2 = OxyColors.Transparent,
                        StrokeThickness = 0,
                        YAxisKey = "price",
                        Tag = group,
                    };
                    fillRising[pair] = rising;
                }
                AddFillPoint(openFills[pair], x, faster, slower);
            }
        }
        foreach (var remaining in openFills)
        {
            if (remaining != null && remaining.Points.Count > 1)
                chart.Series.Add(remaining);
        }

        var fast = new LineSeries { Title = "mac.ema.fast", Color = DefaultColor(KeyFast), StrokeThickness = 2, YAxisKey = "price", Tag = group };
        var second = new LineSeries { Title = "mac.ema.second", Color = DefaultColor(KeySecond), StrokeThickness = 1, YAxisKey = "price", Tag = group };
        var medium = new LineSeries { Title = "mac.sma.medium", Color = DefaultColor(KeyMedium), StrokeThickness = 1, YAxisKey = "price", Tag = group };
        var slow = new LineSeries { Title = "mac.sma.slow", Color = DefaultColor(KeySlow), StrokeThickness = 2, YAxisKey = "price", Tag = group };
        var resistance = new LineSeries { Title = "mac.resistance", Color = DefaultColor(KeyResistance), StrokeThickness = 1, LineStyle = LineStyle.Dot, YAxisKey = "price", Tag = group };
        var support = new LineSeries { Title = "mac.support", Color = DefaultColor(KeySupport), StrokeThickness = 1, LineStyle = LineStyle.Dot, YAxisKey = "price", Tag = group };

        for (int i = 0; i < candles.Count; i++)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;

            double x = openTime.Minutes;
            MacLineValues v = values[i];
            if (v.EmaFast != null)
                fast.Points.Add(new DataPoint(x, v.EmaFast.Value));
            if (v.EmaSecond != null)
                second.Points.Add(new DataPoint(x, v.EmaSecond.Value));
            if (v.SmaMedium != null)
                medium.Points.Add(new DataPoint(x, v.SmaMedium.Value));
            if (v.SmaSlow != null)
                slow.Points.Add(new DataPoint(x, v.SmaSlow.Value));

            // A level holds its value until a new pivot is confirmed and then jumps. Drawn as one
            // continuous line that reads as a staircase, with a vertical stroke at every jump that
            // is not a price level at all. A break in the line (NaN) puts an end to the old level
            // and starts the new one, which is how support and resistance are drawn everywhere.
            AddLevelPoint(resistance, x, v.PivotHigh, i > 0 ? values[i - 1].PivotHigh : null);
            AddLevelPoint(support, x, v.PivotLow, i > 0 ? values[i - 1].PivotLow : null);
        }

        chart.Series.Add(medium);
        chart.Series.Add(second);
        chart.Series.Add(slow);
        chart.Series.Add(fast);
        chart.Series.Add(resistance);
        chart.Series.Add(support);

        // The crossings as a marker instead of a caption: a triangle under the candle for a long and
        // above it for a short, so a chart with many of them stays readable.
        var longMarks = new ScatterSeries
        {
            Title = "mac.open.long",
            MarkerType = MarkerType.Triangle,
            MarkerSize = 5,
            MarkerFill = DefaultColor(KeyOpenLong),
            YAxisKey = "price",
            Tag = group,
        };
        var shortMarks = new ScatterSeries
        {
            Title = "mac.open.short",
            MarkerType = MarkerType.Triangle,
            MarkerSize = 5,
            MarkerFill = DefaultColor(KeyOpenShort),
            YAxisKey = "price",
            Tag = group,
        };

        foreach ((int index, bool up) in FindCrossings(values))
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(candles[index].Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;

            // Clear of the wick by a fraction of the price, so the marker sits next to the candle
            // on a chart of any scale rather than a fixed number of points away.
            double x = openTime.Minutes;
            if (up)
                longMarks.Points.Add(new ScatterPoint(x, (double)candles[index].Low * (1 - MarkerGap)));
            else
                shortMarks.Points.Add(new ScatterPoint(x, (double)candles[index].High * (1 + MarkerGap)));
        }

        chart.Series.Add(longMarks);
        chart.Series.Add(shortMarks);

        // A breakout dot goes UNDER the candle and a breakdown mark above it, which is where the
        // reference puts them: its own settings screen reads "Breakout, below bar" and "Breakdown,
        // above bar". That is the same side as the entry triangles, hence the larger gap.
        var breakoutDots = new ScatterSeries
        {
            Title = "mac.breakout",
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = DefaultColor(KeyBreakout),
            YAxisKey = "price",
            Tag = group,
        };
        var breakdownDots = new ScatterSeries
        {
            Title = "mac.breakdown",
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = DefaultColor(KeyBreakdown),
            YAxisKey = "price",
            Tag = group,
        };

        foreach ((int index, bool up) in FindConfirmations(values, candles))
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(candles[index].Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;

            double x = openTime.Minutes;
            if (up)
                breakoutDots.Points.Add(new ScatterPoint(x, (double)candles[index].Low * (1 - 2 * MarkerGap)));
            else
                breakdownDots.Points.Add(new ScatterPoint(x, (double)candles[index].High * (1 + 2 * MarkerGap)));
        }

        chart.Series.Add(breakoutDots);
        chart.Series.Add(breakdownDots);

        // The line crossing and the exit, on the side of the candle the reference puts them:
        // Cross Up and Close Long over the bar, Cross Down and Close Short under it.
        var crossUp = Marks("mac.cross.up", MarkerType.Cross, KeyCrossUp, group);
        var crossDown = Marks("mac.cross.down", MarkerType.Cross, KeyCrossDown, group);
        foreach ((int index, bool up) in FindLineCrossings(values))
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(candles[index].Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;
            double x = openTime.Minutes;
            if (up)
                crossUp.Points.Add(new ScatterPoint(x, (double)candles[index].High * (1 + MarkerGap)));
            else
                crossDown.Points.Add(new ScatterPoint(x, (double)candles[index].Low * (1 - MarkerGap)));
        }
        chart.Series.Add(crossUp);
        chart.Series.Add(crossDown);

        var closeLong = Marks("mac.close.long", MarkerType.Diamond, KeyCloseLong, group);
        var closeShort = Marks("mac.close.short", MarkerType.Diamond, KeyCloseShort, group);
        foreach ((int index, bool longSide) in FindExits(values, candles))
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(candles[index].Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;
            double x = openTime.Minutes;
            if (longSide)
                closeLong.Points.Add(new ScatterPoint(x, (double)candles[index].High * (1 + MarkerGap)));
            else
                closeShort.Points.Add(new ScatterPoint(x, (double)candles[index].Low * (1 - MarkerGap)));
        }
        chart.Series.Add(closeLong);
        chart.Series.Add(closeShort);
    }


    /// <summary>One marker series, so the four below read as four lines instead of forty.</summary>
    private ScatterSeries Marks(string title, MarkerType shape, string key, object? group) => new()
    {
        Title = title,
        MarkerType = shape,
        MarkerSize = 4,
        MarkerFill = DefaultColor(key),
        MarkerStroke = DefaultColor(key),
        MarkerStrokeThickness = 1.5,
        YAxisKey = "price",
        Tag = group,
    };


    /// <summary>
    /// The second line crossing the third: EMA(40) through SMA(50), which is the reference's Cross
    /// Up and Cross Down. It runs AHEAD of the cloud cross, so it is drawn even while the cloud
    /// still points the other way. Counted against the chart over four coins it misses none.
    /// </summary>
    private static IEnumerable<(int Index, bool Up)> FindLineCrossings(MacLineValues[] values)
    {
        for (int i = 1; i < values.Length; i++)
        {
            MacLineValues now = values[i];
            MacLineValues before = values[i - 1];
            if (now.EmaSecond == null || now.SmaMedium == null
                || before.EmaSecond == null || before.SmaMedium == null)
                continue;

            bool above = now.EmaSecond.Value > now.SmaMedium.Value;
            bool wasAbove = before.EmaSecond.Value > before.SmaMedium.Value;
            if (above != wasAbove)
                yield return (i, above);
        }
    }


    /// <summary>
    /// The close crossing back through the second line: Close Long and Close Short. The same four
    /// conditions the strategy uses in MacBase.CrossedTheSecondLine - the crossing, the cloud still
    /// pointing the way of the position, the close still on that side of the slow line, and the
    /// second line still on that side of the third. Drawing it with fewer conditions puts a long
    /// AND a short exit on one and the same candle.
    /// </summary>
    private static IEnumerable<(int Index, bool LongSide)> FindExits(MacLineValues[] values,
                                                                     List<CryptoCandle> candles)
    {
        for (int i = 1; i < values.Length && i < candles.Count; i++)
        {
            MacLineValues now = values[i];
            MacLineValues before = values[i - 1];
            if (now.EmaFast == null || now.EmaSecond == null || now.SmaSlow == null
                || now.SmaMedium == null || before.EmaSecond == null)
                continue;

            double close = (double)candles[i].Close;
            double closeBefore = (double)candles[i - 1].Close;
            bool cloudUp = now.EmaFast.Value > now.EmaSecond.Value;
            bool stacked = cloudUp ? now.EmaSecond.Value > now.SmaMedium.Value
                                   : now.EmaSecond.Value < now.SmaMedium.Value;
            if (!stacked)
                continue;
            if (cloudUp && closeBefore >= before.EmaSecond.Value && close < now.EmaSecond.Value
                && close > now.SmaSlow.Value)
                yield return (i, true);
            if (!cloudUp && closeBefore <= before.EmaSecond.Value && close > now.EmaSecond.Value
                && close < now.SmaSlow.Value)
                yield return (i, false);
        }
    }


    /// <summary>How far off the wick a marker sits, as a fraction of the price.</summary>
    private const double MarkerGap = 0.004;


    /// <summary>
    /// Adds one point of a level line, breaking the line where the level changes so the two levels
    /// do not get joined by a vertical stroke. A NaN makes OxyPlot lift the pen.
    /// </summary>
    private static void AddLevelPoint(LineSeries series, double x, double? value, double? previous)
    {
        if (value == null)
            return;
        if (previous != null && previous.Value != value.Value)
            series.Points.Add(new DataPoint(x, double.NaN));
        series.Points.Add(new DataPoint(x, value.Value));
    }


    /// <summary>
    /// The candles where the fast line closes through the second one: the bare crossing, without
    /// any of the filters the strategy applies on top of it.
    /// <para>
    /// The other two triggers (the level break and the springboard bounce) are deliberately not
    /// drawn. They depend on settings this overlay does not read, and a marker that disagrees with
    /// the signal list would be worse than no marker at all.
    /// </para>
    /// </summary>
    private static IEnumerable<(int Index, bool Up)> FindCrossings(MacLineValues[] values)
    {
        for (int i = 1; i < values.Length; i++)
        {
            MacLineValues now = values[i];
            MacLineValues before = values[i - 1];
            if (now.EmaFast == null || now.EmaSecond == null
                || before.EmaFast == null || before.EmaSecond == null)
                continue;

            bool above = now.EmaFast.Value > now.EmaSecond.Value;
            bool wasAbove = before.EmaFast.Value > before.EmaSecond.Value;
            if (above != wasAbove)
                yield return (i, above);
        }
    }


    /// <summary>How long after the turn of the cloud a mark can appear, in candles.</summary>
    private const int MarkEarliest = 8;
    private const int MarkLatest = 60;

    /// <summary>Over how many candles the price has to make a new extreme to be marked.</summary>
    private const int MarkExtremeCandles = 5;


    /// <summary>
    /// The candles that trade through the last confirmed level: a white dot under the candle
    /// (Breakout) and a yellow one above it (Breakdown).
    /// <para>
    /// Four conditions, and every one of them is measured against the catalogued marks rather than
    /// chosen. Nine of those are pinned down in MacBreakoutOnRealCandlesTests: 24 October 2023,
    /// 11 to 13 February 2024, 28 and 29 October 2024, 6 November 2024, and 12, 18 and 19 May 2025.
    /// </para>
    /// <list type="number">
    /// <item>The HIGH trades through the last confirmed level - not the close. 19 May 2025 carries
    /// a mark while closing under its level, and one level can be marked more than once: 28 and 29
    /// October 2024 both break the same one.</item>
    /// <item>The cloud points the way of the break.</item>
    /// <item>Not the candle the cloud turned on: a mark prints AFTER the entry, never on it.</item>
    /// <item>Between MarkEarliest and MarkLatest candles after that turn, and a new extreme over
    /// MarkExtremeCandles candles. The nine known marks sit between 11 and 46 candles after their
    /// turn, so the band has room on both sides of what was measured.</item>
    /// </list>
    /// <para>
    /// DELIBERATELY WIDE, by about three to one: this draws some thirty marks a year on a daily
    /// chart where six to nine are wanted. It contains all nine catalogued ones, which is the
    /// property worth keeping until the missing condition is found. What that condition is, is not
    /// guessed at here: measuring how far price stands from the cloud narrows the count but costs
    /// money on every setting tried, so no narrowing is built in.
    /// </para>
    /// </summary>
    private static IEnumerable<(int Index, bool Up)> FindConfirmations(MacLineValues[] values,
                                                                       List<CryptoCandle> candles)
    {
        bool? cloudUp = null;
        int turnedAt = -1;

        for (int i = 0; i < values.Length && i < candles.Count; i++)
        {
            MacLineValues v = values[i];
            if (v.EmaFast == null || v.EmaSecond == null)
                continue;

            bool up = v.EmaFast.Value > v.EmaSecond.Value;
            if (cloudUp == null)
                cloudUp = up;
            else if (up != cloudUp.Value)
            {
                turnedAt = i;
                cloudUp = up;
            }

            if (turnedAt < 0 || i - turnedAt < MarkEarliest || i - turnedAt > MarkLatest)
                continue;
            if (i < MarkExtremeCandles)
                continue;

            if (up)
            {
                if (v.PivotHigh == null || (double)candles[i].High <= v.PivotHigh.Value)
                    continue;
                if (!IsExtreme(candles, i, true))
                    continue;
                yield return (i, true);
            }
            else
            {
                if (v.PivotLow == null || (double)candles[i].Low >= v.PivotLow.Value)
                    continue;
                if (!IsExtreme(candles, i, false))
                    continue;
                yield return (i, false);
            }
        }
    }


    /// <summary>Whether this candle makes a new extreme over the MarkExtremeCandles before it.</summary>
    private static bool IsExtreme(List<CryptoCandle> candles, int index, bool up)
    {
        for (int back = 1; back <= MarkExtremeCandles; back++)
        {
            CryptoCandle earlier = candles[index - back];
            if (up ? earlier.High >= candles[index].High : earlier.Low <= candles[index].Low)
                return false;
        }
        return true;
    }


    public IReadOnlyList<ChartOverlaySeries> GetSeries(CryptoSymbol symbol, CryptoInterval interval,
                                                       List<CryptoCandle> candles)
    {
        if (candles.Count == 0)
            return [];

        MacLineValues[] values = MacLinesHelper.Compute(candles);

        // The defaults of the definitions; the host replaces them with whatever the user set in the
        // chart style screen, so every colour lives in one place.
        var fast = new ChartOverlaySeries { Key = KeyFast, Label = "MAC fast EMA", Color = Css(KeyFast), LineWidth = 2 };
        var second = new ChartOverlaySeries { Key = KeySecond, Label = "MAC second EMA", Color = Css(KeySecond) };
        var medium = new ChartOverlaySeries { Key = KeyMedium, Label = "MAC medium SMA", Color = Css(KeyMedium) };
        var slow = new ChartOverlaySeries { Key = KeySlow, Label = "MAC slow SMA", Color = Css(KeySlow), LineWidth = 2 };
        var resistance = new ChartOverlaySeries { Key = KeyResistance, Label = "MAC resistance", Color = Css(KeyResistance), LineStyle = 1 };
        var support = new ChartOverlaySeries { Key = KeySupport, Label = "MAC support", Color = Css(KeySupport), LineStyle = 1 };

        for (int i = 0; i < candles.Count; i++)
        {
            long time = CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration).ToUnixSeconds();
            MacLineValues v = values[i];
            if (v.EmaFast != null)
                fast.Points.Add(new ChartOverlayPoint { Time = time, Value = v.EmaFast.Value });
            if (v.EmaSecond != null)
                second.Points.Add(new ChartOverlayPoint { Time = time, Value = v.EmaSecond.Value });
            if (v.SmaMedium != null)
                medium.Points.Add(new ChartOverlayPoint { Time = time, Value = v.SmaMedium.Value });
            if (v.SmaSlow != null)
                slow.Points.Add(new ChartOverlayPoint { Time = time, Value = v.SmaSlow.Value });

            // A level holds its value and then jumps. A NaN in front of the new value is passed on
            // as a point WITHOUT a value, which makes the renderer lift the pen - so the old level
            // ends where it ended instead of being joined to the new one by a slanted line.
            AddWebLevel(resistance, time, v.PivotHigh, i > 0 ? values[i - 1].PivotHigh : null);
            AddWebLevel(support, time, v.PivotLow, i > 0 ? values[i - 1].PivotLow : null);
        }

        return [medium, second, slow, fast, resistance, support];
    }


    /// <summary>
    /// One point of a level line for the web chart, with a valueless point in front of it wherever
    /// the level changes. <see cref="double.NaN"/> is how that travels; Chart.razor turns it into a
    /// point with no value, which is what the renderer reads as "lift the pen".
    /// </summary>
    private static void AddWebLevel(ChartOverlaySeries series, long time, double? value, double? previous)
    {
        if (value == null)
            return;
        if (previous != null && previous.Value != value.Value)
            series.Points.Add(new ChartOverlayPoint { Time = time, Value = double.NaN });
        series.Points.Add(new ChartOverlayPoint { Time = time, Value = value.Value });
    }


    /// <summary>
    /// The filled cloud for the web chart, as three bands: one between each neighbouring pair of
    /// lines. Each band runs the whole chart and says per candle which of its two colours it wants,
    /// so a band changes colour where its own pair turns.
    /// </summary>
    public IReadOnlyList<ChartOverlayBand> GetBands(CryptoSymbol symbol, CryptoInterval interval,
                                                    List<CryptoCandle> candles)
    {
        if (candles.Count == 0)
            return [];

        MacLineValues[] values = MacLinesHelper.Compute(candles);

        // Three bands, one per neighbouring pair of lines: fast to second, second to medium,
        // medium to slow. Each band runs the whole chart and carries a direction per candle, so it
        // changes colour where its own pair turns - the upper band flips before the lower one and
        // the cloud runs from one colour into the other instead of switching over in one go.
        //
        // One band per pair, NOT one per stretch: a chart full of little series each behaved
        // differently at the edge of the window and drew wedges across the screen while zooming.
        var bands = new List<ChartOverlayBand>();
        for (int pair = 0; pair < 3; pair++)
        {
            var band = new ChartOverlayBand
            {
                Key = "macCloud" + pair,
                StyleKeyUp = KeyCloudUp,
                StyleKeyDown = KeyCloudDown,
                FillColorUp = Css(KeyCloudUp, BandDepth[pair]),
                FillColorDown = Css(KeyCloudDown, BandDepth[pair]),
                AlphaFactor = BandDepth[pair],
            };

            for (int i = 0; i < candles.Count; i++)
            {
                MacLineValues v = values[i];
                double?[] edges = [v.EmaFast, v.EmaSecond, v.SmaMedium, v.SmaSlow];
                double? faster = edges[pair];
                double? slower = edges[pair + 1];
                long time = CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration).ToUnixSeconds();

                // A candle without both lines becomes a point without values: the renderer lifts
                // the pen there, which is how the warm-up at the left of the chart stays empty.
                band.Points.Add(new ChartOverlayBandPoint
                {
                    Time = time,
                    High = faster == null || slower == null ? null : Math.Max(faster.Value, slower.Value),
                    Low = faster == null || slower == null ? null : Math.Min(faster.Value, slower.Value),
                    Up = faster != null && slower != null && faster.Value > slower.Value,
                });
            }

            if (band.Points.Exists(p => p.High != null))
                bands.Add(band);
        }

        return bands;
    }


    /// <summary>One point of a filled pair on the Avalonia chart.</summary>
    private static void AddFillPoint(AreaSeries? series, double x, double? one, double? other)
    {
        if (series == null || one == null || other == null)
            return;
        series.Points.Add(new DataPoint(x, Math.Max(one.Value, other.Value)));
        series.Points2.Add(new DataPoint(x, Math.Min(one.Value, other.Value)));
    }


    /// <summary>The default of one of the definitions as a CSS colour, alpha included.</summary>
    private string Css(string key) => Css(key, 1.0);


    /// <summary>The same colour at a fraction of its opacity; see <see cref="BandDepth"/>.</summary>
    private string Css(string key, double alphaFactor)
    {
        foreach (var definition in StyleDefinitions)
        {
            if (definition.Key != key)
                continue;
            string hex = definition.Color.TrimStart('#');
            if (hex.Length == 8 && uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out uint argb))
            {
                double alpha = Math.Clamp(((argb >> 24) & 0xff) / 255.0 * alphaFactor, 0.0, 1.0);
                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "rgba({0},{1},{2},{3:0.###})", (argb >> 16) & 0xff, (argb >> 8) & 0xff, argb & 0xff, alpha);
            }
        }
        return "rgba(128,128,128,0.5)";
    }


    /// <summary>
    /// The Open Long / Open Short markers for the web chart, at the same candles
    /// <see cref="Draw"/> puts them on.
    /// </summary>
    public IReadOnlyList<ChartOverlayLabel> GetLabels(CryptoSymbol symbol, CryptoInterval interval,
                                                      List<CryptoCandle> candles)
    {
        if (candles.Count == 0)
            return [];

        MacLineValues[] values = MacLinesHelper.Compute(candles);
        var labels = new List<ChartOverlayLabel>();
        foreach ((int index, bool up) in FindCrossings(values))
        {
            labels.Add(new ChartOverlayLabel
            {
                Time = CandleTime.AlignFromDateTime(candles[index].Date, interval.Duration).ToUnixSeconds(),
                Above = !up,
                Price = up ? (double)candles[index].Low : (double)candles[index].High,
                // A triangle rather than a caption, to match the marker the Avalonia chart draws.
                Text = up ? "▲" : "▼",
                // The colour travels twice: the key so the host can apply whatever the user set,
                // and the value as the fallback for a host that does not look keys up.
                StyleKey = up ? KeyOpenLong : KeyOpenShort,
                Color = up ? Css(KeyOpenLong) : Css(KeyOpenShort),
            });
        }

        foreach ((int index, bool up) in FindConfirmations(values, candles))
        {
            labels.Add(new ChartOverlayLabel
            {
                Time = CandleTime.AlignFromDateTime(candles[index].Date, interval.Duration).ToUnixSeconds(),
                Above = !up,
                Price = up ? (double)candles[index].Low : (double)candles[index].High,
                Text = "●",
                StyleKey = up ? KeyBreakout : KeyBreakdown,
                Color = up ? Css(KeyBreakout) : Css(KeyBreakdown),
            });
        }

        foreach ((int index, bool up) in FindLineCrossings(values))
        {
            labels.Add(new ChartOverlayLabel
            {
                Time = CandleTime.AlignFromDateTime(candles[index].Date, interval.Duration).ToUnixSeconds(),
                Above = up,
                Price = up ? (double)candles[index].High : (double)candles[index].Low,
                Text = "✕",
                StyleKey = up ? KeyCrossUp : KeyCrossDown,
                Color = up ? Css(KeyCrossUp) : Css(KeyCrossDown),
            });
        }

        foreach ((int index, bool longSide) in FindExits(values, candles))
        {
            labels.Add(new ChartOverlayLabel
            {
                Time = CandleTime.AlignFromDateTime(candles[index].Date, interval.Duration).ToUnixSeconds(),
                Above = longSide,
                Price = longSide ? (double)candles[index].High : (double)candles[index].Low,
                Text = "◆",
                StyleKey = longSide ? KeyCloseLong : KeyCloseShort,
                Color = longSide ? Css(KeyCloseLong) : Css(KeyCloseShort),
            });
        }

        return labels;
    }
}
