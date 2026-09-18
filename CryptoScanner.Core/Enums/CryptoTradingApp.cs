namespace CryptoScanner.Core.Enums;

public enum CryptoTradingApp
{
    // The numbers are explicit because the settings file stores this enum as a number: an existing
    // CryptoScanBot-settings.json holds "TradingApp": 2 for TradingView, and renumbering would turn
    // that into another application on the next start.
    Altrady = 0, // Jump start via hidden browser (browser jump directly to Altrady app)
    // 1 was Hypertrader, removed on 18-09-2026 because the application no longer exists. A settings
    // file that still carries the 1 is moved to Altrady in GlobalData.LoadScannerConfiguration.
    TradingView = 2,
    ExchangeUrl = 3,
}
