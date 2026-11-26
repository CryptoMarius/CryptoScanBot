using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Analyzers.XsMom;

/// <summary>
/// One daily bar of the ranking: the close and the volume in the quote (base volume x close).
/// </summary>
public readonly record struct XsMomDailyBar(decimal Close, decimal QuoteVolume);


/// <summary>One coin of a selection: its side, its place in the ranking (0 = weakest) and its return in percent.</summary>
public readonly record struct XsMomPick(CryptoTradeSide Side, int Rank, int Count, decimal ReturnPercentage);


/// <summary>The outcome of one rebalance: the coins to hold, per symbol name.</summary>
public sealed class XsMomSelection
{
    public required CandleTime RebalanceTime { get; init; }
    public required Dictionary<string, XsMomPick> Picks { get; init; }
    // How many coins took part in the ranking (at most TopByVolume)
    public required int RankedCount { get; init; }

    public bool TryGet(string symbolName, out XsMomPick pick) => Picks.TryGetValue(symbolName, out pick);
}


/// <summary>
/// The ranking of the cross-sectional momentum strategy. Pure where it matters: <see cref="Select"/>
/// takes the daily history of all coins and a rebalance moment and only reads the bars that closed
/// BEFORE that moment, so it cannot look ahead whatever the history holds.
/// <para>
/// Where the history comes from:
/// <list type="bullet">
/// <item>In the emulator: the 1d candles of EVERY symbol of the exchange from the candle database,
/// read once per run. So the ranking does not depend on which coins the run replays, nor on the order
/// in which the symbol threads close their candles - every symbol asks the same, already closed
/// question and gets the same answer.</item>
/// <item>Live: the 1d candles in memory. A selection is only kept once the rebalance moment is two
/// minutes behind, because the last daily candles of the other coins may still be arriving.</item>
/// </list>
/// </para>
/// </summary>
public static class XsMomRanking
{
    private const uint MinutesPerDay = 1440;

    private static readonly object Lock = new();
    private static readonly Dictionary<string, XsMomSelection> Cache = [];

    // The emulator history: loaded once per run, exchange and quote
    private static string? historyKey;
    private static uint historyFrom;
    private static Dictionary<string, SortedList<uint, XsMomDailyBar>> history = [];


    /// <summary>True when <paramref name="moment"/> (a candle close time) is a rebalance moment of the settings.</summary>
    public static bool IsRebalanceMoment(CandleTime moment, XsMomSettings settings)
    {
        if (moment.Minutes % MinutesPerDay != 0)
            return false;
        DateTime date = moment.ToDateTime();
        if (date.DayOfWeek != settings.RebalanceDay)
            return false;
        int every = Math.Max(1, settings.RebalanceEveryWeeks);
        if (every == 1)
            return true;
        // Counted in weeks since the epoch (a Monday), so the same weeks are chosen on every run
        long week = moment.Minutes / (7L * MinutesPerDay);
        return week % every == 0;
    }


    /// <summary>
    /// The selection of the rebalance moment for the quote of <paramref name="symbol"/>, or null when
    /// it cannot be made (no history). Cached per moment and settings.
    /// </summary>
    public static XsMomSelection? GetSelection(CryptoSymbol symbol, CandleTime rebalanceTime, XsMomSettings settings)
    {
        string quote = symbol.Quote;
        string key = SelectionKey(symbol.Exchange, quote, rebalanceTime, settings);
        lock (Lock)
        {
            if (Cache.TryGetValue(key, out XsMomSelection? cached))
                return cached;

            Dictionary<string, SortedList<uint, XsMomDailyBar>>? data = GlobalData.IsEmulatorMode
                ? EmulatorHistory(symbol.Exchange, quote, rebalanceTime, settings)
                : LiveHistory(symbol.Exchange, quote, settings);
            if (data == null || data.Count == 0)
                return null;

            // The selection before this one, for the coins that may stay (KeepWithinFactor)
            uint period = (uint)Math.Max(1, settings.RebalanceEveryWeeks) * 7 * MinutesPerDay;
            Cache.TryGetValue(SelectionKey(symbol.Exchange, quote, rebalanceTime - period, settings), out XsMomSelection? previous);

            XsMomSelection selection = Select(data, rebalanceTime, settings, previous);

            bool keep = GlobalData.IsEmulatorMode || GlobalData.Clock.UtcNow >= rebalanceTime.ToDateTime().AddMinutes(2);
            if (keep)
            {
                Cache[key] = selection;
                GlobalData.AddTextToLogTab($"xsmom {quote} {rebalanceTime.ToDateTime():yyyy-MM-dd HH:mm}: ranked {selection.RankedCount} coin(s), "
                    + $"long {string.Join(",", selection.Picks.Where(p => p.Value.Side == CryptoTradeSide.Long).Select(p => p.Key).OrderBy(x => x))}; "
                    + $"short {string.Join(",", selection.Picks.Where(p => p.Value.Side == CryptoTradeSide.Short).Select(p => p.Key).OrderBy(x => x))}");
            }
            return selection;
        }
    }


