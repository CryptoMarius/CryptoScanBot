using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Contracts;

/// <summary>
/// Contract for a strategy plugin that wants to draw on the chart.
/// The host iterates all loaded overlays in the draw loop and calls
/// <see cref="Draw"/> when the user has toggled this overlay on.
/// PlotModel is passed as object to avoid an OxyPlot dependency in Core.
/// </summary>
public interface IChartOverlay
{
    string Label { get; }
    string GroupKey { get; }

    void Draw(object plotModel, CryptoSymbol symbol, CryptoInterval interval,
              List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group);
}
