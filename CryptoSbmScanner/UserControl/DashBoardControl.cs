using System.Text;
using System.Windows.Forms.DataVisualization.Charting;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;

using Dapper;

namespace CryptoSbmScanner;

// Charts:
// https://stackoverflow.com/questions/10622674/chart-creating-dynamically-in-net-c-sharp
// https://stackoverflow.com/questions/335061/add-dynamic-charts-using-asp-net-chart-control-c-sharp


public partial class DashBoardControl : UserControl
{
    public class QueryPositionData
    {
        public DateTime CloseTime { get; set; }
        public string Quote { get; set; }
        public CryptoOrderStatus Status { get; set; }

        // Aantal per dag
        public int Positions { get; set; }

        // Wat totalen
        public decimal Invested { get; set; }
        public decimal Returned { get; set; }
        public decimal Commission { get; set; }
        public decimal TotalProfit { get; set; }

        // De doorlooptijd in minuten
        public decimal MinMin { get; set; }
        public decimal AvgMin { get; set; }
        public decimal MaxMin { get; set; }

        // De winst percentages
        public decimal MinPerc { get; set; }
        public decimal AvgPerc { get; set; }
        public decimal MaxPerc { get; set; }
    }

    public class QueryTradeData
    {
        public DateTime TradeTime { get; set; }
        public CryptoOrderSide Side { get; set; }
        public string Quote { get; set; }

        // Wat totalen
        public decimal Value { get; set; }
    }


    private readonly int OffsetX = 25;
    private readonly int OffsetY = 200;
    private readonly int GraphWidth = 500;
    private readonly int GraphHeight = 300;

    private QueryPositionData OpenData = new();
    private List<QueryTradeData> QueryTradeDataList = new();
    private List<QueryPositionData> QueryPositionDataList = new();

    private Chart ChartPositionsPerDay;
    private Chart ChartProfitsPerDay;
    private Chart ChartProfitPercentagePerDay;
    private Chart ChartInvestedReturnedPerDay;


    public DashBoardControl()
    {
        InitializeComponent();
    }

    private void GetQueryTradeData()
    {
        // dit is de query die per dag het een en ander aan info ophaalt
        StringBuilder builder = new();
        builder.AppendLine("select date(position.CloseTime) as CloseTime, symbol.quote, position.Status, count(position.id) as Positions,");
        builder.AppendLine("round(MIN(ROUND((JULIANDAY(position.CloseTime) - JULIANDAY(position.CreateTime)) * 86400 / 60)), 2) AS MinMin, -- in minutes");
        builder.AppendLine("round(AVG(ROUND((JULIANDAY(position.CloseTime) - JULIANDAY(position.CreateTime)) * 86400 / 60)), 2) AS AvgMin, -- in minutes");
        builder.AppendLine("round(MAX(ROUND((JULIANDAY(position.CloseTime) - JULIANDAY(position.CreateTime)) * 86400 / 60)), 2) AS MaxMin, -- in minutes");
        builder.AppendLine("sum(position.Invested) as Invested,");
        builder.AppendLine("sum(position.Returned) as Returned,");
        builder.AppendLine("sum(position.Commission) as Commission,");
        builder.AppendLine("sum(position.Profit) as TotalProfit,  --= sum(position.Returned - position.Invested - position.Commission) as Open,");
        builder.AppendLine("--100 * sum(position.Profit) / sum(position.Invested) as Average");
        builder.AppendLine("round(min(round(position.percentage, 2)), 2) as MinPerc,");
        builder.AppendLine("round(avg(round(position.percentage, 2)), 2) as AvgPerc,");
        builder.AppendLine("round(max(round(position.percentage, 2)), 2) as MaxPerc");
        builder.AppendLine("from position");
        builder.AppendLine("inner join symbol on position.symbolid = symbol.id");
        builder.AppendLine("--where position.status = 2");
        builder.AppendLine("where symbol.quote = 'USDT'");
        builder.AppendLine("group by date(position.CloseTime), position.Status, symbol.quote");
        builder.AppendLine("order by date(position.CloseTime) asc, position.Status, symbol.quote");

        using CryptoDatabase databaseThread = new();
        databaseThread.Open();

        // Experiment #1 is een chart met per datum het aantal gesloten posities
        // De 1e kolom is het aantal nog openstaande posities, die moeten we nog ergens onderbrengen
        QueryPositionDataList.Clear();
        foreach (QueryPositionData data in databaseThread.Connection.Query<QueryPositionData>(builder.ToString()))
        {
            if (data.CloseTime.Date > new DateTime(2000, 01, 01))
                QueryPositionDataList.Add(data);
            else
                OpenData = data; // het restant
        }
    }

