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
    private readonly CryptoZoneSession Session = new();
    private CryptoZoneData? Data;
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
        {
            StandAlone = true;
            InitializeApplicationStuff();
        }

        Session = CryptoZoneSession.LoadSessionSettings();
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


    private void FrmMain_Load(object? sender, EventArgs? e)
    {
        Move -= FrmMain_Resize;
        Resize -= FrmMain_Resize;
        ApplicationTools.WindowLocationRestore(this, GlobalData.SettingsUser.ChartForm);
        Move += FrmMain_Resize;
        Resize += FrmMain_Resize;
    }


    private void FrmMain_Resize(object? sender, EventArgs? e)
    {
        if (GlobalData.ApplicationIsClosing || !GlobalData.ApplicationIsShowed)
            return;

        ApplicationTools.SaveWindowLocation(this, GlobalData.SettingsUser.ChartForm);
        GlobalData.SaveUserSettings();
    }


    private void ButtonRefreshClick(object? sender, EventArgs e)
    {
        ButtonCalculateClick(null, EventArgs.Empty);
    }


    public void InitIntervalItems()
    {
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

    public static void InitializeApplicationStuff()
    {
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
        LiquidityZones.LoadAllZones();

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
    }

    public void ShowProgress(string text)
    {
        Text = text;
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
        //plotView!.Model = plotModel;
        //plotModel?.InvalidatePlot(true);
        ////var axis = plotView.ActualModel.Axes[0];
        //return;

        if (Data != null && plotModel != null && Data.SymbolInterval.CandleList.Count > 0)
        {
            decimal l = decimal.MaxValue;
            decimal h = decimal.MinValue;
            CryptoCandle candleLast = Data.SymbolInterval.CandleList.Values.Last();
            long unix = candleLast.OpenTime;
            int count = 100;
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


    private void ButtonGoLeftClick(object? sender, EventArgs e)
    {
        if (Data != null && plotModel != null)
        {
            Session.MaxDate -= Data.Interval.Duration;
            _ = ButtonCalculate_ClickAsync();
        }
    }


    private void ButtonGoRightClick(object? sender, EventArgs e)
    {
        if (Data != null && plotModel != null)
        {
            Session.MaxDate += Data.Interval.Duration;
            _ = ButtonCalculate_ClickAsync();
        }
    }


    private async void ButtonMinusClick(object? sender, EventArgs e)
    {
        if (Data != null && plotModel != null && Session.ActiveInterval > CryptoIntervalPeriod.interval1m)
        {
            Session.ActiveInterval -= 1;
            labelInterval.Text = Session.ActiveInterval.ToString();
            foreach (var serie in plotModel.Series)
            {
                if (serie.Title == "Candles")
                {
                    plotModel.Series.Remove(serie);
                    break;
                }
            }

            //CryptoCharting.DrawCandleSerie(plotModel, Data, Session);
            Data.Symbol.CalculatingZones = true;
            try
            {
                CryptoSymbolInterval symbolInterval = Data.Symbol.GetSymbolInterval(Session.ActiveInterval);
                await CandleEngine.LoadCandleDataFromDiskAsync(Data.Symbol, symbolInterval.Interval);
                CryptoCharting.DrawCandleSerie(plotModel, Data, Session);
            }
            finally
            {
                await CandleEngine.CleanLoadedCandlesAsync(Data.Symbol);
                Data.Symbol.CalculatingZones = false;
            }

            plotModel?.InvalidatePlot(true);
        }
    }


    private async void ButtonPlusClick(object? sender, EventArgs e)
    {
        if (Data != null && plotModel != null && Session.ActiveInterval < CryptoIntervalPeriod.interval1d)
        {
            Session.ActiveInterval += 1;
            foreach (var serie in plotModel.Series)
            {
                if (serie.Title == "Candles")
                {
                    plotModel.Series.Remove(serie);
                    break;
                }
            }

            //CryptoCharting.DrawCandleSerie(plotModel, Data, Session);
            Data.Symbol.CalculatingZones = true;
            try
            {
                CryptoSymbolInterval symbolInterval = Data.Symbol.GetSymbolInterval(Session.ActiveInterval);
                await CandleEngine.LoadCandleDataFromDiskAsync(Data.Symbol, symbolInterval.Interval);
                CryptoCharting.DrawCandleSerie(plotModel, Data, Session);
            }
            finally
            {
                await CandleEngine.CleanLoadedCandlesAsync(Data.Symbol);
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


        // Is the Interval supported by this tool?
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

            Session.ActiveInterval = Data.Interval.IntervalPeriod;
            Session.MaxDate = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
            Session.MaxDate = IntervalTools.StartOfIntervalCandle(Session.MaxDate, Data.Interval.Duration);
            Session.MinDate = Session.MaxDate - GlobalData.Settings.Signal.Zones.CandleCount * Data.Interval.Duration;

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

        CryptoSymbol? symbol = GetSymbolFrom(current);
        if (symbol == null)
            return;

        Session.SymbolBase = symbol.Base;
        Session.SymbolQuote = symbol.Quote;
        LoadEdits();
        ButtonCalculateClick(null, EventArgs.Empty);
    }


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
    }


    private async Task CalculateZonesAndPlotZigZagAsync()
    {
        Text = $"{Data!.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName}";
        StringBuilder log = new();
        try
        {
            // Avoid candles being removed...
            if (plotModel != null)
            {
                plotModel.Annotations.Remove(verticalLine);
                plotModel.Annotations.Remove(horizontalLine);
            }
            verticalLine = null;
            horizontalLine = null;

            // Can we combine the account data (used in the scanner) and the visualisation?
            // Couple of problems:
            // - Amount of candles for indicator must be a lot higher (so it needs calculation anyway)
            // - The Deviation is fixed in the visualisation while it is calculated right now (that's is okay)
            // - For the visualisation we only need the 1h (but that has no impact)
            // - For the visualisation we use a MinDate and MaxDate
            // Conclusion, we cannot use the AccountSymbolData shared with the sacanner.
            // However, we can reuse the AccountSymbolData class and calculate our own..
            //AccountSymbolData accountScannerSymbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(Data.Symbol.Name);
            //AccountSymbolIntervalData accountScannerSymbolIntervalData = accountScannerSymbolData.GetAccountSymbolInterval(Data.Interval.IntervalPeriod);


            Data.Symbol.CalculatingZones = true;
            try
            {
                // reset data
                Data.Indicator = new(false, Session.Deviation)
                {
                    MaxTime = Session.MaxDate,
                    ShowSecondary = Session.ShowSecondary,
                    UseOptimizing = Session.UseOptimizing
                };

                Data.IndicatorFib = new(true, Session.Deviation)
                {
                    MaxTime = Session.MaxDate,
                    ShowSecondary = Session.ShowSecondary,
                    UseOptimizing = Session.UseOptimizing
                };


                // Load and (re)calculate the zones
                LiquidityZones.LoadZonesForSymbol(Data.Symbol.Id, Data);
                SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory = []; // bool = if it needs saving
                await LiquidityZones.CalculateZonesForSymbolAsync(ShowProgress, Session, Data, loadedCandlesInMemory);
                CryptoTrendIndicator trend = TrendInterval.InterpretZigZagPoints(Data.Indicator, null);


                // display data
                plotModel = CryptoCharting.CreateChart(Data, out horizontalLine, out verticalLine);
                plotModel.Title = $"{Session.SymbolBase}{Session.SymbolQuote} {Data.Interval.Name} UTC " +
                $"{trend} dev={Data.Indicator.Deviation} candles={Data.Indicator.CandleCount} points={Data.Indicator.ZigZagList.Count}";


                // avoid candles being removed...
                Data.Symbol.CalculatingZones = true;
                try
                {
                    await CandleEngine.LoadCandleDataFromDiskAsync(Data.Symbol, Data.Interval);
                    CryptoCharting.DrawCandleSerie(plotModel, Data, Session);
                }
                finally
                {
                    await CandleEngine.CleanLoadedCandlesAsync(Data.Symbol);
                    Data.Symbol.CalculatingZones = false;
                }

                if (Session.ShowPoints)
                    CryptoCharting.DrawPoints(plotModel, Session, Data.Indicator.PivotList);
                if (Session.ShowLiqZigZag)
                    CryptoCharting.DrawZigZag(plotModel, Session, Data.Indicator.ZigZagList, "liq", OxyColors.White);
                if (Session.ShowLiqBoxes)
                    CryptoCharting.DrawLiqBoxes(plotModel, Data, Session);
                if (Session.ShowFvgZones)
                    CryptoCharting.DrawFvgBoxes(plotModel, Data, Session);
                if (Session.ShowFib)
                    CryptoCharting.DrawFibRetracement(plotModel, Data);
                if (Session.ShowFibZigZag)
                    CryptoCharting.DrawZigZag(plotModel, Session, Data.IndicatorFib.ZigZagList, "fib", OxyColors.White);
                if (Session.ShowSignals)
                    CryptoCharting.DrawSignals(plotModel, Session, Data.Signals);


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
                await CandleEngine.CleanLoadedCandlesAsync(Data.Symbol);
                Data.Symbol.CalculatingZones = false;
            }
        }
        catch (Exception e)
        {
            log.AppendLine(e.ToString());
            ScannerLog.Logger.Error($"ERROR {e}");
        }
    }


    private async Task ButtonCalculate_ClickAsync()
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


    private void ButtonCalculateClick(object? sender, EventArgs e)
    {
        try
        {
            PickupEdits();

            Session.ForceCalculation = sender != null;
            Session.ActiveInterval = CryptoIntervalPeriod.interval1h;
            Session.SaveSessionSettings();
            if (PrepareSessionData(out string reason))
                _ = ButtonCalculate_ClickAsync();
            else
                Text = $"{Data?.Exchange?.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Problem preparing {reason}";
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
        }
    }


    public static CryptoSymbol? GetSymbolFrom(object? current)
    {
        if (current != null)
        {
            if (current is CryptoSymbol symbol)
                return symbol;
            if (current is CryptoSignal signal)
                return signal.Symbol;
            if (current is CryptoPosition position)
                return position.Symbol;
            if (current is CryptoLiveData liveData)
                return liveData.Symbol;
       }
       return null;
    }

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

}