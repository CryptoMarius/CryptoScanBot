using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.Engine;

using System.Collections.ObjectModel;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>One coin and the amount a run starts with, as an editable row.</summary>
public partial class PaperAssetDefaultRow : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private decimal? _total;
}


/// <summary>
/// Backs the Assets dialog of the run parameters: the Default column of Paper assets
/// (Settings.Trading.PaperAssetDefaults), which is the money every emulator run starts with. The
/// scanner edits the same list in Tools -> Paper assets, a screen the emulator does not have, so
/// without this dialog a run could only start with whatever the last queue entry left behind.
/// <para>
/// Works on a copy: the caller takes <see cref="Result"/> over on OK, and the run parameters only
/// write it to the settings on their own OK.
/// </para>
/// </summary>
public partial class PaperAssetDefaultsViewModel : ObservableObject
{
    public ObservableCollection<PaperAssetDefaultRow> Rows { get; } = [];

    [ObservableProperty]
    private PaperAssetDefaultRow? _selectedRow;

    public PaperAssetDefaultsViewModel(IEnumerable<CryptoPaperAssetDefault> defaults)
    {
        foreach (CryptoPaperAssetDefault entry in defaults)
            Rows.Add(new PaperAssetDefaultRow { Name = entry.Name, Total = entry.Total });
    }


    [RelayCommand]
    private void AddRow()
    {
        PaperAssetDefaultRow row = new();
        Rows.Add(row);
        SelectedRow = row;
    }


    [RelayCommand]
    private void RemoveRow(PaperAssetDefaultRow? row)
    {
        row ??= SelectedRow;
        if (row != null)
            Rows.Remove(row);
    }


    /// <summary>The list as it was left, normalised the way the reset reads it (see EmulatorStartCapital).</summary>
    public List<CryptoPaperAssetDefault> Result => EmulatorStartCapital.Normalise(Rows
        .Select(r => new CryptoPaperAssetDefault { Name = r.Name ?? "", Total = r.Total ?? 0m }));
}