    private void GetQueryPositionData()
    {
        // Mhhhh, dat zou eigenlijk via de trades moeten lopen
        // dit is de query die per dag het een en ander aan info ophaalt
        StringBuilder builder = new();
        builder.AppendLine("select");
        builder.AppendLine("  date(trade.TradeTime) as TradeTime,");
        builder.AppendLine("  trade.Side,");
        builder.AppendLine("  symbol.quote,");
        builder.AppendLine("  sum(trade.QuoteQuantity) as Value");
        builder.AppendLine("from trade");
        builder.AppendLine("inner join symbol on trade.symbolid = symbol.id");
        builder.AppendLine("where symbol.quote = 'USDT'");
        builder.AppendLine("group by date(trade.TradeTime), trade.Side,symbol.quote");
        builder.AppendLine("order by date(trade.TradeTime) asc, trade.Side,symbol.quote");

        using CryptoDatabase databaseThread = new();
        databaseThread.Open();

        QueryTradeDataList.Clear();
        foreach (QueryTradeData data in databaseThread.Connection.Query<QueryTradeData>(builder.ToString()))
        {
            if (data.TradeTime.Date > new DateTime(2000, 01, 01))
            {
                QueryTradeDataList.Add(data);
            }
        }
    }

    private Chart CreateChart(string title, int x, int y)
    {
        Chart chart = new Chart();
        //chart.Title = "Title of the Chart";
        //chart.DisplayTitle = true;
        //chart.TabIndex = 0;
        //chart.Name = "chartname";
        //chart.Text = "charttext";
        chart.Titles.Add(title).ForeColor = Color.White;
        chart.BackColor = Color.Black;
        chart.Location = new(x, y);
        chart.Size = new Size(GraphWidth, GraphHeight);
        //chart.Dock = DockStyle.Fill; // right
        //chart1.ChartAreas.Clear();
        return chart;
    }

    private static ChartArea CreateChartArea(string axisYFormat)
    {
        ChartArea chartArea = new();
        //chartArea1.Name = "ChartAreaName";
        chartArea.BackColor = Color.Black;

        chartArea.AxisX.LabelStyle.Format = "dd";
        chartArea.AxisX.LineColor = Color.Gray;
        chartArea.AxisX.MinorGrid.LineColor = Color.Gray;
        chartArea.AxisX.MajorGrid.LineColor = Color.Gray;
        chartArea.AxisX.LabelStyle.ForeColor = Color.Gray;
        chartArea.AxisX.LabelStyle.Enabled = true;

        //chartArea.AxisX.TitleAlignment = StringAlignment.Far;
        //chartArea.AxisX.TextOrientation = TextOrientation.Horizontal;
        //chartArea.AxisX.Title = "Datum";
        //chartArea.AxisX.TitleForeColor = Color.Gray;
        //chartArea.AxisX.TitleFont = new Font("Tahoma", 7, FontStyle.Bold);

        chartArea.AxisY.LabelStyle.Format = axisYFormat;
        chartArea.AxisY.LineColor = Color.Gray;
        chartArea.AxisY.MinorGrid.LineColor = Color.Gray;
        chartArea.AxisY.MajorGrid.LineColor = Color.Gray;
        chartArea.AxisY.LabelStyle.ForeColor = Color.Gray;
        chartArea.AxisY.LabelStyle.Enabled = true;

        //chartArea.AxisY.TitleAlignment = StringAlignment.Far;
        //chartArea.AxisY.TextOrientation = TextOrientation.Horizontal;
        //chartArea.AxisY.Title = "Aantal";
        //chartArea.AxisY.TitleForeColor = Color.Gray;
        //chartArea.AxisY.TitleFont = new Font("Tahoma", 7, FontStyle.Bold);

        return chartArea;
    }

