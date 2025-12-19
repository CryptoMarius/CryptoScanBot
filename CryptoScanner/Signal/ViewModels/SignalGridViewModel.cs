using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Signal.Model;

using System.Collections.ObjectModel;

namespace CryptoScanner.Signal.ViewModels;

/// <summary>
/// ViewModel for the Signal Grid
/// Manages trading signals and their display
/// </summary>
public partial class SignalGridViewModel : ObservableObject
{
    private readonly DispatcherTimer? _updateTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    [ObservableProperty]
    private ObservableCollection<SignalInfo> _signals = [];



    public SignalGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SignalGridViewModel constructor called");

        _updateTimer.Tick += TimerAddSignalsTick;
        _updateTimer.Start();
    }

    private void TimerAddSignalsTick(object? sender, EventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        // Speed up adding signals
        if (GlobalData.SignalQueue.Count > 0)
        {
            if (Monitor.TryEnter(GlobalData.SignalQueue))
            {
                try
                {
                    while (GlobalData.SignalQueue.Count > 0)
                    {
                        CryptoSignal signal = GlobalData.SignalQueue.Dequeue();
                        if (signal != null)
                        {
                            Signals.Add(new SignalInfo
                            {
                                SignalObject = signal,
                            });
                        }
                    }
                    //Sorting? / AddRange()?
                }
                finally
                {
                    Monitor.Exit(GlobalData.SignalQueue);
                }
            }
        }
    }

    /// <summary>
    /// Command to open signal in external program
    /// Triggered from context menu
    /// </summary>
    [RelayCommand]
    private static void OpenExternalProgram(object? parameter)
    {
        if (parameter is not SignalInfo signal)
            return;

        // Implement your external program logic here
        System.Diagnostics.Debug.WriteLine($"Opening {signal.Symbol} in external program");
    }

    /// <summary>
    /// Command to view signal details
    /// </summary>
    [RelayCommand]
    private static void ViewDetails(object? parameter)
    {
        if (parameter is not SignalInfo signal)
            return;

        System.Diagnostics.Debug.WriteLine($"Viewing details for signal: {signal.Symbol}");
    }

    /// <summary>
    /// Command to copy signal to clipboard
    /// </summary>
    [RelayCommand]
    private static void CopySignal(object? parameter)
    {
        if (parameter is not SignalInfo signal)
            return;

        var text = $"{signal.Symbol} - {signal.Side} @ {signal.SignalPrice:F8}";
        System.Diagnostics.Debug.WriteLine($"Copying signal to clipboard: {text}");
    }


}
