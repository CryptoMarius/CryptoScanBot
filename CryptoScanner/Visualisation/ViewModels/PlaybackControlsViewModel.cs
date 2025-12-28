using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CryptoScanner.Visualisation.ViewModels;

public partial class PlaybackControlsViewModel : ObservableObject
{
    public event Action<int>? PlaybackRequested;

    [ObservableProperty]
    private string _currentIntervalDisplay = "";

    [ObservableProperty]
    private string _maxTimeDisplay = "";

    [RelayCommand]
    private void ZoomIn()
    {
        // Zoom in (smaller interval)
        PlaybackRequested?.Invoke(-1);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        // Zoom out (larger interval)
        PlaybackRequested?.Invoke(+1);
    }

    [RelayCommand]
    private void NavigateLeft()
    {
        // Go back in time
        PlaybackRequested?.Invoke(-1);
    }

    [RelayCommand]
    private void NavigateRight()
    {
        // Go forward in time
        PlaybackRequested?.Invoke(+1);
    }

    public void UpdateIntervalDisplay(string interval)
    {
        CurrentIntervalDisplay = interval;
    }

    public void UpdateMaxTimeDisplay(string maxTime)
    {
        MaxTimeDisplay = maxTime;
    }
}
