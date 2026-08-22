namespace CryptoScanner.Core.Enums;

/// <summary>
/// How a PRICE is put onto the exchange's tick grid. Quantities are never affected - those always
/// round down, because rounding a quantity up can cost more than the balance holds.
/// <para>
/// A price is always moved by less than one tick, so whichever setting is chosen the difference is
/// small: measured over the traded symbols of the emulator runs 98-163 one tick is 0.0245% of the
/// price on average, so half a tick - the average shift - is 0.012%.
/// </para>
/// </summary>
public enum CryptoPriceRounding
{
    /// <summary>
    /// Always down to the tick below, how it worked before 22-08-2026. This is the only setting that
    /// treats a long and a short DIFFERENTLY: down is towards the entry for a long target and away
    /// from it for a short target. Measured over 50.683 positions of the runs 98-163 the long target
    /// landed at 1.78772% where the short landed at 1.81225%, on a nominal 1.8% - half a tick either
    /// way. That accounts for 43% of the gap in target distance between the two sides, and about
    /// 0.27 percentage points of the gap in win rate.
    /// </summary>
    Down = 0,

    /// <summary>
    /// To the nearest tick. Neutral: no systematic shift in any direction, so the error averages out
    /// over trades instead of piling up on one side. Exactly halfway rounds up.
    /// </summary>
    Nearest = 1,

    /// <summary>
    /// Long up, short down - away from the direction the position profits in. Both sides get the same
    /// treatment and that treatment is the unfavourable one: the entry is bought dearer or sold
    /// cheaper, the target moves further away, and the stop moves closer to the entry.
    /// </summary>
    AgainstPosition = 2,

    /// <summary>
    /// Long down, short up - towards the direction the position profits in. The mirror image of
    /// <see cref="AgainstPosition"/>: entry bought cheaper or sold dearer, target nearer, stop
    /// further away. Also equal for both sides.
    /// </summary>
    FavourPosition = 3,
}
