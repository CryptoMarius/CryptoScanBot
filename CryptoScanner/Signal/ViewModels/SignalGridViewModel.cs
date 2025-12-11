using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Signal.Common;
using CryptoScanner.Signal.Model;

namespace CryptoScanner.Signal.ViewModels
{
    public class SignalGridViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SignalInfo> _signals = new();
        public ObservableCollection<SignalInfo> Signals {get => _signals; set { _signals = value; OnPropertyChanged();}}
        

        public SignalGridViewModel()
        {
            System.Diagnostics.Debug.WriteLine("SignalGridViewModel constructor called");

            if (SignalShared.Columns.Columns.Count == 0)
                SignalShared.Columns.DefaultColumnDefinition();

            // Laad symbols direct in de observable collection
            foreach (var signal in GlobalData.SignalQueue)
            {
                Signals.Add(new SignalInfo
                {
                    SignalObject = signal,
                });
            }

            // Laad opgeslagen configuratie
            LoadConfigurationFromSettings();

            // Sorteer als er een sort configuratie is
            if (SignalShared.Columns.SortColumn != null)
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
                !Enum.TryParse<ColumnEnum>(sortMemberPath, true, out ColumnEnum gridColumn))
            {
                e.Handled = true;
                return;
            }

            Common.ColumnDefinition column = SignalShared.Columns.Columns[gridColumn];

            // Toggle sort direction
            if (SignalShared.Columns.SortColumn != null && SignalShared.Columns.SortColumn == column)
            {
                // Zelfde kolom, verander richting
                SignalShared.Columns.SortDirection = SignalShared.Columns.SortDirection == GridSortDirection.Ascending
                    ? GridSortDirection.Descending
                    : GridSortDirection.Ascending;
            }
            else
            {
                // Nieuwe kolom, start met Ascending
                SignalShared.Columns.SortColumn = column;
                SignalShared.Columns.SortDirection = GridSortDirection.Ascending;
            }

            System.Diagnostics.Debug.WriteLine($"Sorting by {column.Column} - {SignalShared.Columns.SortDirection}");

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
            var sorted = Signals.OrderBy(s => s, new SymbolComparer(SignalShared.Columns)).ToList();

            // Vervang collectie - dit is snel genoeg voor 10K items
            Signals = new ObservableCollection<SignalInfo>(sorted);
        }

        // Methoden voor het opslaan/laden van configuratie
        public string SaveColumnConfiguration()
        {
            var configs = SignalShared.Columns.Columns.Values
                .OrderBy(c => c.Index)
                .Select(c => $"{(int)c.Column},{c.Visible},{c.Index},{c.Width}");

            var sortConfig = SignalShared.Columns.SortColumn != null
                ? $"|{(int)SignalShared.Columns.SortColumn.Column},{(int)SignalShared.Columns.SortDirection!.Value}"
                : string.Empty;

            return string.Join(";", configs) + sortConfig;
        }

        public void LoadColumnConfiguration(string configString)
        {
            return; // terrible
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

                    var column = (ColumnEnum)int.Parse(items[0]);
                    var visible = bool.Parse(items[1]);
                    var index = int.Parse(items[2]);
                    var width = int.Parse(items[3]);

                    SignalShared.Columns.Columns[column].Visible = visible;
                    SignalShared.Columns.Columns[column].Index = index;
                    SignalShared.Columns.Columns[column].Width = width;
                }

                // Laad sort configuratie
                if (parts.Length > 1)
                {
                    var sortItems = parts[1].Split(',');
                    if (sortItems.Length == 2)
                    {
                        var sortColumn = (ColumnEnum)int.Parse(sortItems[0]);
                        var sortDirection = (GridSortDirection)int.Parse(sortItems[1]);

                        SignalShared.Columns.SortColumn = SignalShared.Columns.Columns[sortColumn];
                        SignalShared.Columns.SortDirection = sortDirection;
                    }
                }

                //// Update de bool properties
                //_showIdColumn = Columns.Columns[ColumnEnum.Id].Visible;
                //_showSymbolColumn = Columns.Columns[ColumnEnum.Symbol].Visible;
                //_showVolumeColumn = Columns.Columns[ColumnEnum.Volume].Visible;
                //_showDistanceColumn = Columns.Columns[ColumnEnum.Distance].Visible;

                //// Notify all column visibility properties
                //OnPropertyChanged(nameof(ShowIdColumn));
                //OnPropertyChanged(nameof(ShowSymbolColumn));
                //OnPropertyChanged(nameof(ShowVolumeColumn));
                //OnPropertyChanged(nameof(ShowDistanceColumn));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }

        // Voeg nieuwe symbol toe (bijvoorbeeld bij live updates)
        public void AddSymbol(SignalInfo symbol)
        {
            Signals.Add(symbol);
            // Optioneel: direct op juiste plek invoegen als gesorteerd
        }

        // Update bestaande symbol
        public void UpdateSymbol(SignalInfo symbol)
        {
            //why?
            //var existing = Signals.FirstOrDefault(s => s.Id == symbol.Id);
            //if (existing != null)
            //{
                //existing.Symbol = symbol.Symbol;
                //existing.Volume = symbol.Volume;
                //existing.Distance = symbol.Distance;
            //}
        }

        // Get kolommen gesorteerd op DisplayOrder
        public IEnumerable<Common.ColumnDefinition> GetColumnsSortedByDisplayOrder()
        {
            return SignalShared.Columns.Columns.Values.OrderBy(c => c.Index);
        }

        // Update display order (voor drag & drop kolommen)
        public void UpdateColumnOrder(ColumnEnum column, int newOrder)
        {
            SignalShared.Columns.Columns[column].Index = newOrder;
            SaveConfigurationToSettings();
        }

        // Get Columns voor een specifieke kolom
        public Common.ColumnDefinition GetColumnConfig(ColumnEnum column)
        {
            return SignalShared.Columns.Columns[column];
        }

        // Get huidige sort kolom
        public Common.ColumnDefinition? GetSortColumn()
        {
            return SignalShared.Columns.SortColumn;
        }

        // Get huidige sort direction
        public GridSortDirection? GetSortDirection()
        {
            return SignalShared.Columns.SortDirection;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Comparer wrapper voor IComparer<T>
    public class SymbolComparer : IComparer<SignalInfo>
    {
        private readonly Common.ColumnDefinitions _config;

        public SymbolComparer(Common.ColumnDefinitions config)
        {
            _config = config;
        }

        public int Compare(SignalInfo? x, SignalInfo? y)
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