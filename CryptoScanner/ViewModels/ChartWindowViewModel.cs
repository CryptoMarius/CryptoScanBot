using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trend;
using CryptoScanner.Core.Zones;
using CryptoScanner.Helpers;
using CryptoScanner.ViewModels.Chart;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CryptoScanner.ViewModels;

public partial class ChartWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private OxyPlot.Avalonia.PlotView _plotView;

    [ObservableProperty]
    private PlotModel _plotModel;

    // Crosshair annotations
    private LineAnnotation? CrossHairX;
    private LineAnnotation? CrossHairY;

    // The data, symbol, interval etc..
    private ZoneConfig? Data { get; set; } = null;
    // The options and attributes for the PlotModel
    private ZoneSession Session { get; set; } = new();


    // Sub-ViewModels for modular UI
    [ObservableProperty]
    private ChartSymbolSelectorViewModel _symbolSelector;

    [ObservableProperty]
    private ChartTrendSettingsViewModel _trendSettings;

    [ObservableProperty]
    private ChartFibSettingsViewModel _fibSettings;

    [ObservableProperty]
    private ChartOptionsViewModel _displayOptions;

    [ObservableProperty]
    private ChartPlaybackControlsViewModel _playbackControls;



    [ObservableProperty]
    private string _windowTitle = "Crypto Visualisation";

    [ObservableProperty]
    private bool _isCalculating = false;

    private string _oldSymbolBase = "";
    private string _oldSymbolQuote = "";
    private string _oldIntervalName = "";

    public ChartWindowViewModel()
    {
        // Initialize sub-ViewModels
        _symbolSelector = new ChartSymbolSelectorViewModel();
        _trendSettings = new ChartTrendSettingsViewModel();
        _fibSettings = new ChartFibSettingsViewModel();
        _displayOptions = new ChartOptionsViewModel();
        _playbackControls = new ChartPlaybackControlsViewModel();

        _plotModel = CreatePlotModel();

        // Create PlotView in ViewModel
        _plotView = new OxyPlot.Avalonia.PlotView
        {
            Model = PlotModel,
            //Background = OxyColors.Transparent,
            Controller = CreateController()
        };

        // Load session
        Session = LoadSessionSettings();
        Session.UseOptimizing = false;

        // Load settings into sub-ViewModels
        SymbolSelector.LoadFromSession(Session);
        TrendSettings.LoadFromSession(Session);
        FibSettings.LoadFromSession(Session);
        DisplayOptions.LoadFromSession(Session);

        // Subscribe to changes from sub-ViewModels
        SymbolSelector.PropertyChanged += OnSymbolChanged;
        TrendSettings.PropertyChanged += OnTrendSettingsChanged;
        FibSettings.PropertyChanged += OnFibSettingsChanged;
        DisplayOptions.PropertyChanged += OnDisplayOptionsChanged;
        PlaybackControls.PlaybackRequested += OnPlaybackRequested;

        // Force display
        //OnPropertyChanged(nameof(SymbolSelector.SelectedSymbol));
        RefreshCommand.ExecuteAsync(null);
        System.Diagnostics.Debug.WriteLine($"VisualisationViewModel default constructor called");
    }

    private static PlotController CreateController()
    {
        var controller = new PlotController();
        //controller.UnbindAll();
        //controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
        //controller.UnbindAll(); // leave the original intact, we just need to tweak it a bit
        controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
        controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control, PlotCommands.ZoomRectangle);
        controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control | OxyModifierKeys.Alt, 2, PlotCommands.ResetAt);
        controller.UnbindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Shift);
        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control | OxyModifierKeys.Alt, PlotCommands.ZoomRectangle);
        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control, 2, PlotCommands.ResetAt);
        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Alt, PlotCommands.PanAt);
        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Shift, PlotCommands.SnapTrack);
        return controller;
    }

    private string LabelFormatterX(double x)
    {
        string s;
        CandleTime unix = new CandleTime((uint)x);
        DateTime date = unix.ToDateTime(); //.ToLocalTime(); problem..?
        if (Data?.Interval?.IntervalPeriod <= CryptoIntervalPeriod.interval1h && date.Hour == 0)
            s = date.Day.ToString();
        else if (Data?.Interval?.IntervalPeriod <= CryptoIntervalPeriod.interval1d)
            s = date.Day.ToString();
        else
            s = "?";

        if (date.Day == 1)
        {
            string monthName = date.ToString("MMM", CultureInfo.InvariantCulture);
            s += "\r\n" + monthName;
        }

        return s;
    }

    private string LabelFormatterY(double x)
    {
        string s = x.ToString(Data?.Symbol?.PriceDisplayFormat);
        return s;
    }


    public PlotModel CreatePlotModel()
    {
        // Create the PlotModel (model) once..

        PlotModel chart = new()
        {
            Background = OxyColors.Black,

            Title = "Chart 1.2.3.",
            Subtitle = "...",
            TitleFont = Const.OxyFontName,
            TitleColor = OxyColors.White,

            TextColor = OxyColors.White,
            SubtitleFont = Const.OxyFontName,
            SubtitleColor = OxyColors.White,
            SubtitleFontWeight = FontWeights.Bold,
        };

        chart.Axes.Clear();

        // x-axis
        chart.Axes.Add(new LinearAxis
        {
            Title = "Time",
            StringFormat = "dd-MM HH:mm",
            Font = Const.OxyFontName,
            FontSize = Const.OxyFontSize,
            TextColor = OxyColors.White,
            LabelFormatter = LabelFormatterX,
            Position = AxisPosition.Bottom,

            MajorTickSize = 15,
            MinorTickSize = 5,
            TicklineColor = OxyColors.Gray,
            TickStyle = TickStyle.Inside,

            AxislineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.Gray,
            AxislineThickness = 2,

            //MajorGridlineStyle = LineStyle.Solid,
            //MinorGridlineStyle = LineStyle.Dot

            //MinorGridlineStyle = LineStyle.None,
            //MinorGridlineColor = OxyColors.Gray,

            //MajorStep = (24 * 60 * 60 / _interval?.Duration) * _interval?.Duration,
            //MinorStep = (24 * 60 * 60 / _interval?.Duration) * _interval?.Duration / 6,
        });


        // Y-axis (Price)
        chart.Axes.Add(new LinearAxis
        {
            Title = "Price",
            LabelFormatter = LabelFormatterY,
            Font = Const.OxyFontName,
            FontSize = Const.OxyFontSize,
            //Font = PlotModel.TitleFont,
            TextColor = OxyColors.White,
            Position = AxisPosition.Right,

            MajorTickSize = 15,
            MinorTickSize = 5,
            TicklineColor = OxyColors.Gray,
            TickStyle = TickStyle.Inside,

            AxislineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.Gray,
            AxislineThickness = 2,

            //MajorGridlineStyle = LineStyle.Solid,
            //MinorGridlineStyle = LineStyle.Dot
        });

        CrossHairX = new LineAnnotation
        {
            Type = LineAnnotationType.Vertical,
            Color = OxyColors.White,
            LineStyle = LineStyle.Dash,
            StrokeThickness = 0.5,
            Tag = "crosshair",
        };
        chart.Annotations.Add(CrossHairX);

        CrossHairY = new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Color = OxyColors.White,
            LineStyle = LineStyle.Dash,
            StrokeThickness = 0.5,
            Tag = "crosshair",
        };
        chart.Annotations.Add(CrossHairY);

