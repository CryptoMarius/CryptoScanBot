using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Zones;

using System.Collections.ObjectModel;

namespace CryptoScanner.ViewModels;

public partial class ChartOptionsViewModel : ObservableObject
{
    /// <summary>Dynamic toggle items for plugin chart overlays.</summary>
    public ObservableCollection<PluginOverlayToggle> PluginOverlays { get; } = [];

    /// <summary>All overlay items (static + plugin), sorted by label for the Overlays groupbox.</summary>
    public ObservableCollection<PluginOverlayToggle> AllOverlays { get; } = [];

    // ShowBbma is now a plugin chart overlay (PluginOverlays).
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

    private static readonly (string key, string label)[] StaticOverlays =
    [
        ("bb", "Bollinger Bands"),
        ("dlz", "DLZ Zones"),
        ("dtb", "DTB"),
        ("fvg", "FVG Zones"),
        ("kc", "Keltner Channel"),
        ("nwe", "NWE (not repainting)"),
        ("nwe.r", "NWE (repainting)"),
        ("psar", "PSar"),
        ("sbm", "SBM SMA lines"),
        ("smc", "SMC Zones"),
    ];

    private void SyncStaticOverlay(string key, bool value)
    {
        switch (key)
        {
            case "bb": ShowBollingerBand = value; break;
            case "dlz": ShowDlzZones = value; break;
            case "dtb": ShowDtb = value; break;
            case "fvg": ShowFvgZones = value; break;
            case "kc": ShowKeltnerChannel = value; break;
            case "nwe": ShowNwe = value; break;
            case "nwe.r": ShowNweRepainting = value; break;
            case "psar": ShowPSar = value; break;
            case "sbm": ShowSmaLinesSbm = value; break;
            case "smc": ShowSmcZones = value; break;
        }
    }

    private bool GetStaticOverlayValue(string key) => key switch
    {
        "bb" => ShowBollingerBand,
        "dlz" => ShowDlzZones,
        "dtb" => ShowDtb,
        "fvg" => ShowFvgZones,
        "kc" => ShowKeltnerChannel,
        "nwe" => ShowNwe,
        "nwe.r" => ShowNweRepainting,
        "psar" => ShowPSar,
        "sbm" => ShowSmaLinesSbm,
        "smc" => ShowSmcZones,
        _ => false,
    };

    public void LoadFromSession(ZoneSession session)
    {
        // Options
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
        foreach (var overlay in PluginManager.ChartOverlays.OrderBy(o => o.Label))
        {
            bool isOn = session.PluginOverlayStates.TryGetValue(overlay.GroupKey, out bool v) && v;
            var toggle = new PluginOverlayToggle(overlay.GroupKey, overlay.Label, isOn);
            toggle.PropertyChanged += (_, _) => OnPropertyChanged(nameof(PluginOverlays));
            PluginOverlays.Add(toggle);
        }

        // Build a combined, sorted list of all overlay items (static + plugin)
        AllOverlays.Clear();
        var all = new List<PluginOverlayToggle>();

        foreach (var (key, label) in StaticOverlays)
        {
            var toggle = new PluginOverlayToggle(key, label, GetStaticOverlayValue(key));
            toggle.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PluginOverlayToggle.IsEnabled))
                    SyncStaticOverlay(toggle.GroupKey, toggle.IsEnabled);
            };
            all.Add(toggle);
        }
        all.AddRange(PluginOverlays);
        all.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));

        foreach (var item in all)
            AllOverlays.Add(item);
    }

    public void SaveToSession(ZoneSession session)
    {
        // Options
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
