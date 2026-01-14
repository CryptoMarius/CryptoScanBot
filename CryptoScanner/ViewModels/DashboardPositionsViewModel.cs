using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using Dapper;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

using System.Collections.ObjectModel;
using System.Text;

namespace CryptoScanner.ViewModels;

public partial class DashboardPositionsViewModel : ObservableObject
{
    public class QueryPositionData
    {
        public DateTime CloseTime { get; set; }
        public string Quote { get; set; } = "";
        public CryptoOrderStatus Status { get; set; }

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
    private string _nettoPnlValue = "0.00";

    [ObservableProperty]
    private string _currentValue = "0.00";

    [ObservableProperty]
    private string _virtualProfit = "0.00";

    [ObservableProperty]
    private string _virtualProfitPercentage = "0.00";

    [ObservableProperty]
    private string _closedProfitPercentage = "";

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
        
        // Add the active quotes (default=usdt)
        List<string> quotes = [];
        foreach (CryptoQuoteData cryptoQuoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (cryptoQuoteData.FetchCandles)
                quotes.Add(cryptoQuoteData.Name);
        }
        if (quotes.Count == 0)
            quotes.Add("USDT");
        QuoteOptions = new ObservableCollection<string>(quotes);
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
        builder.AppendLine("select date(position.CloseTime,'localtime') as CloseTime, symbol.quote, position.Status, count(position.id) as Positions,");
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
        builder.AppendLine("group by date(position.CloseTime,'localtime'), position.Status, symbol.quote");
        builder.AppendLine("order by date(position.CloseTime,'localtime') desc, position.Status, symbol.quote");

        using CryptoDatabase databaseThread = new();
        databaseThread.Open();

        QueryPositionDataList.Clear();
        OpenData = new QueryPositionData();
        ClosedData = new QueryPositionData();

