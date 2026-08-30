using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

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
        return changed;
    }


    /// <summary>Throw everything away and hand out the start capital again.</summary>
    public static void Reset(Model.CryptoExchange? activeExchange, decimal startCapital)
    {
        if (activeExchange == null)
            return;

        PaperAssets.ResetAssets(activeExchange, startCapital);
        GlobalData.AddTextToLogTab($"Paper assets reset to {startCapital.ToString0()} per traded quote coin");
    }
}
