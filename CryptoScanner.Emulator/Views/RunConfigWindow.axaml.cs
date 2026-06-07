using Avalonia.Controls;
using Avalonia.Interactivity;

using CryptoScanner.Emulator.Engine;
using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.Emulator.Views;

/// <summary>
/// Dialog that replaces the old "Open run.json" button: edits the per-run parameters (label,
/// replay period, symbol selection) and writes them back to emulator-run.json on OK. Closes with
/// a bool result (true = saved) so the caller can use <c>ShowDialog&lt;bool&gt;</c>.
/// </summary>
public partial class RunConfigWindow : Window
{
    public RunConfigViewModel ViewModel { get; }

    public RunConfigWindow()
    {
        InitializeComponent();
        ViewModel = new RunConfigViewModel();
        DataContext = ViewModel;
    }


    private void OnOk(object? sender, RoutedEventArgs e)
    {
        // Validation lives in the VM; on failure it fills ValidationMessage and we keep the dialog
        // open so the user can fix the input.
        if (!ViewModel.TryBuild(out EmulatorRunConfig config))
            return;

        RunConfigFile.Save(config);
        Close(true);
    }


    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
