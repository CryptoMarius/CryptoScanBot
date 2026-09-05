namespace CryptoScanner.Core.Model;

/// <summary>
/// The settings of one product: the code behind the dot in a symbol name (BTCUSDC.PERP,
/// GOLDUSDC.XYZ). The counterpart of <see cref="CryptoQuoteData"/> for the other half of the name,
/// and kept for the same reason - so the user can switch a whole product off without typing every
/// one of its symbols in the black list.
/// <para>
/// A product that is switched off is not accepted when the exchange refreshes its symbols
/// (SymbolBase.IsSymbolAccepted), so its symbols go to status 0 like a delisted one: no candles, no
/// subscription, not in the symbol grid. Switching it back on brings them back on the next refresh.
/// The list fills itself as products come by, exactly like the quote coins do.
/// </para>
/// </summary>
public class CryptoProductData
{
    public required string Name { get; set; }

    /// <summary>Whether the symbols of this product are fetched at all.</summary>
    public bool Active { get; set; } = true;
}
