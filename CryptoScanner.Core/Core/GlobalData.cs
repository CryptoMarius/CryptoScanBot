using Avalonia.Controls;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trader;
//using CryptoScanner.Core.TradingView;
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

    public static bool ApplicationIsClosing { get; set; } = false;


    // Mode flag for side-effect gates (sounds, telegram, websocket subscriptions, sleeps).
    // Replaces the legacy "BackTest" boolean. In a later phase these gates are replaced by
    // interface stubs (INotifier, ISoundPlayer, ISubscriptionManager).
    public static bool IsEmulatorMode { get; set; }

    // EmulatorRun.Id of the currently active emulator run, or null for live scanner.
    // Set by the TickRunner at run-start, cleared at run-end. Signals and positions created
    // while non-null are tagged with this id so the same DB can hold multiple runs.
    public static int? CurrentEmulatorRunId { get; set; }


    private static CryptoApplicationStatus _applicationStatus = CryptoApplicationStatus.Initializing;
    public static CryptoApplicationStatus ApplicationStatus
    {
        get { return _applicationStatus; }
        set
        {
            if (_applicationStatus != value)
            {
                _applicationStatus = value;
                SendMvvmMessage(new StatusesHaveChangedMessage());
            }
        }
    }

    public static void SendMvvmMessage<TMessage>(TMessage message) where TMessage : class
    {
        Dispatcher.UIThread.Post(() => { WeakReferenceMessenger.Default.Send(message); }); // Avalonia
        //MainForm!.BeginInvoke(() => { WeakReferenceMessenger.Default.Send(message); }); // Winforms
    }


    // Amount of signals created
    public static int CreatedSignalCount { get; set; }

    // Progress text during candle fetching (e.g. "Loading candles 42 / 350 (BTCUSDT)")
    public static string CandleProgressText { get; set; } = "";

    /// <summary>
    /// Scanner settings
    /// </summary>
    public static SettingsBasic Settings { get; set; } = new();

    /// <summary>
    /// Wall-clock abstraction. Default is <see cref="SystemClock"/> (delegates to DateTime.UtcNow).
    /// The emulator swaps in <see cref="EmulatorClock"/> and advances it per replayed candle so
    /// signal/position timestamps become deterministic. Read via <c>GlobalData.Clock.UtcNow</c>.
    /// </summary>
    public static IClock Clock { get; set; } = new SystemClock();

    /// <summary>
    /// Exchange API settings
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
    //public static readonly List<CryptoPosition> PositionsClosed = [];
    public static readonly Queue<CryptoLiveData> LiveDataQueue = [];
    public static readonly Dictionary<(string, CryptoIntervalPeriod), CryptoLiveData> LiveDataQueueAdded = [];

    public static event PlayMediaEvent? PlaySound;
    public static event PlayMediaEvent? PlaySpeech;
    public static event AddTextEvent? LogToTelegram;

    public static event AddTextEvent? LogToLogTabEvent;
    public static void AddTextToLogTab(string text) => LogToLogTabEvent?.Invoke(text);

    // Events for refresing data
    public static event AddTextEvent? TelegramHasChangedEvent;
    public static void TelegramHasChanged(string text) => TelegramHasChangedEvent?.Invoke(text);
    //public static event AddTextEvent? AssetsHaveChangedEvent;
    //public static void AssetsHaveChanged(string text) => AssetsHaveChangedEvent?.Invoke(text);

    // Ophalen van historische candles duurt lang, dus niet halverwege nog 1 starten (en nog 1 en...)
    public static event SetCandleTimerEnable? SetCandleTimerEnableEvent;
    public static void SetCandleTimerEnable(bool value) => SetCandleTimerEnableEvent?.Invoke(value);

    public static AnalyseEvent? AnalyzeSignalCreated { get; set; }


    // Active exchange
    public static Model.CryptoExchange? ActiveExchange { get; set; }
    //public static string ActivateExchangeName { get; set; } = "";


    // Some running tasks/threads
    public static ThreadSaveObjects? ThreadSaveObjects { get; set; }
    public static ThreadMonitorCandle? ThreadMonitorCandle { get; set; }
    public static ThreadMonitorOrder? ThreadMonitorOrder { get; set; }
    public static ThreadCheckFinishedPosition? ThreadCheckPosition { get; set; }
    public static ZoneThreadCalculate? ThreadZoneCalculate { get; set; }


    // Indexed strategies for colors and soundfiles etc...
    public static Dictionary<CryptoSignalStrategy, (SettingsSignalStrategyBase strategySettings, DateTime lastSignalStrategy)> StrategiesSettings = [];


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
            // Correct interval to minutes instead of seconds..
            switch (interval.IntervalPeriod)
            {
                case CryptoIntervalPeriod.interval1m:
                    interval.Duration = 1;
                    break;
                case CryptoIntervalPeriod.interval2m:
                    interval.Duration = 2;
                    break;
                case CryptoIntervalPeriod.interval3m:
                    interval.Duration = 3;
                    break;
                case CryptoIntervalPeriod.interval5m:
                    interval.Duration = 5;
                    break;
                case CryptoIntervalPeriod.interval10m:
                    interval.Duration = 10;
                    break;
                case CryptoIntervalPeriod.interval15m:
                    interval.Duration = 15;
                    break;
                case CryptoIntervalPeriod.interval30m:
                    interval.Duration = 30;
                    break;
                case CryptoIntervalPeriod.interval1h:
                    interval.Duration = 1 * 60;
                    break;
                case CryptoIntervalPeriod.interval2h:
                    interval.Duration = 2 * 60;
                    break;
                case CryptoIntervalPeriod.interval3h:
                    interval.Duration = 3 * 60;
                    break;
                case CryptoIntervalPeriod.interval4h:
                    interval.Duration = 4 * 60;
                    break;
                case CryptoIntervalPeriod.interval6h:
                    interval.Duration = 6 * 60;
                    break;
                case CryptoIntervalPeriod.interval8h:
                    interval.Duration = 8 * 60;
                    break;
                case CryptoIntervalPeriod.interval12h:
                    interval.Duration = 12 * 60;
                    break;

                case CryptoIntervalPeriod.interval1d:
                    interval.Duration = 24 * 60;
                    break;
                case CryptoIntervalPeriod.interval1w:
                    interval.Duration = 7 * 24 * 60;
                    break;
            }

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
        foreach (CryptoSymbol symbol in database.Connection.Query<CryptoSymbol>(sql, new { exchangeid = GlobalData.ActiveExchange!.Id }))
            AddSymbol(symbol);
    }

    public static List<CryptoSignal> LoadSignals(string filterText = "")
    {
        List<CryptoSignal> list = [];

        // Single codepath now that the live scanner and the emulator each have their own
        // database — there is no need to filter on a per-row BackTest flag anymore.
        string sql;
        using var database = new CryptoDatabase();
        if (string.IsNullOrEmpty(filterText))
            sql = "select * from signal where exchangeid=@exchangeid and ExpirationDate >= @FromDate order by OpenDate";
        else
        {
            sql = "select * from signal " +
                "inner join symbol on signal.symbolid=symbol.id " +
                "where signal.exchangeid=@exchangeid and signal.ExpirationDate >= @FromDate " +
                $"and symbol.name like '%{filterText}%' " +
                "order by signal.OpenDate ";
        }

        foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql,
            new { FromDate = Clock.UtcNow, exchangeid = GlobalData.ActiveExchange!.Id }))
        {
            if (signal.IsInvalid && !GlobalData.Settings.General.ShowInvalidSignals)
                continue;

            if (ExchangeListId.TryGetValue(signal.ExchangeId, out Model.CryptoExchange? exchange2))
            {
                signal.Exchange = exchange2;

                if (exchange2.SymbolListId.TryGetValue(signal.SymbolId, out CryptoSymbol? symbol))
                {
                    signal.Symbol = symbol;

                    if (IntervalListId.TryGetValue(signal.IntervalId, out CryptoInterval? interval))
                        signal.Interval = interval;

                    list.Add(signal);
                }
            }
        }
        return list;
    }


    public static void LoadAssets()
    {
        //GlobalData.AddTextToLogTab("Reading asset information");

        if (GlobalData.ActiveExchange != null)
        {
            // Load all assets
            GlobalData.ActiveExchange.Data.AssetList.Clear();

            using var database = new CryptoDatabase();
            foreach (CryptoAsset asset in database.Connection.GetAll<CryptoAsset>())
            {
                GlobalData.ActiveExchange.Data.AssetList.TryAdd(asset.Name, asset);
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
        if (ApplicationParams.Options != null && !string.IsNullOrEmpty(ApplicationParams.Options.AppLimitSymbols))
        {
            // Test with limits anount of symbols for debugging purposes
            if (!
                (
                symbol.Base.Equals("BTC") || symbol.Base.Equals("ETH") ||
                symbol.Base.Equals("ADA") || symbol.Base.Equals("SOL") ||
                symbol.Base.Equals("TRX") || symbol.Base.Equals("ENA") ||
                symbol.Base.Equals("ZKJ") || symbol.Base.Equals("SUI") ||
                symbol.Base.Equals("ZEC") || symbol.Base.Equals("XRP") ||
                symbol.Base.Equals("MTL") || symbol.Base.Equals("XTZ") ||
                symbol.Base.Equals("MAGIC") || symbol.Base.Equals("ROSE") ||
                symbol.Base.StartsWith("$BMP")
              ))
                return;
        }

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

            symbol.QuoteData = AddQuoteData(symbol.Quote);

            string seperator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;


            int numberOfDecimalPlaces;
            string s = symbol.PriceTickSize.ToString0();
            int x = s.IndexOf(seperator);
            if (x > 0)
            {
                s = s[(x + 1)..];
                numberOfDecimalPlaces = s.Length;
            }
            else numberOfDecimalPlaces = 0;
            symbol.PriceDecimals = (byte)numberOfDecimalPlaces;
            symbol.PriceDisplayFormat = "N" + numberOfDecimalPlaces.ToString();


            s = symbol.QuantityTickSize.ToString0();
            x = s.IndexOf(seperator);
            if (x > 0)
            {
                s = s[(x + 1)..];
                numberOfDecimalPlaces = s.Length;
            }
            else numberOfDecimalPlaces = 0;
            symbol.QuantityDisplayFormat = "N" + numberOfDecimalPlaces.ToString();

            // reset last price
            symbol.LastPrice = null;
        }
    }



    public static void LoadScannerConfiguration()
    {
        try
        {
            string filename = Path.Combine(GlobalData.AppDataFolder, $"{Constants.AppName}-settings.json");
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

            // Fill in empty activate exchange
            if (Settings.General.ActivateExchangeName == "")
                Settings.General.ActivateExchangeName = Settings.General.ExchangeName;
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab("Error loading setting " + error.ToString());
        }
    }


    public static void LoadWebLinkConfiguration()
    {
        string filename = $"{Constants.AppName}-weblinks.json";
        try
        {
            string fullName = Path.Combine(GlobalData.AppDataFolder, filename);
            if (File.Exists(fullName))
            {
                File.Delete(fullName);
                //string text = File.ReadAllText(fullName);
                //var value = JsonSerializer.Deserialize<CryptoExternalUrlList>(text, JsonTools.DeSerializerOptions);
                //if (value != null)
                //    ExternalUrls = value;
                //else
                //    ExternalUrls = [];
                //ExternalUrls!.InitializeUrls(); // add new exchanges
            }

            //else
            {
                ExternalUrls = []; // start from scratch (do not cache in memory)
                ExternalUrls.InitializeUrls(); // add new exchanges
                // het bestand in ieder geval aanmaken(updates moeten achteraf gepushed worden)
                //string text = JsonSerializer.Serialize(ExternalUrls, JsonTools.JsonSerializerIndented);
                //File.WriteAllText(fullName, text);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddTextToLogTab($"Error loading {filename} " + error.ToString());
        }
    }


    public static void LoadTelegramConfiguration()
    {
        string fileName = $"{Constants.AppName}-telegram.json";
        try
        {
            string fullName = Path.Combine(GlobalData.AppDataFolder, fileName);
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
            AddTextToLogTab($"Error loading {fileName} " + error.ToString());
        }
    }

    public static void LoadExchangeConfiguration()
    {
        string fileName = $"{Constants.AppName}-exchange.json";
        try
        {
            string fullName = Path.Combine(AppDataFolder, fileName);
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
            AddTextToLogTab($"Error loading {fileName} " + error.ToString());
        }
    }

    public static void LoadAltradyConfiguration()
    {
        string fileName = $"{Constants.AppName}-altrady.json";
        try
        {
            string fullName = Path.Combine(AppDataFolder, fileName);
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
            AddTextToLogTab($"Error loading {fileName} " + error.ToString());
        }
    }



    public static void LoadConfiguration()
    {
        LoadScannerConfiguration();
        LoadExchangeConfiguration();
        LoadTelegramConfiguration();
        LoadAltradyConfiguration();
        LoadWebLinkConfiguration();
    }


    public static void DefaultConfiguration()
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

    public static void SaveConfiguration()
    {
        string baseFolder = AppDataFolder;
        Directory.CreateDirectory(baseFolder);

        string filename = Path.Combine(baseFolder, $"{Constants.AppName}-settings.json");
        string text = JsonSerializer.Serialize(Settings, JsonTools.JsonSerializerIndented);
        File.WriteAllText(filename, text);

        filename = Path.Combine(baseFolder, $"{Constants.AppName}-telegram.json");
        text = JsonSerializer.Serialize(Telegram, JsonTools.JsonSerializerIndented);
        File.WriteAllText(filename, text);

        filename = Path.Combine(baseFolder, $"{Constants.AppName}-exchange.json");
        text = JsonSerializer.Serialize(TradingApi, JsonTools.JsonSerializerIndented);
        File.WriteAllText(filename, text);

        filename = Path.Combine(baseFolder, $"{Constants.AppName}-altrady.json");
        text = JsonSerializer.Serialize(AltradyApi, JsonTools.JsonSerializerIndented);
        File.WriteAllText(filename, text);

        //#if DEBUG
        ////// Ter debug om te zien of alles okay is
        //fileName = GlobalData.AppDataFolder;
        //Directory.CreateDirectory(fileName);
        //fileName += Path.Combine("settingsSignalsCompiled.json";
        //text = JsonSerializer.Serialize(TradingConfig.Signals, options);
        //File.WriteAllText(fileName, text);

        //fileName = GlobalData.AppDataFolder;
        //Directory.CreateDirectory(fileName);
        //fileName += Path.Combine("settingsTradingCompiled.json";
        //text = JsonSerializer.Serialize(TradingConfig.Trading, options);
        //File.WriteAllText(fileName, text);
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
        if (!IsEmulatorMode)
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
        if (!IsEmulatorMode)
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


    public static string GetBaseDir()
    {
        if (string.IsNullOrEmpty(AppDataFolder))
        {
            ApplicationParams.InitApplicationOptions();
            AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ApplicationParams.Options?.AppDataFolder ?? Constants.AppName);
            Directory.CreateDirectory(AppDataFolder);
            AppDataFolder += @"\";
        }
        return AppDataFolder;
    }


    // Index for the available strategies (available via ui)
    public static void IndexStrategySettings()
    {
        StrategiesSettings = [];
        StrategiesSettings.Add(CryptoSignalStrategy.Jump, (Settings.Signal.Jump, DateTime.Today));
        StrategiesSettings.Add(CryptoSignalStrategy.Stobb, (Settings.Signal.Stobb, DateTime.Today));
        StrategiesSettings.Add(CryptoSignalStrategy.StobbMulti, (Settings.Signal.Stobb, DateTime.Today));
        StrategiesSettings.Add(CryptoSignalStrategy.Sbm1, (Settings.Signal.Sbm, DateTime.Today));
        StrategiesSettings.Add(CryptoSignalStrategy.Sbm2, (Settings.Signal.Sbm, DateTime.Today));
        StrategiesSettings.Add(CryptoSignalStrategy.Sbm3, (Settings.Signal.Sbm, DateTime.Today));
        StrategiesSettings.Add(CryptoSignalStrategy.StoRsi, (Settings.Signal.StoRsi, DateTime.Today));
        //StrategiesSettings.Add(CryptoSignalStrategy.StoRsiMulti, (Settings.Signal.StoRsi, DateTime.Today));
        StrategiesSettings.Add(CryptoSignalStrategy.Nwe, (Settings.Signal.Nwe, DateTime.Today));
#if DEBUG
        StrategiesSettings.Add(CryptoSignalStrategy.NweNp, (Settings.Signal.Nwe, DateTime.Today));
#endif
        StrategiesSettings.Add(CryptoSignalStrategy.DominantLevel, (Settings.Signal.ZonesDlz, DateTime.Today));
        StrategiesSettings.Add(CryptoSignalStrategy.DominantLevelNear, (Settings.Signal.ZonesDlz, DateTime.Today));
        //StrategiesSettings.Add(CryptoSignalStrategy.StobbDlz, (Settings.Signal.ZonesDlz, DateTime.Today));
        //StrategiesSettings.Add(CryptoSignalStrategy.StoRsiDlz, (Settings.Signal.ZonesDlz, DateTime.Today));

        StrategiesSettings.Add(CryptoSignalStrategy.FairValueGap, (Settings.Signal.ZonesFvg, DateTime.Today));
        //StrategiesSettings.Add(CryptoSignalStrategy.StobbFvg, (Settings.Signal.ZonesFvg, DateTime.Today));
        //StrategiesSettings.Add(CryptoSignalStrategy.StoRsiFvg, (Settings.Signal.ZonesFvg, DateTime.Today));
#if DEBUG
        StrategiesSettings.Add(CryptoSignalStrategy.WtLbStoch, (Settings.Signal.WtLbStoch, DateTime.Today));
        StrategiesSettings.Add(CryptoSignalStrategy.WaveTrend, (Settings.Signal.WaveTrend, DateTime.Today));
        StrategiesSettings.Add(CryptoSignalStrategy.BbmaOmni, (Settings.Signal.Bbma, DateTime.Today));
#endif
#if DEBUG
        StrategiesSettings.Add(CryptoSignalStrategy.OrderBlock, (Settings.Signal.ZonesSmc, DateTime.Today));
        StrategiesSettings.Add(CryptoSignalStrategy.OrderBlockRejection, (Settings.Signal.ZonesSmc, DateTime.Today));
#endif
        StrategiesSettings.Add(CryptoSignalStrategy.Baba, (Settings.Signal.Baba, DateTime.Today));
    }
}