using Avalonia.Controls;
using Avalonia.Threading;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Zones;

using Dapper;
using Dapper.Contrib.Extensions;

using Microsoft.Extensions.DependencyInjection;

using System.Globalization;
using System.Text.Json;

namespace CryptoScanner.Core.Core;


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
    // DI (but moved to GlobalData so we can use it in class lib)
    public static IServiceProvider Services { get; set; } = null!;
    public static T? GetService<T>() where T : class => Services?.GetService<T>();
    public static Window? MainWindow { get; set; } = null!;

    public static string AppPath { get; set; } = ""; // For sounds
    public static string LogName { get; set; } = "";
    public static string AppVersion { get; set; } = "";
    public static string AppDataFolder { get; set; } = ""; // depends on startup parameters (also in platformService)

    //public static bool ApplicationIsShowed { get; set; } = false;
    public static bool ApplicationIsClosing { get; set; } = false;


    // todo, kill these properties, the emulator is now excluded and these are left overs..
    // And for the emulator we need to introduce a datetime service + candleservice or something like that
    public static bool BackTest { get; set; }
    public static DateTime BackTestDateTime { get; set; }
    public static CryptoCandle? BackTestCandle { get; set; }


    // Replace with a proper DateTimeService
    public static DateTime GetCurrentDateTime()
    {
        if (BackTest)
            return BackTestDateTime; // or BackTestCandle.OpenTime + 1 minute
        else
            return DateTime.UtcNow;
    }

    private static CryptoApplicationStatus _applicationStatus = CryptoApplicationStatus.Initializing;
    public static CryptoApplicationStatus ApplicationStatus
    {
        get { return _applicationStatus; }
        set { _applicationStatus = value;
            Dispatcher.UIThread.Post(() => { StatusesHaveChangedEvent?.Invoke(""); });
       }
    }

    // Amount of signals created
    public static int CreatedSignalCount { get; set; }

    /// <summary>
    /// Scanner settings
    /// </summary>
    public static SettingsBasic Settings { get; set; } = new();

    /// <summary>
    /// Exchange API settings (not used at this moment)
    /// </summary>
    public static SettingsExchangeApi TradingApi { get; set; } = new();

    /// <summary>
    /// Altrady API settings
    /// </summary>
    public static SettingsAltradyApi AltradyApi { get; set; } = new();

    /// <summary>
    /// Telegram related instellingen
    /// </summary>
    public static SettingsTelegram Telegram { get; set; } = new();

    /// <summary>
    /// Url's settings for all exchanges
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
    public static readonly Queue<CryptoLiveData> LiveDataQueue = [];
    public static readonly Dictionary<(string, CryptoIntervalPeriod), CryptoLiveData> LiveDataQueueAdded = [];

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


    // Active exchange
    public static Model.CryptoExchange? ActiveExchange { get; set; }
    public static string ActivateExchangeName { get; set; } = "";


    // Some running tasks/threads
    public static ThreadSaveObjects? ThreadSaveObjects { get; set; }
    public static ThreadMonitorCandle? ThreadMonitorCandle { get; set; }
    public static ThreadMonitorOrder? ThreadMonitorOrder { get; set; }
    public static ThreadCheckFinishedPosition? ThreadCheckPosition { get; set; }
    public static ZoneThreadCalculate? ThreadZoneCalculate { get; set; }


    // Indexed strategies for colors and soundfiles etc...
    public static Dictionary<CryptoSignalStrategy, (SettingsSignalStrategyBase strategySettings, long lastSignalStrategy)> StrategiesSettings = [];


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
        string sql = "select * from symbol where exchangeid=@exchangeid";
        using var database = new CryptoDatabase();
        foreach (CryptoSymbol symbol in database.Connection.Query<CryptoSymbol>(sql, new { exchangeid = GlobalData.ActiveExchange!.Id}))
            AddSymbol(symbol);
    }

    public static void LoadSignals(string filterText = "")
    {
        //GlobalData.AddTextToLogTab("Reading some signals");

        if (BackTest)
        {
            string sql = "select * from signal where exchangeid=@exchangeid and BackTest=1 order by OpenDate";

            using var database = new CryptoDatabase();
            foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql, new { exchangeid = GlobalData.ActiveExchange!.Id }))
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
            string sql;
            using var database = new CryptoDatabase();
            if (string.IsNullOrEmpty(filterText))
                sql = "select * from signal where exchangeid=@exchangeid and BackTest=0 and ExpirationDate >= @FromDate order by OpenDate";
            else
            {
                sql = "select * from signal " +
                    "inner join symbol on signal.symbolid=symbol.id " +
                    "where signal.exchangeid=@exchangeid and signal.BackTest=0 and signal.ExpirationDate >= @FromDate " +
                    $"and symbol.name like '%{filterText}%' " +
                    "order by signal.OpenDate ";
            }

            SignalQueue.Clear();
            foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql, new { FromDate = DateTime.UtcNow, exchangeid = GlobalData.ActiveExchange!.Id }))
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

            Settings.QuoteCoins.Add(quoteName, quoteData);
        }

        if (quoteName.Equals("EUR") || quoteName.Equals("USDC") || quoteName.Equals("USDT") || quoteName.Equals("BUSD"))
            quoteData.DisplayFormat = "N2";

        return quoteData;
    }

    public static void AddSymbol(CryptoSymbol symbol)
    {
#if LIMITSYMBOLS
        // Test with limits anount of symbols for debugging purposes
        if (
            symbol.Base.Equals("BTC") || symbol.Base.Equals("ETH") ||
            symbol.Base.Equals("ADA") || symbol.Base.Equals("SOL") ||
            symbol.Base.Equals("TRX") || symbol.Base.Equals("ENA") ||
            symbol.Base.Equals("ZKJ") || symbol.Base.Equals("SUI") ||
            symbol.Base.Equals("ZEC") || symbol.Base.Equals("XRP") ||
            symbol.Base.StartsWith("$BMP")
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

            if (!exchange.SymbolListExchangeName.ContainsKey(symbol.ExchangeName))
                exchange.SymbolListExchangeName.Add(symbol.ExchangeName, symbol);

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
            //symbol.AskPrice = null;
            //symbol.BidPrice = null;
            symbol.LastPrice = null;
        }
    }



    public static void LoadBaseSettings()
    {
        try
        {
            string filename = Path.Combine(GetBaseDir(), $"{Constants.AppName}-settings.json");
            if (File.Exists(filename))
            {
                //using (FileStream readStream = new FileStream(fileName, FileMode.Open))
                //{
                //    BinaryFormatter formatter = new BinaryFormatter();
                //    Settings = (Settings)formatter.Deserialize(readStream);
                //    readStream.Close();
                //}
                //string text = File.ReadAllText(fileName);
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

            // Fill in empty activate exchange
            if (Settings.General.ActivateExchangeName == "")
                Settings.General.ActivateExchangeName = Settings.General.ExchangeName;

            AddStrategySettings();
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab("Error loading setting " + error.ToString());
        }
    }


    public static void LoadLinkSettings()
    {
        string filename = $"{Constants.AppName}-weblinks.json";
        try
        {
            string fullName = Path.Combine(GetBaseDir(), filename);
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


    public static void LoadTelegramSettings()
    {
        //string fileName = $"{Constants.AppName}-telegram.json";
        //try
        //{
        //    string fullName = Path.Combine(GetBaseDir(), fileName);
        //    if (File.Exists(fullName))
        //    {
        //        string text = File.ReadAllText(fullName);
        //        var value = JsonSerializer.Deserialize<SettingsTelegram>(text, JsonTools.DeSerializerOptions);
        //        if (value != null)
        //            Telegram = value;
        //        else
        //            Telegram = new();
        //    }
        //}
        //catch (Exception error)
        //{
        //    ScannerLog.Logger.Error(error, "");
        //    AddTextToLogTab($"Error loading {fileName} " + error.ToString());
        //}
    }

    public static void LoadExchangeSettings()
    {
        //string fileName = $"{Constants.AppName}-exchange.json";
        //try
        //{
        //    string fullName = Path.Combine(AppDataFolder, fileName);
        //    if (File.Exists(fullName))
        //    {
        //        File.Delete(fullName);
        //        //        string text = File.ReadAllText(fullName);
        //        //        var value = JsonSerializer.Deserialize<SettingsExchangeApi>(text, JsonTools.DeSerializerOptions);
        //        //        if (value != null)
        //        //            TradingApi = value;
        //        //        else
        //        //            TradingApi = new();
        //    }

        //    //    // Exchange API no longer supported, clear it just in case
        //    //    // (Better to remove it but i'm still hesitating about this)
        //    //    TradingApi.Key = "";
        //    //    TradingApi.Secret = "";
        //    //    TradingApi.PassPhrase = "";
        //}
        //catch (Exception error)
        //{
        //    ScannerLog.Logger.Error(error, "");
        //    AddTextToLogTab($"Error loading {fileName} " + error.ToString());
        //}


        //fileName = $"{Constants.AppName}-altrady.json";
        //try
        //{
        //    string fullName = Path.Combine(AppDataFolder, fileName);
        //    if (File.Exists(fullName))
        //    {
        //        string text = File.ReadAllText(fullName);
        //        var value = JsonSerializer.Deserialize<SettingsAltradyApi>(text, JsonTools.DeSerializerOptions);
        //        if (value != null)
        //            AltradyApi = value;
        //        else
        //            AltradyApi = new();
        //    }
        //}
        //catch (Exception error)
        //{
        //    ScannerLog.Logger.Error(error, "");
        //    AddTextToLogTab($"Error loading {fileName} " + error.ToString());
        //}
    }



    public static void LoadSettings()
    {
        LoadBaseSettings();
        //LoadExchangeSettings();
        //LoadTelegramSettings();
        LoadLinkSettings();
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

    public static void SaveSettings()
    {
        string baseFolder = AppDataFolder;
        Directory.CreateDirectory(baseFolder);

        //using (FileStream writeStream = new FileStream(fileName, FileMode.Create))
        //{
        //    BinaryFormatter formatter = new BinaryFormatter();
        //    formatter.Serialize(writeStream, GlobalData.Settings);
        //    writeStream.Close();
        //}

        string filename = Path.Combine(baseFolder, $"{Constants.AppName}-settings.json");
        string text = JsonSerializer.Serialize(Settings, JsonTools.JsonSerializerIndented);
        File.WriteAllText(filename, text);

        //filename = baseFolder + $"{Constants.AppName}-telegram.json";
        //text = JsonSerializer.Serialize(Telegram, JsonTools.JsonSerializerIndented);
        //File.WriteAllText(filename, text);

        ////fileName = baseFolder + $"{AppName}-exchange.json";
        ////text = JsonSerializer.Serialize(TradingApi, JsonTools.JsonSerializerIndented);
        ////File.WriteAllText(fileName, text);

        //filename = baseFolder + $"{Constants.AppName}-altrady.json";
        //text = JsonSerializer.Serialize(AltradyApi, JsonTools.JsonSerializerIndented);
        //File.WriteAllText(filename, text);

        //#if DEBUG
        //        //// Ter debug om te zien of alles okay is
        //        fileName = GlobalData.GetBaseDir();
        //        Directory.CreateDirectory(fileName);
        //        fileName += "settingsSignalsCompiled.json";
        //        text = JsonSerializer.Serialize(TradingConfig.Signals, options);
        //        File.WriteAllText(fileName, text);

        //        fileName = GlobalData.GetBaseDir();
        //        Directory.CreateDirectory(fileName);
        //        fileName += "settingsTradingCompiled.json";
        //        text = JsonSerializer.Serialize(TradingConfig.Trading, options);
        //        File.WriteAllText(fileName, text);
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


    public static void AddTextToLogTab(string text) => LogToLogTabEvent?.Invoke(text);
    //public static void StatusesHaveChanged(string text) => StatusesHaveChangedEvent?.Invoke(text);
    public static void SymbolsHaveChanged(string text) => SymbolsHaveChangedEvent?.Invoke(text);

    public static void AssetsHaveChanged(string text) => AssetsHaveChangedEvent?.Invoke(text);
    public static void PositionsHaveChanged(string text) => PositionsHaveChangedEvent?.Invoke(text);

    public static void TelegramHasChanged(string text) => TelegramHasChangedEvent?.Invoke(text);
    public static void SetCandleTimerEnable(bool value) => SetCandleTimerEnableEvent?.Invoke(value);


    public static string GetBaseDir()
    {
        //// In desgn mode we just give it an place so it can preview the axaml, otherwise return
        //if (Design.IsDesignMode)
        //{
        //    ApplicationParams.InitApplicationOptions();
        //    AppDataFolder = ApplicationParams.Options!.AppDataFolder!;
        //}
        if (string.IsNullOrEmpty(AppDataFolder))
            throw new InvalidOperationException("AppDataFolder not set");
        return AppDataFolder;
    }


    public static void InitializeExchange()
    {
        // If application params contain an exchange this is leading
        // Otherwise we take the one from the settings
        string? exchangeName = ApplicationParams.Options!.ExchangeName;
        if (exchangeName != null)
        {
            // People forget to use the right casing
            exchangeName = exchangeName.Trim().ToLower();
            string? found = ExchangeListName.Values.Where(x => x.Name.Equals(exchangeName, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault()?.Name;
            if (found != null)
                exchangeName = found;
            Settings.General.ExchangeName = exchangeName;
        }


        if (ExchangeListName.TryGetValue(Settings.General.ExchangeName, out var exchange))
            GlobalData.ActiveExchange = exchange;
        else
            throw new Exception($"Exchange {Settings.General.ExchangeName} does not exist");
    }


    //public static void DumpSessionInformation()
    //{
    //    foreach (Model.CryptoExchange exchange in ExchangeListName.Values.ToList())
    //    {
    //        int candleCount = 0;
    //        foreach (Model.CryptoSymbol symbol in exchange.SymbolListName.Values.ToList())
    //        {
    //            foreach (Model.CryptoSymbolInterval symbolInterval in symbol.SymbolIntervalList.ToList())
    //            {
    //                candleCount += symbolInterval.CandleList.Count;
    //                if (symbolInterval.CandleList.Count > 0)
    //                    AddTextToLogTab(string.Format("{0} {1} {2} candlecount={3}", exchange.Name, symbol.Name, symbolInterval.Interval.Name, symbolInterval.CandleList.Count), false);

    //            }
    //        }

    //        AddTextToLogTab(string.Format("{0} symbolcount={1} candlecount={2}", exchange.Name, exchange.SymbolListName.Count, candleCount), false);
    //    }
    //}

    // Index for the most important strategies
    public static void AddStrategySettings()
    {
        StrategiesSettings = [];
        StrategiesSettings.Add(CryptoSignalStrategy.Jump, (Settings.Signal.Jump, 0));
        StrategiesSettings.Add(CryptoSignalStrategy.Stobb, (Settings.Signal.Stobb, 0));
        StrategiesSettings.Add(CryptoSignalStrategy.StobbMulti, (Settings.Signal.Stobb, 0));
        StrategiesSettings.Add(CryptoSignalStrategy.Sbm1, (Settings.Signal.Sbm, 0));
        StrategiesSettings.Add(CryptoSignalStrategy.Sbm2, (Settings.Signal.Sbm, 0));
        StrategiesSettings.Add(CryptoSignalStrategy.Sbm3, (Settings.Signal.Sbm, 0));
        StrategiesSettings.Add(CryptoSignalStrategy.StoRsi, (Settings.Signal.StoRsi, 0));
        StrategiesSettings.Add(CryptoSignalStrategy.StoRsiMulti, (Settings.Signal.StoRsi, 0));
        StrategiesSettings.Add(CryptoSignalStrategy.NadarayaWatsonEnvelope, (Settings.Signal.Nwe, 0));
        StrategiesSettings.Add(CryptoSignalStrategy.DominantLevel, (Settings.Signal.ZonesDlz, 0));
        StrategiesSettings.Add(CryptoSignalStrategy.DominantLevelNear, (Settings.Signal.ZonesDlz, 0));
        StrategiesSettings.Add(CryptoSignalStrategy.FairValueGap, (Settings.Signal.ZonesFvg, 0));
    }
}
