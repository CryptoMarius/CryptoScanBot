using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Settings;

[Serializable]
/// Settings Bollingerbands indicator
public class SettingsGeneralBB
{
    /// <summary>
    /// Standard Length for the Bollingerbands indicator
    /// </summary>
    public int Length { get; set; } = 20;
    /// <summary>
    /// Standard Deviation for the Bollingerbands indicator
    /// </summary>
    public double Deviation { get; set; } = 2.0;
}


[Serializable]
/// Settings RSI indicator
public class SettingsGeneralRsi
{
    /// <summary>
    /// Standard Length for the RSI indicator
    /// </summary>
    public int Length { get; set; } = 14;

    /// <summary>
    /// RSI oversold value (30)
    /// </summary>
    public double Oversold { get; set; } = 30;
    /// <summary>
    /// RSI overbought value (70)
    /// </summary>
    public double Overbought { get; set; } = 70;
}


[Serializable]
/// Settings Stochastic indicator
public class SettingsGeneralStoch
{
    /// <summary>
    /// Stoch length K (14)
    /// </summary>
    public int Length { get; set; } = 14;
    /// <summary>
    /// Stoch Oscillator Smoothing %K (1) Blue (3?)
    /// </summary>
    public int SmoothingK { get; set; } = 3;
    /// <summary>
    /// Stoch Signal Smoothing %D (3) Orange
    /// </summary>
    public int SmoothingD { get; set; } = 3;


    /// <summary>
    /// Stochastic oversold value (20)
    /// </summary>
    public double Oversold { get; set; } = 20;
    /// <summary>
    /// Stochastic overbought value (80)
    /// </summary>
    public double Overbought { get; set; } = 80;
}

[Serializable]
public class SettingsGeneral
{
    public string ExtraCaption { get; set; } = "";
    public string ExchangeName { get; set; } = "Bybit Spot";
    public string ActivateExchangeName { get; set; } = "";

    // will be replaced by Theme
    //public bool BlackTheming { get; set; } = false;
    public string Theme { get; set; } = string.Empty;
    public CryptoTradingApp TradingApp { get; set; } = CryptoTradingApp.Altrady;
    public CryptoExternalUrlType TradingAppInternExtern { get; set; } = CryptoExternalUrlType.External;
    public CryptoDoubleClickAction DoubleClickAction { get; set; } = CryptoDoubleClickAction.ActivateTradingApp;
    public CryptoIntervalPeriod DefaultInterval { get; set; } = CryptoIntervalPeriod.interval15m;

    // Barometer goes into ApplicationState
    // Need two other states for sound and signal
    public bool SoundTradeNotification { get; set; }
    //public string SelectedBarometerQuote { get; set; } = "USDT";
    //public string SelectedBarometerInterval { get; set; } = "1H";

    // Avalonia: will not be supported
    //public string FontNameNew { get; set; } = "Segoe UI";
    //public float FontSizeNew { get; set; } = 9f;

    public int GetCandleInterval { get; set; } = 60;

    // Wil not be supported?
    public bool HideSelectedRow { get; set; } = false;
    public bool ShowInvalidSignals { get; set; } = false;
    public bool HideSymbolsOnTheLeft { get; set; } = false;
    /// <summary>Lower bound of <see cref="RemoveSignalAfterxCandles"/>.</summary>
    public const int RemoveSignalAfterxCandlesMinimum = 15;

    /// <summary>Upper bound of <see cref="RemoveSignalAfterxCandles"/>.</summary>
    public const int RemoveSignalAfterxCandlesMaximum = 120;

    /// <summary>
    /// How long a signal is kept, counted in candles of its own interval. It decides the
    /// ExpirationDate of every signal, and the signal list drops everything that has expired — so
    /// this number is what governs how many signals stay in memory.
    /// <para>
    /// Clamped in the setter, not just in the settings screen: the value also arrives from the
    /// settings file and from the other application, and a large number there would quietly hold on
    /// to far more signals than intended. Note that DLZ/FVG-style signals are deliberately kept five
    /// times as long (see GetExpirationDate).
    /// </para>
    /// </summary>
    public int RemoveSignalAfterxCandles
    {
        get => _removeSignalAfterxCandles;
        set => _removeSignalAfterxCandles = Math.Clamp(value, RemoveSignalAfterxCandlesMinimum, RemoveSignalAfterxCandlesMaximum);
    }
    private int _removeSignalAfterxCandles = 15;

    public int SoundHeartBeatMinutes { get; set; } = 0;
    public string SoundHeartBeat { get; set; } = "sound-heart-beat.wav";

    public SettingsGeneralBB SettingsBb { get; set; } = new();
    public SettingsGeneralRsi SettingsRsi { get; set; } = new();
    public SettingsGeneralStoch SettingsStoch { get; set; } = new();

    // SignalR server for broadcasting signals to external programs
    public bool SignalREnabled { get; set; } = false;
    public int SignalRPort { get; set; } = 5200;

    public string DebugSymbol { get; set; } = "";
    public bool DebugZoneCandles { get; set; } = false;
    public bool DebugKLineReceive { get; set; } = false;
    public bool DebugSignalCreate { get; set; } = false;
    public bool DebugTrendCalculation { get; set; } = false;
    public bool DebugAssetManagement { get; set; } = false;
    // When on, the signal/trade pipeline logs [SIGNAL-TIMING] lines (Info level, so they also land in
    // the per-run emulator log) tying each signal's trigger candle to the actual entry candle. Used to
    // prove/disprove an off-by-one ("entry one candle too late"). Respects DebugSymbol as a filter.
    public bool DebugSignalTiming { get; set; } = false;
}

