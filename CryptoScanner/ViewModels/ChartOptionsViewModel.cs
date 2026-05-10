using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Zones;

namespace CryptoScanner.ViewModels;

public partial class ChartOptionsViewModel : ObservableObject
{
    
    [ObservableProperty]
    private bool _showBbma = false;

    [ObservableProperty]
    private bool _showSmaLinesSbm = false;

    [ObservableProperty]
    private bool _showNadarayaWatsonEnvelope = false;

    [ObservableProperty]
    private bool _showNadarayaWatsonEnvelopeRepainting = false;

    [ObservableProperty]
    private bool _showGaussianFilter = false;

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
        ShowSmaLinesSbm = session.ShowSmaLinesSbm;
        ShowNadarayaWatsonEnvelope = session.ShowNadarayaWatsonEnvelope;
        ShowNadarayaWatsonEnvelopeRepainting = session.ShowNadarayaWatsonEnvelopeRepainting;
        ShowGaussianFilter = session.ShowGaussianFilter;
        ShowPSar = session.ShowPSar;
        ShowBollingerBand = session.ShowBollingerBand;
        ShowDlzZones = session.ShowDlzZones;
        ShowFvgZones = session.ShowFvgZones;
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
        session.ShowSmaLinesSbm = ShowSmaLinesSbm;
        session.ShowNadarayaWatsonEnvelope = ShowNadarayaWatsonEnvelope;
        session.ShowNadarayaWatsonEnvelopeRepainting = ShowNadarayaWatsonEnvelopeRepainting;
        session.ShowGaussianFilter = ShowGaussianFilter;
        session.ShowPSar = ShowPSar;
        session.ShowBollingerBand = ShowBollingerBand;
        session.ShowDlzZones = ShowDlzZones;
        session.ShowFvgZones = ShowFvgZones;
        session.ShowDtb = ShowDtb;
        session.ShowSignals = ShowSignals;
        session.ShowPositions = ShowPositions;
        session.ShowPoints = ShowPoints;
        session.ShowCandles = ShowCandles;

        // misc
        session.Transparent = Transparent;
    }

}
