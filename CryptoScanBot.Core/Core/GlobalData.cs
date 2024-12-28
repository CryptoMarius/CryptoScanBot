using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.Core.Signal;
using CryptoScanBot.Core.TradingView;
using CryptoScanBot.Core.Zones;

using Dapper;
using Dapper.Contrib.Extensions;

using System.Globalization;
using System.Text.Json;

namespace CryptoScanBot.Core.Core;


/// <summary>
/// Om vanuit de threads tekst in het main scherm te zetten
/// </summary>
public delegate void AddTextEvent(string text);

public delegate void PlayMediaEvent(string text, bool test = false);

/// <summary>
/// Om vanuit de threads de timer voor ophalen Candles te disablen
/// </summary>
public delegate void SetCandleTimerEnable(bool value);


public static class GlobalData
{
    public static string AppName { get; set; } = "";
    public static string AppPath { get; set; } = "";
    public static string LogName { get; set; } = "";
    public static string AppVersion { get; set; } = "";
    private static string? AppDataFolder { get; set; } = ""; // depends on starup parameters

    public static bool ApplicationIsShowed { get; set; } = false;
    public static bool ApplicationIsClosing { get; set; } = false;

    // Emulator kan alleen de backTest zetten (anders gaan er onverwachte zaken naar de database enzo)
    // (staat op de nominatie om te vervallen dmv de Trading.TradeVia == BackTest of GlobalData.ActiveAccount)
    // todo cleanup? (more and more properties voor backtest for the candle and candle.Close date)
    public static bool BackTest { get; set; }
    public static DateTime BackTestDateTime { get; set; }
    public static CryptoCandle? BackTestCandle { get; set; }

    public static DateTime GetCurrentDateTime(CryptoAccount account)
    {
        if (account.AccountType == CryptoAccountType.BackTest)
            return BackTestDateTime; // or BackTestCandle.OpenTime + 1 minute
        else
            return DateTime.UtcNow;
    }

    private static CryptoApplicationStatus _applicationStatus = CryptoApplicationStatus.Initializing;
    public static CryptoApplicationStatus ApplicationStatus
    {
        get { return _applicationStatus; }
        set { _applicationStatus = value; StatusesHaveChangedEvent?.Invoke(""); }
    }

    public static int CreatedSignalCount { get; set; } // Tellertje met het aantal meldingen (komt in de taakbalk c.q. applicatie titel)

    /// <summary>
    /// Alle instellingen van de scanner/trader
    /// </summary>
    public static SettingsBasic Settings { get; set; } = new();

    /// <summary>
    /// Exchange API settings
    /// </summary>
    public static SettingsExchangeApi TradingApi { get; set; } = new();

    /// <summary>
    /// Altrady API settings
    /// </summary>
    public static SettingsAltradyApi AltradyApi { get; set; } = new();

    /// <summary>
    /// Settings for visability and widths of columns + window coordinates
    /// </summary>
    public static SettingsUser SettingsUser { get; set; } = new();

    /// <summary>
    /// Telegram gerelateerde instellingen
    /// </summary>
    public static SettingsTelegram Telegram { get; set; } = new();

    /// <summary>
    /// De url's van de exchanges en/of tradingapps
    /// </summary>
    public static CryptoExternalUrlList ExternalUrls { get; set; } = [];

    public static List<CryptoInterval> IntervalList { get; } = [];
    public static SortedList<int, CryptoInterval> IntervalListId { get; } = [];
    public static SortedList<string, CryptoInterval> IntervalListPeriodName { get; } = [];
    public static SortedList<CryptoIntervalPeriod, CryptoInterval> IntervalListPeriod { get; } = [];

    // Exchanges indexed on name
    public static readonly SortedList<int, Model.CryptoExchange> ExchangeListId = [];
    public static readonly SortedList<string, Model.CryptoExchange> ExchangeListName = [];

