using CryptoScanBot.Core.Enums;
using Dapper.Contrib.Extensions;

using System.Drawing;
using System.Text.Json.Serialization;

namespace CryptoScanBot.Core.Settings;

[Serializable]
public class SettingsGeneral
{
    public string ExtraCaption { get; set; } = "";

    // What exchange
    public string ExchangeName { get; set; } = "Bybit Spot";
    [Computed]
    public int ExchangeId { get; set; } = 2;
    [Computed]
    [JsonIgnore]
    public virtual Model.CryptoExchange? Exchange { get; set; }

    public string ActivateExchangeName { get; set; } = "";


    public bool BlackTheming { get; set; } = false;
    public CryptoTradingApp TradingApp { get; set; } = CryptoTradingApp.Altrady;
    public CryptoExternalUrlType TradingAppInternExtern { get; set; } = CryptoExternalUrlType.External;

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


    // RSI instelbare oversold /overbought (op verzoek)
    /// <summary>
    /// RSI oversold, normally value = 30
    /// </summary>
    public double RsiValueOversold { get; set; } = 30;
    /// <summary>
    /// RSI overbought, normally value = 70
    /// </summary>
    public double RsiValueOverbought { get; set; } = 70;

    
    // STOCH instelbare oversold /overbought (op verzoek)
    /// <summary>
    /// Stochastic oversold, normally value = 20
    /// </summary>
    public double StochValueOversold { get; set; } = 20;
    /// <summary>
    /// Stochastic overbought, normally value = 80
    /// </summary>
    public double StochValueOverbought { get; set; } = 80;

    /// <summary>
    /// Standard Deviation for the Bollingerbands indicator
    /// </summary>
    public double BbStdDeviation { get; set; } = 2.0;



    public string DebugSymbol { get; set; } = "";
    public bool DebugKLineReceive { get; set; } = false;
    public bool DebugSignalCreate { get; set; } = false;
    public bool DebugSignalStrength { get; set; } = false;
    public bool DebugTrendCalculation { get; set; } = false;

}

