using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// The symbol's 24 hour volume during a replay, taken from the daily candle being replayed instead
/// of from the last "fetch symbols".
/// <para>
/// <see cref="Core.Core.CandleHelpers.CheckValidMinimalVolume"/> starts with
/// <c>symbol.Volume &lt; QuoteData.MinimalVolume</c> and rejects the symbol for the whole tick when
/// it fails - before the indicators are even calculated. <c>symbol.Volume</c> is the live 24 hour
/// value the price ticker maintains, which is exactly right for the scanner and exactly wrong for a
/// replay of January: it decides on today's liquidity whether a symbol takes part in a period that
/// is months old.
/// </para>
/// <para>
/// Measured on 29-08-2026: of the 50 symbols in a run, 17 sat below the 15,000,000 threshold and not
/// one of them opened a single position, while the 33 above it all did. The filter WAS the symbol
/// selection. Two identical runs a day apart (401 and 507) differed by 79.82 - 16% - purely because
/// six symbols had dropped below the line and two had risen above it in the meantime. That made runs
/// irreproducible across days, and it is look-ahead: only what is liquid today gets traded in a test
/// about the past.
/// </para>
/// <para>
/// This lives in the emulator on purpose. The check itself sits in Core and is shared with the live
/// scanner, where the current value IS the right answer; the difference belongs to the way the
/// engine is being driven, not to the check. So nothing in Core changes - the emulator simply keeps
/// <c>symbol.Volume</c> pointing at the day it is replaying, and the existing check reads it as it
/// always did. As a result symbols now drop in and out over the run, the way they did at the time.
/// </para>
/// <para>
/// The daily candle carries QUOTE volume, the same unit as the ticker value and the threshold -
/// verified on Binance Perpetual (BTCUSDT 18.2 billion, ETHUSDT 14.7 billion on 24-08-2026, and
/// LAUSDT 3.8 million against 3.1 million from the ticker). A base volume would have made the
/// comparison meaningless.
/// </para>
/// </summary>
public static class EmulatorVolume
{
    private static int symbolsWithoutDailyVolume;

    /// <summary>Number of symbols whose warmup held no daily candle to read a volume from.</summary>
    public static int SymbolsWithoutDailyVolume => Volatile.Read(ref symbolsWithoutDailyVolume);

    public static void ResetDiagnostics() => Interlocked.Exchange(ref symbolsWithoutDailyVolume, 0);

    /// <summary>
    /// Applies a closed daily candle as the symbol's 24 hour volume. Called for every daily candle
    /// the replay hands over, so the value follows the run.
    /// </summary>
    public static void ApplyDailyVolume(CryptoSymbol symbol, CryptoCandle candle)
        => symbol.Volume = (double)candle.Volume;

    /// <summary>
    /// Seeds the volume from the last daily candle before the replay window, so the first replayed
    /// day is already judged on its own liquidity rather than on today's.
    /// <para>
    /// A symbol without any daily candle in its warmup gets zero, not the value from the last fetch.
    /// Zero is what we actually know about it, and it keeps the symbol out until a daily candle
    /// arrives during the run. In practice such a symbol is already blocked by
    /// <c>SymbolTools.CheckNewCoin</c>, which wants <c>Signal.SymbolMustExistsDays</c> daily candles
    /// before it will trade at all.
    /// </para>
    /// </summary>
    public static void SeedFromWarmup(CryptoSymbol symbol)
    {
        CryptoSymbolInterval daily = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1d);

        CryptoCandle? newest = null;
        foreach (CryptoCandle candle in daily.CandleList.Values)
        {
            if (newest == null || candle.OpenTime > newest.Value.OpenTime)
                newest = candle;
        }

        if (newest == null)
        {
            symbol.Volume = 0;
            Interlocked.Increment(ref symbolsWithoutDailyVolume);
            return;
        }

        ApplyDailyVolume(symbol, newest.Value);
    }
}
