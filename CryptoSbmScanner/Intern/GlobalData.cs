using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;
using CryptoSbmScanner.TradingView;

using Dapper;
using Dapper.Contrib.Extensions;

using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CryptoSbmScanner.Intern;


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
    // Emulator kan alleen de backTest zetten (anders gaan er onverwachte zaken naar de database enzo)
    static public bool BackTest { get; set; }

    static public CryptoApplicationStatus ApplicationStatus { get; set; } = CryptoApplicationStatus.Initializing;

    static public int createdSignalCount = 0; // Tellertje met het aantal meldingen (komt in de taakbalk c.q. applicatie titel)

    /// <summary>
    /// Alle instellingen van de scanenr/trader
    /// </summary>
    static public SettingsBasic Settings { get; set; } = new();

    /// <summary>
    /// De url's van de exchanges en/of tradingapps
    /// </summary>
    static public CryptoExternalUrlList ExternalUrls { get; set; } = new();

    // The nlogger stuff
    static public NLog.Logger Logger { get; } = NLog.LogManager.GetCurrentClassLogger();

    static public List<CryptoInterval> IntervalList { get; } = new();
    static public SortedList<int, CryptoInterval> IntervalListId { get; } = new();
    static public SortedList<CryptoIntervalPeriod, CryptoInterval> IntervalListPeriod { get; } = new();

    // Exchanges indexed on name
    static public SortedList<int, Model.CryptoExchange> ExchangeListId { get; } = new();
    static public SortedList<string, Model.CryptoExchange> ExchangeListName { get; } = new();

    static public Queue<CryptoSignal> SignalQueue { get; } = new();
    static public List<CryptoPosition> PositionsClosed { get; } = new();

    static public event PlayMediaEvent PlaySound;
    static public event PlayMediaEvent PlaySpeech;
    static public event AddTextEvent LogToTelegram;
    static public event AddTextEvent LogToLogTabEvent;

    // Events for refresing data
    static public event AddTextEvent AssetsHaveChangedEvent;
    static public event AddTextEvent SymbolsHaveChangedEvent;
    static public event AddTextEvent PositionsHaveChangedEvent;
    static public AddTextEvent ApplicationHasStarted { get; set; }

    // Ophalen van historische candles duurt lang, dus niet halverwege nog 1 starten (en nog 1 en...)
    static public event SetCandleTimerEnable SetCandleTimerEnableEvent;

    static public AnalyseEvent AnalyzeSignalCreated { get; set; }

    // TODO: Deze rare accounts proberen te verbergen (indien mogelijk)
    static public SortedList<int, CryptoTradeAccount> TradeAccountList = new();
    static public CryptoTradeAccount ExchangeBackTestAccount { get; set; }
    static public CryptoTradeAccount ExchangeRealTradeAccount { get; set; }
    static public CryptoTradeAccount ExchangePaperTradeAccount { get; set; }


    // Some running tasks/threads
#if SQLDATABASE
    static public ThreadSaveCandles TaskSaveCandles { get; set; }
#endif
    static public ThreadMonitorCandle ThreadMonitorCandle { get; set; }
#if TRADEBOT
    static public ThreadMonitorOrder ThreadMonitorOrder { get; set; }
#endif
#if BALANCING
    static public ThreadBalanceSymbols ThreadBalanceSymbols { get; set; }
