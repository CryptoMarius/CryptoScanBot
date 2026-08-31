namespace CryptoScanner.Core.Enums;

/// <summary>
/// Why a balance changed without a trade behind it. Everything in this list is money going in or out
/// of the account, never a result - see <see cref="Model.CryptoAssetAdjustment"/>.
/// </summary>
public enum CryptoAssetAdjustmentReason
{
    // The start capital handed out to a traded quote coin on an empty database, or after a reset
    StartCapital, // 0

    // The user corrected a balance by hand in the paper-assets screen. Correcting a balance to zero
    // is how a coin is deleted there, so that arrives here as well.
    ManualCorrection, // 1

    // "Start over": every balance is thrown away before the start capital is handed out again
    Reset, // 2

    // A position was deleted from the database and what it did to the balances was undone with it
    PositionDeleted, // 3
}
