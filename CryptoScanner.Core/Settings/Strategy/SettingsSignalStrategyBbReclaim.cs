namespace CryptoScanner.Core.Settings.Strategy;

// BB extreme + EMA9/SMA20 reclaim.
// Long  : a recent candle pierced (wholly or partially) below BB.lower with close also below both MAs,
//         and the current candle has close above EMA9 with EMA9 above SMA20.
// Short : mirror — recent candle pierced above BB.upper with close above both MAs,
//         and the current candle has close below EMA9 with EMA9 below SMA20.
[Serializable]
public class SettingsSignalStrategyBbReclaim : SettingsSignalStrategyBase
{
    // How many candles back to look for the BB extreme washout candle.
    public int Lookback { get; set; } = 8;

    public SettingsSignalStrategyBbReclaim() : base()
    {
    }
}
