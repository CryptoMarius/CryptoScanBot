using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Signal;

using System.Collections.ObjectModel;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;
}

public partial class StrategyViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<StrategyItem> _strategyList = [];

    // Cross-references to the long/short siblings on the same tab, set once by the parent
    // (AnalyzerTabViewModel / TraderTabViewModel) right after construction. Used by the
    // "Copy from..." popup so the user can mirror the strategy selection from one side to
    // the other without re-ticking dozens of checkboxes. Either may equal this instance —
    // that just makes the corresponding command a self-copy no-op (it's hidden in the UI via
    // CanExecute when the counterpart is this same viewmodel).
    public StrategyViewModel? LongCounterpart { get; set; }
    public StrategyViewModel? ShortCounterpart { get; set; }

    // Cross-tab counterparts (Analyzer ↔ Trader)
    public StrategyViewModel? CrossTabLongCounterpart { get; set; }
    public StrategyViewModel? CrossTabShortCounterpart { get; set; }

    [ObservableProperty]
    private string _crossTabLabel = "Trading";

    public StrategyViewModel()
    {
    }

    public void LoadConfig(List<string> strategyList)
    {
        StrategyList.Clear();
        // Sort alphabetically by name so the UI lists e.g. sbm1/sbm2/sbm3, stobb/stobb.dlz/...,
        // storsi/storsi.dlz/... in a predictable order independent of the registration order
        // in RegisterAlgorithms (which is grouped by topic, not name).
        var ordered = RegisterAlgorithms.AlgorithmDefinitionList.Values
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var algorithm in ordered)
        {
            var item = new StrategyItem
            {
                Name = algorithm.Name,
                IsEnabled = strategyList.Contains(algorithm.Name)
            };
            StrategyList.Add(item);
        }

        // Refresh CanExecute now that the counterparts (typically set before LoadConfig)
        // have populated strategy lists too.
        CopyFromLongCommand.NotifyCanExecuteChanged();
        CopyFromShortCommand.NotifyCanExecuteChanged();
        CopyFromCrossTabLongCommand.NotifyCanExecuteChanged();
        CopyFromCrossTabShortCommand.NotifyCanExecuteChanged();
    }

    public void SaveConfig(List<string> strategyList)
    {
        strategyList.Clear();
        foreach (var strategy in StrategyList)
        {
            if (strategy.IsEnabled)
            {
                strategyList.Add(strategy.Name);
            }
        }
    }


    // ---- Select all / none ----

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in StrategyList)
            item.IsEnabled = true;
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var item in StrategyList)
            item.IsEnabled = false;
    }


    // ---- Copy from sibling ----

    [RelayCommand(CanExecute = nameof(CanCopyFromLong))]
    private void CopyFromLong() => CopyFrom(LongCounterpart);
    private bool CanCopyFromLong() => LongCounterpart != null && !ReferenceEquals(LongCounterpart, this);

    [RelayCommand(CanExecute = nameof(CanCopyFromShort))]
    private void CopyFromShort() => CopyFrom(ShortCounterpart);
    private bool CanCopyFromShort() => ShortCounterpart != null && !ReferenceEquals(ShortCounterpart, this);


    // ---- Copy from cross-tab (Analyzer ↔ Trader) ----

    [RelayCommand(CanExecute = nameof(CanCopyFromCrossTabLong))]
    private void CopyFromCrossTabLong() => CopyFrom(CrossTabLongCounterpart);
    private bool CanCopyFromCrossTabLong() => CrossTabLongCounterpart != null;

    [RelayCommand(CanExecute = nameof(CanCopyFromCrossTabShort))]
    private void CopyFromCrossTabShort() => CopyFrom(CrossTabShortCounterpart);
    private bool CanCopyFromCrossTabShort() => CrossTabShortCounterpart != null;


    private void CopyFrom(StrategyViewModel? source)
    {
        if (source == null || ReferenceEquals(source, this))
            return;

        var enabled = new HashSet<string>(
            source.StrategyList.Where(s => s.IsEnabled).Select(s => s.Name),
            StringComparer.OrdinalIgnoreCase);

        // Only the IsEnabled flag changes — the Name list itself is identical on both sides
        // because both viewmodels are built from RegisterAlgorithms.AlgorithmDefinitionList.
        foreach (var item in StrategyList)
            item.IsEnabled = enabled.Contains(item.Name);
    }
}