#endif

    // On special request of a hardcore trader..
    static public SymbolValue FearAndGreedIndex { get; set; } = new();
    static public SymbolValue TradingViewDollarIndex { get; set; } = new();
    static public SymbolValue TradingViewSpx500 { get; set; } = new();
    static public SymbolValue TradingViewBitcoinDominance { get; set; } = new();
    static public SymbolValue TradingViewMarketCapTotal { get; set; } = new();


    static public void LoadExchanges()
    {
        GlobalData.AddTextToLogTab("Reading exchange information");

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
        GlobalData.AddTextToLogTab("Reading account information");

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
        foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values)
        {
            // Er zijn 3 accounts per exchange aanwezig (of dat een goede keuze is vraag ik me af)
            if (tradeAccount.ExchangeId == GlobalData.Settings.General.ExchangeId)
            {
                if (tradeAccount.TradeAccountType == CryptoTradeAccountType.BackTest)
                    GlobalData.ExchangeBackTestAccount = tradeAccount;
                if (tradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
                    GlobalData.ExchangePaperTradeAccount = tradeAccount;
                if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
                    GlobalData.ExchangeRealTradeAccount = tradeAccount;
            }
        }
    }


    static public void LoadIntervals()
    {
        GlobalData.AddTextToLogTab("Reading interval information");

        // De intervallen uit de database laden
        IntervalList.Clear();
        IntervalListId.Clear();
        IntervalListPeriod.Clear();

        using var database = new CryptoDatabase();
        foreach (CryptoInterval interval in database.Connection.GetAll<CryptoInterval>())
        {
            IntervalList.Add(interval);
            IntervalListId.Add(interval.Id, interval);
            IntervalListPeriod.Add(interval.IntervalPeriod, interval);
        }


        // De ContructFrom object koppelen
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (interval.ConstructFromId > 0)
                interval.ConstructFrom = GlobalData.IntervalListId[(int)interval.ConstructFromId];
        }

        // In MSSQL staan ze niet in dej uiste volgorde (vanwege het toevoegen van 2 intervallen)
        IntervalList.Sort((x, y) => x.IntervalPeriod.CompareTo(y.IntervalPeriod));
    }

    static public void LoadSymbols()
    {
        // De symbols uit de database lezen (ook van andere exchanges)
        // Dat doen we om de symbol van voorgaande signalen en/of posities te laten zien
        GlobalData.AddTextToLogTab("Reading symbol information");
        //string sql = $"select * from symbol where exchangeid={exchange.Id}";
        string sql = "select * from symbol";

        using var database = new CryptoDatabase();
        foreach (CryptoSymbol symbol in database.Connection.Query<CryptoSymbol>(sql))
            GlobalData.AddSymbol(symbol);
    }

    static public void LoadSignals()
    {
        // Een aantal signalen laden
        // TODO - beperken tot de signalen die nog enigzins bruikbaar zijn??
        GlobalData.AddTextToLogTab("Reading some signals");
//#if SQLDATABASE
//        string sql = "select top 50 * from signal order by id desc";
//        //sql = string.Format("select top 50 * from signal where exchangeid={0} order by id desc", exchange.Id);
//#else
//        string sql = "select * from signal order by id desc limit 50";
//        //sql = string.Format("select * from signal where exchangeid={0} order by id desc limit 50", exchange.Id);
//#endif
        string sql = "select * from signal where ExpirationDate >= @FromDate order by OpenDate";

        using var database = new CryptoDatabase();
        foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql, new { FromDate = DateTime.UtcNow}))
        {
            if (GlobalData.ExchangeListId.TryGetValue(signal.ExchangeId, out Model.CryptoExchange exchange2))
            {
                signal.Exchange = exchange2;

                if (exchange2.SymbolListId.TryGetValue(signal.SymbolId, out CryptoSymbol symbol))
                {
                    signal.Symbol = symbol;

                    if (GlobalData.IntervalListId.TryGetValue((int) signal.IntervalId, out CryptoInterval interval))
                        signal.Interval = interval;

                    GlobalData.SignalQueue.Enqueue(signal);
                }
            }
        }
    }


