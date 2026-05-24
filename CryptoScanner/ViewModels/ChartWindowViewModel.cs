using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Messages;
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
using System.Text.Json;

namespace CryptoScanner.ViewModels;

public partial class ChartWindowViewModel : ObservableObject
{
    // Symbol related data
    private readonly ZoneSession Session = new();
    private CryptoSymbol Symbol { get; set; }
    private CryptoInterval Interval { get; set; }
    private CryptoSymbolInterval SymbolInterval { get; set; }

    // Signals and positions for this symbol
    public List<CryptoSignal> SignalList { get; set; } = [];
    public List<CryptoPosition> PositionList { get; set; } = [];
    private CandleTime lastLoadedSignalsAndPositions = CandleTime.MinValue;

    // ZigZag data for the FIB trend and Main trend display
    private TrendZigZagIndicatorList TrendZigZagIndicatorList { get; set; } = [];

    [ObservableProperty]
    private bool _isCalculating = false;

    [ObservableProperty]
    private OxyPlot.Avalonia.PlotView _plotView;

    [ObservableProperty]
    private PlotModel _plotModel;

    // Chart crosshair annotations
    private LineAnnotation? CrossHairX;
    private LineAnnotation? CrossHairY;

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
    private string _windowTitle = "Chart";

    // TODO: How to fix this?
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ChartWindowViewModel()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        // Initialize sub-ViewModels
        _symbolSelector = new ChartSymbolSelectorViewModel();
        _trendSettings = new ChartTrendSettingsViewModel();
        _fibSettings = new ChartFibSettingsViewModel();
        _displayOptions = new ChartOptionsViewModel();

        _plotModel = CreatePlotModel();

        // Create PlotView in ViewModel
        _plotView = new OxyPlot.Avalonia.PlotView
        {
            Model = PlotModel,
            //Dock = DockStyle.Fill,
            //Background = OxyColors.Transparent,
            Controller = CreateController()
        };

        // Load session
        ClearOptions();
        Session = LoadSessionSettings();
        Session.UseOptimizing = false;

        // Load settings into sub-ViewModels
        SymbolSelector.LoadFromSession(Session);
        TrendSettings.LoadFromSession(Session);
        FibSettings.LoadFromSession(Session);
        DisplayOptions.LoadFromSession(Session);

        // Subscribe to changes from sub-ViewModels
        SymbolSelector.PropertyChanged += OnSymbolChanged;
        TrendSettings.PropertyChanged += TrendSettingsChanged;
        FibSettings.PropertyChanged += FibSettingsChanged;
        DisplayOptions.PropertyChanged += DisplayOptionsChanged;

