# PatternScan

Runs every candlestick pattern of the `OHLC_Candlestick_Patterns` package over the scanner's own
candle database and writes one line per detected signal, so the judging can happen in Python next to
the rest of `Tools/EntryTiming`. Deliberately outside the solution: the library works on a whole
series per call while the scanner evaluates one candle at a time.

```
dotnet run -- --db "E:/CryptoScanBot/Data/Binance/Emulator/Binance Perpetual.db" --interval 15m --out patterns.csv
```

## What it measured, and why it stopped there

**The library's pattern definitions use ABSOLUTE price thresholds, so on crypto they measure the
price of the coin instead of the shape of the candle.** Measured 29-08-2026, 15m candles, same
period, same settings:

| symbol | price scale | patterns that fired | signals |
|---|---|---|---|
| BTCUSDT.PERP | ~65 000 | 7 of 74 | 2 931 |
| ADAUSDT.PERP | ~0,50 | 32 of 74 | 32 136 |
| 1000PEPEUSDT.PERP | ~0,01 | 24 of 74 | 29 993 |

Not just the counts - the mix changes completely. On Bitcoin `BullishInvertedHammer` is 81% of
everything found and `BearishEngulfing` never fires at all; on the cheap coins
`BearishLongBlackCandelstick` fires on 51% of ALL candles, which is not a pattern, it is "the candle
is red". A library that finds an engulfing on ADA and never on Bitcoin is reporting the price scale.

The thresholds (`_minimumCandleSize`, `_maximumCandleBodySize`, `_minimumCandleShadowSize` and so
on) are private fields with no public way to set them, so this cannot be tuned from the outside.

Two other things worth recording:

* **Version 2.0.2 cannot be installed at all.** It declares a dependency on a package called
  `Enumerators` 1.0.0 that is not published on nuget.org (confirmed: the flat-container index
  returns 404). Version 2.0.1 is the last usable one and is what this project references.
* The README of the package claims Newtonsoft.Json and ScottPlot are only used in its demo project.
  The nuspec disagrees - Newtonsoft.Json 13.0.3 is a real dependency.

## Where this leaves it

The pattern definitions themselves are public knowledge (Nison), not this author's invention, and the
package is MIT, so reading it is free. What we need is the same shapes expressed RELATIVELY - the
wick as a percentage of the candle's own range - which is how everything else in this codebase is
written. That belongs in the scanner as an entry condition, not in a dependency.