#if TRADEBOT
    static public void LoadClosedPositions()
    {
        // Alle gesloten posities lezen 
        // TODO - beperken tot de laatste 2 dagen? (en wat handigheden toevoegen wellicht)
        GlobalData.AddTextToLogTab("Reading closed position");
#if SQLDATABASE
        string sql = "select top 250 * from position where not closetime is null order by id desc";
#else
        string sql = "select * from position where not closetime is null order by id desc limit 250";
#endif
        using var database = new CryptoDatabase();
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql))
            PositionTools.AddPositionClosed(position);
    }

    static public void LoadOpenPositions()
    {
        // Alle openstaande posities lezen 
        GlobalData.AddTextToLogTab("Reading open position");

        using var database = new CryptoDatabase();
        string sql = "select * from position where closetime is null and status < 2";
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql))
        {
            if (!GlobalData.TradeAccountList.TryGetValue(position.TradeAccountId, out CryptoTradeAccount tradeAccount))
                throw new Exception("Geen trade account gevonden");

            PositionTools.AddPosition(tradeAccount, position);
            PositionTools.LoadPosition(database, position);
        }
    }

    static async public Task CheckOpenPositions()
    {
        // Alle openstaande posities lezen 
        GlobalData.AddTextToLogTab("Checking open position");

        using var database = new CryptoDatabase();
        foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values.ToList())
        {
            //foreach (CryptoPosition position in tradeAccount.PositionList.Values.ToList())
            foreach (var positionList in tradeAccount.PositionList.Values.ToList())
            {
                foreach (var position in positionList.Values.ToList())
                {
                    // Controleer de openstaande orders, zijn ze ondertussen gevuld
                    // Haal de trades van deze positie op vanaf de 1e order
                    // TODO - Hoe doen we dit met papertrading (er is niets geregeld!)
                    await PositionTools.LoadTradesfromDatabaseAndExchange(database, position);
                    PositionTools.CalculatePositionResultsViaTrades(database, position);

                    foreach (CryptoPositionPart part in position.Parts.Values)
                    {
                        if (part.Invested > 0 && part.Quantity == 0 && part.Status != CryptoPositionStatus.Ready)
                        {
                            part.Status = CryptoPositionStatus.Ready;
                            GlobalData.AddTextToLogTab(string.Format("LoadData: Positie {0} Part {1} status aangepast naar {2}", position.Symbol.Name, part.Name, part.Status));
                        }

                        if (part.Status == CryptoPositionStatus.Ready)
                        {
                            if (!part.CloseTime.HasValue)
                                part.CloseTime = DateTime.UtcNow;
                            database.Connection.Update(part);
                        }
                    }

                    if (position.Invested > 0 && position.Quantity == 0 && position.Status != CryptoPositionStatus.Ready)
                    {
                        position.Status = CryptoPositionStatus.Ready;
                        GlobalData.AddTextToLogTab(string.Format("LoadData: Positie {0} status aangepast naar {1}", position.Symbol.Name, position.Status));
                    }

                    if (position.Status == CryptoPositionStatus.Ready)
                    {
                        PositionTools.RemovePosition(position.TradeAccount, position);
                        if (!position.CloseTime.HasValue)
                            position.CloseTime = DateTime.UtcNow;
                        database.Connection.Update(position);
                    }
                }
            }
        }
    }
