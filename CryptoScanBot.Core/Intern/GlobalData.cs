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
public delegate void AddTextEvent(string text, bool extraLineFeed = false);

public delegate void PlayMediaEvent(string text, bool test = false);

/// <summary>
/// Om vanuit de threads de timer voor ophalen Candles te disablen
/// </summary>
public delegate void SetCandleTimerEnable(bool value);



/// De laatst berekende barometer standen
public class BarometerData
{
    public long? PriceDateTime { get; set; }
    public decimal? PriceBarometer { get; set; }

    public long? VolumeDateTime { get; set; }
    public decimal? VolumeBarometer { get; set; }
}


static public class GlobalData
{
    public static string AppName { get; set; } = "";
    public static string AppPath { get; set; } = "";
    public static string AppVersion { get; set; } = "";
    private static string? AppDataFolder { get; set; } = ""; // depends on starup parameters

    public static bool ApplicationIsShowed { get; set; } = false;
    public static bool ApplicationIsClosing { get; set; } = false;

    // Emulator kan alleen de backTest zetten (anders gaan er onverwachte zaken naar de database enzo)
    // (staat op de nominatie om te vervallen dmv de Settings.Trading.TradeVia == BackTest)
    public static bool BackTest { get; set; }

    public static CryptoApplicationStatus ApplicationStatus { get; set; } = CryptoApplicationStatus.Initializing;

    public static int CreatedSignalCount { get; set; } // Tellertje met het aantal meldingen (komt in de taakbalk c.q. applicatie titel)

    /// <summary>
    /// Alle instellingen van de scanner/trader
    /// </summary>
    public static SettingsBasic Settings { get; set; } = new();

    /// <summary>
    /// API gerelateerde instellingen
    /// </summary>
    public static SettingsExchangeApi TradingApi { get; set; } = new();

    /// <summary>
    /// Instellingen zoals kolombreedte, kolom zichtbaar en laatste venster cooridinaten
    /// </summary>
    public static SettingsUser SettingsUser { get; set; } = new();

    /// <summary>
    /// Telegram gerelateerde instellingen
    /// </summary>
    public static SettingsTelegram Telegram { get; set; } = new();


    public static PauseRule PauseTrading { get; set; } = new();

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

    // Probleem, de signallist wordt nooit opgeruimd!
    public static readonly List<CryptoSignal> SignalList = [];
    public static readonly Queue<CryptoSignal> SignalQueue = new();
    public static readonly List<CryptoPosition> PositionsClosed = [];

    public static event PlayMediaEvent? PlaySound;
    public static event PlayMediaEvent? PlaySpeech;
    public static event AddTextEvent? LogToTelegram;
    public static event AddTextEvent? LogToLogTabEvent;

    // Events for refresing data
    public static event AddTextEvent? SymbolsHaveChangedEvent;
    public static event AddTextEvent? TelegramHasChangedEvent;
#if TRADEBOT
    public static event AddTextEvent? AssetsHaveChangedEvent;
    public static event AddTextEvent? PositionsHaveChangedEvent;
#endif
    public static AddTextEvent? ApplicationHasStarted { get; set; }

    // Ophalen van historische candles duurt lang, dus niet halverwege nog 1 starten (en nog 1 en...)
    public static event SetCandleTimerEnable? SetCandleTimerEnableEvent;

    public static AnalyseEvent? AnalyzeSignalCreated { get; set; }

    // TODO: Deze rare accounts proberen te verbergen (indien mogelijk)
    public static readonly SortedList<int, CryptoTradeAccount> TradeAccountList = [];
    public static readonly SortedList<int, CryptoTradeAccount> ActiveTradeAccountList = [];
    public static CryptoTradeAccount? ExchangeBackTestAccount { get; private set; }
    public static CryptoTradeAccount? ExchangeRealTradeAccount { get; set; }
    public static CryptoTradeAccount? ExchangePaperTradeAccount { get; set; }


