using Avalonia.Controls;

using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.Emulator.Views;

public partial class RunSignalsWindow : Window
{
    public RunSignalsWindow() : this(new RunRow())
    {
        // Designer-only path: empty constructor for the XAML preview.
    }

    public RunSignalsWindow(RunRow run)
    {
        InitializeComponent();
        DataContext = new RunSignalsViewModel(run);
    }
}
