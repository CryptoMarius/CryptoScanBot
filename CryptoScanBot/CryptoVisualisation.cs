using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;
using CryptoScanBot.Core.Zones;

using OxyPlot;
using OxyPlot.Annotations;

using System.Diagnostics;
using System.Text;

namespace CryptoScanBot;

public partial class CryptoVisualisation : Form
{
    private PlotModel? plotModel;
    private LineAnnotation? verticalLine;
    private LineAnnotation? horizontalLine;
    private readonly CryptoZoneSession Session = new();
    private CryptoZoneData Data;

    public CryptoVisualisation()
    {
        InitializeComponent();

        //CryptoDatabase.SetDatabaseDefaults();
        //GlobalData.LoadExchanges();
        //GlobalData.LoadIntervals();
        //ApplicationParams.InitApplicationOptions();
        //GlobalData.InitializeExchange();
        //GlobalData.LoadAccounts();
        //GlobalData.Settings.Trading.TradeVia = CryptoAccountType.PaperTrade;
        //GlobalData.SetTradingAccounts();
        //GlobalData.LoadSymbols();
        //ZoneTools.LoadActiveZones();

        Session = CryptoZoneSession.LoadSessionSettings();
        LoadEdits();

        EditSymbolBase.KeyDown += ButtonKeyDown;
        EditSymbolQuote.KeyDown += ButtonKeyDown;
        EditIntervalName.KeyDown += ButtonKeyDown;
        ButtonCalculate.Click += ButtonCalculateClick;
        ButtonZoomLast.Click += ButtonFocusLastCandlesClick;

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

        CryptoCandles.LoadedCandlesFrom = ""; // force
        CryptoCandles.LoadedCandlesInMemory.Clear(); // force
    }

    private void LoadEdits()
    {
        EditSymbolBase.Text = Session.SymbolBase;
        EditSymbolQuote.Text = Session.SymbolQuote;
        EditIntervalName.Text = Session.IntervalName;
        EditShowLiqBoxes.Checked = Session.ShowLiqBoxes;
        EditZoomLiqBoxes.Checked = Session.ZoomLiqBoxes;
        EditShowZigZag.Checked = Session.ShowZigZag;
        EditDeviation.Value = Session.OptimizeZigZag;
    }

    internal void BlaAsync(CryptoSymbol symbol)
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

    private void ButtonCalculateClick(object? sender, EventArgs e)
    {
        
        CryptoCandles.StartupTime = DateTime.UtcNow;

        Session.SymbolBase = EditSymbolBase.Text.ToUpper().Trim();
        Session.SymbolQuote = EditSymbolQuote.Text.ToUpper().Trim();
        Session.IntervalName = EditIntervalName.Text.ToLower().Trim();
        Session.ShowLiqBoxes = EditShowLiqBoxes.Checked;
        Session.ZoomLiqBoxes = EditZoomLiqBoxes.Checked;
        Session.ShowZigZag = EditShowZigZag.Checked;
        Session.OptimizeZigZag = EditDeviation.Value;
        Session.SaveSessionSettings();
        if (!PrepareSessionData(out string reason))
        {
            Text = $"{Data?.Exchange?.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Problem preparing {reason}";
        }
        else
            _ = ButtonCalculate_ClickAsync();
    }


    public void ShowProgress(string text)
    {
        Text = text;
    }

    private async Task ButtonCalculate_ClickAsync()
    {
        //ScannerLog.Logger.Info("ButtonCalculate_ClickAsync.Start");

        ButtonZoomLast.Enabled = false;
        ButtonCalculate.Enabled = false;
        Cursor.Current = Cursors.WaitCursor;
        try
        {
            try
            {
                long startTime = Stopwatch.GetTimestamp();
                Text = $"{Data.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Calculating...";
                await CalculateAndPlotZigZagAsync();
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
        }
        //ScannerLog.Logger.Info("ButtonCalculate_ClickAsync.Stop " + Stopwatch.GetElapsedTime(startTime).TotalSeconds.ToString());
    }



    private bool PrepareSessionData(out string reason)
    {
        //long startTime = Stopwatch.GetTimestamp();
        //ScannerLog.Logger.Info("PrepareSessionData.Start");
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
            Indicator = new(symbolInterval.CandleList, GlobalData.Settings.Signal.Zones.UseHighLow, Session.OptimizeZigZag),
        };
        reason = "";
        //ScannerLog.Logger.Info("PrepareSessionData.Stop " + Stopwatch.GetElapsedTime(startTime).TotalSeconds.ToString());
        return true;
    }



