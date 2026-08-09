using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

namespace CryptoScanner.Core.Contracts;

/// <summary>
/// Per-hub indicator state owned by a strategy plugin. One instance per
/// <see cref="Signal.Indicators.IntervalIndicatorHub"/> (= per symbol+interval).
/// The hub creates it via <see cref="IStrategyPlugin.CreateIndicatorExtension"/>,
/// then calls <see cref="OnCandleAdded"/> in Add() and <see cref="FillData"/> in BuildCurrent().
/// </summary>
public interface IIndicatorExtension
{
    /// <summary>
    /// Wire up the hubs this extension needs. Ask the <paramref name="registry"/> for standard
    /// indicators (they are then shared with everyone else asking for the same parameters) and use
    /// <see cref="IndicatorRegistry.CreateDerivedHub"/> for synthetic series of your own.
    /// </summary>
    void Init(IndicatorRegistry registry);

    void OnCandleAdded(IQuote candle);
    void FillData(CryptoData data);
}