        foreach (QueryPositionData data in databaseThread.Connection.Query<QueryPositionData>(builder.ToString()))
        {
            if (data.CloseTime.Date > new DateTime(2000, 01, 01))
            {
                QueryPositionDataList.Add(data);

                ClosedData.Positions += data.Positions;
                ClosedData.Invested += data.Invested;
                ClosedData.Returned += data.Returned;
                ClosedData.Commission += data.Commission;
                ClosedData.TotalProfit += data.TotalProfit;
                // enzovoort..
            }
            else
            {
                OpenData = data; // het restant
                // verschil vanwege meerdere quotes
                //OpenData.Positions += data.Positions;
                //OpenData.Invested += data.Invested;
                //OpenData.Returned += data.Returned;
                //OpenData.Commission += data.Commission;
            }
        }
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
            Title = "Aantal gesloten posities per dag", 
            TextColor = OxyColors.White, 
            Background = OxyColors.Black
        };
        
        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd-MM",
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = OxyColors.White,
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Aantal",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            TextColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            StringFormat = "N0"
        });

        var series = new LineSeries
        {
            Title = "Posities",
            Color = OxyColors.Green,
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = OxyColors.Green
        };

        foreach (var data in QueryPositionDataList)
        {
            series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(data.CloseTime.Date), data.Positions));
        }

        model.Series.Add(series);
        
        model.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.RightTop
        });

        return model;
    }

    private PlotModel CreateChartProfitPercentagePerDay()
    {
        var model = new PlotModel { Title = "Min, max en gemiddelde winst per dag", TextColor = OxyColors.White, Background = OxyColors.Black };

        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd-MM",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = OxyColors.White,
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Percentage",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = OxyColors.White,
            StringFormat = "N2"
        });

        var minSeries = new LineSeries { Title = "Min %", Color = OxyColors.Red, MarkerType = MarkerType.Circle, MarkerSize = 3 };
        var avgSeries = new LineSeries { Title = "Avg %", Color = OxyColors.Orange, MarkerType = MarkerType.Circle, MarkerSize = 3 };
        var maxSeries = new LineSeries { Title = "Max %", Color = OxyColors.Green, MarkerType = MarkerType.Circle, MarkerSize = 3 };

        foreach (var data in QueryPositionDataList)
        {
            var dateValue = DateTimeAxis.ToDouble(data.CloseTime.Date);
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

    private PlotModel CreateChartProfitsPerDay()
    {
        var model = new PlotModel { Title = "Winst/verlies per dag", TextColor = OxyColors.White, Background = OxyColors.Black };

        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd-MM",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = OxyColors.White,
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = QuoteData?.Name ?? "Value",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = OxyColors.White,
        });

        var series = new LineSeries
        {
            Title = "Winst",
            Color = OxyColors.DarkGreen,
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = OxyColors.DarkGreen
        };

        foreach (var data in QueryPositionDataList)
        {
            series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(data.CloseTime.Date), (double)data.TotalProfit));
        }

        model.Series.Add(series);
        
        model.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.RightTop
        });

        return model;
    }

    private PlotModel CreateChartInvestedReturnedPerDay()
    {
        var model = new PlotModel { Title = "Geinvesteerd en geretourneerd per dag", TextColor = OxyColors.White, Background = OxyColors.Black };

        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd-MM",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = OxyColors.White,
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = QuoteData?.Name ?? "Value",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.Gray,
            AxislineStyle = LineStyle.Solid,
            TextColor = OxyColors.White,
        });

        var investedData = GetQueryInvestedData();
        var returnedData = GetQueryReturnedData();

        var investedSeries = new LineSeries { Title = "Geinvesteerd", Color = OxyColors.Red, MarkerType = MarkerType.Circle, MarkerSize = 3 };
        var returnedSeries = new LineSeries { Title = "Geretourneerd", Color = OxyColors.Green, MarkerType = MarkerType.Circle, MarkerSize = 3 };

        // Aggregate invested and returned per day
        var combinedData = new Dictionary<DateTime, (decimal Invested, decimal Returned)>();

        foreach (var data in investedData)
        {
            if (!combinedData.ContainsKey(data.CloseTime.Date))
                combinedData[data.CloseTime.Date] = (0, 0);
            
            var current = combinedData[data.CloseTime.Date];
            combinedData[data.CloseTime.Date] = (current.Invested + data.Invested, current.Returned);
        }

        foreach (var data in returnedData)
        {
            if (!combinedData.ContainsKey(data.CloseTime.Date))
                combinedData[data.CloseTime.Date] = (0, 0);
            
            var current = combinedData[data.CloseTime.Date];
            combinedData[data.CloseTime.Date] = (current.Invested, current.Returned + data.Returned);
        }

        foreach (var kvp in combinedData.OrderBy(x => x.Key))
        {
            var dateValue = DateTimeAxis.ToDouble(kvp.Key);
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
        var model = new PlotModel { Title = "Minimale, maximale en gemiddelde doorlooptijden in uren", TextColor = OxyColors.White, Background = OxyColors.Black };

        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd-MM",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = OxyColors.White,
        });
        
        model.Axes.Add(new LinearAxis{
            Position = AxisPosition.Left,
            Title = "Uren",
            MajorGridlineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.White,
            AxislineStyle = LineStyle.Solid,
            TextColor = OxyColors.White,
            StringFormat = "N1"            
        });

        var minSeries = new LineSeries { Title = "Minimaal", Color = OxyColors.Green, MarkerType = MarkerType.Circle, MarkerSize = 3 };
        var avgSeries = new LineSeries { Title = "Gemiddeld", Color = OxyColors.Orange, MarkerType = MarkerType.Circle, MarkerSize = 3 };
        var maxSeries = new LineSeries { Title = "Maximaal", Color = OxyColors.Red, MarkerType = MarkerType.Circle, MarkerSize = 3 };

        foreach (var data in QueryPositionDataList)
        {
            var dateValue = DateTimeAxis.ToDouble(data.CloseTime.Date);
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
        builder.AppendLine("select date(positionStep.CloseTime,'localtime') as CloseTime, symbol.quote, sum(positionStep.QuoteQuantityFilled) as Invested");
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
            if (data.CloseTime.Date > new DateTime(2000, 01, 01))
                list.Add(data);
        }
        return list;
    }

    private List<QueryPositionData> GetQueryReturnedData()
    {
        StringBuilder builder = new();
        builder.AppendLine("select date(positionStep.CloseTime,'localtime') as CloseTime, symbol.quote, sum(positionStep.QuoteQuantityFilled) as Returned");
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
            if (data.CloseTime.Date > new DateTime(2000, 01, 01))
                list.Add(data);
        }
        return list;
    }

    private void DoAdditionalData()
    {
        string quoteDataDisplayString = QuoteData?.DisplayFormat ?? "N2";

        // Berekeningen voor open posities
        decimal investedInTrades = OpenData.Invested - OpenData.Returned;
        NettoPnlValue = investedInTrades.ToString(quoteDataDisplayString);

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
        
        CurrentValue = currentValue.ToString(quoteDataDisplayString);
        VirtualProfit = (currentValue - investedInTrades).ToString(quoteDataDisplayString);
        
        if (investedInTrades > 0)
            VirtualProfitPercentage = ((100 * (currentValue / investedInTrades)) - 100).ToString("N2");
        else
            VirtualProfitPercentage = "0.00";

        // Gesloten posities percentage
        if (ClosedData.Invested > 0)
            ClosedProfitPercentage = (100 * (ClosedData.TotalProfit / ClosedData.Invested)).ToString("N2");
        else
            ClosedProfitPercentage = "";

        // Trigger property changed voor formatted values
        OnPropertyChanged(nameof(OpenData));
        OnPropertyChanged(nameof(ClosedData));
    }
}