#pragma warning disable CS0618 // Type or member is obsolete
        chart.MouseMove += PlotModel_MouseMove; // Declared obsolete, but since there is no suggestion how to solve it (ridiculous)
        chart.MouseDown += PlotModel_MouseDown; // Declared obsolete, but since there is no suggestion how to solve it (ridiculous)
#pragma warning restore CS0618 // Type or member is obsolete

        return chart;
    }

    public static ZoneSession LoadSessionSettings()
    {
        // load previous Session settings
        string fileName = Path.Combine(GlobalData.AppDataFolder, $"CryptoScanBot-chart.json");
        if (File.Exists(fileName))
        {
            string text = File.ReadAllText(fileName);
            var session = JsonSerializer.Deserialize<ZoneSession>(text, JsonTools.DeSerializerOptions);
            if (session != null)
                return session;
        }

        return new();
    }

    public void SaveSessionSettings()
    {
        PickupUserInput();

        // save current Session settings
        Directory.CreateDirectory(GlobalData.AppDataFolder);
        string fileName = Path.Combine(GlobalData.AppDataFolder, $"CryptoScanBot-chart.json");
        string text = JsonSerializer.Serialize(Session, JsonTools.JsonSerializerIndented);
        File.WriteAllText(fileName, text);
    }

    private void PickupUserInput()
    {
        SymbolSelector.SaveToSession(Session);
        TrendSettings.SaveToSession(Session);
        FibSettings.SaveToSession(Session);
        DisplayOptions.SaveToSession(Session);
    }

    private void OnSymbolChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChartSymbolSelectorViewModel.SelectedSymbol) ||
            e.PropertyName == nameof(ChartSymbolSelectorViewModel.SelectedInterval))
        {
            // Symbol or interval changed - reload PlotModel
            RefreshCommand.ExecuteAsync(null);
        }
    }


    private bool _refreshChart = false;
    private readonly Dictionary<string, string> optionsInChart = [];
    private bool Toggle(PlotModel chart, string group, bool currentValue, string prefix = "")
    {
        optionsInChart.TryAdd(group, "");
        string stored = optionsInChart[group];
        string current = currentValue ? prefix + "1" : prefix + "0";

        if (stored != current)
        {
            _refreshChart = true;
            optionsInChart[group] = current;

            if (currentValue)
            {
                return true; // draw indicator
            }
            else
            {
                RemoveFromChart(chart, group);
                return false; // already done..
            }
        }
        return false;
    }


    private void OnTrendSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Trend settings changed - refresh display
        //_ = CreateChartAndOverlaysAsync();

        // Display options changed
        if (Data == null)
            return;

        PickupUserInput();

        // Draw trend zigzag
        string group = "candles.trendlines";
        if (Toggle(PlotModel, group, Session.TrendShowZigZag, Session.TrendType.ToString()))
        {
            RemoveFromChart(PlotModel, group);
            SettingsZigZag mainTrend = Session.TrendType == TrendType.Primary ? GlobalData.Settings.Trend.Primary : GlobalData.Settings.Trend.Secondary;
            var mainIndicator = Data.IndicatorList[(mainTrend.TrendType, mainTrend.UseHighLow)];
            ZigZag.Draw(PlotModel, mainIndicator.ZigZagList, "maintrend",
                OxyColors.White, Session.MinDate, Session.MaxDate, group);
        }

        if (_refreshChart && sender != null)
        {
            this.PlotModel.InvalidatePlot(true);
            OnPropertyChanged(nameof(ChartWindowViewModel.PlotModel));
            OnPropertyChanged(nameof(PlotView));
            _refreshChart = false;
        }
    }

    private void OnFibSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // FIB settings changed - refresh display
        //_ = CreateChartAndOverlaysAsync();

        // Display options changed
        if (Data == null)
            return;

        PickupUserInput();

        // Draw FIB retracement
        string group = "fib.indicator";
        if (Toggle(PlotModel, group, FibSettings.ShowFibRetracement))
            FibRetracement.Draw(PlotModel, Data.Symbol, Data.Interval,
                Data.IndicatorList[(FibSettings.FibTrend == 0 ? TrendType.Primary : TrendType.Secondary, true)], group);

        // Draw FIB zigzag
        group = "fib.trendlines";
        if (Toggle(PlotModel, group, FibSettings.ShowZigZag, FibSettings.FibTrend.ToString()))
        {
            RemoveFromChart(PlotModel, group);
            ZigZag.Draw(PlotModel,
                Data.IndicatorList[(FibSettings.FibTrend == 0 ? TrendType.Primary : TrendType.Secondary, true)].ZigZagList,
                "fib", OxyColors.White, Session.MinDate, Session.MaxDate, group);
        }

        if (_refreshChart && sender != null)
        {
            PlotModel.InvalidatePlot(true);
            OnPropertyChanged(nameof(PlotModel));
            OnPropertyChanged(nameof(PlotView));
            _refreshChart = false;
        }
    }


    private void OnDisplayOptionsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Display options changed
        if (Data == null)
            return;

        PickupUserInput();
        SettingsZigZag mainTrend = Session.TrendType == TrendType.Primary ? GlobalData.Settings.Trend.Primary : GlobalData.Settings.Trend.Secondary;
        var mainIndicator = Data.IndicatorList[(mainTrend.TrendType, mainTrend.UseHighLow)];

        // Draw pivots
        string group = "pivotpoints";
        if (Toggle(PlotModel, group, Session.ShowPoints))
            Points.Draw(PlotModel, mainIndicator.PivotList, Session.MinDate, Session.MaxDate, group);

        // Draw double top/bottom
        group = "dtb";
        if (Toggle(PlotModel, group, Session.ShowDtb)) // Test double top/bottom
            Dtb.Draw(PlotModel, Data.Interval, mainIndicator, group);

        // Draw DLZ zones
        group = "dlz.zones";
        if (Toggle(PlotModel, group, Session.ShowDlzZones))
            DlzZones.Draw(PlotModel, Data.Symbol, Session.MinDate, Session.MaxDate, group);

        // Draw FVG zones
        group = "fvg.zones";
        if (Toggle(PlotModel, group, DisplayOptions.ShowFvgZones))
            Chart.FvgZones.Draw(PlotModel, Data.Symbol, Session.MinDate, Session.MaxDate, group);

        // Draw signals
        group = "signals";
        if (Toggle(PlotModel, group, Session.ShowSignals))
            Signals.Draw(PlotModel, Data.Signals, Session.MinDate, Session.MaxDate, group);

        // Draw Nadaraya Watson Envelope (non repainting)
        group = "nwe.notrepainting";
        if (Toggle(PlotModel, group, Session.ShowNadarayaWatsonEnvelope))
            NadarayaWatsonEnvelope.Draw(PlotModel, Data.Symbol, Data.Interval, Session.MinDate, Session.MaxDate, false, group);

        // Draw Nadaraya Watson Envelope (repainting)
        group = "nwe.repainting";
        if (Toggle(PlotModel, group, Session.ShowNadarayaWatsonEnvelopeRepainting))
            NadarayaWatsonEnvelope.Draw(PlotModel, Data.Symbol, Data.Interval, Session.MinDate, Session.MaxDate, true, group);

        // Draw Bollinger Bands
        group = "bb";
        if (Toggle(PlotModel, group, Session.ShowBollingerBand))
            Bollingerbands.Draw(PlotModel, Data.Symbol, Data.Interval, Session.MinDate, Session.MaxDate, group);

        // Draw PSar
        group = "psar";
        if (Toggle(PlotModel, group, Session.ShowPSar))
            PSar.Draw(PlotModel, Data.Symbol, Data.Interval, Session.MinDate, Session.MaxDate, group);

        // Draw SMA lines
        group = "sma";
        if (Toggle(PlotModel, group, Session.ShowSmaLinesSbm))
        {
            Sma.Draw(PlotModel, Data.Symbol, Data.Interval, 200, OxyColors.Red, Session.MinDate, Session.MaxDate, group);
            Sma.Draw(PlotModel, Data.Symbol, Data.Interval, 50, OxyColors.Orange, Session.MinDate, Session.MaxDate, group);
            Sma.Draw(PlotModel, Data.Symbol, Data.Interval, 20, OxyColors.Green, Session.MinDate, Session.MaxDate, group);
        }

        if (_refreshChart && sender != null)
        {
            this.PlotModel.InvalidatePlot(true);
            OnPropertyChanged(nameof(ChartWindowViewModel.PlotModel));
            OnPropertyChanged(nameof(PlotView));
            _refreshChart = false;
        }
    }

    [RelayCommand]
    private async Task Calculate()
    {
        await SymbolOrIntervalChanged(true);
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await SymbolOrIntervalChanged(false);
    }

    private void OnPlaybackRequested(int direction)
    {
        // Handle playback navigation (left/right through time)
        if (Data != null)
        {
            Session.MaxDate += direction * Data.Interval.Duration;
            _ = SymbolOrIntervalChanged(false);
        }
    }

    [RelayCommand]
    private void ZoomLast()
    {
        // Zoom to last candles
        if (Data != null)
        {
            Session.MaxDate = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1);
            Session.MaxDate = IntervalTools.StartOfIntervalCandle(Session.MaxDate, Data.Interval.Duration);
            Session.MinDate = Session.MaxDate - GlobalData.Settings.Signal.ZonesDlz.CandleCount * Data.Interval.Duration;

            // ?

            PlotModel.InvalidatePlot(true);
            OnPropertyChanged(nameof(PlotModel));
            OnPropertyChanged(nameof(PlotView));
        }
    }

    [RelayCommand]
    private void OpenTradingApp()
    {
        if (Data != null)
        {
            CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;
            if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
                tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
            CommandHelper.ActivateTradingApp(GlobalData.Settings.General.TradingApp, Data.Symbol, Data.Interval, tradingAppInternExtern);
        }
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

        if (!exchange.SymbolListName.TryGetValue(Session.SymbolBase + Session.SymbolQuote, out CryptoSymbol? symbol))
        {
            reason = "Symbol not found";
            ScannerLog.Logger.Info($"{reason}");
            return false;
        }

        var interval = GlobalData.IntervalList.Find(x => x.Name.Equals(Session.IntervalName));
        if (interval == null)
        {
            reason = "Interval not supported";
            ScannerLog.Logger.Info($"{reason}");
            return false;
        }

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        Data = new()
        {
            Exchange = exchange,
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbolInterval,
        };

        Data.IndicatorList.Add((TrendType.Primary, false), new(TrendType.Primary, false, Session.Deviation));
        Data.IndicatorList.Add((TrendType.Primary, true), new(TrendType.Primary, true, Session.Deviation));
        Data.IndicatorList.Add((TrendType.Secondary, false), new(TrendType.Secondary, false, Session.Deviation));
        Data.IndicatorList.Add((TrendType.Secondary, true), new(TrendType.Secondary, true, Session.Deviation));

        // Reset dates if symbol/interval changed
        if (_oldSymbolBase != Session.SymbolBase || _oldSymbolQuote != Session.SymbolQuote || _oldIntervalName != Session.IntervalName)
        {
            optionsInChart.Clear();
            _oldSymbolBase = Session.SymbolBase;
            _oldSymbolQuote = Session.SymbolQuote;
            _oldIntervalName = Session.IntervalName;

            Session.IntervalName = Data.Interval.Name;
            Session.ActiveInterval = Data.Interval.IntervalPeriod;
            Session.MaxDate = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1);
            Session.MaxDate = IntervalTools.StartOfIntervalCandle(Session.MaxDate, Data.Interval.Duration);
            Session.MinDate = Session.MaxDate - GlobalData.Settings.Signal.ZonesDlz.CandleCount * Data.Interval.Duration;

            PlaybackControls.UpdateIntervalDisplay(Session.ActiveInterval.ToString());
            PlaybackControls.UpdateMaxTimeDisplay(Session.MaxDate.ToDateTime().ToLocalTime().ToString("dd MMM HH:mm"));

            // Load signals
            ExtraData.LoadSignalsForSymbol(Data, Session.MinDate);
        }

        reason = "";
        return true;
    }

    private async Task CalculateZonesAndPlotZigZagAsync()
    {
        if (Data == null)
            return;

        StringBuilder log = new();
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory = [];

        try
        {
            // Hide crosshair cursor
            if (CrossHairX != null && CrossHairY != null)
            {
                CrossHairX.LineStyle = LineStyle.None;
                CrossHairY.LineStyle = LineStyle.None;
            }

            Data.Symbol.Data.CalculatingZones = true;
            try
            {
                // Load and (re)calculate the zones
                ZoneDlz.LoadZonesForSymbol(Data.Symbol);

                // Calculate FVG if forced
                if (Session.ForceCalculation)
                    await ZoneFvg.CalculateFvgZonesAsync(ShowProgress, Data.Symbol, Data.Interval, loadedCandlesInMemory);

                // Calculate DLZ zones
                await CalculateAllDlzZonesAsync(Session, Data, loadedCandlesInMemory);

                // Create PlotModel and draw overlays
                await CreateChartAndOverlaysAsync();
            }
            finally
            {
                await ZoneCandleEngine.SaveCandleDataToDiskAsync(Data.Symbol, loadedCandlesInMemory);
                await ZoneCandleEngine.CleanLoadedCandlesAsync(Data.Symbol);
                Data.Symbol.Data.CalculatingZones = false;
            }

            PlotModel.InvalidatePlot(true);
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

    private static void RemoveFromChart(PlotModel chart, string tag)
    {
        var series = chart.Series.Where(s => s.Tag?.ToString() == tag).ToList();
        foreach (var serie in series)
        {
            chart.Series.Remove(serie);
        }

        var annotations = chart.Annotations.Where(s => s.Tag?.ToString() == tag).ToList();
        foreach (var annotation in annotations)
        {
            if (annotation.Tag?.ToString() != "crosshair")
                chart.Annotations.Remove(annotation);
        }
    }

    private string lastDisplay = string.Empty;
    private async Task CreateChartAndOverlaysAsync()
    {
        if (Data == null)
            return;

        PickupUserInput();


        var chart = PlotModel;
        string newDisplay = $"{Data.Symbol.Name}, {Data.Interval.Name}";
        if (lastDisplay != newDisplay)
        {
            lastDisplay = newDisplay;
            await ZoneCandleEngine.ReadCandlesFromDiskAsync(Data.Symbol, Data.Interval);

            // Clear all series
            chart.Series.Clear();

            // And all annotations except the crosshairs
            foreach (var annotation in chart.Annotations.ToList())
            {
                if (annotation.Tag?.ToString() != "crosshair")
                    chart.Annotations.Remove(annotation);
            }

            // Get main trend indicator
            SettingsZigZag mainTrend = Session.TrendType == TrendType.Primary ? GlobalData.Settings.Trend.Primary : GlobalData.Settings.Trend.Secondary;
            var mainIndicator = Data.IndicatorList[(mainTrend.TrendType, mainTrend.UseHighLow)];
            CryptoTrendIndicator trendIndicator = TrendInterval.InterpretZigZagPoints(mainIndicator, null);
            chart.Title = $"{Session.SymbolBase}{Session.SymbolQuote} {Data.Interval.Name} UTC " +
                $"{trendIndicator} candles={mainIndicator.CandleCount} points={mainIndicator.ZigZagList.Count}";

            // Akward...
            chart.Axes[0].MajorStep = (24 * 60 / Data.Interval.Duration) * Data.Interval.Duration;
            chart.Axes[0].MinorStep = (24 * 60 / Data.Interval.Duration) * Data.Interval.Duration / 6;

            // Draw candles (should do this just once, it will not change unless interval changes)
            Candles.Draw(chart, Data.Symbol, Data.Interval, Session.MinDate, Session.MaxDate);

            // force indicators to draw itself
            optionsInChart.Clear();
            OnDisplayOptionsChanged(this, null!);
            OnTrendSettingsChanged(this, null!);
            OnFibSettingsChanged(this, null!);
            OnDisplayOptionsChanged(this, null!);

            // force refresh of the PlotModel
            _refreshChart = false;
            PlotModel.InvalidatePlot(true);
            OnPropertyChanged(nameof(PlotModel));
            OnPropertyChanged(nameof(PlotView));
        }


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
    private void PlotModel_MouseMove(object? sender, OxyMouseEventArgs e)
    {
        if (Data != null && CrossHairY != null && CrossHairX != null)
        {
            var model = PlotModel;
            var screenPoint = new ScreenPoint(e.Position.X, e.Position.Y);
            double x = model.Axes[0].InverseTransform(screenPoint.X);
            double y = model.Axes[1].InverseTransform(screenPoint.Y);

            var symbolInterval = Data.Symbol.GetSymbolInterval(Session.ActiveInterval);
            CandleTime unix = new CandleTime((uint)x) + symbolInterval.Interval.Duration / 2;
            unix = IntervalTools.StartOfIntervalCandle(unix, symbolInterval.Interval.Duration);
            if (unix < 0)
                return;

            try
            {
                // Update crosshair coordinates
                CrossHairX.X = unix.Minutes;
                CrossHairX.LineStyle = LineStyle.DashDot;

                CrossHairY.Y = y;
                CrossHairY.LineStyle = LineStyle.DashDot;

                string subtitle;
                if (symbolInterval.CandleList.TryGetValue(unix, out CryptoCandle? candle))
                {
                    subtitle = $"{candle.Date.ToLocalTime():ddd yyyy-MM-dd HH:mm}, price: {y.ToString(Data.Symbol.PriceDisplayFormat)}";
                    subtitle += $" (O: {candle.Open.ToString(Data.Symbol.PriceDisplayFormat)}";
                    subtitle += $" H: {candle.High.ToString(Data.Symbol.PriceDisplayFormat)}";
                    subtitle += $" L: {candle.Low.ToString(Data.Symbol.PriceDisplayFormat)}";
                    subtitle += $" C: {candle.Close.ToString(Data.Symbol.PriceDisplayFormat)}";
                    subtitle += $" V: {candle.Volume.ToString0()})";
                }
                else
                {
                    DateTime date = unix.ToDateTime();
                    subtitle = $"{date.ToLocalTime():yyyy-MM-dd HH:mm}, price: {y.ToString(Data.Symbol.PriceDisplayFormat)}";
                }

                PlotModel.Subtitle = subtitle;

                PlaybackControls.UpdateIntervalDisplay(Session.ActiveInterval.ToString());
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Info("UpdateCrosshair.Error " + error.ToString());
            }



            //if (IsMeasuring Control.ModifierKeys == Keys.Shift && mouseDownPointX != null && mouseDownPointY != null)
            if (IsMeasuring && mouseDownPointX != null && mouseDownPointY != null)
            {
                ScreenPoint mouseUpPoint = new(e.Position.X, e.Position.Y);
                // assuming your x-axis is at the bottom and your y-axis is at the left.
                OxyPlot.Axes.Axis? xAxis = PlotModel!.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
                OxyPlot.Axes.Axis? yAxis = PlotModel!.Axes.FirstOrDefault(a => a.Position == AxisPosition.Right);
                if (xAxis == null || yAxis == null)
                    return;

                double xstart = xAxis.InverseTransform((double)mouseDownPointX);
                double ystart = yAxis.InverseTransform((double)mouseDownPointY);
                double xend = xAxis.InverseTransform(mouseUpPoint.X);
                double yend = yAxis.InverseTransform(mouseUpPoint.Y);
                double perc = 100 * (yend - ystart) / Math.Min(yend, ystart);
                //Line = $"{Session.SymbolBase}{Session.SymbolQuote} {perc:N2}%";

                if (lastRectangle != null)
                    PlotModel.Annotations.Remove(lastRectangle);

                lastRectangle = new RectangleAnnotation
                {
                    MinimumX = xstart,
                    MaximumX = xend,
                    MinimumY = ystart,
                    MaximumY = yend,
                    TextRotation = 0,
                    Text = $"{perc:N2}%",
                    Fill = OxyColor.FromAColor(99, OxyColors.Blue),
                    Stroke = OxyColors.Black,
                    StrokeThickness = 2
                };
                PlotModel.Annotations.Add(lastRectangle);
            }

            PlotModel.InvalidatePlot(true);
            OnPropertyChanged(nameof(PlotModel));
            OnPropertyChanged(nameof(PlotView));
        }
    }


    private void ButtonFocusLastCandlesClick(object? sender, EventArgs e)
    {
        if (Data != null && PlotModel != null && PlotView != null && Data.SymbolInterval.CandleList.Count > 0)
        {
            decimal l = decimal.MaxValue;
            decimal h = decimal.MinValue;
            CryptoCandle candleLast = Data.SymbolInterval.CandleList.Values.Last();
            CandleTime unix = candleLast.OpenTime;
            int count = GlobalData.Settings.Signal.ZonesDlz.CandleCountZoom;
            CryptoCandle xlast = candleLast;
            CryptoCandle xfirst = candleLast;
            while (count > 0)
            {
                if (Data.SymbolInterval.CandleList.TryGetValue(unix, out CryptoCandle? candle))
                {
                    if (candle.High > h)
                        h = candle.High;
                    if (candle.Low < l)
                        l = candle.Low;
                    if (candle.Date < xfirst.Date)
                        xfirst = candle;
                }
                unix -= Data.Interval.Duration;
                count--;
            }

            //PlotModel!.Model = PlotModel;

            int extra = 5;
            if (Session.ShowFibRetracement)
                extra = 25;

            // X axis
            PlotView.ActualModel.Axes[0].Reset();
            PlotView.ActualModel.Axes[0].Minimum = xfirst.OpenTime.Minutes - 5 * Data.Interval.Duration;
            PlotView.ActualModel.Axes[0].Maximum = xlast.OpenTime.Minutes + extra * Data.Interval.Duration;

            // Y axis
            l -= 0.02m * l;
            h += 0.02m * h;
            PlotView.ActualModel.Axes[1].Reset();
            PlotView.ActualModel.Axes[1].Minimum = (double)l;
            PlotView.ActualModel.Axes[1].Maximum = (double)h;

            PlotModel?.InvalidatePlot(true);
            OnPropertyChanged(nameof(PlotModel));
        }
    }


    public void OnClosing()
    {
        SaveSessionSettings();
    }


    private async Task SymbolOrIntervalChanged(bool forceCalculation)
    {
        if (IsCalculating)
            return;

        IsCalculating = true;

        try
        {
            SaveSessionSettings();

            Session.ForceCalculation = forceCalculation;
            if (!PrepareSessionData(out string reason))
            {
                WindowTitle = $"{GlobalData.ActiveExchange!.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Error {reason}";
                return;
            }
            WindowTitle = $"{Data!.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Calculating...";

            await CalculateZonesAndPlotZigZagAsync();
            ButtonFocusLastCandlesClick(null, EventArgs.Empty);

            WindowTitle = $"{Data!.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName}";
        }
        catch (Exception error)
        {
            Debug.WriteLine($"Error: {error.Message}");
            ScannerLog.Logger.Error(error, "Calculate error");
            WindowTitle = $"{Data!.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Error {error.Message}";
        }
        finally
        {
            IsCalculating = false;
        }
    }


}
