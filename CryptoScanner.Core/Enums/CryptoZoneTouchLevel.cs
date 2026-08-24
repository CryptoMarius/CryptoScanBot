namespace CryptoScanner.Core.Enums;

/// <summary>
/// How far price has to come into a zone before it counts as one visit.
/// <para>
/// The only thing that differs between the three zone kinds, and the reason it is a setting rather
/// than a second implementation: what counts as "price came to test this level" is a property of the
/// method the zone comes from, not of the bookkeeping around it.
/// </para>
/// </summary>
public enum CryptoZoneTouchLevel
{
    /// <summary>
    /// Reaching the near edge of the zone is a visit: for a demand zone the candle's low at or below
    /// the top, for a supply zone the candle's high at or above the bottom. The stricter of the two -
    /// price only has to arrive.
    /// </summary>
    Edge = 0,

    /// <summary>
    /// Price has to reach the middle of the zone before it counts. Comes from the order-block school,
    /// where a level is only considered tested once price has come halfway into it; a wick that just
    /// clips the edge is not a test there.
    /// </summary>
    Midpoint = 1,
}