#endif

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

            if (quoteName.Equals("USDT"))
                quoteData.DisplayFormat = "N2";
            if (quoteName.Equals("BUSD"))
                quoteData.DisplayFormat = "N2";

            Settings.QuoteCoins.Add(quoteName, quoteData);
        }
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
          symbol.Name.Equals("AKROUSDT") ||
          symbol.Name.Equals("COMPUSDT") ||


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


    static public void AddTrade(CryptoTrade trade)
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
                    }
                }

            }
        }
    }


    static public void InitBarometerSymbols(CryptoDatabase database)
    {
        // TODO: Deze routine is een discrepantie tussen de scanner en trader!
        // De BarometerTools bevat een vergelijkbare routine, enkel de timing verschilt?

        if (ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
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
            string filename = GetBaseDir() + "settings.json";
            if (File.Exists(filename))
            {
                //using (FileStream readStream = new FileStream(filename, FileMode.Open))
                //{
                //    BinaryFormatter formatter = new BinaryFormatter();
                //    GlobalData.Settings = (Settings)formatter.Deserialize(readStream);
                //    readStream.Close();
                //}
                string text = File.ReadAllText(filename);
                Settings = JsonSerializer.Deserialize<SettingsBasic>(text);
            }
            else
            {
                // Oude naam = "GlobalData.Settings2.json"
                // Toch de instellingen proberen over te nemen
                string oldSettings = GetBaseDir() + "GlobalData.Settings2.json";
                if (File.Exists(oldSettings))
                {
                    try
                    {
                        string text = File.ReadAllText(oldSettings);
                        Settings = JsonSerializer.Deserialize<SettingsBasic>(text);
                    }
                    catch (Exception error)
                    {
                        Logger.Error(error);
                        AddTextToLogTab("Error playing music " + error.ToString(), false);
                    }
                }
                else
                    DefaultSettings();
            }
        }
        catch (Exception error)
        {
            Logger.Error(error);
            AddTextToLogTab("Error loading Weblinks.json " + error.ToString(), false);
        }
    }

    static public void LoadLinkSettings()
    {
        try
        {
            string filename = GetBaseDir() + "Weblinks.json";
            if (File.Exists(filename))
            {
                string text = File.ReadAllText(filename);
                ExternalUrls = JsonSerializer.Deserialize<CryptoExternalUrlList>(text);
            }
            else
            {
                if (ExternalUrls.Count == 0)
                    ExternalUrls.InitializeUrls();

                // het bestand in ieder geval aanmaken (updates moeten achteraf gepushed worden)
                string text = JsonSerializer.Serialize(ExternalUrls, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                });
                File.WriteAllText(filename, text);
            }
        }
        catch (Exception error)
        {
            Logger.Error(error);
            AddTextToLogTab("Error loading Weblinks.json " + error.ToString(), false);
        }
    }

    static public void LoadSettings()
    {
        LoadBaseSettings();
        LoadLinkSettings();
    }

    static public void DefaultSettings()
    {
        // Apply some defaults
        if (Settings.QuoteCoins.Count == 0)
        {
            CryptoQuoteData quote = new()
            {
                Name = "BUSD",
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

    static public void SaveSettings()
    {
        //Laad de gecachte (langere historie, minder overhad)
        string filename = GetBaseDir();
        Directory.CreateDirectory(filename);
        filename += "settings.json";

        //using (FileStream writeStream = new FileStream(filename, FileMode.Create))
        //{
        //    BinaryFormatter formatter = new BinaryFormatter();
        //    formatter.Serialize(writeStream, GlobalData.Settings);
        //    writeStream.Close();
        //}

        string text = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
        //var accountFile = new FileInfo(filename);
        File.WriteAllText(filename, text);

        //filename = GlobalData.GetBaseDir() + "Settings.json";
        //File.WriteAllText(filename, text);
    }


    static public void PlaySomeMusic(string text, bool test = false)
    {
        try
        {
            PlaySound(text, test);
        }
        catch (Exception error)
        {
            Logger.Error(error);
            AddTextToLogTab("Error playing music " + error.ToString(), false);
        }
    }

    static public void PlaySomeSpeech(string text, bool test = false)
    {
        try
        {
            PlaySpeech(text, test);
        }
        catch (Exception error)
        {
            Logger.Error(error);
            AddTextToLogTab("Error playing speech " + error.ToString(), false);
        }
    }

    static public void AddTextToTelegram(string text)
    {
        try
        {
            LogToTelegram(text);
        }
        catch (Exception error)
        {
            Logger.Error(error);
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            AddTextToLogTab(" error telegram thread(1)" + error.ToString(), false);
        }
    }


    static public void AddTextToLogTab(string text, bool extraLineFeed = false)
    {
        LogToLogTabEvent(text, extraLineFeed);
    }


    static public void AssetsHaveChanged(string text)
    {
        AssetsHaveChangedEvent(text);
    }

    static public void SymbolsHaveChanged(string text)
    {
        SymbolsHaveChangedEvent(text);
    }

    static public void PositionsHaveChanged(string text)
    {
        PositionsHaveChangedEvent(text);
    }

    static public void SetCandleTimerEnable(bool value)
    {
        SetCandleTimerEnableEvent(value);
    }

    static bool IsInitialized = false;


    static public string GetBaseDir()
    {
        ApplicationParams.InitApplicationOptions();
        string appDataFolder = ApplicationParams.Options.AppDataFolder;
        if (appDataFolder == null | appDataFolder == "")
            appDataFolder = "CryptoScanner";

        string baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        appDataFolder = Path.Combine(baseFolder, appDataFolder);


        if (!IsInitialized)
        {
            IsInitialized = true;
            Directory.CreateDirectory(appDataFolder);
        }

        return appDataFolder + @"\";
    }

    static public void DebugOnlySymbol(string name)
    {
        // Reduceren naar enkel de BTCUSDT voor traceren van problemen
        Model.CryptoExchange exchangex = GlobalData.ExchangeListId.Values[0];
        for (int i = exchangex.SymbolListId.Values.Count - 1; i >= 0; i--)
        {
            CryptoSymbol symbol = exchangex.SymbolListId.Values[i];
            //De "barometer" munten overslagen AUB
            if (symbol.IsBarometerSymbol())
                continue;
            if (!symbol.Name.Equals(name))
                exchangex.SymbolListId.Remove(symbol.Id);
        }

        // Reduceren naar enkel de BTCUSDT voor traceren van problemen
        for (int i = exchangex.SymbolListName.Values.Count - 1; i >= 0; i--)
        {
            CryptoSymbol symbol = exchangex.SymbolListName.Values[i];
            //De "barometer" munten overslagen AUB
            if (symbol.IsBarometerSymbol())
                continue;
            if (!symbol.Name.Equals(name))
                exchangex.SymbolListName.Remove(symbol.Name);
        }

        SymbolsHaveChangedEvent("");
    }


    static public void InitializeNlog()
    {
        // nlog is lastig te beinvloeden, dus dan maar via code

        /*
        <targets>
            <target name="default" xsi:type="File" 
                fileName="${specialfolder:folder=ApplicationData}/CryptoScanner/CryptoScanner.log" 
                archiveFileName="${specialfolder:folder=ApplicationData}/CryptoScanner/CryptoScanner.{#}.log" 
                archiveEvery="Day" archiveNumbering="Rolling" 
                maxArchiveFiles="7" />
            <target name="errors" xsi:type="File" 
                fileName="${specialfolder:folder=ApplicationData}/CryptoScanner/CryptoScanner-errors.log" 
                archiveFileName="${specialfolder:folder=ApplicationData}/CryptoScanner/CryptoScanner-errors.{#}.log" 
                archiveEvery="Day" 
                archiveNumbering="Rolling" 
                maxArchiveFiles="7" />
        </targets>

		<logger name="*" writeTo="default" />
		<logger name="*" minlevel="Error" writeTo="errors" />
        */

        // Create configuration object 
        var config = new NLog.Config.LoggingConfiguration();

        // Create targets and add them to the configuration 
        var fileTarget = new NLog.Targets.FileTarget();
        config.AddTarget("file", fileTarget);
        fileTarget.Name = "default";
        fileTarget.ArchiveDateFormat = "yyyy-MM-dd";
        fileTarget.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day;
        fileTarget.EnableArchiveFileCompression = false;
        fileTarget.MaxArchiveDays = 10;
        fileTarget.FileName = GetBaseDir() + "CryptoScanner ${date:format=yyyy-MM-dd}.log";
        //fileTarget.Layout = "Exception Type: ${exception:format=Type}${newline}Target Site:  ${event-context:TargetSite }${newline}Message: ${message}";

        var rule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, fileTarget);
        config.LoggingRules.Add(rule);

        fileTarget = new NLog.Targets.FileTarget();
        config.AddTarget("file", fileTarget);
        fileTarget.Name = "errors";
        fileTarget.ArchiveDateFormat = "yyyy-MM-dd";
        fileTarget.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day;
        fileTarget.MaxArchiveDays = 10;
        fileTarget.EnableArchiveFileCompression = false;
        fileTarget.FileName = GetBaseDir() + "CryptoScanner ${date:format=yyyy-MM-dd}-Errors.log";
        //fileTarget.Layout = "Exception Type: ${exception:format=Type}${newline}Target Site:  ${event-context:TargetSite }${newline}Message: ${message}";

        rule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Error, fileTarget);
        config.LoggingRules.Add(rule);


        NLog.LogManager.Configuration = config;

        Logger.Info("");
        Logger.Info("");
        Logger.Info("****************************************************");
    }

    //public static void DumpSessionInformation()
    //{
    //    foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values.ToList())
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
