using System.Text;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Commands;

public class CommandShowTrendInfo : CommandBase
{
    public override string CommandName()
        => "Show trend";


    public override void Execute(object sender)
    {
        if (sender is CryptoSymbol symbol)
        {

            StringBuilder log = new();
            log.AppendLine("Trend " + symbol.Name);

            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab("Trend " + symbol.Name);

            long percentageSum = 0;
            long maxPercentageSum = 0;
            foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList)
            {
                log.AppendLine("");
                log.AppendLine("----");
                log.AppendLine("Interval " + symbolInterval.Interval.Name);

                // Wat is het maximale som (voor de eindberekening)
                maxPercentageSum += symbolInterval.Interval.Duration;

                TrendInterval.Calculate(symbolInterval, 0, 0, log);
                if (symbolInterval.TrendIndicator == CryptoTrendIndicator.Bullish)
                    percentageSum += symbolInterval.Interval.Duration;
                else if (symbolInterval.TrendIndicator == CryptoTrendIndicator.Bearish)
                    percentageSum -= symbolInterval.Interval.Duration;

                string s = "";
                if (symbolInterval.TrendIndicator == CryptoTrendIndicator.Bullish)
                    s = string.Format("{0} {1}, trend=bullish", symbol.Name, symbolInterval.Interval.IntervalPeriod);
                else if (symbolInterval.TrendIndicator == CryptoTrendIndicator.Bearish)
                    s = string.Format("{0} {1}, trend=bearish", symbol.Name, symbolInterval.Interval.IntervalPeriod);
                else
                    s = string.Format("{0} {1}, trend=sideway's", symbol.Name, symbolInterval.Interval.IntervalPeriod);
                GlobalData.AddTextToLogTab(s);
                log.AppendLine(s);
            }

            if (maxPercentageSum > 0)
            {
                decimal trendPercentage = 100 * (decimal)percentageSum / maxPercentageSum;
                string t = string.Format("{0} {1:N2}", symbol.Name, trendPercentage);
                GlobalData.AddTextToLogTab(t);
                log.AppendLine(t);
            }



            //Laad de gecachte (langere historie, minder overhad)
            string filename = GlobalData.GetBaseDir() + "Trend information.txt";
            File.WriteAllText(filename, log.ToString());
        }
    }

}
