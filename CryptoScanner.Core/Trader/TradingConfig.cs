using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Core.Trader;


public static class TradingConfig
{
    public static Dictionary<CryptoTradeSide, SettingsCompiled> Signals { get; } = [];
    public static Dictionary<CryptoTradeSide, SettingsCompiled> Trading { get; } = [];



    //// signals/trading
    //// advantage 1: seperate intervals per strategy (which I prefer in the end)
    //// advantage 2: dlz and fvg can be run on seperate 1m interval
    //public static Dictionary<CryptoSignalStrategy, List<CryptoInterval>> Signaling { get; set; } = [];


    static TradingConfig()
    {
        Signals.Add(CryptoTradeSide.Long, new SettingsCompiled());
        Signals.Add(CryptoTradeSide.Short, new SettingsCompiled());

        Trading.Add(CryptoTradeSide.Long, new SettingsCompiled());
        Trading.Add(CryptoTradeSide.Short, new SettingsCompiled());
    }

    public static void IndexStrategyInternally()
    {
        Signals[CryptoTradeSide.Long].IndexStrategyInternally(GlobalData.Settings.Signal.Long, CryptoTradeSide.Long);
        Signals[CryptoTradeSide.Short].IndexStrategyInternally(GlobalData.Settings.Signal.Short, CryptoTradeSide.Short);

        Trading[CryptoTradeSide.Long].IndexStrategyInternally(GlobalData.Settings.Trading.Long, CryptoTradeSide.Long);
        Trading[CryptoTradeSide.Short].IndexStrategyInternally(GlobalData.Settings.Trading.Short, CryptoTradeSide.Short);
    }


    public static void InitWhiteAndBlackListHelper(List<string> list, SortedList<string, bool> target, string caption)
    {
        // Het 1e woord is de symbol
        char[] delimiterChars = [' ', ',', '-', '.', ':', '\t'];

        target.Clear();
        foreach (string text in list)
        {
            string[] words = text.Split(delimiterChars);
            if (words.Length > 0)
            {
                string symbol = words[0].ToUpper().Trim();
                if (symbol != "")
                {
                    if (!target.ContainsKey(symbol))
                        target.Add(symbol, true);

                    var exchange = GlobalData.ActiveExchange;
                    if (exchange != null)
                    {
                        if (!exchange.SymbolListName.ContainsKey(symbol))
                        {
                            if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running && !string.IsNullOrEmpty(caption))
                                GlobalData.AddTextToLogTab($"Error {caption} {symbol} does not exist!");
                        }
                    }
                }
            }
        }
    }

    public static void InitWhiteAndBlackListSettings()
    {
        // Initialize *signal* black en whitelist
        InitWhiteAndBlackListHelper(GlobalData.Settings.BlackListOversold, Signals[CryptoTradeSide.Long].BlackList, "BlackList.Long");
        InitWhiteAndBlackListHelper(GlobalData.Settings.WhiteListOversold, Signals[CryptoTradeSide.Long].WhiteList, "WhiteList.Long");
        InitWhiteAndBlackListHelper(GlobalData.Settings.BlackListOverbought, Signals[CryptoTradeSide.Short].BlackList, "BlackList.Short");
        InitWhiteAndBlackListHelper(GlobalData.Settings.WhiteListOverbought, Signals[CryptoTradeSide.Short].WhiteList, "WhiteList.Short");

        // Initialize *trading* black en whitelist (we already have the "symbols does not exist!")
        InitWhiteAndBlackListHelper(GlobalData.Settings.BlackListOversold, Trading[CryptoTradeSide.Long].BlackList, "");
        InitWhiteAndBlackListHelper(GlobalData.Settings.WhiteListOversold, Trading[CryptoTradeSide.Long].WhiteList, "");
        InitWhiteAndBlackListHelper(GlobalData.Settings.BlackListOverbought, Trading[CryptoTradeSide.Short].BlackList, "");
        InitWhiteAndBlackListHelper(GlobalData.Settings.WhiteListOverbought, Trading[CryptoTradeSide.Short].WhiteList, "");
    }


}
