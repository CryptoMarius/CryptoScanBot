using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using Dapper.Contrib.Extensions;

using System.Diagnostics.CodeAnalysis;

namespace CryptoScanner.Core.Model;

[Table("Exchange")]
public class CryptoExchange
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; } = string.Empty;
    public bool IsSupported { get; set; } = true;

    // Datum dat de laatste keer de exchange informatie is opgehaald
    public DateTime? LastTimeFetched { get; set; }

    public decimal FeeRate { get; set; } = 0.1m;

    public CryptoExchangeType ExchangeType { get; set; }
    public CryptoTradingType TradingType { get; set; }

    // Last candle time up to which zone invalidation (break/touch checks for DLZ/FVG/SMC)
    // and position hit checks have been performed. On scanner restart this tells us how far
    // back we need to replay candles to catch up. Null = full historical scan required.
    public CandleTime? LastZoneCheckTime { get; set; }

    // Coins indexed on id
    [Computed]
    public SortedList<int, CryptoSymbol> SymbolListId { get; } = [];

    // Scanner mapping index
    [Computed]
    public SortedList<string, CryptoSymbol> SymbolListName { get; } = [];

    // Exchange Mapping index
    [Computed]
    public SortedList<string, CryptoSymbol> SymbolListExchangeName { get; } = [];

    [Computed]
    public CryptoExchangeData Data { get; } = new();

    /// <summary>
    /// The products this market turned out to carry, filled by GlobalData.AddSymbol. Barometer
    /// symbols are not in it: they have no product and would make a market of one product look like
    /// a market of two.
    /// </summary>
    [Computed]
    public HashSet<string> Products { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether this market holds more than one product, which is the only case where the badge
    /// beside a symbol says anything. A market where every line is a perpetual would carry a PERP
    /// badge on all of them, and a marker that is always the same marks nothing - it only takes
    /// space and reads as noise. It earns its place the moment a mistake becomes possible.
    /// </summary>
    [Computed]
    public bool HasSeveralProducts => Products.Count > 1;

    /// <summary>
    /// The product code implied by this market's trading type, for the markets that offer one
    /// product and say so through their trading type. A market that carries several products
    /// (Okx Perpetual holds the swaps and the X-Perps) names the product per instrument instead;
    /// this is only the default. The one place that holds this mapping - the symbol parser and
    /// the pair lookup both read it here.
    /// </summary>
    [Computed]
    public string DefaultProduct => TradingType switch
    {
        CryptoTradingType.Spot => CryptoProduct.Spot,
        CryptoTradingType.Perpetual => CryptoProduct.Perpetual,
        _ => "",
    };


    /// <summary>
    /// Finds a symbol by its PAIR, for the callers that only have a base and a quote: the barometer,
    /// the pause symbol, a Telegram command, a stored chart session, the dashboard.
    /// <para>
    /// A symbol name carries the product behind a dot (BTCUSDT.PERP), so those callers cannot build
    /// the name themselves - they do not know which of the instruments on the pair they want. This
    /// answers with the one this market is about.
    /// </para>
    /// <para>
    /// Three steps, in this order. The name as given, which covers a caller that does have a full
    /// name and the barometer symbols that carry no product at all. Then the pair plus the product
    /// of this market, which is the normal case. Then the pair with each of the products this
    /// market turned out to carry, but only when exactly one matches - with two the question has
    /// no answer and saying so beats guessing. <see cref="Products"/> holds a handful of entries,
    /// so a miss costs a few keyed lookups instead of a walk over the whole symbol list (which
    /// some callers - the dashboard's USDT fallback - would pay on every refresh).
    /// </para>
    /// </summary>
    public bool TryGetSymbolByPair(string pair, [NotNullWhen(true)] out CryptoSymbol? symbol)
    {
        if (SymbolListName.TryGetValue(pair, out symbol))
            return true;

        string product = DefaultProduct;
        if (product.Length > 0 && SymbolListName.TryGetValue(pair + CryptoProduct.Separator + product, out symbol))
            return true;

        symbol = null;
        foreach (string carried in Products)
        {
            if (carried.Length == 0 || carried == product)
                continue;
            if (SymbolListName.TryGetValue(pair + CryptoProduct.Separator + carried, out CryptoSymbol? candidate))
            {
                if (symbol != null)
                {
                    // More than one instrument on this pair, so the pair alone does not point at one
                    symbol = null;
                    return false;
                }
                symbol = candidate;
            }
        }
        return symbol != null;
    }


    /// <summary>
    /// Gives a symbol another instrument name and moves it to that key in
    /// <see cref="SymbolListExchangeName"/>, which is the only index the exchange itself can address:
    /// a price ticker arrives with the name the exchange gave the instrument, not with the scanner
    /// name. Assigning the field on its own would leave this index on the old key, so the symbol
    /// would no longer be found under the name that actually arrives.
    /// <para>
    /// Written as a method rather than done from the setter of
    /// <see cref="CryptoSymbol.ExchangeName"/> on purpose: a setter that quietly rebuilds an index
    /// somewhere else is exactly the kind of hidden effect that is impossible to find back later.
    /// </para>
    /// <para>
    /// An instrument name does change: an exchange renames a contract, or a market carries two
    /// instruments that parse to the same scanner name and the second one takes over the first.
    /// </para>
    /// </summary>
    public void SetSymbolExchangeName(CryptoSymbol symbol, string exchangeName)
    {
        if (symbol.ExchangeName == exchangeName)
            return;

        // Only remove the old key when it still points at THIS symbol. Two symbols sharing an
        // instrument name should not have each other's entry taken away.
        if (SymbolListExchangeName.TryGetValue(symbol.ExchangeName, out CryptoSymbol? current) && current == symbol)
            SymbolListExchangeName.Remove(symbol.ExchangeName);

        symbol.ExchangeName = exchangeName;

        // Not present while the symbol is new: GlobalData.AddSymbol puts it in the indexes once it
        // has an id from the database.
        if (SymbolListId.ContainsKey(symbol.Id) && !SymbolListExchangeName.ContainsKey(exchangeName))
            SymbolListExchangeName.Add(exchangeName, symbol);
    }


    /// <summary>
    /// Gives a symbol another scanner name and moves it to that key in <see cref="SymbolListName"/>.
    /// The counterpart of <see cref="SetSymbolExchangeName"/> for the scanner-name index: assigning
    /// the field on its own would leave this index on the old key, so every lookup by the new name
    /// (including <see cref="TryGetSymbolByPair"/>) would keep missing until a restart - and a
    /// genuinely new listing that takes over the old name could never be added, because
    /// GlobalData.AddSymbol finds the stale key occupied.
    /// </summary>
    public void SetSymbolName(CryptoSymbol symbol, string name)
    {
        if (symbol.Name == name)
            return;

        // Only remove the old key when it still points at THIS symbol, same rule as the
        // instrument-name index above.
        if (SymbolListName.TryGetValue(symbol.Name, out CryptoSymbol? current) && current == symbol)
            SymbolListName.Remove(symbol.Name);

        symbol.Name = name;

        // Not present while the symbol is new: GlobalData.AddSymbol puts it in the indexes once it
        // has an id from the database.
        if (!SymbolListId.ContainsKey(symbol.Id))
            return;

        // A name that is already taken means two instruments of this market compose the same scanner
        // name, which is exactly what the product behind the dot exists to prevent. Leaving the key
        // on the symbol that holds it is still the safest thing to do - taking it over would make
        // THAT symbol unreachable instead - but it is not something to swallow: this symbol is now
        // missing from the name index, so every lookup by name walks past it.
        if (SymbolListName.TryGetValue(name, out CryptoSymbol? occupant))
        {
            if (occupant != symbol)
                GlobalData.AddErrorToLogTab($"{Name}: the name {name} is already held by instrument " +
                    $"{occupant.ExchangeName} (id {occupant.Id}), so {symbol.ExchangeName} is missing from the name index");
            return;
        }

        SymbolListName.Add(name, symbol);
    }


    /// <summary>
    /// Clear symbol information (after change of exchange)
    /// </summary>
    public void Clear()
    {
        SymbolListId.Clear();
        SymbolListName.Clear();
        SymbolListExchangeName.Clear();
        Products.Clear();
    }

}