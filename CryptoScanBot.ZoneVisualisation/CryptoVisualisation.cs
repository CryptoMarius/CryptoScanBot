using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;
using CryptoScanBot.Core.Zones;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;

using System.Diagnostics;
using System.Text;

namespace CryptoScanBot.ZoneVisualisation;

public delegate object? GetNextObject(object? current, int direction = 1);

public partial class CryptoVisualisation : Form
{
    private PlotModel? plotModel;
    private LineAnnotation? verticalLine;
    private LineAnnotation? horizontalLine;
    private readonly ZoneSession Session = new();
    private ZoneData? Data;
    private bool StandAlone { get; set; } = false;

    private string OldSymbolBase = "";
    private string OldSymbolQuote = "";
    private string OldIntervalName = "";


    private static object? CurrentObject = null;
    public static GetNextObject? GetNextObject = null;


    public CryptoVisualisation()
    {
        StartPosition = FormStartPosition.CenterParent;
        InitializeComponent();

        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        if (GlobalData.IntervalList.Count == 0)
            InitializeStandAloneStuff();

        Session = ZoneSession.LoadSessionSettings();
        Session.UseBatchProcess = true;
        Session.ShowPositions = false;
        LoadEdits();

        labelInterval.Text = "";
        labelMaxTime.Text = "";

        // Subscribe
        Load += FrmMain_Load;
        Closing += Form1Closing;
        ButtonRefresh.Click += ButtonRefreshClick;
        ButtonCalculate.Click += ButtonCalculateClick;
        ButtonZoomLast.Click += ButtonFocusLastCandlesClick;
        EditDeviation.Click += ButtonFocusLastCandlesClick;
        EditTransparant.Click += TransparentClick;
        plotView.MouseMove += PlotView_MouseMove;
        KeyDown += FormKeyDown;
        KeyPreview = true;

        InitQuoteItems();
        InitIntervalItems();

        if (StandAlone)
            ButtonRefreshClick(null, EventArgs.Empty);
    }


    // Display
    public void ShowProgress(string text) => Text = text;

    // Zoom
    private void ButtonPlusClick(object? sender, EventArgs e) => ButtonIntervalPlusOrMin(+1);
    private void ButtonMinusClick(object? sender, EventArgs e) => ButtonIntervalPlusOrMin(-1);

    // Replay
    private void ButtonGoLeftClick(object? sender, EventArgs e) => ButtonGoLeftOrRight(-1);
    private void ButtonGoRightClick(object? sender, EventArgs e) => ButtonGoLeftOrRight(+1);

    // Main stuff
    private void ButtonRefreshClick(object? sender, EventArgs e) => Calculate(false);
    private void ButtonCalculateClick(object? sender, EventArgs e) => Calculate(true);


    // restore window position coordinates & size
    private void FrmMain_Load(object? sender, EventArgs? e)
    {
        Move -= FrmMain_Resize;
        Resize -= FrmMain_Resize;
        ApplicationTools.WindowLocationRestore(this, GlobalData.SettingsUser.ChartForm);
        Move += FrmMain_Resize;
        Resize += FrmMain_Resize;
    }


    // save window position coordinates & size
    private void FrmMain_Resize(object? sender, EventArgs? e)
    {
        if (GlobalData.ApplicationIsClosing || !GlobalData.ApplicationIsShowed)
            return;

        ApplicationTools.SaveWindowLocation(this, GlobalData.SettingsUser.ChartForm);
        GlobalData.SaveUserSettings();
    }


    // Goto next symbol of the initial grid
    private void FormKeyDown(object? sender, KeyEventArgs e)
    {
        if (Data != null && GetNextObject != null)
        {
            if (e.Control && e.Alt && e.KeyCode == Keys.Left)
            {
                object? current = GetNextObject(CurrentObject, -1);
                if (current != null)
                    StartWithSymbolAsync(current);
            }
            if (e.Control && e.Alt && e.KeyCode == Keys.Right)
            {
                object? current = GetNextObject(CurrentObject, +1);
                if (current != null)
                    StartWithSymbolAsync(current);
            }
        }
    }


