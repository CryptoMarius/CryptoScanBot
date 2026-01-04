using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Services;
using CryptoScanner.Visualisation.ViewModels;

using System.ComponentModel;

namespace CryptoScanner.Visualisation.Views;

public partial class VisualisationWindow : Window
{
    private readonly ApplicationStateService _applicationStateService;

    public VisualisationWindow()
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
            DataContext = new VisualisationViewModel();
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is VisualisationViewModel vm)
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