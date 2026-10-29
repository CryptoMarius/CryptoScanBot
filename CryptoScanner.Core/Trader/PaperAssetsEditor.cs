using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using System.Globalization;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// One balance the way the paper-assets screen shows it. A snapshot on purpose: the underlying
/// <see cref="CryptoAsset"/> keeps being changed by the trader while the screen is open, and the
/// screen must not show half-updated amounts.
/// </summary>
public class PaperAssetRow
{
    public string Name { get; init; } = "";
    public decimal Total { get; init; }
    public decimal Locked { get; init; }
    public decimal Free { get; init; }

    /// <summary>
    /// What a reset hands out for this coin - its entry in the default paper assets, 0 when it has
    /// none. Shown next to the balance so the start and the current amount are seen together.
    /// </summary>
    public decimal Default { get; init; }

    /// <summary>
    /// The three amounts the way both screens show them, so the Avalonia window and the Photino
    /// dialog round identically - see <see cref="PaperAssetsEditor.FormatAmount"/>.
    /// </summary>
    public string TotalText => PaperAssetsEditor.FormatAmount(Name, Total);

    public string LockedText => PaperAssetsEditor.FormatAmount(Name, Locked);

    public string FreeText => PaperAssetsEditor.FormatAmount(Name, Free);

    public string DefaultText => PaperAssetsEditor.FormatAmount(Name, Default);
}


/// <summary>
/// What one row was changed into. The amount it started with is carried along so
/// <see cref="PaperAssetsEditor.Apply"/> only writes the rows the user really touched.
/// </summary>
public class PaperAssetEdit
{
    public string Name { get; init; } = "";
    public decimal OriginalTotal { get; init; }
    public decimal NewTotal { get; init; }

    // The default amount of the coin as the row was loaded and as it was left
    public decimal OriginalDefault { get; init; }
    public decimal NewDefault { get; init; }
}


