using Avalonia.Controls;
using Avalonia.Threading;

using CryptoScanner.ViewModels;

namespace CryptoScanner.Views;

public partial class LogView : UserControl
{
    public LogView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is LogViewModel viewModel)
        {
            // Subscribe to scroll request
            viewModel.RequestScrollToEnd += OnRequestScrollToEnd;
        }
    }

    private void OnRequestScrollToEnd(object? sender, System.EventArgs e)
    {
        // Scroll to bottom on UI thread
        Dispatcher.UIThread.Post(() =>
        {
            LogScrollViewer.ScrollToEnd();
        }, DispatcherPriority.Background);
    }

    private void SelectableTextBlock_ActualThemeVariantChanged(object? sender, System.EventArgs e)
    {
    }
}
