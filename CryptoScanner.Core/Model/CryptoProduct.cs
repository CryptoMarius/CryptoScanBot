namespace CryptoScanner.Core.Model;

/// <summary>
/// The product codes that <see cref="CryptoSymbol.Product"/> can carry for a market the exchange
/// runs itself. They end up in the symbol name behind a dot (BTCUSDT.PERP) and in the badge beside
/// it, which is why there is one list of them rather than a literal per call site.
/// <para>
/// Text and not an enum, because the set is only half closed. A market that an outside party
/// deployed carries the name of that party instead - "HYNA", "XYZ" - and that list grows whenever
/// someone deploys another one. See <see cref="CryptoSymbol.Product"/> for why both live in the
/// same field.
/// </para>
/// </summary>
public static class CryptoProduct
{
    /// <summary>Buying the coin itself.</summary>
    public const string Spot = "SPOT";

    /// <summary>A perpetual settled in the stablecoin of the pair, which is what the scanner runs on.</summary>
    public const string Perpetual = "PERP";

    /// <summary>A perpetual settled in the base coin. Not fetched today, the code is here so a name cannot be invented twice.</summary>
    public const string Inverse = "INVERSE";

    /// <summary>The X-Perps of Okx: perpetual in behaviour, filed under futures with an expiry in 2031.</summary>
    public const string XPerp = "XPERP";

    /// <summary>A contract with a real expiry date. Not fetched today, same reason as Inverse.</summary>
    public const string Future = "FUTURE";

    /// <summary>
    /// The separator between the pair and the product in a symbol name. A dot cannot occur in a base
    /// or a quote, so splitting on it is unambiguous - which is what lets the black and white list
    /// match on the pair alone.
    /// </summary>
    public const char Separator = '.';

    /// <summary>
    /// The pair part of a scanner name: everything in front of the separator. A name without a
    /// product comes back unchanged, so this is safe to call on any name.
    /// <para>
    /// This is the spelling every exchange uses in its OWN answers that are not per instrument - a
    /// ticker list keyed on BTCUSDT, a 24 hour volume, a price. The scanner name carries the product
    /// behind a dot since 27-08-2026 and no exchange knows about that, so looking a symbol up in
    /// such an answer by <see cref="CryptoSymbol.Name"/> matches nothing at all. That is not a
    /// missing value but a silent one: the volume falls back to zero, the symbol drops below the
    /// minimum volume, and the market ends up with no subscriptions and no barometer.
    /// </para>
    /// <para>
    /// Not the same as <see cref="CryptoSymbol.PairName"/>, which is built from Base and Quote as
    /// the exchange spelled them. This one is cut out of the name itself, so it is always the
    /// uppercase spelling the name was composed with (SymbolBase.ParseSymbol).
    /// </para>
    /// </summary>
    public static string PairOf(string name)
    {
        int dot = name.IndexOf(Separator);
        return dot > 0 ? name[..dot] : name;
    }

    /// <summary>
    /// The codes above, so the part behind the dot can be told apart from the old spelling of a
    /// deployed market. The black and white list is what needs that: before the product existed the
    /// deployer sat IN FRONT of the base (GOLDUSDC.XYZ was called XYZGOLDUSDC), so a rule from that
    /// era is matched against that spelling as well - but only when the part behind the dot is a
    /// deployer and not one of ours, otherwise "PERPBTCUSDT" would suddenly mean something.
    /// <para>
    /// It says nothing about what a deployer may call itself: nothing checks that at the moment a
    /// symbol is composed, so a market deployed under the name "perp" would produce BTCUSDC.PERP.
    /// No such market exists today - the ten on HyperLiquid are named after their party - and the
    /// place to catch it would be SymbolBase.ParseSymbol.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> Reserved =
        new(StringComparer.OrdinalIgnoreCase) { Spot, Perpetual, Inverse, XPerp, Future };

    public static bool IsReserved(string product) => Reserved.Contains(product);

    /// <summary>
    /// The badge colour of a product, as a CSS hex value. One table for both UIs - the Avalonia
    /// side parses it into a brush, the Photino side puts it in an inline style - so the two can
    /// never show the same product in different colours. The exchange's own markets are told apart
    /// by contract type; everything an outside party deployed shares one colour, because there are
    /// ten such markets on HyperLiquid alone and a colour per deployer would be a rainbow nobody
    /// can read. All of them are dark enough to carry white text in both themes, so the badge
    /// needs no colour per theme.
    /// </summary>
    public static string ColorOf(string product)
    {
        return product switch
        {
            Spot => "#2E7D9A",
            Perpetual => "#C2701C",
            XPerp => "#9A7B12",
            Inverse => "#8A3B3B",
            Future => "#4A5E35",
            _ => "#6D3FA0",
        };
    }
}
