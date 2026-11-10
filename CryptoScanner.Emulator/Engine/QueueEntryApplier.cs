using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Lays one queue entry over the settings in memory: stop, take profit and DCA ladder, the per-side
/// configuration, and the signal and trading overrides. Taken out of the queue loop of the main window
/// on 09-11-2026 so the conversion of the open queue files and the tests run the very same steps as a
/// real run.
/// </summary>
public static class QueueEntryApplier
{
    /// <summary>
    /// Applies <paramref name="entry"/>. Since 09-11-2026 the queue first puts the defaults in place
    /// (<see cref="QueueBaseline"/>), so "the settings decide" below means: the default decides.
    /// Returns the overrides for <see cref="SignalGridExpander.Revert"/>; the other keys are written
    /// over the settings objects of the baseline, which are thrown away after the entry.
    /// </summary>
    public static List<SignalGridExpander.Override> Apply(EmulatorQueueEntry entry)
    {
        // Omitted = the settings decide, like every other key of an entry (31-10-2026).
        // Until then a missing StopLossPercentage ran on a default of 2%, and the limit
        // was always rebuilt from it, even when the settings had another distance.
        decimal? effectiveSL = entry.Trading?.StopLossPercentage ?? entry.StopLossPercentage;
        if (effectiveSL.HasValue)
        {
            GlobalData.Settings.Trading.StopLossPercentage = effectiveSL.Value;
            GlobalData.Settings.Trading.StopLossLimitPercentage = effectiveSL.Value + 1m;
        }

        // An empty TP list is treated as "the settings decide" too: a run without a
        // take profit has no way out but the stop, nobody asks for that by leaving a
        // list empty.
        List<CryptoTpEntry>? effectiveTP = entry.Trading?.TpList ?? entry.TpList;
        if (effectiveTP is { Count: > 0 })
            GlobalData.Settings.Trading.TpList = effectiveTP
                .Select(e => new CryptoTpEntry { Factor = e.Factor, Percentage = e.Percentage })
                .ToList();

        // The DCA list is the one key where an EMPTY list means something: no DCA at
        // all (the queue files rely on "DcaList": []). Omitted = the settings decide.
        List<CryptoDcaEntry>? effectiveDCA = entry.Trading?.DcaList ?? entry.DcaList;
        if (effectiveDCA != null)
            GlobalData.Settings.Trading.DcaList = effectiveDCA
                .Select(e => new CryptoDcaEntry { Factor = e.Factor, Percentage = e.Percentage })
                .ToList();

        bool hasPerSideConfig = entry.Long != null || entry.Short != null || entry.MirrorFrom != null;

        if (hasPerSideConfig)
        {
            EmulatorSideConfig? mirrorForLong = "Short".Equals(entry.MirrorFrom, StringComparison.OrdinalIgnoreCase) ? entry.Short : null;
            EmulatorSideConfig? mirrorForShort = "Long".Equals(entry.MirrorFrom, StringComparison.OrdinalIgnoreCase) ? entry.Long : null;
            EmulatorSideConfig longConfig = EmulatorSideConfig.Resolve(entry.Long, mirrorForLong);
            EmulatorSideConfig shortConfig = EmulatorSideConfig.Resolve(entry.Short, mirrorForShort);

            EmulatorSideConfig.Apply(longConfig, GlobalData.Settings.Signal.Long, GlobalData.Settings.Trading.Long);
            EmulatorSideConfig.Apply(shortConfig, GlobalData.Settings.Signal.Short, GlobalData.Settings.Trading.Short);
        }
        else
        {
            if (entry.Intervals is { Count: > 0 })
            {
                GlobalData.Settings.Signal.Long.Interval = new List<string>(entry.Intervals);
                GlobalData.Settings.Signal.Short.Interval = new List<string>(entry.Intervals);
            }

            if ("Short".Equals(entry.Side, StringComparison.OrdinalIgnoreCase))
            {
                GlobalData.Settings.Signal.Long.Strategy = [];
                GlobalData.Settings.Trading.Long.Strategy = [];
            }
            else if ("Long".Equals(entry.Side, StringComparison.OrdinalIgnoreCase))
            {
                GlobalData.Settings.Signal.Short.Strategy = [];
                GlobalData.Settings.Trading.Short.Strategy = [];
            }
        }

        List<SignalGridExpander.Override> overrides = SignalGridExpander.Apply(entry);

        // A stop given in TradingOverrides arrives AFTER the limit above was derived,
        // so "StopLossPercentage": 5 in the overrides ran with a limit of 3, on the
        // wrong side of the stop (31-10-2026). The limit follows the stop here, unless
        // the overrides set the limit themselves. Put back with the saved value in the
        // finally of this batch, like the stop.
        bool stopOverridden = overrides.Any(o => ReferenceEquals(o.Target, GlobalData.Settings.Trading)
            && o.Member.Name == nameof(SettingsTrading.StopLossPercentage));
        bool limitOverridden = overrides.Any(o => ReferenceEquals(o.Target, GlobalData.Settings.Trading)
            && o.Member.Name == nameof(SettingsTrading.StopLossLimitPercentage));
        if (stopOverridden && !limitOverridden)
            GlobalData.Settings.Trading.StopLossLimitPercentage = GlobalData.Settings.Trading.StopLossPercentage + 1m;

        return overrides;
    }
}
