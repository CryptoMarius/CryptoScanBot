using Avalonia.Controls;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CryptoScanner.Services;

/// <summary>
/// Represents saved settings for a single column
/// </summary>
public class DataGridColumnSetting
{
    public string Header { get; set; } = string.Empty;
    public double Width { get; set; }
    public int DisplayIndex { get; set; }
    public bool IsVisible { get; set; }
}

/// <summary>
/// Container for all column settings
/// </summary>
public class DataGridColumnSettings
{
    public List<DataGridColumnSetting> Columns { get; set; } = [];
}


public class DataGridColumnsService : IDataGridColumnsService
{
    private readonly IJsonSerializerService? _jsonService;


    public DataGridColumnsService()
    {
        // Get services from DI container
        _jsonService = App.GetService<IJsonSerializerService>()
            ?? throw new InvalidOperationException("IJsonSerializerService not registered");
    }

    /// <summary>
    /// Load saved column settings (width, order, visibility) from JSON file
    /// </summary>
    public void LoadColumnSettings(DataGrid dataGrid, string settingsFileName)
    {
        if (dataGrid == null)
            return;

        try
        {
            if (!File.Exists(settingsFileName))
                return;

            var json = File.ReadAllText(settingsFileName);
            var settings = JsonSerializer.Deserialize<DataGridColumnSettings>(json);

            if (settings?.Columns == null)
                return;

            // Apply saved settings to columns
            foreach (var columnSetting in settings.Columns)
            {
                var column = dataGrid.Columns.FirstOrDefault(c =>
                    c.Header?.ToString() == columnSetting.Header);

                if (column != null)
                {
                    column.Width = new DataGridLength(columnSetting.Width);
                    column.DisplayIndex = columnSetting.DisplayIndex;
                    column.IsVisible = columnSetting.IsVisible;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading column settings: {ex.Message}");
        }
    }



    /// <summary>
    /// Save column settings to JSON file
    /// </summary>
    public void SaveColumnSettings(DataGrid dataGrid, string settingsFileName)
    {
        if (dataGrid == null)
            return;

        try
        {
            var settings = new DataGridColumnSettings();

            foreach (var column in dataGrid.Columns)
            {
                settings.Columns.Add(new DataGridColumnSetting
                {
                    Header = column.Header?.ToString() ?? "Unknown",
                    Width = column.ActualWidth,
                    DisplayIndex = column.DisplayIndex,
                    IsVisible = column.IsVisible
                });
            }

            var json = JsonSerializer.Serialize(settings, _jsonService!.IndentedOptions);
            File.WriteAllText(settingsFileName, json);
            System.Diagnostics.Debug.WriteLine($"Saved column settings to: {settingsFileName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving column settings: {ex.Message}");
        }
    }

}