    private async Task CalculateAndPlotZigZagAsync()
    {
        //long startTime = Stopwatch.GetTimestamp();
        //ScannerLog.Logger.Info("CalculateAndPlotZigZagAsync.Start");
        Text = $"{Data.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName}";
        StringBuilder log = new();
        try
        {
            // Avoid candles being removed...
            verticalLine = null;
            horizontalLine = null;
            Data.Symbol.CalculatingZones = true;
            try
            {
                //ScannerLog.Logger.Info($"Start calculating data");
                await LiquidityZones.CalculateSymbolAsync(ShowProgress, Session, Data);
                CryptoTrendIndicator trend = TrendInterval.InterpretZigZagPoints(Data.Indicator, null);
                //var best = Data.Indicator; 
                var best = ZigZagGetBest.CalculateBestIndicator(Data.SymbolInterval);

                // start from unix date
                //ScannerLog.Logger.Info($"Start plotting data");
                long unix = 0;
                if (Data.SymbolInterval.CandleList.Count > GlobalData.Settings.Signal.Zones.CandleCount)
                {
                    var candle = Data.SymbolInterval.CandleList.Values[Data.SymbolInterval.CandleList.Count - GlobalData.Settings.Signal.Zones.CandleCount];
                    unix = candle.OpenTime;
                }
                var lastCandle = Data.SymbolInterval.CandleList.Values.Last();


                //if (plotView != null)
                //    Controls.Remove(plotView);
                //plotView = new()
                //{
                //    BackColor = Color.Black,
                //    Dock = DockStyle.Fill
                //};
                //Controls.Add(plotView);

                plotModel = CryptoCharting.CreateChart(Data, out horizontalLine, out verticalLine);
                plotModel.Title = $"{Session.SymbolBase}{Session.SymbolQuote} {Data.Interval.Name} UTC " +
                $"trend={trend} deviation={Data.Indicator.Deviation} (best={best.Deviation}) candles={Data.SymbolInterval.CandleList.Count} pivots={Data.Indicator.ZigZagList.Count}";

                CryptoCharting.DrawCandleSerie(plotModel, Data.SymbolInterval, unix);
                if (Session.ShowZigZag)
                    CryptoCharting.DrawZigZagSerie(Data.Symbol, plotModel, Data.Indicator, unix);
                if (Session.ShowLiqBoxes)
                    CryptoCharting.DrawLiqBoxes(plotModel, Data, unix, lastCandle);

                CryptoCalculation.TrySomethingWithFib();


                plotView.Controller = new PlotController();
                plotView.Controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
                plotView.Controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control, PlotCommands.ZoomRectangle);
                plotView.Controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control | OxyModifierKeys.Alt, 2, PlotCommands.ResetAt);
                plotView.Controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control | OxyModifierKeys.Alt, PlotCommands.ZoomRectangle);
                plotView.Controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control, 2, PlotCommands.ResetAt);
                plotView.Controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Alt, PlotCommands.PanAt);

                //plotView.MouseDown += PlotView_MouseDown;
                plotView.MouseMove += PlotView_MouseMove;
                //plotView.MouseUp += PlotView_MouseUp;
            }
            finally
            {
                await CryptoCandles.CleanLoadedCandlesAsync(Data.Symbol);
                Data.Symbol.CalculatingZones = false;
            }
        }
        catch (Exception e)
        {
            log.AppendLine(e.ToString());
            ScannerLog.Logger.Info($"ERROR {e}");
        }
        //ScannerLog.Logger.Info("CalculateAndPlotZigZagAsync.Stop " + Stopwatch.GetElapsedTime(startTime).TotalSeconds.ToString());
    }

    private void PlotView_MouseMove(object? sender, MouseEventArgs e)
    {
        if (horizontalLine != null && verticalLine != null) // exception on drawing annotations? whats wrong?
        {
            try
            {
                var model = plotView.Model;
                var xAxis = model.Axes[0];
                var yAxis = model.Axes[1];

                var screenPoint = new ScreenPoint(e.X, e.Y);

                double x = xAxis.InverseTransform(screenPoint.X);
                double y = yAxis.InverseTransform(screenPoint.Y);

                long unix = (long)x + Data.Interval.Duration / 2;
                unix = IntervalTools.StartOfIntervalCandle(unix, Data.Interval.Duration); //+ Data.Interval.Duration;

                // Update croshair coordinates
                verticalLine.X = unix;
                horizontalLine.Y = y;
                 
                string s;
                if (Data.SymbolInterval.CandleList.TryGetValue(unix, out CryptoCandle? candle))
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

                plotView.InvalidatePlot(true);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Info("PlotView_MouseMove.Error " + error.ToString());
            }
        }
    }

    private void ButtonFocusLastCandlesClick(object? sender, EventArgs e)
    {
        //plotView!.Model = plotModel;
        //plotModel?.InvalidatePlot(true);
        ////var axis = plotView.ActualModel.Axes[0];
        //return;

        if (Data.SymbolInterval.CandleList.Count > 0)
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

            // X axis
            plotView.ActualModel.Axes[0].Reset();
            plotView.ActualModel.Axes[0].Minimum = xfirst.OpenTime; // + 3600;
            plotView.ActualModel.Axes[0].Maximum = xlast.OpenTime; // + 3600;

            // Y axis
            l -= 0.02m * l;
            h += 0.02m * h;
            plotView.ActualModel.Axes[1].Reset();
            plotView.ActualModel.Axes[1].Minimum = (double)l;
            plotView.ActualModel.Axes[1].Maximum = (double)h;

            plotModel?.InvalidatePlot(true);
        }
    }

}
