namespace CryptoScanner.Core.Enums;

/// <summary>
/// The kind of message that is on its way to Telegram. Every category has its own checkbox in the
/// Telegram settings, so the user can keep the trader quiet without losing the signals (or the
/// other way round).
/// </summary>
public enum CryptoTelegramCategory
{
    /// <summary>
    /// A signal produced by one of the strategies.
    /// </summary>
    Signal,
    /// <summary>
    /// What the scanner sends to the exchange: an entry, a dca, a take profit, a cancel, and the
    /// failures of all of those.
    /// </summary>
    OrderPlaced,
    /// <summary>
    /// What the exchange reports back about an order: it filled, or the user took the position over
    /// by cancelling the order themselves.
    /// </summary>
    OrderFilled,
    /// <summary>
    /// The scanner talking about itself: ready after loading, a restart of the streams, a pause rule
    /// that kicked in.
    /// </summary>
    System,
    /// <summary>
    /// The test button of the settings screen. Ignores every checkbox on purpose - testing the
    /// connection has to work whatever is switched off.
    /// </summary>
    Test
}
