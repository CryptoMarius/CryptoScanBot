using Avalonia.Controls;

namespace CryptoScanner.Services;

public interface IDataGridColumnsService
{
    public void LoadColumnSettings(DataGrid dataGrid, string settingsFileName);
    public void SaveColumnSettings(DataGrid dataGrid, string settingsFileName);
}
