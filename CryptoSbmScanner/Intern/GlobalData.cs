using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;
using CryptoSbmScanner.Trader;
using CryptoSbmScanner.TradingView;

using Dapper;
using Dapper.Contrib.Extensions;

using NLog;

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
    /// Alle instellingen van de scanner/trader
    /// </summary>
    static public SettingsBasic Settings { get; set; } = new();

    static public PauseRule PauseTrading { get; set; } = new();

    /// <summary>
    /// De url's van de exchanges en/of tradingapps
    /// </summary>
    static public CryptoExternalUrlList ExternalUrls { get; set; } = [];

    // The nlogger stuff
    static public NLog.Logger Logger { get; } = NLog.LogManager.GetCurrentClassLogger();

    //static public Logger Logger;
    //static public Logger SeriLogError;


    static public List<CryptoInterval> IntervalList { get; } = [];
    static public SortedList<int, CryptoInterval> IntervalListId { get; } = [];
    static public SortedList<CryptoIntervalPeriod, CryptoInterval> IntervalListPeriod { get; } = [];

    // Exchanges indexed on name
    static public SortedList<int, Model.CryptoExchange> ExchangeListId { get; } = [];
    static public SortedList<string, Model.CryptoExchange> ExchangeListName { get; } = [];

    // Probleem, de signallist wordt nooit opgeruimd!
    static public List<CryptoSignal> SignalList = [];
    static public Queue<CryptoSignal> SignalQueue { get; } = new();
    static public List<CryptoPosition> PositionsClosed { get; } = [];

    static public event PlayMediaEvent PlaySound;
    static public event PlayMediaEvent PlaySpeech;
    static public event AddTextEvent LogToTelegram;
    static public event AddTextEvent LogToLogTabEvent;

    // Events for refresing data
    static public event AddTextEvent SymbolsHaveChangedEvent;
#if TRADEBOT
    static public event AddTextEvent AssetsHaveChangedEvent;
    static public event AddTextEvent PositionsHaveChangedEvent;
#endif
    static public AddTextEvent ApplicationHasStarted { get; set; }

    // Ophalen van historische candles duurt lang, dus niet halverwege nog 1 starten (en nog 1 en...)
    static public event SetCandleTimerEnable SetCandleTimerEnableEvent;

    static public AnalyseEvent AnalyzeSignalCreated { get; set; }

    // TODO: Deze rare accounts proberen te verbergen (indien mogelijk)
    static public SortedList<int, CryptoTradeAccount> TradeAccountList = [];
    static public SortedList<int, CryptoTradeAccount> ActiveTradeAccountList = [];
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
    static public ThreadCheckFinishedPosition ThreadDoubleCheckPosition { get; set; }
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
                if (BackTest || Settings.Trading.TradeViaExchange || Settings.Trading.TradeViaPaperTrading)
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

        using var database = new CryptoDatabase();
        foreach (CryptoInterval interval in database.Connection.GetAll<CryptoInterval>())
        {
            IntervalList.Add(interval);
            IntervalListId.Add(interval.Id, interval);
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

                    if (IntervalListId.TryGetValue((int)signal.IntervalId, out CryptoInterval interval))
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

        if (ExchangeListName.TryGetValue(Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            foreach (CryptoQuoteData quoteData in Settings.QuoteCoins.Values)
            {
                if (quoteData.FetchCandles)
                {
                    if (!exchange.SymbolListName.ContainsKey(Model.Constants.SymbolNameBarometerPrice + quoteData.Name))
                    {
                        CryptoSymbol symbol = new()
                        {
                            Exchange = exchange,
                            ExchangeId = exchange.Id,
                            Base = Model.Constants.SymbolNameBarometerPrice, // De "munt"
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
            var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true, IncludeFields = true };

            string filename = GetBaseDir() + "settings.json";
            if (File.Exists(filename))
            {
                //using (FileStream readStream = new FileStream(filename, FileMode.Open))
                //{
                //    BinaryFormatter formatter = new BinaryFormatter();
                //    Settings = (Settings)formatter.Deserialize(readStream);
                //    readStream.Close();
                //}
                string text = File.ReadAllText(filename);
                Settings = JsonSerializer.Deserialize<SettingsBasic>(text, options);
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
                        Settings = JsonSerializer.Deserialize<SettingsBasic>(text, options);
                    }
                    catch (Exception error)
                    {
                        Logger.Error(error, "");
                        AddTextToLogTab("Error playing music " + error.ToString(), false);
                    }
                }
                else
                    DefaultSettings();
            }
        }
        catch (Exception error)
        {
            Logger.Error(error, "");
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

                //het bestand in ieder geval aanmaken(updates moeten achteraf gepushed worden)
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };
                string text = JsonSerializer.Serialize(ExternalUrls, options);
                File.WriteAllText(filename, text);
            }
        }
        catch (Exception error)
        {
            Logger.Error(error, "");
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

        var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true, IncludeFields = true };
        string text = JsonSerializer.Serialize(Settings, options);
        File.WriteAllText(filename, text);

#if DEBUG
        //// Ter debug om te zien of alles okay is
        filename = GlobalData.GetBaseDir();
        Directory.CreateDirectory(filename);
        filename += "settingsSignalsCompiled.json";
        text = JsonSerializer.Serialize(TradingConfig.Signals, options);
        File.WriteAllText(filename, text);

        filename = GlobalData.GetBaseDir();
        Directory.CreateDirectory(filename);
        filename += "settingsTradingCompiled.json";
        text = JsonSerializer.Serialize(TradingConfig.Trading, options);
        File.WriteAllText(filename, text);
#endif
    }


    static public void PlaySomeMusic(string text, bool test = false)
    {
        try
        {
            PlaySound(text, test);
        }
        catch (Exception error)
        {
            Logger.Error(error, "");
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
            Logger.Error(error, "");
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
            Logger.Error(error, "");
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            AddTextToLogTab(" error telegram thread(1)" + error.ToString(), false);
        }
    }


    static public void AddTextToLogTab(string text, bool extraLineFeed = false)
    {
        LogToLogTabEvent(text, extraLineFeed);
    }


    static public void SymbolsHaveChanged(string text)
    {
        SymbolsHaveChangedEvent?.Invoke(text);
    }

