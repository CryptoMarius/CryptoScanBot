// Does a candleSnapshot sent over the WEBSOCKET count against the same 1200 weight per minute that
// HyperLiquid charges an IP address for REST requests?
//
// It decides whether a scanner start of 117 symbols has to take three minutes or twenty seconds. The
// documentation (https://hyperliquid.gitbook.io/hyperliquid-docs/for-developers/api/rate-limits-and-user-limits)
// lists the websocket limits separately - 2000 messages per minute, 100 simultaneous inflight post
// messages - but never says post messages are free of the REST weight. HyperLiquid.Net models them
// as two gates, HyperLiquidRest at 1200 per minute and HyperLiquidSocket at 2000 per minute, which
// is an opinion and not proof. See CryptoScanner.Exchanges/HyperLiquid/HyperLiquid.md.
//
// HOW IT MEASURES
//
// A cheap REST request - allMids, 2 weight - is fired every --probe-interval from beginning to end.
// That probe is the instrument: while the address has budget it is answered, and the moment the
// address runs out it is refused. Around it:
//
//   WARMUP    only probes. Refusals here mean something ELSE is spending this address's budget - a
//             scanner on the same machine - and the run has to be thrown away. This is the control
//             that the first version of this tool did not have, and without it a stall during the
//             burst cannot be attributed to the burst.
//   BURST     --count candle requests over --transport, as fast as --concurrency allows. At about
//             21 weight each, 100 of them is 2100 against an allowance of 1200: if the exchange
//             counts them, the probe MUST start being refused here.
//   COOLDOWN  only probes again, so it is visible that the address recovers.
//
// Run it three ways. --transport rest is the positive control: refusals during the burst are
// expected, and if they do not appear the instrument is broken. --transport socket is the question.
// --transport none is a quietness check: it should show no refusals at all, and if it does, no other
// run of this tool means anything until whatever is spending the budget stops.
//
// The client side rate limiter is switched OFF on purpose. Left on, the package would hold the
// requests back itself and the exchange would never get the chance to answer.
//
// WARNING: this spends the budget of the whole IP ADDRESS. A scanner running on HyperLiquid at the
// same moment shares it and can be refused for up to a minute afterwards.
using System.Diagnostics;
using System.Globalization;

using HyperLiquid.Net;
using HyperLiquid.Net.Clients;
using HyperLiquid.Net.Enums;

int count = int.Parse(Argument("--count") ?? "100", CultureInfo.InvariantCulture);
int concurrency = int.Parse(Argument("--concurrency") ?? "8", CultureInfo.InvariantCulture);
int minutes = int.Parse(Argument("--minutes") ?? "10", CultureInfo.InvariantCulture);
int warmupSeconds = int.Parse(Argument("--warmup") ?? "20", CultureInfo.InvariantCulture);
int cooldownSeconds = int.Parse(Argument("--cooldown") ?? "25", CultureInfo.InvariantCulture);
int probeInterval = int.Parse(Argument("--probe-interval") ?? "1000", CultureInfo.InvariantCulture);
string transport = (Argument("--transport") ?? "socket").ToLowerInvariant();
int sustainSeconds = int.Parse(Argument("--sustain") ?? "0", CultureInfo.InvariantCulture);
string output = Argument("--out") ?? "hyperliquid-ratetest.csv";

if (transport is not ("socket" or "rest" or "none"))
    throw new ArgumentException("--transport must be socket, rest or none");

// Every request and every gate the package charged, written out at the end so the raw evidence
// survives the console.
List<string> rows = ["kind,phase,ordinal,symbol,atMs,durationMs,success,candles,error"];
List<string> gateEvents = [];

HyperLiquidExchange.RateLimiter.RateLimitTriggered += x =>
{
    // Which gate the PACKAGE charged. Not the exchange's opinion, but it does show whether the
    // socket call really travels a different accounting than the REST one.
    lock (gateEvents)
        gateEvents.Add($"{DateTime.Now:HH:mm:ss.fff} {x.LimitDescription} limit={x.Limit} current={x.Current} behaviour={x.Behaviour}");
};

