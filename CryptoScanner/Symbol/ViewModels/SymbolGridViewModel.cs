using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Symbol.Common;
using CryptoScanner.Symbol.Model;

namespace CryptoScanner.Symbol.ViewModels
{
    public class SymbolGridViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SymbolInfo> _symbols = [];

        public ObservableCollection<SymbolInfo> Symbols
        {
            get => _symbols;
            set
            {
                _symbols = value;
                OnPropertyChanged();
            }
        }

        private readonly GridColumnDefinitions config = new();

        private bool _showIdColumn = true;
        public bool ShowIdColumn
        {
            get => _showIdColumn;
            set
            {
                _showIdColumn = value;
                config.Columns[GridColumn.Id].Visible = value;
                SaveConfigurationToSettings();
                OnPropertyChanged();
            }
        }

        private bool _showNameColumn = true;
        public bool ShowNameColumn
        {
            get => _showNameColumn;
            set
            {
                _showNameColumn = value;
                config.Columns[GridColumn.Symbol].Visible = value;
                SaveConfigurationToSettings();
                OnPropertyChanged();
            }
        }

        private bool _showVolumeColumn = true;
        public bool ShowVolumeColumn
        {
            get => _showVolumeColumn;
            set
            {
                _showVolumeColumn = value;
                config.Columns[GridColumn.Volume].Visible = value;
                SaveConfigurationToSettings();
                OnPropertyChanged();
            }
        }

        private bool _showDistanceColumn = true;
        public bool ShowDistanceColumn
        {
            get => _showDistanceColumn;
            set
            {
                _showDistanceColumn = value;
                config.Columns[GridColumn.Distance].Visible = value;
                SaveConfigurationToSettings();
                OnPropertyChanged();
            }
        }

        public SymbolGridViewModel()
        {
            System.Diagnostics.Debug.WriteLine("SymbolGridViewModel constructor called");

            config.DefaultColumnDefinition();

            // Laad symbols direct in de observable collection
            foreach (var symbol in GlobalData.ActiveExchange?.SymbolListName.Values ?? [])
            {
                Symbols.Add(new SymbolInfo
                {
                    SymbolObject = symbol,
                    Id = symbol.Id,
                    Symbol = symbol.Name,
                    Volume = symbol.Volume,
                    Distance = 0.0
                });
            }

            // Laad opgeslagen configuratie
            LoadConfigurationFromSettings();

            // Sorteer als er een sort configuratie is
            if (config.SortColumn != null)
            {
                SortSymbols();
            }
        }

