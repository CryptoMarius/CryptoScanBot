using CryptoScanner.Analyzers.Mac;
using CryptoScanner.Analyzers.Mac.Indicators;
using CryptoScanner.Analyzers.Mac.Signal;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Indicators;

using Microsoft.Data.Sqlite;

using Skender.Stock.Indicators;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// A measuring instrument, not a unit test: it runs the SHIPPED strategy over the scanner's own
/// candles and writes down, per candle, what it fires.
/// <para>
/// The point is that a rule written out in a side language proves nothing about the scanner. What
/// the chart of the strategy has to be held against is this code, with its own conditions - the
/// cloud that has to point the right way, the level that has to be old enough, the settings as
/// they really are. The result goes to a file that the counting script reads.
/// </para>
/// <para>
/// It is skipped when the candle database is not there, so it never breaks a run elsewhere.
/// </para>
/// </summary>
[DoNotParallelize]
[TestClass]
public class MacMarkerComparisonTests : TestBase
{
    private const string Database = @"E:\CryptoScanBot\Data\Binance\Perpetual\Binance Perpetual.db";
    private const string Output = @"E:\Projects\CryptoScanBot.tools\studies\macloud\mac-scanner-signals.csv";
    // The Speed input of the indicator moves the lengths of all four lines, so it changes the
    // signals themselves. Comparing those against a harvest taken on Standard would only show
    // that they differ, which is why this goes into a file of its own with the speed per row.
    private const string SpeedOutput = @"E:\Projects\CryptoScanBot.tools\studies\macloud\mac-scanner-signals-speed.csv";

    // The identifier of an interval in the candle database is the enum value plus one. More than
    // one interval is run now: the fifteen minute chart is the second, independent set the run
    // reading of the break was to be judged against, and it was harvested straight off the live
    // chart instead of from screenshots.
    private static readonly (string Name, int Id, CryptoIntervalPeriod Period, string[] Symbols)[] Runs =
    [
        ("5m", 4, CryptoIntervalPeriod.interval5m,
            ["BTCUSDT.PERP", "ETHUSDT.PERP", "SOLUSDT.PERP", "NEARUSDT.PERP"]),
        // Six coins at fifteen minutes, not four: the run reading of the break fired nothing false
        // on bitcoin alone, which could just as well mean its thresholds were fitted to bitcoin.
        ("15m", 6, CryptoIntervalPeriod.interval15m,
            ["BTCUSDT.PERP", "ETHUSDT.PERP", "SOLUSDT.PERP", "NEARUSDT.PERP",
             "HYPEUSDT.PERP", "XRPUSDT.PERP", "DOGEUSDT.PERP",
             "ADAUSDT.PERP", "LINKUSDT.PERP", "AVAXUSDT.PERP", "SUIUSDT.PERP"]),
        ("1h", 8, CryptoIntervalPeriod.interval1h, ["BTCUSDT.PERP"]),
        // The same eleven coins on FOUR HOUR candles, harvested 23 September 2026. Our four hour
        // candles start 2 July 2026, so the judgeable window begins on 4 August. 281 markers, of
        // which 91 breaks - and this set leans the other way round from the daily one, 68 Breakouts
        // against 23 Breakdowns.
        ("4h", 11, CryptoIntervalPeriod.interval4h,
            ["BTCUSDT.PERP", "ETHUSDT.PERP", "SOLUSDT.PERP", "NEARUSDT.PERP",
             "HYPEUSDT.PERP", "XRPUSDT.PERP", "DOGEUSDT.PERP",
             "ADAUSDT.PERP", "LINKUSDT.PERP", "AVAXUSDT.PERP", "SUIUSDT.PERP"]),
        // The same eleven coins on DAILY candles, harvested 23 September 2026. A year of market
        // instead of ten days, and the break markers alone come to 91 - almost half again what the
        // fifteen minute set holds. Our daily candles start 10 May 2025, so the judgeable window
        // begins two hundred candles later, on 26 November.
        // 1000PEPE was added on 28 September 2026 as a coin the rule was NEVER fitted on, and with
        // a price three orders of magnitude smaller than the rest - which is where a rounding
        // mistake in the levels or in the wick comparison would show first.
        ("1d", 15, CryptoIntervalPeriod.interval1d,
            ["BTCUSDT.PERP", "ETHUSDT.PERP", "SOLUSDT.PERP", "NEARUSDT.PERP",
             "HYPEUSDT.PERP", "XRPUSDT.PERP", "DOGEUSDT.PERP",
             "ADAUSDT.PERP", "LINKUSDT.PERP", "AVAXUSDT.PERP", "SUIUSDT.PERP",
             "1000PEPEUSDT.PERP",
             // BNB as the quiet counterpart of PEPE: long flat stretches, where the lines
             // lie close together and a marker is easiest to fire one candle early or late.
             "BNBUSDT.PERP"]),
        // WEEKLY candles, harvested 28 September 2026. Seven years of market on one coin: the
        // indicator holds four hundred bars whatever the interval, and on weeks that reaches back
        // to September 2019. Only eighteen markers come out of it - a week is a slow candle - but
        // they sit in market the other sets never touch.
        ("1w", 16, CryptoIntervalPeriod.interval1w, ["BTCUSDT.PERP"]),
    ];

