using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Signal;

namespace CryptoSbmScanner.Intern;

public enum MatchBlackAndWhiteList
{
    Empty,
    Present,
    NotPresent
}

public class TradeConfiguration // c.q. SignalMode, betere naam gewenst? Alles beter dan continue de oversold en overbought te moeten toevoegen
{
    internal SortedList<string, bool> BlackList { get; } = new();
    internal SortedList<string, bool> WhiteList { get; } = new();

    // Omdat de SBMx en Stobb van elkaar "afhankelijk" zijn moet het uitgesplitst worden
    // (deze 2 zijn altijd long of short (en zeker geen info tak)
    // als we de sbm* en stobb herschrijven als 1 methode zou dit aangepast kunnen worden (hint!)
    public SortedList<string, AlgorithmDefinition> StrategySbmStob { get; } = new();
    public SortedList<string, AlgorithmDefinition> StrategyOthers { get; } = new();

    // Geindexeerd lijstjes met de actieve strategieën
    public SortedList<CryptoSignalStrategy, AlgorithmDefinition> AnalyzeStrategy = new();
    public SortedList<CryptoSignalStrategy, AlgorithmDefinition> MonitorStrategy = new();


    public MatchBlackAndWhiteList InBlackList(string name)
    {
        if (!BlackList.Any())
            return MatchBlackAndWhiteList.Empty;

        if (BlackList.ContainsKey(name))
            return MatchBlackAndWhiteList.Present;
        else
            return MatchBlackAndWhiteList.NotPresent;
    }

    public MatchBlackAndWhiteList InWhiteList(string name)
    {
        if (!WhiteList.Any())
            return MatchBlackAndWhiteList.Empty;

        if (WhiteList.ContainsKey(name))
            return MatchBlackAndWhiteList.Present;
        else
            return MatchBlackAndWhiteList.NotPresent;
    }

    public void ClearStrategies()
    {
        StrategySbmStob.Clear();
        StrategyOthers.Clear();

        AnalyzeStrategy.Clear();
        MonitorStrategy.Clear();
    }

}


public static class TradingConfig
{
    // Geindexeerd lijstjes met de actieve intervallen
    static public SortedList<CryptoIntervalPeriod, CryptoInterval> AnalyzeInterval { get; } = new();
    static public SortedList<CryptoIntervalPeriod, CryptoInterval> MonitorInterval { get; } = new();

    // Black en white list en strategies..
    static public Dictionary<CryptoOrderSide, TradeConfiguration> Config { get; } = new();

    static public SortedList<CryptoSignalStrategy, AlgorithmDefinition> AlgorithmDefinitionIndex = new();
    static public List<AlgorithmDefinition> AlgorithmDefinitionList = SignalHelper.GetListOfAlgorithms();

    static TradingConfig()
    {
        Config.Add(CryptoOrderSide.Buy, new TradeConfiguration());
        Config.Add(CryptoOrderSide.Sell, new TradeConfiguration());
    }

    static public void IndexStrategyInternally()
    {
        AnalyzeInterval.Clear();
        MonitorInterval.Clear();
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (GlobalData.Settings.Signal.Analyze.Interval.Contains(interval.Name))
                AnalyzeInterval.Add(interval.IntervalPeriod, interval);

            if (GlobalData.Settings.Trading.Monitor.Interval.Contains(interval.Name))
                MonitorInterval.Add(interval.IntervalPeriod, interval);
        }


        // Het idee was om long en short van elkaar te scheiden, maar achteraf (en technisch) maakt het niet zo weinig uit, het 
        // zou weer samengevoegd kunnen worden (dit vanwege de black en white listen die ook voor short en long uitgesplitst zijn)
        // (soms is het goed om rte refactoren en tot nieuwe ideen te komen, right..)

        AlgorithmDefinitionIndex.Clear();
        Config[CryptoOrderSide.Buy].ClearStrategies();
        Config[CryptoOrderSide.Sell].ClearStrategies();

        foreach (AlgorithmDefinition def in AlgorithmDefinitionList)
        {
            // just for getting the name..
            AlgorithmDefinitionIndex.Add(def.Strategy, def);

            foreach (CryptoOrderSide side in Enum.GetValues(typeof(CryptoOrderSide)))
            {
                bool hasClass;
                if (side == CryptoOrderSide.Buy)
                    hasClass = def.AnalyzeLongType != null;
                else
                    hasClass = def.AnalyzeShortType != null;


                if (hasClass)
                {
                    if (GlobalData.Settings.Signal.Analyze.Strategy[side].Contains(def.Name))
                        Config[side].AnalyzeStrategy.Add(def.Strategy, def);
                    if (GlobalData.Settings.Trading.Monitor.Strategy[side].Contains(def.Name))
                        Config[side].MonitorStrategy.Add(def.Strategy, def);

                    if (GlobalData.Settings.Signal.Analyze.Strategy[side].Contains(def.Name))
                    {
                        if (def.Strategy >= CryptoSignalStrategy.Sbm1 && def.Strategy <= CryptoSignalStrategy.Stobb)
                            Config[side].StrategySbmStob.Add(def.Name, def);
                        else
                            Config[side].StrategyOthers.Add(def.Name, def);
                    }

                }
            }
        }
    }


    static public void InitWhiteAndBlackListHelper(List<string> list, SortedList<string, bool> target, string caption)
    {
        // Het 1e woord is de symbol
        char[] delimiterChars = { ' ', ',', '-', '.', ':', '\t' };

        target.Clear();
        foreach (string text in list)
        {
            string[] words = text.Split(delimiterChars);
            if (words.Length > 0)
            {
                string symbol = words[0].ToUpper();
                if (!target.ContainsKey(symbol))
                    target.Add(symbol, true);

                if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
                {
                    if (!exchange.SymbolListName.ContainsKey(symbol))
                    {
                        if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                            GlobalData.AddTextToLogTab(string.Format("FOUT {0} {1} bestaat niet", caption, symbol));
                    }
                }
            }
        }
    }

    static public void InitWhiteAndBlackListSettings()
    {
        InitWhiteAndBlackListHelper(GlobalData.Settings.BlackListOversold, Config[CryptoOrderSide.Buy].BlackList, "BlackList.Long");
        InitWhiteAndBlackListHelper(GlobalData.Settings.WhiteListOversold, Config[CryptoOrderSide.Buy].WhiteList, "WhiteList.Long");

        InitWhiteAndBlackListHelper(GlobalData.Settings.BlackListOverbought, Config[CryptoOrderSide.Sell].BlackList, "BlackList.Short");
        InitWhiteAndBlackListHelper(GlobalData.Settings.WhiteListOverbought, Config[CryptoOrderSide.Sell].WhiteList, "WhiteList.Short");
    }


}
