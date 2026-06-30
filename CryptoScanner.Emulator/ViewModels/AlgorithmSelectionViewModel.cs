using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Signal;

using System.Collections.ObjectModel;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>
/// One selectable algorithm in the "Run algorithms..." dialog.
/// </summary>
public partial class AlgorithmSelectionItem : ObservableObject
{
    public string Name { get; }

    [ObservableProperty]
    private bool _isSelected;

    public AlgorithmSelectionItem(string name, bool isSelected)
    {
        Name = name;
        _isSelected = isSelected;
    }
}


/// <summary>
/// Backs the "Run algorithms..." dialog: lets the user pick which registered algorithms to run
/// one by one, instead of always running the full <see cref="RegisterAlgorithms.AlgorithmDefinitionList"/>.
/// </summary>
public partial class AlgorithmSelectionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _validationMessage = "";

    public ObservableCollection<AlgorithmSelectionItem> Algorithms { get; } = [];

    public AlgorithmSelectionViewModel()
    {
        foreach (AlgorithmDefinition algorithm in RegisterAlgorithms.AlgorithmDefinitionList.Values
                     .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
            Algorithms.Add(new AlgorithmSelectionItem(algorithm.Name, isSelected: true));
    }


    [RelayCommand]
    private void SelectAll()
    {
        foreach (var a in Algorithms)
            a.IsSelected = true;
    }


    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var a in Algorithms)
            a.IsSelected = false;
    }


    /// <summary>
    /// Validates the selection and, on success, returns the selected algorithm names in the order
    /// they appear in the list.
    /// </summary>
    public bool TryGetSelection(out List<string> selectedNames)
    {
        selectedNames = Algorithms.Where(a => a.IsSelected).Select(a => a.Name).ToList();
        if (selectedNames.Count == 0)
        {
            ValidationMessage = "Select at least one algorithm.";
            return false;
        }

        ValidationMessage = "";
        return true;
    }
}
