using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;
using CryptoScanner.Core.Trend;
using CryptoScanner.Visualisation.Chart;
using CryptoScanner.Core.Settings;

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

    // Crosshair annotations
    private LineAnnotation? _verticalLine;
    private LineAnnotation? _horizontalLine;

    [ObservableProperty]
    private string _windowTitle = "Crypto Visualisation";

    [ObservableProperty]
    private bool _isCalculating = false;

    private string _oldSymbolBase = "";
    private string _oldSymbolQuote = "";
    private string _oldIntervalName = "";

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

    public void Initialize(string symbol, string interval)
    {
        // Split symbol into base/quote
        if (symbol.Length > 4)
        {
            SymbolSelector.SelectedBase = symbol[..^4]; // First part (e.g., "BTC")
            SymbolSelector.SelectedQuote = symbol[^4..]; // Last 4 chars (e.g., "USDT")
        }
        SymbolSelector.SelectedInterval = interval;

        // Auto-load
        RefreshCommand.Execute(null);
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

        ZoneSession.SaveSessionSettings(_session);
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
        if (_data != null)
            _ = CreateChartAndOverlaysAsync();
    }

    private void OnFibSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // FIB settings changed - refresh display
        if (_data != null)
            _ = CreateChartAndOverlaysAsync();
    }

    private void OnDisplayOptionsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Display options changed - refresh plot
        if (_data != null)
            _ = CreateChartAndOverlaysAsync();
    }

    private void OnPlaybackRequested(int direction)
    {
        // Handle playback navigation (left/right through time)
        if (_data != null)
        {
            _session.MaxDate += direction * _data.Interval.Duration;
            _ = CalculateAsync(false);
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task Refresh()
    {
        await CalculateAsync(false);
    }

    [RelayCommand]
    private async Task Calculate()
    {
        await CalculateAsync(true);
    }

    [RelayCommand]
    private void ZoomLast()
    {
        // Zoom to last candles
        if (_data != null)
        {
            _session.MaxDate = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
            _session.MaxDate = IntervalTools.StartOfIntervalCandle(_session.MaxDate, _data.Interval.Duration);
            _session.MinDate = _session.MaxDate - GlobalData.Settings.Signal.ZonesDlz.CandleCount * _data.Interval.Duration;

            RefreshPlot();
        }
    }

    [RelayCommand]
    private void OpenTradingApp()
    {
        if (_data != null)
        {
            CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;
            if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView ||
                GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
                tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;

            GlobalData.LoadLinkSettings();
            LinkTools.OpenTradingApp(_data.Symbol, _data.Interval, tradingAppInternExtern);
        }
    }

    #endregion

    #region Core Logic

    private async Task CalculateAsync(bool forceCalculation)
    {
        if (IsCalculating)
            return;

        IsCalculating = true;
        WindowTitle = $"Loading {SymbolSelector.SelectedSymbol}...";

        try
        {
            PickupUserInput();

            if (!PrepareSessionData(out string reason))
            {
                WindowTitle = $"Error: {reason}";
                return;
            }

            _session.ForceCalculation = forceCalculation;
            await CalculateZonesAndPlotZigZagAsync();

            WindowTitle = $"{_data!.Exchange.Name}.{_session.SymbolBase}{_session.SymbolQuote} {_session.IntervalName}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
            ScannerLog.Logger.Error(ex, "Calculate error");
            WindowTitle = $"Error: {ex.Message}";
        }
        finally
        {
            IsCalculating = false;
        }
    }

    private void PickupUserInput()
    {
        // Transfer UI values to session
        _session.SymbolBase = SymbolSelector.SelectedBase.ToUpper().Trim();
        _session.SymbolQuote = SymbolSelector.SelectedQuote.ToUpper().Trim();
        _session.IntervalName = SymbolSelector.SelectedInterval.ToLower().Trim();

        // Trend settings
        _session.TrendIndicator = TrendSettings.TrendType;
        _session.TrendShowZigZag = TrendSettings.ShowZigZag;

        // FIB settings
        _session.FibIndicator = FibSettings.FibTrend;
        _session.FibShowFib = FibSettings.ShowFib;
        _session.FibShowZigZag = FibSettings.ShowZigZag;

        // Display options
        _session.ShowSignals = DisplayOptions.ShowSignals;
        _session.ShowDlzZones = DisplayOptions.ShowDlzZones;
        _session.ShowFvgZones = DisplayOptions.ShowFvgZones;
        _session.ShowDtb = DisplayOptions.ShowDtb;
        _session.ShowPivots = DisplayOptions.ShowPivots;
        _session.ShowBollingerBand = DisplayOptions.ShowBollingerBand;
        _session.ShowSmaLinesSbm = DisplayOptions.ShowSmaLinesSbm;
        _session.ShowNadarayaWatsonEnvelope = DisplayOptions.ShowNadarayaWatsonEnvelope;
        _session.ShowNadarayaWatsonEnvelopeRepainting = DisplayOptions.ShowNadarayaWatsonEnvelopeRepainting;
        _session.Transparent = DisplayOptions.Transparent;
    }

    private bool PrepareSessionData(out string reason)
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
        {
            reason = "Exchange not found";
            ScannerLog.Logger.Info($"{reason}");
            return false;
        }

        if (!exchange.SymbolListName.TryGetValue(_session.SymbolBase + _session.SymbolQuote, out CryptoSymbol? symbol))
        {
            reason = "Symbol not found";
            ScannerLog.Logger.Info($"{reason}");
            return false;
        }

        var interval = GlobalData.IntervalList.Find(x => x.Name.Equals(_session.IntervalName));
        if (interval == null)
        {
            reason = "Interval not supported";
            ScannerLog.Logger.Info($"{reason}");
            return false;
        }

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        _data = new()
        {
            Exchange = exchange,
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbolInterval,
        };

        _data.IndicatorList.Add((TrendType.Primary, false), new(TrendType.Primary, false, _session.Deviation));
        _data.IndicatorList.Add((TrendType.Primary, true), new(TrendType.Primary, true, _session.Deviation));
        _data.IndicatorList.Add((TrendType.Secondary, false), new(TrendType.Secondary, false, _session.Deviation));
        _data.IndicatorList.Add((TrendType.Secondary, true), new(TrendType.Secondary, true, _session.Deviation));

        // Reset dates if symbol/interval changed
        if (_oldSymbolBase != _session.SymbolBase || _oldSymbolQuote != _session.SymbolQuote || _oldIntervalName != _session.IntervalName)
        {
            _oldSymbolBase = _session.SymbolBase;
            _oldSymbolQuote = _session.SymbolQuote;
            _oldIntervalName = _session.IntervalName;

            _session.IntervalName = _data.Interval.Name;
            _session.ActiveInterval = _data.Interval.IntervalPeriod;
            _session.MaxDate = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
            _session.MaxDate = IntervalTools.StartOfIntervalCandle(_session.MaxDate, _data.Interval.Duration);
            _session.MinDate = _session.MaxDate - GlobalData.Settings.Signal.ZonesDlz.CandleCount * _data.Interval.Duration;

            PlaybackControls.UpdateIntervalDisplay(_session.ActiveInterval.ToString());
            PlaybackControls.UpdateMaxTimeDisplay(CandleTools.GetUnixDate(_session.MaxDate).ToLocalTime().ToString("dd MMM HH:mm"));
        }

        // Load signals and positions
        ExtraData.LoadSignalsForSymbol(_data, _session.MinDate);
        ExtraData.LoadPositionsForSymbol(_data, _session.MinDate);

        reason = "";
        return true;
    }

    private async Task CalculateZonesAndPlotZigZagAsync()
    {
        if (_data == null)
            return;

        StringBuilder log = new();
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory = [];

        try
        {
            // Hide crosshair cursor
            if (_verticalLine != null && _horizontalLine != null)
            {
                _verticalLine.LineStyle = LineStyle.None;
                _horizontalLine.LineStyle = LineStyle.None;
            }

            _data.Symbol.Data.CalculatingZones = true;
            try
            {
                // Load and (re)calculate the zones
                ZoneDlz.LoadZonesForSymbol(_data.Symbol);

                // Calculate FVG if forced
                if (_session.ForceCalculation)
                    await ZoneFvg.CalculateFvgZonesAsync(ShowProgress, _data.Symbol, _data.Interval, loadedCandlesInMemory);

                // Calculate DLZ zones
                await CalculateAllDlzZonesAsync(_session, _data, loadedCandlesInMemory);

                // Create chart and draw overlays
                await CreateChartAndOverlaysAsync();
            }
            finally
            {
                await ZoneCandleEngine.SaveCandleDataToDiskAsync(_data.Symbol, loadedCandlesInMemory);
                await ZoneCandleEngine.CleanLoadedCandlesAsync(_data.Symbol);
                _data.Symbol.Data.CalculatingZones = false;
            }

            RefreshPlot();
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "CalculateZonesAndPlotZigZag error");
            GlobalData.AddTextToLogTab($"ERROR {error}");
        }
    }

    private async Task CalculateAllDlzZonesAsync(ZoneSession session, ZoneConfig data,
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        try
        {
            data.IndicatorList.Clear();
            data.IndicatorList.Add((TrendType.Primary, false), new(TrendType.Primary, false, session.Deviation));
            data.IndicatorList.Add((TrendType.Primary, true), new(TrendType.Primary, true, session.Deviation));
            data.IndicatorList.Add((TrendType.Secondary, false), new(TrendType.Secondary, false, session.Deviation));
            data.IndicatorList.Add((TrendType.Secondary, true), new(TrendType.Secondary, true, session.Deviation));

            await ZoneDlz.CalculateDlzBoxesAsync(ShowProgress, session, data, loadedCandlesInMemory);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "CalculateAllDlzZones error");
            GlobalData.AddTextToLogTab($"ERROR {error}");
        }
    }

    private async Task CreateChartAndOverlaysAsync()
    {
        if (_data == null)
            return;

        // Get main trend indicator
        SettingsZigZag mainTrend = _session.TrendIndicator == 0 ?
            GlobalData.Settings.Trend.Primary : GlobalData.Settings.Trend.Secondary;
        var mainIndicator = _data.IndicatorList[(mainTrend.TrendType, mainTrend.UseHighLow)];
        CryptoTrendIndicator trendIndicator = TrendInterval.InterpretZigZagPoints(mainIndicator, null);

        // Create the chart with crosshairs
        PlotModel = Visualisation.Chart.Chart.Create(_data.Symbol, _data.Interval, out _horizontalLine, out _verticalLine);
        PlotModel.Title = $"{_session.SymbolBase}{_session.SymbolQuote} {_data.Interval.Name} UTC " +
            $"{trendIndicator} candles={mainIndicator.CandleCount} points={mainIndicator.ZigZagList.Count}";

        // Draw candles
        await ZoneCandleEngine.LoadCandleDataFromDiskAsync(_data.Symbol, _data.Interval);
        Candles.Draw(PlotModel, _data.Symbol, _data.Interval, _session.MinDate, _session.MaxDate);

        // Draw trend zigzag
        if (_session.TrendShowZigZag)
            ZigZag.Draw(PlotModel, mainIndicator.ZigZagList, "maintrend", OxyColors.White, _session.MinDate, _session.MaxDate);

        // Draw pivots
        if (_session.ShowPivots)
            Points.Draw(PlotModel, mainIndicator.PivotList, _session.MinDate, _session.MaxDate);

        // Draw double top/bottom
        if (_session.ShowDtb)
            Dtb.Draw(PlotModel, _data.Interval, mainIndicator);

        // Draw FIB retracement
        if (_session.FibShowFib)
            FibRetracement.Draw(PlotModel, _data.Symbol, _data.Interval, _data.IndicatorList[(_session.FibIndicator, true)]);

        // Draw FIB zigzag
        if (_session.FibShowZigZag)
            ZigZag.Draw(PlotModel, _data.IndicatorList[(_session.FibIndicator, true)].ZigZagList,
                "fib", OxyColors.White, _session.MinDate, _session.MaxDate);

        // Draw DLZ zones
        if (_session.ShowDlzZones)
            DlzZones.Draw(PlotModel, _data.Symbol, _session.MinDate, _session.MaxDate);

        // Draw FVG zones
        if (_session.ShowFvgZones)
            FvgZones.Draw(PlotModel, _data.Symbol, _session.MinDate, _session.MaxDate);

        // Draw signals
        if (_session.ShowSignals)
            Signals.Draw(PlotModel, _data.Signals, _session.MinDate, _session.MaxDate);

        // Draw Nadaraya Watson Envelope
        if (_session.ShowNadarayaWatsonEnvelope)
            NadarayaWatsonEnvelope.Draw(PlotModel, _data.Symbol, _data.Interval, _session.MinDate, _session.MaxDate,
                _session.ShowNadarayaWatsonEnvelopeRepainting);

        // Draw Bollinger Bands
        if (_session.ShowBollingerBand)
            Bollingerbands.Draw(PlotModel, _data.Symbol, _data.Interval, _session.MinDate, _session.MaxDate);

        // Draw SMA lines
        if (_session.ShowSmaLinesSbm)
        {
            Sma.Draw(PlotModel, _data.Symbol, _data.Interval, 200, OxyColors.Red, _session.MinDate, _session.MaxDate);
            Sma.Draw(PlotModel, _data.Symbol, _data.Interval, 50, OxyColors.Orange, _session.MinDate, _session.MaxDate);
            Sma.Draw(PlotModel, _data.Symbol, _data.Interval, 20, OxyColors.Green, _session.MinDate, _session.MaxDate);
        }

        RefreshPlot();
    }

    private void RefreshPlot()
    {
        PlotModel.InvalidatePlot(true);
    }

    private void ShowProgress(string text)
    {
        WindowTitle = text;
    }

    #endregion

    public void OnClosing()
    {
        SaveSession();
    }
}
