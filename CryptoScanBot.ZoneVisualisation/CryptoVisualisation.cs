using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;
using CryptoScanBot.Core.Zones;
using CryptoScanBot.ZoneVisualisation.Zones;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;

using System.Diagnostics;
using System.Text;

namespace CryptoScanBot.ZoneVisualisation;

public partial class CryptoVisualisation : Form
{
    private PlotModel? plotModel;
    private LineAnnotation? verticalLine;
    private LineAnnotation? horizontalLine;
    private readonly CryptoZoneSession Session = new();
    private CryptoZoneData Data;
    //private CryptoAccount Account { get; set; }

    private string OldSymbolBase = "";
    private string OldSymbolQuote = "";
    private string OldIntervalName = "";


    public CryptoVisualisation()
    {
        StartPosition = FormStartPosition.CenterParent;
        InitializeComponent();

        if (GlobalData.IntervalList.Count == 0)
            InitializeApplicationStuff();

        Session = CryptoZoneSession.LoadSessionSettings();
        LoadEdits();

        labelInterval.Text = "";
        labelMaxTime.Text = "";
        EditSymbolBase.KeyDown += ButtonKeyDown;
        EditSymbolQuote.KeyDown += ButtonKeyDown;
        EditIntervalName.KeyDown += ButtonKeyDown;
        ButtonCalculate.Click += ButtonCalculateClick;
        ButtonZoomLast.Click += ButtonFocusLastCandlesClick;
        EditDeviation.Click += ButtonFocusLastCandlesClick;
        EditTransparant.Click += TransparentClick;


        //EditSymbolBase.DataSource = new BindingSource(GlobalData.Settings.General.Exchange.SymbolListName, null);
        //EditSymbolBase.DisplayMember = "Key";
        //EditSymbolBase.ValueMember = "Value";
        //try { EditSymbolBase.SelectedValue = Session.SymbolBase; } catch { };

        //EditSymbolQuote.DataSource = new BindingSource(GlobalData.Settings.QuoteCoins, null);
        //EditSymbolQuote.DisplayMember = "Key";
        //EditSymbolQuote.ValueMember = "Value";
        //try { EditSymbolQuote.SelectedValue = Session.SymbolQuote; } catch { };

        //EditIntervalName.DataSource = new BindingSource(GlobalData.IntervalListPeriodName, null);
        //EditIntervalName.DisplayMember = "Key";
        //EditIntervalName.ValueMember = "Value";
        //try { EditIntervalName.SelectedValue = Session.IntervalName; } catch { };
    }


    public static void InitializeApplicationStuff()
    {
        CryptoDatabase.SetDatabaseDefaults();
        GlobalData.LoadExchanges();
        GlobalData.LoadIntervals();
        ApplicationParams.InitApplicationOptions();
        GlobalData.InitializeExchange();
        GlobalData.LoadAccounts();
        GlobalData.Settings.Trading.TradeVia = CryptoAccountType.PaperTrade;
        GlobalData.SetTradingAccounts();
        GlobalData.LoadSymbols();
        ZoneTools.LoadActiveZones();
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
        EditShowLiqBoxes.Checked = Session.ShowLiqBoxes;
        EditZoomLiqBoxes.Checked = Session.ZoomLiqBoxes;
        EditShowZigZag.Checked = Session.ShowLiqZigZag;
        EditDeviation.Value = Session.Deviation;
        EditShowFib.Checked = Session.ShowFib;
        EditShowFibZigZag.Checked = Session.ShowFibZigZag;
        EditShowPivots.Checked = Session.ShowPivots;
        EditShowSecondary.Checked = Session.ShowSecondary;
        EditUseOptimizing.Checked = Session.UseOptimizing;
        EditUseBatchProcess.Checked = Session.UseBatchProcess;


    }

    public void ShowProgress(string text)
    {
        Text = text;
    }

