using Avalonia.Controls;
using Avalonia.Threading;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
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

        // Restore window position, size, state and splitter
        _applicationStateService.RestoreWindowState("ChartWindow", this);

        Closing += OnWindowClosing; // - save state

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

#if !DEBUG
        ShowBbmaCheckBox.IsVisible = false;
#endif
#if !EXPERIMENTAL
        ShowAtrRbCheckBox.IsVisible = false;
        ShowBabaCheckBox.IsVisible = false;
        ShowSlideCheckBox.IsVisible = false;
        ShowBreCheckBox.IsVisible = false;
#endif

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
        // Save window state
        _applicationStateService.SaveWindowState("ChartWindow", this);
    }
}