    /// <summary>
    /// The selection itself. Only bars with an open time of at most rebalance - 1 day are read (a daily
    /// bar is closed once its open time plus a day has passed).
    /// <list type="number">
    /// <item>Candidates: every coin with a close on the day before the rebalance and on the day
    /// LookbackDays before that.</item>
    /// <item>The top TopByVolume of them by average quote volume over the VolumeDays days before the
    /// rebalance (a missing day counts as zero).</item>
    /// <item>Ranked on the return, weakest first: short the first CoinsPerSide, long the last.</item>
    /// <item>With KeepWithinFactor above 1 a coin of <paramref name="previous"/> stays on its side while
    /// it is within KeepWithinFactor x CoinsPerSide of its end of the ranking; the strongest (weakest)
    /// new coins fill up to CoinsPerSide.</item>
    /// </list>
    /// </summary>
    public static XsMomSelection Select(IReadOnlyDictionary<string, SortedList<uint, XsMomDailyBar>> data,
        CandleTime rebalanceTime, XsMomSettings settings, XsMomSelection? previous)
    {
        uint lastDay = rebalanceTime.Minutes - MinutesPerDay;
        uint lookbackDay = lastDay - (uint)Math.Max(1, settings.LookbackDays) * MinutesPerDay;
        int volumeDays = Math.Max(1, settings.VolumeDays);

        List<(string Name, decimal Return, decimal Volume)> candidates = [];
        foreach (var (name, bars) in data)
        {
            if (!bars.TryGetValue(lastDay, out XsMomDailyBar last) || !bars.TryGetValue(lookbackDay, out XsMomDailyBar back))
                continue;
            if (last.Close <= 0 || back.Close <= 0)
                continue;

            decimal volume = 0;
            for (int i = 0; i < volumeDays; i++)
            {
                if (bars.TryGetValue(lastDay - (uint)i * MinutesPerDay, out XsMomDailyBar bar))
                    volume += bar.QuoteVolume;
            }
            candidates.Add((name, last.Close / back.Close - 1m, volume / volumeDays));
        }

        // Ties on the name, so the outcome never depends on the order of a dictionary
        List<(string Name, decimal Return, decimal Volume)> ranked = [.. candidates
            .OrderByDescending(c => c.Volume).ThenBy(c => c.Name, StringComparer.Ordinal)
            .Take(Math.Max(0, settings.TopByVolume))
            .OrderBy(c => c.Return).ThenBy(c => c.Name, StringComparer.Ordinal)];

        int n = ranked.Count;
        int basket = Math.Max(0, Math.Min(settings.CoinsPerSide, n / 2));
        int keepZone = (int)Math.Floor(basket * Math.Max(1m, settings.KeepWithinFactor));
        Dictionary<string, int> position = [];
        for (int i = 0; i < n; i++)
            position[ranked[i].Name] = i;

        Dictionary<string, XsMomPick> picks = [];
        XsMomPick PickAt(string name, CryptoTradeSide side)
        {
            int i = position[name];
            return new XsMomPick(side, i, n, Math.Round(100m * ranked[i].Return, 2));
        }

        // Long: the coins kept from the previous selection (strongest first), then the strongest new ones
        List<string> longs = [];
        if (previous != null && keepZone > basket)
        {
            longs.AddRange(previous.Picks
                .Where(p => p.Value.Side == CryptoTradeSide.Long && position.TryGetValue(p.Key, out int i) && i >= n - keepZone)
                .Select(p => p.Key).OrderByDescending(x => position[x]));
        }
        int longTarget = Math.Max(basket, longs.Count);
        for (int i = n - 1; i >= n - basket && longs.Count < longTarget; i--)
        {
            if (!longs.Contains(ranked[i].Name))
                longs.Add(ranked[i].Name);
        }
        foreach (string name in longs)
            picks[name] = PickAt(name, CryptoTradeSide.Long);

        // Short: the same from the weak end
        List<string> shorts = [];
        if (previous != null && keepZone > basket)
        {
            shorts.AddRange(previous.Picks
                .Where(p => p.Value.Side == CryptoTradeSide.Short && position.TryGetValue(p.Key, out int i) && i < keepZone)
                .Select(p => p.Key).OrderBy(x => position[x]));
        }
        int shortTarget = Math.Max(basket, shorts.Count);
        for (int i = 0; i < basket && shorts.Count < shortTarget; i++)
        {
            if (!shorts.Contains(ranked[i].Name) && !picks.ContainsKey(ranked[i].Name))
                shorts.Add(ranked[i].Name);
        }
        foreach (string name in shorts)
        {
            if (!picks.ContainsKey(name))
                picks[name] = PickAt(name, CryptoTradeSide.Short);
        }

        return new XsMomSelection { RebalanceTime = rebalanceTime, Picks = picks, RankedCount = n };
    }


