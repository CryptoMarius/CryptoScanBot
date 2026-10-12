using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class TraderMiscSettingsView : UserControl
{
    public TraderMiscSettingsView()
    {
        InitializeComponent();

        if (Design.IsDesignMode && DataContext == null)
        {
            DataContext = new TraderMiscSettingsViewModel();
        }
    }

    private async void OnResetPaperAssetsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not TraderMiscSettingsViewModel viewModel)
            return;
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        // Deliberately behind a confirmation: this wipes the balances of a running paper session.
        // Built here because the ConfirmDialog of the scanner lives in a project this one cannot see.
        if (await Confirm(owner, "Throw every paper balance away and hand out the default paper assets again?",
            "Reset paper assets"))
            viewModel.ResetPaperAssets();
    }

    /// <summary>A Yes/No question in the layout of the scanner's ConfirmDialog.</summary>
    private static Task<bool> Confirm(Window owner, string message, string title)
    {
        Window dialog = new()
        {
            Title = title,
            Width = 360,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
        };

        Button yes = new() { Content = "Yes", Width = 80 };
        Button no = new() { Content = "No", Width = 80 };
        yes.Click += (_, _) => dialog.Close(true);
        no.Click += (_, _) => dialog.Close(false);

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 20,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap, FontSize = 13 },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { yes, no },
                },
            },
        };

        return dialog.ShowDialog<bool>(owner);
    }
}
