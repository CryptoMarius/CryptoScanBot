using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.ViewModels;

public partial class ChartOptionsViewModel : ObservableObject
{
    /// <summary>Dynamic toggle items for plugin chart overlays.</summary>
    public ObservableCollection<PluginOverlayToggle> PluginOverlays { get; } = [];


    [ObservableProperty]
    private bool _showBbma = false;

    [ObservableProperty]
    private bool _showStoch = false;

    [ObservableProperty]
    private bool _showRsi = false;

    [ObservableProperty]
    private bool _showLux = false;

    [ObservableProperty]
    private bool _showMacd = false;

    [ObservableProperty]
    private bool _showVolume = false;

    [ObservableProperty]
    private bool _showSmaLinesSbm = false;

    [ObservableProperty]
    private bool _showNwe = false;

    [ObservableProperty]
    private bool _showNweRepainting = false;

    [ObservableProperty]
    private bool _showBollingerBand = false;

    [ObservableProperty]
    private bool _showKeltnerChannel = false;

    // ShowAtrRbBands, ShowBabaBands, ShowBreBands and ShowSlide are now handled
    // dynamically via PluginOverlays (plugin chart overlays).

    [ObservableProperty]
    private bool _showPSar = false;

    [ObservableProperty]
    private bool _showDlzZones = false;

    [ObservableProperty]
    private bool _showFvgZones = false;

    [ObservableProperty]
    private bool _showSmcZones = false;

    [ObservableProperty]
    private bool _showDtb = false;

    [ObservableProperty]
    private bool _showSignals = false;

    [ObservableProperty]
    private bool _showPositions = false;

    [ObservableProperty]
    private bool _showPoints = false;

    [ObservableProperty]
    private bool _showCandles = false;

    [ObservableProperty]
    private bool _transparent = false;

    public void LoadFromSession(ZoneSession session)
    {
        // Options
        ShowBbma = session.ShowBbma;
        ShowStoch = session.ShowStoch;
        ShowRsi = session.ShowRsi;
        ShowLux = session.ShowLux;
        ShowMacd = session.ShowMacd;
        ShowVolume = session.ShowVolume;
        ShowSmaLinesSbm = session.ShowSmaLinesSbm;
        ShowNwe = session.ShowNwe;
        ShowNweRepainting = session.ShowNweRepainting;
        ShowPSar = session.ShowPSar;
        ShowBollingerBand = session.ShowBollingerBand;
        ShowKeltnerChannel = session.ShowKeltnerChannel;
        // AtrRb, Baba, BRE and Slide toggles are now loaded via PluginOverlays below.
        ShowDlzZones = session.ShowDlzZones;
        ShowFvgZones = session.ShowFvgZones;
        ShowSmcZones = session.ShowSmcZones;
        ShowDtb = session.ShowDtb;
        ShowSignals = session.ShowSignals;
        ShowPositions = session.ShowPositions;
        ShowPoints = session.ShowPoints;
        ShowCandles = session.ShowCandles;

        // misc
        Transparent = session.Transparent;

        // Plugin overlays — subscribe to each toggle so a flip triggers a chart redraw
        PluginOverlays.Clear();
        foreach (var overlay in PluginManager.ChartOverlays)
        {
            bool isOn = session.PluginOverlayStates.TryGetValue(overlay.GroupKey, out bool v) && v;
            var toggle = new PluginOverlayToggle(overlay.GroupKey, overlay.Label, isOn);
            toggle.PropertyChanged += (_, _) => OnPropertyChanged(nameof(PluginOverlays));
            PluginOverlays.Add(toggle);
        }
    }

    public void SaveToSession(ZoneSession session)
    {
        // Options
        session.ShowBbma = ShowBbma;
        session.ShowStoch = ShowStoch;
        session.ShowRsi = ShowRsi;
        session.ShowLux = ShowLux;
        session.ShowMacd = ShowMacd;
        session.ShowVolume = ShowVolume;
        session.ShowSmaLinesSbm = ShowSmaLinesSbm;
        session.ShowNwe = ShowNwe;
        session.ShowNweRepainting = ShowNweRepainting;
        session.ShowPSar = ShowPSar;
        session.ShowBollingerBand = ShowBollingerBand;
        session.ShowKeltnerChannel = ShowKeltnerChannel;
        // AtrRb, Baba, BRE and Slide toggles are now saved via PluginOverlays below.
        session.ShowDlzZones = ShowDlzZones;
        session.ShowFvgZones = ShowFvgZones;
        session.ShowSmcZones = ShowSmcZones;
        session.ShowDtb = ShowDtb;
        session.ShowSignals = ShowSignals;
        session.ShowPositions = ShowPositions;
        session.ShowPoints = ShowPoints;
        session.ShowCandles = ShowCandles;

        // misc
        session.Transparent = Transparent;

        // Plugin overlays
        foreach (var toggle in PluginOverlays)
            session.PluginOverlayStates[toggle.GroupKey] = toggle.IsEnabled;
    }

}

/// <summary>Bindable toggle item for a plugin chart overlay checkbox.</summary>
public partial class PluginOverlayToggle : ObservableObject
{
    public string GroupKey { get; }
    public string Label { get; }

    [ObservableProperty]
    private bool _isEnabled;

    public PluginOverlayToggle(string groupKey, string label, bool isEnabled)
    {
        GroupKey = groupKey;
        Label = label;
        _isEnabled = isEnabled;
    }
}
