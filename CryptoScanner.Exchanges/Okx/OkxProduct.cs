using CryptoScanner.Core.Model;

using OKX.Net.Enums;

namespace CryptoScanner.Core.Exchange.Okx;

/// <summary>
/// The product code of one Okx instrument, which at Okx is two questions instead of one: what kind
/// of contract it is, and what that contract is on.
/// <para>
/// Okx answers the second question itself, per instrument, in instCategory (the library calls it
/// <see cref="OKXInstrument.SymbolCategory"/>). Counted on 28-08-2026 over the complete answer of
/// both lists: of the 443 swaps 275 are Crypto, 160 Stocks and 8 Commodities; of the 157 X-Perps
/// 108 are Crypto, 45 Stocks and 4 Commodities. So a third of this market follows a share, an index
/// or a commodity, and until now every one of them read as an ordinary coin in the grids.
/// </para>
/// <para>
/// That distinction is the one a reader cannot make from the pair: AAOIUSDC and AAVEUSDC are the
/// same contract type and read the same, while one follows Applied Optoelectronics on the Nasdaq
/// and the other a coin. Altrady splits its Okx list on exactly this line - "TradFi" beside "xPerp"
/// - and the badges there match this field one for one.
/// </para>
/// </summary>
static internal class OkxProduct
{
    /// <summary>
    /// The product a symbol gets: <see cref="CryptoProduct.TradFi"/> for everything Okx does not
    /// call Crypto, and otherwise the code that belongs to the contract type it was found under.
    /// <para>
    /// Forex and Bonds are in the enum as well and land on TradFi with the shares and the
    /// commodities. Okx lists neither today, and if it ever does they belong on the same side of
    /// this line: not a coin.
    /// </para>
    /// <para>
    /// An absent category counts as Crypto rather than as TradFi. The field is filled on every
    /// instrument of both lists today, so this only fires if Okx stops sending it - and then keeping
    /// the contract type is the answer that changes nothing, while the other way round would rename
    /// the whole market in one refresh.
    /// </para>
    /// </summary>
    /// <param name="category">instCategory of the instrument, as the exchange states it.</param>
    /// <param name="contractProduct">The code for the contract type: PERP for a swap, XPERP for an X-Perp.</param>
    static internal string Of(SymbolCategory? category, string contractProduct)
    {
        if (category.HasValue && category.Value != SymbolCategory.Crypto)
            return CryptoProduct.TradFi;
        return contractProduct;
    }
}
