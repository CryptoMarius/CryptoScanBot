using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Barometer;

public class BarometerTools
{
    private static readonly object LockObject = new();
    private delegate bool CalcBarometerMethod(CryptoQuoteData quoteData, SortedList<string, CryptoSymbol> symbols,
        CryptoInterval interval, CandleTime unixCandleLast, BarometerResult result);


    public static void InitBarometerSymbols()
    {
        // Check all the (internal) barometer symbols
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles)
            {
                CheckBarometerSymbolPrecence(Constants.SymbolNameBarometerPrice, quoteData);
                CheckBarometerSymbolPrecence(Constants.SymbolNameBarometerExtra, quoteData);
            }
        }
    }

    private static CryptoSymbol? CheckBarometerSymbolPrecence(string baseName, CryptoQuoteData quoteData)
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange != null)
        {
            if (!exchange.TryGetSymbolByPair(baseName + quoteData.Name, out CryptoSymbol? symbol))
            {
                symbol = new CryptoSymbol
                {
                    Exchange = exchange,
                    ExchangeId = exchange.Id,
                    Name = baseName + quoteData.Name,
                    Base = baseName, //De "munt"
                    Quote = quoteData.Name, //USDT, BTC etc.
                    QuoteData = quoteData,
                    ExchangeName = baseName + quoteData.Name,
                    QuantityTickSize = 0.01m,
                    PriceTickSize = 0.01m,
                    Volume = 0,
                    Status = 1,
                };

                using CryptoDatabase databaseThread = new();
                databaseThread.Open();
                var transaction = databaseThread.BeginTransaction();
                try
                {
                    databaseThread.Connection.Insert(symbol, transaction);
                    transaction.Commit();
                }
                catch (Exception error)
                {
                    ScannerLog.Logger.Error(error.ToString());
                    transaction.Rollback();
                    throw;
                }

                GlobalData.AddSymbol(symbol);
                //GlobalData.AddTextToLogTab($"Created barometer {symbol.Name}");
            }

            // Apply some defaults
            symbol.Status = 1;
            symbol.PriceDecimals = 2;
            symbol.PriceDisplayFormat = "N2"; // percentage
            symbol.QuantityDisplayFormat = "N2"; // percentage
            return symbol;
        }
        return null;
    }


    private static void CalculateBarometerInternal(CryptoSymbol bmSymbol, CryptoSymbol? bmSymbolExtra,
        CryptoInterval interval, CryptoQuoteData quoteData, CalcBarometerMethod calcBarometerMethod, bool priceBarometer,
        decimal? marketTrendPrimary = null, decimal? marketTrendSecondary = null)
    {
        //if (priceBarometer)
        //    GlobalData.AddTextToLogTab($"Calculating price barometer chart {quoteData.Name} {interval.Name}");
        //else
        //    GlobalData.AddTextToLogTab($"Calculating volume barometer chart {quoteData.Name} {interval.Name}");

        CryptoSymbolInterval symbolInterval = bmSymbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolInterval.CandleList;

        // A candle holds five numbers and one measurement produces more, so the rest goes into the
        // candles of a second symbol. It is kept in lockstep with the first: same minutes, same
        // clean-up, written in the same pass. Never calculated separately - that would cost a second
        // full measurement for figures that are already in hand.
        CryptoSymbolInterval? symbolIntervalExtra = bmSymbolExtra?.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList? candlesExtra = symbolIntervalExtra?.CandleList;

        // Remove old candles from the barometer symbol (< 24 hours, 1440 candles).
        // Barometer does not run in the emulator (it needs the full symbol pool), so the
        // legacy BackTest-branch has been removed from this method entirely.
        CandleTime startFetchUnix = CandleTools.GetCandleFetchStart(bmSymbol, interval, DateTime.UtcNow);
        // Use TryGetFirstCandle() so the read is covered by the CryptoCandleList read lock,
        // preventing InvalidOperationException when another thread concurrently calls Add().
        //
        // Candles written before BarometerCandleFields existed are dropped here as well. Back then
        // all four price fields held the same number, so such a candle reads its average as the
        // breadth and again as the spread - the graph would quietly show nonsense for the part of
        // the history that came out of candles.db. They are always the oldest ones, so this loop
        // reaches them before it meets a valid candle. recalculateFrom brings them back correctly.
        CandleTime? recalculateFrom = null;
        while (candles.TryGetFirstCandle(out CryptoCandle c))
        {
            if (c.OpenTime < startFetchUnix)
                candles.Remove(c.OpenTime);
            else break;
        }

        // The second symbol ages out on the same schedule.
        if (candlesExtra != null)
        {
            while (candlesExtra.TryGetFirstCandle(out CryptoCandle c))
            {
                if (c.OpenTime < startFetchUnix)
                    candlesExtra.Remove(c.OpenTime);
                else break;
            }
        }

        // Unusable candles are not only at the front. A session that stops and starts again leaves a
        // block of them BEHIND the valid ones, so the loop above would meet a good candle and stop
        // right before them. Sweep the whole window instead - it is a few hundred candles, and only
        // the first calculation after a start finds anything.
        foreach (CryptoCandle candle in candles.GetLastNValues(Constants.BarometerGraphHours * 60, 1))
        {
            if (BarometerCandleFields.IsLegacyLayout(candle))
            {
                if (recalculateFrom == null || candle.OpenTime < recalculateFrom.Value)
                    recalculateFrom = candle.OpenTime;
                candles.Remove(candle.OpenTime);

                // Drop the same minute from the second symbol, or the two would describe different
                // moments until the recalculation catches up.
                candlesExtra?.Remove(candle.OpenTime);
            }
        }


        CandleTime periodStart, periodStop;

        CryptoBarometerData? barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(quoteData.Name, interval.IntervalPeriod);

        // Begin van de candle in interval X, bereken het laatste interval opnieuw (bewust)
        if (symbolInterval.LastCandleSynchronized.HasValue)
            periodStart = symbolInterval.LastCandleSynchronized.Value;
        else
        {
            // Geef deze alvast een waarde — use TryGetFirstCandle() for thread-safe key access.
            if (candles.TryGetFirstCandle(out CryptoCandle firstCandle))
                periodStart = firstCandle.OpenTime;
            else
                periodStart = CandleTime.AlignFromDateTime(DateTime.UtcNow.AddDays(-2), 1);

            symbolInterval.LastCandleSynchronized = periodStart;
        }

        // Whatever was dropped for its old layout has to be computed again, so go back that far. The
        // 1m candles it is derived from are fetched for the whole graph window anyway, so this is the
        // same work a first-ever start does - once, right after the update.
        if (recalculateFrom.HasValue && recalculateFrom.Value < periodStart)
            periodStart = recalculateFrom.Value;

        // De laatste candle die we moeten berekenen. Mogelijk 1 te hoog, wat "valse" waarden kan geven?
        // Dat kan opgelost worden door de laatst aangekomen candle mee te geven (vanuit de 1m stream)
        periodStop = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1);
        //DateTime periodStartDebug = CandleTools.GetUnixDate(periodStart);
        //DateTime periodStopDebug = CandleTools.GetUnixDate(periodStop);


        //if (priceBarometer)
        //    GlobalData.AddTextToLogTab($"Calculating price barometer chart {quoteData.Name} {interval.Name} from {periodStart.ToDateTime()} to {periodStop.ToDateTime()}");
        //else
        //    GlobalData.AddTextToLogTab($"Calculating volume barometer chart {quoteData.Name} {interval.Name} from {periodStart.ToDateTime()} to {periodStop.ToDateTime()}");


        // One result object for the whole run. The loop below walks minute by minute and can have
        // hours of backlog after a restart, so a fresh object per measurement would be pure garbage.
        BarometerResult result = new();

        // The newest minute the loop actually wrote a candle for. Not the same as periodStop: that
        // is the minute in progress, whose 1m candles are still in the ticker cache (they are
        // flushed a few seconds AFTER the minute closes, see SubscriptionKLineCachedTicker), so the
        // measurement for it fails and no candle is created. The market trend is attached to this
        // minute below - writing it to periodStop meant writing it nowhere at all.
        CandleTime? lastExtraWritten = null;

        // De opgegeven periode per minuut itereren
        while (periodStart <= periodStop)
        {
            //periodStartDebug = CandleTools.GetUnixDate(periodStart);

            // Bereken de 1e waarde (alleen candle aanmaken als er candles bestaan voor beide intervallen)
            if (calcBarometerMethod(quoteData, bmSymbol.Exchange.SymbolListName, interval, periodStart, result))
            {
                // De candle aanmaken of bijwerken
                if (!candles.TryGetValue(periodStart, out CryptoCandle candle))
                {
                    candle = new CryptoCandle
                    {
                        OpenTime = periodStart,
                    };
                    candles.Add(candle.OpenTime, candle);
                }

                // Just fill all the ohlc + v
                //
                // Which figure goes in which field is decided by BarometerCandleFields, together
                // with the code that reads them back for the graph - see there for the layout and
                // why High/Low are not the highest and lowest value here.
                // CryptoCandle is a struct, so Store() takes it by ref - without that it would fill a
                // copy and the line below would write back an all-zero candle.
                candle.TickDecimals = bmSymbol.PriceDecimals;
                BarometerCandleFields.Store(ref candle, result);
                candles[periodStart] = candle;

                // The remaining figures of the very same measurement, in the second symbol.
                if (candlesExtra != null && bmSymbolExtra != null)
                {
                    if (!candlesExtra.TryGetValue(periodStart, out CryptoCandle candleExtra))
                    {
                        candleExtra = new CryptoCandle
                        {
                            OpenTime = periodStart,
                        };
                        candlesExtra.Add(candleExtra.OpenTime, candleExtra);
                    }

                    candleExtra.TickDecimals = bmSymbolExtra.PriceDecimals;
                    BarometerCandleFields.StoreExtra(ref candleExtra, result);
                    candlesExtra[periodStart] = candleExtra;

                    // Remember which minute this was, so the market trend can be attached to the
                    // newest one afterwards - see the write below the loop.
                    lastExtraWritten = periodStart;

                    if (symbolIntervalExtra != null && periodStart > symbolIntervalExtra.LastCandleSynchronized)
                        symbolIntervalExtra.LastCandleSynchronized = periodStart;
                }


                // Administratie bijwerken
                if (priceBarometer)
                {
                    StorePriceResult(barometerData, result, periodStart);

                    // The market trend belongs to the quote coin and not to this interval, so every
                    // interval of the quote carries the same value - exactly as the emulator stores
                    // it. Only written when it was measured this run: a null here would erase a
                    // value that is still perfectly current.
                    if (marketTrendPrimary.HasValue)
                        barometerData.MarketTrendPrimary = marketTrendPrimary;
                    if (marketTrendSecondary.HasValue)
                        barometerData.MarketTrendSecondary = marketTrendSecondary;
                }
                else
                {
                    barometerData.VolumeDateTime = periodStart;
                    barometerData.VolumeBarometer = result.Average;
                }

                // Willen we dat hier wel bijwerken, zie ook opmerking hierboven
                if (periodStart > symbolInterval.LastCandleSynchronized)
                    symbolInterval.LastCandleSynchronized = periodStart;

                if (GlobalData.Settings.General.DebugKLineReceive && (GlobalData.Settings.General.DebugSymbol == bmSymbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                    ScannerLog.Logger.Trace($"Debug candle {candle.OhlcText(bmSymbol, GlobalData.IntervalList[0], bmSymbol.PriceDisplayFormat, true, true, true)}");

                //if (priceBarometer)
                //    GlobalData.AddTextToLogTab($"Calculated price barometer chart {quoteData.Name} {interval.Name} {periodStart.ToDateTime()} {BarometerPerc}");
                //else
                //    GlobalData.AddTextToLogTab($"Calculated volume barometer chart {quoteData.Name} {interval.Name} {periodStart.ToDateTime()} {BarometerPerc}");
            }

            // Naar de volgende 1m candle
            periodStart += 1;
        }

        // The market trend of this run, on the newest minute that got a candle. The trend can only
        // be read as it stands now, so the backlog minutes of a restart keep whatever they already
        // held: painting the current value over history would be an invention, and a zero would
        // claim a market that is exactly neutral.
        if (candlesExtra != null && lastExtraWritten.HasValue
            && (marketTrendPrimary.HasValue || marketTrendSecondary.HasValue)
            && candlesExtra.TryGetValue(lastExtraWritten.Value, out CryptoCandle trendCandle))
        {
            BarometerCandleFields.StoreMarketTrend(ref trendCandle, marketTrendPrimary, marketTrendSecondary);
            candlesExtra[lastExtraWritten.Value] = trendCandle;
        }
    }

#if debug
    // first 20 should be enough..
    static int dumpCount = 0;
    static DateTime startTime;
    private static void TimerDebugCandles_Tick(CryptoQuoteData quoteData)
    {
        if (dumpCount < 20)
        {
            // Dump X times the candles off BTCUSDT because of the problem Roy reported
            DateTime now = DateTime.UtcNow;
            if (dumpCount == 0)
                startTime = now;

            var exchange = GlobalData.ActiveExchange!;
            if (exchange.TryGetSymbolByPair("BTC" + quoteData.Name, out CryptoSymbol? symbol))
            {

                StringBuilder writer = new();
                var symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                foreach (var candle in symbolInterval.CandleList.Values.ToList())
                {
                    writer.AppendLine(candle.OhlcText(symbol, GlobalData.IntervalList[0], symbol.PriceDisplayFormat, false, false, true));
                }

                var baseFolder = Path.Combine(GlobalData.GetBaseDir(), "$Debug", "Missing Candles", symbol.Quote, symbol.Base, startTime.ToString("yyyy-MM-dd HHmm"));
                Directory.CreateDirectory(baseFolder);
                var filename = Path.Combine(baseFolder, $"{symbol.Name} candles 1m {now:yyyy-MM-dd HHmm}.txt");
                File.WriteAllText(filename, writer.ToString());
            }
            dumpCount++;
        }
    }
#endif

    /// <summary>
    /// Deze routine maakt barometer per 1m (ondanks dat we met de IntervalPeriod suggereren dat we het in een bepaald interval doen)
    /// </summary>
    private static void CalculateBarometerIntervals(CryptoSymbol symbol, CryptoSymbol? symbolExtra,
        CryptoQuoteData quoteData, CalcBarometerMethod calcBarometerMethod, bool pricebarometer,
        decimal? marketTrendPrimary = null, decimal? marketTrendSecondary = null)
    {
#if debug
        TimerDebugCandles_Tick(quoteData);
#endif

        // Herbereken de candles in de andere intervallen (voor de 15m, 30m, 1h, 4h en 1d)
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (Array.IndexOf(BarometerIntervals, interval.IntervalPeriod) >= 0)
            {
                //GlobalData.AddTextToLogTab("Calculating barometer chart " + bmSymbol.Name + " " + interval.Name);
                CalculateBarometerInternal(symbol, symbolExtra, interval, quoteData, calcBarometerMethod, pricebarometer,
                    marketTrendPrimary, marketTrendSecondary);
            }
        }
    }


    /// <summary>
    /// The intervals a barometer is calculated for. In a list of its own because the backlog that
    /// MarketTrendBackfill fills in has to be written to exactly these ones - an interval that is
    /// calculated but not filled would show a hole in the graph that no other interval has.
    /// </summary>
    private static readonly CryptoIntervalPeriod[] BarometerIntervals =
    [
        CryptoIntervalPeriod.interval10m,
        CryptoIntervalPeriod.interval15m,
        CryptoIntervalPeriod.interval30m,
        CryptoIntervalPeriod.interval1h,
        CryptoIntervalPeriod.interval2h,
        CryptoIntervalPeriod.interval3h,
        CryptoIntervalPeriod.interval4h,
        CryptoIntervalPeriod.interval8h,
        CryptoIntervalPeriod.interval12h,
        CryptoIntervalPeriod.interval1d,
    ];

    /// <summary>
    /// Copy one measurement into the last known values of a quote coin. Shared by the calculation
    /// above and the emulator path below, so the two cannot drift apart: PriceBarometer stays the
    /// average (everything that reads it keeps its meaning) and the rest of BarometerResult follows
    /// along.
    /// </summary>
    private static void StorePriceResult(CryptoBarometerData barometerData, BarometerResult result, CandleTime at)
    {
        barometerData.PriceDateTime = at;
        barometerData.PriceBarometer = result.Average;
        barometerData.PriceMedian = result.Median;
        barometerData.PricePercentageRising = result.PercentageRising;
        barometerData.PriceSpread = result.Spread;
        barometerData.PriceSymbolCount = result.SymbolCount;
        barometerData.PriceOutlierCount = result.OutlierCount;
        barometerData.PriceMovement = result.AverageAbsolute;
        barometerData.PriceBitcoinVersusMarket = result.BitcoinVersusMarket;
    }


    /// <summary>
    /// The price barometer of one quote coin over an explicit symbol list, WITHOUT writing barometer
    /// candles. This is the emulator path.
    /// <para>
    /// A replay draws no barometer graph, so the candles of the barometer symbols - which the live
    /// calculation writes, ages out on wall-clock time and reads back for that graph - are pure cost
    /// there. What a replay does need is the value the signal checks read, and that is
    /// <see cref="CryptoBarometerData"/>. This method fills exactly that.
    /// </para>
    /// <para>
    /// A measurement that fails, or one carried by fewer than <paramref name="minimumSymbolCount"/>
    /// coins, leaves the previous value untouched instead of writing a number that describes no
    /// market. That is also what the live scanner does with a failed measurement, and
    /// <see cref="CryptoBarometerData.PriceDateTime"/> tells the reader how old the value is.
    /// </para>
    /// </summary>
    /// <returns>True when a new value was stored.</returns>
    public static bool CalculateForSymbols(Model.CryptoExchange exchange, CryptoQuoteData quoteData,
        IReadOnlyList<CryptoSymbol> symbolList, CryptoInterval interval, CandleTime unixCandleLast,
        int minimumSymbolCount, BarometerResult result)
    {
        if (!CryptoBarometerPrice.CalculatePriceBarometer(quoteData, symbolList, interval, unixCandleLast, result))
            return false;

        if (result.SymbolCount < minimumSymbolCount)
            return false;

        CryptoBarometerData barometerData = exchange.Data.GetBarometer(quoteData.Name, interval.IntervalPeriod);
        StorePriceResult(barometerData, result, unixCandleLast);
        return true;
    }


    /// <summary>
    /// Quote coins whose market trend backlog has been filled in this session. The work is a
    /// warm-up of the zigzag over every coin and every interval, so it may not run again on the
    /// timer every thirty seconds - once it has succeeded, the live measurement keeps the series
    /// complete by itself.
    /// </summary>
    private static readonly HashSet<string> MarketTrendBacklogFilled = [];


    /// <summary>
    /// Forget that the backlog was filled, so the next calculation looks again. The scanner never
    /// needs this - a session fills the hole its own restart left and then keeps the series complete
    /// by itself - but a test that sets up a fresh world would otherwise inherit the mark of the
    /// test that ran before it and silently skip the fill it is testing.
    /// </summary>
    internal static void ForgetMarketTrendBacklog()
    {
        MarketTrendBacklogFilled.Clear();
    }


    /// <summary>
    /// Fill in the market trend of minutes that nobody measured, for the part of the graph window
    /// that is missing it. That is what every restart leaves behind: the values from before it come
    /// back with the candles, the minutes the scanner was down were never measured by anybody.
    /// <para>
    /// A minute that already carries a value is never touched. The test for "has no value" is that
    /// BOTH figures are exactly zero - a measured pair that lands on 0.00 twice over does not occur
    /// in practice (one isolated case in 381.510 measured minutes of emulator data), and the cost of
    /// being wrong about it is one recomputed minute.
    /// </para>
    /// </summary>
    private static void FillMarketTrendBacklog(CryptoSymbol symbolExtra, CryptoQuoteData quoteData)
    {
        if (MarketTrendBacklogFilled.Contains(quoteData.Name))
            return;

        // What is missing, over all the intervals the barometer keeps. They hold the same minutes,
        // but not necessarily the same ones after a restart, so the widest gap decides.
        CandleTime? from = null, to = null;
        foreach (CryptoIntervalPeriod period in BarometerIntervals)
        {
            CryptoCandleList candles = symbolExtra.GetSymbolInterval(period).CandleList;
            foreach (CryptoCandle candle in candles.GetLastNValues(Constants.BarometerGraphHours * 60, 1))
            {
                if (candle.Low != 0 || candle.Volume != 0)
                    continue;
                if (from == null || candle.OpenTime < from.Value)
                    from = candle.OpenTime;
                if (to == null || candle.OpenTime > to.Value)
                    to = candle.OpenTime;
            }
        }

        if (from == null || to == null)
        {
            // Nothing missing at all - a scanner that has been running for hours. Remember it, so
            // the scan above is not repeated every thirty seconds either.
            MarketTrendBacklogFilled.Add(quoteData.Name);
            return;
        }

        SortedList<CandleTime, (decimal? Primary, decimal? Secondary)> series =
            Trend.MarketTrendBackfill.Measure(quoteData.SymbolList, from.Value, to.Value, Trend.MarketTrend.MinimumSymbols);
        if (series.Count == 0)
            return; // too few coins, or their candles are not loaded yet - try again next time

        int written = 0;
        foreach (CryptoIntervalPeriod period in BarometerIntervals)
        {
            CryptoCandleList candles = symbolExtra.GetSymbolInterval(period).CandleList;
            foreach (CryptoCandle candle in candles.GetLastNValues(Constants.BarometerGraphHours * 60, 1))
            {
                if (candle.Low != 0 || candle.Volume != 0)
                    continue;
                if (!series.TryGetValue(candle.OpenTime, out (decimal? Primary, decimal? Secondary) value))
                    continue;

                CryptoCandle updated = candle;
                BarometerCandleFields.StoreMarketTrend(ref updated, value.Primary, value.Secondary);
                candles[candle.OpenTime] = updated;
                written++;
            }
        }

        MarketTrendBacklogFilled.Add(quoteData.Name);
        GlobalData.AddTextToLogTab($"Barometer {quoteData.Name}: market trend calculated afterwards for " +
            $"{written} minutes that were not measured ({from.Value.ToLocalTime():HH:mm} - {to.Value.ToLocalTime():HH:mm})");
    }


    // Separate call because of emulator (calculate only 1 quote)
    public static void CalculatePriceBarometerForQuote(CryptoQuoteData quoteData, bool measureMarketTrend = true)
    {
        //GlobalData.AddTextToLogTab($"Barometer {quoteData.Name}");
        CryptoSymbol? symbol = CheckBarometerSymbolPrecence(Constants.SymbolNameBarometerPrice, quoteData);
        CryptoSymbol? symbolExtra = CheckBarometerSymbolPrecence(Constants.SymbolNameBarometerExtra, quoteData);
        if (symbol != null)
        {
            // Once per quote coin and not once per interval: the trend is a property of the coin,
            // not of the interval it is asked about, so measuring it inside the interval loop would
            // do the same work ten times over. See MarketTrend, and the identical reasoning in
            // BarometerReplay.Execute.
            decimal? marketTrendPrimary = null, marketTrendSecondary = null;
            if (measureMarketTrend)
                (marketTrendPrimary, marketTrendSecondary) = Trend.MarketTrend.Measure(quoteData.SymbolList, Trend.MarketTrend.MinimumSymbols);

            CalculateBarometerIntervals(symbol, symbolExtra, quoteData, CryptoBarometerPrice.CalculatePriceBarometer, true,
                marketTrendPrimary, marketTrendSecondary);

            // And the minutes nobody measured, which is what a restart leaves behind. After the
            // calculation above, so the candles it just created are there to be filled.
            if (measureMarketTrend && symbolExtra != null)
                FillMarketTrendBacklog(symbolExtra, quoteData);
        }
    }


    public static void CalculatePriceBarometerForAllQuotes(bool measureMarketTrend = true)
    {
        // Bereken de (prijs en volume) barometers voor de aangevinkte basismunten
        //GlobalData.AddTextToLogTab("Calculating barometer for all quotes");
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values.ToList())
        {
            if (quoteData.FetchCandles)
                CalculatePriceBarometerForQuote(quoteData, measureMarketTrend);
        }
    }


    /// <summary>
    /// Recalculate the barometer of every quote coin.
    /// <para>
    /// <paramref name="measureMarketTrend"/> false skips the market trend and leaves the previous
    /// value in place. It exists for the one caller that runs on a UI thread: measuring the trend
    /// walks every coin of the quote, and a dashboard timer must not freeze the window for that.
    /// Nothing is lost by skipping it - the scanner session recalculates the barometer every thirty
    /// seconds on a background thread, that run does measure the trend, and it rewrites the same
    /// minute, so the value follows within half a minute.
    /// </para>
    /// </summary>
    public void ExecuteAsync(bool measureMarketTrend = true)
    {
        try
        {
            if (Monitor.TryEnter(LockObject))
            {
                try
                {
                    CalculatePriceBarometerForAllQuotes(measureMarketTrend);
                }
                finally
                {
                    Monitor.Exit(LockObject);
                }

                // Nu de barometer uitgerekend is mag het aantal 1m candles naar beneden
                CandleTools.SetInitialCandleCountFetch(24 * 60 + 10);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }
}
