namespace CryptoScanner.Core.Enums;

/// <summary>
/// What closes a zone, besides the break that always does. One choice per zone kind, replacing the
/// old combination of "Max touches (0=off)" and "Close zones past the midpoint" (10-11-2026): to get
/// "only a body through the zone closes it" you had to switch two other things off, and the rule that
/// always applies was nowhere on the screen.
/// <para>
/// In every option a candle BODY that closes through the far side closes the zone
/// (<see cref="Zones.ZoneInvalidation"/>, rule 1). The options only add a reason to close it earlier.
/// </para>
/// </summary>
public enum CryptoZoneCloseRule
{
    /// <summary>
    /// Only the break: a candle body closes through the far side of the zone. Wicks never close it,
    /// however often or however deep they come in. For a gap this is "fully filled by a close".
    /// </summary>
    BodyThrough = 0,

    /// <summary>
    /// Also closes once the given number of visits had a candle body close past the middle of the
    /// zone. The middle of a gap is what ICT calls the consequent encroachment.
    /// </summary>
    BodyPastMidpoint = 1,

    /// <summary>
    /// Also closes once the given number of visits reached the touch level. The supply/demand view:
    /// a fresh zone is strongest, and every test uses up part of what made the level hold.
    /// </summary>
    Touches = 2,
}
