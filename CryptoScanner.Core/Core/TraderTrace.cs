using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Core;

/// <summary>
/// Opt-in diagnostic logging that ties a signal's trigger candle to the actual entry candle, so an
/// "entry one candle too late" hunch can be proven or disproven objectively (instead of by eyeballing
/// the chart). Controlled by <see cref="Settings.SettingsGeneral.DebugSignalTiming"/> on the Debug tab,
/// and — like the other debug flags — narrowed to one symbol via DebugSymbol when that is filled in.
///
/// Logged at Info level on purpose: the per-run emulator log (StartRunLog) only captures Info and up,
/// and that per-run file is exactly where you want the timing trace when analysing one backtest run.
/// NLog Trace level would only reach a separate "*Trace.log" and only in DEBUG builds.
///
/// Read the lines as: for a given interval the entry is "on time" when the entry candle is the trigger
/// candle's CLOSE (= trigger.open + 1 × interval); a delta of 2 × interval means one candle too late.
/// </summary>
public static class TraderTrace
{
    /// <summary>True when signal-timing tracing is enabled and (optionally) matches the debug symbol.</summary>
    public static bool TimingEnabled(CryptoSymbol symbol)
    {
        var general = GlobalData.Settings.General;
        if (!general.DebugSignalTiming)
            return false;
        return string.IsNullOrEmpty(general.DebugSymbol) || general.DebugSymbol == symbol.Name;
    }

    /// <summary>Writes one [SIGNAL-TIMING] line (gated by <see cref="TimingEnabled"/>).</summary>
    public static void Timing(CryptoSymbol symbol, string message)
    {
        if (!TimingEnabled(symbol))
            return;
        ScannerLog.Logger.Info("[SIGNAL-TIMING] " + message);
    }
}
