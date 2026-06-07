using Avalonia.Controls;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

using System.Collections.ObjectModel;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>
/// First-stage setup dialog: picks the data folder and the exchange the emulator will work
/// against. Runs BEFORE the database is opened, so the exchange list comes from the static
/// seed in <see cref="CryptoDatabase.CreateExchangeList"/> rather than from the DB itself.
/// Once confirmed, App.OnFrameworkInitializationCompleted applies the choices to
/// GlobalData.AppDataFolder + GlobalData.Settings.General.ExchangeName and runs the
/// EmulatorBootstrap.
/// </summary>
public partial class SetupWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _dataFolder;

    [ObservableProperty]
    private ObservableCollection<string> _exchanges = [];

    [ObservableProperty]
    private string? _selectedExchange;

    public bool Confirmed { get; private set; }


    public SetupWindowViewModel()
    {
        // Default folder: last-used (if any) → same rules as the live scanner (IPlatformService
        // + optional --folder argument). The user can still pick a different one in the
        // dialog; we just provide a sensible starting point.
        _dataFolder = LastFolderMemory.Load() ?? ResolveDefaultDataFolder();

        // Populate the exchange combo from the static seed list. Only the supported ones —
        // CreateTableExchange persists all of them in the DB but the unsupported ones cannot
        // actually be used.
        foreach (var exchange in CryptoDatabase.CreateExchangeList())
        {
            if (exchange.IsSupported)
                Exchanges.Add(exchange.Name);
        }

        SelectedExchange = Exchanges.FirstOrDefault();
    }


    [RelayCommand]
    private async Task BrowseFolderAsync(Window? owner)
    {
        if (owner == null)
            return;

        IStorageProvider provider = owner.StorageProvider;
        var folder = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select emulator data folder",
            AllowMultiple = false,
        });
        if (folder.Count > 0)
            DataFolder = folder[0].Path.LocalPath;
    }


    [RelayCommand]
    private void Ok(Window? owner)
    {
        if (string.IsNullOrWhiteSpace(DataFolder) || string.IsNullOrWhiteSpace(SelectedExchange))
            return;

        // Remember the picked folder so next launch lands the user back here without forcing
        // them through the picker again. Exchange isn't persisted — that's run-time choice.
        LastFolderMemory.Save(DataFolder);

        Confirmed = true;
        owner?.Close();
    }


    [RelayCommand]
    private void Cancel(Window? owner)
    {
        Confirmed = false;
        owner?.Close();
    }


    /// <summary>
    /// Same resolution chain Program.cs used before this dialog existed: honour --folder
    /// when given, otherwise default to "{AppName}/Emulator" through the OS-specific
    /// IPlatformService. Keeps cross-platform parity with the live scanner.
    /// </summary>
    private static string ResolveDefaultDataFolder()
    {
        ApplicationParams.InitApplicationOptions();
        ApplicationParams.Options ??= new ApplicationParams();
        if (string.IsNullOrEmpty(ApplicationParams.Options.AppDataFolder))
            ApplicationParams.Options.AppDataFolder = Path.Combine(Constants.AppName, "Emulator");

        IPlatformService platformService = OperatingSystem.IsWindows()
            ? new WindowsPlatformService()
            : OperatingSystem.IsMacOS()
                ? new MacOSPlatformService()
                : OperatingSystem.IsLinux()
                    ? new LinuxPlatformService()
                    : throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion.Platform}");

        return platformService.GetDataDirectory();
    }
}
