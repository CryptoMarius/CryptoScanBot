using Avalonia.Controls;

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

        if (DataContext == null)
        {
            DataContext = new ChartWindowViewModel();
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