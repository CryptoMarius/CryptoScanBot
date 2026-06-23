namespace CryptoScanner.Core.Enums;

public enum CryptoEntryOrDcaPricing
{
    SignalPrice,
    MarketPrice,
    //BidPrice,
    //AskPrice,
    // Limit order placed below SignalPrice for a long, above for a short, by the configured
    // pullback percentage (Settings.Trading.EntryPullbackPercentage / DcaPullbackPercentage).
    // Intended for zone-style signals (smc.rejection, dlz.near …) where the rejection close
    // is by definition outside the zone; a small pullback pulls the entry back toward the
    // proximal edge for better risk/reward.
    //SignalPriceWithPullback
}