    public static readonly Queue<CryptoSignal> SignalQueue = new();
    public static readonly List<CryptoPosition> PositionsClosed = [];
    public static readonly Queue<CryptoWhatever> WhateverQueue = [];
    public static readonly Dictionary<(string, CryptoIntervalPeriod), bool> WhateverQueueAdded = [];

    public static event PlayMediaEvent? PlaySound;
    public static event PlayMediaEvent? PlaySpeech;
    public static event AddTextEvent? LogToTelegram;
    public static event AddTextEvent? LogToLogTabEvent;

    // Events for refresing data
    public static event AddTextEvent? SymbolsHaveChangedEvent;
    public static event AddTextEvent? TelegramHasChangedEvent;
    public static event AddTextEvent? AssetsHaveChangedEvent;
    public static event AddTextEvent? PositionsHaveChangedEvent;
    public static AddTextEvent? ApplicationHasStarted { get; set; }
    public static AddTextEvent? StatusesHaveChangedEvent { get; set; }

    // Ophalen van historische candles duurt lang, dus niet halverwege nog 1 starten (en nog 1 en...)
    public static event SetCandleTimerEnable? SetCandleTimerEnableEvent;

    public static AnalyseEvent? AnalyzeSignalCreated { get; set; }


    // All possible account (overkill)
    public static readonly SortedList<int, CryptoAccount> TradeAccountList = [];
    public static CryptoAccount? ActiveAccount { get; set; }


    // Some running tasks/threads
    public static ThreadSaveObjects? ThreadSaveObjects { get; set; }
    public static ThreadMonitorCandle? ThreadMonitorCandle { get; set; }
    public static ThreadMonitorOrder? ThreadMonitorOrder { get; set; }
    public static ThreadCheckFinishedPosition? ThreadCheckPosition { get; set; }
    public static ThreadZoneCalculate? ThreadZoneCalculate { get; set; }


    // On special request of a hardcore trader..
    public static SymbolValue FearAndGreedIndex { get; set; } = new();
    public static SymbolValue TradingViewDollarIndex { get; set; } = new();
    public static SymbolValue TradingViewSpx500 { get; set; } = new();
    public static SymbolValue TradingViewBitcoinDominance { get; set; } = new();
    public static SymbolValue TradingViewMarketCapTotal { get; set; } = new();

    public static void LoadExchanges()
    {
        // Load & index the exchanges
        //AddTextToLogTab("Reading exchange information");

        ExchangeListId.Clear();
        ExchangeListName.Clear();

        using var database = new CryptoDatabase();
        foreach (Model.CryptoExchange exchange in database.Connection.GetAll<Model.CryptoExchange>())
        {
            if (exchange.IsSupported)
                AddExchange(exchange);
        }
    }

    public static void LoadAccounts()
    {
        // Load & index the accounts
        //AddTextToLogTab("Reading account information");

        TradeAccountList.Clear();

        using var database = new CryptoDatabase();
        foreach (CryptoAccount tradeAccount in database.Connection.GetAll<CryptoAccount>())
        {
            if (ExchangeListId.TryGetValue(tradeAccount.ExchangeId, out var exchange))
            {
                TradeAccountList.Add(tradeAccount.Id, tradeAccount);
                tradeAccount.Exchange = exchange;
            }
        }

        SetTradingAccounts();
    }

    public static void SetTradingAccounts()
    {
        ActiveAccount = null;
        foreach (CryptoAccount tradeAccount in TradeAccountList.Values)
        {
            // There are 3 accounts per exchange
            if (tradeAccount.ExchangeId == Settings.General.ExchangeId)
            {
                if (BackTest && tradeAccount.AccountType == CryptoAccountType.BackTest) // ignore the GlobalData.Settings.Trading.TradeVia in this case
                    ActiveAccount = tradeAccount;
                if (!BackTest && tradeAccount.AccountType == CryptoAccountType.PaperTrade && Settings.Trading.TradeVia == CryptoAccountType.PaperTrade)
                    ActiveAccount = tradeAccount;
                if (!BackTest && tradeAccount.AccountType == CryptoAccountType.RealTrading && Settings.Trading.TradeVia == CryptoAccountType.RealTrading)
                    ActiveAccount = tradeAccount;
                if (!BackTest && tradeAccount.AccountType == CryptoAccountType.Altrady && Settings.Trading.TradeVia == CryptoAccountType.Altrady)
                    ActiveAccount = tradeAccount;
            }
        }
    }


