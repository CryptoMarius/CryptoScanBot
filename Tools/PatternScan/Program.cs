// Offline pattern scan: walks the scanner's own candle database and writes one line per candlestick
// reversal shape found, so which of them predict anything can be judged in Python next to the rest
// of Tools/EntryTiming.
//
// The shapes come from CandlePatternHelper in CryptoScanner.Core - the SAME code the candlepattern
// strategy uses. That is deliberate: a second implementation here would drift, and then a difference
// between this measurement and a run would be impossible to attribute.
//
// It used to call the OHLC_Candlestick_Patterns package. See README.md for why that stopped.
using System.Globalization;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

using Microsoft.Data.Sqlite;

string database = Argument("--db") ?? throw new ArgumentException("--db is required");
string intervalName = Argument("--interval") ?? "15m";
string output = Argument("--out") ?? "patterns.csv";
int minimumCandles = int.Parse(Argument("--min-candles") ?? "1000", CultureInfo.InvariantCulture);
string? only = Argument("--symbols");

// IntervalId equals CryptoIntervalPeriod + 1 (see Tools/EntryTiming/candledb.py).
Dictionary<string, int> intervalIds = new()
{
    ["1m"] = 1, ["2m"] = 2, ["3m"] = 3, ["5m"] = 4, ["10m"] = 5, ["15m"] = 6, ["30m"] = 7,
    ["1h"] = 8, ["2h"] = 9, ["3h"] = 10, ["4h"] = 11, ["6h"] = 12, ["8h"] = 13, ["12h"] = 14,
    ["1d"] = 15, ["1w"] = 16,
};
if (!intervalIds.TryGetValue(intervalName, out int intervalId))
    throw new ArgumentException($"unknown interval {intervalName}");

CryptoCandlePattern[] patterns = Enum.GetValues<CryptoCandlePattern>();
CryptoTradeSide[] sides = [CryptoTradeSide.Long, CryptoTradeSide.Short];
CandlePatternSettings settings = new();
Console.WriteLine($"{patterns.Length} patterns x {sides.Length} sides, interval {intervalName}");

HashSet<string>? wanted = only is null ? null : [.. only.Split(',').Select(s => s.Trim())];

using SqliteConnection connection = new($"Data Source={database};Mode=ReadOnly");
connection.Open();

List<(int Id, string Name)> symbols = [];
using (SqliteCommand command = connection.CreateCommand())
{
    command.CommandText = """
        SELECT s.SymbolId, s.Name, COUNT(*) AS n
        FROM Symbol s JOIN Candle c ON c.SymbolId = s.SymbolId AND c.IntervalId = $interval
        GROUP BY s.SymbolId, s.Name HAVING n >= $minimum ORDER BY s.Name
        """;
    command.Parameters.AddWithValue("$interval", intervalId);
    command.Parameters.AddWithValue("$minimum", minimumCandles);
    using SqliteDataReader reader = command.ExecuteReader();
    while (reader.Read())
    {
        string name = reader.GetString(1);
        if (wanted is null || wanted.Contains(name))
            symbols.Add((reader.GetInt32(0), name));
    }
}
Console.WriteLine($"{symbols.Count} symbol(s)");

using StreamWriter writer = new(output);
writer.WriteLine("symbol,interval,pattern,side,opentime,close");

int written = 0;
foreach ((int symbolId, string symbolName) in symbols)
{
    List<CryptoCandle> bars = [];
    List<long> openTimes = [];
    using (SqliteCommand command = connection.CreateCommand())
    {
        command.CommandText = """
            SELECT OpenTime, Ticks, Open, High, Low, Close FROM Candle
            WHERE SymbolId = $symbol AND IntervalId = $interval ORDER BY OpenTime
            """;
        command.Parameters.AddWithValue("$symbol", symbolId);
        command.Parameters.AddWithValue("$interval", intervalId);
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            // Ticks is the number of decimals and is a PER-ROW value; decoding with the symbol's
            // current tick size instead corrupts part of the series, which contaminated 13.2% of the
            // positions in an earlier analysis.
            byte decimals = (byte)reader.GetInt32(1);
            decimal scale = (decimal)Math.Pow(10, -decimals);
            bars.Add(new CryptoCandle
            {
                TickDecimals = decimals,
                Open = reader.GetInt64(2) * scale,
                High = reader.GetInt64(3) * scale,
                Low = reader.GetInt64(4) * scale,
                Close = reader.GetInt64(5) * scale,
            });
            openTimes.Add(reader.GetInt64(0));
        }
    }

    for (int index = 2; index < bars.Count; index++)
    {
        foreach (CryptoCandlePattern pattern in patterns)
        {
            foreach (CryptoTradeSide side in sides)
            {
                if (!CandlePatternHelper.Matches(pattern, side, bars[index], bars[index - 1], bars[index - 2], settings))
                    continue;
                writer.WriteLine($"{symbolName},{intervalName},{pattern},{side},{openTimes[index]}," +
                    bars[index].Close.ToString(CultureInfo.InvariantCulture));
                written++;
            }
        }
    }
    Console.WriteLine($"  {symbolName}: {bars.Count} candles");
}

Console.WriteLine($"{written} signal(s) written to {output}");

string? Argument(string name)
{
    string[] arguments = Environment.GetCommandLineArgs();
    int index = Array.IndexOf(arguments, name);
    return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
}
