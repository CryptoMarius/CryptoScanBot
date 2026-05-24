using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Messages;

/// <summary>
/// Fired right after ZoneDlz.CalculateZonesAsync and/or ZoneFvg.CalculateZonesAsync
/// finished for a given symbol. The symbol grid uses this to refresh the Distance
/// column for the matching row instead of waiting for the 15-second timer tick.
/// </summary>
public class ZonesCalculatedForSymbolMessage
{
    public CryptoSymbol Symbol { get; }

    public ZonesCalculatedForSymbolMessage(CryptoSymbol symbol)
    {
        Symbol = symbol;
    }
}