    public static void LoadIntervals()
    {
        // Load & index all the available intervals
        //AddTextToLogTab("Reading interval information");

        IntervalList.Clear();
        IntervalListId.Clear();
        IntervalListPeriod.Clear();
        IntervalListPeriodName.Clear();

        using var database = new CryptoDatabase();
        foreach (CryptoInterval interval in database.Connection.GetAll<CryptoInterval>())
        {
            IntervalList.Add(interval);
            IntervalListId.Add(interval.Id, interval);
            IntervalListPeriodName.Add(interval.Name, interval);
            IntervalListPeriod.Add(interval.IntervalPeriod, interval);
        }


        // De ContructFrom object koppelen
        foreach (CryptoInterval interval in IntervalList)
        {
            if (interval.ConstructFromId > 0)
                interval.ConstructFrom = IntervalListId[(int)interval.ConstructFromId];
        }

        // In MSSQL staan ze niet in dej uiste volgorde (vanwege het toevoegen van 2 intervallen)
        IntervalList.Sort((x, y) => x.IntervalPeriod.CompareTo(y.IntervalPeriod));
    }

    public static void LoadSymbols()
    {
        // De symbols uit de database lezen (ook van andere exchanges)
        // Dat doen we om de symbol van voorgaande signalen en/of posities te laten zien
        //AddTextToLogTab("Reading symbol information");
        string sql = "select * from symbol";
        using var database = new CryptoDatabase();
        foreach (CryptoSymbol symbol in database.Connection.Query<CryptoSymbol>(sql))
            AddSymbol(symbol);
    }

