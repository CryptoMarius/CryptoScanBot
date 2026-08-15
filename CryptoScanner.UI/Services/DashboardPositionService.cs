using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Dapper;

using System.Text;

namespace CryptoScanner.UI.Services;

public class DashboardPositionService
{
    public PositionStats OpenData { get; private set; } = new();
    public PositionStats ClosedData { get; private set; } = new();
    public PositionStats TotalData { get; private set; } = new();

    public string NettoPnl { get; private set; } = "0";
    public string CurrentValue { get; private set; } = "0";
    public string VirtualProfit { get; private set; } = "0";
    public string VirtualProfitPercentage { get; private set; } = "0";
    public string ClosedProfitPercentage { get; private set; } = "";
    public string TotalProfitPercentage { get; private set; } = "";

    public string NettoPnlClass { get; private set; } = "";
    public string VirtualProfitClass { get; private set; } = "";
    public string ClosedProfitClass { get; private set; } = "";
    public string TotalProfitClass { get; private set; } = "";

    public List<DailyPositionData> DailyData { get; private set; } = [];
    public List<string> QuoteOptions { get; private set; } = ["USDT"];

    public event Action? DataChanged;

    public void RefreshQuoteOptions()
    {
        List<string> quotes = [];
        foreach (CryptoQuoteData cryptoQuoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (cryptoQuoteData.FetchCandles && cryptoQuoteData.SymbolList.Count > 0)
                quotes.Add(cryptoQuoteData.Name);
        }
        if (quotes.Count == 0)
            quotes.Add("USDT");
        QuoteOptions = quotes;
    }

    public void Refresh(string quote)
    {
        if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Running)
            return;

        if (!GlobalData.Settings.QuoteCoins.TryGetValue(quote, out var quoteData))
            return;

        string displayFormat = quoteData.DisplayFormat ?? "N2";

        QueryPositionData(quote, out var openData, out var closedData, out var dailyData);

        OpenData = openData;
        ClosedData = closedData;
        DailyData = dailyData;

        decimal investedInTrades = openData.Invested - openData.Returned;
        NettoPnl = investedInTrades.ToString(displayFormat);
        NettoPnlClass = ColorClassForValue(investedInTrades);

        decimal currentValue = 0;
        if (GlobalData.ActiveExchange?.Data.PositionList != null)
        {
            foreach (var position in GlobalData.ActiveExchange.Data.PositionList.Values)
            {
                if (position.Symbol.Quote.Equals(quoteData.Name))
                    currentValue += position.CurrentValue();
            }
        }
        CurrentValue = currentValue.ToString(displayFormat);

        decimal virtualProfit = currentValue - investedInTrades;
        VirtualProfit = virtualProfit.ToString(displayFormat);
        VirtualProfitClass = ColorClassForValue(virtualProfit);

        if (investedInTrades > 0)
            VirtualProfitPercentage = ((100 * (currentValue / investedInTrades)) - 100).ToString("N2") + "%";
        else
            VirtualProfitPercentage = "0";

        if (closedData.Invested > 0)
        {
            decimal closedPct = 100 * (closedData.TotalProfit / closedData.Invested);
            ClosedProfitPercentage = closedPct.ToString("N2") + "%";
            ClosedProfitClass = ColorClassForValue(closedPct);
        }
        else
        {
            ClosedProfitPercentage = "";
            ClosedProfitClass = "";
        }

        decimal openVirtualProfit = currentValue - investedInTrades;
        var totalData = new PositionStats
        {
            Positions = openData.Positions + closedData.Positions,
            Invested = openData.Invested + closedData.Invested,
            Returned = openData.Returned + closedData.Returned,
            Commission = openData.Commission + closedData.Commission,
            TotalProfit = closedData.TotalProfit + openVirtualProfit,
        };
        TotalData = totalData;

        if (totalData.Invested > 0)
        {
            decimal totalPct = 100 * (totalData.TotalProfit / totalData.Invested);
            TotalProfitPercentage = totalPct.ToString("N2") + "%";
            TotalProfitClass = ColorClassForValue(totalPct);
        }
        else
        {
            TotalProfitPercentage = "";
            TotalProfitClass = "";
        }

