using Avalonia.Controls;
using Avalonia.Interactivity;

using CryptoScanner.Core.Core;
using CryptoScanner.Emulator.Engine;
using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.Emulator.Views;

/// <summary>
/// Dialog that replaces the old "Open run.json" button: edits the per-run parameters (label,
/// replay period, symbol selection) and writes them back to CryptoScanBot-Emulator.json on OK. Closes with
/// a bool result (true = saved) so the caller can use <c>ShowDialog&lt;bool&gt;</c>.
/// </summary>
public partial class RunConfigWindow : Window
{
    public RunConfigViewModel ViewModel { get; }

    public RunConfigWindow()
    {
        InitializeComponent();

        // A hover hint per column header, like the grids of the Results tab.
        Helpers.ColumnHints.Apply(SymbolsGrid, Helpers.ColumnHints.Symbols);

        ViewModel = new RunConfigViewModel();
        DataContext = ViewModel;
    }


    private void OnOk(object? sender, RoutedEventArgs e)
    {
        // Validation lives in the VM; on failure it fills ValidationMessage and we keep the dialog
        // open so the user can fix the input.
        if (!ViewModel.TryBuild(out EmulatorRunConfig config))
            return;

        //RunConfigFile.Save(config);
        // Refused when the file on disk could not be read (31-10-2026): the dialog stays open and
        // says so, instead of saving the placeholder over the real coin list and period.
        if (!RunConfigFile.Save(config))
        {
            ViewModel.ValidationMessage = $"{RunConfigFile.FileName} could not be read, so it was not overwritten (see the log). Repair or remove the file first.";
            return;
        }

        // The asset choice is a scanner setting, not part of the run config
        if (ViewModel.ApplyAssetSettings())
        {
            GlobalData.SaveConfiguration();
            GlobalData.AddTextToLogTab($"Start assets {ViewModel.PaperAssetSummary}, asset management {(ViewModel.UseAssetManagement ? "on" : "off")} - settings saved");
        }
        Close(true);
    }


    private async void OnAssets(object? sender, RoutedEventArgs e)
    {
        PaperAssetDefaultsWindow window = new(ViewModel.PaperAssetDefaults);
        if (await window.ShowDialog<bool>(this))
            ViewModel.SetPaperAssetDefaults(window.ViewModel.Result);
    }


    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