        private void LoadConfigurationFromSettings()
        {
            try
            {
                var configString = Properties.Settings.Default.GridColumnConfig;
                if (!string.IsNullOrEmpty(configString))
                {
                    LoadColumnConfiguration(configString);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load grid configuration: {ex.Message}");
            }
        }

        private void SaveConfigurationToSettings()
        {
            try
            {
                var configString = SaveColumnConfiguration();
                Properties.Settings.Default.GridColumnConfig = configString;
                Properties.Settings.Default.Save();

                System.Diagnostics.Debug.WriteLine($"Config saved: {configString}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save grid configuration: {ex.Message}");
            }
        }

        public void LaunchExternal()
        {
            Process.Start("notepad.exe");
        }

        public void OnSorting(object? sender, DataGridColumnEventArgs e)
        {
            var sortMemberPath = e.Column is DataGridBoundColumn dataGridColumn
                ? dataGridColumn.SortMemberPath
                : (e.Column as DataGridTemplateColumn)?.SortMemberPath;

            System.Diagnostics.Debug.WriteLine($"Sort triggered - SortMemberPath: {sortMemberPath ?? "NULL"}");

            // Converteer SortMemberPath naar ColumnEnum enum
            if (string.IsNullOrEmpty(sortMemberPath) ||
                !Enum.TryParse<GridColumn>(sortMemberPath, true, out GridColumn gridColumn))
            {
                e.Handled = true;
                return;
            }

            GridColumnDefinition column = config.Columns[gridColumn];

            // Toggle sort direction
            if (config.SortColumn != null && config.SortColumn == column)
            {
                // Zelfde kolom, verander richting
                config.SortDirection = config.SortDirection == GridSortDirection.Ascending
                    ? GridSortDirection.Descending
                    : GridSortDirection.Ascending;
            }
            else
            {
                // Nieuwe kolom, start met Ascending
                config.SortColumn = column;
                config.SortDirection = GridSortDirection.Ascending;
            }

            System.Diagnostics.Debug.WriteLine($"Sorting by {column.Column} - {config.SortDirection}");

            // Sorteer de collectie
            SortSymbols();

            // Auto-save na sorteren
            SaveConfigurationToSettings();

            // BELANGRIJK: e.Handled = true voorkomt dat Avalonia zelf gaat sorteren
            e.Handled = true;
        }

        private void SortSymbols()
        {
            // Sorteer met je snelle bestaande Compare methode
            var sorted = Symbols.OrderBy(s => s, new SymbolComparer(config)).ToList();

            // Vervang collectie - dit is snel genoeg voor 10K items
            Symbols = new ObservableCollection<SymbolInfo>(sorted);
        }

        // Methoden voor het opslaan/laden van configuratie
        public string SaveColumnConfiguration()
        {
            var configs = config.Columns.Values
                .OrderBy(c => c.Index)
                .Select(c => $"{(int)c.Column},{c.Visible},{c.Index},{c.Width}");

            var sortConfig = config.SortColumn != null
                ? $"|{(int)config.SortColumn.Column},{(int)config.SortDirection!.Value}"
                : string.Empty;

            return string.Join(";", configs) + sortConfig;
        }

        public void LoadColumnConfiguration(string configString)
        {
            return;
            if (string.IsNullOrEmpty(configString))
                return;

            try
            {
                // Split sort info van column info
                var parts = configString.Split('|');
                var columnConfigs = parts[0];

                // Laad column configuraties
                var pairs = columnConfigs.Split(';');
                foreach (var pair in pairs)
                {
                    var items = pair.Split(',');
                    if (items.Length != 4)
                        continue;

                    var column = (GridColumn)int.Parse(items[0]);
                    var visible = bool.Parse(items[1]);
                    var index = int.Parse(items[2]);
                    var width = int.Parse(items[3]);

                    config.Columns[column].Visible = visible;
                    config.Columns[column].Index = index;
                    config.Columns[column].Width = width;
                }

                // Laad sort configuratie
                if (parts.Length > 1)
                {
                    var sortItems = parts[1].Split(',');
                    if (sortItems.Length == 2)
                    {
                        var sortColumn = (GridColumn)int.Parse(sortItems[0]);
                        var sortDirection = (GridSortDirection)int.Parse(sortItems[1]);

                        config.SortColumn = config.Columns[sortColumn];
                        config.SortDirection = sortDirection;
                    }
                }

                // Update de bool properties
                _showIdColumn = config.Columns[GridColumn.Id].Visible;
                _showNameColumn = config.Columns[GridColumn.Symbol].Visible;
                _showVolumeColumn = config.Columns[GridColumn.Volume].Visible;
                _showDistanceColumn = config.Columns[GridColumn.Distance].Visible;

                // Notify all column visibility properties
                OnPropertyChanged(nameof(ShowIdColumn));
                OnPropertyChanged(nameof(ShowNameColumn));
                OnPropertyChanged(nameof(ShowVolumeColumn));
                OnPropertyChanged(nameof(ShowDistanceColumn));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }

        // Voeg nieuwe symbol toe (bijvoorbeeld bij live updates)
        public void AddSymbol(SymbolInfo symbol)
        {
            Symbols.Add(symbol);
            // Optioneel: direct op juiste plek invoegen als gesorteerd
        }

        // Update bestaande symbol
        public void UpdateSymbol(SymbolInfo symbol)
        {
            var existing = Symbols.FirstOrDefault(s => s.Id == symbol.Id);
            if (existing != null)
            {
                existing.Symbol = symbol.Symbol;
                existing.Volume = symbol.Volume;
                existing.Distance = symbol.Distance;
            }
        }

        // Get kolommen gesorteerd op DisplayOrder
        public IEnumerable<GridColumnDefinition> GetColumnsSortedByDisplayOrder()
        {
            return config.Columns.Values.OrderBy(c => c.Index);
        }

        // Update display order (voor drag & drop kolommen)
        public void UpdateColumnOrder(GridColumn column, int newOrder)
        {
            config.Columns[column].Index = newOrder;
            SaveConfigurationToSettings();
        }

        // Get Columns voor een specifieke kolom
        public GridColumnDefinition GetColumnConfig(GridColumn column)
        {
            return config.Columns[column];
        }

        // Get huidige sort kolom
        public GridColumnDefinition? GetSortColumn()
        {
            return config.SortColumn;
        }

        // Get huidige sort direction
        public GridSortDirection? GetSortDirection()
        {
            return config.SortDirection;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Comparer wrapper voor IComparer<T>
    public class SymbolComparer : IComparer<SymbolInfo>
    {
        private readonly GridColumnDefinitions _config;

        public SymbolComparer(GridColumnDefinitions config)
        {
            _config = config;
        }

        public int Compare(SymbolInfo? x, SymbolInfo? y)
        {
            if (x == null || y == null)
                return 0;

            return _config.Compare(x, y);
        }
    }

    //public class RelayCommand : ICommand
    //{
    //    private readonly Action _execute;
    //    public RelayCommand(Action execute) => _execute = execute;
    //    public bool CanExecute(object? parameter) => true;
    //    public void Execute(object? parameter) => _execute();
    //    public event EventHandler? CanExecuteChanged;
    //}
}