    private void DoChartPositionsPerDay(int x, int y)
    {
        if (ChartPositionsPerDay == null)
        {
            ChartPositionsPerDay = CreateChart("Aantal gesloten posities per dag", x, y);
            Controls.Add(ChartPositionsPerDay);

            ChartArea chartArea = CreateChartArea("N0");
            ChartPositionsPerDay.ChartAreas.Add(chartArea);


            //chart1.Legends.Clear();
            //chart1.Legends.Add(legend1);
            //Legend legend1 = chart1.Legends.Add("legenda");
            //legend1.BackColor = Color.Black;
            //legend1.ForeColor = Color.White;
        }

        ChartPositionsPerDay.Series.Clear();
        var series1 = new Series
        {
            Name = "Posities",
            Color = Color.Green,
            IsVisibleInLegend = true,
            IsXValueIndexed = true,
            //ChartType = SeriesChartType.Bar
            ChartType = SeriesChartType.Column,
        };
        ChartPositionsPerDay.Series.Add(series1);


        // Experiment #1 is een chart met per datum het aantal gesloten posities
        // De 1e kolom is het aantal nog openstaande posities, die moeten we nog ergens onderbrengen
        foreach (QueryPositionData data in QueryPositionDataList)
        {
            if (data.CloseTime.Date > new DateTime(2000, 01, 01))
                series1.Points.AddXY(data.CloseTime.Date, data.Positions);

            //DataPoint dataPoint = new DataPoint() { AxisLabel = "series", YValues = new double[] { dataPoint } };
            //series1.Points.Add(dataPoint);
        }
        ChartPositionsPerDay.Invalidate();
    }

    private void DoChartProfitsPerDay(int x, int y)
    {
        if (ChartProfitsPerDay == null)
        {
            ChartProfitsPerDay = CreateChart("Behaalde winst bedrag per dag", x, y);
            Controls.Add(ChartProfitsPerDay);

            ChartArea chartArea = CreateChartArea("N2");
            ChartProfitsPerDay.ChartAreas.Add(chartArea);

            //chart1.Legends.Clear();
            //chart1.Legends.Add(legend1);
            //Legend legend1 = chart1.Legends.Add("legenda");
            //legend1.BackColor = Color.Black;
            //legend1.ForeColor = Color.White;
        }
        ChartProfitsPerDay.Series.Clear();


        var series1 = new Series
        {
            Name = "Winst",
            Color = Color.Green,
            IsVisibleInLegend = true,
            IsXValueIndexed = true,
            //ChartType = SeriesChartType.Bar
            ChartType = SeriesChartType.Column,
        };
        ChartProfitsPerDay.Series.Add(series1);


        foreach (QueryPositionData data in QueryPositionDataList)
        {
            if (data.CloseTime.Date > new DateTime(2000, 01, 01))
                series1.Points.AddXY(data.CloseTime.Date, data.TotalProfit);
        }
        ChartProfitsPerDay.Invalidate();
    }


    private void DoChartProfitPercentagePerDay(int x, int y)
    {
        // AVG profit per trade
        if (ChartProfitPercentagePerDay == null)
        {
            ChartProfitPercentagePerDay = CreateChart("Minimale, maximale en gemiddelde winst percentage per dag", x, y);
            Controls.Add(ChartProfitPercentagePerDay);

            ChartArea chartArea = CreateChartArea("N2");
            ChartProfitPercentagePerDay.ChartAreas.Add(chartArea);


            ChartProfitPercentagePerDay.Legends.Clear();
            //chart1.Legends.Add(legend1);
            Legend legend1 = ChartProfitPercentagePerDay.Legends.Add("legenda");
            legend1.BackColor = Color.Black;
            legend1.ForeColor = Color.White;

        }
        ChartProfitPercentagePerDay.Series.Clear();


        var series1 = new Series
        {
            Name = "Minimaal",
            Color = Color.Red,
            IsVisibleInLegend = true,
            IsXValueIndexed = true,
            //ChartType = SeriesChartType.Bar
            ChartType = SeriesChartType.Line,
        };
        ChartProfitPercentagePerDay.Series.Add(series1);

        var series2 = new Series
        {
            Name = "Gemiddeld",
            Color = Color.Orange,
            IsVisibleInLegend = true,
            IsXValueIndexed = true,
            //ChartType = SeriesChartType.Bar
            ChartType = SeriesChartType.Line,
        };
        ChartProfitPercentagePerDay.Series.Add(series2);

        var series3 = new Series
        {
            Name = "Maximaal",
            Color = Color.Green,
            IsVisibleInLegend = true,
            IsXValueIndexed = true,
            //ChartType = SeriesChartType.Bar
            ChartType = SeriesChartType.Line,
        };
        ChartProfitPercentagePerDay.Series.Add(series3);

        // Experiment #2 is een chart met per datum het aantal gesloten posities
        // De 1e kolom is het aantal nog openstaande posities, die moeten we nog ergens onderbrengen
        foreach (QueryPositionData data in QueryPositionDataList)
        {
            if (data.CloseTime.Date > new DateTime(2000, 01, 01))
            {
                series1.Points.AddXY(data.CloseTime.Date, data.MinPerc - 100);
                series2.Points.AddXY(data.CloseTime.Date, data.AvgPerc - 100);
                series3.Points.AddXY(data.CloseTime.Date, data.MaxPerc - 100);
            }
        }
        ChartProfitPercentagePerDay.Invalidate();
    }

