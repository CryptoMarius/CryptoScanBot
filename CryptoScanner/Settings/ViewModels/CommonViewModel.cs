using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Settings.ViewModels;

public partial class CommonViewModel : ObservableObject
{
    [ObservableProperty]
    private CryptoTradingApp _tradingApp = CryptoTradingApp.Altrady;
    [ObservableProperty]
    private List<KeyValuePair<CryptoTradingApp, string>> _tradingAppList = [];

    [ObservableProperty]
    private CryptoDoubleClickAction _doubleClickAction = CryptoDoubleClickAction.ActivateTradingApp;
    [ObservableProperty]
    private List<KeyValuePair<CryptoDoubleClickAction, string>> _doubleClickActionList = [];

    [ObservableProperty]
    private int _soundHeartBeatMinutes = 0;
    //public string SoundHeartBeat { get; set; } = "sound-heart-beat.wav";

    [ObservableProperty]
    private string _theme = string.Empty;
    [ObservableProperty]
    private List<KeyValuePair<string, string>> __themeList = [];


    //public bool ShowInvalidSignals { get; set; } = false;         ?
    //public bool HideSelectedRow { get; set; } = false;            vervalt
    //public bool HideSymbolsOnTheLeft { get; set; } = false;       vervalt

    //public string FontNameNew { get; set; } = "Segoe UI";         vervalt
    //public float FontSizeNew { get; set; } = 9f;                  vervalt
    //public bool BlackTheming { get; set; } = false;               vervalt


    public CommonViewModel()
    {
        BuildThemeList();
        BuildTradingAppList();
        BuildDoubleClickActionList();
    }

    private void BuildThemeList()
    {
        ThemeList.Clear();
        ThemeList.Add(new(string.Empty, "Follow system"));
        ThemeList.Add(new("Dark", "Dark mode"));
        ThemeList.Add(new("Light", "Light mode"));
    }

    private void BuildDoubleClickActionList()
    {
        DoubleClickActionList.Clear();
        DoubleClickActionList.Add(new(CryptoDoubleClickAction.ActivateTradingApp, "Activate trading app"));
        DoubleClickActionList.Add(new(CryptoDoubleClickAction.ActivateChartForm, "Show chart form"));
    }

    private void BuildTradingAppList()
    {
        TradingAppList.Clear();
        TradingAppList.Add(new(CryptoTradingApp.Altrady, "Altrady"));
        TradingAppList.Add(new(CryptoTradingApp.Hypertrader, "Hypertrader"));
        TradingAppList.Add(new(CryptoTradingApp.TradingView, "TradingView"));
        TradingAppList.Add(new(CryptoTradingApp.ExchangeUrl, "Exchange"));
    }


    internal void LoadConfig(SettingsGeneral general)
    {
        TradingApp = general.TradingApp;
        DoubleClickAction = general.DoubleClickAction;
        SoundHeartBeatMinutes = general.SoundHeartBeatMinutes;
        Theme = general.Theme;
    }

    internal void SaveConfig(SettingsGeneral general)
    {
        general.TradingApp = TradingApp;
        general.DoubleClickAction = DoubleClickAction;
        general.SoundHeartBeatMinutes = SoundHeartBeatMinutes;
        general.Theme = Theme;
    }
}