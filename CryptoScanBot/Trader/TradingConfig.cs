﻿using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Settings;

namespace CryptoScanBot.Trader;


public static class TradingConfig
{
    static public Dictionary<CryptoTradeSide, SettingsCompiled> Signals { get; } = new();
    static public Dictionary<CryptoTradeSide, SettingsCompiled> Trading { get; } = new();


    static TradingConfig()
    {
        Signals.Add(CryptoTradeSide.Long, new SettingsCompiled());
        Signals.Add(CryptoTradeSide.Short, new SettingsCompiled());

        Trading.Add(CryptoTradeSide.Long, new SettingsCompiled());
        Trading.Add(CryptoTradeSide.Short, new SettingsCompiled());
    }

    static public void IndexStrategyInternally()
    {
        Signals[CryptoTradeSide.Long].IndexStrategyInternally(GlobalData.Settings.Signal.Long, CryptoTradeSide.Long);
        Signals[CryptoTradeSide.Short].IndexStrategyInternally(GlobalData.Settings.Signal.Short, CryptoTradeSide.Short);

        Trading[CryptoTradeSide.Long].IndexStrategyInternally(GlobalData.Settings.Trading.Long, CryptoTradeSide.Long);
        Trading[CryptoTradeSide.Short].IndexStrategyInternally(GlobalData.Settings.Trading.Short, CryptoTradeSide.Short);
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
                string symbol = words[0].ToUpper().Trim();
                if (symbol != "")
                {
                    if (!target.ContainsKey(symbol))
                        target.Add(symbol, true);

                    if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
                    {
                        if (!exchange.SymbolListName.ContainsKey(symbol))
                        {
                            if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                                GlobalData.AddTextToLogTab($"FOUT {caption} {symbol} bestaat niet");
                        }
                    }
                }
            }
        }
    }

    static public void InitWhiteAndBlackListSettings()
    {
        // Nadat de symbol zijn ingelezen de black en whitelist initialiseren
        InitWhiteAndBlackListHelper(GlobalData.Settings.BlackListOversold, Signals[CryptoTradeSide.Long].BlackList, "BlackList.Long");
        InitWhiteAndBlackListHelper(GlobalData.Settings.WhiteListOversold, Signals[CryptoTradeSide.Long].WhiteList, "WhiteList.Long");
        InitWhiteAndBlackListHelper(GlobalData.Settings.BlackListOverbought, Signals[CryptoTradeSide.Short].BlackList, "BlackList.Short");
        InitWhiteAndBlackListHelper(GlobalData.Settings.WhiteListOverbought, Signals[CryptoTradeSide.Short].WhiteList, "WhiteList.Short");

        // De trading black en whitelist worden ook geinitialiseerd, zijn dus gelijk (voila)
        InitWhiteAndBlackListHelper(GlobalData.Settings.BlackListOversold, Trading[CryptoTradeSide.Long].BlackList, "BlackList.Long");
        InitWhiteAndBlackListHelper(GlobalData.Settings.WhiteListOversold, Trading[CryptoTradeSide.Long].WhiteList, "WhiteList.Long");
        InitWhiteAndBlackListHelper(GlobalData.Settings.BlackListOverbought, Trading[CryptoTradeSide.Short].BlackList, "BlackList.Short");
        InitWhiteAndBlackListHelper(GlobalData.Settings.WhiteListOverbought, Trading[CryptoTradeSide.Short].WhiteList, "WhiteList.Short");
    }

}