    private void DoChartInvestedReturnedPerDay(int x, int y)
    {
        if (ChartInvestedReturnedPerDay == null)
        {
            ChartInvestedReturnedPerDay = CreateChart("Geinvesteerde en geretourneede bedragen per dag", x, y);
            Controls.Add(ChartInvestedReturnedPerDay);

            ChartArea chartArea = CreateChartArea("N2");
            ChartInvestedReturnedPerDay.ChartAreas.Add(chartArea);

            ChartInvestedReturnedPerDay.Legends.Clear();
            //chart1.Legends.Add(legend1);
            var legend1 = ChartInvestedReturnedPerDay.Legends.Add("legenda");
            legend1.BackColor = Color.Black;
            legend1.ForeColor = Color.White;
        }
        ChartInvestedReturnedPerDay.Series.Clear();


        var series1 = new Series
        {
            Name = "Geinvesteerd",
            Color = Color.Red,
            IsVisibleInLegend = true,
            IsXValueIndexed = true,
            //ChartType = SeriesChartType.Bar
            ChartType = SeriesChartType.Line,
        };
        ChartInvestedReturnedPerDay.Series.Add(series1);

        var series2 = new Series
        {
            Name = "Geretourneerd",
            Color = Color.Green,
            IsVisibleInLegend = true,
            IsXValueIndexed = true,
            //ChartType = SeriesChartType.Bar
            ChartType = SeriesChartType.Line,
        };
        ChartInvestedReturnedPerDay.Series.Add(series2);


        foreach (QueryTradeData data in QueryTradeDataList)
        {
            if (data.TradeTime.Date > new DateTime(2000, 01, 01))
            {
                if (data.Side == CryptoOrderSide.Buy)
                    series1.Points.AddXY(data.TradeTime.Date, data.Value);
                else
                    series2.Points.AddXY(data.TradeTime.Date, data.Value);
            }
        }

        ChartInvestedReturnedPerDay.Invalidate();
    }

    private void CreateChart()
    {
        // Diverse statistieken van de posities per dag (via position tabel)
        // +Investeringen en returnments per dag (via trade tabel)

        GetQueryPositionData();
        GetQueryTradeData();

        DoChartPositionsPerDay(OffsetX, OffsetY);
        DoChartProfitPercentagePerDay(OffsetX + GraphWidth + 25, OffsetY);

        DoChartProfitsPerDay(OffsetX, OffsetY + GraphHeight + 25);
        DoChartInvestedReturnedPerDay(OffsetX + GraphWidth + 25, OffsetY + GraphHeight + 25);

        // En de lopende posities
        labelPositions.Text = OpenData.Positions.ToString();
        labelInvested.Text = OpenData.Invested.ToString();
        labelReturned.Text = OpenData.Returned.ToString();
        labelCommission.Text = OpenData.Commission.ToString();
        labelNettoPnlValue.Text = (OpenData.Invested - OpenData.Returned - OpenData.Commission).ToString();
            //OpenData.TotalProfit.ToString();

        return;
    }

    private void button1_Click(object sender, EventArgs e)
    {
        CreateChart();
    }
}
