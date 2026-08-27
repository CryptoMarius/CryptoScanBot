using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using System.Collections.ObjectModel;

namespace CryptoScanner.ViewModels;

/// <summary>
/// One editable row in the paper-assets window. Only Total can be changed - Locked is derived from
/// the orders that are open right now and Free follows from the two, so both are shown as text.
/// </summary>
public partial class AssetRowViewModel(CryptoAsset asset) : ObservableObject
{
    public string Name { get; } = asset.Name;

    [ObservableProperty]
    private decimal _total = asset.Total;

    /// <summary>
    /// Precomputed text on purpose: a StringFormat on a DataGridTextColumn binding over a decimal
    /// throws per cell and floods the log while scrolling.
    /// </summary>
    public string LockedText { get; } = asset.Locked.ToString0();

    public string FreeText { get; } = asset.Free.ToString0();

    /// <summary>The amount this row started with, so Apply only writes what the user really changed.</summary>
    public decimal OriginalTotal { get; } = asset.Total;
}


public partial class AssetWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<AssetRowViewModel> _assets = [];

    [ObservableProperty]
    private decimal _startCapital = GlobalData.Settings.Trading.PaperAssetStartCapital;

    [ObservableProperty]
    private string _summary = "";

    public AssetWindowViewModel()
    {
        Reload();
    }

    public void Reload()
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
        {
            Summary = "No active exchange";
            return;
        }

        // Refresh the reservations first, so what the window shows matches the open orders.
        PaperAssets.RefreshLocked(exchange);

        exchange.Data.AssetListSemaphore.Wait();
        try
        {
            // OrderBy: a ConcurrentDictionary has no guaranteed order, sort for a stable list.
            Assets = [.. exchange.Data.AssetList.Values.OrderBy(a => a.Name).Select(a => new AssetRowViewModel(a))];
        }
        finally
        {
            exchange.Data.AssetListSemaphore.Release();
        }

        Summary = $"{Assets.Count} asset(s) on {exchange.Name}";
    }

    /// <summary>Write back the rows the user actually changed.</summary>
    public void Apply()
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
            return;

        int changed = 0;
        foreach (AssetRowViewModel row in Assets)
        {
            if (row.Total == row.OriginalTotal)
                continue;
            PaperAssets.SetAsset(exchange, row.Name, row.Total);
            GlobalData.AddTextToLogTab($"Paper asset {row.Name} changed from {row.OriginalTotal.ToString0()} to {row.Total.ToString0()}");
            changed++;
        }

        if (changed == 0)
            GlobalData.AddTextToLogTab("Paper assets unchanged");
        Reload();
    }

    /// <summary>Throw everything away and hand out the start capital again.</summary>
    public void Reset()
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
            return;

        PaperAssets.ResetAssets(exchange, StartCapital);
        GlobalData.AddTextToLogTab($"Paper assets reset to {StartCapital.ToString0()} per traded quote coin");
        Reload();
    }
}
