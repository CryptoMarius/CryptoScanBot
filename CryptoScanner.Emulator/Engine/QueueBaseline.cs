using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// The fixed starting point of every queue entry: the signal and trading settings, the analyzer
/// (plugin) settings and the entry amount of the quote coins are put on their defaults from the code
/// before the entry lays its own keys over them, and the settings of the session are put back when
/// the entry is done.
/// <para>
/// Until 09-11-2026 an entry started from the settings of the session (the settings file of the data
/// folder). Everything the entry did not spell out came from there, and that file is also where the
/// small manual tests are done. A test setting left on therefore went along into every queue run
/// without a word: the short side of runs 174-184 of the HyperLiquid 2k series ran with a market RSI
/// cross of 75 nobody had asked for. Now what an entry does not say is the default, the same for every
/// run, whatever the settings file holds at that moment.
/// </para>
/// <para>
/// Left alone, because they are not strategy settings: General (exchange, folders), the trend
/// settings, the products and white/black lists, the other fields of the quote coins (they are linked
/// to the symbols), and the default paper assets of the exchange, which the database holds per
/// exchange and an entry sets through TradingOverrides anyway.
/// </para>
/// </summary>
public static class QueueBaseline
{
    private sealed class Saved
    {
        public required SettingsSignal Signal { get; init; }
        public required SettingsTrading Trading { get; init; }
        public required List<(IStrategyPlugin Plugin, SettingsSignalStrategyBase Settings)> Plugins { get; init; }
        public required List<(CryptoQuoteData Quote, decimal EntryAmount, float EntryPercentage)> Quotes { get; init; }
    }

    private static Saved? _saved;

    /// <summary>Whether the settings in memory are the defaults of a queue entry right now.</summary>
    public static bool IsActive => _saved != null;


    /// <summary>
    /// Puts the defaults in place of the session settings. The session objects themselves are kept
    /// aside untouched - nothing is copied into them - so <see cref="Restore"/> only has to put them
    /// back. Calling it while already active first restores, so the session settings can never be
    /// lost by a missed <see cref="Restore"/>.
    /// </summary>
    public static void Install()
    {
        Restore();

        SettingsBasic settings = GlobalData.Settings;
        Saved saved = new()
        {
            Signal = settings.Signal,
            Trading = settings.Trading,
            Plugins = [.. PluginManager.LoadedPlugins.Values.Distinct().Select(p => (p, p.SettingsBase))],
            Quotes = [.. settings.QuoteCoins.Values.Select(q => (q, q.EntryAmount, q.EntryPercentage))],
        };

        SettingsTrading trading = new()
        {
            // The one default the emulator does not take over: "the bot is active" is off in the code
            // (a fresh scanner does not trade), and a queue run without it opens no position at all.
            Active = true,
            // Data of the exchange (database, per exchange), not a choice of the session
            PaperAssetDefaults = [.. saved.Trading.PaperAssetDefaults],
        };
        settings.Signal = new SettingsSignal();
        settings.Trading = trading;

        foreach (var (plugin, current) in saved.Plugins)
        {
            if (Activator.CreateInstance(current.GetType()) is SettingsSignalStrategyBase fresh)
                plugin.SettingsBase = fresh;
        }

        CryptoQuoteData defaultQuote = new() { Name = "" };
        foreach (var (quote, _, _) in saved.Quotes)
        {
            quote.EntryAmount = defaultQuote.EntryAmount;
            quote.EntryPercentage = defaultQuote.EntryPercentage;
        }

        _saved = saved;
    }


    /// <summary>Puts the session settings back; does nothing when no defaults are in place.</summary>
    public static void Restore()
    {
        Saved? saved = _saved;
        if (saved == null)
            return;

        GlobalData.Settings.Signal = saved.Signal;
        GlobalData.Settings.Trading = saved.Trading;
        foreach (var (plugin, settings) in saved.Plugins)
            plugin.SettingsBase = settings;
        foreach (var (quote, entryAmount, entryPercentage) in saved.Quotes)
        {
            quote.EntryAmount = entryAmount;
            quote.EntryPercentage = entryPercentage;
        }

        _saved = null;
    }
}
