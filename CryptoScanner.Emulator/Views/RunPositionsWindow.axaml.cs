using Avalonia.Controls;

using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.Emulator.Views;

public partial class RunPositionsWindow : Window
{
    public RunPositionsWindow() : this(new RunRow())
    {
        // Designer-only path: empty constructor for the XAML preview.
    }

    public RunPositionsWindow(RunRow run)
    {
        InitializeComponent();
        DataContext = new RunPositionsViewModel(run);
    }
}
