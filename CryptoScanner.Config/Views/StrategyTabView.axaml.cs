using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using CryptoScanner.Config.ViewModels;
using CryptoScanner.Core.Contracts;

using System.Diagnostics;

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

        foreach (var configView in PluginManager.ConfigViews.OrderBy(v => v.TabHeader))
        {
            object content = configView.CreateSettingsView();

            if (!string.IsNullOrEmpty(configView.WikiUrl))
            {
                var wikiLink = new TextBlock
                {
                    Text = "Wiki ↗",
                    FontSize = 12,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Foreground = new SolidColorBrush(Colors.DodgerBlue),
                    TextDecorations = TextDecorations.Underline,
                    Margin = new Thickness(6, 4, 0, 0),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                };
                var url = configView.WikiUrl;
                wikiLink.Tapped += (_, _) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

                var wrapper = new DockPanel { LastChildFill = true };
                DockPanel.SetDock(wikiLink, Dock.Top);
                wrapper.Children.Add(wikiLink);
                wrapper.Children.Add((Control)content);
                content = wrapper;
            }

            var tab = new TabItem
            {
                Header = configView.TabHeader,
                FontSize = 15,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 2),
                Content = content,
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
