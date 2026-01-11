using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Annotations;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;
using CryptoScanner.Core.Trend;
using CryptoScanner.Core.Settings;
using CryptoScanner.Visualisation.Chart;
using CryptoScanner.ZoneVisualisation;
using CryptoScanner.Helpers;

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

    // Crosshair annotations (public for View access)
    private LineAnnotation? VerticalLine;
    private LineAnnotation? HorizontalLine;

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
        _plotModel = new PlotModel { Title = "Chart 1.2.3." };



#pragma warning disable CS0618 // Type or member is obsolete
        PlotModel.MouseMove += _plotModel_MouseMove; // Declared obsolete, but since there is no suggestion how to solve it (ridiculous)
        PlotModel.MouseDown += PlotModel_MouseDown; // Declared obsolete, but since there is no suggestion how to solve it (ridiculous)
#pragma warning restore CS0618 // Type or member is obsolete

        InitializePlot();

        // Subscribe to changes from sub-ViewModels
        SymbolSelector.PropertyChanged += OnSymbolChanged;
        TrendSettings.PropertyChanged += OnTrendSettingsChanged;
        FibSettings.PropertyChanged += OnFibSettingsChanged;
        DisplayOptions.PropertyChanged += OnDisplayOptionsChanged;
        PlaybackControls.PlaybackRequested += OnPlaybackRequested;

        // Load session
        LoadSession();

        System.Diagnostics.Debug.WriteLine($"VisualisationViewModel default constructor called");
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

    public void Initialize()
    {
        //// Split symbol into base/quote
        //if (symbol.Length > 4)
        //{
        //    SymbolSelector.SelectedBase = symbol[..^4]; // First part (e.g., "BTC")
        //    SymbolSelector.SelectedQuote = symbol[^4..]; // Last 4 chars (e.g., "USDT")
        //}
        //SymbolSelector.SelectedInterval = interval;

        // Auto-load
        RefreshCommand.ExecuteAsync(null);
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

        // ✓ FIXED: No parameters needed
        _session.SaveSessionSettings();
    }

    #region Event Handlers

    private void OnSymbolChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SymbolSelectorViewModel.SelectedSymbol) ||
            e.PropertyName == nameof(SymbolSelectorViewModel.SelectedInterval))
        {
            // Symbol or interval changed - reload chart
            RefreshCommand.ExecuteAsync(null);
        }
    }

    private void OnTrendSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Trend settings changed - refresh display
        _ = CreateChartAndOverlaysAsync();
    }

    private void OnFibSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // FIB settings changed - refresh display
        _ = CreateChartAndOverlaysAsync();
    }

    private void OnDisplayOptionsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Display options changed - refresh plot
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
            if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
                tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
            CommandHelper.ActivateTradingApp(GlobalData.Settings.General.TradingApp, _data.Symbol, _data.Interval, tradingAppInternExtern);
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

        // ✓ FIXED: Use local properties instead of non-existent session properties
        // FIB settings stored locally in FibSettings, not in session
        // _session.FibIndicator = FibSettings.FibTrend;  // REMOVED - doesn't exist in ZoneSession
        // _session.FibShowFib = FibSettings.ShowFib;      // REMOVED - doesn't exist in ZoneSession
        _session.FibShowZigZag = FibSettings.ShowZigZag;

        // Display options
        _session.ShowSignals = DisplayOptions.ShowSignals;
        _session.ShowDlzZones = DisplayOptions.ShowDlzZones;
        // ✓ FIXED: ShowFvgZones stored locally in DisplayOptions
        // _session.ShowFvgZones = DisplayOptions.ShowFvgZones;  // REMOVED - use local property
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
            if (VerticalLine != null && HorizontalLine != null)
            {
                VerticalLine.LineStyle = LineStyle.None;
                HorizontalLine.LineStyle = LineStyle.None;
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
        SettingsZigZag mainTrend = _session.TrendIndicator == 0 ? GlobalData.Settings.Trend.Primary : GlobalData.Settings.Trend.Secondary;
        var mainIndicator = _data.IndicatorList[(mainTrend.TrendType, mainTrend.UseHighLow)];
        CryptoTrendIndicator trendIndicator = TrendInterval.InterpretZigZagPoints(mainIndicator, null);

        // Create the chart with crosshairs
        var newPlotModel = Chart.Chart.Create(_data.Symbol, _data.Interval, out HorizontalLine, out VerticalLine);
        newPlotModel.Title = $"{_session.SymbolBase}{_session.SymbolQuote} {_data.Interval.Name} UTC " +
            $"{trendIndicator} candles={mainIndicator.CandleCount} points={mainIndicator.ZigZagList.Count}";

        // Draw candles
        await ZoneCandleEngine.LoadCandleDataFromDiskAsync(_data.Symbol, _data.Interval);
        Candles.Draw(newPlotModel, _data.Symbol, _data.Interval, _session.MinDate, _session.MaxDate);

        // Draw trend zigzag
        if (_session.TrendShowZigZag)
            ZigZag.Draw(newPlotModel, mainIndicator.ZigZagList, "maintrend", OxyColors.White, _session.MinDate, _session.MaxDate);

        // Draw pivots
        if (_session.ShowPivots)
            Points.Draw(newPlotModel, mainIndicator.PivotList, _session.MinDate, _session.MaxDate);

        // Draw double top/bottom
        if (_session.ShowDtb)
            Dtb.Draw(newPlotModel, _data.Interval, mainIndicator);

        // ✓ FIXED: Use local FibSettings properties
        // Draw FIB retracement
        if (FibSettings.ShowFib)
            FibRetracement.Draw(newPlotModel, _data.Symbol, _data.Interval,
                _data.IndicatorList[(FibSettings.FibTrend == 0 ? TrendType.Primary : TrendType.Secondary, true)]);

        // Draw FIB zigzag
        if (FibSettings.ShowZigZag)
            ZigZag.Draw(newPlotModel,
                _data.IndicatorList[(FibSettings.FibTrend == 0 ? TrendType.Primary : TrendType.Secondary, true)].ZigZagList,
                "fib", OxyColors.White, _session.MinDate, _session.MaxDate);

        // Draw DLZ zones
        if (_session.ShowDlzZones)
            DlzZones.Draw(newPlotModel, _data.Symbol, _session.MinDate, _session.MaxDate);

        // ✓ FIXED: Use local DisplayOptions property
        // Draw FVG zones
        if (DisplayOptions.ShowFvgZones)
            Chart.FvgZones.Draw(newPlotModel, _data.Symbol, _session.MinDate, _session.MaxDate);

        // Draw signals
        if (_session.ShowSignals)
            Signals.Draw(newPlotModel, _data.Signals, _session.MinDate, _session.MaxDate);

        // Draw Nadaraya Watson Envelope
        if (_session.ShowNadarayaWatsonEnvelope)
            NadarayaWatsonEnvelope.Draw(newPlotModel, _data.Symbol, _data.Interval, _session.MinDate, _session.MaxDate,
                _session.ShowNadarayaWatsonEnvelopeRepainting);

        // Draw Bollinger Bands
        if (_session.ShowBollingerBand)
            Bollingerbands.Draw(newPlotModel, _data.Symbol, _data.Interval, _session.MinDate, _session.MaxDate);

        // Draw SMA lines
        if (_session.ShowSmaLinesSbm)
        {
            Sma.Draw(newPlotModel, _data.Symbol, _data.Interval, 200, OxyColors.Red, _session.MinDate, _session.MaxDate);
            Sma.Draw(newPlotModel, _data.Symbol, _data.Interval, 50, OxyColors.Orange, _session.MinDate, _session.MaxDate);
            Sma.Draw(newPlotModel, _data.Symbol, _data.Interval, 20, OxyColors.Green, _session.MinDate, _session.MaxDate);
        }

        //RefreshPlot();
        PlotModel = newPlotModel;

        //await RenderChartToImage();
    }

    //public async Task RenderChartToImage()
    //{
    //    var pngExporter = new OxyPlot.PngExporter { Width = 1200, Height = 800 };
    //    using var stream = new MemoryStream();
    //    pngExporter.Export(PlotModel, stream);
    //    stream.Object = 0;

    //    // Convert to Avalonia Bitmap
    //    var bitmap = new Avalonia.Media.Imaging.Bitmap(stream);
    //    OnPropertyChanged(nameof(ChartBitmap)); // Bind to Image
    //}

    private void RefreshPlot()
    {
        PlotModel.InvalidatePlot(true);
    }

    private void ShowProgress(string text)
    {
        WindowTitle = text;
    }


    private bool IsMeasuring = false;
    private double? mouseDownPointX = null;
    private double? mouseDownPointY = null;
    private RectangleAnnotation? lastRectangle = null;

    private void PlotModel_MouseDown(object? sender, OxyMouseDownEventArgs e)
    {
        // start a measurement
        if (!IsMeasuring && e.ChangedButton == OxyMouseButton.Left && e.IsShiftDown)
        {
            mouseDownPointX = e.Position.X;
            mouseDownPointY = e.Position.Y;
            IsMeasuring = true;
            e.Handled = true;
            return;
        }

        // stop the measurement, the rectangle stay's until next mouse click
        if (IsMeasuring && e.ChangedButton == OxyMouseButton.Left && !e.IsShiftDown)
        {
            IsMeasuring = false;
            e.Handled = true;
            return;
        }

        // remove the measurement rectangle if it existed
        if (!IsMeasuring && e.ChangedButton == OxyMouseButton.Left && lastRectangle != null)
        {
            if (lastRectangle != null)
            {
                PlotModel?.Annotations.Remove(lastRectangle);
                lastRectangle = null;
                mouseDownPointX = null;
                e.Handled = true;
            }
            return;
        }
    }

    /// <summary>
    /// Update the crosshair and show some information
    /// </summary>
    private void _plotModel_MouseMove(object? sender, OxyMouseEventArgs e)
    {
        // Helper method for View to update crosshair and subtitle
        if (Data != null && HorizontalLine != null && VerticalLine != null) // exception on drawing annotations? whats wrong?
        {
            var model = PlotModel;
            var screenPoint = new ScreenPoint(e.Position.X, e.Position.Y);
            double x = model.Axes[0].InverseTransform(screenPoint.X);
            double y = model.Axes[1].InverseTransform(screenPoint.Y);

            var symbolInterval = Data.Symbol.GetSymbolInterval(_session.ActiveInterval);
            long unix = (long)x + symbolInterval.Interval.Duration / 2;
            unix = IntervalTools.StartOfIntervalCandle(unix, symbolInterval.Interval.Duration);
            if (unix < 0)
                return;

            try
            {
                // Update crosshair coordinates
                VerticalLine.X = unix;
                HorizontalLine.Y = y;
                VerticalLine.LineStyle = LineStyle.DashDot;
                HorizontalLine.LineStyle = LineStyle.DashDot;

                string subtitle;
                if (symbolInterval.CandleList.TryGetValue(unix, out CryptoCandle? candle))
                {
                    subtitle = $"{candle.Date.ToLocalTime():ddd yyyy-MM-dd HH:mm}, price: {y.ToString(_data.Symbol.PriceDisplayFormat)}";
                    subtitle += $" (O: {candle.Open.ToString(_data.Symbol.PriceDisplayFormat)}";
                    subtitle += $" H: {candle.High.ToString(_data.Symbol.PriceDisplayFormat)}";
                    subtitle += $" L: {candle.Low.ToString(_data.Symbol.PriceDisplayFormat)}";
                    subtitle += $" C: {candle.Close.ToString(_data.Symbol.PriceDisplayFormat)}";
                    subtitle += $" V: {candle.Volume.ToString0()})";
                }
                else
                {
                    DateTime date = CandleTools.GetUnixDate(unix);
                    subtitle = $"{date.ToLocalTime():yyyy-MM-dd HH:mm}, price: {y.ToString(_data.Symbol.PriceDisplayFormat)}";
                }

                PlotModel.Subtitle = subtitle;
                PlaybackControls.UpdateIntervalDisplay(_session.ActiveInterval.ToString());
                PlotModel.InvalidatePlot(true);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Info("UpdateCrosshair.Error " + error.ToString());
            }
        }
    }
    #endregion

    public void OnClosing()
    {
        SaveSession();
    }

    // Expose data for View access
    public ZoneConfig? Data => _data;
    public ZoneSession Session => _session;
}
