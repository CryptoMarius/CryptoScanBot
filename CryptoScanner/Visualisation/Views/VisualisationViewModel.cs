using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.Visualisation.ViewModels;

public partial class VisualisationViewModel : ObservableObject
{
    // Sub-ViewModels for modular UI
    [ObservableProperty]
    private SymbolSelectorViewModel _symbolSelector;

    [ObservableProperty]
    private TrendSettingsViewModel _trendSettings;

    [ObservableProperty]
    private FibSettingsViewModel _fibSettings;

    [ObservableProperty]
    private DisplayOptionsViewModel _displayOptions;

    [ObservableProperty]
    private PlaybackControlsViewModel _playbackControls;

    // Main plot model for OxyPlot
    [ObservableProperty]
    private PlotModel _plotModel;

    // Session and data
    private ZoneSession _session = new();
    private ZoneConfig? _data;

    [ObservableProperty]
    private string _windowTitle = "Crypto Visualisation";

    public VisualisationViewModel()
    {
        // Initialize sub-ViewModels
        _symbolSelector = new SymbolSelectorViewModel();
        _trendSettings = new TrendSettingsViewModel();
        _fibSettings = new FibSettingsViewModel();
        _displayOptions = new DisplayOptionsViewModel();
        _playbackControls = new PlaybackControlsViewModel();

        // Initialize plot
        _plotModel = new PlotModel { Title = "Chart" };
        InitializePlot();

        // Subscribe to changes from sub-ViewModels
        SymbolSelector.PropertyChanged += OnSymbolChanged;
        TrendSettings.PropertyChanged += OnTrendSettingsChanged;
        FibSettings.PropertyChanged += OnFibSettingsChanged;
        DisplayOptions.PropertyChanged += OnDisplayOptionsChanged;
        PlaybackControls.PlaybackRequested += OnPlaybackRequested;

        // Load session
        LoadSession();
    }

    private void InitializePlot()
    {
        PlotModel.Axes.Clear();
        
        // X-axis (Time)
        var xAxis = new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd-MM HH:mm",
            Title = "Time",
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        };
        PlotModel.Axes.Add(xAxis);

        // Y-axis (Price)
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Right,
            Title = "Price",
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        };
        PlotModel.Axes.Add(yAxis);
    }

    private void LoadSession()
    {
        _session = ZoneSession.LoadSessionSettings();
        _session.UseOptimizing = false;

        // Load settings into sub-ViewModels
        SymbolSelector.LoadFromSession(_session);
        TrendSettings.LoadFromSession(_session);
        FibSettings.LoadFromSession(_session);
        DisplayOptions.LoadFromSession(_session);
    }

    private void SaveSession()
    {
        SymbolSelector.SaveToSession(_session);
        TrendSettings.SaveToSession(_session);
        FibSettings.SaveToSession(_session);
        DisplayOptions.SaveToSession(_session);

        //ZoneSession.SaveSessionSettings(_session);
    }

    #region Event Handlers

    private void OnSymbolChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SymbolSelectorViewModel.SelectedSymbol) ||
            e.PropertyName == nameof(SymbolSelectorViewModel.SelectedInterval))
        {
            // Symbol or interval changed - reload chart
            RefreshCommand.Execute(null);
        }
    }

    private void OnTrendSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Trend settings changed - refresh display
        RefreshPlot();
    }

    private void OnFibSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // FIB settings changed - refresh display
        RefreshPlot();
    }

    private void OnDisplayOptionsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Display options changed - refresh plot
        RefreshPlot();
    }

    private void OnPlaybackRequested(int direction)
    {
        // Handle playback navigation
        NavigateCandles(direction);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void Refresh()
    {
        Calculate(false);
    }

    [RelayCommand]
    private void Calculate()
    {
        Calculate(true);
    }

    [RelayCommand]
    private void ZoomLast()
    {
        // Zoom to last candles
        if (_data != null)
        {
            // TODO: Implement zoom logic
            RefreshPlot();
        }
    }

    [RelayCommand]
    private void OpenTradingApp()
    {
        if (_data != null)
        {
            // TODO: Open trading app
            Debug.WriteLine($"Open trading app for {SymbolSelector.SelectedSymbol}");
        }
    }

    #endregion

    #region Core Logic

    private void Calculate(bool recalculate)
    {
        WindowTitle = $"Loading {SymbolSelector.SelectedSymbol}...";

        try
        {
            // TODO: Implement actual calculation logic from original code
            // This is a placeholder that shows the structure

            // Load or calculate zones
            if (recalculate)
            {
                // Recalculate zones
                Debug.WriteLine("Recalculating zones...");
            }
            else
            {
                // Just refresh
                Debug.WriteLine("Refreshing display...");
            }

            RefreshPlot();
            WindowTitle = $"{SymbolSelector.SelectedSymbol} - {SymbolSelector.SelectedInterval}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
            WindowTitle = $"Error: {ex.Message}";
        }
    }

    private void RefreshPlot()
    {
        PlotModel.InvalidatePlot(true);
    }

    private void NavigateCandles(int direction)
    {
        // TODO: Implement candle navigation for playback
        Debug.WriteLine($"Navigate candles: {direction}");
    }

    #endregion

    public void OnClosing()
    {
        SaveSession();
    }
}
