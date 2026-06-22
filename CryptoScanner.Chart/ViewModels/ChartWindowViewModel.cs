using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Context;
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
using OxyPlot.Series;

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

    // Controller for the XAML-hosted PlotView. The PlotView itself lives in ChartWindow.axaml
    // (<oxy:PlotView>); the View assigns both this controller and the control reference back to
    // the VM. See the constructor for why the control is NOT created in the VM.
    public IPlotController PlotController { get; }

    // Chart crosshair annotations
    private LineAnnotation? CrossHairX;
    private LineAnnotation? CrossHairY;
    // Second vertical crosshair for the stoch/RSI sub-panel; null when that panel is hidden.
    private LineAnnotation? CrossHairXStoch;
    // Vertical crosshair for the MACD sub-panel; null when that panel is hidden.
    private LineAnnotation? CrossHairXMacd;
    // Third vertical crosshair for the volume sub-panel; null when that panel is hidden.
    private LineAnnotation? CrossHairXVolume;

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

        // The chart is hosted as <oxy:PlotView> in ChartWindow.axaml with Model bound to PlotModel,
        // and the View assigns the control back into PlotView (plus this controller) once it exists.
        // Do NOT create the PlotView here and host it via a ContentControl: a code-created PlotView
        // placed in a ContentControl never gets its OxyPlot control theme applied and renders as a
        // completely blank panel (no axes, no candles) — even though the model is fully populated.
        PlotController = CreateController();

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

        // NOTE: do NOT start RefreshCommand here. Starting it in the ctor causes a race
        // with Window.Show()'s ExecuteInitialLayoutPass — the async refresh mutates
        // PlotView.Model.Series while OxyPlot's Render is iterating it, throwing NRE in
        // PlotElementUtilities.GetClippingRect because Series.XAxis/YAxis are populated
        // only by PlotModel.Update which runs after Render finished its first pass.
        // The Window's Opened event triggers the first refresh; by that point the initial
        // layout pass is done and mutating Series is safe.
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

    // Upper bound for an axis value (CandleTime minutes) that still converts to a valid DateTime.
    // OxyPlot can call the formatter / hit-test with values far outside the data during margin and
    // tick measurement; (uint)x of a negative or huge value wraps and then epoch.AddMinutes overflows
    // DateTime — which aborted the entire render (blank chart). ~4e9 min ≈ year 9580, safely inside range.
    private const double MaxAxisMinutes = 4_000_000_000d;

    private static string LabelFormatterX(double x)
    {
        // Guard against out-of-range axis values (see MaxAxisMinutes): otherwise the conversion below
        // throws ArgumentOutOfRangeException and OxyPlot's render pass aborts, leaving the chart blank.
        if (x < 0 || x > MaxAxisMinutes)
            return "";

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


        // Y-axis (Price) — index 1; Key "price" allows AdjustPanels to find it by key.
        // StartPosition/EndPosition are adjusted at runtime when the stoch panel is toggled.
        chart.Axes.Add(new LinearAxis
        {
            Key = "price",
            Title = "Price",
            LabelFormatter = LabelFormatterY,
            Font = Const.OxyFontName,
            FontSize = Const.OxyFontSize,
            //Font = PlotModel.TitleFont,
            TextColor = OxyColors.White,
            Position = AxisPosition.Right,
            StartPosition = 0.0,
            EndPosition = 1.0,

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
            RequestRefresh();
        }
    }

    /// <summary>
    /// Queue a refresh of the chart's candles/signals/positions for the current symbol, interval
    /// and window. Called from <see cref="OnSymbolChanged"/> when the user changes the symbol or
    /// interval combo box, AND explicitly by <see cref="CryptoScanner.Views.ChartWindowLauncher"/>
    /// after it reuses an already-open window for a different position. That second caller is
    /// required because SelectedBase/Quote/Interval's generated setters skip the PropertyChanged
    /// notification when the new value equals the current one (e.g. picking a different position
    /// on the same symbol+interval, common when browsing a run's position grid) — without this
    /// explicit call OnSymbolChanged would never fire, so the WindowStart/WindowEnd/WindowEmulatorRunId
    /// that were just updated would silently never be picked up and the chart would keep showing
    /// the previous position's candles.
    /// </summary>
    public void RequestRefresh()
    {
        if (IsCalculating)
        {
            // A refresh is already running; remember to retry once it finishes.
            // The retry picks up the latest ViewModel state via PickupUserInput().
            _pendingRefresh = true;
            return;
        }
        // Defer to the next dispatcher cycle. Otherwise, when CommandShowChart sets
        // SelectedBase/Quote/Interval just before calling Window.Show(), this handler
        // would start the async refresh synchronously — and it would still be busy
        // mutating PlotModel.Series when ExecuteInitialLayoutPass runs the first
        // render, throwing NRE in OxyPlot.PlotElementUtilities.GetClippingRect.
        // The Post queues the refresh AFTER Show()'s layout pass completes.
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => _ = RefreshCommand.ExecuteAsync(null),
            Avalonia.Threading.DispatcherPriority.Background);
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


    /// <summary>
    /// Adds or removes the indicator sub-panels (Stoch/RSI oscillator, MACD and/or Volume) and
    /// adjusts the price panel height. Each requested panel gets its own Y-axis added to the
    /// model; disabled panels have their axis (and crosshair) removed entirely. Dynamically
    /// adding/removing the axes avoids OxyPlot rendering artefacts that occur when an axis is
    /// "collapsed" by setting StartPosition == EndPosition == 0.
    ///
    /// Sub-panels stack from the bottom of the chart upwards in fixed order:
    ///   volume (bottom) → MACD (middle) → Stoch/RSI (top sub-panel) → price.
    /// Heights are fixed; positions are computed from which combination is active so that any
    /// subset still leaves a sensible 1 % gap between sub-panels and a 2 % gap to the price
    /// panel.
    /// </summary>
    private void AdjustPanels(bool showOscillator, bool showMacd, bool showVolume)
    {
        var priceAxis = PlotModel.Axes.FirstOrDefault(a => a.Key == "price");
        if (priceAxis == null)
            return;

        // Compute the panel slots from the bottom up. Heights and gaps mirror the previous
        // hard-coded layout so the visuals stay close to what they were before MACD existed.
        const double volumeHeight = 0.10;
        const double macdHeight = 0.10; // was 0.14
        const double subGap = 0.01; // gap between two sub-panels
        const double priceGap = 0.01; // 0.02 gap between the topmost sub-panel and the price panel

        double cursor = 0.0;

        double volStart = 0.0, volEnd = 0.0;
        if (showVolume)
        {
            volStart = cursor;
            volEnd = cursor + volumeHeight;
            cursor = volEnd;
        }

        double macdStart = 0.0, macdEnd = 0.0;
        if (showMacd)
        {
            if (cursor > 0)
                cursor += subGap;
            macdStart = cursor;
            macdEnd = cursor + macdHeight;
            cursor = macdEnd;
        }

        // ---------- Oscillator (Stoch / RSI) sub-panel ----------
        if (showOscillator)
        {
            // When stacked on top of another sub-panel the oscillator shrinks by 1 % to leave
            // room for the inter-panel gap; when alone it gets the full 0.20 slot.
            if (cursor > 0)
                cursor += subGap;
            double stochStart = cursor;
            double stochEnd = cursor + (cursor == 0.0 ? 0.20 : 0.19);
            cursor = stochEnd;

            // Add the axis if missing, otherwise just keep its bounds in sync.
            var existingStoch = PlotModel.Axes.FirstOrDefault(a => a.Key == "stoch");
            if (existingStoch is LinearAxis stochAxisLinear)
            {
                stochAxisLinear.StartPosition = stochStart;
                stochAxisLinear.EndPosition = stochEnd;
            }
            else
            {
                PlotModel.Axes.Add(new LinearAxis
                {
                    Key = "stoch",
                    Title = "Stoch / RSI",
                    Font = Const.OxyFontName,
                    FontSize = Const.OxyFontSize,
                    TextColor = OxyColors.White,
                    Position = AxisPosition.Right,
                    StartPosition = stochStart,
                    EndPosition = stochEnd,
                    Minimum = 0,
                    Maximum = 100,
                    IsZoomEnabled = false,
                    IsPanEnabled = false,
                    MajorStep = 20,
                    MinorStep = 10,
                    TicklineColor = OxyColors.Gray,
                    TickStyle = OxyPlot.Axes.TickStyle.Inside,
                    AxislineStyle = LineStyle.Solid,
                    AxislineColor = OxyColors.Gray,
                    AxislineThickness = 1,
                    MajorGridlineStyle = LineStyle.Dot,
                    MajorGridlineColor = OxyColor.FromAColor(80, OxyColors.Gray),
                });
            }

            // Add a vertical crosshair for the stoch panel if not yet present.
            if (CrossHairXStoch == null)
            {
                CrossHairXStoch = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    Color = OxyColors.White,
                    LineStyle = LineStyle.None,
                    StrokeThickness = 0.5,
                    YAxisKey = "stoch",
                    Tag = "crosshair",
                };
                PlotModel.Annotations.Add(CrossHairXStoch);
            }
        }
        else
        {
            // Defensive cleanup: orphan every series + annotation that still references the
            // "stoch" axis BEFORE removing it. Without this, OxyPlot can hit a
            // NullReferenceException in PlotElementUtilities.GetClippingRect for the brief
            // window between Axes.Remove and the subsequent Toggle → RemoveFromChart that
            // would otherwise clean the series. Stricter series (RectangleBarSeries) surface
            // it; tolerant LineSeries silently absorb the dangling axis reference.
            foreach (var s in PlotModel.Series.OfType<LineSeries>().Where(x => x.YAxisKey == "stoch").ToList())
                PlotModel.Series.Remove(s);
            foreach (var s in PlotModel.Series.OfType<RectangleBarSeries>().Where(x => x.YAxisKey == "stoch").ToList())
                PlotModel.Series.Remove(s);
            foreach (var a in PlotModel.Annotations.OfType<LineAnnotation>().Where(x => x.YAxisKey == "stoch").ToList())
                PlotModel.Annotations.Remove(a);

            // Remove the indicator axis so it does not interfere with the price panel.
            var stochAxis = PlotModel.Axes.FirstOrDefault(a => a.Key == "stoch");
            if (stochAxis != null)
                PlotModel.Axes.Remove(stochAxis);

            // The annotation cleanup above already removed the crosshair; just null the field.
            CrossHairXStoch = null;
        }

        // ---------- MACD sub-panel ----------
        if (showMacd)
        {
            var existingMacd = PlotModel.Axes.FirstOrDefault(a => a.Key == "macd");
            if (existingMacd is LinearAxis macdAxisLinear)
            {
                macdAxisLinear.StartPosition = macdStart;
                macdAxisLinear.EndPosition = macdEnd;
            }
            else
            {
                PlotModel.Axes.Add(new LinearAxis
                {
                    Key = "macd",
                    Title = "MACD",
                    Font = Const.OxyFontName,
                    FontSize = Const.OxyFontSize,
                    TextColor = OxyColors.White,
                    Position = AxisPosition.Right,
                    StartPosition = macdStart,
                    EndPosition = macdEnd,
                    // Auto-range: MACD values can be positive or negative and depend on price
                    // scale (BTC vs DOGE), so we let OxyPlot size the axis to the data.
                    // Trim the default 1 % padding on both ends so the highest/lowest bars
                    // (almost) touch the panel edges instead of leaving a visibly empty band.
                    MinimumPadding = 0,
                    MaximumPadding = 0,
                    IsZoomEnabled = false,
                    IsPanEnabled = false,
                    TicklineColor = OxyColors.Gray,
                    TickStyle = OxyPlot.Axes.TickStyle.Inside,
                    AxislineStyle = LineStyle.Solid,
                    AxislineColor = OxyColors.Gray,
                    AxislineThickness = 1,
                    MajorGridlineStyle = LineStyle.Dot,
                    MajorGridlineColor = OxyColor.FromAColor(80, OxyColors.Gray),
                });
            }

            if (CrossHairXMacd == null)
            {
                CrossHairXMacd = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    Color = OxyColors.White,
                    LineStyle = LineStyle.None,
                    StrokeThickness = 0.5,
                    YAxisKey = "macd",
                    Tag = "crosshair",
                };
                PlotModel.Annotations.Add(CrossHairXMacd);
            }
        }
        else
        {
            // Same defensive cleanup pattern as the stoch/volume branches: orphan every series
            // and annotation pointing at the "macd" axis before the axis itself goes away.
            foreach (var s in PlotModel.Series.OfType<LineSeries>().Where(x => x.YAxisKey == "macd").ToList())
                PlotModel.Series.Remove(s);
            foreach (var s in PlotModel.Series.OfType<RectangleBarSeries>().Where(x => x.YAxisKey == "macd").ToList())
                PlotModel.Series.Remove(s);
            foreach (var a in PlotModel.Annotations.OfType<LineAnnotation>().Where(x => x.YAxisKey == "macd").ToList())
                PlotModel.Annotations.Remove(a);

            var macdAxis = PlotModel.Axes.FirstOrDefault(a => a.Key == "macd");
            if (macdAxis != null)
                PlotModel.Axes.Remove(macdAxis);

            CrossHairXMacd = null;
        }

        // ---------- Volume sub-panel ----------
        if (showVolume)
        {
            // Always at the very bottom — leaves the MACD/oscillator panels directly above it.
            // Half the height of the oscillator panel (10 % of the chart instead of 20 %).
            // volStart/volEnd are computed at the top of AdjustPanels.

            var existingVol = PlotModel.Axes.FirstOrDefault(a => a.Key == "volume");
            if (existingVol is LinearAxis volAxisLinear)
            {
                volAxisLinear.StartPosition = volStart;
                volAxisLinear.EndPosition = volEnd;
            }
            else
            {
                PlotModel.Axes.Add(new LinearAxis
                {
                    Key = "volume",
                    Title = "Volume",
                    Font = Const.OxyFontName,
                    FontSize = Const.OxyFontSize,
                    TextColor = OxyColors.White,
                    Position = AxisPosition.Right,
                    StartPosition = volStart,
                    EndPosition = volEnd,
                    // Auto-range: volume scale varies wildly per symbol so we let OxyPlot pick.
                    Minimum = 0,
                    // Trim the default 1 % maximum padding so the tallest bar (almost) touches
                    // the top of the sub-panel instead of leaving a visibly empty band there.
                    MinimumPadding = 0,
                    MaximumPadding = 0,
                    IsZoomEnabled = false,
                    IsPanEnabled = false,
                    // Avoid OxyPlot's default scientific notation ("1E+06"). Format the tick
                    // labels with metric suffixes (K / M / B / T) so the scale stays readable
                    // across symbols whose volume ranges from a few hundred to billions.
                    LabelFormatter = FormatVolumeAxisLabel,
                    TicklineColor = OxyColors.Gray,
                    TickStyle = OxyPlot.Axes.TickStyle.Inside,
                    AxislineStyle = LineStyle.Solid,
                    AxislineColor = OxyColors.Gray,
                    AxislineThickness = 1,
                    MajorGridlineStyle = LineStyle.Dot,
                    MajorGridlineColor = OxyColor.FromAColor(80, OxyColors.Gray),
                });
            }

            if (CrossHairXVolume == null)
            {
                CrossHairXVolume = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    Color = OxyColors.White,
                    LineStyle = LineStyle.None,
                    StrokeThickness = 0.5,
                    YAxisKey = "volume",
                    Tag = "crosshair",
                };
                PlotModel.Annotations.Add(CrossHairXVolume);
            }
        }
        else
        {
            // Same defensive cleanup as the stoch branch — orphan every series + annotation
            // pointing at the "volume" axis before the axis itself goes away, so render
            // passes between this point and the trailing Toggle → RemoveFromChart cannot
            // see a series with a dangling YAxis reference (NRE in GetClippingRect).
            foreach (var s in PlotModel.Series.OfType<RectangleBarSeries>().Where(x => x.YAxisKey == "volume").ToList())
                PlotModel.Series.Remove(s);
            foreach (var s in PlotModel.Series.OfType<LineSeries>().Where(x => x.YAxisKey == "volume").ToList())
                PlotModel.Series.Remove(s);
            foreach (var a in PlotModel.Annotations.OfType<LineAnnotation>().Where(x => x.YAxisKey == "volume").ToList())
                PlotModel.Annotations.Remove(a);

            var volAxis = PlotModel.Axes.FirstOrDefault(a => a.Key == "volume");
            if (volAxis != null)
                PlotModel.Axes.Remove(volAxis);

            // The annotation cleanup above already removed the crosshair; just null the field.
            CrossHairXVolume = null;
        }

        // ---------- Price panel height ----------
        // Sub-panels stack from the bottom up; `cursor` holds the top edge of the topmost
        // visible sub-panel. The price panel takes everything above that minus a 2 % gap.
        // Example layouts (showOscillator, showMacd, showVolume):
        //   none       → price 0.00..1.00
        //   stoch only → price 0.22..1.00, stoch  0.00..0.20
        //   vol only   → price 0.12..1.00, volume 0.00..0.10
        //   stoch+vol  → price 0.32..1.00, stoch  0.11..0.30, volume 0.00..0.10
        //   macd only  → price 0.16..1.00, macd   0.00..0.14
        //   all three  → price 0.47..1.00, stoch  0.26..0.45, macd 0.11..0.25, volume 0.00..0.10
        if (cursor > 0)
        {
            priceAxis.StartPosition = cursor + priceGap;
            priceAxis.EndPosition = 1.0;
        }
        else
        {
            priceAxis.StartPosition = 0.0;
            priceAxis.EndPosition = 1.0;
        }
    }

    /// <summary>
    /// LabelFormatter for the volume Y-axis. Returns "850K", "1.5M", "12.4B" etc instead of
    /// OxyPlot's default "1E+06" scientific notation. Sub-1000 values are shown as integers.
    /// Sign is preserved so anyone reusing this on a signed axis still gets a sensible label.
    /// </summary>
    private static string FormatVolumeAxisLabel(double value)
    {
        double absVal = Math.Abs(value);
        string sign = value < 0 ? "-" : "";
        if (absVal >= 1_000_000_000_000d)
            return $"{sign}{absVal / 1_000_000_000_000d:0.##}T";
        if (absVal >= 1_000_000_000d)
            return $"{sign}{absVal / 1_000_000_000d:0.##}B";
        if (absVal >= 1_000_000d)
            return $"{sign}{absVal / 1_000_000d:0.##}M";
        if (absVal >= 1_000d)
            return $"{sign}{absVal / 1_000d:0.##}K";
        return value.ToString("0.##");
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
            model.Title = $"{Session.SymbolBase}{Session.SymbolQuote} {Interval.Name} " +
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

        // Keep panel proportions in sync — oscillator panel is active when stoch OR rsi is
        // enabled, MACD and volume panels each have their own toggle.
        AdjustPanels(Session.ShowStoch || Session.ShowRsi || Session.ShowLux, Session.ShowMacd, Session.ShowVolume);

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

        // Draw SMC zones (Order Blocks). The detector runs synchronously on toggle for every
        // SMC-enabled interval (Settings.Signal.ZonesSmc), results are kept in
        // CryptoSymbolInterval.SmcZones and rendered directly. In the live scanner the same
        // ZoneSmc.Detect is driven by SignalPrepare on the interval boundary.
        group = "smc.zones";
        if (Toggle(model, group, Session.ShowSmcZones))
        {
            foreach (string smcIntervalName in GlobalData.Settings.Signal.ZonesSmc.IntervalList)
            {
                if (GlobalData.IntervalListPeriodName.TryGetValue(smcIntervalName, out CryptoInterval? smcInterval))
                    Core.Zones.ZoneSmc.Detect(Symbol, smcInterval);
            }
            SmcZones.Draw(model, Symbol, Session.MinDate, Session.MaxDate, group);
        }

        // Draw Nadaraya Watson Envelope (non repainting)
        group = "nwe.notrepainting";
        if (Toggle(model, group, Session.ShowNwe))
            Nwe.Draw(model, Symbol, Interval, Session.MinDate, Session.MaxDate, false, group);

        // Draw Nadaraya Watson Envelope (repainting)
        group = "nwe.repainting";
        if (Toggle(model, group, Session.ShowNweRepainting))
            Nwe.Draw(model, Symbol, Interval, Session.MinDate, Session.MaxDate, true, group);

        //// Draw NWE × BB crossover markers — recomputed from the windowed candles via NweBbDetector
        //// (the live strategy's algorithm), so they also show in the emulator where no signals were stored.
        //group = "nwe.bb";
        //if (Toggle(model, group, Session.ShowNweBb))
        //    NweBb.Draw(model, WindowCandleList, Session.MinDate, Session.MaxDate, group);

        // Draw Bollinger Bands
        group = "bb";
        if (Toggle(model, group, Session.ShowBollingerBand))
            Bollingerbands.Draw(model, Symbol, Interval, WindowCandleList, Session.MinDate, Session.MaxDate, group);

        // Draw Keltner Channel
        group = "kc";
        if (Toggle(model, group, Session.ShowKeltnerChannel))
            KeltnerChannel.Draw(model, Symbol, Interval, WindowCandleList, Session.MinDate, Session.MaxDate, group);

        // Draw Baba Bands & Ribbon
        group = "baba";
        if (Toggle(model, group, Session.ShowBabaBands))
            BabaBands.Draw(model, Symbol, Interval, WindowCandleList, Session.MinDate, Session.MaxDate, group);

        // Draw the ACTUAL stored Baba signals (real triggers from the run) — these match the strategy
        // exactly, unlike the recomputed band-break labels above.
        group = "baba.signals";
        if (Toggle(model, group, Session.ShowBabaSignals))
            BabaSignals.Draw(model, SignalList, Interval, Session.MinDate, Session.MaxDate, group);

        // Experimental "glijbaan" (slide) detector overlay — additive, nothing else uses it yet.
        group = "slide";
        if (Toggle(model, group, Session.ShowSlide))
            Slide.Draw(model, WindowCandleList, Session.MinDate, Session.MaxDate, group);

        // Draw PSar
        group = "psar";
        if (Toggle(model, group, Session.ShowPSar))
            PSar.Draw(model, Symbol, Interval, WindowCandleList, Session.MinDate, Session.MaxDate, group);

        // Draw SMA lines
        group = "sma";
        if (Toggle(model, group, Session.ShowSmaLinesSbm))
        {
            Sma.Draw(model, Symbol, Interval, WindowCandleList, 200, OxyColors.Red, Session.MinDate, Session.MaxDate, group);
            Sma.Draw(model, Symbol, Interval, WindowCandleList, 50, OxyColors.Orange, Session.MinDate, Session.MaxDate, group);
            Sma.Draw(model, Symbol, Interval, WindowCandleList, 20, OxyColors.Green, Session.MinDate, Session.MaxDate, group);
        }

        // Draw BBMA
        group = "bbma";
        if (Toggle(model, group, Session.ShowBbma))
            Bbma.Draw(model, Symbol, Interval, WindowCandleList, Session.MinDate, Session.MaxDate, group);


        // Draw Stochastic lines (%K / %D)
        group = "stoch";
        if (Toggle(model, group, Session.ShowStoch))
            Stoch.Draw(model, Symbol, Interval, WindowCandleList, Session.MinDate, Session.MaxDate, group);

        group = "stoch.tresholds";
        if (Toggle(model, group, Session.ShowStoch))
            Stoch.DrawLines(model, group);


        // Draw RSI(14) line
        group = "rsi";
        if (Toggle(model, group, Session.ShowRsi))
            Rsi.Draw(model, Symbol, Interval, WindowCandleList, Session.MinDate, Session.MaxDate, group);

        group = "rsi.tresholds";
        if (Toggle(model, group, Session.ShowRsi))
            Rsi.DrawLines(model, group);

        // Draw the Lux (RSI Multi Length [LuxAlgo], 5m) line in the shared oscillator panel
        group = "lux";
        if (Toggle(model, group, Session.ShowLux))
            Lux.Draw(model, Symbol, Interval, Session.MinDate, Session.MaxDate, group);

        //// Draw Bollinger %B (pink) + band width (red widening / green narrowing) in the oscillator panel
        //group = "bbpercent";
        //if (Toggle(model, group, Session.ShowBbPercent))
        //    Bollingerbands.DrawPercentWidth(model, Symbol, Interval, WindowCandleList, Session.MinDate, Session.MaxDate, group);

        // Draw MACD (line + signal + histogram) in dedicated sub-panel (auto-range "macd" Y axis)
        group = "macd";
        if (Toggle(model, group, Session.ShowMacd))
            Macd.Draw(model, Symbol, Interval, WindowCandleList, Session.MinDate, Session.MaxDate, group);

        group = "macd.tresholds";
        if (Toggle(model, group, Session.ShowMacd))
            Macd.DrawLines(model, group);

        // Draw Volume bars in dedicated sub-panel (auto-range "volume" Y axis)
        group = "volume";
        if (Toggle(model, group, Session.ShowVolume))
            Volume.Draw(model, Symbol, Interval, WindowCandleList, Session.MinDate, Session.MaxDate, group);


        // Other options
        // Draw candles (note: we draw additional candles each minutes if needed)
        group = "candles";
        if (Toggle(model, group, Session.ShowCandles, Session.IntervalName + lastCandleTime.Minutes.ToString()))
            lastCandleTime = Candles.Draw(model, Symbol, Interval, WindowCandleList, Session.MinDate, Session.MaxDate, group);

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
            Positions.Draw(model, Symbol, PositionList, Interval, Session.MinDate, Session.MaxDate, group);


        if (_refreshChart && sender != null)
        {
            model.InvalidatePlot(true);
            OnPropertyChanged(nameof(ChartWindowViewModel.PlotModel));
            OnPropertyChanged(nameof(PlotView));
            _refreshChart = false;
        }
        else
        {
            // Manual invocation (sender == null) from Calculate. Calculate calls
            // InvalidatePlot(true) itself later, BUT if Show() runs before that point the
            // very first OxyPlot layout pass tries to render the freshly-added annotations
            // without their XAxis/YAxis being resolved yet (PlotBase only calls Update when
            // isUpdateRequired is set, which InvalidatePlot would do). Force a non-data
            // model update right here so EnsureAxes runs and every annotation we just added
            // has its axes wired — fixes NRE in PlotElementUtilities.GetClippingRect during
            // ChartWindow.Show() on the initial layout pass.
            ((IPlotModel)model).Update(false);
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

    [RelayCommand]
    private void ZoomLast()
    {
        if (Symbol != null && PlotView.Model != null && WindowCandleList.Count > 0)
        {
            decimal l = decimal.MaxValue;
            decimal h = decimal.MinValue;

            CryptoCandle xfirst;
            CryptoCandle xlast;

            if (WindowStart.HasValue)
            {
                // Explicit window (emulator: a position's lifetime ± margin) → zoom to the whole drawn
                // window [MinDate, MaxDate]; the Y range comes from the candles inside it. Iterate the
                // bounded WindowCandleList (a few hundred candles) instead of the full history, skipping
                // the leading warmup candles (< MinDate) that the list carries for the indicators.
                xfirst = default;
                xlast = default;
                var min = Session.MinDate + (WindowMarginCandles - 30) * Interval.Duration;
                var max = Session.MaxDate - (WindowMarginCandles - 30) * Interval.Duration;

                foreach (var c in WindowCandleList)
                {
                    if (c.OpenTime < min)
                        continue;
                    if (c.OpenTime > max)
                        break;
                    if (xfirst.OpenTime == 0)
                        xfirst = c;
                    xlast = c;
                    if (c.High > h)
                        h = c.High;
                    if (c.Low < l)
                        l = c.Low;
                }
                if (xfirst.OpenTime == 0)
                    return; // no candles in the window
            }
            else
            {
                // Anchor the zoom on the last candle in the window (≤ Session.MaxDate) and walk back
                // CandleCountZoom candles. WindowCandleList is bounded and ends at MaxDate, so its last
                // element is that anchor and we just walk back over the list's tail — no full-history scan.
                CryptoCandle candleLast = WindowCandleList[^1];
                xlast = candleLast;
                xfirst = candleLast;

                int count = GlobalData.Settings.Signal.ZonesDlz.CandleCountZoom;
                for (int i = WindowCandleList.Count - 1; i >= 0 && count > 0; i--, count--)
                {
                    CryptoCandle candle = WindowCandleList[i];
                    if (candle.High > h)
                        h = candle.High;
                    if (candle.Low < l)
                        l = candle.Low;
                    if (candle.Date < xfirst.Date)
                        xfirst = candle;
                }
            }




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

            // When the cursor is outside the plotted data, InverseTransform returns values far outside
            // the axis. (uint)x would then wrap and CandleTime.ToDateTime would overflow — which threw on
            // EVERY mouse move and was logged twice each time (the real cause of the chart feeling slow).
            if (x < 0 || x > MaxAxisMinutes)
                return;

            var symbolInterval = Symbol.GetSymbolInterval(Session.ActiveInterval);
            CandleTime unix = new CandleTime((uint)x) + symbolInterval.Interval.Duration / 2;
            unix = IntervalTools.StartOfIntervalCandle(unix, symbolInterval.Interval.Duration);
            if (unix < 0)
                return;

            try
            {

                // Update crosshair coordinates
                CrossHairX.X = unix.Minutes;
                CrossHairY.Y = y;
                CrossHairX.LineStyle = LineStyle.DashDot;
                CrossHairY.LineStyle = LineStyle.DashDot;

                // Keep the stoch-panel vertical crosshair in sync when the panel is visible.
                if (CrossHairXStoch != null)
                {
                    CrossHairXStoch.X = unix.Minutes;
                    CrossHairXStoch.LineStyle = LineStyle.DashDot;
                }

                // Same for the MACD-panel crosshair.
                if (CrossHairXMacd != null)
                {
                    CrossHairXMacd.X = unix.Minutes;
                    CrossHairXMacd.LineStyle = LineStyle.DashDot;
                }

                // Same for the volume-panel crosshair.
                if (CrossHairXVolume != null)
                {
                    CrossHairXVolume.X = unix.Minutes;
                    CrossHairXVolume.LineStyle = LineStyle.DashDot;
                }

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
                ScannerLog.Logger.Error(error, "Calculate error");
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


    // Candles of context drawn on each side of the position's lifetime when opening from a position.
    private const int WindowMarginCandles = 200;

    // Extra candles loaded BEFORE the visible window purely to warm up the indicators. The longest
    // lookback we draw is SMA(200), so the windowed candle list starts this many candles before
    // MinDate; every indicator value at MinDate is then fully warmed up, identical to computing over
    // the whole history — but without paying for the whole history.
    private const int WindowCalcWarmupCandles = 300;

    /// <summary>
    /// The single windowed candle list every drawer computes AND renders from. Built once per refresh
    /// (BuildWindowCandleList) from [MinDate - warmup .. MaxDate]; ascending by OpenTime. This is the
    /// fix for the chart being unusable on huge histories: indicators (EMA/ATR/BB/SMA/MACD/...) used to
    /// run over the FULL CryptoSymbolInterval.CandleList (tens of thousands of candles) per drawer.
    /// Now they run over this bounded slice instead. NEVER mutate the candles — they are shared with
    /// the live scanner / emulator engine; this list only references them.
    /// </summary>
    private List<CryptoCandle> WindowCandleList { get; } = [];

    /// <summary>
    /// Optional explicit window for the chart. Null (live scanner) → the chart follows the clock.
    /// Set (emulator, opening the chart from a position) → the chart shows WindowStart..WindowEnd (the
    /// position's CreateTime..CloseTime) plus a fixed candle margin on each side, instead of the whole
    /// multi-month run. WindowEnd null (still-open position) falls back to WindowStart.
    /// </summary>
    public DateTime? WindowStart { get; set; }
    public DateTime? WindowEnd { get; set; }

    /// <summary>
    /// Which run's signals/positions to show. Set (emulator, opening the chart from a run's position
    /// grid) → only that EmulatorRun's signals/positions. Null (live scanner) → only live ones
    /// (EmulatorRunId IS NULL). Without this the chart loaded every run's positions for the symbol at
    /// once, which was unreadable. See ExtraData.LoadSignalsForSymbol / LoadPositionsForSymbol.
    /// </summary>
    public int? WindowEmulatorRunId { get; set; }


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

            // Load candles from disk. The live scanner (WindowStart null) reads the full interval
            // history so the chart can show it. The emulator (WindowStart set) does NOT read the whole
            // interval here — only the intervals its run needs are materialised in memory, and pulling
            // the entire history of an extra interval (e.g. a 3m position) on every open is exactly the
            // lag we want to avoid. Instead the visible window is loaded from candles.db further below
            // (LoadWindowCandlesFromDb), bounded to [MinDate - warmup .. MaxDate].
            if (!WindowStart.HasValue)
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

        // Reset the min and maxdate so the refresh draws the new candles and attributes.
        // WindowStart/End (emulator, opening the chart from a position) → the position's lifetime
        // (CreateTime..CloseTime) plus WindowMarginCandles of context on each side. This bounds the
        // drawn candles so a multi-month run's tens of thousands aren't all drawn at once (unusable).
        // Live (WindowStart null) → the normal clock window.
        if (WindowStart.HasValue)
        {
            CandleTime start = CandleTime.AlignFromDateTime(WindowStart.Value, interval.Duration);
            CandleTime end = CandleTime.AlignFromDateTime(WindowEnd ?? WindowStart.Value, interval.Duration);
            Session.MinDate = start - WindowMarginCandles * interval.Duration;
            Session.MaxDate = end + WindowMarginCandles * interval.Duration;
        }
        else
        {
            int candleFetchCount = GlobalData.Settings.Signal.ZonesDlz.CandleCount;
            Session.MaxDate = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, interval.Duration);
            Session.MinDate = Session.MaxDate - candleFetchCount * Interval.Duration;
        }
        //Session.MaxDate += 1 * Interval.Duration; // Allow room for extra candles (we draw the 1m candles there)

        // Emulator: the chart's interval is usually not pre-loaded into memory (only the intervals the
        // run needs are materialised). Load exactly the visible window — including the indicator warmup
        // prefix — from candles.db now that MinDate/MaxDate are known, so candles, pivots and indicators
        // all have data without materialising the whole interval history. Must run before CalculatePivots
        // (in SymbolOrIntervalChangedAsync) so the pivots see these candles too. The live scanner already
        // holds the full history in memory (read above), so this is emulator-only.
        if (WindowStart.HasValue)
            LoadWindowCandlesFromDb(symbol, interval);

        // Load or refresh signals each minute
        var currentTime = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, 1);
        if (lastLoadedSignalsAndPositions.Minutes != currentTime)
        {
            lastLoadedSignalsAndPositions = Session.MaxDate;
            ExtraData.LoadSignalsForSymbol(symbol, Session.MinDate, Session.MaxDate, WindowEmulatorRunId, SignalList);
            optionsInChart.TryAdd("signals", "");
            optionsInChart["signals"] = "";
            ExtraData.LoadPositionsForSymbol(symbol, Session.MinDate, Session.MaxDate, WindowEmulatorRunId, PositionList);
            optionsInChart.TryAdd("positions", "");
            optionsInChart["positions"] = "";
        }

        return (true, "");
    }

    /// <summary>
    /// Rebuild <see cref="WindowCandleList"/> for the current symbol/interval window. Collects the
    /// candles in [MinDate - warmup .. MaxDate] (warmup = WindowCalcWarmupCandles, so SMA(200) and all
    /// shorter indicators are fully warmed up at MinDate). CandleList is a SortedList keyed by OpenTime,
    /// so iteration is ascending and we can stop once we pass MaxDate. Called once per full refresh,
    /// before the drawers run; toggles reuse the list built by the last refresh.
    /// </summary>
    private void BuildWindowCandleList()
    {
        WindowCandleList.Clear();
        if (SymbolInterval == null || Interval == null)
            return;

        uint duration = Interval.Duration;
        CandleTime minDate = Session.MinDate - WindowCalcWarmupCandles * duration;

        // Walk the key range and look each candle up by its OpenTime instead of enumerating the whole
        // CandleList. TryGetValue takes the CryptoCandleList read lock, so this is safe against concurrent
        // writes from the live feed — enumerating .Values is NOT, and threw "collection was modified",
        // which aborted the refresh and left the chart blank (no axes / no candles). It also avoids
        // scanning the full ~50k-candle history just to pick out the few hundred window candles.
        // MinDate (and therefore minDate) is aligned to the interval grid, as are the stored candle
        // OpenTimes, so stepping by Duration hits the exact keys; gaps (missing candles) are skipped.
        for (CandleTime time = minDate; time <= Session.MaxDate; time += duration)
        {
            if (SymbolInterval.CandleList.TryGetValue(time, out CryptoCandle candle))
                WindowCandleList.Add(candle);
        }
    }

    /// <summary>
    /// Emulator-only: materialise the chart's visible window for the current symbol/interval straight
    /// from candles.db into the in-memory CandleList. The emulator only loads the intervals its run
    /// needs, so the chart's interval (e.g. a 3m position) is usually absent; this fills exactly
    /// [MinDate - warmup .. MaxDate] (TryAdd skips anything already present) without materialising the
    /// whole interval history. NEVER mutates existing candles — it only adds missing ones.
    /// </summary>
    private void LoadWindowCandlesFromDb(CryptoSymbol symbol, CryptoInterval interval)
    {
        try
        {
            CandleTime fromDate = Session.MinDate - WindowCalcWarmupCandles * interval.Duration;
            CandleTime toDate = Session.MaxDate;

            using var db = new CandleDatabase(symbol.Exchange);
            db.Open();
            var candles = CandleDatabase.LoadCandlesInRange(db.Connection, symbol, interval, fromDate.Minutes, toDate.Minutes);
            if (candles.Count == 0)
                return;

            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            symbolInterval.CandleList.Lock();
            try
            {
                symbolInterval.LastCandle = default;
                foreach (CryptoCandle candle in candles)
                {
                    symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
                    if (symbolInterval.LastCandle.OpenTime == 0 || candle.OpenTime >= symbolInterval.LastCandle.OpenTime)
                        symbolInterval.LastCandle = candle;
                }
            }
            finally
            {
                symbolInterval.CandleList.Unlock();
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, $"chart window candle load failed for {symbol.Name} {interval.Name}");
            GlobalData.AddTextToLogTab($"chart window candle load failed for {symbol.Name} {interval.Name}: {error.Message}");
        }
    }

    public void HideAnnototionCursor()
    {
        // Hide the crosshair cursor
        if (CrossHairX != null && CrossHairY != null)
        {
            CrossHairX.LineStyle = LineStyle.None;
            CrossHairY.LineStyle = LineStyle.None;
        }
        if (CrossHairXStoch != null)
            CrossHairXStoch.LineStyle = LineStyle.None;
        if (CrossHairXMacd != null)
            CrossHairXMacd.LineStyle = LineStyle.None;
        if (CrossHairXVolume != null)
            CrossHairXVolume.LineStyle = LineStyle.None;
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


            HideAnnototionCursor();

            SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory = [];

            await Symbol.Data.ZoneLock.WaitAsync();
            try
            {
                // Load and (re)calculate the zones
                // Scope to the viewed run (null = live) so the chart only shows that run's zones.
                ZoneDlz.LoadZonesForSymbol(Symbol, WindowEmulatorRunId);

                // Calculate the required zigzag points to draw the FIB and/or Main trend.
                // Skip the exchange fetch when viewing a historical position (WindowStart set): the candles
                // are already loaded and pulling recent candles for an old trade is pointless and slow
                // (it was a network call on every open). The live scanner (WindowStart null) still fetches.
                // CalculatePivots is bounded to [MinDate, MaxDate] (the window), so it stays cheap.
                if (!WindowStart.HasValue)
                    await ZoneDlz.LoadHistoricCandles(Symbol, Interval, loadedCandlesInMemory);
                await ZoneDlz.CalculatePivots(Symbol, Interval, Session.MinDate, Session.MaxDate, TrendZigZagIndicatorList);

                // Force DLZ and FVG zones to be calculated (they are calculated on other intervals).
                // Skipped when viewing a historical position — recomputing zones over the whole history is
                // expensive and not needed just to look at an old trade.
                if (Session.ForceCalculation && !WindowStart.HasValue)
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

                // Build the bounded candle slice the drawers compute + render from (see WindowCandleList).
                // Must run AFTER all candle loading above so the window reflects the final CandleList.
                BuildWindowCandleList();

                // Draw the indicator layers and candles
                DisplayOptionsChanged(null, null!);
                FibSettingsChanged(null, null!);
                TrendSettingsChanged(null, null!);

                // Set the zoom (axis range) BEFORE the first paint. The draws above only flag the plot
                // dirty; the first actual render happens on the next UI yield — the await in the finally
                // below. If ZoomLast ran after that (as it used to), the chart painted once at full
                // auto-scaled range and then a second time zoomed — a visible double build. Running it
                // here, while still synchronous, means that single first paint already has the zoomed range.
                ZoomLast();
            }
            finally
            {
                await ZoneCandleEngine.SaveCandleDataToDiskAsync(Symbol, loadedCandlesInMemory);
                if (!GlobalData.IsEmulatorMode)
                    await ZoneCandleEngine.CleanLoadedCandlesAsync(Symbol);
                Symbol.Data.ZoneLock.Release();
            }

            // No extra InvalidatePlot here: ZoomLast already flagged the plot dirty (with the zoomed
            // axis), so the next UI yield paints it exactly once. A second invalidate would just repaint
            // the identical content.

            // TEMP diagnostic (shows in the emulator Log tab): is the data actually there and drawn?
            // candles=0 → CandleList not loaded; series=0 → nothing drawn; range tells the window used.
            GlobalData.AddTextToLogTab(
                $"Chart {Symbol?.Name} {Interval?.Name}: full={SymbolInterval?.CandleList.Count ?? 0}, " +
                $"window={WindowCandleList.Count}, " +
                $"series={PlotModel?.Series.Count ?? 0}, " +
                $"range {Session.MinDate.ToDateTime():yyyy-MM-dd HH:mm}..{Session.MaxDate.ToDateTime():yyyy-MM-dd HH:mm}");

            WindowTitle = $"{Symbol?.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName}";
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