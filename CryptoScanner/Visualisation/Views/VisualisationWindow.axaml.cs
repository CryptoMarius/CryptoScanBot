using Avalonia.Controls;

using CryptoScanner.Visualisation.ViewModels;

namespace CryptoScanner.Visualisation.Views;

public partial class VisualisationWindow : Window
{
    public VisualisationWindow()
    {
        InitializeComponent();

        DataContext = new VisualisationViewModel();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is VisualisationViewModel vm)
        {
            vm.OnClosing();
        }
        
        base.OnClosing(e);
    }
}
