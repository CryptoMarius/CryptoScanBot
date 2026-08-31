using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

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
    /// The three amounts the way both screens show them, so the Avalonia window and the Photino
    /// dialog round identically - see <see cref="PaperAssetsEditor.FormatAmount"/>.
    /// </summary>
    public string TotalText => PaperAssetsEditor.FormatAmount(Name, Total);

    public string LockedText => PaperAssetsEditor.FormatAmount(Name, Locked);

    public string FreeText => PaperAssetsEditor.FormatAmount(Name, Free);
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

        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            // OrderBy: a ConcurrentDictionary has no guaranteed order, sort for a stable list.
            return [.. activeExchange.Data.AssetList.Values.OrderBy(a => a.Name).Select(a => new PaperAssetRow
            {
                Name = a.Name,
                Total = a.Total,
                Locked = a.Locked,
                Free = a.Free,
            })];
        }
        finally
        {
            activeExchange.Data.AssetListSemaphore.Release();
        }
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
        foreach (PaperAssetEdit edit in edits)
        {
            if (edit.NewTotal == edit.OriginalTotal)
                continue;
            PaperAssets.SetAsset(activeExchange, edit.Name, edit.NewTotal);
            GlobalData.AddTextToLogTab($"Paper asset {edit.Name} changed from {edit.OriginalTotal.ToString0()} to {edit.NewTotal.ToString0()}");
            changed++;
        }

        if (changed == 0)
            GlobalData.AddTextToLogTab("Paper assets unchanged");
        else
            CaptureToday(activeExchange);
        return changed;
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
    public static bool Add(Model.CryptoExchange? activeExchange, string? name, decimal total)
    {
        if (activeExchange == null)
            return false;

        // Coins are administered in capitals everywhere else (the exchange delivers them that way),
        // so "btc" typed in the screen has to become BTC or it lands next to the real balance.
        name = name?.Trim().ToUpperInvariant() ?? "";
        if (name.Length == 0 || total <= 0)
        {
            GlobalData.AddTextToLogTab("Paper asset not booked: fill in a coin and an amount above zero");
            return false;
        }

        PaperAssets.SetAsset(activeExchange, name, total);
        GlobalData.AddTextToLogTab($"Paper asset {name} booked at {FormatAmount(name, total)}");
        CaptureToday(activeExchange);
        return true;
    }


    /// <summary>Throw everything away and hand out the start capital again.</summary>
    public static void Reset(Model.CryptoExchange? activeExchange, decimal startCapital)
    {
        if (activeExchange == null)
            return;

        PaperAssets.ResetAssets(activeExchange, startCapital);
        // Which of the two roads was taken is worth reading back afterwards, so say it here.
        int defaults = GlobalData.Settings.Trading.PaperAssetDefaults.Count;
        if (defaults > 0)
            GlobalData.AddTextToLogTab($"Paper assets reset to the {defaults} default asset(s)");
        else
            GlobalData.AddTextToLogTab($"Paper assets reset to {startCapital.ToString0()} per traded quote coin, " +
                "except the ones with a start capital of their own");
        CaptureToday(activeExchange);
    }


    /// <summary>
    /// Hand out the start capital again after every position was deleted from the database, and
    /// report whether that actually happened.
    /// <para>
    /// The two belong together: the balances carry the result of those positions, so deleting them
    /// while the money stays behind leaves a balance that no trade explains any more - a session that
    /// lost 2.000 and then cleared its positions would keep trading with 8.000 and call it the start.
    /// The amount is the configured start capital; the paper-assets screen is the place to hand out a
    /// different one.
    /// </para>
    /// <para>
    /// Does nothing when the balances are not ours to hand out: with real trading or Altrady the money
    /// is at the exchange, and seeding paper balances there would invent money that does not exist.
    /// </para>
    /// </summary>
    public static bool ResetAfterDeletingAllPositions(Model.CryptoExchange? activeExchange)
    {
        if (activeExchange == null)
            return false;

        if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading ||
            GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.Altrady)
            return false;

        Reset(activeExchange, GlobalData.Settings.Trading.PaperAssetStartCapital);
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
