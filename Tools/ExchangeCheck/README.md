# Exchange check

Tooling for the overnight exchange verification: run six exchanges for a night, get one report per
exchange the next morning instead of digging through logs and databases by hand.

Two scripts, no dependencies beyond a Python 3 installation and Windows PowerShell.

| Script | What it does |
|---|---|
| `sample-process.ps1` | Samples working set, threads and handles of a running scanner into a csv. Start it when the run starts. |
| `check_exchange.py` | Reads the data folder afterwards and writes one markdown report with a verdict per subject. |

## The evening: start the run

One exchange per process, each with its own data folder - `GlobalData.ActiveExchange` is singular,
so a process serves one exchange. Separate folders also keep the databases and the log files apart,
which is what makes the morning report possible at all.

```
CryptoScanner.exe -f "Data\Kraken Spot" -e "Kraken Spot"
```

Then start the sampler ONCE, after all the scanners are up. It picks up every running scanner and
writes a csv per process, named after that process's data folder. Without it the memory section
stays empty, and a leak only shows up as a slope over many hours - a single measurement says
nothing.

```
powershell -File Tools\ExchangeCheck\sample-process.ps1 -Out "D:\runs"
```

That produces `D:\runs\Kraken-Spot-memory.csv`, `D:\runs\Kraken-Futures-memory.csv` and so on. Use
`-Id` to sample one specific process, and `-IntervalSeconds` to change the five minute default.

The start time does not have to be written down: the report finds the last scanner startup in the
log by itself (the plugin registration only happens at process start). Pass `--start`/`--end` when
you want to look at a smaller slice than the whole run.

## The morning: make the report

```
python Tools\ExchangeCheck\check_exchange.py ^
    --folder "%APPDATA%\CryptoScanBot\Data\Kraken\Spot" ^
    --memory-csv "D:\runs\Kraken-Spot-memory.csv" ^
    --out "D:\runs\Kraken-Spot-report.md" --json "D:\runs\Kraken-Spot-facts.json"
```

A bare folder name is looked up inside `%APPDATA%`, so `--folder CryptoScanBot-KRTest` works too.

The window comes from the last startup in the log; the report header says which source it used.
`--start`/`--end` override it. Times are local, the way they appear in the log and on the clock; the
report converts them to UTC itself for the databases.

The exit code is 0 for good, 1 when something needs attention and 2 for a failure, so a batch file
over six folders can flag the ones worth reading first.

The `--json` file holds the same facts without the prose. Keeping it per night makes the fourth
night comparable to the first - that comparison catches slow regressions that a single report never
shows.

## What it checks

| Subject | Source | The question it answers |
|---|---|---|
| Settings | `*-settings.json` | Was trading on, via which route, on which intervals and strategies |
| Symbols | `CryptoScanBot.db` | Instrument count, quotes, tick sizes, missing instrument names |
| Candles | `<Exchange>.db` | Coverage per interval, lateness, gaps in the minute series, impossible candles, subscribed versus delivered |
| Streams | `Log\*.log` | Subscriptions started, connections lost and restored, restarts, symbol list changes |
| Errors | `Log\* Error.log` | Every error grouped by normalised message, plus rate limits and bans |
| Signals | `CryptoScanBot.db` | Signals, zones and (when trading was on) positions inside the window |
| Memory | sampler csv, `$debug\Memory Dump` | Growth per hour, thread and handle growth, managed versus native split |

Every database is opened read-only, so the report can be made while the scanner still runs.

## Things worth knowing before reading a report

- **The log is local time, the databases are UTC.** The report prints the window in both.
- **The instrument list is not the coverage target.** The minimal volume per quote coin keeps most
  instruments out on purpose, so coverage is measured against the number of symbols the log says
  were subscribed.
- **Barometer rows live in the candle table.** `$BMP...` holds percentages, so negative values are
  normal there; those rows are excluded from the plausibility checks and reported separately.
- **A running scanner looks late.** Candles are flushed by the save thread, so up to roughly a
  quarter of an hour of lateness on a live process is the flush interval, not a missing candle.
- **A window under fifteen minutes proves nothing about coverage**, and under an hour of samples
  proves nothing about memory. The report says so instead of guessing.

## Thresholds

All of them sit at the top of `check_exchange.py` as named constants: lateness, gap percentages,
drops per hour, error counts and memory growth. They are a starting point measured on a healthy
Binance Futures run, not a law - adjust them once you know what each exchange normally does.
