using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Zones;

namespace CryptoScanner.Visualisation.ViewModels;

public partial class DisplayOptionsViewModel : ObservableObject
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
    private bool _showDlzZones = false;

    [ObservableProperty]
    private bool _showFvgZones = false;

    [ObservableProperty]
    private bool _showDtb = false;

    [ObservableProperty]
    private bool _showSignals = false;

    [ObservableProperty]
    private bool _showPivots = false;

    [ObservableProperty]
    private bool _transparent = false;

    public void LoadFromSession(ZoneSession session)
    {
        ShowSmaLinesSbm = session.ShowSmaLinesSbm;
        ShowNadarayaWatsonEnvelope = session.ShowNadarayaWatsonEnvelope;
        ShowNadarayaWatsonEnvelopeRepainting = session.ShowNadarayaWatsonEnvelopeRepainting;
        ShowBollingerBand = session.ShowBollingerBand;
        ShowDlzZones = session.ShowDlzZones;
        ShowFvgZones = session.ShowFvgZones;
        ShowDtb = session.ShowDtb;
        ShowSignals = session.ShowSignals;
        ShowPivots = session.ShowPivots;
        Transparent = session.Transparent;
    }

    public void SaveToSession(ZoneSession session)
    {
        session.ShowSmaLinesSbm = ShowSmaLinesSbm;
        session.ShowNadarayaWatsonEnvelope = ShowNadarayaWatsonEnvelope;
        session.ShowNadarayaWatsonEnvelopeRepainting = ShowNadarayaWatsonEnvelopeRepainting;
        session.ShowBollingerBand = ShowBollingerBand;
        session.ShowDlzZones = ShowDlzZones;
        session.ShowFvgZones = ShowFvgZones;
        session.ShowDtb = ShowDtb;
        session.ShowSignals = ShowSignals;
        session.ShowPivots = ShowPivots;
        session.Transparent = Transparent;
    }
}
