using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.Core.Signal;
using CryptoScanBot.Core.TradingView;
using Dapper;
using Dapper.Contrib.Extensions;

using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CryptoScanBot.Core.Intern;


/// <summary>
/// Om vanuit de threads tekst in het main scherm te zetten
/// </summary>
public delegate void AddTextEvent(string text);

public delegate void PlayMediaEvent(string text, bool test = false);

/// <summary>
/// Om vanuit de threads de timer voor ophalen Candles te disablen
/// </summary>
public delegate void SetCandleTimerEnable(bool value);


static public class GlobalData
{
    public static string AppName { get; set; } = "";
    public static string AppPath { get; set; } = "";
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
            return BackTestDateTime; // or BackTestCandle.Date + 1 minute
        else
            return DateTime.UtcNow;
    }

    public static CryptoApplicationStatus ApplicationStatus { get; set; } = CryptoApplicationStatus.Initializing;

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

    // On special request of a hardcore trader..
    public static SymbolValue FearAndGreedIndex { get; set; } = new();
    public static SymbolValue TradingViewDollarIndex { get; set; } = new();
    public static SymbolValue TradingViewSpx500 { get; set; } = new();
    public static SymbolValue TradingViewBitcoinDominance { get; set; } = new();
    public static SymbolValue TradingViewMarketCapTotal { get; set; } = new();

    public static readonly JsonSerializerOptions JsonSerializerIndented = new()
    { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true, IncludeFields = true };

    public static readonly JsonSerializerOptions JsonSerializerNotIndented = new()
    { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false };

    static public void LoadExchanges()
    {
        // Load & index the exchanges
        AddTextToLogTab("Reading exchange information");

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
        AddTextToLogTab("Reading account information");

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
                if (GlobalData.BackTest && tradeAccount.AccountType == CryptoAccountType.BackTest) // ignore the GlobalData.Settings.Trading.TradeVia in this case
                    ActiveAccount = tradeAccount;
                if (!GlobalData.BackTest && tradeAccount.AccountType == CryptoAccountType.PaperTrade && GlobalData.Settings.Trading.TradeVia == CryptoAccountType.PaperTrade)
                    ActiveAccount = tradeAccount;
                if (!GlobalData.BackTest && tradeAccount.AccountType == CryptoAccountType.RealTrading && GlobalData.Settings.Trading.TradeVia == CryptoAccountType.RealTrading)
                    ActiveAccount = tradeAccount;
                if (!GlobalData.BackTest && tradeAccount.AccountType == CryptoAccountType.Altrady && GlobalData.Settings.Trading.TradeVia == CryptoAccountType.Altrady)
                    ActiveAccount = tradeAccount;
            }
        }
    }


    static public void LoadIntervals()
    {
        // Load & index all the available intervals
        AddTextToLogTab("Reading interval information");

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

    static public void LoadSymbols()
    {
        // De symbols uit de database lezen (ook van andere exchanges)
        // Dat doen we om de symbol van voorgaande signalen en/of posities te laten zien
        AddTextToLogTab("Reading symbol information");
        //string sql = $"select * from symbol where exchangeid={exchange.Id}";
        string sql = "select * from symbol";

        using var database = new CryptoDatabase();
        foreach (CryptoSymbol symbol in database.Connection.Query<CryptoSymbol>(sql))
            AddSymbol(symbol);
    }

    static public void LoadSignals()
    {
        GlobalData.AddTextToLogTab("Reading some signals");

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
            string sql = "select * from signal where BackTest=0 and ExpirationDate >= @FromDate order by OpenDate";

            using var database = new CryptoDatabase();
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


    static public void AddExchange(Model.CryptoExchange exchange)
    {
        if (!ExchangeListName.ContainsKey(exchange.Name))
        {
            ExchangeListId.Add(exchange.Id, exchange);
            ExchangeListName.Add(exchange.Name, exchange);
        }
    }


    static public CryptoQuoteData AddQuoteData(string quoteName)
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

    static public void AddSymbol(CryptoSymbol symbol)
    {
#if LIMITSYMBOLS
        // Test with limits anount of symbols for debugging purposes
        if (
            symbol.Name.Equals("BTCUSDT") ||
            //symbol.Name.Equals("TRXUSDT") ||
            symbol.Name.StartsWith("$BMP")
          )
#endif

        if (ExchangeListId.TryGetValue(symbol.ExchangeId, out Model.CryptoExchange? exchange))
        {
            symbol.Exchange = exchange;

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



    static public void LoadBaseSettings()
    {
        try
        {
            string filename = GetBaseDir() + $"{GlobalData.AppName}-settings.json";
            if (File.Exists(filename))
            {
                //using (FileStream readStream = new FileStream(filename, FileMode.Open))
                //{
                //    BinaryFormatter formatter = new BinaryFormatter();
                //    Settings = (Settings)formatter.Deserialize(readStream);
                //    readStream.Close();
                //}
                string text = File.ReadAllText(filename);
                Settings = JsonSerializer.Deserialize<SettingsBasic>(text, JsonSerializerIndented);
            }

            // Fix, sometimes people set this at 1 and that is not what I expected
            if (Settings.General.GetCandleInterval < 30)
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


    static public void LoadLinkSettings()
    {
        string filename = $"{GlobalData.AppName}-weblinks.json";
        try
        {
            string fullName = GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                ExternalUrls = JsonSerializer.Deserialize<CryptoExternalUrlList>(text);
                ExternalUrls!.InitializeUrls(); // add new exchanges
            }
            else
            {
                ExternalUrls = []; // start from scratch (do not cache in memory)
                ExternalUrls.InitializeUrls(); // add new exchanges
                // het bestand in ieder geval aanmaken(updates moeten achteraf gepushed worden)
                string text = JsonSerializer.Serialize(ExternalUrls, JsonSerializerIndented);
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
    static public void LoadUserSettings()
    {
        string filename = $"{GlobalData.AppName}-user.json";
        try
        {
            string fullName = GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                SettingsUser = JsonSerializer.Deserialize<SettingsUser>(text);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab($"Error loading {filename} " + error.ToString());
        }
    }

    static public void LoadTelegramSettings()
    {
        string filename = $"{GlobalData.AppName}-telegram.json";
        try
        {
            string fullName = GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                Telegram = JsonSerializer.Deserialize<SettingsTelegram>(text);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab($"Error loading {filename} " + error.ToString());
        }
    }

    static public void LoadExchangeSettings()
    {
        string filename = $"{GlobalData.AppName}-exchange.json";
        try
        {
            string fullName = GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                TradingApi = JsonSerializer.Deserialize<SettingsExchangeApi>(text);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab($"Error loading {filename} " + error.ToString());
        }


        filename = $"{GlobalData.AppName}-altrady.json";
        try
        {
            string fullName = GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                AltradyApi = JsonSerializer.Deserialize<SettingsAltradyApi>(text);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab($"Error loading {filename} " + error.ToString());
        }
    }



    static public void LoadSettings()
    {
        LoadBaseSettings();
        LoadExchangeSettings();
        LoadTelegramSettings();
        LoadLinkSettings();
        LoadUserSettings();
    }


    static public void DefaultSettings()
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

    static public void SaveUserSettings()
    {
        var baseFolder = GetBaseDir();
        Directory.CreateDirectory(baseFolder);
        var filename = baseFolder + $"{GlobalData.AppName}-user.json";
        string text = JsonSerializer.Serialize(SettingsUser, JsonSerializerIndented);
        File.WriteAllText(filename, text);
    }

    static public void SaveSettings()
    {
        string baseFolder = GetBaseDir();
        Directory.CreateDirectory(baseFolder);

        //using (FileStream writeStream = new FileStream(filename, FileMode.Create))
        //{
        //    BinaryFormatter formatter = new BinaryFormatter();
        //    formatter.Serialize(writeStream, GlobalData.Settings);
        //    writeStream.Close();
        //}

        string filename = baseFolder + $"{GlobalData.AppName}-settings.json";
        string text = JsonSerializer.Serialize(Settings, JsonSerializerIndented);
        File.WriteAllText(filename, text);

        filename = baseFolder + $"{GlobalData.AppName}-telegram.json";
        text = JsonSerializer.Serialize(Telegram, JsonSerializerIndented);
        File.WriteAllText(filename, text);

        filename = baseFolder + $"{GlobalData.AppName}-exchange.json";
        text = JsonSerializer.Serialize(TradingApi, JsonSerializerIndented);
        File.WriteAllText(filename, text);

        filename = baseFolder + $"{GlobalData.AppName}-altrady.json";
        text = JsonSerializer.Serialize(AltradyApi, JsonSerializerIndented);
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


    static public void PlaySomeMusic(string text, bool test = false)
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

    static public void PlaySomeSpeech(string text, bool test = false)
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

    static public void AddTextToTelegram(string text)
    {
        if (!GlobalData.BackTest)
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

    static public void AddTextToTelegram(string text, CryptoPosition position)
    {
        if (!GlobalData.BackTest)
        {
            if (LogToTelegram is null)
                return;
            try
            {
                if (position is not null)
                {
                    string symbol = position.Symbol.Name.ToUpper();
                    (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(Settings.General.TradingApp, true, position.Symbol, position.Interval);
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

    //static public void AddTextToTelegram(string text, CryptoSymbol symbol)
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

    static public void AddTextToLogTab(string text) => LogToLogTabEvent?.Invoke(text);
    static public void SymbolsHaveChanged(string text) => SymbolsHaveChangedEvent?.Invoke(text);

    static public void AssetsHaveChanged(string text) => AssetsHaveChangedEvent?.Invoke(text);
    static public void PositionsHaveChanged(string text) => PositionsHaveChangedEvent?.Invoke(text);

    static public void TelegramHasChanged(string text) => TelegramHasChangedEvent?.Invoke(text);
    static public void SetCandleTimerEnable(bool value) => SetCandleTimerEnableEvent?.Invoke(value);


    static public string GetBaseDir()
    {
        if (string.IsNullOrEmpty(AppDataFolder))
        {
            ApplicationParams.InitApplicationOptions();
            AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                ApplicationParams.Options?.AppDataFolder ?? GlobalData.AppName);
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

            // New exchange
            GlobalData.Settings.General.ExchangeName = exchangeName;
        }


        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out var exchange))
        {
            GlobalData.Settings.General.Exchange = exchange;
            GlobalData.Settings.General.ExchangeId = exchange.Id;
            GlobalData.Settings.General.ExchangeName = exchange.Name;
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