        RefreshCommand.ExecuteAsync(null);
        System.Diagnostics.Debug.WriteLine($"VisualisationViewModel default constructor called");
    }


    //private string lastDisplay = string.Empty;
    private void ClearOptions(string symbolName = "", string intervalName = "")
    {
        //lastDisplay = string.Empty;

        Symbol = null!;
        Interval = null!;
        SymbolInterval = null!;
        SignalList.Clear();
        PositionList.Clear();
        optionsInChart.Clear();
        optionsInChart["symbol"] = symbolName;
        optionsInChart["interval"] = intervalName;

        TrendZigZagIndicatorList.Clear();
        TrendZigZagIndicatorList.Add((TrendType.Primary, false), new(TrendType.Primary, false));
        TrendZigZagIndicatorList.Add((TrendType.Primary, true), new(TrendType.Primary, true));
        TrendZigZagIndicatorList.Add((TrendType.Secondary, false), new(TrendType.Secondary, false));
        TrendZigZagIndicatorList.Add((TrendType.Secondary, true), new(TrendType.Secondary, true));
    }



    //// A weird option to position the form over Altrady graph
    //private void TransparentClick(object? sender, EventArgs e)
    //{
    //    if (EditTransparant.Checked)
    //    {
    //        BackColor = Color.Lime;
    //        TransparencyKey = Color.Lime;
    //        plotView.BackColor = Color.Lime;
    //    }
    //    else
    //    {
    //        BackColor = SystemColors.Control;
    //        TransparencyKey = Color.Lime;
    //        plotView.BackColor = Color.Black;
    //    }
    //    flowLayoutPanel1.BackColor = SystemColors.Control;
    //}




    //private void ButtonGoLeftOrRight(int direction)
    //{
    //    if (Data != null && plotModel != null)
    //    {
    //        PickupUserInput();
    //        Session.MaxDate += direction * Interval.Duration;
    //        _ = CalculateAsync();
    //    }
    //}


    //private async void ButtonIntervalPlusOrMin(int direction)
    //{
    //    if (Data != null && plotModel != null &&
    //        Session.ActiveInterval + direction >= CryptoIntervalPeriod.interval1m &&
    //        Session.ActiveInterval + direction <= CryptoIntervalPeriod.interval1w)
    //    {
    //        Session.ActiveInterval += direction;
    //        foreach (var serie in plotModel.Series)
    //        {
    //            if (serie.Title == "Candles")
    //            {
    //                plotModel.Series.Remove(serie);
    //                break;
    //            }
    //        }

    //        Symbol.Data.CalculatingZones = true;
    //        try
    //        {
    //            CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(Session.ActiveInterval);
    //            await ZoneCandleEngine.ReadCandlesFromDiskAsync(Symbol, symbolInterval.Interval);
    //            Chart.Candles.Draw(plotModel, Symbol, symbolInterval.Interval, Session.MinDate, Session.MaxDate);
    //        }
    //        finally
    //        {
    //            await ZoneCandleEngine.CleanLoadedCandlesAsync(Symbol);
    //            Symbol.Data.CalculatingZones = false;
    //        }

    //        labelInterval.Text = Session.ActiveInterval.ToString();
    //        plotModel?.InvalidatePlot(true);
    //    }
    //}


    private static PlotController CreateController()
    {
        var controller = new PlotController();

        // Change the default behaviour
        controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
        controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Shift, PlotCommands.ZoomRectangle);
        controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control, PlotCommands.ZoomRectangle);
        controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control | OxyModifierKeys.Alt, 2, PlotCommands.ResetAt);
        //controller.UnbindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Shift);

        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control | OxyModifierKeys.Alt, PlotCommands.ZoomRectangle);
        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control, 2, PlotCommands.ResetAt);
        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Alt, PlotCommands.PanAt);
        controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Shift, PlotCommands.SnapTrack);
        return controller;
    }


    // Tracks the last day number shown in a label, to avoid repeating it within the same day.
    // Reset whenever x goes backward, which signals the start of a new render pass.
    private static int _lastShownDay = -1;
    private static double _lastTickX = double.MinValue;

    private static string LabelFormatterX(double x)
    {
        // OxyPlot renders ticks left-to-right within a single pass.
        // If x goes backward, a new render pass has started - reset day tracking.
        if (x < _lastTickX)
            _lastShownDay = -1;
        _lastTickX = x;

        var unix = new CandleTime((uint)x);
        DateTime date = unix.ToLocalTime();  // Local: used for all labels and boundary detection

        if (date.Hour == 0 && date.Minute == 0)
        {
            // Local midnight tick: show local day number
            _lastShownDay = date.Day;
            string s = date.Day.ToString();
            if (date.Day == 1)
            {
                // First of month: add month name on second line
                string monthName = date.ToString("MMM", CultureInfo.InvariantCulture);
                s += "\r\n" + monthName;
            }
            return s;
        }

        // Intra-day tick: show local time, add day on second line only on the first tick of each new local day
        string time = $"{date.Hour:D2}:{date.Minute:D2}";
        if (date.Day != _lastShownDay)
        {
            _lastShownDay = date.Day;
            time += "\r\n" + date.Day;
        }
        return time;
    }

    private string LabelFormatterY(double x)
    {
        string s = x.ToString(Symbol?.PriceDisplayFormat);
        return s;
    }


    public PlotModel CreatePlotModel()
    {
        // Create the PlotModel (model) once..

        PlotModel chart = new()
        {
            Background = OxyColors.Black,

            //Title = "Chart 1.2.3.",
            //Subtitle = "...",
            TitleFont = Const.OxyFontName,
            TitleColor = OxyColors.White,

            TextColor = OxyColors.White,
            SubtitleFont = Const.OxyFontName,
            SubtitleColor = OxyColors.White,
            SubtitleFontWeight = FontWeights.Bold,
        };

        chart.Axes.Clear();

        // x-axis: uses LocalMidnightLinearAxis so major ticks align to local midnight (00:00) instead of UTC midnight
        chart.Axes.Add(new LocalMidnightLinearAxis
        {
            //Title = "Time",
            //StringFormat = "dd-MM HH:mm",
            Font = Const.OxyFontName,
            FontSize = Const.OxyFontSize,
            TextColor = OxyColors.White,
            LabelFormatter = LabelFormatterX,
            Position = AxisPosition.Bottom,

            //MajorTickSize = 15,
            //MinorTickSize = 5,
            TicklineColor = OxyColors.Gray,
            TickStyle = OxyPlot.Axes.TickStyle.Inside,

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
            TickStyle = OxyPlot.Axes.TickStyle.Inside,

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
        chart.Axes[0].AxisChanged += (s, e) => UpdateAxisTicks(chart.Axes[0]);
#pragma warning restore CS0618 // Type or member is obsolete

        return chart;
    }

    private static void UpdateAxisTicks(Axis axis)
    {
        // ActualMinimum/ActualMaximum reflect the current visible range (respects zoom and pan),
        // but are NaN before the first render. Fall back to Minimum/Maximum in that case.
        double min = double.IsNaN(axis.ActualMinimum) ? axis.Minimum : axis.ActualMinimum;
        double max = double.IsNaN(axis.ActualMaximum) ? axis.Maximum : axis.ActualMaximum;
        double visibleRange = max - min;
        if (double.IsNaN(visibleRange) || visibleRange <= 0)
            return;

        (double major, double minor) = PickTickSteps(visibleRange);
        axis.MajorStep = major;
        axis.MinorStep = minor;
    }

    /// <summary>
    /// Returns (majorStep, minorStep) in minutes based on the visible range (also in minutes).
    /// Major ticks get a date/time label via LabelFormatterX; minor ticks only get a tick mark.
    /// At midnight the formatter shows the day number; at other hours it shows "HH:mm".
    /// </summary>
    private static (double majorStep, double minorStep) PickTickSteps(double visibleRange)
    {
        // visibleRange in minutes
        if (visibleRange <= 4 * 60) return (30, 5);          // = 4h:  major 30min,  minor 5min
        if (visibleRange <= 12 * 60) return (60, 15);         // = 12h: major 1h,     minor 15min
        if (visibleRange <= 3 * 1440) return (240, 60);        // = 3d:  major 4h,     minor 1h   ? "04:00","08:00" etc.
        if (visibleRange <= 7 * 1440) return (480, 240);       // = 7d:  major 8h,     minor 4h   ? "08:00","16:00" per dag
        if (visibleRange <= 30 * 1440) return (1440, 240);      // = 30d: major 1d,     minor 4h
        if (visibleRange <= 90 * 1440) return (10080, 1440);    // = 90d: major 1w,     minor 1d
        return (43200, 10080);                                     // >90d:  major ~1mo,   minor 1w
    }

    //private static double SnapToNiceInterval(double rawStep)
    //{
    //    // Nice intervals in minutes, from 1m up to ~2 months
    //    double[] niceIntervals =
    //    [
    //        1, 5, 10, 15, 30,           // minutes
    //    60, 120, 240, 360, 720,     // hours
    //    1440, 2880, 7200, 10080,    // days / weeks
    //    20160, 43200, 86400         // 2w / month / 2m
    //    ];

    //    foreach (var interval in niceIntervals)
    //        if (interval >= rawStep)
    //            return interval;

    //    return niceIntervals[^1];
    //}





    // Save the edits to the session configuration
    private void PickupUserInput()
    {
        SymbolSelector.SaveToSession(Session);
        TrendSettings.SaveToSession(Session);
        FibSettings.SaveToSession(Session);
        DisplayOptions.SaveToSession(Session);
    }

    public static ZoneSession LoadSessionSettings()
    {

        try
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

        }
        catch (Exception error)
        {
            // ignore and fallback on new config (not that important)
            ScannerLog.Logger.Error(error);
        }
        return new();
    }

    public void SaveSessionSettings()
    {
        PickupUserInput();

        // save current session settings
        Directory.CreateDirectory(GlobalData.AppDataFolder);
        string fileName = Path.Combine(GlobalData.AppDataFolder, $"CryptoScanBot-chart.json");
        string text = JsonSerializer.Serialize(Session, JsonTools.JsonSerializerIndented);
        File.WriteAllText(fileName, text);
    }


    private void OnSymbolChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChartSymbolSelectorViewModel.SelectedSymbol) ||
            e.PropertyName == nameof(ChartSymbolSelectorViewModel.SelectedInterval))
        {
            if (IsCalculating)
            {
                // A refresh is already running; remember to retry once it finishes.
                // The retry picks up the latest ViewModel state via PickupUserInput().
                _pendingRefresh = true;
                return;
            }
            RefreshCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand]
    private void OpenTradingApp()
    {
        if (Symbol != null)
        {
            CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;
            if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
                tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
            CommandHelper.ActivateTradingApp(GlobalData.Settings.General.TradingApp, Symbol, Interval, tradingAppInternExtern);
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


    private bool _refreshChart = false;
    private bool _pendingRefresh = false; // set when a symbol/interval change arrived while IsCalculating
    private readonly Dictionary<string, string> optionsInChart = [];
    private bool Toggle(PlotModel model, string group, bool currentValue, string prefix = "")
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
                RemoveFromChart(model, group);
                return false; // already done..
            }
        }
        return false;
    }


    private void TrendSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Display options changed
        if (Symbol == null)
            return;
        PickupUserInput();
        var model = PlotModel;

        string group = "maintrend.title";
        if (Toggle(model, group, true, $"{Session.SymbolBase}{Session.SymbolQuote}{Interval.Name}{Session.TrendType}"))
        {
            SettingsZigZag mainTrend = Session.TrendType == TrendType.Primary ? GlobalData.Settings.Trend.Primary : GlobalData.Settings.Trend.Secondary;
            var mainIndicator = TrendZigZagIndicatorList[(mainTrend.TrendType, mainTrend.UseHighLow)];
            CryptoTrendIndicator trendIndicator = TrendInterval.InterpretZigZagPoints(mainIndicator, null);
            model.Title = $"{Session.SymbolBase}{Session.SymbolQuote} {Interval.Name} UTC " +
                $"{trendIndicator} candles={mainIndicator.CandleCount} points={mainIndicator.ZigZagList.Count}";
        }

        // Draw trend zigzag
        group = "candles.zigzag";
        if (Toggle(model, group, Session.TrendShowZigZag, Session.TrendType.ToString()))
        {
            RemoveFromChart(model, group);
            SettingsZigZag mainTrend = Session.TrendType == TrendType.Primary ? GlobalData.Settings.Trend.Primary : GlobalData.Settings.Trend.Secondary;
            var mainIndicator = TrendZigZagIndicatorList[(mainTrend.TrendType, mainTrend.UseHighLow)];
            ZigZag.Draw(model, mainIndicator.ZigZagList, "maintrend",
                OxyColors.White, Session.MinDate, Session.MaxDate, group);
        }

        if (_refreshChart && sender != null)
        {
            model.InvalidatePlot(true);
            OnPropertyChanged(nameof(ChartWindowViewModel.PlotModel));
            OnPropertyChanged(nameof(PlotView));
            _refreshChart = false;
        }
    }

    private void FibSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Display options changed
        if (Symbol == null)
            return;
        PickupUserInput();
        var model = PlotModel;

        // Draw FIB retracement
        string group = "fib.indicator";
        if (Toggle(model, group, Session.ShowFibRetracement))
            FibRetracement.Draw(model, Symbol, Interval,
                TrendZigZagIndicatorList[(Session.FibTrend == 0 ? TrendType.Primary : TrendType.Secondary, true)], group);

        // Draw FIB zigzag
        group = "fib.trendlines";
        if (Toggle(model, group, Session.ShowFibZigZag, Session.FibTrend.ToString()))
        {
            RemoveFromChart(model, group);
            ZigZag.Draw(model,
                TrendZigZagIndicatorList[(Session.FibTrend == 0 ? TrendType.Primary : TrendType.Secondary, true)].ZigZagList,
                "fib", OxyColors.White, Session.MinDate, Session.MaxDate, group);
        }

        if (_refreshChart && sender != null)
        {
            model.InvalidatePlot(true);
            OnPropertyChanged(nameof(PlotModel));
            OnPropertyChanged(nameof(PlotView));
            _refreshChart = false;
        }
    }

    private CandleTime lastCandleTime = CandleTime.MinValue;
    private void DisplayOptionsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Display options changed
        if (Symbol == null)
            return;
        PickupUserInput();

        SettingsZigZag mainTrend = Session.TrendType == TrendType.Primary ? GlobalData.Settings.Trend.Primary : GlobalData.Settings.Trend.Secondary;
        var mainIndicator = TrendZigZagIndicatorList[(mainTrend.TrendType, mainTrend.UseHighLow)];
        var model = PlotModel;

        // Draw double top/bottom
        string group = "dtb";
        if (Toggle(model, group, Session.ShowDtb)) // Test double top/bottom
            Dtb.Draw(model, Interval, mainIndicator, group);

        // Draw DLZ zones
        group = "dlz.zones";
        if (Toggle(model, group, Session.ShowDlzZones))
            DlzZones.Draw(model, Symbol, Session.MinDate, Session.MaxDate, group);

        // Draw FVG zones
        group = "fvg.zones";
        if (Toggle(model, group, Session.ShowFvgZones))
            FvgZones.Draw(model, Symbol, Session.MinDate, Session.MaxDate, group);

        // Draw Nadaraya Watson Envelope (non repainting)
        group = "nwe.notrepainting";
        if (Toggle(model, group, Session.ShowNadarayaWatsonEnvelope))
            NadarayaWatsonEnvelope.Draw(model, Symbol, Interval, Session.MinDate, Session.MaxDate, false, group);

        // Draw Nadaraya Watson Envelope (repainting)
        group = "nwe.repainting";
        if (Toggle(model, group, Session.ShowNadarayaWatsonEnvelopeRepainting))
            NadarayaWatsonEnvelope.Draw(model, Symbol, Interval, Session.MinDate, Session.MaxDate, true, group);

        // Draw NWE × BB crossover markers
        group = "nwe.bb";
        if (Toggle(model, group, Session.ShowNweBb))
            NweBb.Draw(model, Symbol, Interval, Session.MinDate, Session.MaxDate, group);

        // Draw Bollinger Bands
        group = "bb";
        if (Toggle(model, group, Session.ShowBollingerBand))
            Bollingerbands.Draw(model, Symbol, Interval, Session.MinDate, Session.MaxDate, group);

        // Draw Keltner Channel
        group = "kc";
        if (Toggle(model, group, Session.ShowKeltnerChannel))
            KeltnerChannel.Draw(model, Symbol, Interval, Session.MinDate, Session.MaxDate, group);

        // Draw PSar
        group = "psar";
        if (Toggle(model, group, Session.ShowPSar))
            PSar.Draw(model, Symbol, Interval, Session.MinDate, Session.MaxDate, group);

        // Draw SMA lines
        group = "sma";
        if (Toggle(model, group, Session.ShowSmaLinesSbm))
        {
            Sma.Draw(model, Symbol, Interval, 200, OxyColors.Red, Session.MinDate, Session.MaxDate, group);
            Sma.Draw(model, Symbol, Interval, 50, OxyColors.Orange, Session.MinDate, Session.MaxDate, group);
            Sma.Draw(model, Symbol, Interval, 20, OxyColors.Green, Session.MinDate, Session.MaxDate, group);
        }

        // Draw BBMA
        group = "bbma";
        if (Toggle(model, group, Session.ShowBbma))
            Bbma.Draw(model, Symbol, Interval, Session.MinDate, Session.MaxDate, group);


        // Other options
        // Draw candles (note: we draw additional candles each minutes if needed)
        group = "candles";
        if (Toggle(model, group, Session.ShowCandles, Session.IntervalName + lastCandleTime.Minutes.ToString()))
            lastCandleTime = Candles.Draw(model, Symbol, Interval, Session.MinDate, Session.MaxDate, group);

        // Draw pivots
        group = "pivots";
        if (Toggle(model, group, Session.ShowPoints))
            Points.Draw(model, mainIndicator.PivotList, Session.MinDate, Session.MaxDate, group);

        // Draw signals
        group = "signals";
        if (Toggle(model, group, Session.ShowSignals))
            Signals.Draw(model, SignalList, Session.MinDate, Session.MaxDate, group);

        // Draw signals
        group = "positions";
        if (Toggle(model, group, Session.ShowPositions))
            Positions.Draw(model, PositionList, Interval, Session.MinDate, Session.MaxDate, group);


        if (_refreshChart && sender != null)
        {
            model.InvalidatePlot(true);
            OnPropertyChanged(nameof(ChartWindowViewModel.PlotModel));
            OnPropertyChanged(nameof(PlotView));
            _refreshChart = false;
        }
    }

    [RelayCommand]
    private async Task Calculate()
    {
        await SymbolOrIntervalChangedAsync(true);
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await SymbolOrIntervalChangedAsync(false);
    }

    //[RelayCommand]
    //private void ZoomLast()
    //{
    //    // Zoom to last candles
    //    if (Data != null)
    //    {
    //        Session.MaxDate = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1);
    //        Session.MaxDate = IntervalTools.StartOfIntervalCandle(Session.MaxDate, Interval.Duration);
    //        Session.MinDate = Session.MaxDate - GlobalData.Settings.Signal.ZonesDlz.CandleCount * Interval.Duration;

    //        // ?

    //        PlotModel.InvalidatePlot(true);
    //        OnPropertyChanged(nameof(PlotModel));
    //        OnPropertyChanged(nameof(PlotView));
    //    }
    //}

    [RelayCommand]
    private void ZoomLast()
    {
        if (Symbol != null && PlotView.Model != null && SymbolInterval.CandleList.Count > 0)
        {
            decimal l = decimal.MaxValue;
            decimal h = decimal.MinValue;
            CryptoCandle candleLast = SymbolInterval.CandleList.Values.Last();
            CandleTime unix = candleLast.OpenTime;
            int count = GlobalData.Settings.Signal.ZonesDlz.CandleCountZoom;
            CryptoCandle xlast = candleLast;
            CryptoCandle xfirst = candleLast;
            while (count > 0)
            {
                if (SymbolInterval.CandleList.TryGetValue(unix, out CryptoCandle candle))
                {
                    if (candle!.High > h)
                        h = candle.High;
                    if (candle.Low < l)
                        l = candle.Low;
                    if (candle.Date < xfirst.Date)
                        xfirst = candle;
                }
                unix -= Interval.Duration;
                count--;
            }

            //PlotView!.Model = PlotView.Model;

            int extra = 5;
            if (Session.ShowFibRetracement)
                extra = 25;
            // X axis
            PlotView.ActualModel.Axes[0].Reset();
            PlotView.ActualModel.Axes[0].Minimum = xfirst.OpenTime.Minutes - 5 * Interval.Duration;
            PlotView.ActualModel.Axes[0].Maximum = xlast.OpenTime.Minutes + extra * Interval.Duration;

            // Y axis
            l -= 0.02m * l;
            h += 0.02m * h;
            PlotView.ActualModel.Axes[1].Reset();
            PlotView.ActualModel.Axes[1].Minimum = (double)l;
            PlotView.ActualModel.Axes[1].Maximum = (double)h;

            // Axis range is now known; UpdateAxisTicks falls back to Minimum/Maximum when ActualMinimum is NaN
            UpdateAxisTicks(PlotView.ActualModel.Axes[0]);
            PlotModel?.InvalidatePlot(true);
            OnPropertyChanged(nameof(PlotModel));
        }
    }

    /// <summary>
    /// Called after the window's first OxyPlot render so that ActualMinimum/ActualMaximum
    /// are valid. Re-applies the correct axis tick spacing without resetting the zoom level.
    /// </summary>
    public void RefreshAxisTicks()
    {
        if (PlotView?.ActualModel?.Axes.Count > 0)
        {
            UpdateAxisTicks(PlotView.ActualModel.Axes[0]);
            PlotModel?.InvalidatePlot(false);
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


    // Display
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
        var model = PlotModel;
        if (Symbol != null && CrossHairY != null && CrossHairX != null)
        {
            var screenPoint = new ScreenPoint(e.Position.X, e.Position.Y);
            double x = model.Axes[0].InverseTransform(screenPoint.X);
            double y = model.Axes[1].InverseTransform(screenPoint.Y);

            var symbolInterval = Symbol.GetSymbolInterval(Session.ActiveInterval);
            CandleTime unix = new CandleTime((uint)x) + symbolInterval.Interval.Duration / 2;
            unix = IntervalTools.StartOfIntervalCandle(unix, symbolInterval.Interval.Duration);
            if (unix < 0)
                return;

            try
            {

                // Update croshair coordinates
                CrossHairX.X = unix.Minutes;
                CrossHairY.Y = y;
                CrossHairX.LineStyle = LineStyle.DashDot;
                CrossHairY.LineStyle = LineStyle.DashDot;

                string subtitle;
                if (symbolInterval.CandleList.TryGetValue(unix, out CryptoCandle candle))
                {
                    subtitle = $"{candle.Date.ToLocalTime():ddd yyyy-MM-dd HH:mm}, price: {y.ToString(Symbol.PriceDisplayFormat)}";
                    subtitle += $" (O: {candle.Open.ToString(Symbol.PriceDisplayFormat)}";
                    subtitle += $" H: {candle.High.ToString(Symbol.PriceDisplayFormat)}";
                    subtitle += $" L: {candle.Low.ToString(Symbol.PriceDisplayFormat)}";
                    subtitle += $" C: {candle.Close.ToString(Symbol.PriceDisplayFormat)}";
                    subtitle += $" V: {candle.Volume.ToString0()})";
                }
                else
                {
                    DateTime date = unix.ToDateTime();
                    subtitle = $"{date.ToLocalTime():yyyy-MM-dd HH:mm}, price: {y.ToString(Symbol.PriceDisplayFormat)}";
                }
                model.Subtitle = subtitle;

                PlotView.InvalidatePlot(true);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Info("PlotModel_MouseMove.Error " + error.ToString());
            }
        }

        //if (IsMeasuring Control.ModifierKeys == Keys.Shift && mouseDownPointX != null && mouseDownPointY != null)
        if (IsMeasuring && mouseDownPointX != null && mouseDownPointY != null)
        {
            ScreenPoint mouseUpPoint = new(e.Position.X, e.Position.Y);
            // assuming your x-axis is at the bottom and your y-axis is at the left.
            Axis? xAxis = model!.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
            Axis? yAxis = model!.Axes.FirstOrDefault(a => a.Position == AxisPosition.Right);
            if (xAxis == null || yAxis == null)
                return;

            double xstart = xAxis.InverseTransform((double)mouseDownPointX);
            double ystart = yAxis.InverseTransform((double)mouseDownPointY);
            double xend = xAxis.InverseTransform(mouseUpPoint.X);
            double yend = yAxis.InverseTransform(mouseUpPoint.Y);
            double perc = 100 * (yend - ystart) / Math.Min(yend, ystart);
            WindowTitle = $"{Session.SymbolBase}{Session.SymbolQuote} {perc:N2}%";

            if (lastRectangle != null)
                model.Annotations.Remove(lastRectangle);

            lastRectangle = new RectangleAnnotation
            {
                Layer = AnnotationLayer.BelowSeries,
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
            model.Annotations.Add(lastRectangle);
        }

        model.InvalidatePlot(true);
        OnPropertyChanged(nameof(PlotModel));
        OnPropertyChanged(nameof(PlotView));
    }



    public void OnClosing()
    {
        SaveSessionSettings();
    }


    private async Task<(bool succes, string reason)> PrepareSessionDataAsync()
    {
        string reason = "";

        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
        {
            reason = "Exchange not found";
            ScannerLog.Logger.Info($"{reason}");
            return (false, reason);
        }

        if (!exchange.SymbolListName.TryGetValue(Session.SymbolBase + Session.SymbolQuote, out CryptoSymbol? symbol))
        {
            reason = "Symbol not found";
            ScannerLog.Logger.Info($"{reason}");
            return (false, reason);
        }

        var interval = GlobalData.IntervalList.Find(x => x.Name.Equals(Session.IntervalName));
        if (interval == null)
        {
            reason = "Interval not supported";
            ScannerLog.Logger.Info($"{reason}");
            return (false, reason);
        }

        // Reset dates if symbol/interval changed
        string displayedSymbol = optionsInChart["symbol"];
        string displayedInterval = optionsInChart["interval"];
        if (displayedSymbol != symbol.Name || displayedInterval != interval.Name || Session.ForceCalculation)
        {
            ClearOptions(symbol.Name, interval.Name);
            Symbol = symbol;
            Interval = interval;
            SymbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            lastLoadedSignalsAndPositions = CandleTime.MinValue;

            Session.IntervalName = Interval.Name;
            Session.ActiveInterval = Interval.IntervalPeriod;

            await ZoneCandleEngine.ReadCandlesFromDiskAsync(symbol, interval);


            // Clear all series and and all annotations except the crosshairs
            var chart = PlotView.Model;
            chart.Series.Clear();
            foreach (var annotation in chart.Annotations.ToList())
            {
                if (annotation.Tag?.ToString() != "crosshair")
                    chart.Annotations.Remove(annotation);
            }

            UpdateAxisTicks(chart.Axes[0]);
        }

        // Reset the min and maxdate so the refresh draw'subtitle the new candles and attributes
        int candleFetchCount = GlobalData.Settings.Signal.ZonesDlz.CandleCount;
        Session.MaxDate = CandleTime.AlignFromDateTime(DateTime.UtcNow, interval.Duration);
        Session.MinDate = Session.MaxDate - candleFetchCount * Interval.Duration;
        //Session.MaxDate += 1 * Interval.Duration; // Allow room for extra candles (we draw the 1m candles there)

        // Load or refresh signals each minute
        var currentTime = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1);
        if (lastLoadedSignalsAndPositions.Minutes != currentTime)
        {
            lastLoadedSignalsAndPositions = Session.MaxDate;
            ExtraData.LoadSignalsForSymbol(symbol, Session.MinDate, SignalList);
            optionsInChart.TryAdd("signals", "");
            optionsInChart["signals"] = "";
            ExtraData.LoadPositionsForSymbol(symbol, Session.MinDate, PositionList);
            optionsInChart.TryAdd("positions", "");
            optionsInChart["positions"] = "";
        }

        return (true, "");
    }

    private async Task SymbolOrIntervalChangedAsync(bool forceCalculation)
    {
        if (IsCalculating)
            return;
        IsCalculating = true;

        try
        {
            PickupUserInput();
            SaveSessionSettings();

            Session.ForceCalculation = forceCalculation;
            var (succes, reason) = await PrepareSessionDataAsync();
            if (!succes)
            {
                WindowTitle = $"{GlobalData.ActiveExchange!.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Error {reason}";
                return;
            }
            WindowTitle = $"{Symbol.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Calculating...";


            // Hide the crosshair cursor
            if (CrossHairX != null && CrossHairY != null)
            {
                CrossHairX.LineStyle = LineStyle.None;
                CrossHairY.LineStyle = LineStyle.None;
            }

            SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory = [];

            await Symbol.Data.ZoneLock.WaitAsync();
            try
            {
                // Load and (re)calculate the zones
                ZoneDlz.LoadZonesForSymbol(Symbol);

                // Calculate the required zigzag points to draw the FIB and/or Main trend
                // (DLZ zones will not be calculated, routine can be splitted I guess)
                await ZoneDlz.LoadHistoricCandles(Symbol, Interval, loadedCandlesInMemory);
                await ZoneDlz.CalculatePivots(Symbol, Interval, Session.MinDate, Session.MaxDate, TrendZigZagIndicatorList);

                // Force DLZ and FVG zones to be calculated (they are calculated on other intervals)
                if (Session.ForceCalculation)
                {

                    // Calculate the DLZ zones for the configured intervals
                    foreach (var intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
                    {
                        if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var intervalX))
                        {
                            await ZoneDlz.CalculateZonesAsync(ShowProgress, Symbol, intervalX, loadedCandlesInMemory);
                        }
                    }

                    // Calculate the FVG zones for the configured intervals
                    foreach (var intervalName in GlobalData.Settings.Signal.ZonesFvg.IntervalList)
                    {
                        if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var intervalX))
                        {
                            await ZoneFvg.CalculateZonesAsync(ShowProgress, Symbol, intervalX, loadedCandlesInMemory);
                        }
                    }

                    // Refresh the Distance column for this symbol in the symbol grid.
                    GlobalData.SendMvvmMessage(new ZonesCalculatedForSymbolMessage(Symbol));
                }

                // Draw the indicator layers and candles
                DisplayOptionsChanged(null, null!);
                FibSettingsChanged(null, null!);
                TrendSettingsChanged(null, null!);
            }
            finally
            {
                await ZoneCandleEngine.SaveCandleDataToDiskAsync(Symbol, loadedCandlesInMemory);
                await ZoneCandleEngine.CleanLoadedCandlesAsync(Symbol);
                Symbol.Data.ZoneLock.Release();
            }

            ZoomLast();
            PlotModel.InvalidatePlot(true);

            WindowTitle = $"{Symbol.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName}";
        }
        catch (Exception error)
        {
            Debug.WriteLine($"Error: {error.Message}");
            ScannerLog.Logger.Error(error, "Calculate error");
            WindowTitle = $"{Symbol.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Error {error.Message}";
        }
        finally
        {
            IsCalculating = false;

            // If the user changed symbol/interval while we were busy, process it now.
            if (_pendingRefresh)
            {
                _pendingRefresh = false;
                _ = RefreshCommand.ExecuteAsync(null);
            }
        }
    }



}