using CryptoScanBot.Core.Model;

using Newtonsoft.Json;

using System.Text;

namespace CryptoScanBot.BackTest;

public class CryptoBackTestResults
{
    public int WinCount = 0;
    public int LossCount = 0;

    public decimal Invested = 0m;
    public decimal Returned = 0m;
    public decimal Commission = 0m;

    public double TimeTotal = 0;
    public double TimeLongest = 0;
    public double TimeShortest = 0;

    //public decimal ProfitAverage = 0;
    public decimal ProfitLowest = 0;
    public decimal ProfitHighest = 0;

    public string Quote;
    public CryptoSymbol Symbol;
    public CryptoInterval Interval;
    public CryptoBackConfig Config;

    public CryptoBackTestResults(string quote, CryptoSymbol symbol, CryptoInterval interval, CryptoBackConfig config)
    {
        this.Quote = quote;
        this.Symbol = symbol;
        this.Interval = interval;
        this.Config = config;
        Reset();
    }

    public void Reset()
    {
        WinCount = 0;
        LossCount = 0;

        TimeTotal = 0;
        TimeLongest = 0;
        TimeShortest = int.MaxValue;

        ProfitLowest = int.MaxValue;
        //ProfitAverage = 0;
        ProfitHighest = 0;

        Invested = 0m;
        Returned = 0m;
        Commission = 0m;
    }

    public void Add(CryptoBackTestResults Results)
    {
        WinCount += Results.WinCount;
        LossCount += Results.LossCount;

        TimeTotal += Results.TimeTotal;
        if (Results.TimeShortest < TimeShortest)
            TimeShortest = Results.TimeShortest;
        if (Results.TimeLongest > TimeLongest)
            TimeLongest = Results.TimeLongest;

        if (Results.ProfitLowest < ProfitLowest)
            ProfitLowest = Results.ProfitLowest;
        if (Results.ProfitHighest > ProfitHighest)
            ProfitHighest = Results.ProfitHighest;

        Invested += Results.Invested;
        Returned += Results.Returned;
        Commission += Results.Commission;
    }

    public void ShowHeader(StringBuilder log, bool includeConfig = true)
    {
        if (Symbol == null)
            log.AppendLine("Backtest");
        else
        {
            log.AppendLine("Backtest " + Symbol.Name);
            log.AppendLine("Volume " + Symbol.Volume.ToString());
        }

        log.AppendLine("Interval " + Interval.Name);
        //log.AppendLine("Candles " + Candles.Count.ToString());

        log.AppendLine("");
        if (includeConfig)
        {
            // Na invullen symbols is anders circulair en dat vindt json niet fijn
            string s = JsonConvert.SerializeObject(Config, Formatting.Indented);
            log.AppendLine(s);
        }
        log.AppendLine("");
        log.AppendLine("");
    }

    public string ShowFooter(StringBuilder log)
    {
        string s;
        log.AppendLine();
        log.AppendLine();

        s = string.Format("Totaal={0:N2} uur", TimeTotal / 60);
        log.AppendLine(s);
        s = string.Format("Gemiddeld={0:N2} uur", (TimeTotal / 60) / (WinCount + LossCount));
        log.AppendLine(s);
        if (TimeShortest != int.MaxValue)
        {
            s = string.Format("Kortste={0:N2} uur", TimeShortest / 60);
            log.AppendLine(s);

            s = string.Format("Langste={0:N2} uur", TimeLongest / 60);
            log.AppendLine(s);
        }
        log.AppendLine();


        if (ProfitLowest != int.MaxValue)
        {
            s = string.Format("Laagste={0:N2}%", ProfitLowest);
            log.AppendLine(s);

            s = string.Format("Hoogste={0:N2}%", ProfitHighest);
            log.AppendLine(s);
        }
        log.AppendLine();



        decimal percentage = 0m;
        if (Invested != 0m)
            percentage = 100 * (Returned - Commission) / Invested;

        s = string.Format("Invested={0:N8}", Invested);
        if (!Quote.Equals("USDT"))
            s += string.Format(" usdt={0:N2}", Invested * Config.UsdtValue);
        log.AppendLine(s);

        s = string.Format("Returned={0:N8}", Returned);
        if (!Quote.Equals("USDT"))
            s += string.Format(" usdt={0:N2}", Returned * Config.UsdtValue);
        log.AppendLine(s);

        s = string.Format("Commission={0:N8}", Commission);
        if (!Quote.Equals("USDT"))
            s += string.Format(" usdt={0:N2}", Commission * Config.UsdtValue);
        log.AppendLine(s);

        s = string.Format("Profit={0:N8}", (Returned - Invested - Commission));
        if (!Quote.Equals("USDT"))
            s += string.Format(" usdt={0:N2}", (Returned - Invested - Commission) * Config.UsdtValue);
        log.AppendLine(s);

        s = string.Format("Percentage={0:N2}", percentage);
        log.AppendLine(s);
        log.AppendLine("");

        if (Symbol == null)
            return "";
        else
        {
            // bij benadering, exclusief fees, alles wordt gekocht en verkocht, dus 2* (zowel buy als sell) 0.0750% kosten erbij = 0.15%
            //decimal total = WinCount * Config.ProfitPercentage - LossCount * Config.StoplossPercentage;total={7,7:N2} total, 

            string t;
            if (Returned - Invested - Commission < 0)
                t = string.Format("{0,-10} {1,-10} volume={2,20:N2} win={3,5} loss={4,5} profit%={5,7:N2} profit={6:N8}",
                Symbol.Base, Symbol.Quote, Symbol.Volume, WinCount, LossCount, percentage, Returned - Invested - Commission);
            else
                t = string.Format("{0,-10} {1,-10} volume={2,20:N2} win={3,5} loss={4,5} profit%={5,7:N2} profit= {6:N8}",
                Symbol.Base, Symbol.Quote, Symbol.Volume, WinCount, LossCount, percentage, Returned - Invested - Commission);

            if (!Quote.Equals("BUSD") && !Quote.Equals("USDT"))
                t += string.Format(" profitusdt ={0,7:N2}", (Returned - Invested - Commission) * Config.UsdtValue);
            return t;
        }
    }
}