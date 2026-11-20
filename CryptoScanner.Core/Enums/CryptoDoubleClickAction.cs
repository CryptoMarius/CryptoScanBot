namespace CryptoScanner.Core.Enums;

public enum CryptoDoubleClickAction
{
    // The numbers are explicit because the settings file stores this enum as a number
    ActivateTradingApp = 0,
    ActivateChartForm = 1,
    TradingViewInternal = 2,
    TradingViewExternal = 3,
    ExchangeUrl = 4,
}

