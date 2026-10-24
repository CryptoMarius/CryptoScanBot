using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Barometer;

/// <summary>
/// The figure the barometer graph of the dashboard shows, as a line in a sub panel of the price chart.
/// <para>
/// Both charts draw a figure of 0..100 in the oscillator panel beside the RSI and the Stochastic, and
/// any other figure in a panel of its own under the candles (see <see cref="BelongsInOscillatorPanel"/>);
/// until 23-10-2026 it was drawn over the candles on an axis of its own on the left. Which figure follows the dropdown of the dashboard (stored in ApplicationStateService), and
/// for a figure that depends on the interval, the interval chosen there too. The quote is the quote of
/// the symbol in the chart: the market of that coin.
/// </para>
/// <para>
/// The data is what the barometer stored, so the line covers what the dashboard graph covers. The slow
/// figures (market trend, market state and its parts, market RSI) go back
/// <see cref="Const.Constants.BarometerHistoryDays"/> days with a point per hour, plus the last
/// <see cref="Const.Constants.BarometerGraphHours"/> hours with a point per minute. The figures that
/// depend on the interval (average, median, rising, ...) are only kept for those last hours. Further
/// back the line is simply absent. The emulator stores no market state, so there the slow figures are
/// absent.
/// </para>
/// <para>
/// Kept in one file with the rest of the overlay so it can be removed in one go if it is not wanted
/// after all: this class, the drawer in each chart and the "barometer" toggle.
/// </para>
/// </summary>
public static class BarometerChartOverlay
{
    /// <summary>Key of the toggle in both overlay menus and of the line in the chart style settings.</summary>
    public const string OverlayKey = "barometer";

    /// <summary>Label of the toggle in both overlay menus.</summary>
    public const string OverlayLabel = "Barometer (dashboard graph)";

    /// <summary>
    /// How long one stored value keeps standing for the candles after it. The hourly part of the
    /// history has a point per whole hour; a little more than that bridges one late hour, while a
    /// longer hole (the scanner was not running) stays a hole instead of a straight line across it.
    /// </summary>
    public const int MaxHoldMinutes = 90;

    /// <summary>One line: its label, a value per chart candle, and how its axis should look.</summary>
    /// <param name="Label">Name of the figure, as in the dashboard dropdown.</param>
    /// <param name="Points">Open time of a chart candle and the value known at the close of that candle.</param>
    /// <param name="Low">Fixed bottom of the axis, or null to follow the data.</param>
    /// <param name="High">Fixed top of the axis, or null to follow the data.</param>
    /// <param name="Reference">Where the neutral line goes (50 for the share above the EMA50), or null.</param>
    public sealed record Line(string Label, List<(CandleTime Time, double Value)> Points, double? Low, double? High, double? Reference);


    /// <summary>
    /// Whether the figure runs from 0 to 100 like the RSI and the Stochastic (the market RSI and the
    /// share above the EMA50). Both charts draw such a figure in the oscillator panel beside the RSI
    /// of the coin itself; any other figure has no common scale with it and gets a panel of its own.
    /// </summary>
    /// <param name="graphValueName">The dropdown label from the dashboard; empty means the average.</param>
    public static bool BelongsInOscillatorPanel(string? graphValueName)
    {
        BarometerGraphScale scale = BarometerCandleFields.GetScale(BarometerCandleFields.Parse(graphValueName ?? ""));
        return !scale.CenteredOnZero && scale.Low == 0m && scale.High == 100m;
    }


    /// <summary>
    /// The line for the candles of a chart. Null when the barometer symbol or its candles are not there.
    /// </summary>
    /// <param name="quote">Quote of the symbol in the chart.</param>
    /// <param name="graphValueName">The dropdown label from the dashboard; empty means the average.</param>
    /// <param name="barometerIntervalName">The interval chosen on the dashboard, used only by the
    /// figures that depend on it.</param>
    /// <param name="candleOpenTimes">Open times of the chart candles, oldest first.</param>
    /// <param name="intervalMinutes">Duration of one chart candle in minutes.</param>
    public static Line? Build(string quote, string? graphValueName, string? barometerIntervalName,
        IReadOnlyList<CandleTime> candleOpenTimes, int intervalMinutes)
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null || string.IsNullOrEmpty(quote) || candleOpenTimes.Count == 0)
            return null;

        BarometerGraphValue value = BarometerCandleFields.Parse(graphValueName ?? "");
        string symbolName = BarometerCandleFields.GetSymbolName(value) + quote;
        if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            return null;

        // A figure that does not depend on the interval holds the same value in every interval list
        string intervalName = BarometerCandleFields.UsesInterval(value) && !string.IsNullOrEmpty(barometerIntervalName)
            ? barometerIntervalName
            : "1h";
        if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
            return null;

        List<(CandleTime Time, decimal Value)> stored = [];
        foreach (var kv in symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList.GetSnapshot())
        {
            // A candle from before the fields were split holds the average in every field, which
            // would draw as a wrong figure; the dashboard skips it the same way
            if (BarometerCandleFields.IsLegacyLayout(kv.Value))
                continue;
            stored.Add((kv.Key, BarometerCandleFields.Read(kv.Value, value)));
        }

        List<(CandleTime Time, double Value)> points = Sample(stored, candleOpenTimes, intervalMinutes, MaxHoldMinutes);

        string label = BarometerCandleFields.GetName(value);
        if (BarometerCandleFields.UsesInterval(value))
            label += " " + intervalName;

        // The raw figure on its own axis: a fixed range where the dashboard uses one, otherwise the
        // axis follows the data. The neutral line sits where the dashboard draws its red line; for a
        // figure the dashboard shifts onto zero (the share that rose) that is the shift itself.
        BarometerGraphScale scale = BarometerCandleFields.GetScale(value);
        double? low = null, high = null, reference;
        if (scale.CenteredOnZero)
            reference = (double)BarometerCandleFields.GetOffset(value);
        else
        {
            if (scale.Low.HasValue && scale.High.HasValue)
            {
                low = (double)scale.Low.Value;
                high = (double)scale.High.Value;
            }
            reference = scale.ReferenceLine.HasValue ? (double)scale.ReferenceLine.Value : null;
        }

        return new Line(label, points, low, high, reference);
    }


    /// <summary>
    /// One value per chart candle: the last stored value from before the close of that candle, so a
    /// candle never shows what was only known after it. A candle whose last stored value is more than
    /// <paramref name="maxHoldMinutes"/> before its close gets none.
    /// </summary>
    /// <param name="stored">Stored barometer values by minute, oldest first.</param>
    public static List<(CandleTime Time, double Value)> Sample(IReadOnlyList<(CandleTime Time, decimal Value)> stored,
        IReadOnlyList<CandleTime> candleOpenTimes, int intervalMinutes, int maxHoldMinutes)
    {
        List<(CandleTime, double)> result = [];
        if (stored.Count == 0)
            return result;

        int index = -1;
        foreach (CandleTime open in candleOpenTimes)
        {
            uint close = open.Minutes + (uint)intervalMinutes;
            while (index + 1 < stored.Count && stored[index + 1].Time.Minutes < close)
                index++;
            if (index < 0)
                continue;

            (CandleTime time, decimal value) = stored[index];
            if (close - time.Minutes > maxHoldMinutes + intervalMinutes)
                continue;
            result.Add((open, (double)value));
        }
        return result;
    }
}
