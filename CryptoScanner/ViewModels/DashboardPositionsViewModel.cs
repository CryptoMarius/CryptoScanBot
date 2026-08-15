using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;

using Dapper;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

using System.Collections.ObjectModel;
using System.Text;

using Const = CryptoScanner.Chart.ViewModels.Chart.Const;

namespace CryptoScanner.ViewModels;

public partial class DashboardPositionsViewModel : ObservableObject
{
    public class QueryPositionData
    {
        public DateTime? CloseTime { get; set; } = null;
        public string Quote { get; set; } = "";
        //public CryptoOrderStatus Status { get; set; }

        public int Positions { get; set; }
        public decimal Invested { get; set; }
        public decimal Returned { get; set; }
        public decimal Commission { get; set; }
        public decimal TotalProfit { get; set; }

        public decimal MinMin { get; set; }
        public decimal AvgMin { get; set; }
        public decimal MaxMin { get; set; }

        public decimal MinPerc { get; set; }
        public decimal AvgPerc { get; set; }
        public decimal MaxPerc { get; set; }

        // Formatted properties voor display
        public string InvestedFormatted => Invested.ToString("N2");
        public string ReturnedFormatted => Returned.ToString("N2");
        public string CommissionFormatted => Commission.ToString("N2");
        public string TotalProfitFormatted => TotalProfit.ToString("N2");

        // Average profit per position (simple division)
        public decimal AverageProfit => Positions > 0 ? TotalProfit / Positions : 0m;

        // Decimal-aligned display: the whole part is right-aligned up to the decimal separator,
        // the fraction part (including the separator) is left-aligned after it. This way a plain
        // count (no fraction) lines up with the values on the comma.
        public string PositionsWhole => Positions.ToString("N0");
        public string InvestedWhole => SplitNumber(Invested, "N2").Whole;
        public string InvestedFraction => SplitNumber(Invested, "N2").Fraction;
        public string ReturnedWhole => SplitNumber(Returned, "N2").Whole;
        public string ReturnedFraction => SplitNumber(Returned, "N2").Fraction;
        public string CommissionWhole => SplitNumber(Commission, "N2").Whole;
        public string CommissionFraction => SplitNumber(Commission, "N2").Fraction;
        public string TotalProfitWhole => SplitNumber(TotalProfit, "N2").Whole;
        public string TotalProfitFraction => SplitNumber(TotalProfit, "N2").Fraction;
        public string AverageProfitWhole => SplitNumber(AverageProfit, "N2").Whole;
        public string AverageProfitFraction => SplitNumber(AverageProfit, "N2").Fraction;

        //private double offset = 0.4; // unused, kept as a note of the intended bar offset

        // Splits a formatted number into a whole part and a fraction part (including the separator).
        public static (string Whole, string Fraction) SplitNumber(decimal value, string format)
        {
            string text = value.ToString(format);
            string separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            int index = text.IndexOf(separator, StringComparison.Ordinal);
            return index < 0 ? (text, "") : (text[..index], text[index..]);
        }
    }

    public class QueryTradeData
    {
        public DateTime TradeTime { get; set; }
        public CryptoOrderSide Side { get; set; }
        public string Quote { get; set; } = "";
        public decimal Value { get; set; }
    }

    [ObservableProperty]
    private ObservableCollection<string> _quoteOptions = [];

    [ObservableProperty]
    private string _selectedQuote = "";

    [ObservableProperty]
    private CryptoQuoteData? _quoteData;

    [ObservableProperty]
    private QueryPositionData _openData = new();

    [ObservableProperty]
    private QueryPositionData _closedData = new();

    [ObservableProperty]
    private QueryPositionData _totalData = new();

    [ObservableProperty]
    private string _nettoPnlWhole = "0";

    [ObservableProperty]
    private string _nettoPnlFraction = "";

    [ObservableProperty]
    private string _currentValueWhole = "0";

    [ObservableProperty]
    private string _currentValueFraction = "";

    [ObservableProperty]
    private string _virtualProfitWhole = "0";

    [ObservableProperty]
    private string _virtualProfitFraction = "";