/// <summary>
/// The three actions behind the paper-assets screen: show the balances, correct one by hand, or
/// start over with the start capital.
/// <para>
/// Lives in Core because both user interfaces have this screen - the Avalonia AssetWindow and the
/// Photino AssetsDialog - and they must not drift apart. The wording of the log lines is part of
/// that: the same correction has to read the same way whichever scanner made it.
/// </para>
/// </summary>
public static class PaperAssetsEditor
{
    /// <summary>
    /// Bring the reservations up to date and return a stable, sorted snapshot of the balances.
    /// </summary>
    public static List<PaperAssetRow> LoadRows(Model.CryptoExchange? activeExchange)
    {
        if (activeExchange == null)
            return [];

        // Refresh the reservations first, so what the screen shows matches the open orders.
        PaperAssets.RefreshLocked(activeExchange);

        Dictionary<string, decimal> defaults = SavedDefaults();

        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            List<PaperAssetRow> rows = [.. activeExchange.Data.AssetList.Values.Select(a => new PaperAssetRow
            {
                Name = a.Name,
                Total = a.Total,
                Locked = a.Locked,
                Free = a.Free,
                Default = defaults.TryGetValue(a.Name, out decimal amount) ? amount : 0,
            })];

            // A coin with only a default amount gets a row as well: the reset will hand it out, so it
            // belongs in the list a reset is judged by.
            foreach (KeyValuePair<string, decimal> entry in defaults)
            {
                if (!activeExchange.Data.AssetList.ContainsKey(entry.Key))
                    rows.Add(new PaperAssetRow { Name = entry.Key, Default = entry.Value });
            }

            // OrderBy: a ConcurrentDictionary has no guaranteed order, sort for a stable list.
            return [.. rows.OrderBy(r => r.Name, StringComparer.Ordinal)];
        }
        finally
        {
            activeExchange.Data.AssetListSemaphore.Release();
        }
    }


    /// <summary>
    /// The saved default paper assets per coin, in capitals. A row without a coin or without an amount
    /// hands out nothing (see PaperAssets.SeedStartBalances), so it is left out here too.
    /// </summary>
    private static Dictionary<string, decimal> SavedDefaults()
    {
        Dictionary<string, decimal> defaults = [];
        foreach (CryptoPaperAssetDefault entry in GlobalData.Settings.Trading.PaperAssetDefaults)
        {
            if (string.IsNullOrWhiteSpace(entry.Name) || entry.Total <= 0)
                continue;
            defaults[entry.Name.Trim().ToUpperInvariant()] = entry.Total;
        }
        return defaults;
    }


    /// <summary>
    /// Writes the settings file after the default paper assets changed. A property so the tests can
    /// keep their hands off the real settings file.
    /// </summary>
    internal static Action SaveSettings { get; set; } = GlobalData.SaveConfiguration;


    /// <summary>
    /// Set the default amount of one coin in the settings; zero takes the coin out of the list. Does
    /// not write the settings file, the caller does that once for all its changes.
    /// </summary>
    private static void SetDefault(string name, decimal total)
    {
        List<CryptoPaperAssetDefault> list = GlobalData.Settings.Trading.PaperAssetDefaults;
        list.RemoveAll(d => string.Equals(d.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase));
        if (total > 0)
            list.Add(new CryptoPaperAssetDefault { Name = name, Total = total });
        list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
    }


    /// <summary>
    /// One balance as text.
    /// <para>
    /// A quote coin is shown the way the rest of the application shows amounts in that coin, through
    /// its own <see cref="CryptoQuoteData.DisplayFormat"/> - which is what rounds USDT (and the other
    /// stable coins) to two decimals instead of printing every decimal a fill left behind. Everything
    /// else in the list is a traded quantity, and those keep their decimals: 0.0108 BTC rounded to two
    /// decimals is zero.
    /// </para>
    /// </summary>
    public static string FormatAmount(string name, decimal amount)
    {
        if (GlobalData.Settings.QuoteCoins.TryGetValue(name, out CryptoQuoteData? quoteData))
            return amount.ToString(quoteData.DisplayFormat);
        return amount.ToString0();
    }


    /// <summary>
    /// Read an amount back from a screen. The text comes from <see cref="FormatAmount"/>, so it may
    /// carry the thousand separators of the current culture; the invariant notation is accepted as
    /// well because that is what a browser number field hands over.
    /// </summary>
    public static bool TryParseAmount(string? text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out amount)
            || decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
    }


    /// <summary>The line above the list, telling which exchange these balances belong to.</summary>
    public static string Describe(Model.CryptoExchange? activeExchange, int rowCount)
    {
        if (activeExchange == null)
            return "No active exchange";
        return $"{rowCount} asset(s) on {activeExchange.Name}";
    }


    /// <summary>
    /// Write back the rows the user actually changed, and return how many that were.
    /// </summary>
    public static int Apply(Model.CryptoExchange? activeExchange, IEnumerable<PaperAssetEdit> edits)
    {
        if (activeExchange == null)
            return 0;

        int changed = 0;
        int defaultsChanged = 0;
        foreach (PaperAssetEdit edit in edits)
        {
            if (edit.NewDefault != edit.OriginalDefault && edit.NewDefault >= 0)
            {
                SetDefault(edit.Name, edit.NewDefault);
                GlobalData.AddTextToLogTab($"Paper asset {edit.Name} default changed from {edit.OriginalDefault.ToString0()} to {edit.NewDefault.ToString0()}");
                defaultsChanged++;
            }

            if (edit.NewTotal == edit.OriginalTotal)
                continue;
            PaperAssets.SetAsset(activeExchange, edit.Name, edit.NewTotal);
            GlobalData.AddTextToLogTab($"Paper asset {edit.Name} changed from {edit.OriginalTotal.ToString0()} to {edit.NewTotal.ToString0()}");
            changed++;
        }

        if (defaultsChanged > 0)
            SaveSettings();

        if (changed == 0 && defaultsChanged == 0)
            GlobalData.AddTextToLogTab("Paper assets unchanged");
        else if (changed > 0)
            CaptureToday(activeExchange);
        return changed + defaultsChanged;
    }


    /// <summary>
    /// Book a balance for a coin by hand - the way to compose a starting position without throwing
    /// everything away first, and the only way to get a coin into the list that is not there yet
    /// (a balance that reaches zero is dropped, see <see cref="PaperAssets.UpdateAsset"/>).
    /// <para>
    /// A coin that IS already there is set to this amount, exactly as correcting it in the grid
    /// would. Either way it goes through <see cref="PaperAssets.SetAsset"/>, so the ledger records
    /// what the balance was and what it became and the capital line does not read it as profit.
    /// </para>
    /// <para>
    /// Returns false when the name or the amount cannot be used - nothing is booked then.
    /// </para>
    /// </summary>
    public static bool Add(Model.CryptoExchange? activeExchange, string? name, decimal total, decimal defaultTotal = 0)
    {
        if (activeExchange == null)
            return false;

        // Coins are administered in capitals everywhere else (the exchange delivers them that way),
        // so "btc" typed in the screen has to become BTC or it lands next to the real balance.
        name = name?.Trim().ToUpperInvariant() ?? "";
        // A default alone is a valid row too: the coin is not held now, but a reset hands it out
        if (name.Length == 0 || total < 0 || defaultTotal < 0 || (total == 0 && defaultTotal == 0))
        {
            GlobalData.AddTextToLogTab("Paper asset not booked: fill in a coin and an amount or a default above zero");
            return false;
        }

        if (defaultTotal > 0)
        {
            SetDefault(name, defaultTotal);
            SaveSettings();
            GlobalData.AddTextToLogTab($"Paper asset {name} default set to {FormatAmount(name, defaultTotal)}");
        }

        if (total > 0)
        {
            PaperAssets.SetAsset(activeExchange, name, total);
            GlobalData.AddTextToLogTab($"Paper asset {name} booked at {FormatAmount(name, total)}");
            CaptureToday(activeExchange);
        }
        return true;
    }


    /// <summary>
    /// Throw everything away and hand out the start capital again. <paramref name="defaultAssets"/>
    /// replaces the saved default asset list for this reset; null uses the saved one.
    /// </summary>
    public static void Reset(Model.CryptoExchange? activeExchange, decimal startCapital,
        List<CryptoPaperAssetDefault>? defaultAssets = null)
    {
        if (activeExchange == null)
            return;

        PaperAssets.ResetAssets(activeExchange, startCapital, defaultAssets);
        // Which of the two roads was taken is worth reading back afterwards, so say it here.
        int defaults = (defaultAssets ?? GlobalData.Settings.Trading.PaperAssetDefaults).Count;
        if (defaults > 0)
            GlobalData.AddTextToLogTab($"Paper assets reset to the {defaults} default asset(s)");
        else
            GlobalData.AddTextToLogTab($"Paper assets reset to {startCapital.ToString0()} per traded quote coin");
        CaptureToday(activeExchange);
    }


    /// <summary>
    /// Take one coin out of the paper account - the delete of the paper-assets screen. Booked as a
    /// manual correction to zero, so the ledger records the balance that left and the capital line
    /// does not read it as a loss.
    /// <para>
    /// A coin with an open order keeps its row: the reservation of that order still needs it, see
    /// <see cref="PaperAssets.UpdateAsset"/>. The log tab says so instead of pretending it is gone.
    /// </para>
    /// </summary>
    public static bool Remove(Model.CryptoExchange? activeExchange, string? name)
    {
        if (activeExchange == null || string.IsNullOrWhiteSpace(name))
            return false;

        // The row shows the balance and the default together, so removing it removes both - a
        // default left behind would bring the row straight back.
        if (SavedDefaults().ContainsKey(name.Trim().ToUpperInvariant()))
        {
            SetDefault(name.Trim().ToUpperInvariant(), 0);
            SaveSettings();
            GlobalData.AddTextToLogTab($"Paper asset {name} default removed");
        }

        if (!activeExchange.Data.AssetList.ContainsKey(name))
            return true;

        PaperAssets.SetAsset(activeExchange, name, 0);
        if (activeExchange.Data.AssetList.ContainsKey(name))
            GlobalData.AddTextToLogTab($"Paper asset {name} set to zero, kept in the list because an open order still reserves it");
        else
            GlobalData.AddTextToLogTab($"Paper asset {name} removed");
        CaptureToday(activeExchange);
        return true;
    }


    /// <summary>
    /// The reset of the paper-assets screen: start over with the default asset list as it is saved in
    /// the settings (trader, asset management). An empty list falls back to
    /// <see cref="PaperAssets.DefaultStartCapital"/> per traded quote coin, the same as the reset of
    /// the trader settings screen and deleting all positions.
    /// </summary>
    public static void ResetToSavedDefaults(Model.CryptoExchange? activeExchange)
    {
        Reset(activeExchange, PaperAssets.DefaultStartCapital);
    }


    /// <summary>
    /// What <see cref="ResetToSavedDefaults"/> is going to hand out, in words - for the confirmation,
    /// so the user sees the amounts before the balances are thrown away.
    /// </summary>
    public static string DescribeSavedDefaults()
    {
        List<string> parts = [.. GlobalData.Settings.Trading.PaperAssetDefaults
            .Where(d => !string.IsNullOrWhiteSpace(d.Name) && d.Total > 0)
            .Select(d =>
            {
                string name = d.Name.Trim().ToUpperInvariant();
                return $"{name} {FormatAmount(name, d.Total)}";
            })];
        if (parts.Count > 0)
            return string.Join(", ", parts);

        List<string> quotes = [.. GlobalData.Settings.QuoteCoins.Values.Where(q => q.FetchCandles).Select(q => q.Name)];
        if (quotes.Count == 0)
            return "nothing (no default assets and no traded quote coin)";
        return $"{PaperAssets.DefaultStartCapital.ToString0()} per traded quote coin ({string.Join(", ", quotes)}), " +
            "because no coin has a default under Paper assets";
    }


    /// <summary>
    /// Hand out the start capital again after every position was deleted from the database, and
    /// report whether that actually happened.
    /// <para>
    /// The two belong together: the balances carry the result of those positions, so deleting them
    /// while the money stays behind leaves a balance that no trade explains any more - a session that
    /// lost 2.000 and then cleared its positions would keep trading with 8.000 and call it the start.
    /// The amount is the default asset list from the settings; the paper-assets screen is the place
    /// to hand out a different one.
    /// </para>
    /// <para>
    /// The capital per day goes with them: that line is the course of the positions that were just
    /// deleted, and without positions there is no history to draw. The snapshots and the ledger of
    /// the live scanner are dropped, and the snapshot <see cref="Reset"/> takes afterwards is the
    /// new first point - the start capital, on today.
    /// </para>
    /// <para>
    /// Hands out nothing when the balances are not ours to hand out: with real trading or Altrady the
    /// money is at the exchange, and seeding paper balances there would invent money that does not
    /// exist. The history is dropped there as well, for the same reason as above.
    /// </para>
    /// </summary>
    public static bool ResetAfterDeletingAllPositions(Model.CryptoExchange? activeExchange)
    {
        if (activeExchange == null)
            return false;

        // Before the reset, otherwise the reset's own ledger lines (the old balance out, the start
        // capital in) would be thrown away with the rest.
        AssetSnapshotTools.DeleteLiveHistory();

        if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading ||
            GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.Altrady)
            return false;

        Reset(activeExchange, PaperAssets.DefaultStartCapital);
        return true;
    }


    /// <summary>
    /// Redo the snapshot of today, so the capital line shows the corrected balances right away.
    /// <para>
    /// Without this the change would only become visible tomorrow, and the ledger line recording it
    /// would sit on a day whose snapshot still holds the balance from before - a jump on the wrong
    /// day. Only for corrections made from this screen; the emulator takes its own snapshots.
    /// </para>
    /// </summary>
    private static void CaptureToday(Model.CryptoExchange activeExchange)
    {
        AssetSnapshotTools.Capture(activeExchange, GlobalData.Clock.UtcNow);
    }
}
