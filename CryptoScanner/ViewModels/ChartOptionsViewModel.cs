using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Zones;

namespace CryptoScanner.ViewModels;

public partial class ChartOptionsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _showSmaLinesSbm = false;

    [ObservableProperty]
    private bool _showNadarayaWatsonEnvelope = false;

    [ObservableProperty]
    private bool _showNadarayaWatsonEnvelopeRepainting = false;

    [ObservableProperty]
    private bool _showBollingerBand = false;

    [ObservableProperty]
    private bool _showPSar = false;

    [ObservableProperty]
    private bool _showDlzZones = false;

    [ObservableProperty]
    private bool _showFvgZones = false;

    [ObservableProperty]
    private bool _showDtb = false;

    [ObservableProperty]
    private bool _showSignals = false;

    [ObservableProperty]
    private bool _showPoints = false;

    [ObservableProperty]
    private bool _transparent = false;

    public void LoadFromSession(ZoneSession session)
    {
        // Options
        ShowSmaLinesSbm = session.ShowSmaLinesSbm;
        ShowNadarayaWatsonEnvelope = session.ShowNadarayaWatsonEnvelope;
        ShowNadarayaWatsonEnvelopeRepainting = session.ShowNadarayaWatsonEnvelopeRepainting;
        ShowPSar = session.ShowPSar;
        ShowBollingerBand = session.ShowBollingerBand;
        ShowDlzZones = session.ShowDlzZones;
        ShowFvgZones = session.ShowFvgZones;
        ShowDtb = session.ShowDtb;
        ShowSignals = session.ShowSignals;
        ShowPoints = session.ShowPoints;

        // misc
        Transparent = session.Transparent;
    }

    public void SaveToSession(ZoneSession session)
    {
        // Options
        session.ShowSmaLinesSbm = ShowSmaLinesSbm;
        session.ShowNadarayaWatsonEnvelope = ShowNadarayaWatsonEnvelope;
        session.ShowNadarayaWatsonEnvelopeRepainting = ShowNadarayaWatsonEnvelopeRepainting;
        session.ShowPSar = ShowPSar;
        session.ShowBollingerBand = ShowBollingerBand;
        session.ShowDlzZones = ShowDlzZones;
        session.ShowFvgZones = ShowFvgZones;
        session.ShowDtb = ShowDtb;
        session.ShowSignals = ShowSignals;
        session.ShowPoints = ShowPoints;

        // misc
        session.Transparent = Transparent;
    }

}