    private static readonly DateTime Epoch = new(2010, 1, 4, 0, 0, 0, DateTimeKind.Utc);

    private MacSettings _before = new();

    [ClassInitialize]
    public static void Register(TestContext _) => TestBase.RegisterPlugin(new MacPlugin());

    [TestInitialize]
    public void Remember()
    {
        InitTestSession();
        _before = MacPlugin.Settings;
    }

    [TestCleanup]
    public void Restore() => new MacPlugin().SettingsBase = _before;


    [TestMethod]
    public void WriteWhatTheScannerFires()
    {
        if (!File.Exists(Database))
        {
            Assert.Inconclusive("no candle database at " + Database);
            return;
        }

        List<string> lines = ["symbol,interval,time,kind"];
        foreach (var (intervalName, intervalId, period, symbols) in Runs)
        {
            foreach (string name in symbols)
            {
                List<CryptoCandle> candles = Load(name, intervalId);
                Console.WriteLine($"{name} {intervalName}: {candles.Count} candles");
                if (candles.Count < 250)
                    continue;
                // One pass per marker kind, so each one is measured on its own instead of
                // through a mixture of triggers.
                foreach (string kind in new[] { "open", "cross", "break", "exit" })
                    lines.AddRange(Run(name, candles, kind, intervalName, period));
            }
        }

        File.WriteAllLines(Output, lines);
        Console.WriteLine((lines.Count - 1) + " signals written to " + Output);
        Assert.IsTrue(lines.Count > 1, "the scanner fired nothing at all, which is a fault here");
    }


    /// <summary>
    /// The SIGNAL on the Close marker has to land on exactly the candles the EXIT on that marker
    /// lands on, mirrored: a long signal on every Close Short, a short signal on every Close Long.
    /// The exit is what has been held against the chart, so this is what makes that measurement
    /// count for the signal too. Until 30 September 2026 the signal read the bare crossing and
    /// fired on candles where the indicator draws nothing.
    /// </summary>
    [TestMethod]
    public void TheCloseSignalIsTheMirrorOfTheCloseExit()
    {
        if (!File.Exists(Database))
        {
            Assert.Inconclusive("no candle database at " + Database);
            return;
        }

        int exits = 0;
        int sets = 0;
        List<string> differences = [];
        foreach (var (intervalName, intervalId, period, symbols) in Runs)
        {
            foreach (string name in symbols)
            {
                List<CryptoCandle> candles = Load(name, intervalId);
                if (candles.Count < 250)
                    continue;
                sets++;

                // The exit labels Close Long on the long side; the signal labels the SAME candle
                // Close Long on its short side, so the two lists read alike when they agree.
                HashSet<string> exit = [.. Run(name, candles, "exit", intervalName, period)];
                HashSet<string> signal = [.. Run(name, candles, "close", intervalName, period)];
                exits += exit.Count;
                differences.AddRange(exit.Except(signal).Select(line => "exit only: " + line));
                differences.AddRange(signal.Except(exit).Select(line => "signal only: " + line));
                Console.WriteLine($"{name} {intervalName}: {exit.Count} exits, {signal.Count} signals, "
                    + $"{exit.Except(signal).Count() + signal.Except(exit).Count()} differences");
            }
        }

        Console.WriteLine($"{sets} sets, {exits} Close markers");
        Assert.IsTrue(exits > 0, "no Close marker fired anywhere, which is a fault here");
        Assert.AreEqual(0, differences.Count, string.Join(Environment.NewLine, differences));
    }


