using Avalonia.Controls;
using Avalonia.Input;

using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.Emulator.Views;

public partial class RunResultsView : UserControl
{
    public RunResultsView()
    {
        InitializeComponent();

        // Wire the double-click drill-down. Done in code-behind because the handler needs the
        // owner Window (to root the modal positions dialog) and the selected row — both easier
        // here than via an MVVM binding. The owner is resolved at click-time from the visual
        // tree because this control lives inside MainWindow's TabControl, not its own Window.
        RunsGrid.DoubleTapped += OnRunDoubleTapped;
    }


    private async void OnRunDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (RunsGrid.SelectedItem is not RunRow row)
            return;
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var positions = new RunPositionsWindow(row);
        await positions.ShowDialog(owner);
    }
}
