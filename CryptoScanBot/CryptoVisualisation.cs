using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Experimental;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.WindowsForms;

using System.Text;

namespace CryptoShowTrend;

public partial class CryptoVisualisation : Form
{
    private PlotView? plotView;
    private PlotModel? plotModel;
    private readonly CryptoZoneSession Session = new();
    private CryptoZoneData Data;

    public CryptoVisualisation()
    {
        InitializeComponent();

        //ScannerLog.Logger.Info("Starting app");
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
        ButtonZoomLast.Click += Button1_Click;

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
        EditUseHighLow.Checked = Session.UseHighLow;
        EditShowLiqBoxes.Checked = Session.ShowLiqBoxes;
        EditZoomLiqBoxes.Checked = Session.ZoomLiqBoxes;
        EditShowZigZag.Checked = Session.ShowZigZag;
        EditDeviation.Value = Session.OptimizeZigZag;
        EditCandleCount.Value = GlobalData.Settings.Signal.Zones.CandleCount;
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
        Session.SymbolBase = EditSymbolBase.Text.ToUpper().Trim();
        Session.SymbolQuote = EditSymbolQuote.Text.ToUpper().Trim();
        Session.IntervalName = EditIntervalName.Text.ToLower().Trim();
        Session.UseHighLow = EditUseHighLow.Checked;
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



    private async Task ButtonCalculate_ClickAsync()
    {
        try
        {
            Text = $"{Data.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Calculating...";
            await CalculateAndPlotZigZagAsync();
            Button1_Click(null, EventArgs.Empty);
            Text = $"{Session.SymbolBase}{Session.SymbolQuote}";
        }
        catch (Exception error)
        {
            GlobalData.AddTextToLogTab(error.ToString());
            Text = $"{Data.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName} Error {error.Message}";
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
            Indicator = new(symbolInterval.CandleList, Session.UseHighLow, Session.OptimizeZigZag),
            //Indicator.Deviation = 1; Grrrr
        };
        reason = "";
        return true;
    }



    private async Task CalculateAndPlotZigZagAsync()
    {
        Text = $"{Data.Exchange.Name}.{Session.SymbolBase}{Session.SymbolQuote} {Session.IntervalName}";
        StringBuilder log = new();
        try
        {
            // avoind candles being removed...
            ButtonZoomLast.Enabled = false;
            ButtonCalculate.Enabled = false;
            Cursor.Current = Cursors.WaitCursor;
            Data.Symbol.CalculatingZones = true;
            try
            {
                ScannerLog.Logger.Info($"Start calculating data");
                await LiquidityZones.CalculateSymbolAsync(this, Session, Data);
                CryptoTrendIndicator trend = TrendInterval.InterpretZigZagPoints(Data.Indicator, null);

                var best = ZigZagGetBest.CalculateBestIndicator(Data.SymbolInterval);

                // start from unix date
                ScannerLog.Logger.Info($"Start plotting data");
                long unix = 0;
                if (Data.SymbolInterval.CandleList.Count > GlobalData.Settings.Signal.Zones.CandleCount)
                {
                    var candle = Data.SymbolInterval.CandleList.Values[Data.SymbolInterval.CandleList.Count - GlobalData.Settings.Signal.Zones.CandleCount];
                    unix = candle.OpenTime;
                }
                var lastCandle = Data.SymbolInterval.CandleList.Values.Last();


                if (plotModel != null)
                    Controls.Remove(plotView);
                plotView = new()
                {
                    BackColor = Color.Black,
                    Dock = DockStyle.Fill
                };
                //plotModel.MouseDown += MyPlotModel_MouseDown;
                Controls.Add(plotView);

                CryptoCharting.CreateChart(out plotModel, Data.Symbol);
                plotView.Model = plotModel;
                plotView.Model.InvalidatePlot(true);

                string extraText = Data.Indicator.AddedExtraZigZag1 == null ? "" : "!";
                plotModel.Title = $"{Session.SymbolBase}{Session.SymbolQuote} {Data.Interval.Name} UTC " +
                $"trend={trend} deviation={Data.Indicator.Deviation} (best={best.Deviation}) candles={Data.SymbolInterval.CandleList.Count} pivots={Data.Indicator.ZigZagList.Count} {extraText}";


                CryptoCharting.DrawCandleSerie(plotModel, Data.SymbolInterval, unix);
                if (Session.ShowZigZag)
                    CryptoCharting.DrawZigZagSerie(plotModel, Data.Indicator, unix);
                if (Session.ShowLiqBoxes)
                    CryptoCharting.DrawLiqBoxes(plotModel, Data, unix, lastCandle);

                CryptoCalculation.TrySomethingWithFib();


                plotView.Model = plotModel;
                plotView.Controller = new PlotController();
                //plotView.Controller.UnbindAll();

                plotView.Controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
                plotView.Controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control, PlotCommands.ZoomRectangle);
                plotView.Controller.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.Control | OxyModifierKeys.Alt, 2, PlotCommands.ResetAt);

                plotView.Controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control | OxyModifierKeys.Alt, PlotCommands.ZoomRectangle);
                plotView.Controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Control, 2, PlotCommands.ResetAt);
                plotView.Controller.BindMouseDown(OxyMouseButton.Right, OxyModifierKeys.Alt, PlotCommands.PanAt);


                //plotView.Model.InvalidatePlot(true);
                //plotView.Model.MouseDown += OnChartClick;
                ScannerLog.Logger.Info($"Done plotting data");
            }
            finally
            {
                Data.Symbol.CalculatingZones = false;
                ButtonZoomLast.Enabled = true;
                ButtonCalculate.Enabled = true;
                Cursor.Current = Cursors.Default;
            }
        }
        catch (Exception e)
        {
            log.AppendLine(e.ToString());
            ScannerLog.Logger.Info($"ERROR {e}");
        }
    }

    private void Button1_Click(object? sender, EventArgs e)
    {
        if (Data.SymbolInterval.CandleList.Count > 0)
        {
            decimal l = decimal.MaxValue;
            decimal h = decimal.MinValue;
            CryptoCandle candleLast = Data.SymbolInterval.CandleList.Values.Last();
            long unix = candleLast.OpenTime;
            int count = 200;
            DateTime xfirst = candleLast.Date;
            DateTime xlast = candleLast.Date;
            while (count > 0)
            {
                if (Data.SymbolInterval.CandleList.TryGetValue(unix, out CryptoCandle? candle))
                {
                    if (candle.High > h)
                        h = candle.High;
                    if (candle.Low < l)
                        l = candle.Low;
                    if (candle.Date < xfirst)
                        xfirst = candle.Date;
                }
                unix -= Data.Interval.Duration;
                count--;
            }


            //foreach (var ax in plotModel.Axes)
            //    ax.Maximum = ax.Minimum = Double.NaN;
            //plotModel.ResetAllAxes();

            // X axis
            plotView.ActualModel.Axes[0].Reset();
            plotView.ActualModel.Axes[0].Minimum = DateTimeAxis.ToDouble(xfirst);
            plotView.ActualModel.Axes[0].Maximum = DateTimeAxis.ToDouble(xlast);

            // Y axis
            l = l - 0.02m * l;
            h = h + 0.02m * h;
            plotView.ActualModel.Axes[1].Reset();
            plotView.ActualModel.Axes[1].Minimum = (double)l;
            plotView.ActualModel.Axes[1].Maximum = (double)h;

            plotView.InvalidatePlot(true);

            //foreach (var ax in plotModel.Axes)
            //    ax.Maximum = ax.Minimum = Double.NaN;
            //plotModel.ResetAllAxes();
        }
    }


}