Console.WriteLine($"HyperLiquid rate limit test - transport={transport} count={count} concurrency={concurrency}");
Console.WriteLine($"Probe: allMids (2 weight) every {probeInterval} ms, {warmupSeconds}s warmup / burst / {cooldownSeconds}s cooldown.");
if (transport != "none")
    Console.WriteLine($"Burst: {count} candle requests of about 21 weight = {count * 21} weight, against 1200 per minute.");
Console.WriteLine();

// The client side limiter is what normally keeps us under the ceiling. It has to be out of the way,
// otherwise the package queues the burst and the exchange never sees more than 1200 in a minute.
using var restClient = new HyperLiquidRestClient(options =>
{
    options.RateLimiterEnabled = false;
});
using var socketClient = new HyperLiquidSocketClient(options =>
{
    options.RateLimiterEnabled = false;
});

// The symbol names come from the exchange itself, exactly like Symbol.GetSymbolsAsync does, so a
// name that the candle request rejects cannot be blamed on this tool.
Console.WriteLine("Reading the symbol list (one info request, 20 weight)...");
var tickerInfo = await restClient.FuturesApi.ExchangeData.GetExchangeInfoAndTickersAsync();
if (!tickerInfo.Success || tickerInfo.Data == null)
{
    Console.WriteLine($"FAILED to read the symbol list: {tickerInfo.Error}");
    return 1;
}

List<string> symbols = [.. tickerInfo.Data.Tickers
    .Where(t => !string.IsNullOrEmpty(t.Symbol))
    .OrderByDescending(t => t.NotionalVolume)
    .Select(t => t.Symbol!)
    .Take(Math.Max(count, 1))];

if (symbols.Count == 0)
{
    Console.WriteLine("FAILED: the exchange returned no tickers");
    return 1;
}
Console.WriteLine($"{symbols.Count} symbols, most traded first ({string.Join(", ", symbols.Take(5))}, ...)");
Console.WriteLine();

var clock = Stopwatch.StartNew();
string phase = "warmup";
List<(long atMs, string phase, bool success)> probes = [];

