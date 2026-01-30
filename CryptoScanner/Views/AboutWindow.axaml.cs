using Avalonia.Controls;

using CryptoScanner.ViewModels;

namespace CryptoScanner.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var viewModel = new AboutWindowViewModel();
        DataContext = viewModel;

        // Subscribe to close event
        viewModel.CloseRequested += (s, e) => Close();
    }


}
