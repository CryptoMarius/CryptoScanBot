using System.Text;

using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

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
            foreach (CryptoSymbolInterval cryptoSymbolInterval in symbol.IntervalPeriodList)
            {
                log.AppendLine("");
                log.AppendLine("----");
                log.AppendLine("Interval " + cryptoSymbolInterval.Interval.Name);

                // Wat is het maximale som (voor de eindberekening)
                maxPercentageSum += cryptoSymbolInterval.Interval.Duration;

                TrendIndicator trendIndicatorClass = new(symbol, cryptoSymbolInterval)
                {
                    Log = log
                };
                // TODO Parameter voor de trendIndicatorClass.CalculateTrend goed invullen
                CryptoTrendIndicator trendIndicator = trendIndicatorClass.CalculateTrend(0);
                if (trendIndicator == CryptoTrendIndicator.Bullish)
                    percentageSum += cryptoSymbolInterval.Interval.Duration;
                else if (trendIndicator == CryptoTrendIndicator.Bearish)
                    percentageSum -= cryptoSymbolInterval.Interval.Duration;


                // Ahh, dat gaat niet naar een tabel (zoals ik eerst dacht)
                //CryptoSymbolInterval symbolInterval = signal.Symbol.GetSymbolInterval(interval.IntervalPeriod);
                //symbolInterval.TrendIndicator = trendIndicator;
                //symbolInterval.TrendInfoDate = DateTime.UtcNow;

                string s = "";
                if (trendIndicator == CryptoTrendIndicator.Bullish)
                    s = string.Format("{0} {1}, trend=bullish", symbol.Name, cryptoSymbolInterval.Interval.IntervalPeriod);
                else if (trendIndicator == CryptoTrendIndicator.Bearish)
                    s = string.Format("{0} {1}, trend=bearish", symbol.Name, cryptoSymbolInterval.Interval.IntervalPeriod);
                else
                    s = string.Format("{0} {1}, trend=sideway's", symbol.Name, cryptoSymbolInterval.Interval.IntervalPeriod);
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
