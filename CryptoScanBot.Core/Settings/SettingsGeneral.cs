using CryptoScanBot.Core.Enums;

using Dapper.Contrib.Extensions;

using System.Text.Json.Serialization;

namespace CryptoScanBot.Core.Settings;

public enum CryptoDoubleClickAction
{
    ActivateTradingApp,
    ActivateChartForm,
}

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
    public int SmoothingK { get; set; } = 1;
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

    public bool BlackTheming { get; set; } = false;
    public CryptoTradingApp TradingApp { get; set; } = CryptoTradingApp.Altrady;
    public CryptoExternalUrlType TradingAppInternExtern { get; set; } = CryptoExternalUrlType.External;
    public CryptoDoubleClickAction DoubleClickAction { get; set; } = CryptoDoubleClickAction.ActivateTradingApp;

    public bool SoundTradeNotification { get; set; }
    public string SelectedBarometerQuote { get; set; } = "USDT";
    public string SelectedBarometerInterval { get; set; } = "1H";

    public string FontNameNew { get; set; } = "Segoe UI";
    public float FontSizeNew { get; set; } = 9f;

    public int GetCandleInterval { get; set; } = 60;

    public bool HideSelectedRow { get; set; } = false;
    public bool ShowInvalidSignals { get; set; } = false;
    public bool HideSymbolsOnTheLeft { get; set; } = false;
    public int RemoveSignalAfterxCandles { get; set; } = 15;

    public int SoundHeartBeatMinutes { get; set; } = 0;
    public string SoundHeartBeat { get; set; } = "sound-heart-beat.wav";

    public SettingsGeneralBB SettingsBb { get; set; } = new();
    public SettingsGeneralRsi SettingsRsi { get; set; } = new();
    public SettingsGeneralStoch SettingsStoch { get; set; } = new();
    
    public string DebugSymbol { get; set; } = "";
    public bool DebugZoneCandles { get; set; } = false;
    public bool DebugKLineReceive { get; set; } = false;
    public bool DebugSignalCreate { get; set; } = false;
    public bool DebugSignalStrength { get; set; } = false;
    public bool DebugTrendCalculation { get; set; } = false;
    public bool DebugAssetManagement { get; set; } = false;
}

