using Avalonia.Controls;
using Avalonia.Interactivity;

using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.Emulator.Views;

/// <summary>
/// Dialog for the "Run algorithms..." command: lets the user pick a subset of the registered
/// algorithms before they're run one by one. Closes with a bool result (true = run) so the
/// caller can use <c>ShowDialog&lt;bool&gt;</c> and then read <see cref="ViewModel"/>'s selection.
/// </summary>
public partial class AlgorithmSelectionWindow : Window
{
    public AlgorithmSelectionViewModel ViewModel { get; }

    public AlgorithmSelectionWindow()
    {
        InitializeComponent();
        ViewModel = new AlgorithmSelectionViewModel();
        DataContext = ViewModel;
    }


    private void OnOk(object? sender, RoutedEventArgs e)
    {
        // Validation lives in the VM; on failure it fills ValidationMessage and we keep the dialog
        // open so the user can fix the selection.
        if (!ViewModel.TryGetSelection(out _))
            return;

        Close(true);
    }


    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