    /// <summary>
    /// The same comparison with the Speed input of the indicator on Fast and on Slow. Both move the
    /// lengths of all four lines - Fast to 20/30/40/80, Slow to 20/50/100/200 - so every marker can
    /// land on another candle, and a harvest of one speed says nothing about the others.
    /// <para>
    /// BNB only, on daily candles: the point is whether the SETTING is reproduced, and for that one
    /// coin with markers on both speeds is enough. Harvested 28 September 2026.
    /// </para>
    /// </summary>
    [TestMethod]
    public void WriteWhatTheScannerFiresPerSpeed()
    {
        if (!File.Exists(Database))
        {
            Assert.Inconclusive("no candle database at " + Database);
            return;
        }

        // BNB on daily candles, ETH and SOL on fifteen minutes. The daily set gives markers over a
        // year, the fifteen minute one gives many markers over four days - the harvest stops at
        // four hundred bars whatever the interval, so the two buy different things.
        (string Interval, int Id, CryptoIntervalPeriod Period, string[] Symbols)[] runs =
        [
            ("1d", 15, CryptoIntervalPeriod.interval1d, ["BNBUSDT.PERP"]),
            ("15m", 6, CryptoIntervalPeriod.interval15m, ["ETHUSDT.PERP", "SOLUSDT.PERP"]),
        ];

        List<string> lines = ["speed,symbol,interval,time,kind"];
        foreach (MacSpeed speed in new[] { MacSpeed.Fast, MacSpeed.Slow })
        {
            foreach (var (intervalName, intervalId, period, symbols) in runs)
            {
                foreach (string name in symbols)
                {
                    List<CryptoCandle> candles = Load(name, intervalId);
                    if (candles.Count < 250)
                        continue;
                    foreach (string kind in new[] { "open", "cross", "break", "exit" })
                        lines.AddRange(Run(name, candles, kind, intervalName, period, speed)
                            .Select(line => speed + "," + line));
                }
            }
        }

        File.WriteAllLines(SpeedOutput, lines);
        Console.WriteLine((lines.Count - 1) + " signals written to " + SpeedOutput);
        Assert.IsTrue(lines.Count > 1, "the scanner fired nothing at all, which is a fault here");
    }


    /// <summary>The candles of one symbol, straight out of the scanner's own database.</summary>
    private static List<CryptoCandle> Load(string name, int intervalId)
    {
        List<CryptoCandle> candles = [];
        using SqliteConnection connection = new("Data Source=" + Database + ";Mode=ReadOnly");
        connection.Open();
        using SqliteCommand find = connection.CreateCommand();
        find.CommandText = "select SymbolId from Symbol where Name=@name";
        find.Parameters.AddWithValue("@name", name);
        object? found = find.ExecuteScalar();
        if (found == null)
            return candles;

        using SqliteCommand read = connection.CreateCommand();
        // The volume comes along now: the run reading of the break asks the candle to carry at
        // least the average of the last twenty, and a constant here would make that test pass
        // always - which is exactly what it quietly did before.
        read.CommandText = "select OpenTime,Ticks,Open,High,Low,Close,Volume from Candle "
                         + "where SymbolId=@id and IntervalId=@interval order by OpenTime";
        read.Parameters.AddWithValue("@id", found);
        read.Parameters.AddWithValue("@interval", intervalId);
        using SqliteDataReader rows = read.ExecuteReader();
        while (rows.Read())
        {
            long openTime = rows.GetInt64(0);
            int ticks = rows.GetInt32(1);
            int decimals = ticks & 15;
            decimal scale = (decimal)Math.Pow(10, decimals);
            candles.Add(new CryptoCandle
            {
                TickDecimals = (byte)decimals,
                OpenTime = new CandleTime((uint)openTime),
                Open = rows.GetInt64(2) / scale,
                High = rows.GetInt64(3) / scale,
                Low = rows.GetInt64(4) / scale,
                Close = rows.GetInt64(5) / scale,
                Volume = rows.GetInt64(6) / scale,
            });
        }
        return candles;
    }


