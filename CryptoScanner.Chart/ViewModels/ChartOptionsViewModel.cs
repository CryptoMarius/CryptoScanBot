using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Zones;

namespace CryptoScanner.ViewModels;

public partial class ChartOptionsViewModel : ObservableObject
{

    [ObservableProperty]
    private bool _showBbma = false;

    [ObservableProperty]
    private bool _showStoch = false;

    [ObservableProperty]
    private bool _showRsi = false;

    [ObservableProperty]
    private bool _showLux = false;

    [ObservableProperty]
    private bool _showBbPercent = false;

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
    private bool _showNweBb = false;

    [ObservableProperty]
    private bool _showNweBbAtrRb = false;

    [ObservableProperty]
    private bool _showBollingerBand = false;

    [ObservableProperty]
    private bool _showKeltnerChannel = false;

    [ObservableProperty]
    private bool _showAtrRbBands = false;

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
        ShowBbPercent = session.ShowBbPercent;
        ShowMacd = session.ShowMacd;
        ShowVolume = session.ShowVolume;
        ShowSmaLinesSbm = session.ShowSmaLinesSbm;
        ShowNwe = session.ShowNwe;
        ShowNweRepainting = session.ShowNweRepainting;
        ShowNweBb = session.ShowNweBb;
        ShowNweBbAtrRb = session.ShowNweBbAtrRb;
        ShowPSar = session.ShowPSar;
        ShowBollingerBand = session.ShowBollingerBand;
        ShowKeltnerChannel = session.ShowKeltnerChannel;
        ShowAtrRbBands = session.ShowAtrRbBands;
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
    }

    public void SaveToSession(ZoneSession session)
    {
        // Options
        session.ShowBbma = ShowBbma;
        session.ShowStoch = ShowStoch;
        session.ShowRsi = ShowRsi;
        session.ShowLux = ShowLux;
        session.ShowBbPercent = ShowBbPercent;
        session.ShowMacd = ShowMacd;
        session.ShowVolume = ShowVolume;
        session.ShowSmaLinesSbm = ShowSmaLinesSbm;
        session.ShowNwe = ShowNwe;
        session.ShowNweRepainting = ShowNweRepainting;
        session.ShowNweBb = ShowNweBb;
        session.ShowNweBbAtrRb = ShowNweBbAtrRb;
        session.ShowPSar = ShowPSar;
        session.ShowBollingerBand = ShowBollingerBand;
        session.ShowKeltnerChannel = ShowKeltnerChannel;
        session.ShowAtrRbBands = ShowAtrRbBands;
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
    }

}
