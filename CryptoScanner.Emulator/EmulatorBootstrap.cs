using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Emulator;

/// <summary>
/// Initial state setup that the live scanner does via ScannerSession.AfterStartup +
/// ApplyConfigurationAsync. The emulator needs the same data plumbing (DB created, exchanges
/// loaded, settings deserialized, symbols indexed, strategies prepared) so the Configure
/// dialog has something to show and the engine can find symbols. It deliberately does NOT
/// touch the live-scanner side: no websockets, no tickers, no timers, no telegram bot — those
/// are guarded elsewhere by <see cref="GlobalData.IsEmulatorMode"/>.
///
/// Mirrors ScannerSession.AfterStartup line-for-line where possible so we follow the same
/// init order; just stops before the bits that come up the network or schedule timers.
/// </summary>
public static class EmulatorBootstrap
{
    public static async Task InitializeAsync(string? exchangeOverride = null)
    {
        // ─── ScannerSession.AfterStartup equivalent ─────────────────────────────────
        Directory.CreateDirectory(GlobalData.AppDataFolder);
        CryptoDatabase.SetDatabaseDefaults();    // DB file, tables, seed exchanges
        GlobalData.LoadExchanges();              // ExchangeListName / ExchangeListId
        GlobalData.LoadIntervals();              // IntervalListPeriodName / IntervalListId
        GlobalData.LoadConfiguration();          // settings.json + telegram + altrady + weblinks
        PickupExchangeFromParameter();

        // Wire the persistence queue early so it is available from the very first signal or zone
        // operation. ApplyRunOverrides also does ??= but that runs only once the user starts a run;
        // code that fires earlier (e.g. zone calculation triggered during candle load) would see null
        // and silently swallow the NullReferenceException inside the per-signal try/catch.
        GlobalData.ThreadSaveObjects ??= new ThreadSaveObjects();

        // How much 1m history the engine reserves (GetCandleFetchStart). The live scanner starts at
        // a day plus the barometer graph hours and lowers it to this once the barometer has been
        // calculated (BarometerTools). The emulator draws no barometer GRAPH - the extra hours exist
        // only to fill that graph after a start - so it would keep fetching them for nothing: set the
        // lowered value straight away. A day plus a few candles is what the 24-hour change
        // calculation needs, and also what the 1d barometer of a replay reaches back for
        // (BarometerReplay, since 31-08-2026; IndicatorWarmup.WarmupDepth keeps the same depth).
        CandleTools.SetInitialCandleCountFetch(24 * 60 + 10);

        // Setup dialog override wins over everything (settings.json AND -e argument), because
        // the user explicitly picked this exchange for this session in the wizard.
        if (!string.IsNullOrEmpty(exchangeOverride))
            GlobalData.Settings.General.ExchangeName = exchangeOverride;

        // ─── ScannerSession.ApplyConfigurationAsync subset ──────────────────────────
        // Bind the active exchange the user/settings selected.
        string exchangeName = GlobalData.Settings.General.ExchangeName ?? "";
        if (!string.IsNullOrEmpty(exchangeName)
            && GlobalData.ExchangeListName.TryGetValue(exchangeName, out var activeExchange))
        {
            GlobalData.ActiveExchange = activeExchange;
            activeExchange.GetApiInstance().ExchangeDefaults();
            GlobalData.LoadSymbols();

            // Wire symbols into their quote-side index. The Configure dialog reads
            // QuoteData.SymbolList to show per-quote symbol counts; without this call the
            // counts stay at 0 even when the Symbol table is full. Same call ThreadLoadData
            // makes after a Symbol.GetSymbolsAsync.
            ThreadLoadData.IndexQuoteDataSymbols(activeExchange);

            // Make sure the per-exchange candles DB exists with the Candle table created.
            // The live scanner does this lazily on first read/write path; the emulator's
            // Fetch button tries to save into Candle straight away — without InitializeSchema
            // the user gets a SQLite "no such table: Candle" error on the first save.
            CandleDatabase.InitializeSchema(activeExchange);

            // Load already-persisted candles into memory. CandleDatabase.LoadCandlesAsync
            // also primes symbolInterval.LastCandleSynchronized — without this null value
            // GetCandlesForAllIntervalsAsync has no idea where to resume from, so the REST
            // fetch silently does nothing (which was the "no progress, no DB writes" symptom).
            // DataStore.LoadCandlesAsync is only relevant for the legacy file-based store; it
            // is a no-op when there is nothing to migrate but kept here to mirror the live
            // scanner's load order exactly.
            await DataStore.LoadCandlesAsync();
            await CandleDatabase.LoadCandlesAsync();

            GlobalData.AddTextToLogTab($"Active exchange: {activeExchange.Name} ({activeExchange.SymbolListName.Count} symbols loaded)");
        }

        // Restore the analyzer plugin settings from the AnalyzerSettings, JSON blocks that
        // LoadConfiguration just deserialized — same call and same order as
        // ScannerSession.ApplyConfigurationAsync. Without this the plugins keep their 
        // defaults and IndexStrategySettings below indexes those defaults.
        PluginManager.RestoreSettings(GlobalData.Settings.Signal.AnalyzerSettings);

        // Build the per-strategy index, white/blacklist lookup, indicator/signal preparation —
        // exact same calls as the live ApplyConfigurationAsync. Cheap, no side effects.
        GlobalData.IndexStrategySettings();
        TradingConfig.IndexStrategyInternally();
        TradingConfig.InitWhiteAndBlackListSettings();

        BarometerTools.InitBarometerSymbols();

        SignalPrepare.Prepare();
        SignalExecute.Prepare();

        // The same report the live scanner runs after these two (ScannerSession), and it was the one
        // call the emulator did not copy. Without it a strategy that can never signal - a name the
        // settings still carry after a rename, a plugin that is not in this build - produces a run
        // that finishes as "completed" with zero signals and says nothing about why. That cost a
        // whole afternoon on 28-08-2026: ten runs in a row with indicators 0.0s and no line anywhere
        // naming the cause. Silent when everything is in order.
        CryptoScanner.Core.Signal.Indicators.StrategyDiagnostics.Report();
    }


    /// <summary>
    /// Honours <c>-e ExchangeName</c> on the command line, exactly like
    /// ScannerSession.PickupExchangeFromParameter does. Useful when the user wants to start
    /// the emulator targeting an exchange other than the one in settings.json.
    /// </summary>
    private static void PickupExchangeFromParameter()
    {
        string? exchangeName = ApplicationParams.Options?.ExchangeName;
        if (string.IsNullOrEmpty(exchangeName))
            return;

        string trimmed = exchangeName.Trim();
        var match = GlobalData.ExchangeListName.Values
            .FirstOrDefault(x => x.Name.Equals(trimmed, StringComparison.CurrentCultureIgnoreCase));

        // Same refusal as ScannerSession.PickupExchangeFromParameter. Quietly ignoring an unknown
        // name meant the emulator ran against whatever stood in the settings while the user believed
        // they had picked another exchange - a run that looks fine and answers the wrong question.
        if (match == null)
        {
            string known = string.Join(", ", GlobalData.ExchangeListName.Values.Select(x => x.Name).Order());
            throw new Exception($"Exchange \"{trimmed}\" (parameter -e) does not exist. Known exchanges: {known}");
        }

        GlobalData.Settings.General.ExchangeName = match.Name;
    }
}