    private void PlotView_MouseMove(object? sender, MouseEventArgs e)
    {
        if (horizontalLine != null && verticalLine != null) // exception on drawing annotations? whats wrong?
        {
            try
            {
                var model = plotView.Model;
                var screenPoint = new ScreenPoint(e.X, e.Y);
                double x = model.Axes[0].InverseTransform(screenPoint.X);
                double y = model.Axes[1].InverseTransform(screenPoint.Y);

                var symbolInterval = Data.Symbol.GetSymbolInterval(Session.ActiveInterval);
                long unix = (long)x + symbolInterval.Interval.Duration / 2;
                unix = IntervalTools.StartOfIntervalCandle(unix, symbolInterval.Interval.Duration); //+ Data.Interval.Duration;

                // Update croshair coordinates
                verticalLine.X = unix;
                horizontalLine.Y = y;

                string s;
                if (symbolInterval.CandleList.TryGetValue(unix, out CryptoCandle? candle))
                {
                    s = $"{candle.Date:yyyy-MM-dd HH:mm}, price: " + y.ToString(Data.Symbol.PriceDisplayFormat);
                    s += " (o: " + candle.Open.ToString(Data.Symbol.PriceDisplayFormat);
                    s += " h: " + candle.High.ToString(Data.Symbol.PriceDisplayFormat);
                    s += " l: " + candle.Low.ToString(Data.Symbol.PriceDisplayFormat);
                    s += " c: " + candle.Close.ToString(Data.Symbol.PriceDisplayFormat);
                    s += " v: " + candle.Volume.ToString0() + ')';
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

        if (Control.ModifierKeys == Keys.Shift && mouseDownPointX != null && mouseDownPointY != null)
        {
            ScreenPoint mouseUpPoint = new(e.X, e.Y);
            // assuming your x-axis is at the bottom and your y-axis is at the left.
            Axis xAxis = plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
            Axis yAxis = plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Right);

            double xstart = xAxis.InverseTransform((double)mouseDownPointX);
            double ystart = yAxis.InverseTransform((double)mouseDownPointY);
            double xend = xAxis.InverseTransform(mouseUpPoint.X);
            double yend = yAxis.InverseTransform(mouseUpPoint.Y);
            double perc = 100 * (yend - ystart) / Math.Min(yend, ystart);
            Text = $"{Session.SymbolBase}{Session.SymbolQuote} {perc:N2}%";


            //var Event = new PolygonAnnotation();

            //Event.Layer = AnnotationLayer.BelowAxes;
            //Event.StrokeThickness = 5;
            //Event.Stroke = OxyColor.FromRgb(0, 0, 255);
            //Event.LineStyle = LineStyle.Automatic;

            //Event.Points.Add(new DataPoint(X, Y));
            //Event.Points.Add(new DataPoint(X, Y));
            //Event.Points.Add(new DataPoint(X, Y));
            //Event.Points.Add(new DataPoint(X, Y));

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

        //if (Control.ModifierKeys != Keys.Shift)
        //{
        //    if (lastRectangle != null)
        //    {
        //        plotModel?.Annotations.Remove(lastRectangle);
        //        lastRectangle = null;
        //        mouseDownPointX = null;
        //    }
        //}
    }

    private double? mouseDownPointX = null;
    private double? mouseDownPointY = null;
    private RectangleAnnotation? lastRectangle = null;


    private void PlotModel_MouseDown(object? sender, OxyMouseDownEventArgs e)
    {
        if (e.ChangedButton == OxyMouseButton.Left && e.IsShiftDown)
        {
            mouseDownPointX = e.Position.X;
            mouseDownPointY = e.Position.Y;
        }
    }

    private void PlotModel_MouseUp(object? sender, OxyMouseEventArgs e)
    {
        if (Control.ModifierKeys != Keys.Shift)
        {
            if (lastRectangle != null)
            {
                plotModel?.Annotations.Remove(lastRectangle);
                lastRectangle = null;
                mouseDownPointX = null;
            }
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
            int count = 150;
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

    private void ButtonGoLeftClick(object sender, EventArgs e)
    {
        if (Data != null && plotModel != null)
        {
            Session.MaxUnix -= Data.Interval.Duration;
            _ = ButtonCalculate_ClickAsync();
        }
    }

    private void ButtonGoRightClick(object sender, EventArgs e)
    {
        if (Data != null && plotModel != null)
        {
            Session.MaxUnix += Data.Interval.Duration;
            _ = ButtonCalculate_ClickAsync();
        }
    }

    private void ButtonMinusClick(object sender, EventArgs e)
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

            CryptoCharting.DrawCandleSerie(plotModel, Data, Session);
            plotModel?.InvalidatePlot(true);
        }
    }

    private void ButtonPlusClick(object sender, EventArgs e)
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

            CryptoCharting.DrawCandleSerie(plotModel, Data, Session);
            labelInterval.Text = Session.ActiveInterval.ToString();
            plotModel?.InvalidatePlot(true);
        }
    }


    private async Task ButtonCalculate_ClickAsync()
    {
        //ScannerLog.Logger.Info("ButtonCalculate_ClickAsync.Start");
        PickupEdits();
        labelInterval.Text = Session.ActiveInterval.ToString();
        labelMaxTime.Text = CandleTools.GetUnixDate(Session.MaxUnix).ToString("dd MMM HH:mm");

        UseWaitCursor = true;
        ButtonZoomLast.Enabled = false;
        ButtonCalculate.Enabled = false;
        Cursor.Current = Cursors.WaitCursor;
        try
        {
            try
            {
                long startTime = Stopwatch.GetTimestamp();
                Text = $"{Data.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Calculating...";
                await CalculateZonesAndPlotZigZagAsync();
                ButtonFocusLastCandlesClick(ShowProgress, EventArgs.Empty);
                Text = $"{Session.SymbolBase}{Session.SymbolQuote} ({Stopwatch.GetElapsedTime(startTime).TotalSeconds} seconds)";

            }
            catch (Exception error)
            {
                GlobalData.AddTextToLogTab(error.ToString());
                Text = $"{Data.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Error {error.Message}";
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
        //ScannerLog.Logger.Info("ButtonCalculate_ClickAsync.Stop " + Stopwatch.GetElapsedTime(startTime).TotalSeconds.ToString());
    }


    private async Task CalculateZonesAndPlotZigZagAsync()
    {
        //long startTime = Stopwatch.GetTimestamp();
        //ScannerLog.Logger.Info("CalculateZonesAndPlotZigZagAsync.Start");
        Text = $"{Data.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName}";
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


            Data.Symbol.CalculatingZones = true;
            try
            {
                // reset data
                CandleEngine.LoadedCandlesInMemory.Clear(); // force loading because we clean them afterwards
                Data.IndicatorFib = new(Data.SymbolInterval.CandleList, true, Session.Deviation, Data.Interval.Duration);
                Data.Indicator = new(Data.SymbolInterval.CandleList, GlobalData.Settings.Signal.Zones.UseHighLow, Session.Deviation, Data.Interval.Duration);

                //
                Data.Indicator.MaxTime = Session.MaxUnix;
                Data.Indicator.ShowSecondary = Session.ShowSecondary;
                Data.Indicator.UseOptimizing = Session.UseOptimizing;
                if (Session.UseBatchProcess)
                    Data.Indicator.StartBatch();

                Data.IndicatorFib.MaxTime = Session.MaxUnix;
                Data.IndicatorFib.ShowSecondary = Session.ShowSecondary;
                Data.IndicatorFib.UseOptimizing = Session.UseOptimizing;
                if (Session.UseBatchProcess)
                    Data.IndicatorFib.StartBatch();


                // calculate zones
                await LiquidityZones.CalculateZonesForSymbolAsync(ShowProgress, Session, Data);
                CryptoTrendIndicator trend = TrendInterval.InterpretZigZagPoints(Data.Indicator, null);
                var best = ZigZagGetBest.CalculateBestIndicator(Data.SymbolInterval);
                //var best = Data.Indicator; 


                // display data
                plotModel = CryptoCharting.CreateChart(Data, out horizontalLine, out verticalLine);
                plotModel.Title = $"{Session.SymbolBase}{Session.SymbolQuote} {Data.Interval.Name} UTC " +
                $"trend={trend} deviation={Data.Indicator.Deviation} (best={best.Deviation}) candles={Data.SymbolInterval.CandleList.Count} pivots={Data.Indicator.ZigZagList.Count}";


                CryptoCharting.DrawCandleSerie(plotModel, Data, Session);

                if (Session.ShowPivots)
                    CryptoCharting.DrawPivots(plotModel, Session, Data.Indicator.PivotList);
                if (Session.ShowLiqZigZag)
                    CryptoCharting.DrawZigZag(plotModel, Session, Data.Indicator.ZigZagList, "liq");
                if (Session.ShowLiqBoxes)
                    CryptoCharting.DrawLiqBoxes(plotModel, Data, Session);

                if (Session.ShowFib)
                    CryptoCharting.DrawFibRetracement(plotModel, Data);
                if (Session.ShowFibZigZag)
                    CryptoCharting.DrawZigZag(plotModel, Session, Data.IndicatorFib.ZigZagList, "fib");


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

                //plotView.MouseDown += PlotView_MouseDown;
                plotView.MouseMove += PlotView_MouseMove;
                //plotView.MouseUp += PlotView_MouseUp;

                plotModel.MouseDown += PlotModel_MouseDown;
                plotModel.MouseUp += PlotModel_MouseUp;

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
            ScannerLog.Logger.Info($"ERROR {e}");
        }
        //ScannerLog.Logger.Info("CalculateZonesAndPlotZigZagAsync.Stop " + Stopwatch.GetElapsedTime(startTime).TotalSeconds.ToString());
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


        if (!exchange.SymbolListName.TryGetValue(Session.SymbolBase + Session.SymbolQuote, out CryptoSymbol symbol))
        {
            reason = "Symbol not found";
            ScannerLog.Logger.Info($"{reason}");
            return false;
        }
        ScannerLog.Logger.Info($"Symbol {symbol.Name}");


        // Is the Interval supported by this tool?
        var interval = GlobalData.IntervalList.Find(x => x.Name.Equals(Session.IntervalName));
        interval ??= GlobalData.IntervalList.Find(x => x.Name.Equals(Session.IntervalName));
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

        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        Data = new()
        {
            Exchange = exchange,
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbolInterval,
            IndicatorFib = new(symbolInterval.CandleList, true, Session.Deviation, symbolInterval.Interval.Duration),
            Indicator = new(symbolInterval.CandleList, GlobalData.Settings.Signal.Zones.UseHighLow, Session.Deviation, symbolInterval.Interval.Duration),
            //AccountSymbolIntervalData = GlobalData.ActiveAccount!.Data.GetSymbolTrendData(symbol.Name, interval.IntervalPeriod),
        };


        if (OldSymbolBase != Session.SymbolBase || OldSymbolQuote != Session.SymbolQuote || OldIntervalName != Session.IntervalName)
        {
            OldSymbolBase = Session.SymbolBase;
            OldSymbolQuote = Session.SymbolQuote;
            OldIntervalName = Session.IntervalName;

            Session.ActiveInterval = Data.Interval.IntervalPeriod;
            Session.MaxUnix = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
            Session.MinUnix = Session.MaxUnix - GlobalData.Settings.Signal.Zones.CandleCount * Data.Interval.Duration;

            labelInterval.Text = Session.ActiveInterval.ToString();
            labelMaxTime.Text = CandleTools.GetUnixDate(Session.MaxUnix).ToString("dd MMM HH:mm");
        }


        reason = "";
        return true;
    }


    public void StartWithSymbolAsync(CryptoSymbol symbol)
    {
        Session.SymbolBase = symbol.Base;
        Session.SymbolQuote = symbol.Quote;
        LoadEdits();
        ButtonCalculateClick(null, EventArgs.Empty);
    }

    private void ButtonKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            ButtonCalculate.Focus();
            ButtonCalculateClick(sender, e);
        }
    }

    private void PickupEdits()
    {
        Session.SymbolBase = EditSymbolBase.Text.ToUpper().Trim();
        Session.SymbolQuote = EditSymbolQuote.Text.ToUpper().Trim();
        Session.IntervalName = EditIntervalName.Text.ToLower().Trim();

        Session.ShowLiqBoxes = EditShowLiqBoxes.Checked;
        Session.ZoomLiqBoxes = EditZoomLiqBoxes.Checked;
        Session.ShowLiqZigZag = EditShowZigZag.Checked;
        Session.Deviation = EditDeviation.Value;
        Session.ShowFib = EditShowFib.Checked;
        Session.ShowFibZigZag = EditShowFibZigZag.Checked;
        Session.ShowSecondary = EditShowSecondary.Checked;
        Session.UseOptimizing = EditUseOptimizing.Checked;
        Session.ShowPivots = EditShowPivots.Checked;
        Session.UseBatchProcess = EditUseBatchProcess.Checked;

    }

    private void ButtonCalculateClick(object? sender, EventArgs e)
    {
        CandleEngine.StartupTime = DateTime.UtcNow;
        PickupEdits();

        Session.ActiveInterval = CryptoIntervalPeriod.interval1h;
        Session.SaveSessionSettings();
        if (PrepareSessionData(out string reason))
            _ = ButtonCalculate_ClickAsync();
        else
            Text = $"{Data?.Exchange?.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Problem preparing {reason}";
    }

}