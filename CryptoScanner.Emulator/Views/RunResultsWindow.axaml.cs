using Avalonia.Controls;
using Avalonia.Input;

using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.Emulator.Views;

public partial class RunResultsWindow : Window
{
    public RunResultsViewModel ViewModel { get; }

    public RunResultsWindow()
    {
        InitializeComponent();
        ViewModel = new RunResultsViewModel();
        DataContext = ViewModel;

        // Wire the double-click drill-down. Done in code-behind because the handler needs the
        // owner Window (to root the modal) and the selected row — both are easier here than in
        // an MVVM-style binding.
        RunsGrid.DoubleTapped += OnRunDoubleTapped;
    }


    private async void OnRunDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (RunsGrid.SelectedItem is not RunRow row)
            return;

        var positions = new RunPositionsWindow(row);
        await positions.ShowDialog(this);
    }
}
