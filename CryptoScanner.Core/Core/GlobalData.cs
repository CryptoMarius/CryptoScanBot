using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.SignalR;
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
    public static object? MainWindow { get; set; } = null!;

    public static string AppPath { get; set; } = ""; // For sounds
    public static string LogName { get; set; } = "";
    public static string AppVersion { get; set; } = "";
    public static string AppDataFolder { get; set; } = ""; // depends on startup parameters (also in platformService)
    public static string CandleDataFolder { get; set; } = ""; // separate folder for per-exchange candle DBs (falls back to AppDataFolder when empty)

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

    public static Action<Action>? RunOnUiThread { get; set; }

    public static void SendMvvmMessage<TMessage>(TMessage message) where TMessage : class
    {
        if (RunOnUiThread != null)
            RunOnUiThread(() => { WeakReferenceMessenger.Default.Send(message); });
        else
            WeakReferenceMessenger.Default.Send(message);
    }


    public static Action<string>? SetTheme { get; set; }
    public static Action<string>? SetTitle { get; set; }

    /// <summary>
    /// The window caption: application, version, exchange and the user's own extra caption. Every
    /// host builds its title from this, so a machine running several instances stays readable in
    /// the taskbar and in the task manager (which shows the window title per process).
    /// </summary>
    public static string ApplicationTitle =>
        $"{Constants.AppName} {AppVersion} {Settings.General.ExchangeName} {Settings.General.ExtraCaption}".Trim();

    public static Action? RequestShutdown { get; set; }

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

    /// <summary>
    /// Write a line to the log file and show it on the log tab.
    ///
    /// The NLog write lives here and no longer in the UI subscribers. Every host (the Avalonia
    /// scanner, the Photino UI, the emulator) used to mirror the line into NLog itself, which made
    /// on-disk logging depend on a ViewModel being alive and subscribed, and made it impossible to
    /// log a line at anything other than Info - see <see cref="AddErrorToLogTab"/>.
    /// </summary>
    public static void AddTextToLogTab(string text)
    {
        // Empty lines are separators for the log tab only; they carry nothing in the file.
        if (!string.IsNullOrWhiteSpace(text))
            WriteToLogFile(text, false);
        LogToLogTabEvent?.Invoke(text);
    }

    /// <summary>
    /// Same as <see cref="AddTextToLogTab"/>, but the line is written at Error level so it also
    /// lands in the separate error log file (see ScannerLog.InitializeLogging). Use this for
    /// anything that actually went wrong - a rejected API call, a failed fetch - so the error log
    /// is a real summary of the failures instead of staying empty while the main log hides them.
    /// </summary>
    public static void AddErrorToLogTab(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            WriteToLogFile(text, true);
        LogToLogTabEvent?.Invoke(text);
    }

    private static void WriteToLogFile(string text, bool isError)
    {
        // Wrapped so a logging failure can never break a scan or a run
        try
        {
            if (isError)
                ScannerLog.Logger.Error(text);
            else
                ScannerLog.Logger.Info(text);
        }
        catch
        {
            // ignore - never let logging crash the caller
        }
    }

    // Events for refresing data
    public static event AddTextEvent? TelegramHasChangedEvent;
    public static void TelegramHasChanged(string text) => TelegramHasChangedEvent?.Invoke(text);
    //public static event AddTextEvent? AssetsHaveChangedEvent;
    //public static void AssetsHaveChanged(string text) => AssetsHaveChangedEvent?.Invoke(text);

    // Ophalen van historische candles duurt lang, dus niet halverwege nog 1 starten (en nog 1 en...)
    public static event SetCandleTimerEnable? SetCandleTimerEnableEvent;
    public static void SetCandleTimerEnable(bool value) => SetCandleTimerEnableEvent?.Invoke(value);

    public static AnalyseEvent? AnalyzeSignalCreated { get; set; }

    public static Action<CryptoPosition>? PositionCreated { get; set; }
    public static Action<CryptoPosition>? PositionClosed { get; set; }
    public static Action<CryptoPosition>? PositionDeleted { get; set; }
    public static Action? PositionDeletedAll { get; set; }

    public static SignalRService? SignalRService { get; set; }


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
    public static Dictionary<string, (SettingsSignalStrategyBase strategySettings, DateTime lastSignalStrategy)> StrategiesSettings = [];


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

        if (GlobalData.ActiveExchange != null)
        {
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
        }
        return list;
    }


    /// <summary>
    /// Load all assets. The implementation (including seeding the start capital for the quote coins
    /// we trade) lives in <see cref="Trader.PaperAssets.LoadAssets"/> - this used to be a second,
    /// identical copy next to the one in TradeTools.
    /// </summary>
    public static void LoadAssets()
    {
        //GlobalData.AddTextToLogTab("Reading asset information");

        if (GlobalData.ActiveExchange != null)
            Trader.PaperAssets.LoadAssets(GlobalData.ActiveExchange);
    }

    public static void AddExchange(Model.CryptoExchange exchange)
    {
        if (!ExchangeListName.ContainsKey(exchange.Name))
        {
            ExchangeListId.Add(exchange.Id, exchange);
            ExchangeListName.Add(exchange.Name, exchange);
        }
    }


    /// <summary>
    /// Quotes with a (roughly) fixed dollar/euro value, so an amount expressed in them is
    /// meaningful without knowing the current rate.
    /// </summary>
    private static readonly string[] StableQuotes = ["USD", "USDC", "USDT", "EUR"];

    /// <summary>
    /// Entry amount given to a newly discovered stable quote. Without it a new quote starts at
    /// zero, which is not a usable setting - at least this way there is a default.
    /// </summary>
    private const decimal DefaultEntryAmount = 15m;

    public static CryptoQuoteData AddQuoteData(string quoteName)
    {
        if (!Settings.QuoteCoins.TryGetValue(quoteName, out CryptoQuoteData? quoteData))
        {
            quoteData = new CryptoQuoteData
            {
                Name = quoteName,
                DisplayFormat = "N8",
            };

            // Only for a quote the settings have never seen: an existing (user adjusted) amount,
            // zero included, must never be overwritten on a later startup.
            if (StableQuotes.Contains(quoteName))
                quoteData.EntryAmount = DefaultEntryAmount;

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



    /// <summary>
    /// Serialises every read and write of the configuration files. SaveConfiguration is reached
    /// from the configuration screen, from the Telegram bot commands and from the shutdown path,
    /// each on its own thread. Two of those writing the same file at the same moment left one of
    /// them with an IOException, and the settings of whoever lost simply never reached disk.
    /// </summary>
    private static readonly object ConfigurationFileLock = new();

    /// <summary>
    /// Set when the settings file could not be read at startup. Everything then runs on the
    /// defaults, which is survivable — but writing those defaults back would turn a json that can
    /// still be repaired by hand into the permanent loss of every setting. So while this is set,
    /// <see cref="SaveConfiguration"/> refuses to touch the file.
    /// </summary>
    public static bool SettingsLoadFailed { get; private set; }

    /// <summary>
    /// Write a configuration file without ever leaving a half-written one behind: serialise to a
    /// temporary file next to it, then swap them in one step. File.Replace also keeps the previous
    /// version as .backup, which is what LoadJsonFile falls back on.
    /// </summary>
    private static void WriteJsonFile<T>(string folder, string fileName, T value)
    {
        string filename = Path.Combine(folder, fileName);
        string temporary = filename + ".tmp";
        string backup = filename + ".backup";

        string text = JsonSerializer.Serialize(value, JsonTools.JsonSerializerIndented);
        File.WriteAllText(temporary, text);

        if (File.Exists(filename))
            File.Replace(temporary, filename, backup);
        else
            File.Move(temporary, filename);
    }

    /// <summary>
    /// Read a configuration file, falling back on the .backup left by <see cref="WriteJsonFile"/>
    /// when the file itself cannot be parsed. Returns null when neither can be read; the caller
    /// decides what that means for its own settings object.
    /// </summary>
    private static T? ReadJsonFile<T>(string folder, string fileName) where T : class
    {
        string filename = Path.Combine(folder, fileName);
        if (!File.Exists(filename))
            return null;

        try
        {
            using FileStream stream = File.OpenRead(filename);
            return JsonSerializer.Deserialize<T>(stream, JsonTools.DeSerializerOptions);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, $"Error reading {fileName}");
            AddErrorToLogTab($"Error reading {fileName}: {error.Message}");
        }

        string backup = filename + ".backup";
        if (!File.Exists(backup))
            return null;

        try
        {
            using FileStream stream = File.OpenRead(backup);
            var value = JsonSerializer.Deserialize<T>(stream, JsonTools.DeSerializerOptions);
            if (value != null)
                AddTextToLogTab($"Recovered {fileName} from {fileName}.backup");
            return value;
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, $"Error reading {fileName}.backup");
            AddErrorToLogTab($"Error reading {fileName}.backup: {error.Message}");
            return null;
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
                var value = ReadJsonFile<SettingsBasic>(GlobalData.AppDataFolder, $"{Constants.AppName}-settings.json");
                if (value != null)
                {
                    Settings = value;
                }
                else
                {
                    // Neither the file nor its backup could be read. Carry on with the defaults so
                    // the application still starts, but mark it so nothing writes over the file
                    // that is still sitting there waiting to be repaired.
                    Settings = new();
                    SettingsLoadFailed = true;
                    AddTextToLogTab("The settings could not be read. Running on defaults, and the "
                        + "settings file will NOT be overwritten - repair or remove it first.");
                }
            }

            // Fix, sometimes people set this at 1 and that is not what I expected
            if (Settings!.General.GetCandleInterval < 30)
                Settings.General.GetCandleInterval = 30;

            // A settings file written before 27-08-2026 names a market that no longer exists
            Settings.General.ExchangeName = FixLegacyExchangeName(Settings.General.ExchangeName);
            Settings.General.ActivateExchangeName = FixLegacyExchangeName(Settings.General.ActivateExchangeName);

            // Fill in empty activate exchange
            if (Settings.General.ActivateExchangeName == "")
                Settings.General.ActivateExchangeName = Settings.General.ExchangeName;
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddErrorToLogTab("Error loading setting " + error.ToString());
        }
    }


    /// <summary>
    /// Translates a market name from before 27-08-2026, when every derivatives market was called
    /// "&lt;exchange&gt; Futures". Those markets are perpetuals and the name says so now. The database
    /// rows are renamed by the migration, but a settings file, an emulator run configuration or an
    /// -e parameter written before that day still carries the old name - and a name that resolves to
    /// nothing makes the application refuse to start.
    /// Anything else is returned unchanged.
    /// </summary>
    public static string FixLegacyExchangeName(string exchangeName)
    {
        const string legacy = " Futures";
        if (exchangeName != null && exchangeName.EndsWith(legacy, StringComparison.OrdinalIgnoreCase))
            return string.Concat(exchangeName.AsSpan(0, exchangeName.Length - legacy.Length), " Perpetual");
        return exchangeName ?? "";
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
            AddErrorToLogTab($"Error loading {filename} " + error.ToString());
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
                var value = ReadJsonFile<SettingsTelegram>(AppDataFolder, fileName);
                if (value != null)
                    Telegram = value;
                else
                    Telegram = new();
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddErrorToLogTab($"Error loading {fileName} " + error.ToString());
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
                var value = ReadJsonFile<SettingsExchangeApi>(AppDataFolder, fileName);
                if (value != null)
                    TradingApi = value;
                else
                    TradingApi = new();
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddErrorToLogTab($"Error loading {fileName} " + error.ToString());
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
                var value = ReadJsonFile<SettingsAltradyApi>(AppDataFolder, fileName);
                if (value != null)
                    AltradyApi = value;
                else
                    AltradyApi = new();
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            AddErrorToLogTab($"Error loading {fileName} " + error.ToString());
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

    /// <summary>
    /// Write the configuration files. Logs and rethrows on failure: the caller has to know, because
    /// silently returning here is what made a failed save look like a successful one - the screen
    /// closed, and at the next start the previous values were back.
    /// </summary>
    public static void SaveConfiguration()
    {
        if (SettingsLoadFailed)
        {
            AddTextToLogTab("Not saving: the settings could not be read at startup, so what is in "
                + "memory are the defaults. Repair or remove the settings file first.");
            return;
        }

        lock (ConfigurationFileLock)
        {
            try
            {
                string baseFolder = AppDataFolder;
                Directory.CreateDirectory(baseFolder);

                Contracts.PluginManager.CollectSettings(Settings.Signal.AnalyzerSettings);

                WriteJsonFile(baseFolder, $"{Constants.AppName}-settings.json", Settings);
                WriteJsonFile(baseFolder, $"{Constants.AppName}-telegram.json", Telegram);
                WriteJsonFile(baseFolder, $"{Constants.AppName}-exchange.json", TradingApi);
                WriteJsonFile(baseFolder, $"{Constants.AppName}-altrady.json", AltradyApi);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "SaveConfiguration");
                AddErrorToLogTab("Error saving the settings: " + error.ToString());
                throw;
            }
        }

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
            AddErrorToLogTab("Error playing music " + error.ToString());
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
            AddErrorToLogTab("Error playing speech " + error.ToString());
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
                AddErrorToLogTab(" error telegram thread(1)" + error.ToString());
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
                AddErrorToLogTab(" error telegram thread(1)" + error.ToString());
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
            // Use the platform path separator; a hardcoded "\" produced invalid paths on macOS/Linux
            // (e.g. ~/.config/CryptoScanBot\CryptoScanBot.db).
            AppDataFolder += Path.DirectorySeparatorChar;
        }
        return AppDataFolder;
    }


    // Index for the available strategies (available via ui)
    public static void IndexStrategySettings()
    {
        StrategiesSettings = [];

        // Merge settings from dynamically loaded strategy plugins
        foreach (var (strategy, plugin) in PluginManager.LoadedPlugins)
        {
            StrategiesSettings.Add(strategy, (plugin.SettingsBase, DateTime.Today));
        }
    }
}