    public static void LoadSignals()
    {
        //GlobalData.AddTextToLogTab("Reading some signals");

        if (BackTest)
        {
            string sql = "select * from signal where BackTest=1 order by OpenDate";

            using var database = new CryptoDatabase();
            foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql))
            {
                if (ExchangeListId.TryGetValue(signal.ExchangeId, out Model.CryptoExchange? exchange2))
                {
                    signal.Exchange = exchange2;

                    if (exchange2.SymbolListId.TryGetValue(signal.SymbolId, out CryptoSymbol? symbol))
                    {
                        signal.Symbol = symbol;

                        if (IntervalListId.TryGetValue(signal.IntervalId, out CryptoInterval? interval))
                            signal.Interval = interval;

                        SignalQueue.Enqueue(signal);
                    }
                }
            }
        }
        else
        {

            using var database = new CryptoDatabase();
            string sql = "select * from signal where BackTest=0 and ExpirationDate >= @FromDate order by OpenDate";
            foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql, new { FromDate = DateTime.UtcNow }))
            {
                if (ExchangeListId.TryGetValue(signal.ExchangeId, out Model.CryptoExchange? exchange2))
                {
                    signal.Exchange = exchange2;

                    if (exchange2.SymbolListId.TryGetValue(signal.SymbolId, out CryptoSymbol? symbol))
                    {
                        signal.Symbol = symbol;

                        if (IntervalListId.TryGetValue(signal.IntervalId, out CryptoInterval? interval))
                            signal.Interval = interval;

                        SignalQueue.Enqueue(signal);
                    }
                }
            }
        }
    }


    public static void AddExchange(Model.CryptoExchange exchange)
    {
        if (!ExchangeListName.ContainsKey(exchange.Name))
        {
            ExchangeListId.Add(exchange.Id, exchange);
            ExchangeListName.Add(exchange.Name, exchange);
        }
    }


    public static CryptoQuoteData AddQuoteData(string quoteName)
    {
        if (!Settings.QuoteCoins.TryGetValue(quoteName, out CryptoQuoteData? quoteData))
        {
            quoteData = new CryptoQuoteData
            {
                Name = quoteName,
                DisplayFormat = "N8",
            };

            if (quoteName.Equals("USDT") || quoteName.Equals("BUSD"))
                quoteData.DisplayFormat = "N2";

            Settings.QuoteCoins.Add(quoteName, quoteData);
        }

        if (quoteName.Equals("USDT") || quoteName.Equals("BUSD"))
            quoteData.DisplayFormat = "N2";

        return quoteData;
    }

    public static void AddSymbol(CryptoSymbol symbol)
    {
#if LIMITSYMBOLS
        // Test with limits anount of symbols for debugging purposes
        if (
            symbol.Name.Equals("BTCUSDT") ||
            symbol.Name.Equals("ETHUSDT") ||
            symbol.Name.Equals("SOLUSDT") ||
            symbol.Name.Equals("TRXUSDT") ||
            symbol.Name.Equals("ENAUSDT") ||
            symbol.Name.Equals("APEXUSDT") ||
            symbol.Name.StartsWith("$BMP")
          )
#endif

        if (ExchangeListId.TryGetValue(symbol.ExchangeId, out Model.CryptoExchange? exchange))
        {
            symbol.Exchange = exchange;

            if (symbol.Name == "" || exchange.SymbolListId.ContainsKey(symbol.Id))
            {
                //TODO: Delete the symbol? (first report all of them.......)
                AddTextToLogTab($"DUPLICATE SYMBOL {exchange.Name} #{symbol.Id} {symbol.Name} {symbol.Base}/{symbol.Quote}?");
            }


            if (!exchange.SymbolListId.ContainsKey(symbol.Id))
                exchange.SymbolListId.Add(symbol.Id, symbol);

            if (!exchange.SymbolListName.ContainsKey(symbol.Name))
                exchange.SymbolListName.Add(symbol.Name, symbol);

            // Een referentie naar de globale quote data opzoeken of aanmaken
            symbol.QuoteData = AddQuoteData(symbol.Quote);


            string seperator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            // Niet de ideale wereld
            int numberOfDecimalPlaces;
            string s = symbol.PriceTickSize.ToString0();
            int x = s.IndexOf(seperator);
            if (x > 0)
            {
                s = s[(x + 1)..];
                numberOfDecimalPlaces = s.Length;
            }
            else numberOfDecimalPlaces = 0;
            symbol.PriceDisplayFormat = "N" + numberOfDecimalPlaces.ToString();
            //if (symbol.PriceDisplayFormat == "N0")
            //    symbol.PriceDisplayFormat = "N8";



            s = symbol.QuantityTickSize.ToString0();
            x = s.IndexOf(seperator);
            if (x > 0)
            {
                s = s[(x + 1)..];
                numberOfDecimalPlaces = s.Length;
            }
            else numberOfDecimalPlaces = 0;
            symbol.QuantityDisplayFormat = "N" + numberOfDecimalPlaces.ToString();
            //if (symbol.QuantityTickSize == 1.0m)
            //    symbol.QuantityDisplayFormat = "N8";

            // reset last prices
            symbol.AskPrice = null;
            symbol.BidPrice = null;
            symbol.LastPrice = null;
        }
    }



    public static void LoadBaseSettings()
    {
        try
        {
            string filename = GetBaseDir() + $"{AppName}-settings.json";
            if (File.Exists(filename))
            {
                //using (FileStream readStream = new FileStream(filename, FileMode.Open))
                //{
                //    BinaryFormatter formatter = new BinaryFormatter();
                //    Settings = (Settings)formatter.Deserialize(readStream);
                //    readStream.Close();
                //}
                //string text = File.ReadAllText(filename);
                //var value = JsonSerializer.Deserialize<SettingsBasic>(text, JsonTools.DeSerializerOptions);
                using FileStream stream = File.OpenRead(filename);
                var value = JsonSerializer.Deserialize<SettingsBasic>(stream, JsonTools.DeSerializerOptions);
                if (value != null)
                    Settings = value;
                else
                    Settings = new();
            }

            // Fix, sometimes people set this at 1 and that is not what I expected
            if (Settings!.General.GetCandleInterval < 30)
                Settings.General.GetCandleInterval = 30;

            if (Settings.General.ActivateExchangeName == "")
                Settings.General.ActivateExchangeName = Settings.General.ExchangeName;

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab("Error loading setting " + error.ToString());
        }
    }


    public static void LoadLinkSettings()
    {
        string filename = $"{AppName}-weblinks.json";
        try
        {
            string fullName = GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                var value = JsonSerializer.Deserialize<CryptoExternalUrlList>(text, JsonTools.DeSerializerOptions);
                if (value != null)
                    ExternalUrls = value;
                else
                    ExternalUrls = [];
                ExternalUrls!.InitializeUrls(); // add new exchanges
            }
            else
            {
                ExternalUrls = []; // start from scratch (do not cache in memory)
                ExternalUrls.InitializeUrls(); // add new exchanges
                // het bestand in ieder geval aanmaken(updates moeten achteraf gepushed worden)
                string text = JsonSerializer.Serialize(ExternalUrls, JsonTools.JsonSerializerIndented);
                File.WriteAllText(fullName, text);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab($"Error loading {filename} " + error.ToString());
        }
    }


    /// <summary>
    /// Window coordinates, active columns etc.
    /// </summary>
    public static void LoadUserSettings()
    {
        string filename = $"{AppName}-user.json";
        try
        {
            string fullName = GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                var value = JsonSerializer.Deserialize<SettingsUser>(text, JsonTools.DeSerializerOptions);
                if (value != null)
                    SettingsUser = value;
                else
                    SettingsUser = new();
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab($"Error loading {filename} " + error.ToString());
        }
    }

    public static void LoadTelegramSettings()
    {
        string filename = $"{AppName}-telegram.json";
        try
        {
            string fullName = GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                var value = JsonSerializer.Deserialize<SettingsTelegram>(text, JsonTools.DeSerializerOptions);
                if (value != null)
                    Telegram = value;
                else
                    Telegram = new();
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab($"Error loading {filename} " + error.ToString());
        }
    }

    public static void LoadExchangeSettings()
    {
        string filename = $"{AppName}-exchange.json";
        try
        {
            string fullName = GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                var value = JsonSerializer.Deserialize<SettingsExchangeApi>(text, JsonTools.DeSerializerOptions);
                if (value != null)
                    TradingApi = value;
                else
                    TradingApi = new();
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab($"Error loading {filename} " + error.ToString());
        }


        filename = $"{AppName}-altrady.json";
        try
        {
            string fullName = GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                var value = JsonSerializer.Deserialize<SettingsAltradyApi>(text, JsonTools.DeSerializerOptions);
                if (value != null)
                    AltradyApi = value;
                else
                    AltradyApi = new();
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab($"Error loading {filename} " + error.ToString());
        }
    }



    public static void LoadSettings()
    {
        LoadBaseSettings();
        LoadExchangeSettings();
        LoadTelegramSettings();
        LoadLinkSettings();
        LoadUserSettings();
    }


    public static void DefaultSettings()
    {
        // Apply some defaults
        if (Settings.QuoteCoins.Count == 0)
        {
            CryptoQuoteData quote = new()
            {
                Name = "ETH",
                FetchCandles = false,
                MinimalVolume = 6500000,
                MinimalPrice = 0.00000001m
            };
            Settings.QuoteCoins.Add(quote.Name, quote);

            quote = new CryptoQuoteData
            {
                Name = "USDT",
                FetchCandles = true,
                MinimalVolume = 6500000,
                MinimalPrice = 0.00000001m
            };
            Settings.QuoteCoins.Add(quote.Name, quote);

            quote = new CryptoQuoteData
            {
                Name = "BTC",
                FetchCandles = false,
                MinimalVolume = 250,
                MinimalPrice = 0.00000001m
            };
            Settings.QuoteCoins.Add(quote.Name, quote);
        }
    }

    public static void SaveUserSettings()
    {
        var baseFolder = GetBaseDir();
        Directory.CreateDirectory(baseFolder);
        var filename = baseFolder + $"{AppName}-user.json";
        string text = JsonSerializer.Serialize(SettingsUser, JsonTools.JsonSerializerIndented);
        File.WriteAllText(filename, text);
    }

    public static void SaveSettings()
    {
        string baseFolder = GetBaseDir();
        Directory.CreateDirectory(baseFolder);

        //using (FileStream writeStream = new FileStream(filename, FileMode.Create))
        //{
        //    BinaryFormatter formatter = new BinaryFormatter();
        //    formatter.Serialize(writeStream, GlobalData.Settings);
        //    writeStream.Close();
        //}

        string filename = baseFolder + $"{AppName}-settings.json";
        string text = JsonSerializer.Serialize(Settings, JsonTools.JsonSerializerIndented);
        File.WriteAllText(filename, text);

        filename = baseFolder + $"{AppName}-telegram.json";
        text = JsonSerializer.Serialize(Telegram, JsonTools.JsonSerializerIndented);
        File.WriteAllText(filename, text);

        filename = baseFolder + $"{AppName}-exchange.json";
        text = JsonSerializer.Serialize(TradingApi, JsonTools.JsonSerializerIndented);
        File.WriteAllText(filename, text);

        filename = baseFolder + $"{AppName}-altrady.json";
        text = JsonSerializer.Serialize(AltradyApi, JsonTools.JsonSerializerIndented);
        File.WriteAllText(filename, text);

        //#if DEBUG
        //        //// Ter debug om te zien of alles okay is
        //        filename = GlobalData.GetBaseDir();
        //        Directory.CreateDirectory(filename);
        //        filename += "settingsSignalsCompiled.json";
        //        text = JsonSerializer.Serialize(TradingConfig.Signals, options);
        //        File.WriteAllText(filename, text);

        //        filename = GlobalData.GetBaseDir();
        //        Directory.CreateDirectory(filename);
        //        filename += "settingsTradingCompiled.json";
        //        text = JsonSerializer.Serialize(TradingConfig.Trading, options);
        //        File.WriteAllText(filename, text);
        //#endif
    }


    public static void PlaySomeMusic(string text, bool test = false)
    {
        try
        {
            PlaySound?.Invoke(text, test);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab("Error playing music " + error.ToString());
        }
    }

    public static void PlaySomeSpeech(string text, bool test = false)
    {
        try
        {
            PlaySpeech?.Invoke(text, test);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab("Error playing speech " + error.ToString());
        }
    }

    public static void AddTextToTelegram(string text)
    {
        if (!BackTest)
        {
            try
            {
                LogToTelegram?.Invoke(text);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                AddTextToLogTab(" error telegram thread(1)" + error.ToString());
            }
        }
    }

    public static void AddTextToTelegram(string text, CryptoPosition position)
    {
        if (!BackTest)
        {
            if (LogToTelegram is null)
                return;
            try
            {
                if (position is not null)
                {
                    string symbol = position.Symbol.Name.ToUpper();
                    (string Url, CryptoExternalUrlType Execute) = ExternalUrls.GetExternalRef(Settings.General.TradingApp, true, position.Symbol, position.Interval!);
                    if (Url != "")
                    {
                        string x = $"<a href='{Url}'>{symbol}</a>";
                        text = text.Replace(symbol, x);
                    }
                }
                LogToTelegram(text);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                AddTextToLogTab(" error telegram thread(1)" + error.ToString());
            }
        }
    }

    //public static void AddTextToTelegram(string text, CryptoSymbol symbol)
    //{
    //    if (LogToTelegram is null)
    //        return;
    //    try
    //    {
    //        if (symbol is not null)
    //        {
    //            string symbolName = symbol.Name.ToUpper();
    //            (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(Settings.General.TradingApp, true, symbol, IntervalList[0]);
    //            if (Url != "")
    //            {
    //                string x = $"<a href='{Url}'>{symbolName}</a>";
    //                text = text.Replace(symbolName, x);
    //            }
    //        }
    //        LogToTelegram(text);
    //    }
    //    catch (Exception error)
    //    {
    //        ScannerLog.Logger.Error(error, "");
    //        AddTextToLogTab(" error telegram thread(2)" + error.ToString());
    //    }
    //}

    public static void AddTextToLogTab(string text) => LogToLogTabEvent?.Invoke(text);
    public static void StatusesHaveChanged(string text) => StatusesHaveChangedEvent?.Invoke(text);
    public static void SymbolsHaveChanged(string text) => SymbolsHaveChangedEvent?.Invoke(text);

    public static void AssetsHaveChanged(string text) => AssetsHaveChangedEvent?.Invoke(text);
    public static void PositionsHaveChanged(string text) => PositionsHaveChangedEvent?.Invoke(text);

    public static void TelegramHasChanged(string text) => TelegramHasChangedEvent?.Invoke(text);
    public static void SetCandleTimerEnable(bool value) => SetCandleTimerEnableEvent?.Invoke(value);


    public static string GetBaseDir()
    {
        if (string.IsNullOrEmpty(AppDataFolder))
        {
            ApplicationParams.InitApplicationOptions();
            AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ApplicationParams.Options?.AppDataFolder ?? AppName);
            Directory.CreateDirectory(AppDataFolder);
            AppDataFolder += @"\";
        }
        return AppDataFolder;
    }


    public static void InitializeExchange()
    {
        // If application params contain an exchange this is leading
        // Otherwise we take the one from the settings
        string? exchangeName = ApplicationParams.Options!.ExchangeName;
        if (exchangeName != null)
        {
            // Migration (not needed but its cheap)
            if (exchangeName == "Binance")
                exchangeName = "Binance Spot";
            if (exchangeName == "Bybit")
                exchangeName = "Bybit Spot";
            if (exchangeName == "Kraken")
                exchangeName = "Kraken Spot";
            if (exchangeName == "Kucoin")
                exchangeName = "Kucoin Spot";
            if (exchangeName == "Mexc")
                exchangeName = "Mexc Spot";

            // People forget to use the right casing
            exchangeName = exchangeName.Trim().ToLower();
            string? found = ExchangeListName.Values.Where(x => x.Name.Equals(exchangeName, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault()?.Name;
            if (found != null)
                exchangeName = found;

            // New exchange
            Settings.General.ExchangeName = exchangeName;
        }


        if (ExchangeListName.TryGetValue(Settings.General.ExchangeName, out var exchange))
        {
            Settings.General.Exchange = exchange;
            Settings.General.ExchangeId = exchange.Id;
            Settings.General.ExchangeName = exchange.Name;
        }
        else throw new Exception($"Exchange {exchangeName} bestaat niet");
    }


    //public static void DumpSessionInformation()
    //{
    //    foreach (Model.CryptoExchange exchange in ExchangeListName.Values.ToList())
    //    {
    //        int candleCount = 0;
    //        foreach (Model.CryptoSymbol symbol in exchange.SymbolListName.Values.ToList())
    //        {
    //            foreach (Model.CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList.ToList())
    //            {
    //                candleCount += symbolInterval.CandleList.Count;
    //                if (symbolInterval.CandleList.Count > 0)
    //                    AddTextToLogTab(string.Format("{0} {1} {2} candlecount={3}", exchange.Name, symbol.Name, symbolInterval.Interval.Name, symbolInterval.CandleList.Count), false);

    //            }
    //        }

    //        AddTextToLogTab(string.Format("{0} symbolcount={1} candlecount={2}", exchange.Name, exchange.SymbolListName.Count, candleCount), false);
    //    }
    //}
}