    [ObservableProperty]
    private string _virtualProfitPercentageWhole = "0";

    [ObservableProperty]
    private string _virtualProfitPercentageFraction = "";

    [ObservableProperty]
    private string _closedProfitPercentageWhole = "";

    [ObservableProperty]
    private string _closedProfitPercentageFraction = "";

    [ObservableProperty]
    private string _totalProfitPercentageWhole = "";

    [ObservableProperty]
    private string _totalProfitPercentageFraction = "";

    [ObservableProperty]
    private PlotModel? _chartPositionsPerDay;

    [ObservableProperty]
    private PlotModel? _chartProfitsPerDay;

    [ObservableProperty]
    private PlotModel? _chartProfitPercentagePerDay;

    [ObservableProperty]
    private PlotModel? _chartInvestedReturnedPerDay;

    [ObservableProperty]
    private PlotModel? _chartDoorlooptijden;

    private List<QueryPositionData> QueryPositionDataList = [];

    public DashboardPositionsViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(RefreshInformationAsync);

        InitializeQuoteOptions();

        WeakReferenceMessenger.Default.Register<SymbolsHaveChangedMessage>(this, OnSymbolsHaveChanged);
        WeakReferenceMessenger.Default.Register<ExchangeSwitchedMessage>(this, OnExchangeSwitched);
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<SymbolsHaveChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ExchangeSwitchedMessage>(this);
    }

    private void OnSymbolsHaveChanged(object recipient, SymbolsHaveChangedMessage message)
    {
        // Refresh quote options after GetSymbolsAsync() or exchange switch
        InitializeQuoteOptions();
    }

    private void OnExchangeSwitched(object recipient, ExchangeSwitchedMessage message)
    {
        // Reinitialize quote options for the new exchange
        InitializeQuoteOptions();

        // Switch to the default quote of the new exchange if it is available
        string? defaultQuote = ExchangeBase.ExchangeOptions.DefaultQuote;
        if (!string.IsNullOrEmpty(defaultQuote) && QuoteOptions.Contains(defaultQuote))
            SelectedQuote = defaultQuote;

        // Clear stale statistics from the previous exchange
        ResetDashboard();
    }

    private void ResetDashboard()
    {
        // Clear position data
        QueryPositionDataList.Clear();
        OpenData = new QueryPositionData();
        ClosedData = new QueryPositionData();
        TotalData = new QueryPositionData();
        QuoteData = null;

        // Reset summary labels
        NettoPnlWhole = "0";
        NettoPnlFraction = "";
        CurrentValueWhole = "0";
        CurrentValueFraction = "";
        VirtualProfitWhole = "0";
        VirtualProfitFraction = "";
        VirtualProfitPercentageWhole = "0";
        VirtualProfitPercentageFraction = "";
        ClosedProfitPercentageWhole = "";
        ClosedProfitPercentageFraction = "";
        TotalProfitPercentageWhole = "";
        TotalProfitPercentageFraction = "";

        // Clear charts
        ChartPositionsPerDay = null;
        ChartProfitsPerDay = null;
        ChartProfitPercentagePerDay = null;
        ChartInvestedReturnedPerDay = null;
        ChartDoorlooptijden = null;
    }

    private void InitializeQuoteOptions()
    {
        // Add the active quotes (default=usdt)
        List<string> quotes = [];
        foreach (CryptoQuoteData cryptoQuoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (cryptoQuoteData.FetchCandles && cryptoQuoteData.SymbolList.Count > 0)
                quotes.Add(cryptoQuoteData.Name);
        }
        if (quotes.Count == 0)
            quotes.Add("USDT");
        QuoteOptions = new ObservableCollection<string>(quotes);

        // Keep the selected quote if it is still valid, otherwise fall back to the first option
        if (!quotes.Contains(SelectedQuote))
            SelectedQuote = quotes[0];
    }

    partial void OnSelectedQuoteChanged(string value)
    {
        _ = RefreshInformationAsync();
    }

    public IAsyncRelayCommand RefreshCommand { get; }

    private async Task RefreshInformationAsync()
    {
        if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Running)
            return;

        try
        {
            await Task.Run(() =>
            {
                GetQueryQuoteData();

                if (!GlobalData.Settings.QuoteCoins.TryGetValue(SelectedQuote, out var quoteData))
                    return;

                QuoteData = quoteData;
                GetQueryTradeData();
                DoAllCharts();
                DoAdditionalData();
            });
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "Dashboard refresh error");
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }

    private void GetQueryQuoteData()
    {
        StringBuilder builder = new();
        builder.AppendLine("select symbol.quote, count(symbol.quote)");
        builder.AppendLine("from PositionStep");
        builder.AppendLine("inner join position on Position.Id = positionStep.PositionId");
        builder.AppendLine("inner join symbol on Position.symbolid = symbol.id");
        builder.AppendLine("where PositionStep.status in (1, 2)");
        builder.AppendLine("and position.Invested > 0");
        builder.AppendLine("and position.Status in (0,1,2,3)");
        builder.AppendLine("group by symbol.quote");
        builder.AppendLine("order by count(symbol.quote) desc");

        using CryptoDatabase databaseThread = new();
        databaseThread.Open();

        foreach (QueryTradeData data in databaseThread.Connection.Query<QueryTradeData>(builder.ToString()))
        {
            if (!QuoteOptions.Contains(data.Quote))
                QuoteOptions.Add(data.Quote);
        }
        if (QuoteOptions.Count == 0)
            QuoteOptions.Add("USDT");
    }

    private void GetQueryTradeData()
    {
        // Query voor positie data
        StringBuilder builder = new();
        builder.AppendLine("select date(position.CloseTime,'localtime') as CloseTime,");
        builder.AppendLine("symbol.quote, count(position.id) as Positions,");
        builder.AppendLine("round(MIN(ROUND((JULIANDAY(position.CloseTime) - JULIANDAY(position.CreateTime)) * 86400 / 3600)), 2) AS MinMin,");
        builder.AppendLine("round(AVG(ROUND((JULIANDAY(position.CloseTime) - JULIANDAY(position.CreateTime)) * 86400 / 3600)), 2) AS AvgMin,");
        builder.AppendLine("round(MAX(ROUND((JULIANDAY(position.CloseTime) - JULIANDAY(position.CreateTime)) * 86400 / 3600)), 2) AS MaxMin,");
        builder.AppendLine("sum(position.Invested) as Invested,");
        builder.AppendLine("sum(position.Returned) as Returned,");
        builder.AppendLine("sum(position.Commission) as Commission,");
        builder.AppendLine("sum(position.Profit) as TotalProfit,");
        builder.AppendLine("min(position.Percentage) as MinPerc,");
        builder.AppendLine("avg(position.Percentage) as AvgPerc,");
        builder.AppendLine("max(position.Percentage) as MaxPerc");
        builder.AppendLine("from Position");
        builder.AppendLine("inner join symbol on Position.symbolid = symbol.id");
        builder.AppendLine("where position.Invested > 0");
        builder.AppendLine("and position.Status in (0,1,2,3)");
        builder.AppendLine($"and symbol.quote = '{QuoteData!.Name}'");
        builder.AppendLine("group by date(position.CloseTime,'localtime'), symbol.quote");
        builder.AppendLine("order by date(position.CloseTime,'localtime'), position.Status, symbol.quote");

        using CryptoDatabase databaseThread = new();
        databaseThread.Open();

        QueryPositionDataList.Clear();
        var openData = new QueryPositionData();
        var closedData = new QueryPositionData();

        foreach (QueryPositionData data in databaseThread.Connection.Query<QueryPositionData>(builder.ToString()))
        {
            if (data.CloseTime.HasValue)
            {
                QueryPositionDataList.Add(data);
                closedData.Positions += data.Positions;
                closedData.Invested += data.Invested;
                closedData.Returned += data.Returned;
                closedData.Commission += data.Commission;
                closedData.TotalProfit += data.TotalProfit;
                // etc..
            }
            else
            {
                openData = data; // what remains
                // verschil vanwege meerdere quotes
                //openData.Positions += data.Positions;
                //openData.Invested += data.Invested;
                //openData.Returned += data.Returned;
                //openData.Commission += data.Commission;
            }
        }

        OpenData = openData;
        ClosedData = closedData;
    }

    private void DoAllCharts()
    {
        ChartPositionsPerDay = CreateChartPositionsPerDay();
        ChartProfitPercentagePerDay = CreateChartProfitPercentagePerDay();
        ChartProfitsPerDay = CreateChartProfitsPerDay();
        ChartInvestedReturnedPerDay = CreateChartInvestedReturnedPerDay();
        ChartDoorlooptijden = CreateChartDoorlooptijden();
    }

    private PlotModel CreateChartPositionsPerDay()
    {
        var model = new PlotModel
        {
            Title = "Positions per day",
            TextColor = Const.ChartTextColor,
            Background = Const.ChartSurfaceColor
        };

        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(80, 255, 255, 255),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromArgb(40, 255, 255, 255),
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = Const.ChartTextColor,
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            //Title = "Count",
            Minimum = 0,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(80, 255, 255, 255),
            AxislineColor = OxyColors.White,
            TextColor = Const.ChartTextColor,
            AxislineStyle = LineStyle.Solid,
            StringFormat = "N0"
        });

        // OxyPlot 2.x: RectangleBarSeries uses explicit left/right bounds in data-space (OADate days),
        // so each bar is exactly 80% of one day wide regardless of zoom or chart size.
        // LinearBarSeries.BarWidth is in pixels, which made bars appear only 2 pixels wide.
        var series = new RectangleBarSeries
        {
            Title = "number of positions",
            FillColor = OxyColors.Green,
            StrokeColor = OxyColors.DarkGreen,
            StrokeThickness = 1,
            // {0}=series title  {2}=date (X midpoint, OADate double)  {Y1}=bar top value (RectangleBarItem property)
            TrackerFormatString = "{0}\n{2:dd-MM-yyyy}\n{Y1:N0}",
        };

        foreach (QueryPositionData data in QueryPositionDataList)
        {
            double x = DateTimeAxis.ToDouble(data.CloseTime!.Value);
            // Bar runs from x+0.1 to x+0.9 so it stays fully inside this day's axis space.
            series.Items.Add(new RectangleBarItem(x + 0.1, 0, x + 0.9, data.Positions));
        }

        model.Series.Add(series);

        //model.Legends.Add(new Legend
        //{
        //    LegendPosition = LegendPosition.RightTop
        //});

        return model;
    }

    private PlotModel CreateChartProfitsPerDay()
    {
        var model = new PlotModel
        {
            Title = "Profits per day",
            TextColor = Const.ChartTextColor,
            Background = Const.ChartSurfaceColor
        };

        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(80, 255, 255, 255),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromArgb(40, 255, 255, 255),
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = Const.ChartTextColor,
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            //Title = "Value",
            //Minimum = 0,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(80, 255, 255, 255),
            AxislineColor = OxyColors.White,
            TextColor = Const.ChartTextColor,
            AxislineStyle = LineStyle.Solid,
            StringFormat = QuoteData!.DisplayFormat,
        });

        // OxyPlot 2.x: RectangleBarSeries uses explicit left/right bounds in data-space (OADate days),
        // so each bar is exactly 80% of one day wide regardless of zoom or chart size.
        // LinearBarSeries.BarWidth is in pixels, which made bars appear only 2 pixels wide.
        // RectangleBarItem has no per-item color, so two separate series are used for positive/negative profit.
        var seriesProfit = new RectangleBarSeries
        {
            Title = "Total profit",
            FillColor = OxyColors.Green,
            StrokeColor = OxyColors.DarkGreen,
            StrokeThickness = 1,
            // {0}=series title  {2}=date (X midpoint, OADate double)  {Y1}=bar top value (RectangleBarItem property)
            TrackerFormatString = "{0}\n{2:dd-MM-yyyy}\n{Y1:" + QuoteData!.DisplayFormat + "}",
        };

        var seriesLoss = new RectangleBarSeries
        {
            Title = "Total loss",
            FillColor = OxyColors.Red,
            StrokeColor = OxyColors.DarkRed,
            StrokeThickness = 1,
            // {0}=series title  {2}=date (X midpoint, OADate double)  {Y1}=bar top value (RectangleBarItem property)
            TrackerFormatString = "{0}\n{2:dd-MM-yyyy}\n{Y1:" + QuoteData!.DisplayFormat + "}",
        };

        foreach (QueryPositionData data in QueryPositionDataList)
        {
            double x = DateTimeAxis.ToDouble(data.CloseTime!.Value);
            // Bar runs from x+0.1 to x+0.9 so it stays fully inside this day's axis space.
            if (data.TotalProfit < 0)
                seriesLoss.Items.Add(new RectangleBarItem(x + 0.1, 0, x + 0.9, (double)data.TotalProfit));
            else
                seriesProfit.Items.Add(new RectangleBarItem(x + 0.1, 0, x + 0.9, (double)data.TotalProfit));
        }

        model.Series.Add(seriesProfit);
        model.Series.Add(seriesLoss);

        // Thick horizontal zero line to clearly separate profit from loss.
        model.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = 0,
            Color = OxyColors.White,
            StrokeThickness = 2,
            LineStyle = LineStyle.Solid,
        });

        return model;
    }

    private PlotModel CreateChartProfitPercentagePerDay()
    {
        var model = new PlotModel { Title = "Min, max en gemiddelde winst per dag", TextColor = Const.ChartTextColor, Background = Const.ChartSurfaceColor };

        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = Const.ChartTextColor,
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Percentage",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = Const.ChartTextColor,
            StringFormat = "N2"
        });

        var minSeries = new LineSeries { Title = "Min %", Color = OxyColors.Red, MarkerType = MarkerType.Circle, MarkerSize = 3 };
        var avgSeries = new LineSeries { Title = "Avg %", Color = OxyColors.Orange, MarkerType = MarkerType.Circle, MarkerSize = 3 };
        var maxSeries = new LineSeries { Title = "Max %", Color = OxyColors.Green, MarkerType = MarkerType.Circle, MarkerSize = 3 };

        foreach (var data in QueryPositionDataList)
        {
            var dateValue = DateTimeAxis.ToDouble(data.CloseTime!.Value);
            minSeries.Points.Add(new DataPoint(dateValue, (double)data.MinPerc));
            avgSeries.Points.Add(new DataPoint(dateValue, (double)data.AvgPerc));
            maxSeries.Points.Add(new DataPoint(dateValue, (double)data.MaxPerc));
        }

        model.Series.Add(minSeries);
        model.Series.Add(avgSeries);
        model.Series.Add(maxSeries);

        model.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.RightTop
        });

        return model;
    }

    private PlotModel CreateChartInvestedReturnedPerDay()
    {
        var model = new PlotModel { Title = "Invested and returned per day", TextColor = Const.ChartTextColor, Background = Const.ChartSurfaceColor };

        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd-MM",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = Const.ChartTextColor,
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = QuoteData?.Name ?? "Value",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.Gray,
            AxislineStyle = LineStyle.Solid,
            TextColor = Const.ChartTextColor,
        });

        var investedData = GetQueryInvestedData();
        var returnedData = GetQueryReturnedData();

        var investedSeries = new LineSeries { Title = "Invested", Color = OxyColors.Red, MarkerType = MarkerType.Circle, MarkerSize = 3 };
        var returnedSeries = new LineSeries { Title = "Returned", Color = OxyColors.Green, MarkerType = MarkerType.Circle, MarkerSize = 3 };

        // Aggregate invested and returned per day
        var combinedData = new Dictionary<DateTime, (decimal Invested, decimal Returned)>();

        foreach (var data in investedData)
        {
            var day = data.CloseTime!.Value;
            if (!combinedData.ContainsKey(day))
                combinedData[day] = (0, 0);

            var current = combinedData[day];
            combinedData[day] = (current.Invested + data.Invested, current.Returned);
        }

        foreach (var data in returnedData)
        {
            var day = data.CloseTime!.Value;
            if (!combinedData.ContainsKey(day))
                combinedData[day] = (0, 0);

            var current = combinedData[day];
            combinedData[day] = (current.Invested, current.Returned + data.Returned);
        }

        foreach (var kvp in combinedData.OrderBy(x => x.Key))
        {
            var dateValue = DateTimeAxis.ToDouble(kvp.Key.Date);
            investedSeries.Points.Add(new DataPoint(dateValue, (double)kvp.Value.Invested));
            returnedSeries.Points.Add(new DataPoint(dateValue, (double)kvp.Value.Returned));
        }

        model.Series.Add(investedSeries);
        model.Series.Add(returnedSeries);

        model.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.RightTop
        });

        return model;
    }

    private PlotModel CreateChartDoorlooptijden()
    {
        var model = new PlotModel { Title = "Minimale, maximale en gemiddelde doorlooptijden in uren", TextColor = Const.ChartTextColor, Background = Const.ChartSurfaceColor };

        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd-MM",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = Const.ChartTextColor,
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Uren",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = Const.ChartTextColor,
            StringFormat = "N1"
        });

        var minSeries = new LineSeries { Title = "Minimal", Color = OxyColors.Green, MarkerType = MarkerType.Circle, MarkerSize = 3 };
        var avgSeries = new LineSeries { Title = "Average", Color = OxyColors.Orange, MarkerType = MarkerType.Circle, MarkerSize = 3 };
        var maxSeries = new LineSeries { Title = "Maximal", Color = OxyColors.Red, MarkerType = MarkerType.Circle, MarkerSize = 3 };

        foreach (var data in QueryPositionDataList)
        {
            var dateValue = DateTimeAxis.ToDouble(data.CloseTime!.Value);
            minSeries.Points.Add(new DataPoint(dateValue, (double)data.MinMin));
            avgSeries.Points.Add(new DataPoint(dateValue, (double)data.AvgMin));
            maxSeries.Points.Add(new DataPoint(dateValue, (double)data.MaxMin));
        }

        model.Series.Add(minSeries);
        model.Series.Add(avgSeries);
        model.Series.Add(maxSeries);

        model.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.RightTop
        });

        return model;
    }

    private List<QueryPositionData> GetQueryInvestedData()
    {
        StringBuilder builder = new();
        builder.AppendLine("select date(positionStep.CloseTime,'localtime') as CloseTime,");
        builder.AppendLine("symbol.quote, sum(positionStep.QuoteQuantityFilled) as Invested");
        builder.AppendLine("from PositionStep");
        builder.AppendLine("inner join position on Position.Id = positionStep.PositionId");
        builder.AppendLine("inner join symbol on Position.symbolid = symbol.id");
        builder.AppendLine("where PositionStep.status in (1, 2) and PositionStep.Side = 0");
        builder.AppendLine("and position.Invested > 0");
        builder.AppendLine("and position.Status in (1,2)");
        builder.AppendLine($"and symbol.quote = '{QuoteData!.Name}'");
        builder.AppendLine("group by date(PositionStep.CloseTime,'localtime'), PositionStep.Status, symbol.quote");
        builder.AppendLine("order by date(PositionStep.CloseTime,'localtime') desc, PositionStep.Status, symbol.quote");

        using CryptoDatabase databaseThread = new();
        databaseThread.Open();

        List<QueryPositionData> list = new();
        foreach (QueryPositionData data in databaseThread.Connection.Query<QueryPositionData>(builder.ToString()))
        {
            if (data.CloseTime.HasValue)
                list.Add(data);
        }
        return list;
    }

    private List<QueryPositionData> GetQueryReturnedData()
    {
        StringBuilder builder = new();
        builder.AppendLine("select date(positionStep.CloseTime,'localtime') as CloseTime,");
        builder.AppendLine("symbol.quote, sum(positionStep.QuoteQuantityFilled) as Returned");
        builder.AppendLine("from PositionStep");
        builder.AppendLine("inner join position on Position.Id = positionStep.PositionId");
        builder.AppendLine("inner join symbol on Position.symbolid = symbol.id");
        builder.AppendLine("where PositionStep.status in (1, 2) and PositionStep.Side = 1");
        builder.AppendLine("and position.Invested > 0");
        builder.AppendLine("and position.Status in (1,2)");
        builder.AppendLine($"and symbol.quote = '{QuoteData!.Name}'");
        builder.AppendLine("group by date(PositionStep.CloseTime,'localtime'), PositionStep.Status, symbol.quote");
        builder.AppendLine("order by date(PositionStep.CloseTime,'localtime') desc, PositionStep.Status, symbol.quote");

        using CryptoDatabase databaseThread = new();
        databaseThread.Open();

        List<QueryPositionData> list = new();
        foreach (QueryPositionData data in databaseThread.Connection.Query<QueryPositionData>(builder.ToString()))
        {
            if (data.CloseTime.HasValue)
                list.Add(data);
        }
        return list;
    }

    private void DoAdditionalData()
    {
        string quoteDataDisplayString = QuoteData?.DisplayFormat ?? "N2";

        // Berekeningen voor open posities
        decimal investedInTrades = OpenData.Invested - OpenData.Returned;
        (NettoPnlWhole, NettoPnlFraction) = QueryPositionData.SplitNumber(investedInTrades, quoteDataDisplayString);

        // Huidige waarde van open posities
        decimal currentValue = 0;
        if (GlobalData.ActiveExchange?.Data.PositionList != null)
        {
            foreach (var position in GlobalData.ActiveExchange.Data.PositionList.Values)
            {
                if (position.Symbol.Quote.Equals(QuoteData?.Name))
                    currentValue += position.CurrentValue();
            }
        }

        (CurrentValueWhole, CurrentValueFraction) = QueryPositionData.SplitNumber(currentValue, quoteDataDisplayString);
        (VirtualProfitWhole, VirtualProfitFraction) = QueryPositionData.SplitNumber(currentValue - investedInTrades, quoteDataDisplayString);

        if (investedInTrades > 0)
            (VirtualProfitPercentageWhole, VirtualProfitPercentageFraction) = QueryPositionData.SplitNumber((100 * (currentValue / investedInTrades)) - 100, "N2");
        else
        {
            VirtualProfitPercentageWhole = "0";
            VirtualProfitPercentageFraction = "";
        }

        // Gesloten posities percentage
        if (ClosedData.Invested > 0)
        {
            var (whole, fraction) = QueryPositionData.SplitNumber(100 * (ClosedData.TotalProfit / ClosedData.Invested), "N2");
            ClosedProfitPercentageWhole = whole;
            ClosedProfitPercentageFraction = fraction;
        }
        else
        {
            ClosedProfitPercentageWhole = "";
            ClosedProfitPercentageFraction = "";
        }

        // Totaal (openstaand + gesloten samengeteld).
        // For the profit we use the realized profit of the closed positions plus the
        // virtual (unrealized) profit of the still-open positions (currentValue - investedInTrades).
        // The DB field position.Profit of an open position is not a realistic realized return,
        // so summing it would heavily overstate the total profit.
        decimal openVirtualProfit = currentValue - investedInTrades;
        var totalData = new QueryPositionData
        {
            Positions = OpenData.Positions + ClosedData.Positions,
            Invested = OpenData.Invested + ClosedData.Invested,
            Returned = OpenData.Returned + ClosedData.Returned,
            Commission = OpenData.Commission + ClosedData.Commission,
            TotalProfit = ClosedData.TotalProfit + openVirtualProfit,
        };
        TotalData = totalData;

        // Totaal percentage
        if (totalData.Invested > 0)
        {
            var (whole, fraction) = QueryPositionData.SplitNumber(100 * (totalData.TotalProfit / totalData.Invested), "N2");
            TotalProfitPercentageWhole = whole;
            TotalProfitPercentageFraction = fraction;
        }
        else
        {
            TotalProfitPercentageWhole = "";
            TotalProfitPercentageFraction = "";
        }

        // Trigger property changed voor formatted values
        OnPropertyChanged(nameof(OpenData));
        OnPropertyChanged(nameof(ClosedData));
        OnPropertyChanged(nameof(TotalData));
    }
}
