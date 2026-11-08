using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Threading;

using CryptoScanner.Chart.Services;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Settings;
using CryptoScanner.ViewModels;

using System.ComponentModel;

namespace CryptoScanner.Views;

public partial class ChartWindow : Window
{
    private readonly ApplicationStateService _applicationStateService;

    public ChartWindow()
    {
        // Runtime - get service from App
        _applicationStateService = GlobalData.GetService<ApplicationStateService>()
            ?? throw new InvalidOperationException("ApplicationStateService not registered");

        InitializeComponent();

        // Centred the first time; a saved position (RestoreWindowState) switches this to Manual
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        // Restore window position, size, state and splitter
        _applicationStateService.RestoreWindowState("ChartWindow", this);
        _applicationStateService.TrackWindowState("ChartWindow", this);

        Closing += OnWindowClosing; // - save state

        // The Indicators menu closes on a click anywhere else in the window, as in the Photino chart.
        // Tunnel and handledEventsToo: the chart (OxyPlot) handles its own mouse presses.
        AddHandler(PointerPressedEvent, OnWindowPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(KeyDownEvent, OnIndicatorsKeyDown, RoutingStrategies.Tunnel);

        // After OxyPlot's first render ActualMinimum/ActualMaximum are set correctly.
        // Refresh the x-axis ticks at that point so the initial labels are correct.
        // Also kick off the FIRST data refresh here, NOT in the ChartWindowViewModel
        // constructor — running it in the ctor races with Window.Show()'s
        // ExecuteInitialLayoutPass, mutating PlotModel.Series while OxyPlot's Render
        // iterates it, and throws NRE in PlotElementUtilities.GetClippingRect.
        // By the time Opened fires the initial layout pass is complete, so any Series
        // mutations the refresh does are safe.
        Opened += (_, _) =>
        {
            if (DataContext is ChartWindowViewModel vm)
            {
                Dispatcher.UIThread.Post(
                    () => vm.RefreshAxisTicks(),
                    DispatcherPriority.Background);

                Dispatcher.UIThread.Post(
                    () => _ = vm.RefreshCommand.ExecuteAsync(null),
                    DispatcherPriority.Background);
            }
        };

        if (DataContext == null)
        {
            DataContext = new ChartWindowViewModel();
        }

        // The PlotView is hosted in XAML (Model bound to PlotModel). Hand the control and its
        // controller to the VM so ZoomLast / crosshair / axis-tick logic can drive it directly,
        // exactly like the original named-PlotView pattern did before the refactoring.
        if (DataContext is ChartWindowViewModel chartVm)
        {
            PlotViewControl.Controller = chartVm.PlotController;
            chartVm.PlotView = PlotViewControl;
        }
        //if (DataContext is VisualisationViewModel vm)
        //{
        //    vm.PlotView = this.FindControl<PlotView>("PlotViewControl")
        //    ?? throw new InvalidOperationException("PlotViewControl not found");
        //}

        //// Change behaviour
        //PlotViewControl.AttachedToVisualTree += (_, _) =>
        //{
        //    if (PlotViewControl.ActualController is { } controller)
        //    {
        //        //controller.UnbindAll(); // leave the original intact, we just need to tweak it a bit
        //        controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
        //        controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control, PlotCommands.ZoomRectangle);
        //        controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control | OxyModifierKeys.Alt, 2, PlotCommands.ResetAt);
        //        controller.UnbindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Shift);
        //        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control | OxyModifierKeys.Alt, PlotCommands.ZoomRectangle);
        //        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control, 2, PlotCommands.ResetAt);
        //        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Alt, PlotCommands.PanAt);
        //        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Shift, PlotCommands.SnapTrack);
        //    }
        //};

    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is ChartWindowViewModel vm)
        {
            vm.OnClosing();
        }

        base.OnClosing(e);
    }


    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        IndicatorsPopup.IsOpen = false;

