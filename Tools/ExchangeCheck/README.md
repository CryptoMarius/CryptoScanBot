# Exchange check

Tooling for the overnight exchange verification: run six exchanges for a night, get one report per
exchange the next morning instead of digging through logs and databases by hand.

Two scripts, no dependencies beyond a Python 3 installation and Windows PowerShell.

| Script | What it does |
|---|---|
| `sample-process.ps1` | Samples working set, threads and handles of a running scanner into a csv. Start it when the run starts. |
| `check_exchange.py` | Reads the data folder afterwards and writes one markdown report with a verdict per subject. |

The numbered `.cmd` files are the double-click front ends, one line per exchange so a market can be
switched off by putting `rem` in front of it:

| File | What it does |
|---|---|
| `1 Start memory sampling.cmd` | Only the sampler |
| `2 Make reports.cmd` | Only the reports |
| `3 Start all scanners.cmd` | Every exchange, then calls `1` |
| `3a Start all Perpetual scanners.cmd` | The nine perpetual markets of `3` |
| `3b Start all Spot scanners.cmd` | The ten spot markets of `3` |
| `4 Stop all scanners.cmd` | Asks every scanner of BOTH builds to close, then calls `2` |
| `5 Start all scanners (Photino).cmd` | Same as `3` for the Photino build |
| `5a Start all Perpetual scanners (Photino).cmd` | Same as `3a` for the Photino build |
| `5b Start all Spot scanners (Photino).cmd` | Same as `3b` for the Photino build |
| `7 Clear all logs.cmd` | Empties every Log folder, for a clean slate before a run |
| `8 Clear all candle databases.cmd` | Deletes the candle database of every exchange, asks first |
| `9 Clear all scanner databases.cmd` | Deletes `CryptoScanBot.db` of every exchange, wants the word DELETE typed |

## The evening: start the run

One exchange per process, each with its own data folder - `GlobalData.ActiveExchange` is singular,
so a process serves one exchange. Separate folders also keep the databases and the log files apart,
which is what makes the morning report possible at all.

Double click **`3 Start all scanners.cmd`** (or `5` for the Photino build). Every supported market
from `CryptoDatabase.CreateExchangeList` is started in `<data>\<brand>\<market>`, five seconds
apart, and the memory sampling then takes over that same window. The scanner creates its data folder
itself, so a new market is one line in that file.

Starting it twice starts a second process on the same folder; the data folder lock stops that one,
so the damage is a message, not a broken database.

A night about one market type only is `3a` for the nine perpetual markets and `3b` for the ten spot
ones, `5a` and `5b` for the same halves of the Photino build. Half the processes, and on an exchange
that counts per ip address half of what the machine asks of it. Running both halves at once is the
same as running the whole one, except that each half starts a memory sampling of its own.

By hand it is one process per market:

```
CryptoScanBot.exe -e "Kraken Spot" -f "E:\CryptoScanBot\Data\Kraken\Spot"
```

Then start the sampler ONCE, after all the scanners are up. It picks up every running scanner and
writes a csv per process, named after that process's data folder. Without it the memory section
stays empty, and a leak only shows up as a slope over many hours - a single measurement says
nothing.

```
powershell -File Tools\ExchangeCheck\sample-process.ps1 -Out "D:\runs"
```

That produces `D:\runs\Kraken-Spot-memory.csv`, `D:\runs\Kraken-Perpetual-memory.csv` and so on. Use
`-Id` to sample one specific process, and `-IntervalSeconds` to change the five minute default.

Both user interfaces are picked up: the Avalonia scanner runs as `CryptoScanBot.exe` and the Photino
one as `CryptoScanBot.Photino.exe`, which are two different process names. The emulator
(`CryptoScanBot.Emulator.exe`) is deliberately left out - it is not a scanner and does not belong in
an exchange report. Pass `-Name` if you want a different set.

Each sample also adds up the WebView2 processes that hang under the scanner, because their memory is
NOT part of its working set. Both user interfaces have them (Photino for its whole window, Avalonia
for the hidden browser), and on a normal run they are good for several hundred megabytes - measured
on Binance Perpetual: 743 MB in the scanner plus 497 MB in six WebView2 processes.

The start time does not have to be written down: the report finds the last scanner startup in the
log by itself (the plugin registration only happens at process start). Pass `--start`/`--end` when
you want to look at a smaller slice than the whole run.

Starting a fresh series? **`7 Clear all logs.cmd`** empties the Log folder of every exchange, so the
report cannot pick up errors from the night before. It asks before it deletes, and it only takes the
log files - databases, settings and candles stay. Run it with the scanners stopped: a running
scanner holds its log file open, so those survive and you end up with half a reset. The file warns
when it sees one running and lists afterwards what could not be deleted.

