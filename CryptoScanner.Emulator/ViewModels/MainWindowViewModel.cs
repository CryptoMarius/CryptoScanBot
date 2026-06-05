using Avalonia.Controls;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Config.Views;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Emulator;
using CryptoScanner.Core.Model;

using System.Diagnostics;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>
/// Glue between the placeholder MainWindow and the emulator engine. Holds run state, exposes
/// commands for the three buttons (Configure scanner, Open run.json, Start/Stop), and surfaces
/// progress for the status bar. Deliberately minimal — once the engine is proven we can swap
/// the JSON file for a proper symbols/dates form.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _appVersion = GlobalData.AppVersion;

    [ObservableProperty]
    private string _appPath = GlobalData.AppPath;

    [ObservableProperty]
    private string _dataFolder = GlobalData.AppDataFolder;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private double _progressMaximum = 1;

    [ObservableProperty]
    private string _currentSymbol = "";

    [ObservableProperty]
    private bool _isRunning;


    private CancellationTokenSource? _cts;


    /// <summary>
    /// Opens the scanner ConfigurationWindow as a modal dialog rooted at this window. The
    /// dialog reads from and writes back to GlobalData.Settings; the settings.json the
    /// emulator just loaded is the one being edited.
    /// </summary>
    [RelayCommand]
    private async Task ConfigureScannerAsync(Window? owner)
    {
        ConfigurationWindow window = new();
        if (owner != null)
            await window.ShowDialog(owner);
        else
            window.Show();
    }


    /// <summary>
    /// Opens emulator-run.json in the OS-default text editor. Quick way to edit symbols/dates
    /// before a proper UI exists. The file is created on first start by RunConfigFile.Load.
    /// </summary>
    [RelayCommand]
    private void OpenRunConfig()
    {
        // Ensure the file exists (Load creates a placeholder if missing).
        RunConfigFile.Load();
        string path = RunConfigFile.FilePath;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Status = $"Could not open {path}: {ex.Message}";
        }
    }


    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning)
            return;

        EmulatorRunConfig config;
        try
        {
            config = RunConfigFile.Load();
        }
        catch (Exception ex)
        {
            Status = $"Failed to read run config: {ex.Message}";
            return;
        }

        if (config.Symbols.Count == 0)
        {
            Status = "Run config has no symbols — edit emulator-run.json first.";
            return;
        }

        IsRunning = true;
        ProgressValue = 0;
        Status = $"Starting run \"{config.Label}\"";

        _cts = new CancellationTokenSource();
        CryptoEmulatorRun? run = null;

        try
        {
            // Tag every signal and position with this run. ConfigJson captures the user's
            // intent (which symbols/period) and the live settings at the moment of start.
            string configJson = System.Text.Json.JsonSerializer.Serialize(config);
            run = EmulatorDb.StartRun(configJson);

            var runner = new TickRunner
            {
                Progress = new Progress<TickRunProgress>(OnTickProgress),
            };
            await runner.RunAsync(config, _cts.Token);

            EmulatorDb.FinishRun("completed");
            Status = $"Run \"{config.Label}\" completed.";
        }
        catch (OperationCanceledException)
        {
            EmulatorDb.FinishRun("cancelled");
            Status = $"Run \"{config.Label}\" cancelled.";
        }
        catch (Exception ex)
        {
            EmulatorDb.FinishRun($"failed: {ex.GetType().Name}");
            Status = $"Run failed: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }


    [RelayCommand]
    private void Stop()
    {
        _cts?.Cancel();
        Status = "Cancelling…";
    }


    private void OnTickProgress(TickRunProgress p)
    {
        // The Progress<T> callback already marshals to the UI thread when constructed on the
        // UI thread; the explicit Post is defensive in case this VM ever runs in a worker.
        Dispatcher.UIThread.Post(() =>
        {
            CurrentSymbol = p.SymbolName;
            ProgressMaximum = Math.Max(1, p.TotalBars);
            ProgressValue = p.ProcessedBars;
            Status = $"{p.SymbolName}: {p.ProcessedBars}/{p.TotalBars}";
        });
    }
}
