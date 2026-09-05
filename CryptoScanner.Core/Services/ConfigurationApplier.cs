using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Services;

/// <summary>
/// Everything that has to happen after the user pressed Save in the configuration screen.
/// <para>
/// This used to live only in the Avalonia CommandShowConfiguration, so the Blazor hosts saved the
/// settings file and stopped there: the strategy/interval execution tables
/// (<c>SignalExecute.Prepare</c>), the plugin settings, the black/white lists, the timers, the
/// title and the Telegram bot were never rebuilt — new analyzer settings simply had no effect
/// until a restart.
/// </para>
/// </summary>
public static class ConfigurationApplier
{
    /// <summary>
    /// Snapshot of the settings that require a reload when they change. Take this
    /// <b>before</b> opening the configuration screen.
    /// </summary>
    public sealed class ConfigurationSnapshot
    {
        public Model.CryptoExchange? Exchange { get; init; }
        public string ExchangeName { get; init; } = "";
        public string ActiveQuotes { get; init; } = "";
    }

    public static ConfigurationSnapshot TakeSnapshot()
    {
        return new ConfigurationSnapshot
        {
            Exchange = GlobalData.ActiveExchange,
            ExchangeName = GlobalData.Settings.General.ExchangeName,
            ActiveQuotes = GetQuoteRelatedSettings(),
        };
    }

    private static string GetQuoteRelatedSettings()
    {
        string activeQuoteData = "";
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                activeQuoteData += "," + quoteData.Name;
        }

        // The products count as well: switching one off (or on) has to deactivate or bring back
        // its symbols, and that is done by the symbol refresh the reload below performs. Without
        // this the change waited for the hourly refresh, and the user saw nothing happen.
        foreach (CryptoProductData productData in GlobalData.Settings.Products.Values)
        {
            if (productData.Active)
                activeQuoteData += ";" + productData.Name;
        }
        return activeQuoteData;
    }

    /// <summary>
    /// Switches the symbols of a product the user just switched off to inactive, right away. The
    /// symbol refresh would do the same on its next cycle, but that cycle is up to an hour away and
    /// even the reload after the save takes a good ten seconds - long enough for a user to conclude
    /// that the checkbox does nothing (reported on 05-09-2026, the day the products arrived). The
    /// symbol goes the same way a delisted one goes: status 0, candles gone, out of the per quote
    /// index and out of the grids.
    /// <para>
    /// Switching a product back ON is left to the reload: whether an instrument still exists is
    /// something only the exchange can say, and the symbol refresh at startup asks it.
    /// </para>
    /// </summary>
    internal static void DeactivateSwitchedOffProducts()
    {
        Model.CryptoExchange? exchange = GlobalData.ActiveExchange;
        if (exchange == null)
            return;

        Dictionary<string, int> deactivated = [];
        foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
        {
            if (symbol.Status != 1 || symbol.IsBarometerSymbol() || symbol.Product.Length == 0)
                continue;
            if (GlobalData.IsProductActive(symbol.Product))
                continue;

            symbol.Status = 0;
            symbol.ClearCandles();
            // Persisted the way the refresh persists it, so a restart before the next refresh does
            // not bring the symbol back. Null when the session is not running; then the refresh at
            // the next start decides again, from the settings, and reaches the same answer.
            GlobalData.ThreadSaveObjects?.AddToQueue(symbol);
            deactivated[symbol.Product] = deactivated.GetValueOrDefault(symbol.Product) + 1;
        }

        if (deactivated.Count == 0)
            return;

        foreach ((string product, int count) in deactivated)
            GlobalData.AddTextToLogTab($"Product {product} switched off: {count} symbols deactivated");

        // The per quote index and both symbol grids only carry status 1; rebuilding the index sends
        // the message that makes the grids rebuild as well.
        ThreadLoadData.IndexQuoteDataSymbols(exchange);
    }


    /// <summary>
    /// Persist the settings and re-apply them to the running scanner. Call this after the user
    /// confirmed the configuration screen, passing the snapshot taken before it was opened.
    /// </summary>
    public static async Task SaveAndApplyAsync(IScannerSession scannerSession, ConfigurationSnapshot previous)
    {
        // Deliberately OUTSIDE the try below. A failed write has to reach the caller so the
        // configuration screen can stay open and say so; swallowing it here is what made a save
        // that never happened look like one that did.
        GlobalData.SaveConfiguration();

        try
        {
            // The indicator hubs freeze their parameters and their set of plugin extensions at
            // construction, so every existing hub has to be rebuilt before the new settings can
            // take effect. Bumping the version does that lazily on the next candle.
            Signal.Indicators.IndicatorConfiguration.Bump();
            // No StrategyDiagnostics.Report() here: ApplyConfigurationAsync below runs it on every
            // path, and calling it twice logged every finding in duplicate.

            // A product the user just switched off disappears right away, before the reload below
            DeactivateSwitchedOffProducts();

            // Apply the theme right away, before the (potentially slow) exchange/quote reload
            // below. Waiting until after it made a theme switch appear to take ten seconds or more.
            GlobalData.SetTheme?.Invoke(GlobalData.Settings.General.Theme ?? "Default");

            // Don't save exchange immediately, lots of data still in memory
            if (!GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange? newActiveExchange))
                return;
            if (newActiveExchange == null)
                return;

            // Did we choose another exchange (reload)
            bool exchangeChanged = previous.ExchangeName != GlobalData.Settings.General.ExchangeName;
            if (exchangeChanged)
                GlobalData.AddTextToLogTab("Exchange was changed (reload)!");

            // Did we changes quotes (reload)
            string currentActiveQuotes = GetQuoteRelatedSettings();
            bool quoteChanged = previous.ActiveQuotes != currentActiveQuotes;
            if (quoteChanged)
                GlobalData.AddTextToLogTab("Quotes have changed (reload)!");

            if (exchangeChanged || quoteChanged)
            {
                GlobalData.AddTextToLogTab("");

                await scannerSession.StopAsync();

                // Stop the current exchange
                if (previous.Exchange != null)
                {
                    if (exchangeChanged)
                    {
                        previous.Exchange?.Clear();
                        previous.Exchange?.Data.Clear();
                        // TODO: Delete symbols, assets, orders, trades, positions, parts, steps from database!
                    }

                    // Clear candle data
                    if (quoteChanged)
                    {
                        foreach (var symbol in previous.Exchange!.SymbolListId.Values)
                        {
                            if (!symbol.QuoteData.FetchCandles || symbol.Status == 0)
                            {
                                symbol.ClearCandles();
                            }
                        }
                    }
                }

                // Standaard timers e.d.
                await scannerSession.ApplyConfigurationAsync(true);

                // Schedule a reload of data
                scannerSession.ScheduleRefresh();

                // Notify subscribers that the active exchange has changed
                if (exchangeChanged)
                    GlobalData.SendMvvmMessage(new ExchangeSwitchedMessage());
            }
            else
            {
                await scannerSession.ApplyConfigurationAsync(false);
            }

            // Refresh the symbol grid so filters like MinimalPrice take effect immediately. On both
            // paths, not only on the one below: a changed quote coin takes the branch above, and that
            // one sent nothing at all unless the EXCHANGE had changed as well. Switching a quote coin
            // on or off therefore left the grid showing the symbols of the previous selection.
            GlobalData.SendMvvmMessage(new SymbolsHaveChangedMessage());

            // Reset cached strategy colors in the signal grid
            GlobalData.SendMvvmMessage(new ConfigurationChangedMessage());
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddErrorToLogTab("ERROR settings " + error.ToString());
        }
    }
}
