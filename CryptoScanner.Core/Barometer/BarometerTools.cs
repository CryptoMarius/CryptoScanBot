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
        CryptoInterval interval, CryptoQuoteData quoteData, CalcBarometerMethod calcBarometerMethod, bool priceBarometer)
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

                    if (symbolIntervalExtra != null && periodStart > symbolIntervalExtra.LastCandleSynchronized)
                        symbolIntervalExtra.LastCandleSynchronized = periodStart;
                }


                // Administratie bijwerken
                if (priceBarometer)
                {
                    barometerData.PriceDateTime = periodStart;
                    barometerData.PriceBarometer = result.Average;
                    barometerData.PriceMedian = result.Median;
                    barometerData.PricePercentageRising = result.PercentageRising;
                    barometerData.PriceSpread = result.Spread;
                    barometerData.PriceSymbolCount = result.SymbolCount;
                    barometerData.PriceOutlierCount = result.OutlierCount;
                    barometerData.PriceMovement = result.AverageAbsolute;
                    barometerData.PriceBitcoinVersusMarket = result.BitcoinVersusMarket;
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
        CryptoQuoteData quoteData, CalcBarometerMethod calcBarometerMethod, bool pricebarometer)
    {
#if debug
        TimerDebugCandles_Tick(quoteData);
#endif

        // Herbereken de candles in de andere intervallen (voor de 15m, 30m, 1h, 4h en 1d)
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (interval.IntervalPeriod == CryptoIntervalPeriod.interval10m ||
                interval.IntervalPeriod == CryptoIntervalPeriod.interval15m ||
                interval.IntervalPeriod == CryptoIntervalPeriod.interval30m ||
                interval.IntervalPeriod == CryptoIntervalPeriod.interval1h ||
                interval.IntervalPeriod == CryptoIntervalPeriod.interval2h ||
                interval.IntervalPeriod == CryptoIntervalPeriod.interval3h ||
                interval.IntervalPeriod == CryptoIntervalPeriod.interval4h ||
                interval.IntervalPeriod == CryptoIntervalPeriod.interval8h ||
                interval.IntervalPeriod == CryptoIntervalPeriod.interval12h ||
                interval.IntervalPeriod == CryptoIntervalPeriod.interval1d)
            {
                //GlobalData.AddTextToLogTab("Calculating barometer chart " + bmSymbol.Name + " " + interval.Name);
                CalculateBarometerInternal(symbol, symbolExtra, interval, quoteData, calcBarometerMethod, pricebarometer);
            }
        }
    }

    // Separate call because of emulator (calculate only 1 quote)
    public static void CalculatePriceBarometerForQuote(CryptoQuoteData quoteData)
    {
        //GlobalData.AddTextToLogTab($"Barometer {quoteData.Name}");
        CryptoSymbol? symbol = CheckBarometerSymbolPrecence(Constants.SymbolNameBarometerPrice, quoteData);
        CryptoSymbol? symbolExtra = CheckBarometerSymbolPrecence(Constants.SymbolNameBarometerExtra, quoteData);
        if (symbol != null)
        {
            CalculateBarometerIntervals(symbol, symbolExtra, quoteData, CryptoBarometerPrice.CalculatePriceBarometer, true);
        }
    }


    public static void CalculatePriceBarometerForAllQuotes()
    {
        // Bereken de (prijs en volume) barometers voor de aangevinkte basismunten
        //GlobalData.AddTextToLogTab("Calculating barometer for all quotes");
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values.ToList())
        {
            if (quoteData.FetchCandles)
                CalculatePriceBarometerForQuote(quoteData);
        }
    }


    public void ExecuteAsync()
    {
        try
        {
            if (Monitor.TryEnter(LockObject))
            {
                try
                {
                    CalculatePriceBarometerForAllQuotes();
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
