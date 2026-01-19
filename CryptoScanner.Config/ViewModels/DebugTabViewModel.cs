using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class DebugTabViewModel : ObservableObject
{   
    [ObservableProperty]
    private bool _debugTrendCalculation = false;

    [ObservableProperty]
    private bool _debugSignalStrength = false; 

    [ObservableProperty]
    private string _debugSymbol = ""; 

    [ObservableProperty]
    private bool _debugKLineReceive = false; 

    [ObservableProperty]
    private bool _debugSignalCreate = false; 

    [ObservableProperty]
    private bool _debugAssetManagement = false;

    [ObservableProperty]
    private bool _debugZoneCandles = false;

    public void LoadConfig(SettingsGeneral settings)
    {
        DebugTrendCalculation = settings.DebugTrendCalculation;
        DebugSignalStrength = settings.DebugSignalStrength;
        DebugSymbol = settings.DebugSymbol.Trim().ToUpper();
        DebugKLineReceive = settings.DebugKLineReceive;
        DebugSignalCreate = settings.DebugSignalCreate;
        DebugAssetManagement = settings.DebugAssetManagement;
        DebugZoneCandles = settings.DebugZoneCandles;
    }

    public void SaveConfig(SettingsGeneral settings)
    {
        settings.DebugTrendCalculation = DebugTrendCalculation;
        settings.DebugSignalStrength = DebugSignalStrength;
        settings.DebugSymbol = DebugSymbol.Trim().ToUpper();
        settings.DebugKLineReceive = DebugKLineReceive;
        settings.DebugSignalCreate = DebugSignalCreate;
        settings.DebugAssetManagement = DebugAssetManagement;
        settings.DebugZoneCandles = DebugZoneCandles;
    }
}
