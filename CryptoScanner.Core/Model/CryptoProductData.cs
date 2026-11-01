using System.Text.Json.Serialization;

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

    /// <summary>
    /// The instruments the exchange offers for this product at its symbol refresh (by their name at the
    /// exchange), counted before any filter: also those of a switched off product. So the settings
    /// screen can show what there is to trade, not only what is fetched now (31-10-2026). Not saved,
    /// every refresh fills it again; an instrument that left the exchange is counted until a restart.
    /// </summary>
    private readonly HashSet<string> _offeredInstruments = [];

    public void NoteOffered(string instrument)
    {
        lock (_offeredInstruments)
            _offeredInstruments.Add(instrument);
    }

    [JsonIgnore]
    public int OfferedCount
    {
        get
        {
            lock (_offeredInstruments)
                return _offeredInstruments.Count;
        }
    }
}
