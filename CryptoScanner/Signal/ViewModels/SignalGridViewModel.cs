using Avalonia.Controls;
using Avalonia.Interactivity;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Signal.Model;

using System.Collections.ObjectModel;

namespace CryptoScanner.Signal.ViewModels;

/// <summary>
/// ViewModel for the Signal Grid
/// Manages trading signals and their display
/// </summary>
public partial class SignalGridViewModel : ObservableObject
{
    /// <summary>
    /// Collection of signals to display in the grid
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SignalInfo> _signals = [];

    /// <summary>
    /// Currently selected signal
    /// </summary>
    [ObservableProperty]
    private SignalInfo? _selectedSignal;

    public SignalGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SignalGridViewModel constructor called");


        // Laad symbols direct in de observable collection
        foreach (var signal in GlobalData.SignalQueue)
        {
            Signals.Add(new SignalInfo
            {
                SignalObject = signal,
            });
        }

        //// Sorteer als er een sort configuratie is
        //if (SignalShared.Columns.SortColumn != null)
        //{
        //    SortSymbols();
        //}
    }

    /// <summary>
    /// Command to open signal in external program
    /// Triggered from context menu
    /// </summary>
    [RelayCommand]
    private void OpenExternalProgram(object? parameter)
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
    private void ViewDetails(object? parameter)
    {
        if (parameter is not SignalInfo signal)
            return;

        System.Diagnostics.Debug.WriteLine($"Viewing details for signal: {signal.Symbol}");
    }

    /// <summary>
    /// Command to copy signal to clipboard
    /// </summary>
    [RelayCommand]
    private void CopySignal(object? parameter)
    {
        if (parameter is not SignalInfo signal)
            return;

        var text = $"{signal.Symbol} - {signal.Side} @ {signal.SignalPrice:F8}";
        System.Diagnostics.Debug.WriteLine($"Copying signal to clipboard: {text}");
    }
}
