using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using CryptoScanner.Config.ViewModels;
using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Config.Views;

public partial class StrategyTabView : UserControl
{
    public StrategyTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyTabViewModel();
        }

        foreach (var configView in PluginManager.ConfigViews)
        {
            var tab = new TabItem
            {
                Header = configView.TabHeader,
                FontSize = 15,
                FontWeight = FontWeight.Bold,
                Margin = new Avalonia.Thickness(0, 0, 0, 2),
                Content = configView.CreateSettingsView(),
                Tag = configView,
            };
            this.FindControl<TabControl>("StrategyTabControl")!.Items.Add(tab);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