    /// <summary>Forget every selection and the loaded history (tests, and a new emulator run).</summary>
    public static void Reset()
    {
        lock (Lock)
        {
            Cache.Clear();
            history = [];
            historyKey = null;
        }
    }


    private static string SelectionKey(CryptoScanner.Core.Model.CryptoExchange exchange, string quote, CandleTime time, XsMomSettings settings)
        => $"{GlobalData.CurrentEmulatorRunId}|{exchange.Name}|{quote}|{time.Minutes}|{settings.LookbackDays}|{settings.TopByVolume}|"
            + $"{settings.VolumeDays}|{settings.SkipTradFi}|{settings.CoinsPerSide}|{settings.KeepWithinFactor}|{settings.RebalanceEveryWeeks}";


    /// <summary>The symbols that may take part: same quote, no barometer, no TradFi when asked.</summary>
    private static IEnumerable<CryptoSymbol> Eligible(CryptoScanner.Core.Model.CryptoExchange exchange, string quote, XsMomSettings settings)
    {
        foreach (CryptoSymbol s in exchange.SymbolListName.Values)
        {
            if (s.Quote != quote || s.IsBarometerSymbol())
                continue;
            if (settings.SkipTradFi && s.Product == CryptoProduct.TradFi)
                continue;
            yield return s;
        }
    }


    /// <summary>
    /// The daily history of the whole exchange from the candle database, loaded once per run (and again
    /// when a rebalance needs older bars than were loaded).
    /// </summary>
    private static Dictionary<string, SortedList<uint, XsMomDailyBar>>? EmulatorHistory(CryptoScanner.Core.Model.CryptoExchange exchange,
        string quote, CandleTime rebalanceTime, XsMomSettings settings)
    {
        uint needFrom = rebalanceTime.Minutes - (uint)(Math.Max(settings.LookbackDays, settings.VolumeDays) + 2) * MinutesPerDay;
        string key = $"{GlobalData.CurrentEmulatorRunId}|{exchange.Name}|{quote}|{settings.SkipTradFi}";
        if (historyKey == key && historyFrom <= needFrom)
            return history;

        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1d, out CryptoInterval? interval1d))
            return null;

        // A margin of 60 days so the next rebalances of the run do not reload
        uint from = needFrom > 60 * MinutesPerDay ? needFrom - 60 * MinutesPerDay : 0;
        Dictionary<string, CryptoSymbol> byInstrument = new(StringComparer.OrdinalIgnoreCase);
        foreach (CryptoSymbol s in Eligible(exchange, quote, settings))
            byInstrument.TryAdd(CandleDatabase.InstrumentKey(s), s);

        Dictionary<string, SortedList<uint, XsMomDailyBar>> loaded = [];
        using (var db = new CandleDatabase(exchange))
        {
            db.Open();
            foreach (var (instrument, rows) in CandleDatabase.LoadCloseAndVolumeForAll(db.Connection, interval1d, from))
            {
                if (!byInstrument.TryGetValue(instrument, out CryptoSymbol? symbol))
                    continue;
                SortedList<uint, XsMomDailyBar> bars = new(rows.Count);
                foreach (var (openTime, close, volume) in rows)
                    bars[openTime] = new XsMomDailyBar(close, volume * close);
                loaded[symbol.Name] = bars;
            }
        }

        history = loaded;
        historyKey = key;
        historyFrom = from;
        GlobalData.AddTextToLogTab($"xsmom {quote}: daily history of {loaded.Count} coin(s) loaded from {new CandleTime(from).ToDateTime():yyyy-MM-dd}");
        return history;
    }


    /// <summary>The daily history from the candles in memory (live).</summary>
    private static Dictionary<string, SortedList<uint, XsMomDailyBar>> LiveHistory(CryptoScanner.Core.Model.CryptoExchange exchange,
        string quote, XsMomSettings settings)
    {
        Dictionary<string, SortedList<uint, XsMomDailyBar>> result = [];
        foreach (CryptoSymbol symbol in Eligible(exchange, quote, settings))
        {
            try
            {
                var candles = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1d).CandleList;
                SortedList<uint, XsMomDailyBar> bars = [];
                foreach (CryptoCandle candle in candles.Values.ToList())
                    bars[candle.OpenTime.Minutes] = new XsMomDailyBar(candle.Close, candle.Volume * candle.Close);
                if (bars.Count > 0)
                    result[symbol.Name] = bars;
            }
            catch (InvalidOperationException)
            {
                // The candle list changed while it was copied; this coin sits this ranking out
            }
        }
        return result;
    }
}
