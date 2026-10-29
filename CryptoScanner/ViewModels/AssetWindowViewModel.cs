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
    [NotifyPropertyChangedFor(nameof(TotalText))]
    private decimal _total = asset.Total;

    /// <summary>
    /// The editable amount as text, so the cell rounds the same way as the two columns next to it -
    /// a decimal straight into the grid prints every decimal a fill left on a USDT balance.
    /// <para>
    /// Text that is already what the cell shows changes nothing: the grid commits a cell even when
    /// the user only clicked into it, and parsing a rounded amount back would silently correct the
    /// balance by the decimals that were rounded away.
    /// </para>
    /// </summary>
    public string TotalText
    {
        get => PaperAssetsEditor.FormatAmount(Name, Total);
        set
        {
            if (value != TotalText && PaperAssetsEditor.TryParseAmount(value, out decimal parsed))
                Total = parsed;
            else
                OnPropertyChanged(); // unreadable, so put the amount back the way it was
        }
    }

    /// <summary>
    /// Precomputed text on purpose: a StringFormat on a DataGridTextColumn binding over a decimal
    /// throws per cell and floods the log while scrolling.
    /// </summary>
    public string LockedText { get; } = asset.LockedText;

    public string FreeText { get; } = asset.FreeText;

    /// <summary>The amount this row started with, so Apply only writes what the user really changed.</summary>
    public decimal OriginalTotal { get; } = asset.Total;

    /// <summary>What a reset hands out for this coin, editable like Total and saved in the settings.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DefaultText))]
    private decimal _default = asset.Default;

    /// <summary>The default as text, read back the same careful way as <see cref="TotalText"/>.</summary>
    public string DefaultText
    {
        get => PaperAssetsEditor.FormatAmount(Name, Default);
        set
        {
            if (value != DefaultText && PaperAssetsEditor.TryParseAmount(value, out decimal parsed) && parsed >= 0)
                Default = parsed;
            else
                OnPropertyChanged(); // unreadable, so put the amount back the way it was
        }
    }

    public decimal OriginalDefault { get; } = asset.Default;
}


public partial class AssetWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<AssetRowViewModel> _assets = [];

    [ObservableProperty]
    private string _summary = "";

    /// <summary>The coin and the amount of the add row, as typed - both are read as text.</summary>
    [ObservableProperty]
    private string _newAssetName = "";

    [ObservableProperty]
    private string _newAssetAmount = "";

    [ObservableProperty]
    private string _newAssetDefault = "";

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
            OriginalDefault = row.OriginalDefault,
            NewDefault = row.Default,
        }));
        Reload();
    }

    /// <summary>
    /// Book the balance of the add row. Nothing happens when the coin or the amount cannot be used;
    /// the log tab says why, and what was typed stays where it is so it can be corrected.
    /// </summary>
    public void Add()
    {
        PaperAssetsEditor.TryParseAmount(NewAssetAmount, out decimal amount);
        PaperAssetsEditor.TryParseAmount(NewAssetDefault, out decimal defaultAmount);
        if (!PaperAssetsEditor.Add(GlobalData.ActiveExchange, NewAssetName, amount, defaultAmount))
            return;

        NewAssetName = "";
        NewAssetAmount = "";
        NewAssetDefault = "";
        Reload();
    }

    /// <summary>Take one coin out of the paper account.</summary>
    public void Remove(AssetRowViewModel row)
    {
        if (PaperAssetsEditor.Remove(GlobalData.ActiveExchange, row.Name))
            Reload();
    }

    /// <summary>What a reset hands out, for the confirmation.</summary>
    public static string DescribeReset() => PaperAssetsEditor.DescribeSavedDefaults();

    /// <summary>Throw everything away and hand out the default assets of the settings again.</summary>
    public void Reset()
    {
        PaperAssetsEditor.ResetToSavedDefaults(GlobalData.ActiveExchange);
        Reload();
    }
}
