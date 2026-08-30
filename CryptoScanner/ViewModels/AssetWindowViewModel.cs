using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Trader;

using System.Collections.ObjectModel;

namespace CryptoScanner.ViewModels;

/// <summary>
/// One editable row in the paper-assets window. Only Total can be changed - Locked is derived from
/// the orders that are open right now and Free follows from the two, so both are shown as text.
/// </summary>
public partial class AssetRowViewModel(PaperAssetRow asset) : ObservableObject
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
        // The loading, the corrections and the reset all live in PaperAssetsEditor, so this window
        // and the Photino assets dialog behave identically.
        var exchange = GlobalData.ActiveExchange;
        Assets = [.. PaperAssetsEditor.LoadRows(exchange).Select(row => new AssetRowViewModel(row))];
        Summary = PaperAssetsEditor.Describe(exchange, Assets.Count);
    }

    /// <summary>Write back the rows the user actually changed.</summary>
    public void Apply()
    {
        PaperAssetsEditor.Apply(GlobalData.ActiveExchange, Assets.Select(row => new PaperAssetEdit
        {
            Name = row.Name,
            OriginalTotal = row.OriginalTotal,
            NewTotal = row.Total,
        }));
        Reload();
    }

    /// <summary>Throw everything away and hand out the start capital again.</summary>
    public void Reset()
    {
        PaperAssetsEditor.Reset(GlobalData.ActiveExchange, StartCapital);
        Reload();
    }
}