// The instrument. allMids weighs 2, so firing it once a second for a minute and a half costs about
// 180 weight - enough to read the address's state without being the reason it runs out.
using CancellationTokenSource probeStop = new();
Task probeLoop = Task.Run(async () =>
{
    while (!probeStop.IsCancellationRequested)
    {
        long at = clock.ElapsedMilliseconds;
        string current = phase;
        var watch = Stopwatch.StartNew();
        try
        {
            var result = await restClient.FuturesApi.ExchangeData.GetPricesAsync(ct: probeStop.Token);
            watch.Stop();
            lock (probes)
            {
                probes.Add((at, current, result.Success));
                rows.Add($"probe,{current},,allMids,{at},{watch.ElapsedMilliseconds},{result.Success},,\"{result.Error}\"");
            }
            if (!result.Success)
                Console.WriteLine($"  [{at / 1000.0,6:F1}s] probe REFUSED during {current}: {result.Error}");
        }
        catch (OperationCanceledException)
        {
            break;
        }

        try
        {
            await Task.Delay(probeInterval, probeStop.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }
});

Console.WriteLine($"WARMUP {warmupSeconds}s - is anything else spending this address's budget?");
await Task.Delay(TimeSpan.FromSeconds(warmupSeconds));

int burstRefused = 0;
int firstRefusal = -1;
long burstMs = 0;
if (transport != "none")
{
    phase = "burst";
    Console.WriteLine($"BURST - {count} candle requests over {transport}");
    bool burstOverSocket = transport == "socket";
    var burstWatch = Stopwatch.StartNew();
    using SemaphoreSlim slots = new(concurrency, concurrency);
    bool[] burstSuccess = new bool[count];
    long[] burstStarted = new long[count];

    await Task.WhenAll(Enumerable.Range(0, count).Select(async ordinal =>
    {
        await slots.WaitAsync();
        try
        {
            string symbol = symbols[ordinal % symbols.Count];
            long at = clock.ElapsedMilliseconds;
            var watch = Stopwatch.StartNew();
            bool success;
            int candles;
            string? error;
            if (burstOverSocket)
            {
                var result = await socketClient.FuturesApi.ExchangeData.GetKlinesAsync(
                    symbol, KlineInterval.OneMinute, DateTime.UtcNow.AddMinutes(-minutes), DateTime.UtcNow);
                (success, candles, error) = (result.Success, result.Data?.Count() ?? 0, result.Error?.ToString());
            }
            else
            {
                var result = await restClient.FuturesApi.ExchangeData.GetKlinesAsync(
                    symbol, KlineInterval.OneMinute, DateTime.UtcNow.AddMinutes(-minutes), DateTime.UtcNow);
                (success, candles, error) = (result.Success, result.Data?.Count() ?? 0, result.Error?.ToString());
            }
            watch.Stop();

            burstSuccess[ordinal] = success;
            burstStarted[ordinal] = at;
            lock (rows)
                rows.Add($"burst,burst,{ordinal},{symbol},{at},{watch.ElapsedMilliseconds},{success},{candles},\"{error}\"");

            if (!success)
                Console.WriteLine($"  [{at / 1000.0,6:F1}s] burst #{ordinal + 1} {symbol,-12} FAILED: {error}");
        }
        finally
        {
            slots.Release();
        }
    }));
    burstWatch.Stop();
    burstMs = burstWatch.ElapsedMilliseconds;

    burstRefused = burstSuccess.Count(s => !s);
    firstRefusal = Array.FindIndex(burstSuccess, s => !s);
    Console.WriteLine($"  {count - burstRefused} of {count} accepted in {burstMs} ms" +
        (firstRefusal >= 0 ? $", first failure at number {firstRefusal + 1}" : ", no failures"));
}

// How many candle requests the exchange lets through PER MINUTE once the burst allowance is gone.
// The burst above answers "how much may I spend at once", this answers "and then what", and only the
// second number decides how long a start of 117 symbols takes. Keeps asking without pause and counts
// what was accepted per ten seconds: the first buckets show whatever was left of the allowance, the
// later ones settle on the rate at which the exchange hands it back.
if (sustainSeconds > 0)
{
    phase = "sustain";
    Console.WriteLine($"SUSTAIN {sustainSeconds}s - accepted candle requests per 10 seconds");
    var sustainWatch = Stopwatch.StartNew();
    int[] accepted = new int[sustainSeconds / 10 + 1];
    int[] attempted = new int[accepted.Length];
    using SemaphoreSlim sustainSlots = new(concurrency, concurrency);
    List<Task> workers = [];

    for (int worker = 0; worker < concurrency; worker++)
    {
        workers.Add(Task.Run(async () =>
        {
            int index = 0;
            while (sustainWatch.Elapsed.TotalSeconds < sustainSeconds)
            {
                string symbol = symbols[Interlocked.Increment(ref index) % symbols.Count];
                int bucket = Math.Min((int)(sustainWatch.Elapsed.TotalSeconds / 10), accepted.Length - 1);
                long at = clock.ElapsedMilliseconds;
                var watch = Stopwatch.StartNew();
                var result = await restClient.FuturesApi.ExchangeData.GetKlinesAsync(
                    symbol, KlineInterval.OneMinute, DateTime.UtcNow.AddMinutes(-minutes), DateTime.UtcNow);
                watch.Stop();

                Interlocked.Increment(ref attempted[bucket]);
                if (result.Success)
                    Interlocked.Increment(ref accepted[bucket]);
                lock (rows)
                    rows.Add($"sustain,sustain,{bucket},{symbol},{at},{watch.ElapsedMilliseconds},{result.Success}," +
                        $"{result.Data?.Count() ?? 0},\"{result.Error}\"");
            }
        }));
    }
    await Task.WhenAll(workers);

    for (int bucket = 0; bucket < accepted.Length; bucket++)
    {
        if (attempted[bucket] == 0)
            continue;
        Console.WriteLine($"  {bucket * 10,3}-{bucket * 10 + 10,-3}s  {accepted[bucket],4} accepted of {attempted[bucket],4} asked" +
            $"   = {accepted[bucket] * 6,4} per minute");
    }
    Console.WriteLine();
}

phase = "cooldown";
Console.WriteLine($"COOLDOWN {cooldownSeconds}s");
await Task.Delay(TimeSpan.FromSeconds(cooldownSeconds));

probeStop.Cancel();
await probeLoop;
Console.WriteLine();

int Refused(string p) => probes.Count(x => x.phase == p && !x.success);
int Total(string p) => probes.Count(x => x.phase == p);

Console.WriteLine("PROBE TIMELINE (allMids, one dot per probe, X = refused)");
foreach (string p in (string[])["warmup", "burst", "cooldown"])
{
    var section = probes.Where(x => x.phase == p).OrderBy(x => x.atMs).ToList();
    if (section.Count == 0)
        continue;
    string line = string.Concat(section.Select(x => x.success ? "." : "X"));
    Console.WriteLine($"  {p,-9} {line}  ({Total(p) - Refused(p)}/{Total(p)} answered)");
}
Console.WriteLine();

if (gateEvents.Count > 0)
{
    Console.WriteLine($"Gates the PACKAGE charged ({gateEvents.Count} events, first few):");
    foreach (string line in gateEvents.Take(5))
        Console.WriteLine($"  {line}");
    Console.WriteLine();
}

Console.WriteLine("VERDICT");
if (Refused("warmup") > 0)
{
    Console.WriteLine($"  NOT CLEAN: {Refused("warmup")} of {Total("warmup")} probes were refused BEFORE the burst started.");
    Console.WriteLine("  Something else is spending this address's budget - a scanner on HyperLiquid, most likely.");
    Console.WriteLine("  Nothing in this run can be attributed to the burst. Wait until it is quiet and run again.");
}
else if (transport == "none")
{
    Console.WriteLine($"  Quietness check: {Total("warmup") + Total("cooldown")} probes, none refused.");
    Console.WriteLine("  The address has budget to spare, so a burst run now would be readable.");
}
else if (Refused("burst") > 0)
{
    Console.WriteLine($"  The probe was refused {Refused("burst")} times DURING the {transport} burst and never before it.");
    Console.WriteLine($"  READS AS: {transport} candle requests are charged against the same 1200 weight per minute.");
    if (transport == "socket")
        Console.WriteLine("  A websocket post is not a free lane - moving the catch-up to it buys nothing.");
}
else
{
    Console.WriteLine($"  {count} requests is about {count * 21} weight against an allowance of 1200, and the probe");
    Console.WriteLine($"  was answered throughout ({Total("burst")} probes during the burst, none refused).");
    if (transport == "socket")
    {
        Console.WriteLine("  READS AS: websocket post requests are NOT on the 1200 REST budget.");
        Console.WriteLine("  Confirm with --transport rest before building on it: that run HAS to show refusals,");
        Console.WriteLine("  otherwise the probe simply cannot see them.");
    }
    else
    {
        Console.WriteLine("  READS AS: even over REST nothing was refused, so the probe proves nothing.");
        Console.WriteLine("  Raise --count, or the weight model in HyperLiquid.md is wrong.");
    }
}
if (transport != "none")
    Console.WriteLine($"  Burst itself: {count - burstRefused} of {count} accepted in {burstMs} ms" +
        (firstRefusal >= 0 ? $", first failure at number {firstRefusal + 1}." : "."));

await File.WriteAllLinesAsync(output, rows);
Console.WriteLine();
Console.WriteLine($"Wrote {rows.Count - 1} requests to {Path.GetFullPath(output)}");
return 0;


string? Argument(string name)
{
    string[] arguments = Environment.GetCommandLineArgs();
    for (int i = 0; i < arguments.Length - 1; i++)
    {
        if (arguments[i] == name)
            return arguments[i + 1];
    }
    return null;
}
