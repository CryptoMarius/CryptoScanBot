using Avalonia.Controls;

using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.Emulator.Views;

public partial class SetupWindow : Window
{
    public SetupWindowViewModel ViewModel { get; }

    public SetupWindow()
    {
        InitializeComponent();
        ViewModel = new SetupWindowViewModel();
        DataContext = ViewModel;
    }
}