    // Some running tasks/threads
#if SQLDATABASE
    static public ThreadSaveCandles TaskSaveCandles { get; set; }
#endif
    public static ThreadMonitorCandle? ThreadMonitorCandle { get; set; }
#if TRADEBOT
    public static ThreadMonitorOrder? ThreadMonitorOrder { get; set; }
    public static ThreadCheckFinishedPosition? ThreadDoubleCheckPosition { get; set; }
#endif
#if BALANCING
    static public ThreadBalanceSymbols ThreadBalanceSymbols { get; set; }
#endif

    // On special request of a hardcore trader..
    public static SymbolValue FearAndGreedIndex { get; set; } = new();
    public static SymbolValue TradingViewDollarIndex { get; set; } = new();
    public static SymbolValue TradingViewSpx500 { get; set; } = new();
    public static SymbolValue TradingViewBitcoinDominance { get; set; } = new();
    public static SymbolValue TradingViewMarketCapTotal { get; set; } = new();

    public static readonly JsonSerializerOptions JsonSerializerIndented = new()
    { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true, IncludeFields = true };


    static public void LoadExchanges()
    {
        AddTextToLogTab("Reading exchange information");

        // De exchanges uit de database laden
        ExchangeListId.Clear();
        ExchangeListName.Clear();

        using var database = new CryptoDatabase();
        foreach (Model.CryptoExchange exchange in database.Connection.GetAll<Model.CryptoExchange>())
        {
            AddExchange(exchange);
        }
    }

