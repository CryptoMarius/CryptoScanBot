﻿using CryptoSbmScanner.Model;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace CryptoSbmScanner.Settings;

public enum TradingApp
{
    Altrady,
    AltradyWeb,
    Hypertrader
}

public enum DoubleClickAction
{
    activateTradingApp,
    activateTradingAppAndTradingViewInternal,
    activateTradingViewBrowerInternal,
    activateTradingViewBrowerExternal
}

public enum TrendCalculationMethod
{
    trendCalculationViaAlgo1,
    trendCalculationViaAlgo2,
    trendCalculationcViaEma
}

[Serializable]
public class IntervalAndStrategyConfig
{
    public List<string> Interval { get; set; } = new();
    public Dictionary<CryptoTradeDirection, List<string>> Strategy { get; set; } = new();

    public IntervalAndStrategyConfig()
    {
        Strategy.Add(CryptoTradeDirection.Long, new List<string>());
        Strategy.Add(CryptoTradeDirection.Short, new List<string>());
    }
}

[Serializable]
public class SettingsGeneral
{
    public bool BlackTheming { get; set; } = false;
    public TradingApp TradingApp { get; set; } = TradingApp.Altrady;
    public bool SoundTradeNotification { get; set; }
    public string SelectedBarometerQuote { get; set; } = "BUSD";
    public string SelectedBarometerInterval { get; set; } = "1H";

    public string FontName { get; set; } = "Microsoft Sans Serif";
    public float FontSize { get; set; } = 8.25f;

    public DoubleClickAction DoubleClickAction { get; set; } = DoubleClickAction.activateTradingApp;

    public int GetCandleInterval { get; set; } = 60;

    public bool ShowFluxIndicator5m { get; set; } = false;
    public int RemoveSignalAfterxCandles { get; set; } = 15;
    public bool HideTechnicalStuffSignals { get; set; } = false;

    public int SoundHeartBeatMinutes { get; set; } = 0;
    public string SoundHeartBeat { get; set; } = "sound-heart-beat.wav";

    [JsonConverter(typeof(Intern.RectangleConverter))]
    public Rectangle WindowPosition { get; set; } = new Rectangle();
    public FormWindowState WindowState { get; set; } = FormWindowState.Normal;

    public TrendCalculationMethod TrendCalculationMethod { get; set; } = TrendCalculationMethod.trendCalculationViaAlgo1;

    public SettingsGeneral()
    {
    }
}