    public void InitIntervalItems()
    {
        // Question: Why not add only the checked intervals of the zone configuration?
        foreach (var interval in GlobalData.IntervalList)
        {
            if (GlobalData.Settings.General.Exchange!.IsIntervalSupported(interval.IntervalPeriod))
                EditIntervalName.Items.Add(interval.Name);
        }
    }

    private void InitQuoteItems()
    {
        foreach (var quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles)
                EditSymbolQuote.Items.Add(quoteData.Name);
        }
    }

    public void InitializeStandAloneStuff()
    {
        StandAlone = true;
        GlobalData.LoadSettings();
        CryptoDatabase.SetDatabaseDefaults();
        GlobalData.LoadExchanges();
        GlobalData.LoadIntervals();
        ApplicationParams.InitApplicationOptions();
        GlobalData.InitializeExchange();
        GlobalData.LoadAccounts();
        GlobalData.Settings.Trading.TradeVia = CryptoAccountType.PaperTrade;
        GlobalData.SetTradingAccounts();
        GlobalData.LoadSymbols();
        ZoneDlz.LoadAllZones();

        // Saving the zones
        GlobalData.ThreadSaveObjects = new ThreadSaveObjects();
        _ = Task.Run(GlobalData.ThreadSaveObjects!.Execute).ConfigureAwait(false);
    }

    private void Form1Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Unsubscribe
        Load -= FrmMain_Load;
        Closing -= Form1Closing;
        ButtonRefresh.Click -= ButtonRefreshClick;
        ButtonCalculate.Click -= ButtonCalculateClick;
        ButtonZoomLast.Click -= ButtonFocusLastCandlesClick;
        EditDeviation.Click -= ButtonFocusLastCandlesClick;
        EditTransparant.Click -= TransparentClick;
        KeyDown -= FormKeyDown;
        plotView.MouseMove -= PlotView_MouseMove;

        PickupEdits();
        Session.SaveSessionSettings();
    }


    // A weird option to position the form over Altrady graph
    private void TransparentClick(object? sender, EventArgs e)
    {
        if (EditTransparant.Checked)
        {
            BackColor = Color.Lime;
            TransparencyKey = Color.Lime;
            plotView.BackColor = Color.Lime;
        }
        else
        {
            BackColor = SystemColors.Control;
            TransparencyKey = Color.Lime;
            plotView.BackColor = Color.Black;
        }
        flowLayoutPanel1.BackColor = SystemColors.Control;
    }


    private void PlotView_MouseMove(object? sender, MouseEventArgs e)
    {
        if (Data != null && horizontalLine != null && verticalLine != null) // exception on drawing annotations? whats wrong?
        {
            var model = plotView.Model;
            var screenPoint = new ScreenPoint(e.X, e.Y);
            double x = model.Axes[0].InverseTransform(screenPoint.X);
            double y = model.Axes[1].InverseTransform(screenPoint.Y);

            var symbolInterval = Data.Symbol.GetSymbolInterval(Session.ActiveInterval);
            long unix = (long)x + symbolInterval.Interval.Duration / 2;
            unix = IntervalTools.StartOfIntervalCandle(unix, symbolInterval.Interval.Duration); //+ Data.Interval.Duration;
            if (unix < 0)
                return;

            try
            {

                // Update croshair coordinates
                verticalLine.X = unix;
                horizontalLine.Y = y;
                verticalLine.LineStyle = LineStyle.DashDot;
                horizontalLine.LineStyle = LineStyle.DashDot;

                string s;
                if (symbolInterval.CandleList.TryGetValue(unix, out CryptoCandle? candle))
                {
                    s = $"{candle.Date:ddd yyyy-MM-dd HH:mm}, price: " + y.ToString(Data.Symbol.PriceDisplayFormat);
                    s += " (O: " + candle.Open.ToString(Data.Symbol.PriceDisplayFormat);
                    s += " H: " + candle.High.ToString(Data.Symbol.PriceDisplayFormat);
                    s += " L: " + candle.Low.ToString(Data.Symbol.PriceDisplayFormat);
                    s += " C: " + candle.Close.ToString(Data.Symbol.PriceDisplayFormat);
                    s += " V: " + candle.Volume.ToString0() + ')';
                }
                else
                {
                    DateTime date = CandleTools.GetUnixDate(unix); //.ToLocalTime();
                    s = $"{date:yyyy-MM-dd HH:mm}, price: " + y.ToString(Data.Symbol.PriceDisplayFormat);
                }
                //long u = (long)x % Data.Interval.Duration;
                //DateTime date2 = CandleTools.GetUnixDate(unix); //.ToLocalTime();
                //s += "\r\n" + $" x: {x} {unix} {date2} {u}";
                model.Subtitle = s;

                labelInterval.Text = Session.ActiveInterval.ToString();
                plotView.InvalidatePlot(true);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Info("PlotView_MouseMove.Error " + error.ToString());
            }
        }

        //if (IsMeasuring Control.ModifierKeys == Keys.Shift && mouseDownPointX != null && mouseDownPointY != null)
        if (IsMeasuring && mouseDownPointX != null && mouseDownPointY != null)
        {
            ScreenPoint mouseUpPoint = new(e.X, e.Y);
            // assuming your x-axis is at the bottom and your y-axis is at the left.
            Axis? xAxis = plotModel!.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
            Axis? yAxis = plotModel!.Axes.FirstOrDefault(a => a.Position == AxisPosition.Right);
            if (xAxis == null || yAxis == null)
                return;

            double xstart = xAxis.InverseTransform((double)mouseDownPointX);
            double ystart = yAxis.InverseTransform((double)mouseDownPointY);
            double xend = xAxis.InverseTransform(mouseUpPoint.X);
            double yend = yAxis.InverseTransform(mouseUpPoint.Y);
            double perc = 100 * (yend - ystart) / Math.Min(yend, ystart);
            Text = $"{Session.SymbolBase}{Session.SymbolQuote} {perc:N2}%";

            if (lastRectangle != null)
                plotModel.Annotations.Remove(lastRectangle);

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
            plotModel.Annotations.Add(lastRectangle);
        }
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
                plotModel?.Annotations.Remove(lastRectangle);
                lastRectangle = null;
                mouseDownPointX = null;
                e.Handled = true;
            }
            return;
        }
    }


    private void ButtonFocusLastCandlesClick(object? sender, EventArgs e)
    {
        if (Data != null && plotModel != null && Data.SymbolInterval.CandleList.Count > 0)
        {
            decimal l = decimal.MaxValue;
            decimal h = decimal.MinValue;
            CryptoCandle candleLast = Data.SymbolInterval.CandleList.Values.Last();
            long unix = candleLast.OpenTime;
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

            plotView!.Model = plotModel;

            int extra = 5;
            if (Session.ShowFib)
                extra = 25;
            // X axis
            plotView.ActualModel.Axes[0].Reset();
            plotView.ActualModel.Axes[0].Minimum = xfirst.OpenTime - 5 * Data.Interval.Duration;
            plotView.ActualModel.Axes[0].Maximum = xlast.OpenTime + extra * Data.Interval.Duration;

            // Y axis
            l -= 0.02m * l;
            h += 0.02m * h;
            plotView.ActualModel.Axes[1].Reset();
            plotView.ActualModel.Axes[1].Minimum = (double)l;
            plotView.ActualModel.Axes[1].Maximum = (double)h;

            labelInterval.Text = Session.ActiveInterval.ToString();
            plotModel?.InvalidatePlot(true);
        }
    }


    private void ButtonGoLeftOrRight(int direction)
    {
        if (Data != null && plotModel != null)
        {
            Session.MaxDate += direction * Data.Interval.Duration;
            _ = CalculateAsync();
        }
    }


    private async void ButtonIntervalPlusOrMin(int direction)
    {
        if (Data != null && plotModel != null && Session.ActiveInterval > CryptoIntervalPeriod.interval1m && Session.ActiveInterval < CryptoIntervalPeriod.interval1d)
        {
            Session.ActiveInterval += direction;
            foreach (var serie in plotModel.Series)
            {
                if (serie.Title == "Candles")
                {
                    plotModel.Series.Remove(serie);
                    break;
                }
            }

            Data.Symbol.CalculatingZones = true;
            try
            {
                CryptoSymbolInterval symbolInterval = Data.Symbol.GetSymbolInterval(Session.ActiveInterval);
                await ZoneCandleEngine.LoadCandleDataFromDiskAsync(Data.Symbol, symbolInterval.Interval);
                Chart.Candles.Draw(plotModel, Data.Symbol, Data.Interval, Session.MinDate, Session.MaxDate);
            }
            finally
            {
                await ZoneCandleEngine.CleanLoadedCandlesAsync(Data.Symbol);
                Data.Symbol.CalculatingZones = false;
            }

            labelInterval.Text = Session.ActiveInterval.ToString();
            plotModel?.InvalidatePlot(true);
        }
    }



    private bool PrepareSessionData(out string reason)
    {
        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange == null)
        {
            reason = "Exchange not found";
            ScannerLog.Logger.Info($"{reason}");
            return false;
        }
        ScannerLog.Logger.Info($"Exchange {exchange.Name}");


        if (!exchange.SymbolListName.TryGetValue(Session.SymbolBase + Session.SymbolQuote, out CryptoSymbol? symbol))
        {
            reason = "Symbol not found";
            ScannerLog.Logger.Info($"{reason}");
            return false;
        }
        ScannerLog.Logger.Info($"Symbol {symbol.Name}");


        // Is the IntervalList supported by this tool?
        var interval = GlobalData.IntervalList.Find(x => x.Name.Equals(Session.IntervalName));
        if (interval == null)
        {
            reason = "Interval not supported";
            ScannerLog.Logger.Info($"{reason}");
            return false;
        }
        ScannerLog.Logger.Info($"Interval {interval.Name}");

        // Is the Interval supported on the Exchange?
        if (!exchange.IsIntervalSupported(interval.IntervalPeriod))
        {
            reason = "Exchange interval not supported";
            ScannerLog.Logger.Info($"{reason}");
            return false;
        }
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        Data = new()
        {
            Account = GlobalData.ActiveAccount!,
            Exchange = exchange,
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbolInterval,
            IndicatorFib = new(true, Session.Deviation),
            Indicator = new(false, Session.Deviation),
        };


        // reset dates
        if (OldSymbolBase != Session.SymbolBase || OldSymbolQuote != Session.SymbolQuote || OldIntervalName != Session.IntervalName)
        {
            OldSymbolBase = Session.SymbolBase;
            OldSymbolQuote = Session.SymbolQuote;
            OldIntervalName = Session.IntervalName;

            Session.IntervalName = Data.Interval.Name;
            Session.ActiveInterval = Data.Interval.IntervalPeriod;
            Session.MaxDate = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
            Session.MaxDate = IntervalTools.StartOfIntervalCandle(Session.MaxDate, Data.Interval.Duration);
            Session.MinDate = Session.MaxDate - GlobalData.Settings.Signal.ZonesDlz.CandleCount * Data.Interval.Duration;

            labelInterval.Text = Session.ActiveInterval.ToString();
            labelMaxTime.Text = CandleTools.GetUnixDate(Session.MaxDate).ToString("dd MMM HH:mm");
        }


        // signals
        ExtraData.LoadSignalsForSymbol(Data, Session.MinDate);

        // positions
        ExtraData.LoadPositionsForSymbol(Data, Session.MinDate);

        reason = "";
        return true;
    }


    public void StartWithSymbolAsync(object? current)
    {
        CurrentObject = current;
        if (current == null)
            return;

        CryptoSymbol? symbol = null;
        if (current is CryptoSymbol symbolx)
            symbol = symbolx;
        else if (current is CryptoSignal signal)
            symbol = signal.Symbol;
        else if (current is CryptoPosition position)
            symbol = position.Symbol;
        else if (current is CryptoLiveData liveData)
            symbol = liveData.Symbol;
        if (symbol == null)
            return;

        Session.SymbolBase = symbol.Base;
        Session.SymbolQuote = symbol.Quote;
        LoadEdits();
        Calculate(false);
    }


    // Fill the edits with the information from the last session
    private void LoadEdits()
    {
        EditSymbolBase.Text = Session.SymbolBase;
        EditSymbolQuote.Text = Session.SymbolQuote;
        EditIntervalName.Text = Session.IntervalName;
#if DEBUGZIGZAG
        EditShowPositions.Checked = Session.ShowPositions;
        EditUseBatchProcess.Checked = Session.UseBatchProcess;
#else
        PanelPlayBack.Visible = false;
        EditShowPositions.Visible = false;
        EditUseBatchProcess.Visible = false;
#endif
        EditShowLiqBoxes.Checked = Session.ShowLiqBoxes;
        EditZoomLiqBoxes.Checked = Session.ZoomLiqBoxes;
        EditShowZigZag.Checked = Session.ShowLiqZigZag;
        EditDeviation.Value = Session.Deviation;
        EditShowFib.Checked = Session.ShowFib;
        EditShowFibZigZag.Checked = Session.ShowFibZigZag;
        EditShowPivots.Checked = Session.ShowPoints;
        EditShowSecondary.Checked = Session.ShowSecondary;
        EditUseOptimizing.Checked = Session.UseOptimizing;
        EditShowSignals.Checked = Session.ShowSignals;
        EditShowFvgZones.Checked = Session.ShowFvgZones;
        EditShowDtb.Checked = Session.ShowDtb;
    }


    // Save the edits to the session configuration
    private void PickupEdits()
    {
        Session.SymbolBase = EditSymbolBase.Text.ToUpper().Trim();
        Session.SymbolQuote = EditSymbolQuote.Text.ToUpper().Trim();
        Session.IntervalName = EditIntervalName.Text.ToLower().Trim();
#if DEBUGZIGZAG
        Session.ShowPositions = EditShowPositions.Checked;
        Session.UseBatchProcess = EditUseBatchProcess.Checked;
#else
        //Session.IntervalName = "1h";
        Session.ShowPositions = false;
        Session.UseBatchProcess = true;
#endif
        Session.ShowLiqBoxes = EditShowLiqBoxes.Checked;
        Session.ZoomLiqBoxes = EditZoomLiqBoxes.Checked;
        Session.ShowLiqZigZag = EditShowZigZag.Checked;
        Session.Deviation = EditDeviation.Value;
        Session.ShowFib = EditShowFib.Checked;
        Session.ShowFibZigZag = EditShowFibZigZag.Checked;
        Session.ShowSecondary = EditShowSecondary.Checked;
        Session.UseOptimizing = EditUseOptimizing.Checked;
        Session.ShowPoints = EditShowPivots.Checked;
        Session.ShowSignals = EditShowSignals.Checked;
        Session.ShowFvgZones = EditShowFvgZones.Checked;
        Session.ShowDtb = EditShowDtb.Checked;
    }


    private static async Task CalculateAllDlzZonesAsync(AddTextEvent? showProgress, CryptoAccount account, ZoneSession session,
        ZoneData data, SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        if (GlobalData.Settings.Signal.ZonesDlz.ShowSignalsLong || GlobalData.Settings.Signal.ZonesDlz.ShowSignalsShort)
        {
            AccountSymbolData symbolData = account!.Data.GetSymbolData(data.Symbol.Name);
            try
            {
                if (data.Symbol.Exchange.IsIntervalSupported(data.Interval.IntervalPeriod))
                {
                    data.Indicator = new(false, session.Deviation)
                    {
                        MaxTime = session.MaxDate,
                        ShowSecondary = session.ShowSecondary,
                        UseOptimizing = session.UseOptimizing
                    };

                    data.IndicatorFib = new(true, session.Deviation)
                    {
                        MaxTime = session.MaxDate,
                        ShowSecondary = session.ShowSecondary,
                        UseOptimizing = session.UseOptimizing
                    };

                    await ZoneDlz.CalculateDlzZonesAsync(showProgress, session, data, loadedCandlesInMemory);
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Info($"ERROR {error}");
                GlobalData.AddTextToLogTab($"ERROR {error}");
            }
        }
    }

    private async Task CalculateZonesAndPlotZigZagAsync()
    {
        Text = $"{Data!.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName}";
        StringBuilder log = new();
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory = []; // bool = if it needs saving
        try // catch
        {
            // Hide hair cursor
            if (plotModel != null && verticalLine != null && horizontalLine != null)
            {
                //plotModel.Annotations.Remove(verticalLine);
                //plotModel.Annotations.Remove(horizontalLine);
                verticalLine.LineStyle = LineStyle.None;
                horizontalLine.LineStyle = LineStyle.None;
            }
            //verticalLine = null;
            //horizontalLine = null;



            Data.Symbol.CalculatingZones = true;
            try // finally
            {
                // Load and (re)calculate the dlz and fvg zones
                ZoneDlz.LoadZonesForSymbol(Data.Symbol);

                // calculate the FVG
                if (Session.ForceCalculation)
                    await ZoneFvg.CalculateFvgZonesAsync(ShowProgress, Data.Account, Data.Symbol, Data.Interval, loadedCandlesInMemory);

                // calculate the DLZ zones (alway's because of lines)
                await CalculateAllDlzZonesAsync(ShowProgress, Data.Account, Session, Data, loadedCandlesInMemory);

                CryptoTrendIndicator trend = TrendInterval.InterpretZigZagPoints(Data.Indicator, null);


                // display data
                plotModel = Chart.Chart.Create(Data.Symbol, Data.Interval, out horizontalLine, out verticalLine);
                plotModel.Title = $"{Session.SymbolBase}{Session.SymbolQuote} {Data.Interval.Name} UTC " +
                $"{trend} dev={Data.Indicator.Deviation} candles={Data.Indicator.CandleCount} points={Data.Indicator.ZigZagList.Count}";


                // avoid candles being removed...
                await ZoneCandleEngine.LoadCandleDataFromDiskAsync(Data.Symbol, Data.Interval);
                Chart.Candles.Draw(plotModel, Data.Symbol, Data.Interval, Session.MinDate, Session.MaxDate);


                if (Session.ShowDtb) // Test double top/bottom
                    Chart.Dtb.Draw(plotModel, Data.Interval, Data.Indicator);
                if (Session.ShowPoints)
                    Chart.Points.Draw(plotModel, Data.Indicator.PivotList, Session.MinDate, Session.MaxDate);
                if (Session.ShowLiqZigZag)
                    Chart.ZigZag.Draw(plotModel, Data.Indicator.ZigZagList, "liq", OxyColors.White, Session.MinDate, Session.MaxDate);
                if (Session.ShowLiqBoxes)
                    Chart.DlzZones.Draw(plotModel, Data.Symbol, Session.MinDate, Session.MaxDate);
                if (Session.ShowFvgZones)
                    Chart.ChartDrawFvgZones.Draw(plotModel, Data.Symbol, Session.MinDate, Session.MaxDate);
                if (Session.ShowFib)
                    Chart.FibRetracement.Draw(plotModel, Data.Symbol, Data.Interval, Data.IndicatorFib);
                if (Session.ShowFibZigZag)
                    Chart.ZigZag.Draw(plotModel, Data.IndicatorFib.ZigZagList, "fib", OxyColors.White, Session.MinDate, Session.MaxDate);
                if (Session.ShowSignals)
                    Chart.Signals.Draw(plotModel, Data.Signals, Session.MinDate, Session.MaxDate);



                plotView.Controller = new PlotController();
                plotView.Controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
                //plotView.Controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Shift, PlotCommands.ZoomRectangle);
                plotView.Controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control, PlotCommands.ZoomRectangle);
                plotView.Controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control | OxyModifierKeys.Alt, 2, PlotCommands.ResetAt);

                plotView.Controller.UnbindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Shift);

                plotView.Controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control | OxyModifierKeys.Alt, PlotCommands.ZoomRectangle);
                plotView.Controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control, 2, PlotCommands.ResetAt);
                plotView.Controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Alt, PlotCommands.PanAt);
                plotView.Controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Shift, PlotCommands.SnapTrack);