## The morning: stop the run

**`4 Stop all scanners.cmd`** is the only stop file and it closes both builds, Avalonia and
Photino, named one by one so a running `CryptoScanBot.Emulator.exe` is left alone. A build that was
not started says "none running" instead of an error, so a mixed night stops with one double click.

It runs `taskkill` **without** `/F`. That is a request, not a kill: it posts a close message to the
window, the same thing as clicking the cross, and the scanner then runs its own shutdown. Measured
on a test run the difference is visible from the outside - after a clean stop the `-wal` and `-shm` files next to the database are gone and a
data folder that never had a settings file has one. Adding `/F` throws exactly that away, so do not.

After the wait the file lists whatever is still running. Close those from their own window; their
last candles are not on disk yet. Then the reports are built.

Both builds share one process name each, so this stops all Avalonia scanners at once (or all Photino
ones). A single exchange is closed from its own window.

The memory sampling window does not stop by itself - close that one yourself.

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
| Streams | `Log\*.log` | Subscriptions started, connections lost and restored, restarts, symbol list changes, and how often the inactivity check tore the whole feed down and rebuilt it |
| Errors | `Log\* Error.log` | Errors CLASSIFIED, not counted: from our own code / not recognised / known and recovered / not the scanner's |
| Signals | `CryptoScanBot.db` | Signals, zones and (when trading was on) positions inside the window |
| Memory | sampler csv, `$debug\Memory Dump` | Growth per hour of the scanner AND its WebView2 processes together, thread and handle growth, managed versus native split |

Every database is opened read-only, so the report can be made while the scanner still runs.

## Things worth knowing before reading a report

- **The log is local time, the databases are UTC.** The report prints the window in both.
- **A subscription can fail without ever saying "connection lost".** One that stays connected and
  stops delivering is found by the inactivity check instead, which logs `One of ... tickers has
  stopped` and then rebuilds. Those are separate log lines and a separate row in the report; a
  market can be perfectly quiet on the drop count and still rebuild its whole feed every seven
  minutes, which is exactly what HyperLiquid Spot did on 21/22-08-2026 while the report said good.
  The check number in that line is a counter that only resets on a clean check, so one that keeps
  climbing all night means the inactivity is a property of that market, not an incident - and the
  fix is `ExchangeOptions.MaximumTickerInactivity` for that exchange, measured from the longest
  inactivity per symbol in this same report.
- **The instrument list is not the coverage target.** The minimal volume per quote coin keeps most
  instruments out on purpose, so coverage is measured against the number of symbols the log says
  were subscribed.
- **Barometer rows live in the candle table.** `$BMP...` holds percentages, so negative values are
  normal there; those rows are excluded from the plausibility checks and reported separately.
- **A running scanner looks late.** Candles are flushed by the save thread, so up to roughly a
  quarter of an hour of lateness on a live process is the flush interval, not a missing candle.
- **A window under fifteen minutes proves nothing about coverage**, and under an hour of samples
  proves nothing about memory. The report says so instead of guessing.
- **The memory verdict is about the scanner plus its WebView2 processes.** A leak in the browser
  side leaves the scanner process itself perfectly flat, so a verdict on that process alone would
  call such a night healthy. The report prints both numbers: when the total climbs while the scanner
  stays flat, the growth is on the WebView2 side.
- **A csv from before those columns existed keeps its old layout.** The sampler does not widen an
  existing file halfway through, and the report says out loud that it is then judging the scanner
  process on its own. Start a fresh csv to get the full picture.

## Thresholds

All of them sit at the top of `check_exchange.py` as named constants: lateness, gap percentages,
drops per hour and memory growth. They are a starting point measured on a healthy Binance Perpetual
run, not a law - adjust them once you know what each exchange normally does.

Errors are the exception: they have no threshold at all since 21-08-2026. Counting could not tell
the two cases apart that matter, and both happened in the same week - the race in
`BulkCalculateCandles` on Okx Perpetual was TWO lines and slipped under every threshold, while 73 of
the 131 lines of 18/19-08 were Avalonia notices that decide nothing. So the verdict follows the KIND
of error (`ERRORS_OURS`, `ERRORS_RECOVERED`, `ERRORS_NOT_OURS`, and everything else): an exception
carrying our own stack frames is always reported no matter how few, a recovered timeout never sets
the verdict no matter how many, and anything the classification cannot place goes up WITH its text.

That last box is the one that keeps the rest honest. A classification that swallows everything is a
filter, not a classification, so resist widening `ERRORS_RECOVERED` until someone has actually
looked at the shape being added.
