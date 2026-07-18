using CryptoScanner.Core.Model;

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
    void Init(QuoteHub quoteHub);
    void OnCandleAdded(IQuote candle);
    void FillData(CryptoData data);
}