        // Save window state
        _applicationStateService.SaveWindowState("ChartWindow", this);
    }

    /// <summary>The Indicators button opens and closes its menu.</summary>
    private void OnIndicatorsClick(object? sender, RoutedEventArgs e)
    {
        IndicatorsPopup.IsOpen = !IndicatorsPopup.IsOpen;
    }

    /// <summary>The arrow on the button follows the menu, as in the Photino chart.</summary>
    private void OnIndicatorsPopupChanged(object? sender, EventArgs e)
    {
        IndicatorsButton.Content = IndicatorsPopup.IsOpen ? "Indicators ▲" : "Indicators ▼";
    }

    /// <summary>
    /// A click in the window outside the button closes the menu. Clicks inside the menu DO get
    /// here: the root of a popup routes its events on through the Popup into this window. Closing
    /// on those took the gear away before its Click, so the colour dialog never opened.
    /// </summary>
    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IndicatorsPopup.IsOpen)
            return;
        if (e.Source is Avalonia.Visual source)
        {
            if (source == IndicatorsButton || IndicatorsButton.IsVisualAncestorOf(source))
                return;
            if (IndicatorsPopup.Child is Avalonia.Visual menu && (source == menu || menu.IsVisualAncestorOf(source)))
                return;
            // A ComboBox inside the menu opens a popup of its own (the trend pickers)
            if (source.GetVisualRoot() is Avalonia.Controls.Primitives.PopupRoot)
                return;
        }
        if (IndicatorsPopup.IsPointerOverPopup)
            return;
        IndicatorsPopup.IsOpen = false;
    }

    private void OnIndicatorsKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IndicatorsPopup.IsOpen)
        {
            IndicatorsPopup.IsOpen = false;
            e.Handled = true;
        }
    }

    /// <summary>A gear of a fixed item of the Indicators menu; its Tag is "style key|dialog title".</summary>
    private void OnStyleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { Tag: string tag })
            return;

        int separator = tag.IndexOf('|');
        if (separator < 0)
            OpenStyleDialog(tag, tag);
        else
            OpenStyleDialog(tag[..separator], tag[(separator + 1)..]);
    }

    /// <summary>A gear of an item in the Overlays list of the Indicators menu.</summary>
    private void OnOverlayStyleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: PluginOverlayToggle toggle })
            OpenStyleDialog(toggle.GroupKey, toggle.Label);
    }

    /// <summary>
    /// The colours and lines of one item, the same dialog as behind the gear in the Photino chart.
    /// The menu closes first: leaving it open behind the dialog puts two layers over the chart you
    /// are judging the colours against.
    /// </summary>
    private async void OpenStyleDialog(string key, string title)
    {
        try
        {
            var definitions = ChartStyleSettings.DefinitionsForOverlay(key).ToList();
            if (definitions.Count == 0)
                return;

            //IndicatorsButton.Flyout?.Hide();
            IndicatorsPopup.IsOpen = false;

            var dialogViewModel = new ChartStyleDialogViewModel(title, definitions);
            dialogViewModel.Changed += ScheduleStyleRedraw;
            var dialog = new ChartStyleWindow { DataContext = dialogViewModel };
            await dialog.ShowDialog(this);
            dialogViewModel.Changed -= ScheduleStyleRedraw;
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "ChartWindow.OpenStyleDialog");
        }
    }

    private DispatcherTimer? _styleRedrawTimer;

    /// <summary>
    /// Redraw once the picking has paused. A colour picker reports every step of a drag, and
    /// rebuilding the whole chart for each of them made the dialog lag behind the mouse.
    /// </summary>
    private void ScheduleStyleRedraw()
    {
        if (_styleRedrawTimer == null)
        {
            _styleRedrawTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _styleRedrawTimer.Tick += (_, _) =>
            {
                _styleRedrawTimer.Stop();
                if (DataContext is ChartWindowViewModel vm)
                    _ = vm.RefreshCommand.ExecuteAsync(null);
            };
        }

        _styleRedrawTimer.Stop();
        _styleRedrawTimer.Start();
    }
}