using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Emulator.Engine;

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>
/// One selectable symbol in the run-parameters dialog: the name, its current 24h volume, plus a
/// checkbox state. Raises change notification on <see cref="IsSelected"/> so the parent VM can keep
/// the "N selected" counter live.
/// </summary>
public partial class SymbolSelectionItem : ObservableObject
{
    public string Name { get; }

    // 24h quote volume, as already tracked on CryptoSymbol.Volume. 0 for a symbol that came only from
    // a saved run config and isn't (yet) known on the active exchange (e.g. Fetch symbols not run).
    public double Volume { get; }
    public string VolumeText => Volume.ToString("N0");

    [ObservableProperty]
    private bool _isSelected;

    public SymbolSelectionItem(string name, bool isSelected, double volume = 0)
    {
        Name = name;
        Volume = volume;
        _isSelected = isSelected;
    }
}


/// <summary>
/// Backs the run-parameters dialog (<c>RunConfigWindow</c>) that replaces the old "Open run.json"
/// button. Edits the same CryptoScanBot-Emulator.json the engine reads — label, replay period and symbol
/// selection — without the user hand-editing JSON. Strategies/intervals/indicators still come
/// from the scanner settings (Configure dialog); this only covers the per-run knobs.
/// </summary>
public partial class RunConfigViewModel : ObservableObject
{
    [ObservableProperty]
    private string _label = "";

    [ObservableProperty]
    private DateTime? _fromDate;

    [ObservableProperty]
    private DateTime? _toDate;

    public List<string> BaseIntervals { get; } = GlobalData.IntervalListPeriodName.Values
        .OrderBy(i => i.Duration).Select(i => i.Name).ToList();

    [ObservableProperty]
    private string _selectedBaseInterval = "1m";

    /// <summary>Live text filter over the symbol list (substring, case-insensitive).</summary>
    [ObservableProperty]
    private string _symbolFilter = "";

    /// <summary>When on, the list shows ONLY the currently selected (active) symbols — so a large
    /// selection is easy to review and switch off without hunting through the whole exchange list.</summary>
    [ObservableProperty]
    private bool _showOnlySelected;

    [ObservableProperty]
    private string _selectedSummary = "";

    [ObservableProperty]
    private string _validationMessage = "";

    /// <summary>The full symbol set; <see cref="FilteredSymbols"/> is the filtered view shown.</summary>
    private readonly List<SymbolSelectionItem> _allSymbols = [];

    [ObservableProperty]
    private ObservableCollection<SymbolSelectionItem> _filteredSymbols = [];


    public RunConfigViewModel()
    {
        EmulatorRunConfig config = RunConfigFile.Load();

        Label = config.Label;
        FromDate = config.FromDate == default ? DateTime.UtcNow.Date.AddDays(-7) : config.FromDate;
        ToDate = config.ToDate == default ? DateTime.UtcNow.Date : config.ToDate;
        SelectedBaseInterval = BaseIntervals.Contains(config.BaseInterval) ? config.BaseInterval : "1m";

        // Pre-check the symbols already in the run config. Build the full list from the active
        // exchange's known symbols; any config symbol not (yet) on the exchange is still added so
        // a saved selection is never silently dropped just because Fetch symbols wasn't run.
        var selected = new HashSet<string>(config.Symbols, StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (GlobalData.ActiveExchange != null)
        {
            foreach (var (name, symbol) in GlobalData.ActiveExchange.SymbolListName)
            {
                if (seen.Add(name))
                    AddSymbol(name, selected.Contains(name), symbol.Volume);
            }
        }
        foreach (string name in config.Symbols)
        {
            if (seen.Add(name))
                AddSymbol(name, true);
        }

        _allSymbols.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        ApplyFilter();
        UpdateSummary();
    }


    private void AddSymbol(string name, bool isSelected, double volume = 0)
    {
        var item = new SymbolSelectionItem(name, isSelected, volume);
        item.PropertyChanged += OnSymbolItemChanged;
        _allSymbols.Add(item);
    }


    private void OnSymbolItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SymbolSelectionItem.IsSelected))
            UpdateSummary();
    }


    partial void OnSymbolFilterChanged(string value) => ApplyFilter();

    partial void OnShowOnlySelectedChanged(bool value) => ApplyFilter();


    private void ApplyFilter()
    {
        IEnumerable<SymbolSelectionItem> query = _allSymbols;
        string f = SymbolFilter?.Trim() ?? "";
        if (f.Length > 0)
            query = query.Where(s => s.Name.Contains(f, StringComparison.OrdinalIgnoreCase));
        if (ShowOnlySelected)
            query = query.Where(s => s.IsSelected);
        FilteredSymbols = new ObservableCollection<SymbolSelectionItem>(query);
    }


    private void UpdateSummary()
    {
        int n = _allSymbols.Count(s => s.IsSelected);
        SelectedSummary = $"{n} symbol(s) selected";
    }


    /// <summary>Select every symbol currently visible under the filter.</summary>
    [RelayCommand]
    private void SelectAllFiltered()
    {
        foreach (var s in FilteredSymbols)
            s.IsSelected = true;
    }


    /// <summary>Deselect every symbol currently visible under the filter.</summary>
    [RelayCommand]
    private void DeselectAllFiltered()
    {
        foreach (var s in FilteredSymbols)
            s.IsSelected = false;
    }


    /// <summary>
    /// Validates the inputs and, if valid, builds the run config and reports it via
    /// <paramref name="config"/>. On failure <see cref="ValidationMessage"/> explains why and
    /// the method returns false so the dialog stays open.
    /// </summary>
    public bool TryBuild(out EmulatorRunConfig config)
    {
        config = new EmulatorRunConfig();

        var symbols = _allSymbols.Where(s => s.IsSelected).Select(s => s.Name).ToList();
        if (symbols.Count == 0)
        {
            ValidationMessage = "Select at least one symbol.";
            return false;
        }
        if (FromDate == null || ToDate == null)
        {
            ValidationMessage = "Both a from-date and a to-date are required.";
            return false;
        }
        if (ToDate.Value.Date <= FromDate.Value.Date)
        {
            ValidationMessage = "The to-date must be after the from-date.";
            return false;
        }

        ValidationMessage = "";
        config = new EmulatorRunConfig
        {
            // Always pin the config to the active exchange — the engine looks the exchange up by
            // this name, and the emulator only ever drives the one bootstrapped exchange.
            ExchangeName = GlobalData.ActiveExchange?.Name ?? "",
            Symbols = symbols,
            // The date picker hands out a DateTime of kind Unspecified. Pin it to UTC: the replay
            // window is aligned in UTC and CandleTime.AlignFromDateTime calls ToUniversalTime(),
            // which would otherwise shift the window by the local time zone offset.
            FromDate = DateTime.SpecifyKind(FromDate.Value.Date, DateTimeKind.Utc),
            ToDate = DateTime.SpecifyKind(ToDate.Value.Date, DateTimeKind.Utc),
            Label = Label ?? "",
            BaseInterval = SelectedBaseInterval ?? "1m",
        };
        return true;
    }
}