#pragma warning disable CS0618 // Type or member is obsolete
                plotModel.MouseDown += PlotModel_MouseDown; // Declarared obsolete, there is no workaround/new method, kind of ridiculous?
#pragma warning restore CS0618 // Type or member is obsolete

            }
            finally
            {
                await ZoneCandleEngine.SaveCandleDataToDiskAsync(Data.Symbol, loadedCandlesInMemory);
                await ZoneCandleEngine.CleanLoadedCandlesAsync(Data.Symbol);
                Data.Symbol.CalculatingZones = false;
            }
        }
        catch (Exception e)
        {
            log.AppendLine(e.ToString());
            ScannerLog.Logger.Error($"ERROR {e}");
            //await CandleEngine.SaveCandleDataToDiskAsync(Data.Symbol, loadedCandlesInMemory);
            //await CandleEngine.CleanLoadedCandlesAsync(Data.Symbol);
        }
    }


    private async Task CalculateAsync()
    {
        PickupEdits();
        labelInterval.Text = Session.ActiveInterval.ToString();
        labelMaxTime.Text = CandleTools.GetUnixDate(Session.MaxDate).ToString("dd MMM HH:mm");

        UseWaitCursor = true;
        ButtonZoomLast.Enabled = false;
        ButtonCalculate.Enabled = false;
        Cursor.Current = Cursors.WaitCursor;
        try
        {
            try
            {
                long startTime = Stopwatch.GetTimestamp();
                Text = $"{Data!.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Calculating...";
                await CalculateZonesAndPlotZigZagAsync();
                ButtonFocusLastCandlesClick(null, EventArgs.Empty);
                Text = $"{Session.SymbolBase}{Session.SymbolQuote} ({Stopwatch.GetElapsedTime(startTime).TotalSeconds} seconds)";

            }
            catch (Exception error)
            {
                GlobalData.AddTextToLogTab(error.ToString());
                Text = $"{Data!.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Error {error.Message}";
                ScannerLog.Logger.Info("ButtonCalculate_ClickAsync.Error " + Text);
            }
        }
        finally
        {
            ButtonZoomLast.Enabled = true;
            ButtonCalculate.Enabled = true;
            Cursor.Current = Cursors.Default;
            UseWaitCursor = false;
        }
    }


    private async void Calculate(bool forceCalculation)
    {
        try
        {
            PickupEdits();
            CryptoInterval interval = GlobalData.IntervalListPeriodName[Session.IntervalName];
            Session.ActiveInterval = interval.IntervalPeriod;
            Session.ForceCalculation = forceCalculation;
            Session.SaveSessionSettings();

            if (PrepareSessionData(out string reason))
                await CalculateAsync();
            else
                Text = $"{Data?.Exchange?.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Problem preparing {reason}";
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
        }
    }


}