using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using CryptoScanner.ViewModels;

namespace CryptoScanner.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        AvaloniaXamlLoader.Load(this);
        //InitializeComponent();

        var viewModel = new AboutViewModel();
        DataContext = viewModel;

        // Subscribe to close event
        viewModel.CloseRequested += (s, e) => Close();
    }

    private void InitializeComponent()
    {
    }

}