        DataChanged?.Invoke();
    }

    private static void QueryPositionData(string quote, out PositionStats openData, out PositionStats closedData, out List<DailyPositionData> dailyData)
    {
        openData = new PositionStats();
        closedData = new PositionStats();
        dailyData = [];

        try
        {
            StringBuilder builder = new();
            builder.AppendLine("select date(position.CloseTime,'localtime') as CloseTime,");
            builder.AppendLine("symbol.quote, count(position.id) as Positions,");
            // Duration (hours) and profit percentage per day, needed for the duration and
            // percentage charts the Avalonia dashboard shows.
            builder.AppendLine("round(MIN(ROUND((JULIANDAY(position.CloseTime) - JULIANDAY(position.CreateTime)) * 86400 / 3600)), 2) AS MinDuration,");
            builder.AppendLine("round(AVG(ROUND((JULIANDAY(position.CloseTime) - JULIANDAY(position.CreateTime)) * 86400 / 3600)), 2) AS AvgDuration,");
            builder.AppendLine("round(MAX(ROUND((JULIANDAY(position.CloseTime) - JULIANDAY(position.CreateTime)) * 86400 / 3600)), 2) AS MaxDuration,");
            builder.AppendLine("min(position.Percentage) as MinPercentage,");
            builder.AppendLine("avg(position.Percentage) as AvgPercentage,");
            builder.AppendLine("max(position.Percentage) as MaxPercentage,");
            builder.AppendLine("sum(position.Invested) as Invested,");
            builder.AppendLine("sum(position.Returned) as Returned,");
            builder.AppendLine("sum(position.Commission) as Commission,");
            builder.AppendLine("sum(position.Profit) as TotalProfit");
            builder.AppendLine("from Position");
            builder.AppendLine("inner join symbol on Position.symbolid = symbol.id");
            builder.AppendLine("where position.Invested > 0");
            builder.AppendLine("and position.Status in (0,1,2,3)");
            builder.AppendLine($"and symbol.quote = @quote");
            builder.AppendLine("group by date(position.CloseTime,'localtime'), symbol.quote");
            builder.AppendLine("order by date(position.CloseTime,'localtime'), position.Status, symbol.quote");

            using CryptoDatabase database = new();
            database.Open();

            foreach (var row in database.Connection.Query<QueryRow>(builder.ToString(), new { quote }))
            {
                if (row.CloseTime.HasValue)
                {
                    dailyData.Add(new DailyPositionData
                    {
                        Date = row.CloseTime.Value,
                        Positions = row.Positions,
                        TotalProfit = row.TotalProfit,
                        Invested = row.Invested,
                        Returned = row.Returned,
                        MinPercentage = row.MinPercentage,
                        AvgPercentage = row.AvgPercentage,
                        MaxPercentage = row.MaxPercentage,
                        MinDuration = row.MinDuration,
                        AvgDuration = row.AvgDuration,
                        MaxDuration = row.MaxDuration,
                    });
                    closedData.Positions += row.Positions;
                    closedData.Invested += row.Invested;
                    closedData.Returned += row.Returned;
                    closedData.Commission += row.Commission;
                    closedData.TotalProfit += row.TotalProfit;
                }
                else
                {
                    openData.Positions = row.Positions;
                    openData.Invested = row.Invested;
                    openData.Returned = row.Returned;
                    openData.Commission = row.Commission;
                }
            }
        }
        catch
        {
        }
    }

    private static string ColorClassForValue(decimal value)
    {
        if (value > 0) return "text-green";
        if (value < 0) return "text-red";
        return "";
    }

    private class QueryRow
    {
        public DateTime? CloseTime { get; set; }
        public string Quote { get; set; } = "";
        public int Positions { get; set; }
        public decimal Invested { get; set; }
        public decimal Returned { get; set; }
        public decimal Commission { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal MinPercentage { get; set; }
        public decimal AvgPercentage { get; set; }
        public decimal MaxPercentage { get; set; }
        public double MinDuration { get; set; }
        public double AvgDuration { get; set; }
        public double MaxDuration { get; set; }
    }
}

public class PositionStats
{
    public int Positions { get; set; }
    public decimal Invested { get; set; }
    public decimal Returned { get; set; }
    public decimal Commission { get; set; }
    public decimal TotalProfit { get; set; }

    public decimal AverageProfit => Positions > 0 ? TotalProfit / Positions : 0m;

    public string Format(decimal value, string format = "N2") => value.ToString(format);
}

public class DailyPositionData
{
    public DateTime Date { get; set; }
    public int Positions { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal Invested { get; set; }
    public decimal Returned { get; set; }
    public decimal MinPercentage { get; set; }
    public decimal AvgPercentage { get; set; }
    public decimal MaxPercentage { get; set; }
    public double MinDuration { get; set; }
    public double AvgDuration { get; set; }
    public double MaxDuration { get; set; }
}