#if TRADEBOT

    static public void AssetsHaveChanged(string text)
    {
        AssetsHaveChangedEvent?.Invoke(text);
    }

    static public void PositionsHaveChanged(string text)
    {
        PositionsHaveChangedEvent?.Invoke(text);
    }
#endif

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


    static public void InitializeLogging()
    {
        // nlog is lastig te beinvloeden, daarom maar via code
        // serilog is niet veel anders, prima logging, maar beinvloeding van bestandsnamen is gelimiteerd (splitsen errors is een probleem)

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


        <?xml version="1.0" encoding="utf-8" ?>
        <nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
                 xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" >
          <extensions>
            <add assembly="My.Awesome.LoggingExentions"/>
          </extensions>
            <targets>
                <target name="file1" xsi:type="File"
                          fileName="${basedir}/Logs/${date:format=yyyy-MM-dd}.log"
                          layout="${longdate} 
                          ${level:uppercase=true:padding=5} 
                          ${session} 
        ${storeid} ${msisdn} - ${logger:shortName=true} - ${message} 
        ${exception:format=tostring}"
                          keepFileOpen="true"
                        />
            </targets>
          <rules>
              <logger name="*" minlevel="Trace" writeTo="file1" />
          </rules>
        </nlog>
        */


        // ik ben het wel even zat met nlog en die filenames

        //// Create configuration object 
        var config = new NLog.Config.LoggingConfiguration();

        // Create targets and add them to the configuration 
        var fileTarget = new NLog.Targets.FileTarget();
        config.AddTarget("file", fileTarget);
        fileTarget.Name = "default";
        fileTarget.KeepFileOpen = true;
        fileTarget.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day; // None?
        fileTarget.FileName = GetBaseDir() + "CryptoScanner ${date:format=yyyy-MM-dd}.log";
        fileTarget.MaxArchiveDays = 14;
        //fileTarget.ArchiveDateFormat = "yyyy-MM-dd";
        //fileTarget.EnableArchiveFileCompression = false;
        //fileTarget.ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date;
        //fileTarget.MaxArchiveDays = 10;
        //fileTarget.ArchiveFileName = fileTarget.FileName; //"${logDirectory}/Log.{#}.log";
        //fileTarget.Layout = "Exception Type: ${exception:format=Type}${newline}Target Site:  ${event-context:TargetSite }${newline}Message: ${message}";
        var rule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, fileTarget);
        config.LoggingRules.Add(rule);


        fileTarget = new NLog.Targets.FileTarget();
        config.AddTarget("file", fileTarget);
        fileTarget.Name = "errors";
        fileTarget.KeepFileOpen = true;
        fileTarget.MaxArchiveDays = 14;
        fileTarget.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day; // None?
        fileTarget.FileName = GetBaseDir() + "CryptoScanner ${date:format=yyyy-MM-dd}-Errors.log";
        rule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Error, fileTarget);
        config.LoggingRules.Add(rule);


        //fileTarget = new NLog.Targets.FileTarget();
        //config.AddTarget("file", fileTarget);
        //fileTarget.Name = "trace";
        //fileTarget.KeepFileOpen = true;
        //fileTarget.MaxArchiveDays = 14;
        //fileTarget.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day; // None?
        //fileTarget.FileName = GetBaseDir() + "CryptoScanner ${date:format=yyyy-MM-dd}-Trace.log";
        //rule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, fileTarget);
        //config.LoggingRules.Add(rule);



        NLog.LogManager.Configuration = config;

        Logger.Info("");
        Logger.Info("");
        Logger.Info("****************************************************");
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
