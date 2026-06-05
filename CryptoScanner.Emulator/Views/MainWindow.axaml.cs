using Avalonia.Controls;

using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.Emulator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