    /// <summary>
    /// One pass over the candles with one group of settings switched on, so each kind of marker is
    /// measured on its own instead of through a mixture of triggers.
    /// </summary>
    private static List<string> Run(string name, List<CryptoCandle> candles, string kind,
        string intervalName, CryptoIntervalPeriod period, MacSpeed speed = MacSpeed.Standard)
    {
        MacSettings settings = new()
        {
            EntryOnBreakMarker = kind == "break",
            EntryOnOpenMarker = kind == "open",
            EntryOnCrossMarker = kind == "cross",
            EntryOnCloseMarker = kind == "close",
            RequirePriceOutsideCloud = false,
            ExitOnCloudFlip = false,
            ExitOnSecondLineCross = kind == "exit",
            Speed = speed,
        };
        new MacPlugin().SettingsBase = settings;

        CryptoSymbol symbol = MakeSymbol(name);
        CryptoInterval interval = GlobalData.IntervalListPeriod[period];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        symbolInterval.CandleList.Clear();
        symbolInterval.Data.Clear();

        // The moving averages of the cloud live in the registry, not in the extension, and the
        // scanner feeds BOTH from IntervalIndicatorHub.Add. Feeding only the extension leaves the
        // cloud empty and every signal answers "the cloud is not there yet".
        IndicatorRegistry registry = new(500);
        MacIndicatorExtension extension = new();
        extension.Init(registry);

        List<string> fired = [];
        for (int i = 0; i < candles.Count; i++)
        {
            CryptoCandle candle = candles[i];
            DateTime moment = Epoch.AddMinutes(candle.OpenTime.Minutes);
            Quote quote = new(moment, candle.Open, candle.High, candle.Low, candle.Close,
                candle.Volume);
            registry.QuoteHub.Add(quote);
            extension.OnCandleAdded(quote);
            CryptoData data = new();
            extension.FillData(data);
            symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
            symbolInterval.Data[candle.OpenTime] = data;

            // The slow line is a 150 average; nothing before that means anything.
            if (i < 200)
                continue;

            foreach (CryptoTradeSide side in new[] { CryptoTradeSide.Long, CryptoTradeSide.Short })
            {
                MyData newest = new() { Candle = candle, CandleData = data };
                MacBase algorithm = side == CryptoTradeSide.Long
                    ? new MacLong
                    {
                        Symbol = symbol,
                        Interval = interval,
                        SymbolInterval = symbolInterval,
                        SignalSide = side,
                        SignalStrategy = MacPlugin.StrategyInternal.ToLower(),
                        CandleLast = newest,
                    }
                    : new MacShort
                    {
                        Symbol = symbol,
                        Interval = interval,
                        SymbolInterval = symbolInterval,
                        SignalSide = side,
                        SignalStrategy = MacPlugin.StrategyInternal.ToLower(),
                        CandleLast = newest,
                    };
                bool hit = kind == "exit" ? algorithm.IsExitSignal() : algorithm.IsSignal();
                if (hit)
                    fired.Add(name + "," + intervalName + ","
                              + moment.ToString("yyyy-MM-dd HH:mm:ss") + "," + Label(kind, side));
            }
        }
        return fired;
    }


    private static string Label(string kind, CryptoTradeSide side)
    {
        bool longSide = side == CryptoTradeSide.Long;
        return kind switch
        {
            "open" => longSide ? "Open Long" : "Open Short",
            "cross" => longSide ? "Cross Up" : "Cross Down",
            "break" => longSide ? "Breakout" : "Breakdown",
            // The signal on the Close marker enters the OTHER way: a long on Close Short.
            "close" => longSide ? "Close Short" : "Close Long",
            _ => longSide ? "Close Long" : "Close Short",
        };
    }