    static public void LoadAccounts()
    {
        AddTextToLogTab("Reading account information");

        // De accounts uit de database laden
        TradeAccountList.Clear();

        using var database = new CryptoDatabase();
        foreach (CryptoTradeAccount tradeAccount in database.Connection.GetAll<CryptoTradeAccount>())
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
        ActiveTradeAccountList.Clear();
        foreach (CryptoTradeAccount tradeAccount in TradeAccountList.Values)
        {
            // Er zijn 3 accounts per exchange aanwezig (of dat een goede keuze is vraag ik me af)
            if (tradeAccount.ExchangeId == Settings.General.ExchangeId)
            {
                if (tradeAccount.TradeAccountType == CryptoTradeAccountType.BackTest)
                    ExchangeBackTestAccount = tradeAccount;
                if (tradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
                    ExchangePaperTradeAccount = tradeAccount;
                if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
                    ExchangeRealTradeAccount = tradeAccount;

                // Niet echt super, enumeratie oid hiervoor in het leven roepen, werkt verder wel
                if (BackTest || Settings.Trading.TradeVia != CryptoTradeAccountType.NoTrading)
                    ActiveTradeAccountList.Add(tradeAccount.Id, tradeAccount);
            }
        }
    }


    static public void LoadIntervals()
    {
        AddTextToLogTab("Reading interval information");

        // De intervallen uit de database laden
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
        // Een aantal signalen laden
        // TODO - beperken tot de signalen die nog enigzins bruikbaar zijn??
        AddTextToLogTab("Reading some signals");
        //#if SQLDATABASE
        //        string sql = "select top 50 * from signal order by id desc";
        //        //sql = string.Format("select top 50 * from signal where exchangeid={0} order by id desc", exchange.Id);
        //#else
        //        string sql = "select * from signal order by id desc limit 50";
        //        //sql = string.Format("select * from signal where exchangeid={0} order by id desc limit 50", exchange.Id);
        //#endif
        string sql = "select * from signal where ExpirationDate >= @FromDate order by OpenDate";

        using var database = new CryptoDatabase();
        foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql, new { FromDate = DateTime.UtcNow }))
        {
            if (ExchangeListId.TryGetValue(signal.ExchangeId, out Model.CryptoExchange exchange2))
            {
                signal.Exchange = exchange2;

                if (exchange2.SymbolListId.TryGetValue(signal.SymbolId, out CryptoSymbol symbol))
                {
                    signal.Symbol = symbol;

                    if (IntervalListId.TryGetValue(signal.IntervalId, out CryptoInterval interval))
                        signal.Interval = interval;

                    SignalList.Add(signal);
                    SignalQueue.Enqueue(signal);
                }
            }
        }
    }


    static public void AddExchange(Model.CryptoExchange exchange)
    {
        // Deze exchange kan nog niet ondersteund worden (experimenteel)
        if (exchange.Name.Equals("Kraken"))
            return;

        if (!ExchangeListName.ContainsKey(exchange.Name))
        {
            ExchangeListId.Add(exchange.Id, exchange);
            ExchangeListName.Add(exchange.Name, exchange);
        }
    }


    static public CryptoQuoteData AddQuoteData(string quoteName)
    {
        if (!Settings.QuoteCoins.TryGetValue(quoteName, out CryptoQuoteData quoteData))
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
        // Testje met gelimiteerd aantal symbols
        if (
            symbol.Name.Equals("BTCUSDT") ||
          //  symbol.Name.Equals("BNBUSDT") ||
          //symbol.Name.Equals("ETHUSDT") ||
          //symbol.Name.Equals("XRPUSDT") ||
          //symbol.Name.Equals("ADAUSDT") ||
          //  symbol.Name.Equals("AAVE3SUSDT") ||
          //  symbol.Name.Equals("DASHUSDT") ||
          symbol.Name.Equals("ADAUSDT") ||
          symbol.Name.Equals("WLDUSDT") ||
          symbol.Name.Equals("STORJUSDT") ||


          symbol.Name.Equals("$BMPUSDT") ||
          //symbol.Name.Equals("ADABTC") ||
          //symbol.Name.Equals("COMPBTC") ||
          //symbol.Name.Equals("VEGAUSDT") ||
          //symbol.Name.Equals("UNICUSDT") ||
          symbol.Name.Equals("$BMPBTC")
          )
#endif

        if (ExchangeListId.TryGetValue(symbol.ExchangeId, out Model.CryptoExchange exchange))
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
        }
    }





    static public void AddOrder(CryptoOrder order, bool log = true)
    {
        if (TradeAccountList.TryGetValue(order.TradeAccountId, out CryptoTradeAccount tradeAccount))
        {
            order.TradeAccount = tradeAccount;

            if (ExchangeListId.TryGetValue(order.ExchangeId, out Model.CryptoExchange exchange))
            {
                order.Exchange = exchange;

                if (exchange.SymbolListId.TryGetValue(order.SymbolId, out CryptoSymbol symbol))
                {
                    order.Symbol = symbol;

                    if (!symbol.OrderList.ContainsKey(order.OrderId))
                    {
                        symbol.OrderList.Add(order.OrderId, order);
                        if (log)
                            AddTextToLogTab($"{order.Symbol.Name} added order {order.CreateTime} {order.OrderId} {order.Status} (#{order.Id})");
                    }
                }

            }
        }
    }


    static public void AddTrade(CryptoTrade trade, bool log = true)
    {
        if (TradeAccountList.TryGetValue(trade.TradeAccountId, out CryptoTradeAccount tradeAccount))
        {
            trade.TradeAccount = tradeAccount;

            if (ExchangeListId.TryGetValue(trade.ExchangeId, out Model.CryptoExchange exchange))
            {
                trade.Exchange = exchange;

                if (exchange.SymbolListId.TryGetValue(trade.SymbolId, out CryptoSymbol symbol))
                {
                    trade.Symbol = symbol;

                    if (!symbol.TradeList.ContainsKey(trade.TradeId))
                    {
                        symbol.TradeList.Add(trade.TradeId, trade);
                        if (log)
                            AddTextToLogTab($"{trade.Symbol.Name} order {trade.TradeTime} {trade.OrderId} added trade {trade.TradeId} (#{trade.Id})");
                    }
                }

            }
        }
    }

    static public void InitBarometerSymbols(CryptoDatabase database)
    {
        // TODO: Deze routine is een discrepantie tussen de scanner en trader!
        // De BarometerTools bevat een vergelijkbare routine, enkel de timing verschilt?

        if (ExchangeListName.TryGetValue(Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            foreach (CryptoQuoteData quoteData in Settings.QuoteCoins.Values)
            {
                if (quoteData.FetchCandles)
                {
                    if (!exchange.SymbolListName.ContainsKey(Constants.SymbolNameBarometerPrice + quoteData.Name))
                    {
                        CryptoSymbol symbol = new()
                        {
                            Exchange = exchange,
                            ExchangeId = exchange.Id,
                            Base = Constants.SymbolNameBarometerPrice, // De "munt"
                            Quote = quoteData.Name, // USDT, BTC etc.
                            Volume = 0,
                            Status = 1,
                        };
                        symbol.Name = symbol.Base + symbol.Quote;

                        using var transaction = database.Connection.BeginTransaction();
                        database.Connection.Insert(symbol, transaction);
                        transaction.Commit();

                        AddSymbol(symbol);
                    }
                }
            }
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
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab("Error loading setting " + error.ToString(), false);
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
                ExternalUrls.InitializeUrls();
            }
            else
            {
                ExternalUrls.InitializeUrls();
                //het bestand in ieder geval aanmaken(updates moeten achteraf gepushed worden)
                string text = JsonSerializer.Serialize(ExternalUrls, JsonSerializerIndented);
                File.WriteAllText(fullName, text);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab($"Error loading {filename} " + error.ToString(), false);
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
            AddTextToLogTab($"Error loading {filename} " + error.ToString(), false);
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
            AddTextToLogTab($"Error loading {filename} " + error.ToString(), false);
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
            AddTextToLogTab($"Error loading {filename} " + error.ToString(), false);
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
                CreateSignals = false,
                MinimalVolume = 6500000,
                MinimalPrice = 0.00000001m
            };
            Settings.QuoteCoins.Add(quote.Name, quote);

            quote = new CryptoQuoteData
            {
                Name = "USDT",
                FetchCandles = true,
                CreateSignals = true,
                MinimalVolume = 6500000,
                MinimalPrice = 0.00000001m
            };
            Settings.QuoteCoins.Add(quote.Name, quote);

            quote = new CryptoQuoteData
            {
                Name = "BTC",
                FetchCandles = false,
                CreateSignals = false,
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
            AddTextToLogTab("Error playing music " + error.ToString(), false);
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
            AddTextToLogTab("Error playing speech " + error.ToString(), false);
        }
    }

    static public void AddTextToTelegram(string text)
    {
        try
        {
            LogToTelegram?.Invoke(text);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab(" error telegram thread(1)" + error.ToString(), false);
        }
    }

    static public void AddTextToTelegram(string text, CryptoPosition position)
    {
        if (LogToTelegram is null)
            return;
        try
        {
            if (position is not null)
            {
                string symbol = position.Symbol.Name.ToUpper();
                (string Url, CryptoExternalUrlType Execute) = ExternalUrls.GetExternalRef(Settings.General.TradingApp, true, position.Symbol, position.Interval);
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
            AddTextToLogTab(" error telegram thread(1)" + error.ToString(), false);
        }
    }

    static public void AddTextToTelegram(string text, CryptoSymbol symbol)
    {
        if (LogToTelegram is null)
            return;
        try
        {
            if (symbol is not null)
            {
                string symbolName = symbol.Name.ToUpper();
                (string Url, CryptoExternalUrlType Execute) = ExternalUrls.GetExternalRef(Settings.General.TradingApp, true, symbol, IntervalList[0]);
                if (Url != "")
                {
                    string x = $"<a href='{Url}'>{symbolName}</a>";
                    text = text.Replace(symbolName, x);
                }
            }
            LogToTelegram(text);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab(" error telegram thread(2)" + error.ToString(), false);
        }
    }

    static public void AddTextToLogTab(string text, bool extraLineFeed = false) => LogToLogTabEvent?.Invoke(text, extraLineFeed);
    static public void SymbolsHaveChanged(string text) => SymbolsHaveChangedEvent?.Invoke(text);

#if TRADEBOT

    static public void AssetsHaveChanged(string text) => AssetsHaveChangedEvent?.Invoke(text);
    static public void PositionsHaveChanged(string text) => PositionsHaveChangedEvent?.Invoke(text);
#endif

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