    /// <summary>
    /// Every candle of every set with the numbers the break rule is allowed to look at, so a
    /// candidate rule can be tried in the counting script instead of by rebuilding this project.
    /// <para>
    /// Only what stands there at the time is written: the four lines, the two levels with their
    /// age, the rank inside the run and the candle itself. Whether the run turns out well is the
    /// future and is deliberately absent.
    /// </para>
    /// </summary>
    [TestMethod]
    public void WriteTheCandleFacts()
    {
        if (!File.Exists(Database))
        {
            Assert.Inconclusive("no candle database at " + Database);
            return;
        }

        List<string> lines =
        [
            "symbol,interval,time,open,high,low,close,volume,fast,second,medium,slow," +
            "levelHigh,levelHighAge,levelLow,levelLowAge,upRank,downRank,upOk,downOk"
        ];
        foreach (var (intervalName, intervalId, period, symbols) in Runs)
        {
            foreach (string name in symbols)
            {
                List<CryptoCandle> candles = Load(name, intervalId);
                if (candles.Count < 250)
                    continue;
                lines.AddRange(Facts(name, candles, intervalName));
            }
        }

        File.WriteAllLines(Facts_Output, lines);
        Console.WriteLine((lines.Count - 1) + " candles written to " + Facts_Output);
        Assert.IsTrue(lines.Count > 1);
    }


    private const string Facts_Output =
        @"E:\Projects\CryptoScanBot.tools\studies\macloud\mac-candle-facts.csv";


    /// <summary>One pass over the candles that only reads the indicator, and fires nothing.</summary>
    private static List<string> Facts(string name, List<CryptoCandle> candles, string intervalName)
    {
        MacSettings settings = new();
        new MacPlugin().SettingsBase = settings;

        IndicatorRegistry registry = new(500);
        MacIndicatorExtension extension = new();
        extension.Init(registry);

        List<string> rows = [];
        for (int i = 0; i < candles.Count; i++)
        {
            CryptoCandle candle = candles[i];
            DateTime moment = Epoch.AddMinutes(candle.OpenTime.Minutes);
            registry.QuoteHub.Add(new Quote(moment, candle.Open, candle.High, candle.Low,
                candle.Close, candle.Volume));
            extension.OnCandleAdded(new Quote(moment, candle.Open, candle.High, candle.Low,
                candle.Close, candle.Volume));
            CryptoData data = new();
            extension.FillData(data);
            if (i < 200)
                continue;

            MacCandleData mac = data.GetPluginData<MacCandleData>()!;
            rows.Add(string.Join(",",
                name, intervalName, moment.ToString("yyyy-MM-dd HH:mm:ss"),
                Number(candle.Open), Number(candle.High), Number(candle.Low), Number(candle.Close),
                Number(candle.Volume),
                Number(mac.EmaFast), Number(mac.EmaSecond), Number(mac.SmaMedium), Number(mac.SmaSlow),
                Number(mac.RsiLevelHigh), mac.RsiLevelHighAge,
                Number(mac.RsiLevelLow), mac.RsiLevelLowAge,
                mac.BreakoutRank, mac.BreakdownRank,
                mac.BreakoutRunAllowed ? 1 : 0, mac.BreakdownRunAllowed ? 1 : 0));
        }
        return rows;
    }


    private static string Number(double? value) =>
        value == null ? "" : value.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    private static string Number(decimal value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);


    private static CryptoSymbol MakeSymbol(string name)
    {
        Exchange exchange = new() { Id = 1, Name = "TestExchange", FeeRate = 0.1m };
        return new CryptoSymbol
        {
            Id = 1,
            Name = name,
            Base = name.Replace("USDT.PERP", ""),
            Quote = "USDT",
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = exchange.Name,
            QuoteData = GlobalData.AddQuoteData("USDT"),
            PriceTickSize = 0.01m,
        };
    }
}
